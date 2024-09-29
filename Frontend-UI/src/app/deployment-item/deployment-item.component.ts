import {ChangeDetectorRef, Component, Input, OnInit} from '@angular/core';
import { HttpClient } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatButtonToggleModule } from '@angular/material/button-toggle'; // Use Material Button Toggle
import { MatButtonModule } from '@angular/material/button';
import { MatChipsModule } from '@angular/material/chips';
import { MatGridListModule } from '@angular/material/grid-list';
import { CardModule } from 'primeng/card';
import { TerminalModule } from 'primeng/terminal';
import { InputNumberModule } from 'primeng/inputnumber';
import { port } from "../constants";

interface Pod {
  podName: string;
  nodeName: string;
  containers: Array<{ name: string; image: string }>;
  status: string;
  startTime: string;
  labels: Array<{ key: string; value: string }>;
}

@Component({
  selector: 'app-deployment-item',
  standalone: true,
  imports: [
    MatFormFieldModule,
    MatInputModule,
    MatButtonToggleModule,  // Added MatButtonToggleModule
    MatButtonModule,
    MatChipsModule,
    MatGridListModule,
    FormsModule,
    CommonModule,
    CardModule,
    TerminalModule,
    InputNumberModule
  ],
  templateUrl: './deployment-item.component.html',
  styleUrls: ['./deployment-item.component.css']
})
export class DeploymentItemComponent implements OnInit {
  @Input() deployment: any; // Deployment object
  podsList: any[] = []; // Array to store pod data
  logs: string = ''; // Initialize logs

  // New properties for ML model input, output, and settings
  mlModelData: string = '';
  modelOutput: string = '';
  modelSettings: number = 0; // Default value for incrementer
  isModelRunning: boolean = false;
  conditionsStatus1: string = "";
  conditionsStatus2: string = "";

  constructor(private http: HttpClient, private cdr: ChangeDetectorRef) { }

  ngOnInit() {
    if (this.deployment.logs) {
      this.logs = this.deployment.logs;
    }

    // Set default modelSettings from deployment, if available
    if (this.deployment.availableReplicas) {
      this.modelSettings = this.deployment.availableReplicas; // Assuming deployment has a modelSettings field
    }

    if (this.deployment.availableReplicas == 0) {
      this.isModelRunning = false;
      this.conditionsStatus1 = 'False';
      this.conditionsStatus2 = 'False';
    } else {
      this.isModelRunning = true;
      this.conditionsStatus1 = 'True';
      this.conditionsStatus2 = 'True';
    }

    this.getPodsForDeployment(this.deployment.name);
  }

  // Fetch pods using the updated API response structure
  getPodsForDeployment(deploymentName: string) {
    const apiUrl = `http://127.0.0.1:${port}/api/ControlPlane/getDeploymentPods/${deploymentName}`;
    this.http.get<Pod[]>(apiUrl).subscribe({
      next: (data: Pod[]) => {
        try {
          this.podsList = data.map(pod => ({
            podName: pod.podName,
            nodeName: pod.nodeName,
            containerName: pod.containers[0]?.name,
            containerImage: pod.containers[0]?.image,
            status: pod.status,
            startTime: pod.startTime,
            labels: pod.labels.map((label: any) => `${label.key}: ${label.value}`).join(', ')
          }));
        }catch (e) {

        }
      },
      error: (error) => {
        console.error('Error fetching pods:', error);
      }
    });
  }

  private adjustReplicas() {
    console.log(this.deployment.name);
    console.log(this.modelSettings);

    const apiUrl = `http://127.0.0.1:${port}/api/ControlPlane/scalePod`;
    const postData = {
      PodName: this.deployment.name,
      Replicas: this.modelSettings
    };

    this.http.post(apiUrl, postData).subscribe({
      next: (response) => {
        console.log('POST request successful:', response);

      },
      error: (error) => {
        console.error('Error in POST request:', error);
      }
    });
  }

  toggleModel() {
    if (this.isModelRunning) {
      this.stopModel();
    } else {
      this.startModel();
    }
    this.isModelRunning = !this.isModelRunning; // Toggle the state
  }

  // Increment and Decrement methods for model settings
  increment() {
    if (this.modelSettings < 100) {
      this.modelSettings++;
    }
    this.adjustReplicas();
  }

  decrement() {
    if (this.modelSettings > 1) {
      this.modelSettings--;
    }
    this.adjustReplicas();
  }

  sendData() {
    console.log('Sending ML model data:', this.mlModelData);

    const apiUrl = `http://127.0.0.1:${port}/api/ControlPlane/callDeploymentAPI`;
    const array: number[] = JSON.parse(this.mlModelData);

    const postData = {
      ip: `python-service-${this.deployment.name.replace('-deployment', '')}.model-deployments.svc.cluster.local`,
      features: array,
    };

    this.http.post(apiUrl, postData).subscribe({
      next: (response: any) => {
        console.log('POST request successful:', response);
        this.modelOutput = `Prediction: "${response.prediction}"`;
      },
      error: (error) => {
        console.error('Error in POST request:', error);
      }
    });
  }

  stopModel() {
    console.log('Model stopped');
    this.conditionsStatus1 = 'False';
    this.conditionsStatus2 = 'False';

    const apiUrl = `http://127.0.0.1:${port}/api/ControlPlane/stopDeployment`;
    const postData = {
      PodName: this.deployment.name,
    };

    this.http.post(apiUrl, postData).subscribe({
      next: (response) => {
        console.log('POST request successful:', response);

        // Clear the podsList when the pod is stopped
        this.podsList = [];

        setTimeout(() => {
          console.log("Reloading the page");
          this.podsList = [];
          this.reloadComponent();

        }, 4000);  // Adjust the delay time as needed
      },
      error: (error) => {
        console.error('Error in POST request:', error);
      }
    });
  }


// Method to reload data and trigger change detection
  reloadComponent() {
    // Fetch updated podsList or any other data
    this.getPodsForDeployment(this.deployment.name);

    // Trigger change detection manually if needed
    this.cdr.detectChanges();
  }


  startModel() {
    console.log('Model started');
    this.conditionsStatus1 = 'True';
    this.conditionsStatus2 = 'True';

    const apiUrl = `http://127.0.0.1:${port}/api/ControlPlane/startDeployment`;
    const postData = {
      PodName: this.deployment.name,
    };

    this.http.post(apiUrl, postData).subscribe({
      next: (response) => {
        console.log('POST request successful:', response);
      },
      error: (error) => {
        console.error('Error in POST request:', error);
      }
    });
  }

  deleteDeployment() {
    console.log('Delete deployment:', this.deployment.name);

    const apiUrl = `http://127.0.0.1:${port}/api/ControlPlane/delete`;

    const requestBody = { PodName: this.deployment.name }; // Pass the pod name in the body

    this.http.delete(apiUrl, { body: requestBody }).subscribe({
      next: (response) => {
        console.log('DELETE request successful:', response);
      },
      error: (error) => {
        console.error('Error in DELETE request:', error);
      }
    });
  }
}
