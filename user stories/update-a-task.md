# Update a task

As a signed-in user, I can edit my task details and mark a task complete.

## Acceptance criteria

- A valid edit persists and appears in the task list. The user can set the status to open or completed.
- Invalid input leaves the saved task unchanged. The user cannot edit tasks owned by another account.

## Tasks API contract

Authenticated `PUT /api/tasks/{id}` accepts JSON with `title`, optional `description`, and `status` (`open` or `completed`). Reuse the [title rule and task response shape](create-and-list-tasks.md). Success returns `200 OK` with the updated task. Validation failures return `400`, missing or invalid credentials return `401`, and absent or inaccessible tasks return `404`.
