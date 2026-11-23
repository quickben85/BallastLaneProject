# Update a task

As a signed-in user, I can edit a task I own, mark it complete, or reopen it.

## Acceptance criteria

- I can edit the title, description, and optional due date. I can set the status to open or completed.
- I can keep an existing overdue date or clear it. A replacement date must be today or later in my calendar.
- A successful edit appears in my list. A rejected save leaves the saved task unchanged and keeps my form values for correction.
- When I finish or cancel an edit, the new-task form restores any draft I had entered.
- Only signed-in users can edit tasks, and each user can edit only their own tasks.

## Tasks API contract

Authenticated `PUT /api/tasks/{id}` replaces the task's editable fields. The JSON body contains `title`, optional `description`, optional `dueDate` (`YYYY-MM-DD`), optional `timeZone`, and `status` (`open` or `completed`). Omit or send null for `description` or `dueDate` to clear the field. Reuse the [title and calendar rules](create-and-list-tasks.md), with one exception for updates: the submitted date may equal the task's stored date even if it has passed. Angular supplies the browser's IANA time zone when it sends a due date. If no time zone is supplied, use UTC.

Success returns `200 OK` with the existing task response shape. Invalid input returns `400` with field errors. An unsupported content type returns `415`. Missing or invalid credentials return `401`. An absent task or one owned by another user returns `404`.

## Verification

- Start with a failing behavior test for an attempted update.
- Verify that title, description, date, and status persist, and that rejected or unauthorized requests leave the original row unchanged.
- In Angular and the running Compose app, verify that a user can edit and reopen a task, see the result in the list, and correct a rejected edit.
