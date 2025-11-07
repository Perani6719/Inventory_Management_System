import { Routes } from '@angular/router';
import { Login } from './components/login/login';
import { Layout } from './components/layout/layout';
import { Landing } from './components/dashboard/landing/landing';
import { ProductComponent } from './components/dashboard/add-product/add-product';
import { ShelfComponent } from './components/dashboard/shelf/shelf';
import { Category } from './components/dashboard/category/category';
import { Register } from './components/register/register';
import { RoleGuard } from './components/guards/role-guard';
import { ProductShelf } from './components/dashboard/product-shelf/product-shelf';
import { ShelfMetricsComponent } from './components/dashboard/shelf-metrics/shelf-metrics';
import { StockRequestComponent } from './components/dashboard/stock-request/stock-request';
import { DeliveryLogComponent } from './components/dashboard/delivery-log-status/delivery-log-status';
import { CreateRequestAlertComponent } from './components/dashboard/create-request-alert/create-request-alert';
import { AllRequestsComponent } from './components/dashboard/all-requests/all-requests';
import { WarehouseDashboardComponent } from './components/dashboard/warehouse/warehouse-dashboard';
import { ReplenishmentAlertsComponent } from './components/dashboard/replenishment-alerts/replenishment-alerts';
import { PredictDepletionComponent } from './components/dashboard/predict-depletion/predict-depletion';
import { AssignTasksComponent } from './components/dashboard/assign-tasks/assign-tasks';
import { AllRestockTasksComponent } from './components/dashboard/all-restock-tasks/all-restock-tasks';
import { OrganizeProductsComponent } from './components/dashboard/organize-products/organize-products';
import { StaffComponent } from './components/dashboard/staff/staff';
import { ViewStaffComponent } from './components/dashboard/viewstaff/viewstaff';
import { InventoryReportService } from './components/services/inventory-report.service';
import { InventoryReportComponent } from './components/dashboard/inventory-report/inventory-report';

export const routes: Routes = [
  { path: '', redirectTo: 'login', pathMatch: 'full' },
  { path: 'login', component: Login },

  {
    path: 'warehouse-dashboard',
    component: WarehouseDashboardComponent,
    canActivate: [RoleGuard],
    data: { roles: ['warehouse'] }
  },

  {
    path: '',
    component: Layout,
    canActivateChild: [RoleGuard],
    children: [
      { path: 'landing', component: Landing, data: { roles: ['admin', 'manager', 'staff'] } },
      { path: 'add-product', component: ProductComponent, data: { roles: ['admin', 'manager', 'staff'] } },
      { path: 'add-category', component: Category, data: { roles: ['admin', 'manager', 'staff'] } },
      { path: 'add-shelf', component: ShelfComponent, data: { roles: ['admin', 'manager', 'staff'] } },
      { path: 'register', component: Register, data: { roles: ['admin', 'manager'] } },
      { path: 'product-shelf', component: ProductShelf, data: { roles: ['admin', 'manager', 'staff'] } },
      { path: 'shelf-metrics', component: ShelfMetricsComponent, data: { roles: ['admin', 'manager', 'staff'] } },
      { path: 'create-request-alert', component: CreateRequestAlertComponent, data: { roles: ['admin', 'manager', 'staff'] } },
      { path: 'all-requests', component: AllRequestsComponent, data: { roles: ['admin', 'manager', 'staff'] } },
      { path: 'replenishment-alerts', component: ReplenishmentAlertsComponent, data: { roles: ['admin', 'manager', 'staff'] } },
      { path: 'predict-depletion', component: PredictDepletionComponent, data: { roles: ['admin', 'manager', 'staff'] } },
      { path: 'delivery-log', component: DeliveryLogComponent, canActivate: [RoleGuard], data: { roles: ['admin', 'manager', 'staff'] } },
      { path: 'assign-tasks', component: AssignTasksComponent, data: { roles: ['admin', 'manager', 'staff'] } },
      { path: 'all-restock-tasks', component: AllRestockTasksComponent, data: { roles: ['admin', 'manager', 'staff'] } },
      { path: 'organize-products', component: OrganizeProductsComponent, data: { roles: ['admin', 'manager', 'staff'] } },
      { path: 'staff', component: StaffComponent, data: { roles: ['admin', 'manager'] } },
      { path: 'view-staff', component: ViewStaffComponent, data: { roles: ['admin', 'manager'] } },
      { path: 'inventory-report',component:InventoryReportComponent,data:{roles:['admin','manager','staff']}},

      // NavBar details
      { path: 'home', loadComponent: () => import('./components/dashboard/navbarDetails/home/home').then(m => m.HomeComponent), data: { roles: ['admin', 'manager', 'staff'] } },
      { path: 'about', loadComponent: () => import('./components/dashboard/navbarDetails/about/about').then(m => m.AboutComponent), data: { roles: ['admin', 'manager', 'staff'] } },
      { path: 'reviews', loadComponent: () => import('./components/dashboard/navbarDetails/reviews/reviews').then(m => m.ReviewsComponent), data: { roles: ['admin', 'manager', 'staff'] } },
      { path: 'location', loadComponent: () => import('./components/dashboard/navbarDetails/location/location').then(m => m.LocationComponent), data: { roles: ['admin', 'manager', 'staff'] } },





      // Footer
      { path: 'footer', loadComponent: () => import('./components/dashboard/footer/footer').then(m => m.FooterComponent), data: { roles: ['admin', 'manager', 'staff'] } }
    ]
  },

  { path: '**', redirectTo: 'login' }
];
