// import { Component, OnInit } from '@angular/core';
// import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
// import { CommonModule } from '@angular/common';
// import { HttpClientModule } from '@angular/common/http';
// import { ToastrService } from 'ngx-toastr';
// import { ProductService } from '../../services/product.service';
// import { CategoryService, Category } from '../../services/category.service';

// export interface Product {
//   productId: number;
//   stockKeepingUnit: string;
//   productName: string;
//   categoryId: number;
//   packageSize?: string;
//   unit?: string;
//   imageUrl?: string;
//   categoryName?: string; // added to display name instead of id
// }

// @Component({
//   selector: 'app-product',
//   standalone: true,
//   imports: [CommonModule, FormsModule, ReactiveFormsModule, HttpClientModule],
//   templateUrl: './add-product.html',
//   styleUrls: ['./add-product.css']
// })
// export class ProductComponent implements OnInit {
//   productForm!: FormGroup;
//   products: Product[] = [];
//   categories: Category[] = [];

//   isEditMode = false;
//   editProductId: number | null = null;
//   isSubmitting = false;
//   isLoadingProducts = false;
//   selectedImageFile: File | null = null;
//   selectedImagePreview: string | null = null;

//   showDeleteModal = false;
//   pendingDeleteId: number | null = null;
//   pendingDeleteName = '';

//   // Filtering, Sorting, Pagination
//   filterText = '';
//   sortColumn: keyof Product = 'productName';
//   sortDirection: 'asc' | 'desc' = 'asc';
//   currentPage = 1;
//   pageSize = 5;

//   constructor(
//     private fb: FormBuilder,
//     private toastr: ToastrService,
//     private productService: ProductService,
//     private categoryService: CategoryService
//   ) {}

//   ngOnInit(): void {
//     this.productForm = this.fb.group({
//       stockKeepingUnit: ['', [Validators.required, Validators.maxLength(50)]],
//       productName: ['', [Validators.required, Validators.maxLength(100)]],
//       categoryId: [null, Validators.required],
//       packageSize: ['', Validators.maxLength(50)],
//       unit: ['', Validators.maxLength(20)]
//     });

//     // load categories first so we can show names in the form and map when loading products
//     this.loadCategories();
//     // load products list
//     this.loadProducts();
//   }

//   loadProducts(): void {
//     this.isLoadingProducts = true;
//     this.products = [];

//     this.productService.getAllProducts().subscribe({
//       next: (res: Product[]) => {
//         this.products = (res ?? []).map(p => ({
//           ...p,
//           categoryName: this.getCategoryName(p.categoryId)
//         }));
//         this.toastr.success(`Loaded ${this.products.length} products.`, 'Success');
//         this.isLoadingProducts = false;
//       },
//       error: () => {
//         this.toastr.error('Failed to load products.', 'Error');
//         this.isLoadingProducts = false;
//       }
//     });
//   }

//   loadCategories(): void {
//     this.categoryService.getAllCategories().subscribe({
//       next: (res: any) => {
//         const data = Array.isArray(res) ? res : res?.data || res?.result;
//         this.categories = Array.isArray(data) ? data : [];
//       },
//       error: () => {
//         this.toastr.error('Failed to load categories.', 'Error');
//       }
//     });
//   }

//   private getCategoryName(id: number): string {
//   const cat = this.categories.find(c => c.categoryId === id);
//   return cat?.categoryName ?? `Category ${id}`;
// }


//   get filteredProducts(): Product[] {
//     const text = this.filterText.trim().toLowerCase();
//     let filtered = this.products.filter(p =>
//       p.stockKeepingUnit.toLowerCase().includes(text) ||
//       p.productName.toLowerCase().includes(text) ||
//       p.categoryId.toString().includes(text) ||
//       p.packageSize?.toLowerCase().includes(text) ||
//       p.unit?.toLowerCase().includes(text)
//     );

//     filtered.sort((a, b) => {
//       const aVal = (a[this.sortColumn] ?? '').toString().toLowerCase();
//       const bVal = (b[this.sortColumn] ?? '').toString().toLowerCase();
//       return this.sortDirection === 'asc'
//         ? aVal.localeCompare(bVal)
//         : bVal.localeCompare(aVal);
//     });

//     return filtered;
//   }

//   get paginatedProducts(): Product[] {
//     const start = (this.currentPage - 1) * this.pageSize;
//     return this.filteredProducts.slice(start, start + this.pageSize);
//   }

//   get totalPages(): number {
//     return Math.ceil(this.filteredProducts.length / this.pageSize);
//   }

//   changePageSize(size: number): void {
//     this.pageSize = size;
//     this.currentPage = 1;
//   }

//   nextPage(): void {
//     if (this.currentPage < this.totalPages) this.currentPage++;
//   }

//   prevPage(): void {
//     if (this.currentPage > 1) this.currentPage--;
//   }

//   sortBy(column: keyof Product): void {
//     if (this.sortColumn === column) {
//       this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
//     } else {
//       this.sortColumn = column;
//       this.sortDirection = 'asc';
//     }
//   }

