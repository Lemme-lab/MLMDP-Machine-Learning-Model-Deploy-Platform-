import { Component, Input, OnInit } from '@angular/core';
import {MatFormFieldModule} from "@angular/material/form-field";
import {MatInputModule} from "@angular/material/input";
import {MatIconModule} from "@angular/material/icon";
import {MatButtonModule} from "@angular/material/button";
import {MatChipsModule} from "@angular/material/chips";
import {FormsModule} from "@angular/forms";
import {CommonModule} from "@angular/common";
import {CardModule} from "primeng/card";
import {TerminalModule} from "primeng/terminal";

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
  @Input() deployment: any; // Deployment object includes logs
  logs: string = ''; // Initialize logs

  replicas: number = 1; // For controlling replicas

  ngOnInit() {
    if (this.deployment.logs) {
      this.logs = this.deployment.logs; // Set logs
    }
  }

  togglePodState() {
    this.deployment.status = this.deployment.status === 'Running' ? 'Stopped' : 'Running';
  }

  deleteDeployment() {
    console.log('Delete deployment:', this.deployment.podName);
  }

  incrementReplicas() {
    this.replicas++;
  }

  decrementReplicas() {
    if (this.replicas > 1) {
      this.replicas--;
    }
  }
}
