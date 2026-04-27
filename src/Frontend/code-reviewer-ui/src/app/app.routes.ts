import { Routes } from '@angular/router';

export const routes: Routes = [
  {
    path: '',
    loadComponent: () =>
      import('./features/review-list/review-list.component').then(m => m.ReviewListComponent)
  },
  {
    path: 'reviews/:id',
    loadComponent: () =>
      import('./features/review-detail/review-detail.component').then(m => m.ReviewDetailComponent)
  },
  { path: '**', redirectTo: '' }
];