//   onSubmit(): void {
//     if (this.productForm.invalid) {
//       this.productForm.markAllAsTouched();
//       return;
//     }

//     this.isSubmitting = true;
//     // If user selected an image, send FormData to include the file; otherwise send JSON payload
//     let request: any;
//     if (this.selectedImageFile) {
//       const formData = new FormData();
//       const vals: any = this.productForm.value;
//       formData.append('stockKeepingUnit', vals.stockKeepingUnit);
//       formData.append('productName', vals.productName);
//       formData.append('categoryId', String(vals.categoryId));
//       formData.append('packageSize', vals.packageSize || '');
//       formData.append('unit', vals.unit || '');
//       formData.append('image', this.selectedImageFile);

//       // cast FormData to any so it can be passed to service methods expecting Partial<Product>
//       request = this.isEditMode && this.editProductId
//         ? this.productService.updateProduct(this.editProductId, formData as any)
//         : this.productService.createProduct(formData as any);
//     } else {
//       const payload = { ...this.productForm.value };
//       request = this.isEditMode && this.editProductId
//         ? this.productService.updateProduct(this.editProductId, payload)
//         : this.productService.createProduct(payload);
//     }

//     request.subscribe({
//       next: () => {
//         this.toastr.success(this.isEditMode ? 'Product updated!' : 'Product created!', 'Success');
//         this.loadProducts();
//       },
//       error: () => this.toastr.error('Operation failed.', 'Error'),
//       complete: () => this.resetForm()
//     });
//   }

//   openEditModal(product: Product): void {
//     this.isEditMode = true;
//     this.editProductId = product.productId;
//     this.productForm.patchValue(product);
//     // Show existing server image when editing (if available)
//     // product may include imageUrl property coming from backend
//     // keep selectedImageFile null until user picks a new file
//     this.selectedImagePreview = product.imageUrl || null;
//     this.selectedImageFile = null;
//   }

//   resetForm(): void {
//     this.productForm.reset();
//     this.isEditMode = false;
//     this.editProductId = null;
//     this.isSubmitting = false;
//     this.selectedImageFile = null;
//     this.selectedImagePreview = null;
//   }

//   openDeleteModal(id: number, name: string): void {
//     this.pendingDeleteId = id;
//     this.pendingDeleteName = name;
//     this.showDeleteModal = true;
//   }

//   cancelDelete(): void {
//     this.pendingDeleteId = null;
//     this.pendingDeleteName = '';
//     this.showDeleteModal = false;
//   }

//   confirmDelete(): void {
//     if (!this.pendingDeleteId) return;

//     this.productService.deleteProduct(this.pendingDeleteId).subscribe({
//       next: () => {
//         this.toastr.success('Product deleted.', 'Deleted');
//         this.loadProducts();
//       },
//       error: () => this.toastr.error('Failed to delete product.', 'Error'),
//       complete: () => this.cancelDelete()
//     });
//   }

//   onImageSelected(event: any): void {
//     const file = event.target.files[0];
//     if (file) {
//       this.selectedImageFile = file;
//       const reader = new FileReader();
//       reader.onload = (e: any) => this.selectedImagePreview = e.target.result;
//       reader.readAsDataURL(file);
//     } else {
//       this.selectedImageFile = null;
//       this.selectedImagePreview = null;
//     }
//   }

//   get f() {
//     return this.productForm.controls;
//   }
// }


import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, Validators, ReactiveFormsModule, FormsModule } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { ToastrService } from 'ngx-toastr';
import { ProductService } from '../../services/product.service';
import { CategoryService, Category } from '../../services/category.service';

export interface Product {
  productId: number;
  stockKeepingUnit: string;
  productName: string;
  categoryId: number;
  packageSize?: string;
  unit?: string;
  imageUrl?: string;
  categoryName?: string;
}

@Component({
  selector: 'app-product',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, HttpClientModule],
  templateUrl: './add-product.html',
  styleUrls: ['./add-product.css']
})
export class ProductComponent implements OnInit {
  productForm!: FormGroup;
  products: Product[] = [];
  categories: Category[] = [];

  isEditMode = false;
  editProductId: number | null = null;
  isSubmitting = false;
  isLoadingProducts = false;
  selectedImageFile: File | null = null;
  selectedImagePreview: string | null = null;

  showDeleteModal = false;
  pendingDeleteId: number | null = null;
  pendingDeleteName = '';

  filterText = '';
  sortColumn: keyof Product = 'productName';
  sortDirection: 'asc' | 'desc' = 'asc';
  currentPage = 1;
  pageSize = 5;

  constructor(
    private fb: FormBuilder,
    private toastr: ToastrService,
    private productService: ProductService,
    private categoryService: CategoryService
  ) {}

  ngOnInit(): void {
    this.productForm = this.fb.group({
      stockKeepingUnit: ['', [Validators.required, Validators.maxLength(50)]],
      productName: ['', [Validators.required, Validators.maxLength(100)]],
      categoryId: [null, Validators.required],
      packageSize: ['', Validators.maxLength(50)],
      unit: ['', Validators.maxLength(20)]
    });

    this.loadCategories();
    this.loadProducts();
  }

