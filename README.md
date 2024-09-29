# MLMDP - Machine Learning Models Deployment Platform

MLMDP is a Kubernetes-based platform for deploying, managing, and monitoring machine learning models at scale. With features for model deployment, scaling, logs, and monitoring, MLMDP simplifies the process of running ML models in production.

## Features

- **Dynamic Model Deployment**: Upload machine learning models through a drag-and-drop interface, and deploy them on the Kubernetes cluster with persistent volume claims for model storage.
- **Kubernetes Integration**: Seamlessly interact with Kubernetes deployments, services, and pods.
- **Horizontal Pod Autoscaling (HPA)**: Automatically scale ML model pods based on CPU utilization.
- **Logs and Monitoring**: View detailed logs of running pods and monitor their status.
- **Model API**: Each deployed model is exposed via a RESTful API, making it easy to send data and retrieve predictions.
- **Service Exposure**: Deployed models are exposed via Kubernetes LoadBalancer services, with service IPs and ports dynamically allocated.

## Screenshots

### Deployment Dashboard

<img width="1728" alt="Screenshot 2024-09-29 at 15 21 23" src="https://github.com/user-attachments/assets/0137b8d8-9ff5-4a22-b895-a29f2635406e">
<img width="1728" alt="Screenshot 2024-09-29 at 15 21 38" src="https://github.com/user-attachments/assets/23262c2a-63a0-4bdf-819d-5cb9445f1286">


- Monitor the status of deployments with real-time logs, replica counts, and API endpoints.
  
### Pod Monitoring

<img width="1728" alt="Screenshot 2024-09-29 at 16 09 17" src="https://github.com/user-attachments/assets/91bc476d-de59-4d36-95c3-6808e82a0b0e">

- View detailed pod logs and status, including node information, container images, and resource usage.

## Getting Started

### Prerequisites

- Kubernetes cluster (Minikube or cloud provider)
- Docker for building model images
- .NET 6 SDK for API development

### Setup

1. Clone the repository:
   ```bash
   git clone https://github.com/yourusername/mlmdp.git
   cd mlmdp
   ```

2. Build and deploy the MLMDP components:
   ```bash
   kubectl apply -f k8s/deployment.yaml
   ```

3. Upload your machine learning models through the platform's interface, or use the `/api/ControlPlane/uploadModel` API endpoint to deploy models programmatically.

4. Access the MLMDP UI through the Kubernetes service endpoint:
   ```bash
   minikube service frontend-ui-service --url
   ```

### API Endpoints

- `GET /api/ControlPlane/getDeployments`: Retrieve all model deployments.
- `GET /api/ControlPlane/getDeploymentPods/{deploymentName}`: Retrieve all pods associated with a specific deployment.
- `POST /api/ControlPlane/uploadModel`: Upload and deploy a machine learning model.
- `POST /api/ControlPlane/scalePod`: Scale the number of replicas for a specific pod.
- `POST /api/ControlPlane/callDeploymentAPI`: Call the prediction API of a deployed model pod with feature data.

### Example

To send data to a deployed model:
```bash
curl -X POST http://{service_ip}:80/predict/ \
   -H 'Content-Type: application/json' \
   -d '{"features": [1.0, 2.0, 3.0, 4.0]}'
```

### Scaling and Monitoring

MLMDP leverages Kubernetes’ built-in HPA to automatically scale pods based on CPU usage. You can also manually scale replicas using the `/scalePod` API.

### Deleting Pods and Deployments

You can easily delete any pod or deployment using the `/delete` API endpoint or via the MLMDP interface.

## Contributing

We welcome contributions! Please submit a pull request or open an issue if you find bugs or have suggestions for new features.

## License

This project is licensed under the MIT License - see the [LICENSE](LICENSE) file for details.

---

This README provides a comprehensive introduction to your platform, including features, setup, and key API endpoints. You can customize it further by replacing the `path_to_image_X` with links to your screenshots in the repository.
