# CLAUDE.md

## Documentation Structure

- Reports (analysis/research): Documentation/reports/ (.txt format, Chinese)
- Rules (project conventions): Documentation/rules/ (.txt format)
- Tasks (code generation tasks): Documentation/tasks/ (.txt format)

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