  loadCategories(): void {
    this.categoryService.getAllCategories().subscribe({
      next: (res: any) => {
        const data = Array.isArray(res) ? res : res?.data || res?.result;
        this.categories = Array.isArray(data) ? data : [];
      },
      error: () => this.toastr.error('Failed to load categories.', 'Error')
    });
  }

  loadProducts(): void {
    this.isLoadingProducts = true;
    this.products = [];

    this.productService.getAllProducts().subscribe({
      next: (res: Product[]) => {
        this.products = (res ?? []).map(p => ({
          ...p,
          categoryName: this.getCategoryName(p.categoryId)
        }));
        this.toastr.success(`Loaded ${this.products.length} products.`, 'Success');
        this.isLoadingProducts = false;
      },
      error: () => {
        this.toastr.error('Failed to load products.', 'Error');
        this.isLoadingProducts = false;
      }
    });
  }

  private getCategoryName(id: number): string {
    const cat = this.categories.find(c => c.categoryId === id);
    return cat?.categoryName ?? `Category ${id}`;
  }

  get filteredProducts(): Product[] {
    const text = this.filterText.trim().toLowerCase();
    let filtered = this.products.filter(p =>
      p.stockKeepingUnit.toLowerCase().includes(text) ||
      p.productName.toLowerCase().includes(text) ||
      p.categoryName?.toLowerCase().includes(text) ||
      p.packageSize?.toLowerCase().includes(text) ||
      p.unit?.toLowerCase().includes(text)
    );

    filtered.sort((a, b) => {
      const aVal = (a[this.sortColumn] ?? '').toString().toLowerCase();
      const bVal = (b[this.sortColumn] ?? '').toString().toLowerCase();
      return this.sortDirection === 'asc'
        ? aVal.localeCompare(bVal)
        : bVal.localeCompare(aVal);
    });

    return filtered;
  }

  get paginatedProducts(): Product[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredProducts.slice(start, start + this.pageSize);
  }

  get totalPages(): number {
    return Math.ceil(this.filteredProducts.length / this.pageSize);
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

  sortBy(column: keyof Product): void {
    if (this.sortColumn === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortColumn = column;
      this.sortDirection = 'asc';
    }
  }

  onSubmit(): void {
    if (this.productForm.invalid) {
      this.productForm.markAllAsTouched();
      return;
    }

    this.isSubmitting = true;
    let request: any;

    if (this.selectedImageFile) {
      const formData = new FormData();
      const vals: any = this.productForm.value;
      formData.append('stockKeepingUnit', vals.stockKeepingUnit);
      formData.append('productName', vals.productName);
      formData.append('categoryId', String(vals.categoryId));
      formData.append('packageSize', vals.packageSize || '');
      formData.append('unit', vals.unit || '');
      formData.append('image', this.selectedImageFile);

      request = this.isEditMode && this.editProductId
        ? this.productService.updateProduct(this.editProductId, formData as any)
        : this.productService.createProduct(formData as any);
    } else {
      const payload = { ...this.productForm.value };
      request = this.isEditMode && this.editProductId
        ? this.productService.updateProduct(this.editProductId, payload)
        : this.productService.createProduct(payload);
    }

    request.subscribe({
      next: () => {
        this.toastr.success(this.isEditMode ? 'Product updated!' : 'Product created!', 'Success');
        this.loadProducts();
      },
      error: () => this.toastr.error('Operation failed.', 'Error'),
      complete: () => this.resetForm()
    });
  }

  openEditModal(product: Product): void {
    this.isEditMode = true;
    this.editProductId = product.productId;
    this.productForm.patchValue(product);
    this.selectedImagePreview = product.imageUrl || null;
    this.selectedImageFile = null;
  }

  resetForm(): void {
    this.productForm.reset();
    this.isEditMode = false;
    this.editProductId = null;
    this.isSubmitting = false;
    this.selectedImageFile = null;
    this.selectedImagePreview = null;
  }

  openDeleteModal(id: number, name: string): void {
    this.pendingDeleteId = id;
    this.pendingDeleteName = name;
    this.showDeleteModal = true;
  }

  cancelDelete(): void {
    this.pendingDeleteId = null;
    this.pendingDeleteName = '';
    this.showDeleteModal = false;
  }

  confirmDelete(): void {
    if (!this.pendingDeleteId) return;

    this.productService.deleteProduct(this.pendingDeleteId).subscribe({
      next: () => {
        this.toastr.success('Product deleted.', 'Deleted');
        this.loadProducts();
      },
      error: () => this.toastr.error('Failed to delete product.', 'Error'),
      complete: () => this.cancelDelete()
    });
  }

  onImageSelected(event: any): void {
    const file = event.target.files[0];
    if (file) {
      this.selectedImageFile = file;
      const reader = new FileReader();
      reader.onload = (e: any) => this.selectedImagePreview = e.target.result;
      reader.readAsDataURL(file);
    } else {
      this.selectedImageFile = null;
      this.selectedImagePreview = null;
    }
  }

  get f() {
    return this.productForm.controls;
  }
}
