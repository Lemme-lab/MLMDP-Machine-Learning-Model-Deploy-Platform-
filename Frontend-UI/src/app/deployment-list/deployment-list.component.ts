import { Component, OnInit, OnDestroy } from '@angular/core';
import { HttpClient, HttpClientModule } from '@angular/common/http';
import { interval, Subject } from 'rxjs';
import { debounceTime, switchMap, takeUntil } from 'rxjs/operators';
import { MatGridListModule } from '@angular/material/grid-list';
import { DeploymentItemComponent } from '../deployment-item/deployment-item.component';
import { NgForOf } from '@angular/common';
import { MatCardModule } from '@angular/material/card';
import {
  Condition,
  ContainerPort,
  Deployment,
  OwnerReference,
  PodTemplate,
  Service,
  ServicePort, TargetPort,
  Volume
} from "../Deployment";
import { isEqual } from 'lodash';
import {port} from "../constants";

@Component({
  selector: 'app-deployment-list',
  standalone: true,
  imports: [
    MatGridListModule,
    DeploymentItemComponent,
    NgForOf,
    MatCardModule,
    HttpClientModule // Import HttpClientModule here
  ],
  templateUrl: './deployment-list.component.html',
  styleUrls: ['./deployment-list.component.css']
})
export class DeploymentListComponent implements OnInit, OnDestroy {
  deploymentsList: Deployment[] = [];
  apiUrl: string = `http://127.0.0.1:${port}/api/ControlPlane/getDeployments`;
  private unsubscribe$ = new Subject<void>();

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    // Initial API call when component is loaded
    this.fetchDeployments();

    // Fetch the deployments every 2 seconds (2000 milliseconds)
    interval(2000)
      .pipe(
        switchMap(() => this.http.get<any[]>(this.apiUrl)),
        takeUntil(this.unsubscribe$)
      )
      .subscribe({
        next: (data: any[]) => {
          this.handleApiResponse(data);
        },
        error: (error) => {
          console.error('There was an error fetching the deployments', error);
        }
      });
  }

  // Helper method for API calls
  fetchDeployments(): void {
    this.http.get<any[]>(this.apiUrl).subscribe({
      next: (data: any[]) => {
        this.handleApiResponse(data);
      },
      error: (error) => {
        console.error('There was an error fetching the deployments', error);
      }
    });
  }

  // Helper method to handle API response
  // Helper method to handle API response
  handleApiResponse(data: any[]): void {
    const newDeploymentsList = data.map(item => new Deployment(
      item.name || 'Unknown', // Default to 'Unknown' if name is missing
      item.namespace || 'default', // Default to 'default' if namespace is missing
      item.replicas ?? 0, // Default to 0 if replicas are missing
      item.availableReplicas ?? 0, // Default to 0 if availableReplicas are missing
      item.readyReplicas ?? 0, // Default to 0 if readyReplicas are missing
      item.creationTimestamp || 'N/A', // Default to 'N/A' if timestamp is missing
      item.labels || {}, // Provide empty object if labels are missing
      item.annotations || {}, // Provide empty object if annotations are missing
      item.selector || {}, // Provide empty object if selector is missing
      item.strategy || 'RollingUpdate', // Default strategy if not provided
      item.minReadySeconds ?? 0, // Default to 0 if minReadySeconds are missing
      item.revisionHistoryLimit ?? 10, // Default to 10 if revisionHistoryLimit is missing
      Array.isArray(item.conditions) && item.conditions.length > 0
        ? item.conditions.map((c: any) => new Condition(c.type || 'Unknown', c.status || 'Unknown', c.lastTransitionTime || 'N/A'))
        : [], // Handle empty or undefined conditions
      Array.isArray(item.podTemplate) && item.podTemplate.length > 0
        ? item.podTemplate.map((p: any) => new PodTemplate(
          p.containerName || 'Unknown',
          p.image || 'Unknown',
          Array.isArray(p.ports) && p.ports.length > 0
            ? p.ports.map((port: any) => new ContainerPort(port.containerPort ?? 0, port.protocol || 'TCP'))
            : [], // Handle empty or undefined ports
          p.resources || {}, // Default to empty object if resources are missing
          p.env || [], // Default to empty array if environment variables are missing
          p.imagePullPolicy || 'IfNotPresent' // Default to 'IfNotPresent' if imagePullPolicy is missing
        ))
        : [], // Handle empty or undefined podTemplate
      Array.isArray(item.volumes) && item.volumes.length > 0
        ? item.volumes.map((v: any) => new Volume(v.name || 'Unknown', v.volumeType || 'EmptyDir', v.claimName || 'N/A'))
        : [], // Handle empty or undefined volumes
      Array.isArray(item.ownerReferences) && item.ownerReferences.length > 0
        ? item.ownerReferences.map((o: any) => new OwnerReference(
          o.apiVersion || 'Unknown',
          o.kind || 'Unknown',
          o.name || 'Unknown',
          o.uid || 'N/A'
        ))
        : null, // Handle empty or undefined ownerReferences
      item.service
        ? new Service(
          item.service.clusterIP || 'None',
          Array.isArray(item.service.ports) && item.service.ports.length > 0
            ? item.service.ports.map((port: any) => new ServicePort(
              port.port ?? 0,
              new TargetPort(port.targetPort?.value || 0),
              port.protocol || 'TCP'
            ))
            : [], // Handle empty or undefined service ports
          item.service.error || null // Default to null if service error is missing
        )
        : null // Handle null service
    ));

    // Only update the list if the new data is different from the current list
    if (!isEqual(this.deploymentsList, newDeploymentsList)) {
      this.deploymentsList = newDeploymentsList;
    }
  }

  ngOnDestroy(): void {
    // Signal unsubscription to avoid multiple intervals
    this.unsubscribe$.next();
    this.unsubscribe$.complete();
  }
}
