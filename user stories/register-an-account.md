# Register an account

As a new user, I can register with my email and password, then sign in to manage my private tasks.

## Acceptance criteria

- I can open the registration form from sign-in, enter my email and password, and create an account. On success I return to sign-in and see confirmation that the account was created.
- The email is trimmed, checked against the address format used by the sign-in form, and compared case-insensitively. A duplicate email shows an error and leaves the original account intact.
- Invalid input shows field errors. Registration uses the existing Identity password policy, and a rejected registration leaves the form available for correction.
- I can sign in with the new credentials through the existing sign-in flow. My task list starts empty.

## Accounts API contract

Public `POST /api/accounts/register` accepts a JSON object with `email` and `password`. A successful request returns `201 Created` with `id` and `email`. Invalid fields or malformed JSON return `400` with field errors. An unsupported content type returns `415`. An email already in use returns `409 Conflict`.

Accounts stores the email as the Identity username and persists the Identity password hash. The frontend uses the existing sign-in endpoint to establish a session.

## Verification

- Run an API test that expects registration to return `201` before the route exists.
- Check PostgreSQL persistence, case-insensitive duplicate detection, concurrent registration, field errors, sign-in, and the new account's empty task list.
- Verify the registration form, errors, and sign-in handoff in Angular tests. Exercise the workflow in a browser against Docker Compose.
