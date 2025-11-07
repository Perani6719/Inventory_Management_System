import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { StockRequestService, StockRequestApiModel, ApiResponse } from '../../services/stock-request.service';

export interface StockAlert {
  id: number;
  productName: string;
  currentStock?: number;
  minThreshold?: number;
  suggestedOrder?: number;
  priority: 'high' | 'medium' | 'low';
  lastUpdated?: Date | string;
  productId?: number;
}

@Component({
  selector: 'app-create-request-alert',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './create-request-alert.html',
  styleUrls: ['./create-request-alert.css']
})
export class CreateRequestAlertComponent implements OnInit {
  alerts: StockAlert[] = [];
  serverMessage: string = '';

  constructor(private stockRequestService: StockRequestService) {}

  ngOnInit() {
    // Show any alerts that are currently represented by pending stock requests with alertId
    this.loadAlertsFromPendingRequests();
  }

  private loadAlertsFromPendingRequests() {
    // First, load open alerts that need stock requests
    this.stockRequestService.getOpenAlerts().subscribe({
      next: (res: ApiResponse<StockAlert[]>) => {
        if (res.data) {
          this.alerts = res.data;
          // After getting open alerts, also check pending requests
          this.loadExistingPendingRequests();
        }
      },
      error: (err) => {
        console.error('Failed to load open alerts', err);
        // If open alerts fail, still try to load pending requests
        this.loadExistingPendingRequests();
      }
    });
  }

  private loadExistingPendingRequests() {
    this.stockRequestService.getPending().subscribe({
      next: (res: StockRequestApiModel[] | ApiResponse<StockRequestApiModel[]>) => {
        let pendingRequests: StockRequestApiModel[] = Array.isArray(res)
          ? res
          : ('data' in res ? res.data : []);
        
        // Filter out alerts that already have pending requests
        const existingAlertIds = new Set(pendingRequests.map(r => r.alertId));
        this.alerts = this.alerts.filter(alert => !existingAlertIds.has(alert.id));
      },
      error: (err) => {
        console.error('Failed to load pending requests', err);
      }
    });
  }

  successMessage: string = '';
  createdRequests: any[] = [];
  hasError: boolean = false;

  createRequestsFromSelected() {
    this.hasError = false;
    this.successMessage = '';
    this.createdRequests = [];
    
    this.stockRequestService.createFromAlerts().subscribe({
      next: (res: any) => {
        try {
          if (typeof res === 'string') {
            // Try to parse if it's a JSON string
            try {
              const parsedRes = JSON.parse(res);
              this.handleSuccessResponse(parsedRes);
            } catch {
              this.successMessage = res;
            }
          } else {
            this.handleSuccessResponse(res);
          }
        } catch (error) {
          console.error('Error processing response:', error);
          this.hasError = true;
          this.serverMessage = 'Error processing server response.';
        }
        // refresh alerts view
        this.loadAlertsFromPendingRequests();
      },
      error: (err) => {
        console.error('Failed to create requests from alerts', err);
        this.hasError = true;
        this.serverMessage = 'Failed to create requests from alerts.';
      }
    });
  }

private handleSuccessResponse(res: any) {
  if (res.data && Array.isArray(res.data)) {
    this.createdRequests = res.data;
    this.successMessage = res.message || `${res.data.length} stock requests successfully created.`;
  } else if (res.message) {
    this.successMessage = res.message;
  } else {
    this.successMessage = 'Stock requests created successfully.';
  }
}

getPriorityClass(priority: string): string {
  switch (priority) {
    case 'high':
      return 'priority-high';
    case 'medium':
      return 'priority-medium';
    case 'low':
      return 'priority-low';
    default:
      return '';
  }
}}
