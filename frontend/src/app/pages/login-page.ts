import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { ACCOUNTS_API } from '../api/accounts-client';
import { AccountField } from '../api/account-field-errors';
import { loginError } from '../api/login-feedback';
import { SESSION } from '../features/auth/session-state';

@Component({
  selector: 'app-login-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './login-page.html',
})
export class LoginPage {
  private readonly api = inject(ACCOUNTS_API);
  private readonly session = inject(SESSION);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);
  private readonly registeredEmail: unknown = this.router.currentNavigation()?.extras.state?.['registeredEmail'];

  protected readonly accountCreated = typeof this.registeredEmail === 'string';
  protected readonly form = inject(FormBuilder).nonNullable.group({
    email: [typeof this.registeredEmail === 'string' ? this.registeredEmail : '',
      [Validators.required, Validators.email, Validators.maxLength(254)]],
    password: ['', [Validators.required, Validators.maxLength(1024)]],
  });

  protected readonly pending = signal(false);
  protected readonly error = signal('');
  protected readonly fieldErrors = signal<Partial<Record<AccountField, string>>>({});

  protected clearFieldError(field: AccountField): void {
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
