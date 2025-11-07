import { Component } from '@angular/core';
import { CommonModule } from '@angular/common'; // <-- Add this import
 
// Update the path below to the correct relative path where restock-task.service.ts exists
import { RestockTaskService } from '../../services/restock-task.service';
@Component({
  selector: 'app-assign-tasks',
  imports: [CommonModule], // <-- Add this line
 
  templateUrl: './assign-tasks.html',
  styleUrls: ['./assign-tasks.css']
})
export class AssignTasksComponent {
  loading = false;
  resultMessage = '';
 
  constructor(private restockTaskService: RestockTaskService) {}
 
  assignTasks() {
    this.loading = true;
    this.resultMessage = '';
    this.restockTaskService.assignTasksFromDeliveredStock().subscribe({
      next: (response: string) => {
        this.resultMessage = response;
        this.loading = false;
      },
      error: (err: any) => {
        this.resultMessage = 'Error assigning tasks.';
        this.loading = false;
      }
    });
  }
}
 
 