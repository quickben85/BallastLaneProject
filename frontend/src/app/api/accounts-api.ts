import { HttpClient } from '@angular/common/http';
import { inject, Injectable } from '@angular/core';
import { AccountsClient } from './accounts-client';
import { ACCOUNTS_API_ORIGIN } from './api-origins';
import { LoginRequest } from './login-request';
import { LoginResponse } from './login-response';

@Injectable()
export class AccountsApi implements AccountsClient {
  private readonly http = inject(HttpClient);

  login(credentials: LoginRequest) {
    return this.http.post<LoginResponse>(`${ACCOUNTS_API_ORIGIN}/api/accounts/login`, credentials);
  }
}
