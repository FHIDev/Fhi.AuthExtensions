import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    pathMatch: 'full',
    title: 'Home — FHI Sample BFF',
    loadComponent: () =>
      import('./home/home.component').then((m) => m.HomeComponent),
  },
  {
    path: 'tokens',
    title: 'User tokens — FHI Sample BFF',
    loadComponent: () =>
      import('./user-token/user-token.component').then(
        (m) => m.UserSessionComponent,
      ),
  },
  {
    path: 'health-records',
    title: 'Health records — FHI Sample BFF',
    loadComponent: () =>
      import('./healthRecords/health-record.component').then(
        (m) => m.HealthRecordComponent,
      ),
  },
];
