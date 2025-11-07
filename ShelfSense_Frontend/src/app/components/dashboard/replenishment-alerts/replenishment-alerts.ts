import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HttpClientModule } from '@angular/common/http';
import { ReplenishmentAlertService, ReplenishmentAlert } from '../../services/replenishment-alert.service';
import { ToastrService } from 'ngx-toastr';
import { FormsModule } from '@angular/forms'; // ✅ Required for ngModel
 
@Component({
  selector: 'app-replenishment-alerts-dashboard',
  standalone: true,
  imports: [CommonModule, RouterModule, HttpClientModule, FormsModule],
  templateUrl: './replenishment-alerts.html',
  styleUrls: ['./replenishment-alerts.css']
})
export class ReplenishmentAlertsComponent implements OnInit {
  alerts: ReplenishmentAlert[] = [];
  filteredAlerts: ReplenishmentAlert[] = [];
  filterProductId: string = '';
  isLoading = false;
 
  total = 0;
  byUrgency: Record<string, number> = {};
 
  // ✅ Urgency filter setup
  urgencyLevels: string[] = ['low', 'medium', 'high', 'critical'];
  selectedUrgencies: string[] = [];
 
  constructor(private svc: ReplenishmentAlertService, private toastr: ToastrService) {}
 
  ngOnInit(): void {
    this.load();
  }
 
  load(): void {
    this.isLoading = true;
    this.alerts = [];
    this.filteredAlerts = [];
 
    this.svc.getAlerts().subscribe({
      next: (res: { data: ReplenishmentAlert[] }) => {
        this.alerts = res.data || [];
        this.filteredAlerts = [...this.alerts];
        this.total = this.alerts.length;
 
        this.byUrgency = this.alerts.reduce((acc: Record<string, number>, a: ReplenishmentAlert) => {
          const key = a.urgencyLevel.toLowerCase();
          acc[key] = (acc[key] || 0) + 1;
          return acc;
        }, {});
 
        this.toastr.info(`Loaded ${this.total} alerts.`, 'Info');
        this.isLoading = false;
      },
      error: () => {
        this.toastr.error('Failed to load alerts', 'Error');
        this.isLoading = false;
      }
    });
  }
 
  applyFilter(): void {
    const term = this.filterProductId.trim().toLowerCase();
 
    this.filteredAlerts = this.alerts.filter(a =>
      (!term || String(a.productId).toLowerCase().includes(term)) &&
      (this.selectedUrgencies.length === 0 || this.selectedUrgencies.includes(a.urgencyLevel.toLowerCase()))
    );
  }
 
  toggleUrgencyFilter(level: string, isChecked: boolean): void {
    const normalized = level.toLowerCase();
 
    if (isChecked && !this.selectedUrgencies.includes(normalized)) {
      this.selectedUrgencies.push(normalized);
    } else if (!isChecked) {
      this.selectedUrgencies = this.selectedUrgencies.filter(l => l !== normalized);
    }
 
    this.applyFilter();
  }
 
  formatDate(dt: any): string {
    if (!dt) return '';
    const d = new Date(dt);
    return isNaN(d.getTime()) ? String(dt) : d.toLocaleString();
  }
 
  getUrgencyKeys(): string[] {
    return Object.keys(this.byUrgency || {});
  }
 
  resetFilters(): void {
    this.filterProductId = '';
    this.selectedUrgencies = [];
    this.filteredAlerts = [...this.alerts];
  }
}
 
 