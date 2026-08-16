import { Routes } from '@angular/router';
import { DashboardComponent } from './pages/dashboard.component';
import { HistoryComponent } from './pages/history.component';

export const routes: Routes = [
  { path: '', component: DashboardComponent },
  { path: 'history', component: HistoryComponent },
  { path: '**', redirectTo: '' }
];
