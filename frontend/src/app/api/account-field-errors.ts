import { HttpErrorResponse } from '@angular/common/http';

export type AccountField = 'email' | 'password';

export function readAccountFieldErrors(error: HttpErrorResponse): Partial<Record<AccountField, string>> {
  const errors = error.error?.errors as Record<string, unknown> | undefined;
  const fields: Partial<Record<AccountField, string>> = {};
  for (const name of ['email', 'password'] as const) {
    const messages = errors?.[name];
    if (Array.isArray(messages) && typeof messages[0] === 'string') {
      fields[name] = messages[0];
    }
  }
  return fields;
}
