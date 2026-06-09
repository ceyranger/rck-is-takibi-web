# AGENTS.md

This file defines mandatory working rules for any AI or coding agent operating in this repository.

## Purpose

- Preserve repo stability while making progress.
- Keep changes small, reviewable, and verifiable.
- Enforce a consistent workflow across sessions and agents.

## Start Here

Before making code changes:

1. **FIRST**: Read `/memories/repo/critical-notes.md` for active blockers and current status
2. Read `tasks/todo.md`.
3. Read `tasks/lessons.md`.
4. Read any relevant session notes, reports, or prompts related to the task.
5. Analyze the task before editing code.
6. For any non-trivial task, create a plan first and do not jump directly into implementation.

**Important**: Update `/memories/repo/critical-notes.md` when:
- A new blocker is discovered
- A task is completed or blocked
- Host/environment issues prevent progress
- Critical findings affect other sessions

Non-trivial means work with multiple steps, architectural choices, risky WPF/UI behavior, validation changes, or anything that can affect startup, rendering, state flow, or tests.

## Required Workflow

For non-trivial work:

1. Write the task as a checklist in `tasks/todo.md`.
2. Execute the work in small, incremental steps.
3. Mark completed checklist items as you progress.
4. Add a short review/result note to `tasks/todo.md` when the task is complete.
5. **Update `/memories/repo/critical-notes.md`** if blockers, findings, or status changes occur.

If the user corrects the agent, or if a repeated mistake or new durable lesson is discovered:

1. Update `tasks/lessons.md`.
2. Update `/memories/repo/critical-notes.md` if the issue affects future sessions.
3. Add a practical rule that prevents the same mistake from happening again.

## Change Safety

- Prefer minimal, localized, reversible patches.
- Do not rewrite large files unless it is clearly necessary.
- Do not change startup flow, DI wiring, WPF initialization, or entry points unless the task requires it.
- Do not change Designer files, generated files, or control/event names unless there is a concrete need.
- Do not treat host or environment failures as repo code failures without evidence.
- Never run `git restore` (file-level or repo-wide) unless the user explicitly gives permission in the current conversation.
- After completing any meaningful code change, run `git commit` when this workspace has a `.git` directory. If Git is unavailable, report that checkpointing could not be performed.

## Validation Rules

Never mark work as complete without proof.

- Run the relevant build command before closing the task.
- Run relevant tests when they exist.
- If the host blocks the standard test path, use the repo-supported fallback test path when available.
- Do not claim "should work" without verification output.
- For application changes in this repo, the user’s manual verification target is the Release publish executable at `RizaCanKilicIsTakibi\bin\Release\publish\RizaCanKilicIsTakibi.exe`; ensure the final verified build lands there when it is safe to update it.
- Live data beside the publish executable must be preserved: never delete or overwrite `Data`, `Backup`, or `Logs` while publishing or cleaning build outputs.
- Do not leave a second app executable in test or fallback output folders that could be mistaken for the latest application build.

Validation standard for this repo:

1. Restore if needed.
2. Build the relevant project or solution with `dotnet build RizaCanKilicIsTakibi.sln`.
3. Run targeted tests first when the change is narrow, then run `dotnet test RizaCanKilicIsTakibi.sln` before final delivery.
4. Publish Release to a temporary folder first; update `bin\Release\publish` only after confirming the daily-use executable is not running.
5. After publish, verify the canonical exe path and confirm live data file timestamps/sizes did not unexpectedly change.

## Repo-Specific Notes

- `tasks/todo.md` and `tasks/lessons.md` are part of the required workflow, not optional notes.
- WPF startup, XAML parse safety, binding behavior, popup/dialog behavior, scrolling, selection, and rendering regressions are high-risk areas.
- When solution validation is unreliable for the active SDK/host, validate through the relevant `.csproj` files instead.
- When NuGet or path-related host errors occur, verify environment variables and host conditions before assuming the project is broken.
- If the user says they verify behavior by launching the Release publish exe, align build/output decisions to that workflow and keep `bin\Release\publish\RizaCanKilicIsTakibi.exe` canonical.
- Build cleanup may remove generated `bin`/`obj` artifacts, but must skip the live publish data directories: `Data`, `Backup`, and `Logs`.
- The app stores user data under the executable directory through `PathService`; any change to publish location, single-file behavior, or path resolution is high risk and must be validated with data-preservation checks.

## Reporting Expectations

At the end of the task, report:

- Modified files
- What changed
- What validation was run
- Validation results
- Any remaining risk or host limitation

Be explicit when a limitation comes from the environment rather than the repository.
