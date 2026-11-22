# Sign in to my account

As a user with an account, I can sign in to view my private tasks.

## Acceptance criteria

- Valid credentials open the task list. Invalid credentials show a generic sign-in error.
- Signing out clears the session and displayed tasks. Reloading the app or returning after token expiry requires sign-in.
- A seeded demo account can sign in through the Angular form. The signed-in task page is the destination for the later create-and-list slice.
- Missing or malformed fields produce field errors. An incorrect password and an unknown email share the same public error. A subsequent attempt with valid credentials can still sign in.
- The API issues a 15-minute bearer token for the Tasks API. The browser holds it in memory, and signed-out users visiting the task page return to sign-in.

## Accounts API contract

Public `POST /api/accounts/login` accepts JSON with `email` and `password`. Valid credentials return `200 OK` with `accessToken` and `expiresInSeconds`. Invalid JSON or fields return `400` with validation details. An unsupported content type returns `415`. Unsuccessful authentication returns a generic `401`.
