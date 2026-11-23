# User stories

Give `$implement-use-case` a short description of the behavior you want and any details you already know. You can also paste a draft using the template below. The owning story records the agreed behavior and HTTP contract before implementation.

## Implemented user stories

- [Register an account](register-an-account.md)
- [Sign in to my account](sign-in.md)
- [Create a task and see it in my list](create-and-list-tasks.md)
- [Update a task](update-a-task.md)
- [Delete a task](delete-a-task.md)

## Use-case template

Fill in what you know. Include the API contract section when the story changes an HTTP endpoint. Identify the resource and choose the method, route, and response codes using the [REST conventions](../backend/AGENTS.md#endpoints-and-use-cases).

```markdown
# [Use-case title]

As a [user or role], I can [action] so that [outcome].

## Acceptance criteria

- [Successful user flow and observable result]
- [Input validation and any relevant failure or recovery behavior]
- [Authorization or ownership rules, if relevant]

## API contract

| Host | Method and route | Access | Request | Success | Errors |
| --- | --- | --- | --- | --- | --- |
| [API] | [Method and path] | [Public or protected] | [Fields] | [Status and response] | [Statuses and error details] |

## Verification

- [First behavior test and expected failure]
- [API, persistence, or authorization checks, when applicable]
- [Browser flow, when applicable]
```
