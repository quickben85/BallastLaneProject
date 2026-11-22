import { provideHttpClient, withInterceptors } from '@angular/common/http';
import { ApplicationConfig, provideBrowserGlobalErrorListeners } from '@angular/core';
import { provideRouter } from '@angular/router';
import { AccountsApi } from './api/accounts-api';
import { ACCOUNTS_API } from './api/accounts-client';
import { taskAuthInterceptor } from './api/task-auth.interceptor';
import { routes } from './app.routes';
import { Session } from './features/auth/session';
import { SESSION } from './features/auth/session-state';

export const appConfig: ApplicationConfig = {
  providers: [
    provideBrowserGlobalErrorListeners(),
    provideRouter(routes),
    provideHttpClient(withInterceptors([taskAuthInterceptor])),
    { provide: ACCOUNTS_API, useClass: AccountsApi },
    { provide: SESSION, useClass: Session },
  ],
};
