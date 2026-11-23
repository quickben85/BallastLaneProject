import { HttpTestingController, provideHttpClientTesting } from '@angular/common/http/testing';
import { TestBed } from '@angular/core/testing';
import { RouterTestingHarness } from '@angular/router/testing';
import { appConfig } from '../app.config';
import { SESSION } from '../features/auth/session-state';

const tasksUrl = 'http://localhost:5082/api/tasks';

describe('Task management', () => {
  beforeEach(() => {
    TestBed.configureTestingModule({
      providers: [...appConfig.providers, provideHttpClientTesting()],
    });
    TestBed.inject(SESSION).start({ accessToken: 'token', expiresInSeconds: 900 });
  });

  afterEach(() => {
    TestBed.inject(HttpTestingController).verify();
    TestBed.inject(SESSION).end();
  });

  it('saves the task and retries a failed list reload without saving again', async () => {
    const requests = TestBed.inject(HttpTestingController);
    const page = await RouterTestingHarness.create('/tasks');
    requests.expectOne({ method: 'GET', url: tasksUrl }).flush([]);
    const screen = page.routeNativeElement!;
    const title = screen.querySelector<HTMLInputElement>('#task-title')!;
    const date = screen.querySelector<HTMLInputElement>('#task-due-date')!;
    change(title, 'Prepare handoff');
    change(date, '2099-11-22');
    submit(screen);

    const save = requests.expectOne({ method: 'POST', url: tasksUrl });
    expect(save.request.headers.get('Authorization')).toBe('Bearer token');
    expect(save.request.body).toMatchObject({ title: 'Prepare handoff', dueDate: '2099-11-22' });
    expect(save.request.body.timeZone).toBe(Intl.DateTimeFormat().resolvedOptions().timeZone);
    const item = { id: 'task-1', title: 'Prepare handoff', status: 'open', dueDate: '2099-11-22' };
    save.flush(item, { status: 201, statusText: 'Created' });
    requests.expectOne({ method: 'GET', url: tasksUrl }).flush({}, { status: 500, statusText: 'Error' });
    page.detectChanges();

    expect(title.value).toBe('');
    expect(screen.querySelector('[role="status"]')?.textContent).toContain('Task saved.');
    expect(screen.querySelector('[role="alert"]')?.textContent).toContain('Could not load your tasks.');
    screen.querySelector<HTMLButtonElement>('.retry')!.click();

    requests.expectNone({ method: 'POST', url: tasksUrl });
    requests.expectOne({ method: 'GET', url: tasksUrl }).flush([item]);
    page.detectChanges();
    expect(screen.querySelector('.task-list')?.textContent).toContain('Prepare handoff');
    expect(screen.querySelector('time')?.textContent).toBe('2099-11-22');
  });

  it('preserves input on a rejected save and associates server errors with the field', async () => {
    const requests = TestBed.inject(HttpTestingController);
    const page = await RouterTestingHarness.create('/tasks');
    requests.expectOne({ method: 'GET', url: tasksUrl }).flush([]);
    const screen = page.routeNativeElement!;
    const title = screen.querySelector<HTMLInputElement>('#task-title')!;
    const description = screen.querySelector<HTMLTextAreaElement>('#task-description')!;
    change(title, 'Keep this title');
    change(description, 'Keep this description');
    submit(screen);

    requests.expectOne({ method: 'POST', url: tasksUrl }).flush(
      { errors: { title: ['Check the title.'] } },
      { status: 400, statusText: 'Bad Request' },
    );
    page.detectChanges();

    expect(title.value).toBe('Keep this title');
    expect(description.value).toBe('Keep this description');
    expect(title.getAttribute('aria-describedby')).toBe('task-title-error');
    expect(screen.querySelector('[role="alert"]')?.textContent).toContain('Check the title.');
    requests.expectNone({ method: 'GET', url: tasksUrl });
  });

  it('blocks blank titles and past due dates before sending a save request', async () => {
    const requests = TestBed.inject(HttpTestingController);
    const page = await RouterTestingHarness.create('/tasks');
    requests.expectOne({ method: 'GET', url: tasksUrl }).flush([]);
    const screen = page.routeNativeElement!;
    change(screen.querySelector<HTMLInputElement>('#task-title')!, '   ');
    change(screen.querySelector<HTMLInputElement>('#task-due-date')!, '2000-01-01');
    submit(screen);
    page.detectChanges();

    requests.expectNone({ method: 'POST', url: tasksUrl });
    expect(screen.querySelector('#task-title-error')?.textContent).toContain('Enter a title');
    expect(screen.querySelector('#task-due-date-error')?.textContent).toContain('today or later');
  });

  it('preserves the form and blocks a partially entered date', async () => {
    const requests = TestBed.inject(HttpTestingController);
    const page = await RouterTestingHarness.create('/tasks');
    requests.expectOne({ method: 'GET', url: tasksUrl }).flush([]);
    const screen = page.routeNativeElement!;
    const title = screen.querySelector<HTMLInputElement>('#task-title')!;
    const date = screen.querySelector<HTMLInputElement>('#task-due-date')!;
    change(title, 'Keep the draft');
    Object.defineProperty(date, 'validity', { value: { badInput: true } });
    submit(screen);
    page.detectChanges();

    requests.expectNone({ method: 'POST', url: tasksUrl });
    expect(title.value).toBe('Keep the draft');
    expect(screen.querySelector('#task-due-date-error')?.textContent).toContain('complete date');
  });

  it('edits an existing task and preserves its overdue date when completing it', async () => {
    const requests = TestBed.inject(HttpTestingController);
    const page = await RouterTestingHarness.create('/tasks');
    const original = {
      id: 'task-1', title: 'Original', description: 'Notes', dueDate: '2000-01-01', status: 'open',
    };
    requests.expectOne({ method: 'GET', url: tasksUrl }).flush([original]);
    page.detectChanges();
    const screen = page.routeNativeElement!;
    change(screen.querySelector<HTMLInputElement>('#task-title')!, 'Unfinished new task');
    screen.querySelector<HTMLButtonElement>('.edit-task')!.click();
    page.detectChanges();

    const title = screen.querySelector<HTMLInputElement>('#task-title')!;
    const date = screen.querySelector<HTMLInputElement>('#task-due-date')!;
    expect(title.value).toBe('Original');
    expect(date.value).toBe('2000-01-01');
    change(title, 'Updated task');
    const status = screen.querySelector<HTMLSelectElement>('#task-status')!;
    status.value = 'completed';
    status.dispatchEvent(new Event('change'));
    submit(screen);

    const save = requests.expectOne({ method: 'PUT', url: `${tasksUrl}/task-1` });
    expect(save.request.headers.get('Authorization')).toBe('Bearer token');
    expect(save.request.body).toMatchObject({
      title: 'Updated task', description: 'Notes', status: 'completed', dueDate: '2000-01-01',
    });
    const updated = { ...original, title: 'Updated task', status: 'completed' };
    save.flush(updated);
    requests.expectOne({ method: 'GET', url: tasksUrl }).flush([updated]);
    page.detectChanges();

    expect(screen.querySelector('.task-list')?.textContent).toContain('Updated task');
    expect(screen.querySelector('.task-status')?.textContent).toBe('Completed');
    expect(screen.querySelector('[role="status"]')?.textContent).toBe('Task updated.');
    expect(title.value).toBe('Unfinished new task');
  });

  it('restores an unfinished new-task draft when cancelling an edit', async () => {
    const requests = TestBed.inject(HttpTestingController);
    const page = await RouterTestingHarness.create('/tasks');
    requests.expectOne({ method: 'GET', url: tasksUrl }).flush([
      { id: 'task-1', title: 'Saved title', description: null, dueDate: null, status: 'open' },
    ]);
    page.detectChanges();
    const screen = page.routeNativeElement!;
    const title = screen.querySelector<HTMLInputElement>('#task-title')!;
    const description = screen.querySelector<HTMLTextAreaElement>('#task-description')!;
    const date = screen.querySelector<HTMLInputElement>('#task-due-date')!;
    change(title, 'Unfinished new task');
    change(description, 'Draft notes');
    change(date, '2099-12-01');
    screen.querySelector<HTMLButtonElement>('.edit-task')!.click();
    page.detectChanges();
    expect(title.value).toBe('Saved title');
    screen.querySelector<HTMLButtonElement>('.form-actions .secondary')!.click();
    page.detectChanges();

    expect(title.value).toBe('Unfinished new task');
    expect(description.value).toBe('Draft notes');
    expect(date.value).toBe('2099-12-01');
    requests.expectNone({ method: 'PUT', url: `${tasksUrl}/task-1` });
  });

  it('keeps the edit draft and leaves the task list unchanged when the API rejects an update', async () => {
    const requests = TestBed.inject(HttpTestingController);
    const page = await RouterTestingHarness.create('/tasks');
    requests.expectOne({ method: 'GET', url: tasksUrl }).flush([
      { id: 'task-1', title: 'Saved title', description: null, dueDate: null, status: 'completed' },
    ]);
    page.detectChanges();
    const screen = page.routeNativeElement!;
    screen.querySelector<HTMLButtonElement>('.edit-task')!.click();
    page.detectChanges();
    const title = screen.querySelector<HTMLInputElement>('#task-title')!;
    change(title, 'Keep this edit');
    submit(screen);
    requests.expectOne({ method: 'PUT', url: `${tasksUrl}/task-1` }).flush(
      { errors: { title: ['Check the edit.'] } }, { status: 400, statusText: 'Bad Request' },
    );
    page.detectChanges();

    expect(title.value).toBe('Keep this edit');
    expect(screen.querySelector('.task-list')?.textContent).toContain('Saved title');
    expect(screen.querySelector('#task-title-error')?.textContent).toBe('Check the edit.');
    requests.expectNone({ method: 'GET', url: tasksUrl });
  });

  it('opens the confirmation without deleting and allows Cancel or Escape', async () => {
    const { dialog, page, requests, rowDelete, screen } = await openDeleteConfirmation();
    requests.expectNone({ method: 'DELETE' });
    expect(dialog.textContent).toContain('Task to delete');
    dialog.querySelector<HTMLButtonElement>('.cancel-delete')!.click();
    expect(dialog.open).toBe(false);
    expect(screen.querySelector('.task-list')?.textContent).toContain('Task to delete');

    rowDelete.click();
    await page.fixture.whenStable();
    expect(dialog.open).toBe(true);
    dialog.dispatchEvent(new Event('cancel', { cancelable: true }));
    expect(dialog.open).toBe(false);
    requests.expectNone({ method: 'DELETE' });
  });

  it('deletes only after confirmation and keeps the task gone when the list reloads', async () => {
    const { dialog, page, requests, screen } = await openDeleteConfirmation();
    requests.expectNone({ method: 'DELETE' });
    const confirm = dialog.querySelector<HTMLButtonElement>('.confirm-delete')!;
    confirm.click();
    confirm.click();
    const request = requests.expectOne({ method: 'DELETE', url: `${tasksUrl}/task-to-delete` });
    expect(request.request.headers.get('Authorization')).toBe('Bearer token');
    request.flush(null, { status: 204, statusText: 'No Content' });
    requests.expectOne({ method: 'GET', url: tasksUrl }).flush([]);
    page.detectChanges();

    expect(dialog.open).toBe(false);
    expect(screen.querySelector('.task-list')).toBeNull();
    expect(screen.textContent).toContain('No tasks yet.');
    expect(screen.querySelector('[role="status"]')?.textContent).toBe('Task deleted.');
  });

  it('keeps the task and shows an error when a confirmed deletion fails', async () => {
    const { dialog, page, requests, screen } = await openDeleteConfirmation();
    dialog.querySelector<HTMLButtonElement>('.confirm-delete')!.click();
    requests.expectOne({ method: 'DELETE', url: `${tasksUrl}/task-to-delete` }).flush(
      {}, { status: 500, statusText: 'Server Error' },
    );
    page.detectChanges();

    expect(dialog.open).toBe(true);
    expect(dialog.querySelector('[role="alert"]')?.textContent).toContain('Could not delete the task');
    expect(screen.querySelector('.task-list')?.textContent).toContain('Task to delete');
    requests.expectNone({ method: 'GET', url: tasksUrl });
  });

  it('removes the stale row when a confirmed deletion returns not found', async () => {
    const { dialog, page, requests, screen } = await openDeleteConfirmation();
    const confirm = dialog.querySelector<HTMLButtonElement>('.confirm-delete')!;
    confirm.click();
    requests.expectOne({ method: 'DELETE', url: `${tasksUrl}/task-to-delete` }).flush(
      {}, { status: 404, statusText: 'Not Found' },
    );
    page.detectChanges();

    expect(dialog.open).toBe(true);
    expect(dialog.querySelector('[role="alert"]')?.textContent).toBe('This task is no longer available.');
    expect(screen.querySelector('.task-list')).toBeNull();
    expect(confirm.disabled).toBe(true);
    requests.expectOne({ method: 'GET', url: tasksUrl }).flush([]);
    dialog.querySelector<HTMLButtonElement>('.cancel-delete')!.click();
    expect(dialog.open).toBe(false);
    expect(document.activeElement?.id).toBe('task-list-title');
  });
});

