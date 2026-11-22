import { computed, Injectable, signal } from '@angular/core';
import { LoginResponse } from '../../api/login-response';
import { SessionState } from './session-state';

@Injectable()
export class Session implements SessionState {
  private readonly accessToken = signal<string | null>(null);
  private expiryTimer?: ReturnType<typeof setTimeout>;
  readonly token = this.accessToken.asReadonly();
  readonly signedIn = computed(() => this.token() !== null);

  start(response: LoginResponse): void {
    this.end();
    this.accessToken.set(response.accessToken);
    this.expiryTimer = setTimeout(() => this.end(), response.expiresInSeconds * 1000);
  }

  end(): void {
    clearTimeout(this.expiryTimer);
    this.expiryTimer = undefined;
    this.accessToken.set(null);
  }
}
