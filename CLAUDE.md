# CLAUDE.md

## Documentation Structure

- Reports (analysis/research): Documentation/reports/ (.txt format, Chinese)
- Rules (project conventions): Documentation/rules/ (.txt format)
- Tasks (code generation tasks): Documentation/tasks/ (.txt format)
- Default logs (mistake records): Documentation/defaults/ (.txt format, Chinese)

### File naming convention
All documentation files use: YYMMDD-{type}-{description}.txt
- type: report, task, or rule
- description: kebab-case, concise
- Examples: 260609-report-action-system.txt, 260609-task-implement-action-queue.txt

### Report format
1. Brief background/introduction
2. Detailed code analysis (with source file paths)
3. Detailed solution overview (all viable options with pros/cons)
4. Brief conclusion

### Report scope
Keep each report small and focused — one report covers ONE topic/direction, not many.
- A report should answer a single question; if it spans multiple concerns, split it into several narrow reports.
- Prefer many small reports over one broad report.
- Do not duplicate content already covered by an existing report. Check Documentation/reports/ first; if a topic is already covered, append a focused section to that report or cross-reference it instead of re-explaining.
- Cross-reference related reports by filename (e.g. 《260611-report-namespace-vs-assembly》) so a narrow report can lean on others rather than absorbing them.
- When a broad "study" (e.g. analyzing another codebase) touches several topics, distribute its findings into the relevant topical reports rather than making one catch-all report.

## Task Management

Task location: kcg-game/b1.tasks/
Task format: YYMMDD.task-NNN.description.pending/
Status: .pending → .active → .done (or .blocked, .obsolete)

### Task status updates
- .pending → .active: When picking a task to work on (reserves it so other devs won't pick it)
- .active → .done: When creating a PR (work is complete, PR is the deliverable)
- If PR is not merged, revert to .pending or .active as appropriate

When creating a PR for a task:
1. Rename folder from .active (or .pending) to .done
2. Update Status line in spec to: done (PR #NNNN) - include PR number or link
3. Include the rename in the same commit as the code changes
4. Verify Status line is updated BEFORE committing (not left as "active" or "pending")

## Design Discussion Rules

### Multi-solution proposal
When suggesting code architecture, design patterns, or implementation approaches, always list ALL viable solutions with pros/cons comparison, so the developer can make an informed choice. Never default to a single approach without presenting alternatives.

### Default logging
When a mistake is identified (misleading explanation, incorrect claim, ambiguous wording that caused confusion), immediately write a default log to Documentation/defaults/ using the standard naming convention (YYMMDD-default-{description}.txt). The log must include:
1. What went wrong (the exact misleading content)
2. Root cause analysis (why it happened)
3. Correction applied (what was fixed)
4. Lesson learned (rule to prevent recurrence)

### Disambiguation in explanations
When explaining a concept that could be confused with a previously discussed or similar concept:
1. Explicitly state what the concept IS for and what it is NOT for
2. Avoid ambiguous metaphors — use precise terminology tied to the specific system/feature
3. If a concept serves System A but not System B, say so upfront, especially when System B is the current topic of discussion

## Coding Rules

### No var
Always use explicit types.

### One class per file
No nested classes/structs/enums. File name = class name.

### Always use functions inside MyAssert.cs to handle errors
Crash on invalid data, never silently skip.


## Git Rules

### Never push to main
All changes through PRs.

### Commit format
- Imperative mood, capital letter, no period, no emoji
- No AI/Claude/Co-Authored-By mentions
- Action verbs: Add, Remove, Refactor, Fix, Update, Implement, Register, Set

### Staging
No git add -A or git add . - use specific file paths.