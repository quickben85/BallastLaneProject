import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router } from '@angular/router';
import { finalize } from 'rxjs';
import { ACCOUNTS_API } from '../api/accounts-client';
import { LoginField, loginError } from '../api/login-feedback';
import { SESSION } from '../features/auth/session-state';

@Component({
  selector: 'app-login-page',
  imports: [ReactiveFormsModule],
  templateUrl: './login-page.html',
})
export class LoginPage {
  private readonly api = inject(ACCOUNTS_API);
  private readonly session = inject(SESSION);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
    password: ['', [Validators.required, Validators.maxLength(1024)]],
  });

  protected readonly pending = signal(false);
  protected readonly error = signal('');
  protected readonly fieldErrors = signal<Partial<Record<LoginField, string>>>({});

  protected clearFieldError(field: LoginField): void {
    this.fieldErrors.update((errors) => ({ ...errors, [field]: undefined }));
  }

  protected submit(): void {
    if (this.pending()) return;

    const email = this.form.controls.email;
    email.setValue(email.value.trim());
    this.form.markAllAsTouched();
    this.error.set('');
    this.fieldErrors.set({});
    if (this.form.invalid) return;

    this.pending.set(true);
    this.api.login(this.form.getRawValue()).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => this.pending.set(false)),
    ).subscribe({
      next: (result) => {
        this.session.start(result);
        void this.router.navigateByUrl('/tasks', { replaceUrl: true });
      },
      error: (error: unknown) => {
        const feedback = loginError(error);
        this.error.set(feedback.message ?? '');
        this.fieldErrors.set(feedback.fields ?? {});
      },
    });
  }
}
