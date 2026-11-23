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
  protected readonly editingTask = signal<TaskSummary | null>(null);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    title: ['', (control: AbstractControl<string>) => {
      const length = control.value.trim().length;
      return length >= 1 && length <= 120 ? null : { titleLength: true };
    }],
    description: [''],
    dueDate: ['', (control: AbstractControl<string>) =>
      control.value && control.value !== this.editingTask()?.dueDate && control.value < this.today()
        ? { pastDate: true } : null],
    status: ['open' as TaskSummary['status']],
  });
  private createDraft = this.form.getRawValue();

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

  protected edit(task: TaskSummary, titleInput: HTMLInputElement, dueDateInput: HTMLInputElement): void {
    if (this.saving() || this.editingTask()) return;
    if (this.markIncompleteDate(dueDateInput)) {
      dueDateInput.focus();
      return;
    }

    this.createDraft = this.form.getRawValue();
    this.editingTask.set(task);
    this.form.reset({
      title: task.title,
      description: task.description ?? '',
      dueDate: task.dueDate ?? '',
      status: task.status,
    });
    this.saveError.set('');
    this.saveMessage.set('');
    titleInput.focus();
  }

  protected cancelEdit(): void {
    if (this.saving()) return;

    this.restoreCreateDraft();
    this.saveError.set('');
  }

  protected save(dueDateInput: HTMLInputElement): void {
    if (this.saving()) return;

    this.saveError.set('');
    this.saveMessage.set('');
    for (const control of Object.values(this.form.controls)) {
      control.updateValueAndValidity();
    }
    this.markIncompleteDate(dueDateInput);
    this.form.markAllAsTouched();
    if (this.form.invalid) return;

    const editing = this.editingTask();
    const { title, description, dueDate, status } = this.form.getRawValue();
    const details = {
      title,
      description: description.trim() ? description : undefined,
      dueDate: dueDate || undefined,
      timeZone: dueDate ? Intl.DateTimeFormat().resolvedOptions().timeZone : undefined,
    };
    this.saving.set(true);
    const request = editing
      ? this.api.update(editing.id, {
        ...details, status, description: details.description ?? null, dueDate: details.dueDate ?? null,
      })
      : this.api.create(details);
    request.pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => this.saving.set(false)),
    ).subscribe({
      next: (saved) => {
        if (editing) {
          this.tasks.update((tasks) => tasks.map((task) => task.id === saved.id ? saved : task));
          this.restoreCreateDraft();
        } else {
          this.form.reset();
        }
        this.saveMessage.set(editing ? 'Task updated.' : 'Task saved.');
        this.reload();
      },
      error: (error: unknown) => this.showSaveError(error),
    });
  }

  protected fieldError(name: keyof typeof this.form.controls): string | null {
    const control = this.form.controls[name];
    if (!control.touched || !control.invalid) return null;
    if (control.hasError('incompleteDate')) return 'Enter a complete date or leave it blank.';

    if (control.hasError('titleLength')) return 'Enter a title of 1–120 characters.';
    if (control.hasError('pastDate')) return 'Enter a due date of today or later.';
    return control.getError('server') ?? null;
  }

  private restoreCreateDraft(): void {
    this.editingTask.set(null);
    this.form.reset(this.createDraft);
  }

  private markIncompleteDate(input: HTMLInputElement): boolean {
    if (!input.validity.badInput) return false;
    this.form.controls.dueDate.setErrors({ incompleteDate: true });
    this.form.controls.dueDate.markAsTouched();
    return true;
  }

  private showSaveError(error: unknown): void {
    if (error instanceof HttpErrorResponse && error.status === 400) {
      const errors = error.error?.errors;
      let hasFieldError = false;
      for (const name of ['title', 'description', 'dueDate', 'status'] as const) {
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

    if (error instanceof HttpErrorResponse && error.status === 404 && this.editingTask()) {
      this.saveError.set('This task is no longer available. Cancel the edit to continue.');
      return;
    }

    this.saveError.set('Could not save the task. Try again.');
  }
}
