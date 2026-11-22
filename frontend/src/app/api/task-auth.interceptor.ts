import { HttpErrorResponse, HttpInterceptorFn } from '@angular/common/http';
import { inject } from '@angular/core';
import { tap } from 'rxjs';
import { SESSION } from '../features/auth/session-state';
import { TASKS_API_ORIGIN } from './api-origins';

const tasksEndpoint = `${TASKS_API_ORIGIN}/api/tasks`;

export const taskAuthInterceptor: HttpInterceptorFn = (request, next) => {
  const url = request.url.split('?')[0];
  if (url !== tasksEndpoint && !url.startsWith(`${tasksEndpoint}/`)) return next(request);

  const session = inject(SESSION);
  const token = session.token();
  if (!token) return next(request);

  return next(request.clone({ setHeaders: { Authorization: `Bearer ${token}` } })).pipe(
    tap({
      error: (error: unknown) => {
        if (error instanceof HttpErrorResponse && error.status === 401 && session.token() === token) {
          session.end();
        }
      },
    }),
  );
};
