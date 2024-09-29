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

### Flowchart
![Blank diagram-8](https://github.com/user-attachments/assets/05107e4b-d607-41c6-830a-8707e71b69e5)

## Getting Started

### Prerequisites

- Kubernetes cluster (Minikube or cloud provider)
- Docker for building model images
- .NET 6 SDK for API development

### Setup (Mac Only at the time)

1. Clone the repository:
   ```bash
   git clone https://github.com/Lemme-lab/MLMDP-Machine-Learning-Model-Deploy-Platform-.git
   cd mlmdp
   ```

2. Build and deploy the MLMDP Backend:
   ```bash
   minikube delete
   minikube start
   eval $(minikube docker-env)   
   cd ControlPlaneAPI
   docker build -t csharp-api:latest -f Kubernetes/NetAPI.Dockerfile .     
   kubectl create namespace model-deployments 
   kubectl apply -f kubernetes/Service.yaml  
   kubectl apply -f kubernetes/rbac.yaml 
   kubectl apply -f kubernetes/PersistentVolume.yaml   
   kubectl apply -f kubernetes/Deployment.yaml   
   cd ..
   cd PythonEnv
   docker build -t ml-pythonenv:latest -f PythonEnv.Dockerfile .
   minikube service csharp-api-service -n model-deployments
   ```

3. Check the generated Port and enter it into the Frontend-UI/scr/app/Constants.ts
   
4. Build and deploy the MLMDP Frontend:
   ```bash
   eval $(minikube docker-env)   
   docker build -t frontend-ui:latest -f FrontendUI.Dockerfile .
   kubectl apply -f frontend-ui-deployment.yaml 
   kubectl rollout restart deployment frontend-ui-deployment -n model-deployments 
   minikube service frontend-ui-service -n model-deployments
   ```

5. Upload your machine learning models through the platform's interface, or use the `/api/ControlPlane/uploadModel` API endpoint to deploy models programmatically.

6. Access the MLMDP UI through the Kubernetes service endpoint:
   ```bash
   minikube service frontend-ui-service --url -n model-deployments
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
curl --location 'http://127.0.0.1:52066/api/ControlPlane/callDeploymentAPI' \
    --header 'Content-Type: application/json' \
    --data '{
       "ip": "python-service-chatllm.model-deployments.svc.cluster.local",
       "features": [1.0, 2.0, 3.0, 4.0, 5.0, 6.0, 7.0, 8.0, 9.0, 10.0]
    }'
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
