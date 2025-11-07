import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';

interface Review {
  name: string;
  role: string;
  comment: string;
  date: string;
  rating: number; // 1 to 5
}

@Component({
  selector: 'app-reviews',
  standalone: true,
  imports: [CommonModule],
  templateUrl: './reviews.html',
  styleUrls: ['./reviews.css']
})
export class ReviewsComponent {
  reviews: Review[] = [
    { name: 'Ravi Kumar', role: 'Warehouse Staff', comment: 'ShelfSense has made restocking tasks so much easier.', date: 'Oct 25, 2025', rating: 5 },
    { name: 'Meena Sharma', role: 'Store Manager', comment: 'The dashboard is intuitive and helps me track everything.', date: 'Oct 28, 2025', rating: 4 },
    { name: 'Anil Verma', role: 'Inventory Analyst', comment: 'Predictive depletion is a game changer.', date: 'Oct 29, 2025', rating: 5 },
    { name: 'Priya Desai', role: 'Retail Supervisor', comment: 'Alerts are timely and accurate.', date: 'Oct 30, 2025', rating: 4 },
    { name: 'Karan Joshi', role: 'Stock Manager', comment: 'ShelfSense reduced our manual tracking by 70%.', date: 'Oct 27, 2025', rating: 5 },
    { name: 'Neha Reddy', role: 'Operations Lead', comment: 'Role-based access keeps everything secure and organized.', date: 'Oct 26, 2025', rating: 4 },
    { name: 'Vikram Singh', role: 'Logistics Coordinator', comment: 'The UI is clean and easy to use.', date: 'Oct 24, 2025', rating: 5 },
    { name: 'Aarti Mehta', role: 'Procurement Officer', comment: 'We’ve seen a real boost in efficiency.', date: 'Oct 23, 2025', rating: 5 }
  ];
}
