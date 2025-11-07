import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { AllRestockTasksService, RestockTask } from '../../services/all-restock-tasks.service';

@Component({
  selector: 'app-all-restock-tasks',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './all-restock-tasks.html',
  styleUrls: ['./all-restock-tasks.css']
})
export class AllRestockTasksComponent implements OnInit {
  restockTasks: RestockTask[] = [];
  loading = false;
  error = '';

  // Filters
  statusFilter: string = '';
  taskIdFilter: number | null = null;

  // Pagination for Task ID filter
  currentPageTaskId: number = 1;
  itemsPerPageTaskId: number = 3;

  // Pagination for Status filter
  currentPageStatus: number = 1;
  itemsPerPageStatus: number = 3;

  constructor(private allRestockTasksService: AllRestockTasksService) {}

  ngOnInit() {
    this.loading = true;
    this.allRestockTasksService.getAllRestockTasks().subscribe({
      next: (tasks) => {
        this.restockTasks = tasks;
        this.loading = false;
      },
      error: () => {
        this.error = 'Failed to load restock tasks.';
        this.loading = false;
      }
    });
  }

  getStatusClass(status: string): string {
    switch (status.toLowerCase()) {
      case 'pending': return 'status-pending';
      case 'completed': return 'status-completed';
      case 'delayed': return 'status-delayed';
      default: return '';
    }
  }

  // Filters
  get filteredTasksById(): RestockTask[] {
    if (this.taskIdFilter) {
      return this.restockTasks.filter(task => task.taskId === this.taskIdFilter);
    }
    return this.restockTasks;
  }

  get filteredTasksByStatus(): RestockTask[] {
    if (this.statusFilter) {
      return this.restockTasks.filter(task => task.status.toLowerCase() === this.statusFilter.toLowerCase());
    }
    return this.restockTasks;
  }

  // Pagination for Task ID
  get paginatedTasksTaskId(): RestockTask[] {
    const startIndex = (this.currentPageTaskId - 1) * this.itemsPerPageTaskId;
    return this.filteredTasksById.slice(startIndex, startIndex + this.itemsPerPageTaskId);
  }

  get totalPagesTaskId(): number {
    return Math.ceil(this.filteredTasksById.length / this.itemsPerPageTaskId);
  }

  changePageTaskId(page: number) {
    if (page >= 1 && page <= this.totalPagesTaskId) {
      this.currentPageTaskId = page;
    }
  }

  // Pagination for Status
  get paginatedTasksStatus(): RestockTask[] {
    const startIndex = (this.currentPageStatus - 1) * this.itemsPerPageStatus;
    return this.filteredTasksByStatus.slice(startIndex, startIndex + this.itemsPerPageStatus);
  }

  get totalPagesStatus(): number {
    return Math.ceil(this.filteredTasksByStatus.length / this.itemsPerPageStatus);
  }

  changePageStatus(page: number) {
    if (page >= 1 && page <= this.totalPagesStatus) {
      this.currentPageStatus = page;
    }
  }

  // Reset page when filters change
  onTaskIdFilterChange() {
    this.currentPageTaskId = 1;
  }

  onStatusFilterChange() {
    this.currentPageStatus = 1;
  }
}
