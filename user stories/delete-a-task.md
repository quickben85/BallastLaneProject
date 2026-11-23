# Delete a task

As a signed-in user, I can delete a task I own when I no longer need it.

## Acceptance criteria

- Selecting **Delete** on a task opens a confirmation popup with the task title and **Cancel** and **Delete** actions. Cancelling or pressing Escape closes the popup without sending a deletion request.
- Confirming **Delete** sends one request. On success, the task disappears from my list and stays gone after I reload and sign in again.
- If the request fails, the popup shows the error. The task stays in the list after a temporary error. If the API says the task is already gone or inaccessible, the list removes the stale entry.
- Only signed-in users can delete tasks, and each user can delete only their own tasks.

## Tasks API contract

Authenticated `DELETE /api/tasks/{id}` takes a task UUID in the URL and has no request body. It returns `204 No Content` after deleting the caller's task. A missing task or one owned by another user returns `404 Not Found`. Missing or invalid credentials return `401 Unauthorized`.

## Verification

- Run a failing API test for an owner's deletion before adding the route.
- With the real APIs and PostgreSQL, verify that deleted tasks stay gone across fresh requests and that unauthorized or foreign requests leave the task intact.
- In Angular, verify that the initial Delete action and Cancel send no deletion request, and that confirmation sends one owner-authenticated request. Check the responsive popup and reloaded list against the running Compose stack.
