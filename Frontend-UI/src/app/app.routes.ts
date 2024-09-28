import { Routes } from '@angular/router';
import { DeploymentListComponent } from './deployment-list/deployment-list.component';
import {PodsComponent} from "./pod-list/pod-list.component";

export const routes: Routes = [
  { path: 'deployments', component: DeploymentListComponent },
  { path: 'pods', component: PodsComponent },
  { path: '', redirectTo: '/deployments', pathMatch: 'full' } // default route
];
