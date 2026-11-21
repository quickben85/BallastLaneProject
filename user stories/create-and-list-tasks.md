# Create a task and see it in my list

As a signed-in user, I can create a task and see it in my list.

## Acceptance criteria

- Given a signed-in user, when they submit a valid title and optional description, the task is persisted under their account, starts open, and appears after the list reloads.
- Trim the title before applying its 1–120 character limit. An invalid title shows a field error and creates no row. A rejected save preserves the values entered.
- If the save succeeds and the list reload fails, clear the form, show that the task was saved, and let the user retry loading the list.
- Anonymous requests cannot create or read tasks. A signed-in user can only fetch and list tasks they own.

## Tasks API contract

| Route | Request | Success |
| --- | --- | --- |
| `POST /api/tasks` | JSON with `title` and optional `description` | `201 Created` with the task and its item URL in `Location` |
| `GET /api/tasks` | Bearer token | `200 OK` with the caller's tasks sorted by title, then ID |
| `GET /api/tasks/{id}` | Bearer token and task ID in the URL | `200 OK` with the task |

All three routes require a bearer token. The task response contains `id`, `title`, `description`, and `status` (`open` or `completed`). The server assigns the task ID, owner, and initial status on creation. Validation failures return `400` Problem Details with field-keyed errors. Missing or invalid credentials return `401`. An item request returns `404` if the task is absent or owned by another user. Implement the item route in this slice so the `Location` URL works.

## Form to database and back

1. The user [signs in](sign-in.md) and submits the task form. Angular sends the authenticated create request.
2. The endpoint passes the validated token subject and input to `CreateTask`. The Domain enforces the title rule and `CreateTask` calls `ITaskRepository.AddAsync`.
3. The Infrastructure adapter writes the row to PostgreSQL through Dapper before the endpoint returns success.
4. Angular clears the form and fetches the list. `ListTasks` queries PostgreSQL by owner and Angular displays the new task.

## Verification

| Layer | Behavior to prove |
| --- | --- |
| Domain unit | Start with a failing test for a title made only of spaces. Cover trimming and the 120-character boundary. |
| API and database | Sign in through Accounts API, create through Tasks API, assert `201`, follow `Location`, and confirm a separate list request and PostgreSQL query both show the saved task. Verify a second account receives `404` for the item and its list excludes it. Verify anonymous access returns `401` and a whitespace-only title returns `400` without writing a row. |
| Angular | Use TestBed and the CLI's Vitest runner to submit the form through its visible controls. Verify the task appears after the list reloads and API errors are announced and preserve input. |
| Live browser | Open the app through the Node frontend in Docker Compose. Sign in through Accounts API with the seeded demo account, create a task, reload the browser, sign in again, and verify PostgreSQL still supplies the task through Tasks API. |
