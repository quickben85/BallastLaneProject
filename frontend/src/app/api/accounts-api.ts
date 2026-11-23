import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { AccountsClient } from './accounts-client';
import { ACCOUNTS_API_ORIGIN } from './api-origins';
import { LoginRequest } from './login-request';
import { LoginResponse } from './login-response';
import { RegisterRequest } from './register-request';
import { RegisterResponse } from './register-response';

@Injectable()
export class AccountsApi implements AccountsClient {
  private readonly http = inject(HttpClient);

  login(credentials: LoginRequest) {
    return this.http.post<LoginResponse>(`${ACCOUNTS_API_ORIGIN}/api/accounts/login`, credentials);
  }

  register(credentials: RegisterRequest) {
    return this.http.post<RegisterResponse>(`${ACCOUNTS_API_ORIGIN}/api/accounts/register`, credentials);
  }
}
