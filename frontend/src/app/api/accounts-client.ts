import { InjectionToken } from '@angular/core';
import { Observable } from 'rxjs';
import { LoginRequest } from './login-request';
import { LoginResponse } from './login-response';
import { RegisterRequest } from './register-request';
import { RegisterResponse } from './register-response';

export interface AccountsClient {
  login(credentials: LoginRequest): Observable<LoginResponse>;
  register(credentials: RegisterRequest): Observable<RegisterResponse>;
}

export const ACCOUNTS_API = new InjectionToken<AccountsClient>('Accounts API');
