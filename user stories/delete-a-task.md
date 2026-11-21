# Delete a task

As a signed-in user, I can remove a task I no longer need.

## Acceptance criteria

- After a successful deletion, the task disappears from the list and stays gone after reloading.
- The user cannot delete tasks owned by another account.

## Tasks API contract

Authenticated `DELETE /api/tasks/{id}` returns `204 No Content` on success. Missing or invalid credentials return `401`. An absent or inaccessible task returns `404`.
