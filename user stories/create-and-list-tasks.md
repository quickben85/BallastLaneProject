# Create a task and see it in my list

As a signed-in user, I can create a private task and see it in my list so I can keep track of my work.

## Acceptance criteria

- The list is empty before the user creates their first task. When they submit a valid task, it is saved under their account, starts open, and appears after the list reloads. The list shows its title, status, and any description and due date.
- Trim the title before applying its 1–120 character limit. The description and due date are optional. A due date has no time of day and must be today or later in the user's calendar.
- Invalid fields show an error without creating a task row. A rejected save keeps the form values. An incomplete date shows a due-date error.
- If the save succeeds and the list reload fails, clear the form, show that the task was saved, and let the user retry loading the list without submitting the task again.
- Anonymous users cannot create or read tasks. Signed-in users can list and fetch only their own tasks.

## Tasks API contract

| Route | Request | Success |
| --- | --- | --- |
| `POST /api/tasks` | JSON with `title`, optional `description`, optional `dueDate` (`YYYY-MM-DD`), and optional `timeZone` | `201 Created` with the task and its item URL in `Location` |
| `GET /api/tasks` | No body | `200 OK` with the caller's tasks sorted by title, then ID |
| `GET /api/tasks/{id}` | Task ID as a UUID in the URL | `200 OK` with the task |

All three routes require a bearer token. The task response contains `id`, `title`, `description`, `status`, and `dueDate`. When absent, `description` and `dueDate` are JSON null. The server assigns the task ID, owner, and initial `open` status. Angular sends the browser's IANA time zone when a date is entered. API clients that omit `timeZone` use UTC.

Invalid JSON or fields return `400` with Problem Details. Field validation errors use the JSON field names. An unsupported content type returns `415`. Missing or invalid credentials return `401`. An item request returns `404` if the task is absent or belongs to another user.

## Form to database and back

1. The user [signs in](sign-in.md) and submits the task form. Angular sends the authenticated create request.
2. The endpoint checks the request and passes the verified token subject and task details to `CreateTask`. The Domain factory uses FluentValidation to enforce the trimmed title length and earliest due date.
3. `CreateTask` calls `ITaskRepository.AddAsync`. `TaskRepository` uses Dapper to write the task to PostgreSQL with the user's ID and stores the optional date in the `due_date` column.
4. After a successful response, Angular clears the form and reloads the list. The repository filters the query by owner, and Angular displays the saved task.

## Verification

| Layer | Behavior verified |
| --- | --- |
| Domain and Application | Run the title tests red before implementing trimming and length validation. Cover date boundaries and verify that an invalid title never reaches the repository. |
| API and database | Sign in through Accounts API, verify the demo account's list starts empty, create a task through Tasks API, follow `Location`, and confirm the owner and due date in PostgreSQL. Verify owner isolation, anonymous access, and invalid input. |
| Angular | Test form submission, input preservation after rejection, incomplete dates, and a list-only retry after a successful save. |
| Live browser | Sign in against Docker Compose, create a task, reload the browser, and sign in again to find the task in the list. Check the layout at mobile, tablet, and desktop widths. |
