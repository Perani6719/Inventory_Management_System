import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { OrganizeProductService } from '../../services/organize-product.service';
 
@Component({
  selector: 'app-organize-products',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './organize-products.html',
  styleUrls: ['./organize-products.css']
})
export class OrganizeProductsComponent {
  staffId!: number;
  taskId!: number;
  message: string = '';
  success: boolean = false;
  loading: boolean = false;
 
  constructor(private organizeService:  OrganizeProductService) {}
 
  onSubmit() {
    this.loading = true;
    this.message = '';
    this.organizeService.organizeProduct(this.staffId, this.taskId)
  .subscribe({
    next: (msg: string) => {
      this.message = msg;
      this.success = true;
      this.loading = false;
    },
    error: (err: any) => {
      this.message = 'Error: ' + err.message;
      this.success = false;
      this.loading = false;
    }
  });
  }
}
 
 