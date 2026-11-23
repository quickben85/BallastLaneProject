---
name: implement-use-case
description: Turn a user-supplied use-case description and details into one verified Task Manager vertical slice. Refine the story and confirm uncertain technical decisions before implementing it.
---

# Implement a use case

## Intake and refinement

1. Take the use-case description and any supplied details from the invocation. Accept free text or the [use-case template](../../../user%20stories/README.md#use-case-template). Read the repository and affected directory `AGENTS.md` files, the architecture, and relevant code and tests. Use [the implemented sign-in story](../../../user%20stories/sign-in.md) as a reference for structure and level of detail.
2. Ask the user to refine missing or ambiguous requirements when the answer could change the observable outcome, acceptance criteria, or scope. Ask focused questions about the actor, goal, rules, errors, or permissions that need clarification. Collect related questions together and wait for the answers before implementing the affected behavior.
3. Use technical details the user supplied and check them against existing contracts. For each material technical detail left open, such as the API host, HTTP method, endpoint, payload, or authorization rule, propose a value on your own only when confidence is **above 80%** based on repository evidence and the story. Record the assumption. At **80% or below**, ask the user to confirm the proposed detail and wait before finalizing the dependent contract or implementing it. Ask about conflicts with an existing contract that would change its behavior.
4. Use a dedicated feature branch and preserve unrelated working-tree changes. Create or revise the owning story in `user stories/` using the template. Record observable acceptance criteria and the agreed API contract, when applicable, before editing either side of that contract.

## Implement and verify

1. Add the smallest behavior test that pins the requested change and run it red. Start with a Domain or Application test when the slice introduces a rule. Use a compiling stub if necessary to reach the intended assertion. Record what failed and why.
2. Carry the slice through the layers it needs. Implement the rule and use case, introduce an Application dependency interface only where needed, add the Infrastructure adapter and migration as applicable, then wire the API endpoint and the UI. Run focused tests as you go and refactor once green.
3. For API changes, test the HTTP contract and relevant persistence or authorization behavior with the real host and isolated PostgreSQL. For UI changes, cover visible behavior in component tests and complete the flow in a running browser against the Docker Compose stack. Use the README commands for affected builds and suites.
4. Once the slice works, spawn one fresh independent reviewer. Give the reviewer the branch, story, acceptance criteria, verification results, and access to all uncommitted feature changes, including new files. Ask for findings on behavior, ownership, dependency direction, and test gaps. The implementer addresses actionable findings in one round and reruns the affected checks.
5. Present the uncommitted changes to the user on the feature branch. Report the branch name, verified user flow, red and green test evidence, findings addressed, and any remaining gaps. Commit or push only if explicitly requested.
