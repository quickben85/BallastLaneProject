import { HttpClient } from '@angular/common/http';
import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { vi } from 'vitest';
import { appConfig } from './app.config';
import { SESSION } from './features/auth/session-state';

describe('Sign-in flow', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [...appConfig.providers, provideHttpClientTesting()],
    });
  });

  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
    TestBed.inject(SESSION).end();
  });

  it('redirects a signed-out visitor from the task page to sign-in', async () => {
    const harness = await RouterTestingHarness.create('/tasks');

    expect(TestBed.inject(Router).url).toBe('/login');
    expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe('Sign in');
  });

  it('signs in through the form and clears the session on sign-out', async () => {
    const harness = await RouterTestingHarness.create('/login');
    submitCredentials(harness);
    const request = TestBed.inject(HttpTestingController).expectOne('http://localhost:5081/api/accounts/login');
    expect(request.request.method).toBe('POST');
    expect(request.request.body).toEqual({ email: 'demo@example.test', password: 'DemoTasks!2026' });
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({ accessToken: 'token', expiresInSeconds: 900 });
    await harness.fixture.whenStable();

    TestBed.inject(HttpTestingController).expectOne('http://localhost:5082/api/tasks').flush([]);
    expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe('Your tasks');
    expect(TestBed.inject(SESSION).token()).toBe('token');
    harness.routeNativeElement?.querySelector('button')?.click();
    await harness.fixture.whenStable();

    expect(TestBed.inject(Router).url).toBe('/login');
    expect(TestBed.inject(SESSION).token()).toBe(null);
  });

  it('announces rejected credentials and preserves the email', async () => {
    const harness = await RouterTestingHarness.create('/login');
    submitCredentials(harness);
    TestBed.inject(HttpTestingController).expectOne('http://localhost:5081/api/accounts/login')
      .flush({}, { status: 401, statusText: 'Unauthorized' });
    harness.detectChanges();

    const page = harness.routeNativeElement!;
    expect(page.querySelector('[role="alert"]')?.textContent).toContain('Invalid email or password.');
    expect(page.querySelector<HTMLInputElement>('#email')?.value).toBe('demo@example.test');
    expect(TestBed.inject(SESSION).token()).toBe(null);
  });

  it('associates API validation messages with the corresponding inputs', async () => {
    const harness = await RouterTestingHarness.create('/login');
    submitCredentials(harness);
    TestBed.inject(HttpTestingController).expectOne('http://localhost:5081/api/accounts/login').flush(
      { errors: { email: ['Check the email.'], password: ['Check the password.'] } },
      { status: 400, statusText: 'Bad Request' },
    );
    harness.detectChanges();

    const page = harness.routeNativeElement!;
    expect(page.querySelector('#email')?.getAttribute('aria-describedby')).toBe('email-error');
    expect(page.querySelector('#password')?.getAttribute('aria-describedby')).toBe('password-error');
    expect(page.querySelector('#email-error')?.textContent).toBe('Check the email.');
    expect(page.querySelector('#password-error')?.textContent).toBe('Check the password.');
  });

  it('only attaches the bearer token to the configured Tasks API', () => {
    const session = TestBed.inject(SESSION);
    const client = TestBed.inject(HttpClient);
    const requests = TestBed.inject(HttpTestingController);
    session.start({ accessToken: 'token', expiresInSeconds: 900 });

    client.get('http://localhost:5082/api/tasks?status=open').subscribe();
    const tasks = requests.expectOne('http://localhost:5082/api/tasks?status=open');
    expect(tasks.request.headers.get('Authorization')).toBe('Bearer token');
    tasks.flush([]);

    client.get('http://localhost:5083/api/tasks').subscribe();
    const other = requests.expectOne('http://localhost:5083/api/tasks');
    expect(other.request.headers.has('Authorization')).toBe(false);
    other.flush([]);
  });

  it('clears the in-memory session when its lifetime elapses', () => {
    vi.useFakeTimers();
    try {
      const session = TestBed.inject(SESSION);
      session.start({ accessToken: 'token', expiresInSeconds: 900 });
      vi.advanceTimersByTime(899_999);
      expect(session.signedIn()).toBe(true);

      vi.advanceTimersByTime(1);
      expect(session.signedIn()).toBe(false);
      expect(session.token()).toBe(null);
    } finally {
      vi.useRealTimers();
    }
  });
});

function submitCredentials(harness: RouterTestingHarness): void {
  const page = harness.routeNativeElement!;
  const email = page.querySelector<HTMLInputElement>('#email')!;
  const password = page.querySelector<HTMLInputElement>('#password')!;
  email.value = 'demo@example.test';
  email.dispatchEvent(new Event('input'));
  password.value = 'DemoTasks!2026';
  password.dispatchEvent(new Event('input'));
  page.querySelector('form')!.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
}
