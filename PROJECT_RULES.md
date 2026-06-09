# PROJECT_RULES.md

This file defines the project-level engineering rules for code changes in this repository.

## Architecture Direction

- Follow incremental clean architecture.
- Prefer abstraction-first changes when introducing or cleaning cross-layer behavior.
- Keep UI logic out of Views and code-behind when it can live in ViewModels or abstractions.
- Avoid making `Application` depend directly on concrete UI or infrastructure details unless there is no safer short-term path.
- Do not do aggressive large-scale refactors in one pass. First create safety through tests or abstractions, then split or clean incrementally.

## Coding Rules

- Prefer minimal localized patches.
- Change only the files relevant to the task.
- Preserve existing MVVM, CommunityToolkit.Mvvm, and DI patterns unless the task explicitly requires a change.
- Do not leave placeholder code, unused `Class1.cs` files, dead code, or backup artifacts in active project paths.
- Do not hide real issues with blanket suppression if the root cause can be fixed safely.
- Nullable issues should be fixed at the source whenever practical instead of being silently suppressed.
- Never run `git restore` without explicit user approval in the current session.
- After completing any meaningful code change, run `git commit` when this workspace has a `.git` directory. If Git is unavailable, report that checkpointing could not be performed.

## WPF Safety Rules

- Treat startup, XAML parsing, bindings, scrolling, selection, popups, dialogs, and custom controls as high-risk areas.
- Validate both behavior and interaction, not only appearance.
- Do not break initialization order, event flow, or UI state synchronization.
- Be careful with code-behind changes; preserve behavior unless the task explicitly targets that area.
- If a View has specialized behavior, prefer the least invasive fix that keeps the current user workflow intact.

## Testing Strategy

- Domain logic should be protected primarily by unit tests.
- ViewModel behavior should be protected with fake or recording services where appropriate.
- Test orchestration, guards, and state transitions before testing expensive file/export success paths.
- For regressions, add tests that lock the observed failure mode whenever practical.

## Validation Standard

Standard order:

1. Restore if needed.
2. Build the relevant project or projects.
3. Run relevant tests.
4. If the normal test path is blocked by host limitations, use the repo-supported fallback path and state that clearly.

For the WPF application in this repo:

- The canonical manual QA target is `RizaCanKilicIsTakibi\bin\Release\publish\RizaCanKilicIsTakibi.exe`.
- Live publish data under `Data`, `Backup`, and `Logs` must be preserved during publish and cleanup.
- Avoid leaving duplicate `RizaCanKilicIsTakibi.exe` copies in other output folders when they can mislead manual QA.

## Command Guidance

- Prefer validating the relevant `.csproj` directly when solution validation is unreliable in the current host or SDK.
- Use targeted `dotnet test` filters for slice work, then run the full solution test command before delivery.
- Publish Release to a temporary folder first, then copy verified application output into the canonical publish folder only after checking the app process is not running.
- If NuGet restore fails with environment or path-related errors, inspect host environment variables and SDK context before treating the repo as broken.

## Decision Bias

- Choose the least invasive correct solution.
- Prefer maintainability over cleverness.
- Prefer explicitness over hidden behavior.
- Prefer verified behavior over assumptions.
