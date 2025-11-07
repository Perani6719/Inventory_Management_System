import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { ToastrService } from 'ngx-toastr';
import { ProductShelfService, ProductShelfEntry } from '../../services/product-shelf.service';
import { ProductService } from '../../services/product.service';
import { ShelfService } from '../../services/shelf.service';
import { CategoryService, Category } from '../../services/category.service';
import { Product } from '../add-product/add-product';
 
@Component({
  selector: 'app-product-shelf',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, HttpClientModule],
  templateUrl: './product-shelf.html',
  styleUrls: ['./product-shelf.css']
})
export class ProductShelf implements OnInit {
  mappingForm!: FormGroup;
  productShelves: ProductShelfEntry[] = [];
 
  products: Product[] = [];
  shelves: any[] = [];
  categories: Category[] = [];
 
  isEditMode = false;
  editId: number | null = null;
  isSubmitting = false;
  isLoading = false;
 
  showDeleteModal = false;
  pendingDeleteId: number | null = null;
 
  filterText = '';
  sortColumn: keyof ProductShelfEntry = 'productShelfId';
  sortDirection: 'asc' | 'desc' = 'asc';
  currentPage = 1;
  pageSize = 5;
 
  constructor(
    private fb: FormBuilder,
    private toastr: ToastrService,
    private productShelfService: ProductShelfService
    , private productService: ProductService
    , private shelfService: ShelfService
    , private categoryService: CategoryService
  ) {
    this.mappingForm = this.fb.group({
      productId: [null, [Validators.required, Validators.min(1)]],
      shelfId: [null],
      categoryId: [null, [Validators.required, Validators.min(1)]],
      initialQuantity: [null, [Validators.required, Validators.min(1)]],
      maxCapacity: [200, [Validators.required, Validators.min(1)]]
    });
  }
 
  ngOnInit(): void {
    this.loadProducts();
    this.loadShelves();
    this.loadCategories();
    this.loadMappings();
  }
 
  loadProducts(): void {
    this.productService.getAllProducts().subscribe({
      next: (res: any) => {
        const data = Array.isArray(res) ? res : res?.data || res?.result;
        this.products = Array.isArray(data) ? data : [];
        // Attempt to attach product names to any already-loaded mappings
        this.attachNamesToMappings();
      }
    });
  }
 
  loadShelves(): void {
    this.shelfService.getAllShelves().subscribe({
      next: (res: any) => {
        const data = Array.isArray(res) ? res : res?.data || res?.result;
        this.shelves = Array.isArray(data) ? data : [];
        // After shelves load, update mapping entries with shelf names
        this.attachNamesToMappings();
      }
    });
  }
 
  loadCategories(): void {
    this.categoryService.getAllCategories().subscribe({
      next: (res: any) => {
        const data = Array.isArray(res) ? res : res?.data || res?.result;
        this.categories = Array.isArray(data) ? data : [];
      }
    });
  }
 
  loadMappings(): void {
    this.isLoading = true;
    this.productShelfService.getAll().subscribe({
      next: (res) => {
        // Map ids to human readable names when possible
        this.productShelves = (res.data || []).map((e: any) => ({ ...e }));
        // Attach readable names once we have products/shelves
        this.attachNamesToMappings();
        this.isLoading = false;
      },
      error: () => {
        this.toastr.error('Failed to load mappings.', 'Error');
        this.isLoading = false;
      }
    });
  }
 
  /**
   * Attach human-readable productName and shelfName to productShelf entries.
   * Called after products, shelves or mappings are loaded to ensure names appear
   * even if requests resolve in different orders.
   */
  private attachNamesToMappings(): void {
    if (!this.productShelves || this.productShelves.length === 0) return;
 
    this.productShelves = this.productShelves.map((e: any) => ({
      ...e,
      productName: this.getProductName(e.productId),
      shelfName: this.getShelfName(e.shelfId)
    }));
  }
 
  private getProductName(id: number): string {
    const p = this.products.find(x => x.productId === id);
    return p ? p.productName : String(id);
  }
 
  private getShelfName(id: number): string {
    const s = this.shelves.find(x => x.shelfId === id || x.id === id);
    // Try multiple possible shelf name fields and also use shelfCode if available
    if (!s) return String(id);
    return (
      s.shelfName || s.name || s.label || s.shelfCode || (s as any).code || String(id)
    );
  }
 
