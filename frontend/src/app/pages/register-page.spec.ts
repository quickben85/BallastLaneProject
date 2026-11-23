import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { Router } from '@angular/router';
import { RouterTestingHarness } from '@angular/router/testing';
import { appConfig } from '../app.config';

const registerUrl = 'http://localhost:5081/api/accounts/register';
const email = 'new@example.test';
const password = 'NewAccount!2026';

describe('Registration flow', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [...appConfig.providers, provideHttpClientTesting()],
    });
  });

  afterEach(() => TestBed.inject(HttpTestingController).verify());

  it('opens registration from sign-in and returns to sign-in with the new email', async () => {
    const harness = await RouterTestingHarness.create('/login');
    const link = harness.routeNativeElement?.querySelector<HTMLAnchorElement>('a[href="/register"]');
    expect(link).not.toBeNull();
    link!.click();
    await harness.fixture.whenStable();
    expect(harness.routeNativeElement?.querySelector('h1')?.textContent).toBe('Create an account');
    submit(harness);

    const request = TestBed.inject(HttpTestingController).expectOne({ method: 'POST', url: registerUrl });
    expect(request.request.body).toEqual({ email, password });
    expect(request.request.headers.has('Authorization')).toBe(false);
    request.flush({ id: 'account-id', email }, { status: 201, statusText: 'Created' });
    await harness.fixture.whenStable();
    harness.detectChanges();

    expect(TestBed.inject(Router).url).toBe('/login');
    const page = harness.routeNativeElement!;
    expect(page.querySelector('[role="status"]')?.textContent).toContain('Account created');
    expect(page.querySelector<HTMLInputElement>('#email')?.value).toBe(email);
    expect(page.querySelector<HTMLInputElement>('#password')?.value).toBe('');
  });

  it('shows a duplicate email error and keeps the form values', async () => {
    const harness = await RouterTestingHarness.create('/register');
    submit(harness);
    TestBed.inject(HttpTestingController).expectOne(registerUrl).flush(
      {}, { status: 409, statusText: 'Conflict' },
    );
    harness.detectChanges();

    const page = harness.routeNativeElement!;
    expect(page.querySelector('[role="alert"]')?.textContent).toContain('already exists');
    expect(page.querySelector<HTMLInputElement>('#register-email')?.value).toBe(email);
    expect(page.querySelector<HTMLInputElement>('#register-password')?.value).toBe(password);
  });

  it('associates server validation errors with the registration fields', async () => {
    const harness = await RouterTestingHarness.create('/register');
    submit(harness);
    TestBed.inject(HttpTestingController).expectOne(registerUrl).flush(
      { errors: { password: ['Include an uppercase letter.'] } },
      { status: 400, statusText: 'Bad Request' },
    );
    harness.detectChanges();

    const page = harness.routeNativeElement!;
    expect(page.querySelector('#register-password')?.getAttribute('aria-describedby'))
      .toContain('register-password-error');
    expect(page.querySelector('#register-password-error')?.textContent).toBe('Include an uppercase letter.');
  });
});

function submit(harness: RouterTestingHarness): void {
  const page = harness.routeNativeElement!;
  for (const [id, value] of [['register-email', email], ['register-password', password]]) {
    const field = page.querySelector<HTMLInputElement>(`#${id}`)!;
    field.value = value;
    field.dispatchEvent(new Event('input'));
  }
  page.querySelector('form')!.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
}
