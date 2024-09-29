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
using Newtonsoft.Json;

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

                if (deploymentList.Items.Count == 0)
                {
                    return Ok(new { Message = "No deployments found in the namespace." });
                }

                var deployments = new List<object>();

                foreach (var deployment in deploymentList.Items)
                {
                    object serviceDetails = null;
                    try
                    {
                        var deploymentName = deployment.Metadata.Name;

                        // Remove '-deployment' suffix from the deployment name to match the service name
                        var serviceName = $"python-service-{deploymentName.Replace("-deployment", "")}";

                        // Fetch the service using the constructed service name
                        var service = await _kubernetesClient.ReadNamespacedServiceAsync(serviceName, _namespaceName);

                        if (service != null)
                        {
                            serviceDetails = new
                            {
                                ClusterIP = service.Spec.ClusterIP,
                                Ports = service.Spec.Ports?.Select(port =>
                                    new { port.Port, port.TargetPort, port.Protocol })
                            };
                        }
                        else
                        {
                            serviceDetails = new { Error = $"Service not found for deployment: {deploymentName}" };
                        }
                    }
                    catch (Exception serviceEx)
                    {
                        // Handle service retrieval errors
                        serviceDetails = new { Error = $"Error retrieving service: {serviceEx.Message}" };
                    }

                    deployments.Add(new
                    {
                        Name = deployment.Metadata.Name,
                        Namespace = deployment.Metadata.NamespaceProperty,
                        Replicas = deployment.Spec.Replicas,
                        AvailableReplicas = deployment.Status.AvailableReplicas ?? 0,
                        ReadyReplicas = deployment.Status.ReadyReplicas ?? 0,
                        CreationTimestamp = deployment.Metadata.CreationTimestamp,
                        Labels = deployment.Metadata.Labels ?? new Dictionary<string, string>(),
                        Annotations = deployment.Metadata.Annotations ?? new Dictionary<string, string>(),
                        Selector = deployment.Spec.Selector?.MatchLabels ?? new Dictionary<string, string>(),
                        Strategy = deployment.Spec.Strategy?.Type ?? "RollingUpdate",
                        MinReadySeconds = deployment.Spec.MinReadySeconds,
                        RevisionHistoryLimit = deployment.Spec.RevisionHistoryLimit ?? 10,
                        Conditions = deployment.Status.Conditions?.Select(c => new
                        {
                            Type = c.Type,
                            Status = c.Status,
                            LastTransitionTime = c.LastTransitionTime
                        }).ToList(),
                        PodTemplate = deployment.Spec.Template.Spec.Containers.Select(container => new
                        {
                            ContainerName = container.Name,
                            Image = container.Image,
                            Ports = container.Ports?.Select(port => new { port.ContainerPort, port.Protocol }),
                            Resources = new
                            {
                                Requests = container.Resources?.Requests,
                                Limits = container.Resources?.Limits
                            },
                            Env = container.Env?.Select(envVar => new { envVar.Name, envVar.Value }),
                            ImagePullPolicy = container.ImagePullPolicy
                        }),
                        OwnerReferences = deployment.Metadata.OwnerReferences?.Select(owner => new
                        {
                            OwnerName = owner.Name,
                            OwnerKind = owner.Kind
                        }),
                        Volumes = deployment.Spec.Template.Spec.Volumes?.Select(volume => new
                        {
                            Name = volume.Name,
                            VolumeType = volume.PersistentVolumeClaim != null ? "PersistentVolumeClaim" : "Other",
                            ClaimName = volume.PersistentVolumeClaim?.ClaimName
                        }),
                        Service = serviceDetails // Add service details or error message
                    });
                }

                // Sort deployments by CreationTimestamp in descending order
                var sortedDeployments = deployments
                    .OrderByDescending(d => ((DateTimeOffset)((dynamic)d).CreationTimestamp))
                    .ToList();

                return Ok(sortedDeployments);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = $"Error retrieving deployments: {ex.Message}" });
            }
        }
        
        [HttpGet("getDeploymentPods/{deploymentName}")]
        public async Task<IActionResult> GetAllPodsForDeployment(string deploymentName)
        {
            try
            {
                var podList = await _kubernetesClient.ListNamespacedPodAsync(_namespaceName);

                if (podList.Items.Count == 0)
                {
                    return Ok(new { Message = "No pods found in the namespace." });
                }

                // Filter pods based on the deployment selector
                var matchedPods = podList.Items.Where(pod => pod.Metadata.Labels != null &&
                                                             pod.Metadata.Labels.ContainsKey("app") &&
                                                             pod.Metadata.Labels["app"] ==
                                                             deploymentName.Replace("-deployment", "")).ToList();

                if (matchedPods.Count == 0)
                {
                    return Ok(new { Message = $"No pods found for deployment: {deploymentName}" });
                }

                // Create a list of pod details to return as JSON
                var podDetails = matchedPods.Select(pod => new
                {
                    PodName = pod.Metadata.Name,
                    NodeName = pod.Spec.NodeName,
                    Containers = pod.Spec.Containers.Select(c => new
                    {
                        c.Name,
                        c.Image
                    }).ToList(),
                    Status = pod.Status.Phase,
                    StartTime = pod.Status.StartTime,
                    Labels = pod.Metadata.Labels.Select(l => new { Key = l.Key, Value = l.Value }).ToList()
                }).ToList();

                return Ok(podDetails);
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { Error = $"Error retrieving pods for deployment: {ex.Message}" });
            }
        }

        [HttpGet("getallPods")]
        public async Task<IActionResult> GetAllPods()
        {
            try
            {
                var podList = await _kubernetesClient.ListNamespacedPodAsync(_namespaceName);
                var pods = new List<object>(); // Create a list to store pod information in JSON format

                foreach (var pod in podList.Items)
                {
                    string podLogs = string.Empty;

                    try
                    {
                        // Read the logs for the pod using StreamReader to convert Stream to string
                        using (var stream =
                               await _kubernetesClient.ReadNamespacedPodLogAsync(pod.Metadata.Name, _namespaceName))
                        using (var reader = new StreamReader(stream))
                        {
                            podLogs = await reader.ReadToEndAsync(); // Convert stream to string
                        }
                    }
                    catch (Exception logEx)
                    {
                        podLogs = $"Error retrieving logs for pod {pod.Metadata.Name}: {logEx.Message}";
                    }

                    var podInfo = new
                    {
                        PodName = pod.Metadata.Name,
                        NodeName = pod.Spec.NodeName,
                        Containers = pod.Spec.Containers.Select(c => c.Image),
                        Status = pod.Status.Phase,
                        StartTime = pod.Status.StartTime,
                        Resources = pod.Spec.Containers.Select(c => new
                        {
                            ContainerName = c.Name,
                            CPU = c.Resources?.Requests?["cpu"],
                            Memory = c.Resources?.Requests?["memory"]
                        }),
                        ClusterIP = pod.Status.PodIP, // Use PodIP here
                        Ports = pod.Spec.Containers.SelectMany(c => c.Ports.Select(p => p.ContainerPort)),
                        Logs = podLogs // Add the logs to the pod information
                    };

                    pods.Add(podInfo); // Add each pod's info to the list
                }

                return Ok(pods); // Return the list as JSON
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error retrieving pods: {ex.Message}");
            }
        }
        
        [HttpDelete("deletePod")]
        public async Task<IActionResult> DeletePod([FromBody] PodRequest request)
        {
            try
            {
                var podName = request.PodName;

                // Delete the pod
                await _kubernetesClient.DeleteNamespacedPodAsync(podName, _namespaceName);

                var response = new
                {
                    PodName = podName,
                    Status = "Success",
                    Message = $"Pod {podName} deleted successfully."
                };

                return Ok(response);
            }
            catch (HttpOperationException e)
            {
                var errorResponse = new
                {
                    Status = "Error",
                    Message = $"Error deleting pod: {e.Response.Content}"
                };
                return StatusCode(500, errorResponse);
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    Status = "Error",
                    Message = $"An error occurred: {ex.Message}"
                };
                return StatusCode(500, errorResponse);
            }
        }
        
        [HttpPost("stopDeployment")]
        public async Task<IActionResult> StopDeployment([FromBody] PodRequest request)
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

                var response = new
                {
                    PodName = request.PodName,
                    DeploymentName = deploymentName,
                    Replicas = 0,
                    Status = "Success",
                    Message = $"Deployment {deploymentName} halted successfully (scaled to 0 replicas)."
                };

                return Ok(response);
            }
            catch (HttpOperationException e)
            {
                var errorResponse = new
                {
                    Status = "Error",
                    Message = $"Error halting deployment: {e.Response.Content}"
                };
                return StatusCode(500, errorResponse);
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    Status = "Error",
                    Message = $"An error occurred: {ex.Message}"
                };
                return StatusCode(500, errorResponse);
            }
        }

        [HttpPost("startDeployment")]
        public async Task<IActionResult> StartDeployment([FromBody] PodRequest request)
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

                var response = new
                {
                    PodName = request.PodName,
                    DeploymentName = deploymentName,
                    Replicas = 1,
                    Status = "Success",
                    Message = $"Deployment {deploymentName} started successfully (scaled to 1 replica)."
                };

                return Ok(response);
            }
            catch (HttpOperationException e)
            {
                var errorResponse = new
                {
                    Status = "Error",
                    Message = $"Error starting deployment: {e.Response.Content}"
                };
                return StatusCode(500, errorResponse);
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    Status = "Error",
                    Message = $"An error occurred: {ex.Message}"
                };
                return StatusCode(500, errorResponse);
            }
        }

        [HttpDelete("delete")]
        public async Task<IActionResult> DeleteDeployment([FromBody] PodRequest request)
        {
            if (request == null || string.IsNullOrEmpty(request.PodName))
            {
                return BadRequest(new { Status = "Error", Message = "PodName is required." });
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
                await _kubernetesClient.DeleteNamespacedPodAsync(podName, _namespaceName, deleteOptions);
                deletedResources.Add($"Pod: {podName}");
            }
            catch (HttpOperationException e) when (e.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                failedResources.Add($"Pod: {podName}");
            }
            catch (Exception e)
            {
                failedResources.Add($"Pod: {podName}");
            }

            // Attempt to delete the deployment
            var deploymentName = $"{basePodName}-deployment";
            try
            {
                await _kubernetesClient.DeleteNamespacedDeploymentAsync(deploymentName, _namespaceName, deleteOptions);
                deletedResources.Add($"Deployment: {deploymentName}");
            }
            catch (HttpOperationException e) when (e.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                failedResources.Add($"Deployment: {deploymentName}");
            }
            catch (Exception e)
            {
                failedResources.Add($"Deployment: {deploymentName}");
            }

            // Attempt to delete the service
            var serviceName = $"python-service-{basePodName}";
            try
            {
                await _kubernetesClient.DeleteNamespacedServiceAsync(serviceName, _namespaceName, deleteOptions);
                deletedResources.Add($"Service: {serviceName}");
            }
            catch (HttpOperationException e) when (e.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                failedResources.Add($"Service: {serviceName}");
            }
            catch (Exception e)
            {
                failedResources.Add($"Service: {serviceName}");
            }

            // Attempt to delete the HPA
            var hpaName = $"{basePodName}-hpa";
            try
            {
                await _kubernetesClient.DeleteNamespacedHorizontalPodAutoscalerAsync(hpaName, _namespaceName,
                    deleteOptions);
                deletedResources.Add($"HPA: {hpaName}");
            }
            catch (HttpOperationException e) when (e.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                failedResources.Add($"HPA: {hpaName}");
            }
            catch (Exception e)
            {
                failedResources.Add($"HPA: {hpaName}");
            }

            return Ok(new
            {
                Status = "Completed",
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

                var response = new
                {
                    PodName = request.PodName,
                    Replicas = request.Replicas,
                    Status = "Success",
                    Message = $"Deployment {request.PodName} scaled to {request.Replicas} replicas."
                };

                return Ok(response);
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    Status = "Error",
                    Message = $"Failed to scale pod: {ex.Message}"
                };
                return StatusCode(500, errorResponse);
            }
        }
        
        [HttpPost("scaleDownPod")]
        public async Task<IActionResult> ScaleDownPod([FromBody] PodScaleRequest request)
        {
            try
            {
                var deployment = await _kubernetesClient.ReadNamespacedDeploymentAsync(request.PodName, _namespaceName);
        
                // Scale down by 1 if replicas are greater than 0
                var currentReplicas = deployment.Spec.Replicas ?? 1;  // Default to 1 if null
                if (currentReplicas > 1)
                {
                    deployment.Spec.Replicas = currentReplicas - 1;
                    await _kubernetesClient.ReplaceNamespacedDeploymentAsync(deployment, request.PodName, _namespaceName);

                    var response = new
                    {
                        PodName = request.PodName,
                        Replicas = deployment.Spec.Replicas,
                        Status = "Success",
                        Message = $"Deployment {request.PodName} scaled down by 1. New replica count: {deployment.Spec.Replicas}."
                    };

                    return Ok(response);
                }
                else
                {
                    var errorResponse = new
                    {
                        Status = "Error",
                        Message = "Cannot scale down, replicas are already at the minimum value of 1."
                    };
                    return BadRequest(errorResponse);
                }
            }
            catch (Exception ex)
            {
                var errorResponse = new
                {
                    Status = "Error",
                    Message = $"Failed to scale down pod: {ex.Message}"
                };
                return StatusCode(500, errorResponse);
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

        [HttpPost("callDeploymentAPI")]
        public async Task<IActionResult> CallDeploymentAPI([FromBody] DeploymentRequest request)
        {
            try
            {
                using (var httpClient = new HttpClient())
                {
                    var url = $"http://{request.Ip}/predict/";
                    var requestData = new
                    {
                        features = request.Features
                    };

                    var jsonData = JsonConvert.SerializeObject(requestData);
                    var content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                    // Send the POST request to the Python API
                    var httpResponse = await httpClient.PostAsync(url, content);
                    var responseContent = await httpResponse.Content.ReadAsStringAsync();

                    Console.WriteLine($"Response Status Code: {httpResponse.StatusCode}");
                    Console.WriteLine($"Response Content: {responseContent}");

                    if (httpResponse.IsSuccessStatusCode)
                    {
                        // Deserialize the response to extract the 'prediction'
                        var jsonResponse = JsonConvert.DeserializeObject<dynamic>(responseContent);

                        // Extract the first value from the 'prediction' array
                        var prediction = jsonResponse.prediction != null && jsonResponse.prediction[0] != null
                            ? jsonResponse.prediction[0][0].ToString()
                            : "No prediction found";

                        return Ok(new { Status = "Success", Prediction = prediction });
                    }
                    else
                    {
                        return StatusCode((int)httpResponse.StatusCode,
                            new { Status = "Error", Details = responseContent });
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Exception occurred: {ex.Message}");
                return StatusCode(500, new { Status = "Error", Message = ex.Message });
            }
        }
        
        private async Task<string> DeployModelPod(string modelName)
        {
            var namespaceName = _namespaceName;

            // Ensure the namespace exists
            await EnsureNamespaceExists(namespaceName);

            var sanitizedModelName = modelName.ToLower().Replace("_", "-");
            var image = "ml-pythonenv:latest"; // Your Docker image

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

        public class PodRequest
        {
            public string? PodName { get; set; }
        }
        
        public class DeploymentRequest
        {
            public string Ip { get; set; }
            public List<double> Features { get; set; }
        }


    }
}