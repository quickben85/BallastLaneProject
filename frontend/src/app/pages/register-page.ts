import { HttpErrorResponse } from '@angular/common/http';
import { Component, DestroyRef, inject, signal } from '@angular/core';
import { takeUntilDestroyed } from '@angular/core/rxjs-interop';
import { FormBuilder, ReactiveFormsModule, Validators } from '@angular/forms';
import { Router, RouterLink } from '@angular/router';
import { finalize } from 'rxjs';
import { AccountField, readAccountFieldErrors } from '../api/account-field-errors';
import { ACCOUNTS_API } from '../api/accounts-client';

@Component({
  selector: 'app-register-page',
  imports: [ReactiveFormsModule, RouterLink],
  templateUrl: './register-page.html',
})
export class RegisterPage {
  private readonly api = inject(ACCOUNTS_API);
  private readonly router = inject(Router);
  private readonly destroyRef = inject(DestroyRef);

  protected readonly form = inject(FormBuilder).nonNullable.group({
    email: ['', [Validators.required, Validators.email, Validators.maxLength(254)]],
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
    this.api.register(this.form.getRawValue()).pipe(
      takeUntilDestroyed(this.destroyRef),
      finalize(() => this.pending.set(false)),
    ).subscribe({
      next: (account) => {
        void this.router.navigateByUrl('/login', {
          replaceUrl: true,
          state: { registeredEmail: account.email },
        });
      },
      error: (error: unknown) => this.showError(error),
    });
  }

  private showError(error: unknown): void {
    if (error instanceof HttpErrorResponse) {
      if (error.status === 409) {
        this.fieldErrors.set({ email: 'An account already exists for this email address.' });
        return;
      }
      if (error.status === 400) {
        const fields = readAccountFieldErrors(error);
        this.fieldErrors.set(fields);
        if (!fields.email && !fields.password) this.error.set('Check your email and password.');
        return;
      }
    }
    this.error.set('Registration is unavailable right now. Try again.');
  }
}
