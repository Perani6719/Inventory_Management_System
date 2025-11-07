import { Component } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Sidebar } from "../sidebar/sidebar";


@Component({
  selector: 'app-landing',
  standalone: true,
  imports: [CommonModule, Sidebar],
  templateUrl: './landing.html',
  styleUrls: ['./landing.css']
})
export class Landing {}
