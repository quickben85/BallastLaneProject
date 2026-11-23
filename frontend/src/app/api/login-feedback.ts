import { HttpErrorResponse } from '@angular/common/http';
import { AccountField, readAccountFieldErrors } from './account-field-errors';

export interface LoginFeedback {
  message?: string;
  fields?: Partial<Record<AccountField, string>>;
}

export function loginError(error: unknown): LoginFeedback {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 401) return { message: 'Invalid email or password.' };
    if (error.status === 400) {
      const fields = readAccountFieldErrors(error);
      if (fields.email || fields.password) return { fields };
      return { message: 'Check your email and password.' };
    }
  }
  return { message: 'Sign-in is unavailable right now. Try again.' };
}
