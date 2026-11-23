import { inject } from '@angular/core';
import { Router, Routes } from '@angular/router';
import { SESSION } from './features/auth/session-state';
import { LoginPage } from './pages/login-page';
import { RegisterPage } from './pages/register-page';
import { TasksPage } from './pages/tasks-page';

export const routes: Routes = [
  { path: '', pathMatch: 'full', redirectTo: 'tasks' },
  {
    path: 'login',
    component: LoginPage,
    canActivate: [() => !inject(SESSION).signedIn() || inject(Router).parseUrl('/tasks')],
  },
  {
    path: 'register',
    component: RegisterPage,
    canActivate: [() => !inject(SESSION).signedIn() || inject(Router).parseUrl('/tasks')],
  },
  {
    path: 'tasks',
    component: TasksPage,
    canActivate: [() => inject(SESSION).signedIn() || inject(Router).parseUrl('/login')],
  },
  { path: '**', redirectTo: 'tasks' },
];
