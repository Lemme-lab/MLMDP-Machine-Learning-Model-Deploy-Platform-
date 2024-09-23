using k8s;
using k8s.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Microsoft.Rest;

namespace ControlPlaneAPI.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ControlPlaneController : ControllerBase
    {
        private readonly IKubernetes _kubernetesClient;
        private readonly string _namespaceName;
        private const string ModelDir = "/app/shared-models/";

        public ControlPlaneController(IKubernetes kubernetesClient, string namespaceName)
        {
            _kubernetesClient = kubernetesClient;
            _namespaceName = namespaceName;

            if (!Directory.Exists(ModelDir))
            {
                Directory.CreateDirectory(ModelDir);
            }
        }

        [HttpGet]
        public IActionResult Get()
        {
            return Ok("Control Plane API for managing ML models");
        }

        [HttpPost("upload_model")]
        public async Task<IActionResult> UploadModel(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                return BadRequest("No model file provided.");
            }

            // Use the file name as the pod name (sanitized)
            var modelName = Path.GetFileNameWithoutExtension(file.FileName).ToLower().Replace("_", "-");
            var filePath = Path.Combine(ModelDir, "model.h5");

            // Save the uploaded model as 'model.h5'
            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }

            // Deploy a new pod for the model
            var deploymentResult = await DeployModelPod(modelName);

            // Create a LoadBalancer service to expose the Python API
            var serviceResult = await CreatePythonLoadBalancerService(modelName);

            return Ok(new { Message = $"Model {file.FileName} uploaded successfully", Deployment = deploymentResult, Service = serviceResult });
        }

        private async Task<string> DeployModelPod(string modelName)
        {
            var namespaceName = _namespaceName;

            // Ensure the namespace exists
            await EnsureNamespaceExists(namespaceName);

            var sanitizedModelName = modelName.ToLower().Replace("_", "-");
            var image = "leemme/ml-pythonenv:latest"; // Your Docker image

            var deployment = new V1Deployment
            {
                ApiVersion = "apps/v1",
                Kind = "Deployment",
                Metadata = new V1ObjectMeta
                {
                    Name = $"{sanitizedModelName}-deployment",
                    NamespaceProperty = namespaceName,
                    Labels = new Dictionary<string, string> { { "app", sanitizedModelName } }
                },
                Spec = new V1DeploymentSpec
                {
                    Replicas = 1,
                    Selector = new V1LabelSelector
                    {
                        MatchLabels = new Dictionary<string, string> { { "app", sanitizedModelName } }
                    },
                    Template = new V1PodTemplateSpec
                    {
                        Metadata = new V1ObjectMeta
                        {
                            Labels = new Dictionary<string, string> { { "app", sanitizedModelName } }
                        },
                        Spec = new V1PodSpec
                        {
                            Containers = new List<V1Container>
                            {
                                new V1Container
                                {
                                    Name = $"{sanitizedModelName}-container",
                                    Image = image,
                                    ImagePullPolicy = "IfNotPresent",
                                    Ports = new List<V1ContainerPort>
                                    {
                                        new V1ContainerPort { ContainerPort = 8000 }
                                    },
                                    VolumeMounts = new List<V1VolumeMount>
                                    {
                                        new V1VolumeMount
                                        {
                                            Name = "shared-volume",
                                            MountPath = "/app/models"
                                        }
                                    },
                                    Env = new List<V1EnvVar>
                                    {
                                        new V1EnvVar { Name = "MODEL_NAME", Value = modelName },
                                        new V1EnvVar { Name = "MODEL_DIR", Value = "/app/models" }
                                    }
                                }
                            },
                            Volumes = new List<V1Volume>
                            {
                                new V1Volume
                                {
                                    Name = "shared-volume",
                                    PersistentVolumeClaim = new V1PersistentVolumeClaimVolumeSource
                                    {
                                        ClaimName = "shared-pvc"
                                    }
                                }
                            }
                        }
                    }
                }
            };

            try
            {
                // Create the deployment in the specified namespace
                await _kubernetesClient.CreateNamespacedDeploymentAsync(deployment, namespaceName);
            }
            catch (HttpOperationException e)
            {
                var errorContent = e.Response.Content;
                Console.WriteLine($"Failed to create deployment: {errorContent}");
                throw new Exception($"Error creating deployment: {errorContent}");
            }

            return $"Deployment for model {sanitizedModelName} created successfully in namespace {namespaceName}.";
        }

        private async Task<string> CreatePythonLoadBalancerService(string modelName)
        {
            var sanitizedModelName = modelName.ToLower().Replace("_", "-");

            var service = new V1Service
            {
                ApiVersion = "v1",
                Kind = "Service",
                Metadata = new V1ObjectMeta
                {
                    Name = $"python-service-{sanitizedModelName}",
                    NamespaceProperty = _namespaceName,
                },
                Spec = new V1ServiceSpec
                {
                    Selector = new Dictionary<string, string> { { "app", sanitizedModelName } },
                    Ports = new List<V1ServicePort>
                    {
                        new V1ServicePort
                        {
                            Protocol = "TCP",
                            Port = 80,           // Expose on port 80
                            TargetPort = 8000    // Target port inside Python container
                        }
                    },
                    Type = "LoadBalancer"
                }
            };

            try
            {
                // Create the LoadBalancer service in the specified namespace
                await _kubernetesClient.CreateNamespacedServiceAsync(service, _namespaceName);
            }
            catch (HttpOperationException e)
            {
                var errorContent = e.Response.Content;
                Console.WriteLine($"Failed to create service: {errorContent}");
                throw new Exception($"Error creating service: {errorContent}");
            }

            return $"LoadBalancer Service for model {sanitizedModelName} created successfully.";
        }

        private async Task EnsureNamespaceExists(string namespaceName)
        {
            try
            {
                // Check if the namespace already exists
                await _kubernetesClient.ReadNamespaceAsync(namespaceName);
            }
            catch (HttpOperationException e) when (e.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                // Namespace doesn't exist, so create it
                var namespaceObj = new V1Namespace
                {
                    ApiVersion = "v1",
                    Kind = "Namespace",
                    Metadata = new V1ObjectMeta { Name = namespaceName }
                };
                await _kubernetesClient.CreateNamespaceAsync(namespaceObj);
            }
        }
    }
}
