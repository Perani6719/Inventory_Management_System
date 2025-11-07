import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, FormsModule, ReactiveFormsModule, Validators } from '@angular/forms';
import { CommonModule } from '@angular/common';
import { HttpClientModule } from '@angular/common/http';
import { ToastrService } from 'ngx-toastr';
import { ShelfService } from '../../services/shelf.service';
import { CategoryService } from '../../services/category.service';
 
export interface Shelf {
  shelfId: number;
  shelfCode: string;
  storeId: number;
  categoryId: number;
  locationDescription?: string;
  capacity: number;
  imageUrl?: string;
  // optional human-friendly category name populated after loading
  categoryName?: string;
}
 
export interface Category {
  categoryId: number;
  categoryName: string;
}
 
@Component({
  selector: 'app-shelf',
  standalone: true,
  imports: [CommonModule, FormsModule, ReactiveFormsModule, HttpClientModule],
  templateUrl: './shelf.html',
  styleUrls: ['./shelf.css']
})
export class ShelfComponent implements OnInit {
  shelfForm!: FormGroup;
  shelves: Shelf[] = [];
  categories: Category[] = [];
 
  isEditMode = false;
  editShelfId: number | null = null;
  isSubmitting = false;
  isLoadingShelves = false;
  selectedImageFile: File | null = null;
  selectedImagePreview: string | null = null;
 
  showDeleteModal = false;
  pendingDeleteId: number | null = null;
  pendingDeleteCode = '';
 
  filterText = '';
  sortColumn: keyof Shelf = 'shelfCode';
  sortDirection: 'asc' | 'desc' = 'asc';
  currentPage = 1;
  pageSize = 5;
 
  constructor(
    private fb: FormBuilder,
    private toastr: ToastrService,
    private shelfService: ShelfService,
    private categoryService: CategoryService
  ) {}
 
  ngOnInit(): void {
    this.shelfForm = this.fb.group({
      shelfCode: ['', [Validators.required, Validators.maxLength(50)]],
      storeId: [null, [Validators.required, Validators.min(1)]],
      categoryId: [null, [Validators.required, Validators.min(1)]],
      locationDescription: ['', Validators.maxLength(100)],
      capacity: [null, [Validators.required, Validators.min(1)]]
    });
 
    this.loadCategories(); // ✅ Load categories on init
    // ❌ Do NOT auto-load shelves
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
 
  loadShelves(): void {
    this.isLoadingShelves = true;
    this.shelfService.getAllShelves().subscribe({
      next: (res: any) => {
        const data = Array.isArray(res) ? res : res?.data;
        const raw = Array.isArray(data) ? data : [];
        // Attach categoryName when possible for easier display
        this.shelves = raw.map((s: any) => ({
          ...s,
          categoryName: this.getCategoryName(s.categoryId)
        }));
        this.toastr.info(`Loaded ${this.shelves.length} shelves.`, 'Info');
        this.isLoadingShelves = false;
      },
      error: () => {
        this.toastr.error('Failed to load shelves.', 'Error');
        this.isLoadingShelves = false;
      }
    });
  }
 
  get filteredShelves(): Shelf[] {
    const text = this.filterText.trim().toLowerCase();
    let filtered = this.shelves.filter(s =>
      s.shelfCode.toLowerCase().includes(text) ||
      s.storeId.toString().includes(text) ||
      (s.categoryName || s.categoryId).toString().toLowerCase().includes(text) ||
      s.locationDescription?.toLowerCase().includes(text) ||
      s.capacity.toString().includes(text)
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
 
  get paginatedShelves(): Shelf[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredShelves.slice(start, start + this.pageSize);
  }
 
  get totalPages(): number {
    return Math.ceil(this.filteredShelves.length / this.pageSize);
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
 
  sortBy(column: keyof Shelf): void {
    if (this.sortColumn === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortColumn = column;
      this.sortDirection = 'asc';
    }
  }
 
  onSubmit(): void {
    if (this.shelfForm.invalid) {
      this.shelfForm.markAllAsTouched();
      this.toastr.warning('Please fix the errors in the form.', 'Validation Error');
      return;
    }
 
    this.isSubmitting = true;
    // If an image file was selected, send multipart/form-data, otherwise send JSON
    let request: any;
    if (this.selectedImageFile) {
      const formData = new FormData();
      const vals: any = this.shelfForm.value;
      formData.append('shelfCode', vals.shelfCode);
      formData.append('storeId', String(Number(vals.storeId)));
      formData.append('categoryId', String(Number(vals.categoryId)));
      formData.append('locationDescription', vals.locationDescription || '');
      formData.append('capacity', String(Number(vals.capacity)));
      formData.append('image', this.selectedImageFile);
 
      request = this.isEditMode && this.editShelfId
        ? this.shelfService.updateShelf(this.editShelfId, formData)
        : this.shelfService.createShelf(formData);
    } else {
      const payload = {
        ...this.shelfForm.value,
        storeId: Number(this.shelfForm.value.storeId),
        categoryId: Number(this.shelfForm.value.categoryId),
        capacity: Number(this.shelfForm.value.capacity),
      };
 
      request = this.isEditMode && this.editShelfId
        ? this.shelfService.updateShelf(this.editShelfId, payload)
        : this.shelfService.createShelf(payload);
    }
 
    request.subscribe({
      next: () => {
        this.toastr.success(this.isEditMode ? 'Shelf updated!' : 'Shelf created!', 'Success');
        this.loadShelves();
      },
      error: (err: { error: { message: any; }; }) => {
        this.toastr.error(err.error?.message || 'Operation failed.', 'Error');
      },
      complete: () => this.resetForm()
    });
  }
 
  openEditModal(shelf: Shelf): void {
    this.isEditMode = true;
    this.editShelfId = shelf.shelfId;
    this.shelfForm.patchValue({
      ...shelf,
      categoryId: shelf.categoryId.toString(),
      storeId: shelf.storeId.toString(),
      capacity: shelf.capacity.toString(),
    });
    // show existing server image as preview when editing
    this.selectedImagePreview = shelf.imageUrl || null;
    this.selectedImageFile = null;
  }
 
  resetForm(): void {
    this.shelfForm.reset({
      storeId: null,
      categoryId: null,
      capacity: null,
    });
    this.isEditMode = false;
    this.editShelfId = null;
    this.isSubmitting = false;
    this.selectedImageFile = null;
    this.selectedImagePreview = null;
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
 
    this.shelfService.deleteShelf(this.pendingDeleteId).subscribe({
      next: () => {
        this.toastr.success('Shelf deleted.', 'Deleted');
        this.loadShelves();
      },
      error: (err) => this.toastr.error(err.error?.message || 'Failed to delete shelf.', 'Error'),
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
    return this.shelfForm.controls;
  }
 
  private getCategoryName(id: number): string {
    const c = this.categories.find(x => x.categoryId === id || (x as any).id === id);
    return c ? (c.categoryName || (c as any).name || String(id)) : String(id);
  }
}
 
 