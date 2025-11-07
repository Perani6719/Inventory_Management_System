import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { DeliveryLogService, DeliveryStatusLog } from '../../services/delivery-log-status';

@Component({
  selector: 'app-delivery-log',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './delivery-log-status.html',
  styleUrls: ['./delivery-log-status.css']
})
export class DeliveryLogComponent implements OnInit {
  logs: DeliveryStatusLog[] = [];
  filteredLogs: DeliveryStatusLog[] = [];
  paginatedLogs: DeliveryStatusLog[] = [];

  requestIdFilter: string = '';
  loading = false;
  error = '';

  currentPage: number = 1;
  pageSize: number = 7;

  constructor(private deliveryLogService: DeliveryLogService) {}

  ngOnInit(): void {
    this.loading = true;
    this.deliveryLogService.getAllLogs().subscribe({
      next: (res) => {
        this.logs = res.data;
        this.filteredLogs = res.data;
        this.updatePagination();
        this.loading = false;
      },
      error: () => {
        this.error = 'Failed to load delivery logs.';
        this.loading = false;
      }
    });
  }

  applyFilter(): void {
    const filter = this.requestIdFilter.trim();
    this.filteredLogs = filter
      ? this.logs.filter(log => log.requestId.toString().includes(filter))
      : this.logs;

    this.currentPage = 1;
    this.updatePagination();
  }

  updatePagination(): void {
    const startIndex = (this.currentPage - 1) * this.pageSize;
    const endIndex = startIndex + this.pageSize;
    this.paginatedLogs = this.filteredLogs.slice(startIndex, endIndex);
  }

  goToPage(page: number): void {
    if (page < 1 || page > this.totalPages()) return;
    this.currentPage = page;
    this.updatePagination();
  }

  totalPages(): number {
    return Math.ceil(this.filteredLogs.length / this.pageSize);
  }
}
