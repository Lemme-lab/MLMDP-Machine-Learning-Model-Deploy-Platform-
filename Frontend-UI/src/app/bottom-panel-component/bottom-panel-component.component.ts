import { Component } from '@angular/core';
import { HttpClientModule, HttpClient } from '@angular/common/http';
import { MatCardModule } from '@angular/material/card';
import { MatButtonModule } from '@angular/material/button';
import { MatIconModule } from '@angular/material/icon';
import { MatProgressBarModule } from '@angular/material/progress-bar';
import { CommonModule } from '@angular/common';
import { port } from "../constants";

@Component({
  selector: 'app-bottom-panel-component',
  standalone: true,
  imports: [MatCardModule, MatButtonModule, MatIconModule, MatProgressBarModule, CommonModule, HttpClientModule],
  templateUrl: './bottom-panel-component.component.html',
  styleUrls: ['./bottom-panel-component.component.css']
})
export class BottomPanelComponent {
  selectedFile: File | null = null;
  uploadInProgress = false;
  isDragOver = false;
  labelUpload = "Drag & Drop or Click to Choose File";

  constructor(private http: HttpClient) {}

  // Triggered when a file is selected through the file input
  onFileSelected(event: any): void {
    this.selectedFile = event.target.files[0];
  }

  // Triggered when a file is dragged over the drop zone
  onDragOver(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver = true;
  }

  // Triggered when a file is dropped onto the drop zone
  onDrop(event: DragEvent): void {
    event.preventDefault();
    this.isDragOver = false;

    if (event.dataTransfer?.files) {
      this.selectedFile = event.dataTransfer.files[0];
      this.labelUpload = event.dataTransfer.files[0].name;
      console.log(event.dataTransfer.files[0].name);
    }
  }

  // Triggered when the file is dragged away from the drop zone
  onDragLeave(event: DragEvent): void {
    this.isDragOver = false;
  }

  // Function to upload the selected file to the backend API
  uploadFile(): void {
    if (this.selectedFile) {
      this.uploadInProgress = true;

      const formData = new FormData();
      formData.append('file', this.selectedFile, this.selectedFile.name);

      // Make HTTP POST request to upload the file
      this.http.post(`http://127.0.0.1:${port}/api/ControlPlane/uploadModel`, formData).subscribe({
        next: (response) => {
          console.log('Upload successful', response);
          this.uploadInProgress = false;
          this.labelUpload = "Drag & Drop or Click to Choose File"; // Reset label after upload
        },
        error: (error) => {
          console.error('Upload failed', error);
          this.uploadInProgress = false; // Stop progress bar on error
        }
      });
    }
  }
}
