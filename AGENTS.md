# Repository instructions

This repository is a .NET full-stack technical interview project. Read [the architecture](docs/architecture.md) before changing a cross-layer contract. Keep acceptance criteria and endpoint contracts in the [owning user story](user%20stories/README.md).

- Before editing `backend/`, read [backend/AGENTS.md](backend/AGENTS.md). Before editing `frontend/`, read [frontend/AGENTS.md](frontend/AGENTS.md). Read both for a vertical slice.
- Use [implement-use-case](.agents/skills/implement-use-case/SKILL.md) for a user-visible slice. Work on a dedicated feature branch with one Codex implementer responsible for the complete slice. Keep frontend, backend, and coordination with the implementer until there is enough parallel work and an agreed API contract.
- Once the slice works, spawn one fresh independent reviewer to inspect the complete working-tree changes, including new files. The implementer addresses actionable findings in a single fix round, reruns affected checks, and reports any remaining findings at handoff.
- Use test-driven development (TDD) for behavior changes. Write a focused test first and run it to observe the intended failure. Implement the minimum behavior to pass, then refactor with the relevant tests green. A missing dependency or broken build needs to be resolved before it can demonstrate a behavioral failure.
- Preserve the dependency direction. Domain contains task rules. Application contains use cases and defines external dependency interfaces. Infrastructure provides persistence and Identity adapters. API hosts translate HTTP to application calls. The frontend consumes HTTP contracts.
- Run relevant targeted checks during development and all affected build and test commands from the [README](README.md) before handing off. For cross-stack slices, also exercise the flow in the running app with its real APIs and database. Report commands, outcomes, and any checks that could not run.
- Keep setup and demo instructions aligned with executable commands as the scaffold grows. Keep generated build output, local database configuration, and signing material out of version control.
- Present the feature for user review with its changes uncommitted on the feature branch. Include the branch name, verification results, and review outcome. Commit or push only when the user explicitly asks for that action.
