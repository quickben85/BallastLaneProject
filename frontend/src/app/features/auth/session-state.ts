import { InjectionToken, Signal } from '@angular/core';
import { LoginResponse } from '../../api/login-response';

export interface SessionState {
  readonly token: Signal<string | null>;
  readonly signedIn: Signal<boolean>;
  start(response: LoginResponse): void;
  end(): void;
}

export const SESSION = new InjectionToken<SessionState>('Session');
