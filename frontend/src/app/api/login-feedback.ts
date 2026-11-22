import { HttpErrorResponse } from '@angular/common/http';
import { LoginRequest } from './login-request';

export type LoginField = keyof LoginRequest;

export interface LoginFeedback {
  message?: string;
  fields?: Partial<Record<LoginField, string>>;
}

export function loginError(error: unknown): LoginFeedback {
  if (error instanceof HttpErrorResponse) {
    if (error.status === 401) return { message: 'Invalid email or password.' };
    if (error.status === 400) {
      const errors = error.error?.errors as Record<string, unknown> | undefined;
      const fields: Partial<Record<LoginField, string>> = {};
      for (const name of ['email', 'password'] as const) {
        const messages = errors?.[name];
        if (Array.isArray(messages) && typeof messages[0] === 'string') {
          fields[name] = messages[0];
        }
      }
      if (fields.email || fields.password) return { fields };
      return { message: 'Check your email and password.' };
    }
  }
  return { message: 'Sign-in is unavailable right now. Try again.' };
}
