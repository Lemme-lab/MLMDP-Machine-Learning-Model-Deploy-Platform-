import {ChangeDetectorRef, Component, Input, OnInit} from '@angular/core';
import {MatFormFieldModule} from "@angular/material/form-field";
import {MatInputModule} from "@angular/material/input";
import {MatIconModule} from "@angular/material/icon";
import {MatButtonModule} from "@angular/material/button";
import {MatChipsModule} from "@angular/material/chips";
import {FormsModule} from "@angular/forms";
import {CommonModule} from "@angular/common";
import {CardModule} from "primeng/card";
import {TerminalModule} from "primeng/terminal";
import {port} from "../constants";
import {HttpClient, HttpHeaders} from "@angular/common/http";

@Component({
  selector: 'app-pod-item',
  standalone: true,
  imports: [
    MatFormFieldModule,
    MatInputModule,
    MatIconModule,
    MatButtonModule,
    MatChipsModule, // Ensure this is imported
    FormsModule,
    CommonModule,
    CardModule, // PrimeNG CardModule
    TerminalModule
  ],
  templateUrl: './pod-item.component.html',
  styleUrls: ['./pod-item.component.css']
})
export class PodtItemComponent implements OnInit {
  @Input() Pod: any; // Deployment object includes logs
  logs: string = ''; // Initialize logs

  replicas: number = 1; // For controlling replicas

  constructor(private http: HttpClient) { }

  ngOnInit() {
    if (this.Pod.logs) {
      this.logs = this.Pod.logs; // Set logs
    }
  }

  togglePodState() {
    this.Pod.status = this.Pod.status === 'Running' ? 'Stopped' : 'Running';
  }

  deleteDeployment() {
    console.log('Delete Pod:', this.Pod.podName);

    const apiUrl = `http://127.0.0.1:${port}/api/ControlPlane/deletePod`;

    const options = {
      headers: new HttpHeaders({ 'Content-Type': 'application/json' }),
      body: {
        "PodName": this.Pod.podName
      },
    };

    this.http.request('DELETE', apiUrl, options).subscribe({
      next: (response) => {
        console.log('DELETE request successful:', response);
      },
      error: (error) => {
        console.error('Error in DELETE request:', error);
      }
    });

    var index = this.Pod.podName.indexOf("-deployment");
    var name = "";

// If "-deployment" is found, cut the string up to that point plus "-deployment" length
    if (index != -1) {
      name = this.Pod.podName.substring(0, index + "-deployment".length);
    }



    const apiUrl2 = `http://127.0.0.1:${port}/api/ControlPlane/scaleDownPod`;
    const postData = {
      PodName: name
    };

    this.http.post(apiUrl2, postData).subscribe({
      next: (response) => {
        console.log('POST request successful:', response);

      },
      error: (error) => {
        console.error('Error in POST request:', error);
      }
    });
  }

}
