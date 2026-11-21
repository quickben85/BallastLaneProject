# Register an account

As a new user, I can create an account and then sign in.

## Acceptance criteria

- Valid registration creates an account that can sign in and starts with an empty task list.
- Invalid input shows field errors. A duplicate email shows a registration error and does not create another account.

## Accounts API contract

Public `POST /api/accounts/register` accepts JSON with `email` and `password`. Successful registration returns `201 Created` with the account's `id` and `email`. Invalid input returns `400` with validation details and a duplicate email returns `409`.
