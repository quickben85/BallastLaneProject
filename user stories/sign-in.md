# Sign in to my account

As a user with an account, I can sign in to view my private tasks.

## Acceptance criteria

- Valid credentials open the task list. Invalid credentials show a generic sign-in error.
- Signing out clears the session and displayed tasks. Reloading the app or returning after token expiry requires sign-in.

## Accounts API contract

Public `POST /api/accounts/login` accepts JSON with `email` and `password`. Valid credentials return `200 OK` with `accessToken` and `expiresInSeconds`. Invalid input returns `400` with validation details. Unsuccessful authentication returns a generic `401`.
