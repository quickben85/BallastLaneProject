import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, effect, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { AbstractControl, FormBuilder, ReactiveFormsModule } from '@angular/forms';
import { Router } from '@angular/router';
import { catchError, finalize, of, Subject, switchMap } from 'rxjs';
import { TaskSummary } from '../api/task-summary';
import { TASKS_API } from '../api/tasks-client';
import { SESSION } from '../features/auth/session-state';

@Component({
  selector: 'app-tasks-page',
  imports: [ReactiveFormsModule],
  templateUrl: './tasks-page.html',
  styleUrl: './tasks-page.css',
})
export class TasksPage {
  protected readonly session = inject(SESSION);
  private readonly router = inject(Router);
  private readonly api = inject(TASKS_API);
  private readonly destroyRef = inject(DestroyRef);
  private readonly reloadRequests = new Subject<void>();

  protected readonly tasks = signal<TaskSummary[]>([]);
  protected readonly listLoading = signal(false);
  protected readonly listError = signal(false);
  protected readonly saving = signal(false);
  protected readonly saveError = signal('');
  protected readonly saveMessage = signal('');

  protected readonly form = inject(FormBuilder).nonNullable.group({
    title: ['', (control: AbstractControl<string>) => {
      const length = control.value.trim().length;
      return length >= 1 && length <= 120 ? null : { titleLength: true };
    }],
    description: [''],
    dueDate: ['', (control: AbstractControl<string>) =>
      control.value && control.value < this.today() ? { pastDate: true } : null],
  });

  constructor() {
    effect(() => {
      if (!this.session.signedIn()) {
        void this.router.navigateByUrl('/login', { replaceUrl: true });
      }
    });

    this.reloadRequests.pipe(
      switchMap(() => {
        this.listLoading.set(true);
        this.listError.set(false);
        return this.api.list().pipe(catchError(() => of(null)));
      }),
      takeUntilDestroyed(this.destroyRef),
    ).subscribe((tasks) => {
      this.listLoading.set(false);
      this.listError.set(tasks === null);
      if (tasks !== null) this.tasks.set(tasks);
    });

    this.reload();
  }

  protected today(): string {
    const now = new Date();
    const month = String(now.getMonth() + 1).padStart(2, '0');
    const day = String(now.getDate()).padStart(2, '0');
    return `${now.getFullYear()}-${month}-${day}`;
  }

  protected reload(): void {
    this.reloadRequests.next();
  }

  protected save(dueDateInput: HTMLInputElement): void {
    if (this.saving()) return;

    this.saveError.set('');
    this.saveMessage.set('');
    for (const control of Object.values(this.form.controls)) {
      control.updateValueAndValidity();
    }
    if (dueDateInput.validity.badInput) {
      this.form.controls.dueDate.setErrors({ incompleteDate: true });
    }
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    const { title, description, dueDate } = this.form.getRawValue();
    this.saving.set(true);
    this.api.create({
      title,
      description: description.trim() ? description : undefined,
      dueDate: dueDate || undefined,
      timeZone: dueDate ? Intl.DateTimeFormat().resolvedOptions().timeZone : undefined,
    }).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => this.saving.set(false)),
    ).subscribe({
      next: () => {
        this.form.reset();
        this.saveMessage.set('Task saved.');
        this.reload();
      },
      error: (error: unknown) => this.showSaveError(error),
    });
  }

  protected fieldError(name: keyof typeof this.form.controls): string | null {
    const control = this.form.controls[name];
    if (!control.touched || !control.invalid) return null;
    if (control.hasError('incompleteDate')) return 'Enter a complete date or leave it blank.';

    return control.getError('server') ?? (name === 'title'
      ? 'Enter a title of 1–120 characters.'
      : 'Enter a due date of today or later.');
  }

  private showSaveError(error: unknown): void {
    if (error instanceof HttpErrorResponse && error.status === 400) {
      const errors = error.error?.errors;
      let hasFieldError = false;
      for (const name of ['title', 'description', 'dueDate'] as const) {
        const message = errors?.[name]?.[0];
        if (typeof message === 'string') {
          const control = this.form.controls[name];
          control.setErrors({ server: message });
          control.markAsTouched();
          hasFieldError = true;
        }
      }
      if (hasFieldError) return;
    }

    this.saveError.set('Could not save the task. Try again.');
  }
}
