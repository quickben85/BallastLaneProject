# Frontend instructions

Follow the [repository instructions](../AGENTS.md), the [architecture](../docs/architecture.md), and the [owning user story](../user%20stories/README.md).

- Use Angular CLI and standalone components. Keep page orchestration in `src/app/pages/`, feature components in `src/app/features/`, and UI state near the component. Use signals for local state and an injectable session service for shared auth state. Clear task state when the session ends.
- Declare each class and interface in its own TypeScript file. Use a kebab-case filename that matches the declaration, such as `session.ts` for `Session` and `session-state.ts` for `SessionState`.
- Define interfaces for app-owned injectable services. Bind a typed `InjectionToken<T>` to the implementation in the app configuration and inject the token in consumers. Angular components and framework-provided services use their normal framework types. Call stateless functions directly.
- Keep Angular `HttpClient` services and transport types in `src/app/api/`. Build absolute request URLs using the configured API origins. Attach bearer tokens only to protected requests at the Tasks API origin, parse Problem Details consistently, and clear the expired session on a protected request's `401`.
- Use typed reactive forms with visible labels, keyboard submission, pending feedback, and accessible field errors. Prevent duplicate submissions while a save is pending and preserve values on rejected saves. Keep server validation authoritative.
- Follow the owning story for mutation feedback and data reloads. Render relevant loading, empty, success, and error states.
- Every user-facing page must be responsive. Verify affected flows at mobile, tablet, and desktop widths before handoff. Navigation, forms, and task actions must stay usable without clipped controls or horizontal page scrolling. Check keyboard navigation and visible feedback.
- Use Angular TestBed with the CLI-managed Vitest runner. Test visible behavior and use Angular's HTTP testing utilities to verify request contracts. Run `npm --prefix frontend test -- --watch=false` and `npm --prefix frontend run build` from the repository root.
