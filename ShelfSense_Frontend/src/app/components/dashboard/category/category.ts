 
import { Component, OnInit } from '@angular/core';
import { FormBuilder, FormGroup, ReactiveFormsModule, Validators } from '@angular/forms';
import { HttpClientModule } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { CategoryService } from '../../services/category.service';
 
@Component({
  selector: 'app-category',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, HttpClientModule, FormsModule],
  templateUrl: './category.html',
  styleUrls: ['./category.css']
})
export class Category implements OnInit {
  categoryForm!: FormGroup;
  categories: any[] = [];
  isLoadingCategories = false;
  isSubmitting = false;
 
  isEditMode = false;
  editCategoryId: number | null = null;
  selectedImageFile: File | null = null;
  selectedImagePreview: string | null = null;
 
  showDeleteModal = false;
  pendingDeleteId: number | null = null;
  pendingDeleteName = '';
 
  filterText = '';
  currentPage = 1;
  pageSize = 5;
  sortColumn: string = 'categoryName';
  sortDirection: 'asc' | 'desc' = 'asc';
 
  constructor(
    private fb: FormBuilder,
    private toastr: ToastrService,
    private categoryService: CategoryService
  ) {}
 
  ngOnInit(): void {
    this.categoryForm = this.fb.group({
      categoryName: ['', [Validators.required, Validators.maxLength(100)]],
      description: ['', Validators.maxLength(255)]
    });
 
    this.loadCategories();
  }
 
  onImageSelected(event: any): void {
    const file = event.target.files[0];
    if (file) {
      this.selectedImageFile = file;
      const reader = new FileReader();
      reader.onload = (e: any) => {
        this.selectedImagePreview = e.target.result;
      };
      reader.readAsDataURL(file);
    } else {
      this.selectedImageFile = null;
      this.selectedImagePreview = null;
    }
  }
 
  onSubmit(): void {
    if (this.categoryForm.invalid) {
      this.categoryForm.markAllAsTouched();
      return;
    }
 
    this.isSubmitting = true;
    const formData = new FormData();
    formData.append('categoryName', this.categoryForm.value.categoryName);
    formData.append('description', this.categoryForm.value.description || '');
    if (this.selectedImageFile) {
      formData.append('image', this.selectedImageFile);
    }
 
    const request = this.isEditMode && this.editCategoryId
      ? this.categoryService.updateCategory(this.editCategoryId, formData)
      : this.categoryService.createCategory(formData);
 
    request.subscribe({
      next: () => {
        this.toastr.success(this.isEditMode ? 'Category updated!' : 'Category created!', 'Success');
        this.loadCategories();
      },
      error: err => {
        this.toastr.error(err.status === 409 ? 'Category already exists.' : 'Operation failed.', 'Error');
      },
      complete: () => this.resetForm()
    });
  }
 
  loadCategories(): void {
    this.isLoadingCategories = true;
    this.categories = [];
 
    this.categoryService.getAllCategories().subscribe({
      next: (res: any) => {
        this.isLoadingCategories = false;
        const data = Array.isArray(res) ? res : res?.data || res?.result;
        this.categories = Array.isArray(data) ? data : [];
        this.toastr.info(`Loaded ${this.categories.length} categories.`, 'Info');
      },
      error: () => {
        this.isLoadingCategories = false;
        this.toastr.error('Failed to load categories.', 'Error');
      }
    });
  }
 
  get filteredCategories(): any[] {
    const text = this.filterText.trim().toLowerCase();
    let filtered = this.categories.filter(c =>
      (c.categoryName || '').toLowerCase().includes(text) ||
      (c.description || '').toLowerCase().includes(text)
    );
 
    filtered.sort((a, b) => {
      const aVal = (a[this.sortColumn] || '').toString().toLowerCase();
      const bVal = (b[this.sortColumn] || '').toString().toLowerCase();
      return this.sortDirection === 'asc'
        ? aVal.localeCompare(bVal)
        : bVal.localeCompare(aVal);
    });
 
    return filtered;
  }
 
  get paginatedCategories(): any[] {
    const start = (this.currentPage - 1) * this.pageSize;
    return this.filteredCategories.slice(start, start + this.pageSize);
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
 
  get totalPages(): number {
    return Math.ceil(this.filteredCategories.length / this.pageSize);
  }
 
  sortBy(column: string): void {
    if (this.sortColumn === column) {
      this.sortDirection = this.sortDirection === 'asc' ? 'desc' : 'asc';
    } else {
      this.sortColumn = column;
      this.sortDirection = 'asc';
    }
  }
 
  openEditModal(category: any): void {
    this.isEditMode = true;
    this.editCategoryId = category.categoryId || category.id;
    this.categoryForm.patchValue({
      categoryName: category.categoryName,
      description: category.description
    });
    // Show existing image (from server) as the preview when editing
    this.selectedImageFile = null;
    this.selectedImagePreview = category.imageUrl || null;
  }
 
  resetForm(): void {
    this.categoryForm.reset();
    this.isEditMode = false;
    this.editCategoryId = null;
    this.selectedImageFile = null;
    this.selectedImagePreview = null;
    this.isSubmitting = false;
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
 
    this.categoryService.deleteCategory(this.pendingDeleteId).subscribe({
      next: () => {
        this.toastr.success('Category deleted.', 'Deleted');
        this.loadCategories();
      },
      error: err => {
        this.toastr.error(`Failed to delete category: ${err.error?.message || 'Server error'}`, 'Error');
      },
      complete: () => this.cancelDelete()
    });
  }
 
  get f() {
    return this.categoryForm.controls;
  }
}
 
 