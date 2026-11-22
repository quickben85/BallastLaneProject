import { Component, effect, inject } from '@angular/core';
import { Router } from '@angular/router';
import { SESSION } from '../features/auth/session-state';

@Component({
  selector: 'app-tasks-page',
  template: `
    <main class="tasks-layout">
      <header class="panel tasks-header">
        <div>
          <p class="product-name">Task Manager</p>
          <h1>Your tasks</h1>
        </div>
        <button type="button" (click)="session.end()">Sign out</button>
      </header>
    </main>
  `,
})
export class TasksPage {
  protected readonly session = inject(SESSION);
  private readonly router = inject(Router);

  constructor() {
    effect(() => {
      if (!this.session.signedIn()) {
        void this.router.navigateByUrl('/login', { replaceUrl: true });
      }
    });
  }
}
