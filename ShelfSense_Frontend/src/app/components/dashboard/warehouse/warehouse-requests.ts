import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { WarehouseService } from '../../services/warehouse.service';
import { StockRequestApiModel } from '../../services/stock-request.service';

@Component({
  selector: 'app-warehouse-requests',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './warehouse-requests.html',
  styleUrls: ['./warehouse-requests.css']
})
export class WarehouseRequestsComponent implements OnInit {
  requests: StockRequestApiModel[] = [];
  filteredRequests: StockRequestApiModel[] = [];
  statusFilter: string = 'all';
  loading = false;
  error = '';

  constructor(private warehouseService: WarehouseService) {}

  ngOnInit(): void {
    this.loadRequests();
  }

  loadRequests() {
    this.loading = true;
    this.error = '';
    this.warehouseService.getIncomingRequests().subscribe({
      next: (res) => {
        const rawRequests = res?.data || [];
        this.requests = rawRequests.map(r => ({
          ...r,
          deliveryStatus: this.getDeliveryStatusLabel(r.deliveryStatus)
        }));
        this.applyFilters();
        this.loading = false;
      },
      error: (err) => {
        console.error('Failed to load incoming requests', err);
        this.error = 'Failed to load incoming requests.';
        this.loading = false;
      }
    });
  }

  applyFilters() {
    this.filteredRequests = this.statusFilter === 'all'
      ? this.requests
      : this.requests.filter(r => r.deliveryStatus === this.statusFilter);
  }

  getDeliveryStatusLabel(raw?: string | number): string {
    if (raw === null || raw === undefined) return 'requested';
    const s = String(raw).trim().toLowerCase();

    if (s === '0') return 'requested';
    if (s === '1') return 'delivered';
    if (s === '2') return 'in-transit';
    if (s === '3') return 'cancelled';

    if (s === 'in_transit' || s === 'in-transit' || s === 'in transit' || s === 'intransit' || s === 'shipping') return 'in-transit';
    if (s === 'delivered' || s === 'complete' || s === 'completed') return 'delivered';
    if (s === 'cancelled' || s === 'canceled' || s === 'cancel') return 'cancelled';
    if (s === 'requested' || s === 'pending' || s === 'new' || s === 'request') return 'requested';

    if (s.includes('deliver')) return 'delivered';
    if (s.includes('cancel')) return 'cancelled';
    if (s.includes('transit') || s.includes('ship')) return 'in-transit';

    return 'requested';
  }

  openDispatch(request: StockRequestApiModel) {
    const eta = new Date().toISOString();
    this.warehouseService.dispatch(request.requestId as number, eta).subscribe({
      next: () => this.loadRequests(),
      error: (err) => console.error(err)
    });
  }

  markDelivered(request: StockRequestApiModel) {
    this.warehouseService.markDelivered(request.requestId as number).subscribe({
      next: () => this.loadRequests(),
      error: (err) => console.error(err)
    });
  }

  cancel(request: StockRequestApiModel) {
    this.warehouseService.cancel(request.requestId as number, 'Cancelled by warehouse').subscribe({
      next: () => this.loadRequests(),
      error: (err) => console.error(err)
    });
  }
}
