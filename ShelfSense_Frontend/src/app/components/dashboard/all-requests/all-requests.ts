import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StockRequestService, StockRequestApiModel } from '../../services/stock-request.service';

interface StockRequestView {
  requestId: number;
  storeId?: number;
  productId?: number;
  productName?: string;
  quantity: number;
  requestDate?: string | Date;
  requestedDeliveryDate?: string | Date;
  alertId?: number | null;
  deliveryStatus?: string;
  estimatedTimeOfArrival?: string | Date;
  requestedBy?: string;
}

@Component({
  selector: 'app-all-requests',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './all-requests.html',
  styleUrls: ['./all-requests.css']
})
export class AllRequestsComponent implements OnInit {
  requests: StockRequestView[] = [];
  filteredRequests: StockRequestView[] = [];
  paginatedRequests: StockRequestView[] = [];

  statusFilter: string = 'all';
  requestIdQuery: number | null = null;

  currentPage: number = 1;
  pageSize: number = 5;

  constructor(private stockRequestService: StockRequestService) {}

  ngOnInit() {
    this.loadAllRequests();
  }

  loadAllRequests() {
    this.stockRequestService.getAll().subscribe(
      (res: StockRequestApiModel[] | { data: StockRequestApiModel[] }) => {
        const resultArray = Array.isArray(res) ? res : (res && (res as any).data ? (res as any).data : []);
        this.requests = (resultArray || []).map((r: StockRequestApiModel) => ({
          requestId: r.requestId || 0,
          storeId: r.storeId,
          productId: r.productId,
          productName: r.productName || `Product ${r.productId}`,
          quantity: r.quantity || 0,
          requestDate: r.requestDate || '',
          requestedDeliveryDate: r.requestedDeliveryDate || '',
          alertId: r.alertId ?? null,
          deliveryStatus: this.getDeliveryStatusLabel(r.deliveryStatus),
          estimatedTimeOfArrival: r.estimatedTimeOfArrival || '',
          requestedBy: r.requestedBy || ''
        }));
        this.applyFilters();
      },
      (err) => {
        console.error('Failed to load stock requests from backend', err);
        this.requests = [];
        this.applyFilters();
      }
    );
  }

  applyFilters() {
    this.filteredRequests = this.requests.filter(request => {
      const matchesStatus = this.statusFilter === 'all' || request.deliveryStatus === this.statusFilter;
      const matchesRequestId = !this.requestIdQuery || request.requestId === this.requestIdQuery;
      return matchesStatus && matchesRequestId;
    });

    this.currentPage = 1;
    this.updatePagination();
  }

  updatePagination(): void {
    const startIndex = (this.currentPage - 1) * this.pageSize;
    const endIndex = startIndex + this.pageSize;
    this.paginatedRequests = this.filteredRequests.slice(startIndex, endIndex);
  }

  goToPage(page: number): void {
    this.currentPage = page;
    this.updatePagination();
  }

  totalPages(): number {
    return Math.ceil(this.filteredRequests.length / this.pageSize);
  }

  public getDeliveryStatusLabel(raw?: string | number): string {
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
}
