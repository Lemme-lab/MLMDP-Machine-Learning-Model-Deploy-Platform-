import { ChangeDetectionStrategy, Component, signal } from '@angular/core';
import { MatToolbarModule } from '@angular/material/toolbar';
import { MatButtonToggleModule } from '@angular/material/button-toggle';
import { Router, ActivatedRoute } from '@angular/router';
import { CommonModule } from '@angular/common'; // Import CommonModule for ngClass

@Component({
  selector: 'app-nav-bar',
  standalone: true,
  imports: [MatToolbarModule, MatButtonToggleModule, CommonModule], // Add CommonModule here
  templateUrl: './nav-bar.component.html',
  styleUrls: ['./nav-bar.component.css']
})
export class NavBarComponent {
  hideSingleSelectionIndicator = signal(true);
  activeRoute: string = '';

  constructor(private router: Router, private route: ActivatedRoute) {}

  toggleSingleSelectionIndicator() {
    this.hideSingleSelectionIndicator.update(value => !value);
  }

  ngOnInit() {
    // Subscribe to route changes and set the active route
    this.router.events.subscribe(() => {
      this.activeRoute = this.router.url; // Get the current URL
    });
  }

  onSelectionChange(selectedValue: string) {
    if (selectedValue === 'deployments') {
      this.router.navigate(['/deployments']);
    } else if (selectedValue === 'pods') {
      this.router.navigate(['/pods']);
    }
  }

  // Method to check if the button is active
  isActive(tab: string): boolean {
    return this.activeRoute.includes(tab);
  }
}
