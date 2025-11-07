import { Component, OnInit } from '@angular/core';
import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { FormsModule } from '@angular/forms';
import { NgxEchartsModule } from 'ngx-echarts';
import { ToastrService } from 'ngx-toastr';
import { InventoryReportService, StockoutReportItem } from '../../services/inventory-report.service';

type SortableColumn = keyof Pick<
  StockoutReportItem,
  'productName' | 'shelfLocation' | 'stockoutCount' | 'avgReplenishmentTimeInHours' | 'avgReplenishmentDelayInHours' | 'shelfAvailabilityPercentage'
>;

@Component({
  selector: 'app-inventory-report',
  standalone: true,
  imports: [CommonModule, HttpClientModule, FormsModule, NgxEchartsModule],
  templateUrl: './inventory-report.html',
  styleUrls: ['./inventory-report.css']
})
export class InventoryReportComponent implements OnInit {
  startDate!: string;
  endDate!: string;
  isLoading = false;
  reportItems: StockoutReportItem[] = [];
  filterText = '';
  sortColumn: SortableColumn = 'stockoutCount';
  sortDirection: 'asc' | 'desc' = 'desc';

  chartOptions: any = {};
  showChart = false;

  constructor(private reportService: InventoryReportService, private toastr: ToastrService) {}

  ngOnInit(): void {}

  generateReport(): void {
    if (!this.startDate || !this.endDate || this.startDate > this.endDate) {
      this.toastr.warning('Please select a valid date range.', 'Warning');
      return;
    }

    const today = new Date().toISOString().split('T')[0];
    if (this.endDate > today) {
      this.toastr.warning('End date cannot be in the future.', 'Validation Error');
      return;
    }

    this.isLoading = true;
    this.reportService.getInventoryReport(this.startDate, this.endDate).subscribe({
      next: (res) => {
        this.reportItems = res || [];
        this.toastr.success(`Loaded ${this.reportItems.length} report items.`, 'Success');
        this.prepareChart();
      },
      error: () => this.toastr.error('Failed to load report.', 'Error'),
      complete: () => (this.isLoading = false)
    });
  }

  prepareChart(): void {
    const topItems = [...this.reportItems]
      .sort((a, b) => b.stockoutCount - a.stockoutCount)
      .slice(0, 10);

    this.chartOptions = {
      title: {
        text: 'Top 10 Stockouts',
        left: 'center'
      },
      tooltip: {},
      xAxis: {
        type: 'category',
        data: topItems.map(item => item.productName)
      },
      yAxis: {
        type: 'value'
      },
      series: [
        {
          name: 'Stockouts',
          type: 'bar',
          data: topItems.map(item => item.stockoutCount),
          itemStyle: {
            color: '#388e3c'
          }
        }
      ]
    };

    this.showChart = true;
  }

  downloadPdf(): void {
    if (!this.startDate || !this.endDate || this.startDate > this.endDate) {
      this.toastr.warning('Please select a valid date range.', 'Warning');
      return;
    }

    const today = new Date().toISOString().split('T')[0];
    if (this.endDate > today) {
      this.toastr.warning('End date cannot be in the future.', 'Validation Error');
      return;
    }

    this.isLoading = true;
    this.reportService.getInventoryReportPdf(this.startDate, this.endDate).subscribe({
      next: (blob) => {
        const url = window.URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = `InventoryReport_${this.startDate}_to_${this.endDate}.pdf`;
        a.click();
        window.URL.revokeObjectURL(url);
        this.toastr.success('PDF downloaded successfully.', 'Success');
      },
      error: () => this.toastr.error('Failed to generate PDF.', 'Error'),
      complete: () => (this.isLoading = false)
    });
  }

  get filteredItems(): StockoutReportItem[] {
    const text = this.filterText.trim().toLowerCase();
    return this.reportItems
      .filter(item =>
        item.productName.toLowerCase().includes(text) ||
        item.shelfLocation.toLowerCase().includes(text)
      )
      .sort((a, b) => {
        const aVal = a[this.sortColumn];
        const bVal = b[this.sortColumn];

        if (typeof aVal === 'string' && typeof bVal === 'string') {
          return this.sortDirection === 'asc'
            ? aVal.localeCompare(bVal)
            : bVal.localeCompare(aVal);
        }

        if (typeof aVal === 'number' && typeof bVal === 'number') {
          return this.sortDirection === 'asc' ? aVal - bVal : bVal - aVal;
        }

        return 0;
      });
  }

  sortBy(column: SortableColumn): void {
    if (this.sortColumn === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortColumn = column;
      this.sortDirection = 'asc';
    }
  }
}