async function openDeleteConfirmation() {
  const requests = TestBed.inject(HttpTestingController);
  const page = await RouterTestingHarness.create('/tasks');
  requests.expectOne({ method: 'GET', url: tasksUrl }).flush([
    { id: 'task-to-delete', title: 'Task to delete', description: null, dueDate: null, status: 'open' },
  ]);
  page.detectChanges();
  const screen = page.routeNativeElement!;
  const dialog = screen.querySelector<HTMLDialogElement>('dialog');
  const rowDelete = screen.querySelector<HTMLButtonElement>('.delete-task');
  expect(dialog).not.toBeNull();
  expect(rowDelete).not.toBeNull();

  // Emulate the two native dialog methods in the DOM test environment.
  dialog!.showModal = () => { dialog!.open = true; };
  dialog!.close = () => { dialog!.open = false; };
  rowDelete!.click();
  await page.fixture.whenStable();
  page.detectChanges();
  expect(dialog!.open).toBe(true);
  return { dialog: dialog!, page, requests, rowDelete: rowDelete!, screen };
}

function change(control: HTMLInputElement | HTMLTextAreaElement, value: string): void {
  control.value = value;
  control.dispatchEvent(new Event('input'));
}

function submit(screen: HTMLElement): void {
  screen.querySelector('form')!.dispatchEvent(new Event('submit', { bubbles: true, cancelable: true }));
}
