import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { ShelfMetricsService, ShelfMetric } from '../../services/shelf-metrics.service';
import { ToastrService } from 'ngx-toastr';

@Component({
  selector: 'app-shelf-metrics',
  standalone: true,
  imports: [CommonModule, HttpClientModule],
  templateUrl: './shelf-metrics.html',
  styleUrls: ['./shelf-metrics.css']
})
export class ShelfMetricsComponent implements OnInit {
  metrics: ShelfMetric[] = [];
  isLoading = false;

  showDeleteModal = false;
  pendingDeleteId: number | null = null;
  pendingDeleteCode = '';

  constructor(
    private metricsService: ShelfMetricsService,
    private toastr: ToastrService
  ) {}

  ngOnInit(): void {
    // Manual reload only
    // this.loadMetrics();
  }

  loadMetrics(): void {
    this.isLoading = true;
    this.metrics = [];

    this.metricsService.getShelfMetrics().subscribe({
      next: res => {
        this.metrics = res.data;
        this.toastr.info(`Loaded ${this.metrics.length} shelf metrics.`, 'Info');
        this.isLoading = false;
      },
      error: () => {
        this.toastr.error('Failed to load shelf metrics.', 'Error');
        this.isLoading = false;
      }
    });
  }

  viewMetric(metric: ShelfMetric): void {
    this.toastr.info(`Viewing shelf ${metric.shelfCode}`, 'View');
    console.log('Viewing metric:', metric);
  }

  openDeleteModal(id: number, code: string): void {
    this.pendingDeleteId = id;
    this.pendingDeleteCode = code;
    this.showDeleteModal = true;
  }

  cancelDelete(): void {
    this.pendingDeleteId = null;
    this.pendingDeleteCode = '';
    this.showDeleteModal = false;
  }

  confirmDelete(): void {
    if (!this.pendingDeleteId) return;

    // Optional: implement actual delete logic if supported
    this.toastr.success(`Deleted shelf ${this.pendingDeleteCode}`, 'Deleted');
    this.metrics = this.metrics.filter(m => m.shelfId !== this.pendingDeleteId);
    this.cancelDelete();
  }
}
