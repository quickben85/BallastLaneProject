---
name: implement-use-case
description: Implement and verify one user-visible vertical slice in the Task Manager repository, from acceptance criteria and a failing behavior test through the changed backend layers and frontend.
---

# Implement a use case

1. Read the root and affected directory `AGENTS.md` files and the architecture. Use a dedicated feature branch for the slice and preserve unrelated working-tree changes. Open or add the owning document in `user stories/`. Record observable acceptance criteria, including success, validation, and relevant authorization behavior. Put route, request, response, and error details in that story before editing both sides.
2. Add the smallest behavior test that pins the requested change and run it red. Start with a Domain or Application test when the slice introduces a rule. Use a compiling stub if necessary to reach the intended assertion. Record what failed and why.
3. Carry the slice through the layers it needs. Implement the rule and use case, introduce an Application dependency interface only where needed, add the Infrastructure adapter and migration as applicable, then wire the API endpoint and the UI. Run focused tests as you go and refactor once green.
4. For API changes, test the HTTP contract and relevant persistence or authorization behavior with the real host and isolated PostgreSQL. For UI changes, cover visible behavior in component tests and complete the flow in a running browser against the Docker Compose stack. Use the README commands for affected builds and suites.
5. Once the slice works, spawn one fresh independent reviewer. Give the reviewer the branch, story, acceptance criteria, verification results, and access to all uncommitted feature changes, including new files. Ask for findings on behavior, ownership, dependency direction, and test gaps. The implementer addresses actionable findings in one round and reruns the affected checks, then moves to handoff.
6. Present the uncommitted changes to the user on the feature branch. Report the branch name, verified user flow, red and green test evidence, findings addressed, and any remaining gaps. Commit or push only if explicitly requested.

Use [the create-and-list story](../../../user%20stories/create-and-list-tasks.md) as the reference for acceptance criteria, contracts, and verification.
