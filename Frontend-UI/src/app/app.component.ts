import { Component } from '@angular/core';
import { NavBarComponent } from './nav-bar/nav-bar.component';
import { DeploymentListComponent } from './deployment-list/deployment-list.component';
import { BottomPanelComponent } from './bottom-panel-component/bottom-panel-component.component';
import { TerminalService } from 'primeng/terminal';
import {RouterOutlet} from "@angular/router"; // Import TerminalService

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [RouterOutlet, NavBarComponent, DeploymentListComponent, BottomPanelComponent],
  providers: [TerminalService], // Provide the TerminalService here
  templateUrl: './app.component.html',
  styleUrls: ['./app.component.css']
})
export class AppComponent {
  title = 'Frontend-UI';
}
