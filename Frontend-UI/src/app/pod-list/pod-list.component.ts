import {Component, OnDestroy, OnInit} from '@angular/core';
import {MatGridListModule} from "@angular/material/grid-list";
import {NgForOf} from "@angular/common";
import {MatCardModule} from "@angular/material/card";
import {HttpClient, HttpClientModule} from "@angular/common/http";
import {Pod} from "../Pod";
import {debounceTime, interval, Subject} from "rxjs";
import {switchMap, takeUntil} from "rxjs/operators";
import {PodtItemComponent} from "../pod-item/pod-item.component";
import {port} from "../constants";

@Component({
  selector: 'app-pods',
  standalone: true,
  imports: [
    MatGridListModule,
    PodtItemComponent,
    NgForOf,
    MatCardModule,
    HttpClientModule // Import HttpClientModule here
  ],
  templateUrl: './pod-list.component.html',
  styleUrl: './pod-list.component.css'
})

export class PodsComponent implements OnInit, OnDestroy {
  deploymentsList: Pod[] = [];
  apiUrl: string = `http://127.0.0.1:${port}/api/ControlPlane/getallPods`;
  previousData: string = ''; // Stores the previous data as a string for comparison
  private unsubscribe$ = new Subject<void>(); // Subject to signal unsubscription

  constructor(private http: HttpClient) {}

  ngOnInit(): void {
    // Initial API call when component is loaded
    this.fetchPods();

    // Fetch the deployments every 2 seconds (2000 milliseconds)
    interval(2000)
      .pipe(
        debounceTime(500), // Adds a delay to prevent rapid consecutive API calls
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
  fetchPods(): void {
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
  handleApiResponse(data: any[]): void {
    const newDataString = JSON.stringify(data);
    if (newDataString !== this.previousData) {
      this.deploymentsList = [];
      this.deploymentsList = data.map(item => new Pod(
        item.clusterIP,
        item.ports[0]?.toString(),
        item.podName,
        item.status,
        item.containers.join(", "),
        `Pod is ${item.status} on node ${item.nodeName}`,
        item.logs
      ));
      this.previousData = newDataString;
    }
  }

  ngOnDestroy(): void {
    // Signal unsubscription to avoid multiple intervals
    this.unsubscribe$.next();
    this.unsubscribe$.complete();
  }
}
