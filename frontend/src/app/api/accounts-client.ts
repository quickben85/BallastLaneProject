import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';
import { LoginRequest } from './login-request';
import { LoginResponse } from './login-response';

export interface AccountsClient {
  login(credentials: LoginRequest): Observable<LoginResponse>;
}

export const ACCOUNTS_API = new InjectionToken<AccountsClient>('Accounts API');