  get filteredMappings(): ProductShelfEntry[] {
    const text = this.filterText.trim().toLowerCase();
    return this.productShelves
      .filter(entry =>
        (entry.productName || entry.productId).toString().toLowerCase().includes(text) ||
        (entry.shelfName || entry.shelfId)?.toString().toLowerCase().includes(text) ||
        entry.quantity.toString().includes(text) ||
        (entry.maxCapacity ?? '').toString().includes(text)
      )
      .sort((a, b) => {
        const aVal = (a[this.sortColumn] ?? '').toString().toLowerCase();
        const bVal = (b[this.sortColumn] ?? '').toString().toLowerCase();
        return this.sortDirection === 'asc'
          ? aVal.localeCompare(bVal)
          : bVal.localeCompare(aVal);
      });
  }
 
  get paginatedMappings(): ProductShelfEntry[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredMappings.slice(start, start + this.pageSize);
  }
 
  get totalPages(): number {
    return Math.ceil(this.filteredMappings.length / this.pageSize);
  }
 
  changePageSize(size: number): void {
    this.pageSize = size;
    this.currentPage = 1;
  }
 
  nextPage(): void {
    if (this.currentPage < this.totalPages) this.currentPage++;
  }
 
  prevPage(): void {
    if (this.currentPage > 1) this.currentPage--;
  }
 
  sortBy(column: keyof ProductShelfEntry): void {
    if (this.sortColumn === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortColumn = column;
      this.sortDirection = 'asc';
    }
  }
 
  onSubmit(): void {
    if (this.mappingForm.invalid) {
      this.mappingForm.markAllAsTouched();
      return;
    }
 
    this.isSubmitting = true;
 
    if (this.isEditMode && this.editId) {
      const updatePayload = {
        productId: Number(this.mappingForm.value.productId),
        shelfId: Number(this.mappingForm.value.shelfId),
        quantity: Number(this.mappingForm.value.initialQuantity),
        maxCapacity: Number(this.mappingForm.value.maxCapacity)
      };
 
      this.productShelfService.update(this.editId, updatePayload).subscribe({
        next: () => {
          this.toastr.success('Mapping updated!', 'Success');
          this.loadMappings();
          this.resetForm();
        },
        error: (err) => {
          this.toastr.error(err.error?.message || 'Update failed.', 'Error');
          this.isSubmitting = false;
        }
      });
    } else {
      const assignPayload = {
        productId: Number(this.mappingForm.value.productId),
        categoryId: Number(this.mappingForm.value.categoryId),
        initialQuantity: Number(this.mappingForm.value.initialQuantity),
        maxCapacity: Number(this.mappingForm.value.maxCapacity)
      };
 
      this.productShelfService.autoAssign(assignPayload).subscribe({
        next: () => {
          this.toastr.success('Product auto-assigned!', 'Success');
          this.loadMappings();
          this.resetForm();
        },
        error: (err) => {
          this.toastr.error(err.error?.message || 'Auto-assignment failed.', 'Error');
          this.isSubmitting = false;
        }
      });
    }
  }
 
  openEditModal(entry: ProductShelfEntry): void {
    this.isEditMode = true;
    this.editId = entry.productShelfId;
    this.mappingForm.patchValue({
      productId: entry.productId,
      shelfId: entry.shelfId,
      categoryId: null,
      initialQuantity: entry.quantity,
      maxCapacity: entry.maxCapacity
    });
 
    this.mappingForm.get('categoryId')?.clearValidators();
    this.mappingForm.get('categoryId')?.updateValueAndValidity();
 
    this.mappingForm.get('shelfId')?.setValidators([Validators.required, Validators.min(1)]);
    this.mappingForm.get('shelfId')?.updateValueAndValidity();
  }
 
  resetForm(): void {
    this.mappingForm.reset({ maxCapacity: 200 });
    this.isEditMode = false;
    this.editId = null;
    this.isSubmitting = false;
 
    this.mappingForm.get('categoryId')?.setValidators([Validators.required, Validators.min(1)]);
    this.mappingForm.get('categoryId')?.updateValueAndValidity();
 
    this.mappingForm.get('shelfId')?.clearValidators();
    this.mappingForm.get('shelfId')?.updateValueAndValidity();
  }
 
  openDeleteModal(id: number): void {
    this.pendingDeleteId = id;
    this.showDeleteModal = true;
  }
 
  cancelDelete(): void {
    this.pendingDeleteId = null;
    this.showDeleteModal = false;
  }
 
  confirmDelete(): void {
    if (!this.pendingDeleteId) return;
 
    this.productShelfService.delete(this.pendingDeleteId).subscribe({
      next: () => {
        this.toastr.success('Mapping deleted.', 'Deleted');
        this.loadMappings();
      },
      error: () => this.toastr.error('Failed to delete mapping.', 'Error'),
      complete: () => this.cancelDelete()
    });
  }
 
  get f() {
    return this.mappingForm.controls;
  }
}
 
 