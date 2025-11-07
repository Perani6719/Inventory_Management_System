import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { ToastrService } from 'ngx-toastr';
import { StaffService,Staff } from '../../services/staff.service';
  
@Component({
  selector: 'app-view-staff',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './viewstaff.html',
  styleUrls: ['./viewstaff.css']
})
export class ViewStaffComponent {
  staffList: Staff[] = [];
  isLoading = false;
  filterText = '';
 
  constructor(private staffService: StaffService, private toastr: ToastrService) {}
 
  loadStaff(): void {
    this.isLoading = true;
    this.staffService.getAllStaff().subscribe({
      next: (res: Staff[]) => {
        this.staffList = res ?? [];
        this.toastr.success(`Loaded ${this.staffList.length} staff.`, 'Success');
        this.isLoading = false;
      },
      error: () => {
        this.toastr.error('Failed to load staff.', 'Error');
        this.isLoading = false;
      }
    });
  }
 
  get filteredStaff(): Staff[] {
    const text = this.filterText.trim().toLowerCase();
    return this.staffList.filter(s =>
      s.name.toLowerCase().includes(text) ||
      s.email.toLowerCase().includes(text) ||
      s.role.toLowerCase().includes(text) ||
      s.storeId.toString().includes(text)
    );
  }
}
 
 