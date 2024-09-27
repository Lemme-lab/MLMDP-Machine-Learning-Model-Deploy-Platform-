using k8s;
using k8s.Models;
using Microsoft.AspNetCore.Mvc;
using System;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Text;
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

        [HttpGet("getDeployments")]
        public async Task<IActionResult> GetAllDeployments()
        {
            try
            {
                var deploymentList = await _kubernetesClient.ListNamespacedDeploymentAsync(_namespaceName);
                var stringBuilder = new StringBuilder();

                if (deploymentList.Items.Count == 0)
                {
                    return Ok("No deployments found in the namespace.");
                }

                foreach (var deployment in deploymentList.Items)
                {
                    var creationTimestamp = deployment.Metadata.CreationTimestamp;
                    var replicas = deployment.Spec.Replicas;
                    var availableReplicas = deployment.Status.AvailableReplicas;
                    var readyReplicas = deployment.Status.ReadyReplicas;
                    var labels = string.Join(", ", deployment.Metadata.Labels.Select(l => $"{l.Key}: {l.Value}"));

                    stringBuilder.AppendLine($"Deployment Name: {deployment.Metadata.Name}");
                    stringBuilder.AppendLine($"  Replicas: {replicas}");
                    stringBuilder.AppendLine($"  Available Replicas: {availableReplicas ?? 0}");
                    stringBuilder.AppendLine($"  Ready Replicas: {readyReplicas ?? 0}");
                    stringBuilder.AppendLine($"  Creation Timestamp: {creationTimestamp}");
                    stringBuilder.AppendLine($"  Labels: {labels}");
                    stringBuilder.AppendLine();
                }

                return Ok(stringBuilder.ToString());
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving deployments: {ex.Message}");
            }
        }
        
        [HttpGet("getDeploymentPods/{deploymentName}")]
        public async Task<IActionResult> GetAllPodsForDeployment(string deploymentName)
        {
            try
            {
                var podList = await _kubernetesClient.ListNamespacedPodAsync(_namespaceName);
                var stringBuilder = new StringBuilder();

                if (podList.Items.Count == 0)
                {
                    return Ok("No pods found in the namespace.");
                }

                // Filter pods based on the deployment selector
                var matchedPods = podList.Items.Where(pod => pod.Metadata.Labels != null &&
                                                             pod.Metadata.Labels.ContainsKey("app") &&
                                                             pod.Metadata.Labels["app"] ==
                                                             deploymentName.Replace("-deployment", "")).ToList();

                if (matchedPods.Count == 0)
                {
                    return Ok($"No pods found for deployment: {deploymentName}");
                }

                foreach (var pod in matchedPods)
                {
                    var nodeName = pod.Spec.NodeName;
                    var containers = string.Join(", ", pod.Spec.Containers.Select(c => c.Image));
                    var labels = string.Join(", ", pod.Metadata.Labels.Select(l => $"{l.Key}: {l.Value}"));
                    var phase = pod.Status.Phase;
                    var startTime = pod.Status.StartTime;

                    stringBuilder.AppendLine($"Pod Name: {pod.Metadata.Name}");
                    stringBuilder.AppendLine($"  Node Name: {nodeName}");
                    stringBuilder.AppendLine($"  Containers: {containers}");
                    stringBuilder.AppendLine($"  Status: {phase}");
                    stringBuilder.AppendLine($"  Start Time: {startTime}");
                    stringBuilder.AppendLine($"  Labels: {labels}");
                    stringBuilder.AppendLine();
                }

                return Ok(stringBuilder.ToString());
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving pods for deployment: {ex.Message}");
            }
        }

        [HttpGet("getallPods")]
        public async Task<IActionResult> GetAllPods()
        {
            try
            {
                List<Pod> PodList = new List<Pod>();
                var podList = await _kubernetesClient.ListNamespacedPodAsync(_namespaceName);
                var stringBuilder = new StringBuilder();

                foreach (var pod in podList.Items)
                {
                    try
                    {
                        var podLabels = pod.Metadata.Labels;
                        var nodeName = pod.Spec.NodeName;
                        var containers = string.Join(", ", pod.Spec.Containers.Select(c => c.Image));
                        var phase = pod.Status.Phase;
                        var startTime = pod.Status.StartTime;
                        var resources = string.Join(", ", pod.Spec.Containers.Select(c =>
                            $"Container: {c.Name}, CPU: {c.Resources?.Requests["cpu"]}, Memory: {c.Resources?.Requests["memory"]}"));

                        if (podLabels == null || podLabels.Count == 0)
                        {
                            return NotFound("The pod does not have any labels.");
                        }

                        var serviceList = await _kubernetesClient.ListNamespacedServiceAsync(_namespaceName);

                        foreach (var service in serviceList.Items)
                        {
                            if (service.Spec.Selector != null && service.Spec.Selector.All(selector =>
                                    podLabels.ContainsKey(selector.Key) && podLabels[selector.Key] == selector.Value))
                            {
                                stringBuilder.AppendLine($"Pod Name: {pod.Metadata.Name}");
                                stringBuilder.AppendLine($"  Node Name: {nodeName}");
                                stringBuilder.AppendLine($"  Containers: {containers}");
                                stringBuilder.AppendLine($"  Status: {phase}");
                                stringBuilder.AppendLine($"  Start Time: {startTime}");
                                stringBuilder.AppendLine($"  Resources: {resources}");
                                stringBuilder.AppendLine($"  Cluster IP: {service.Spec.ClusterIP}");
                                stringBuilder.AppendLine(
                                    $"  Ports: {string.Join(", ", service.Spec.Ports.Select(p => $"Port {p.Port}"))}");
                                stringBuilder.AppendLine();
                            }
                        }
                    }
                    catch (Exception ex)
                    {
                        return StatusCode(500, $"Error finding service for pod: {ex.Message}");
                    }
                }

                return Ok(stringBuilder.ToString());
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving pods: {ex.Message}");
            }
        }
        
        [HttpPost("stopPod")]
        public async Task<IActionResult> StopPod([FromBody] PodRequest request)
        {
            try
            {
                // Extract the deployment name by removing the pod-specific suffix after "-deployment"
                var deploymentName = request.PodName.Contains("-deployment")
                    ? request.PodName.Substring(0, request.PodName.IndexOf("-deployment") + "-deployment".Length)
                    : request.PodName;

                // Get the existing deployment
                var deployment = await _kubernetesClient.ReadNamespacedDeploymentAsync(deploymentName, _namespaceName);

                // Set the replicas to 0 to halt the deployment
                deployment.Spec.Replicas = 0;

                // Update the deployment with the new replica count
                await _kubernetesClient.ReplaceNamespacedDeploymentAsync(deployment, deploymentName, _namespaceName);

                return Ok($"Deployment {deploymentName} halted successfully (scaled to 0 replicas).");
            }
            catch (HttpOperationException e)
            {
                var errorContent = e.Response.Content;
                return StatusCode(500, $"Error halting deployment: {errorContent}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpPost("startPod")]
        public async Task<IActionResult> StartPod([FromBody] PodRequest request)
        {
            try
            {
                // Extract the deployment name by removing the pod-specific suffix after "-deployment"
                var deploymentName = request.PodName.Contains("-deployment")
                    ? request.PodName.Substring(0, request.PodName.IndexOf("-deployment") + "-deployment".Length)
                    : request.PodName;

                // Get the existing deployment
                var deployment = await _kubernetesClient.ReadNamespacedDeploymentAsync(deploymentName, _namespaceName);

                // Set the replicas to 1 (or more) to start the deployment
                deployment.Spec.Replicas = 1; // You can modify this value if you want to scale to more replicas

                // Update the deployment with the new replica count
                await _kubernetesClient.ReplaceNamespacedDeploymentAsync(deployment, deploymentName, _namespaceName);

                return Ok($"Deployment {deploymentName} started successfully (scaled to 1 replica).");
            }
            catch (HttpOperationException e)
            {
                var errorContent = e.Response.Content;
                return StatusCode(500, $"Error starting deployment: {errorContent}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"An error occurred: {ex.Message}");
            }
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeletePod([FromBody] PodRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.PodName))
            {
                return BadRequest("PodName is required.");
            }

            var podName = request.PodName;
            var sanitizedPodName = podName.ToLower().Replace("_", "-");

            // Remove the '-deployment' suffix and get the base name
            var basePodName = sanitizedPodName.Contains("-deployment")
                ? sanitizedPodName.Split("-deployment")[0]
                : sanitizedPodName;

            var deleteOptions = new V1DeleteOptions();
            var deletedResources = new List<string>();
            var failedResources = new List<string>();

            // Attempt to delete the pod
            try
            {
                Console.WriteLine($"Attempting to delete pod: {podName}");
                await _kubernetesClient.DeleteNamespacedPodAsync(podName, _namespaceName, deleteOptions);
                Console.WriteLine($"Pod '{podName}' deleted successfully.");
                deletedResources.Add($"Pod: {podName}");
            }
            catch (HttpOperationException e) when (e.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine($"Pod '{podName}' not found.");
                failedResources.Add($"Pod: {podName}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to delete pod: {e.Message}");
                failedResources.Add($"Pod: {podName}");
            }

            // Attempt to delete the deployment
            var deploymentName = $"{basePodName}-deployment";
            try
            {
                Console.WriteLine($"Attempting to delete deployment: {deploymentName}");
                await _kubernetesClient.DeleteNamespacedDeploymentAsync(deploymentName, _namespaceName, deleteOptions);
                Console.WriteLine($"Deployment '{deploymentName}' deleted successfully.");
                deletedResources.Add($"Deployment: {deploymentName}");
            }
            catch (HttpOperationException e) when (e.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine($"Deployment '{deploymentName}' not found.");
                failedResources.Add($"Deployment: {deploymentName}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to delete deployment: {e.Message}");
                failedResources.Add($"Deployment: {deploymentName}");
            }

            // Attempt to delete the service
            var serviceName = $"python-service-{basePodName}";
            try
            {
                Console.WriteLine($"Attempting to delete service: {serviceName}");
                await _kubernetesClient.DeleteNamespacedServiceAsync(serviceName, _namespaceName, deleteOptions);
                Console.WriteLine($"Service '{serviceName}' deleted successfully.");
                deletedResources.Add($"Service: {serviceName}");
            }
            catch (HttpOperationException e) when (e.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine($"Service '{serviceName}' not found.");
                failedResources.Add($"Service: {serviceName}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to delete service: {e.Message}");
                failedResources.Add($"Service: {serviceName}");
            }

            // Attempt to delete the HPA
            var hpaName = $"{basePodName}-hpa";
            try
            {
                Console.WriteLine($"Attempting to delete HPA: {hpaName}");
                await _kubernetesClient.DeleteNamespacedHorizontalPodAutoscalerAsync(hpaName, _namespaceName,
                    deleteOptions);
                Console.WriteLine($"HPA '{hpaName}' deleted successfully.");
                deletedResources.Add($"HPA: {hpaName}");
            }
            catch (HttpOperationException e) when (e.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                Console.WriteLine($"HPA '{hpaName}' not found.");
                failedResources.Add($"HPA: {hpaName}");
            }
            catch (Exception e)
            {
                Console.WriteLine($"Failed to delete HPA: {e.Message}");
                failedResources.Add($"HPA: {hpaName}");
            }

            // Return a structured JSON response
            return Ok(new
            {
                Deleted = deletedResources,
                FailedToDelete = failedResources
            });
        }

        [HttpPost("scalePod")]
        public async Task<IActionResult> ScalePod([FromBody] PodScaleRequest request)
        {
            try
            {
                var deployment = await _kubernetesClient.ReadNamespacedDeploymentAsync(request.PodName, _namespaceName);
                deployment.Spec.Replicas = request.Replicas;
                await _kubernetesClient.ReplaceNamespacedDeploymentAsync(deployment, request.PodName, _namespaceName);
                return Ok($"Deployment {request.PodName} scaled to {request.Replicas} replicas.");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to scale pod: {ex.Message}");
            }
        }

        [HttpGet("getPodLogs/{podName}")]
        public async Task<IActionResult> GetPodLogs(string podName)
        {
            try
            {
                var logs = await _kubernetesClient.ReadNamespacedPodLogAsync(podName, _namespaceName);
                return Ok(logs);
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Failed to retrieve logs: {ex.Message}");
            }
        }

        [HttpPost("uploadModel")]
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

            return Ok(new
            {
                Message = $"Model {file.FileName} uploaded successfully", Deployment = deploymentResult,
                Service = serviceResult
            });
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
                await CreateHPA(sanitizedModelName); // Add HPA for auto-scaling
            }
            catch (HttpOperationException e)
            {
                var errorContent = e.Response.Content;
                Console.WriteLine($"Failed to create deployment: {errorContent}");
                throw new Exception($"Error creating deployment: {errorContent}");
            }

            return $"Deployment for model {sanitizedModelName} created successfully in namespace {namespaceName}.";
        }

        private async Task CreateHPA(string modelName)
        {
            var sanitizedModelName = modelName.ToLower().Replace("_", "-");

            var hpa = new V1HorizontalPodAutoscaler
            {
                ApiVersion = "autoscaling/v1",
                Kind = "HorizontalPodAutoscaler",
                Metadata = new V1ObjectMeta
                {
                    Name = $"{sanitizedModelName}-hpa",
                    NamespaceProperty = _namespaceName
                },
                Spec = new V1HorizontalPodAutoscalerSpec
                {
                    ScaleTargetRef = new V1CrossVersionObjectReference
                    {
                        ApiVersion = "apps/v1",
                        Kind = "Deployment",
                        Name = $"{sanitizedModelName}-deployment"
                    },
                    MinReplicas = 1,
                    MaxReplicas = 10,
                    TargetCPUUtilizationPercentage = 50 // Scale based on 50% CPU utilization
                }
            };

            try
            {
                await _kubernetesClient.CreateNamespacedHorizontalPodAutoscalerAsync(hpa, _namespaceName);
                Console.WriteLine($"HPA for model {sanitizedModelName} created successfully.");
            }
            catch (HttpOperationException e)
            {
                var errorContent = e.Response.Content;
                Console.WriteLine($"Failed to create HPA: {errorContent}");
                throw new Exception($"Error creating HPA: {errorContent}");
            }
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
                            Port = 80, // Expose on port 80
                            TargetPort = 8000 // Target port inside Python container
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

        public class PodScaleRequest
        {
            public string PodName { get; set; }
            public int Replicas { get; set; }
        }
    }
}