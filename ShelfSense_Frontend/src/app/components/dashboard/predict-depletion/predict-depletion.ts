import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { HttpClientModule } from '@angular/common/http';
import { ToastrService } from 'ngx-toastr';
import { FormsModule } from '@angular/forms';
import { DepletionService } from '../../services/predict-depletion';
 
 
@Component({
  selector: 'app-predict-depletion',
  standalone: true,
  imports: [CommonModule, RouterModule, HttpClientModule, FormsModule],
  templateUrl: './predict-depletion.html',
  styleUrls: ['./predict-depletion.css']
})
export class PredictDepletionComponent implements OnInit {
  rows: any[] = [];
  headers: string[] = [];
 
 
  filterProductId: string = '';
  filteredRows: any[] = [];
 
  // preferred order and friendly labels for columns returned by backend
  private preferredOrder = [
    'productId',
    'shelfId',
    'quantity',
    'salesVelocity',
    'daysToDepletion',
    'expectedDepletionDate',
    'isLowStock'
  ];
 
  headerLabels: Record<string, string> = {
    productId: 'Product ID',
    shelfId: 'Shelf ID',
    quantity: 'Quantity',
    salesVelocity: 'Sales Velocity',
    daysToDepletion: 'Days To Deplete',
    expectedDepletionDate: 'Expected Depletion Date',
    isLowStock: 'Low Stock'
  };
 
  isLoading = false;
 
  error = '';
 
  constructor(private predict:DepletionService , private toastr: ToastrService) {}
 
  ngOnInit(): void {
    // auto-load when route activates
    this.load();
  }
 
  formatDate(value: any): string {
    if (!value) return '';
    const d = new Date(value);
    if (isNaN(d.getTime())) return String(value);
    return d.toLocaleString();
  }
 
  lowStockLabel(value: any): string {
    return value ? 'Yes' : 'No';
  }
 
  load(): void {
  this.isLoading = true;
  this.rows = [];
  this.predict.getPredictions().subscribe({
    next: (res: any) => {
      this.rows = Array.isArray(res?.data) ? res.data : (res && res.data ? [res.data] : []);
      this.filteredRows = this.rows; // ✅ Initially show all rows
      if (this.rows.length) {
        const keys = Object.keys(this.rows[0]);
        this.headers = this.preferredOrder.filter(k => keys.includes(k)).concat(keys.filter(k => !this.preferredOrder.includes(k)));
      } else {
        this.headers = [];
      }
      this.toastr.info(`Loaded ${this.rows.length} records.`, 'Info');
      this.isLoading = false;
    },
    error: (err: any) => {
      this.error = err?.message || 'Failed to load predictions';
      this.toastr.error(this.error, 'Error');
      this.isLoading = false;
    }
  });
}
applyFilter(): void {
  const term = this.filterProductId.trim().toLowerCase();
  if (term) {
    this.filteredRows = this.rows.filter(r =>
      String(r.productId).toLowerCase().includes(term)
    );
  } else {
    this.filteredRows = this.rows;
  }
}
}
 
 