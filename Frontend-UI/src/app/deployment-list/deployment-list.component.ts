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
  apiUrl: string = 'http://127.0.0.1:55166/api/ControlPlane/getDeployments';
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
      item.name,
      item.namespace,
      item.replicas,
      item.availableReplicas,
      item.readyReplicas,
      item.creationTimestamp,
      item.labels || {}, // Provide empty object if null
      item.annotations || {}, // Provide empty object if null
      item.selector || {}, // Provide empty object if null
      item.strategy,
      item.minReadySeconds ?? null, // Handle null
      item.revisionHistoryLimit ?? 10, // Default to 10 if not available
      item.conditions.map((c: any) => new Condition(c.type, c.status, c.lastTransitionTime)),
      item.podTemplate.map((p: any) => new PodTemplate(
        p.containerName,
        p.image,
        p.ports.map((port: any) => new ContainerPort(port.containerPort, port.protocol)),
        p.resources,
        p.env,
        p.imagePullPolicy
      )),
      item.volumes.map((v: any) => new Volume(v.name, v.volumeType, v.claimName)),
      item.ownerReferences ? item.ownerReferences.map((o: any) => new OwnerReference(o.apiVersion, o.kind, o.name, o.uid)) : null, // Handle ownerReferences if present
      item.service ? new Service(
        item.service.clusterIP || null,
        item.service.ports ? item.service.ports.map((port: any) => new ServicePort(
          port.port,
          new TargetPort(port.targetPort.value),
          port.protocol
        )) : [], // Ensure ports exist or provide an empty array
        item.service.error || null
      ) : null // Handle service and its fields
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
