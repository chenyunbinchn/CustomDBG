---
name: docs-and-tasks
description: Conventions for writing files under Documentation/ in this repo — reports, rules, tasks, and default logs. Covers where each file type lives, the YYMMDD-{type}-{description}.txt naming convention, the required report structure and revision-log block, report scope rules, and task Status values and transitions. Load before creating or editing any file under Documentation/.
---

# Documentation & task conventions

## Where things live

- Reports (analysis/research): `Documentation/reports/` (.txt format, Chinese)
- Rules (project conventions): `Documentation/rules/` (.txt format)
- Tasks (code generation tasks): `Documentation/tasks/` (.txt format)
- Default logs (mistake records): `Documentation/defaults/` (.txt format, Chinese)

## File naming convention

All documentation files use: `YYMMDD-{type}-{description}.txt`

- type: report, task, or rule
- description: kebab-case, concise
- Examples: `260609-report-action-system.txt`, `260609-task-implement-action-queue.txt`

## Report format

1. Brief background/introduction
2. Detailed code analysis (with source file paths)
3. Detailed solution overview (all viable options with pros/cons)
4. Brief conclusion

The 《Evidence requirement》 in CLAUDE.md governs what a report may assert — it stays in
CLAUDE.md deliberately, because it applies whether or not this skill is loaded.

## Report revision log

Every report carries a revision-log block right under the title. On every change
(create or edit), add one line: `YYYY-MM-DD <one-sentence description of the change>`.

- First creation logs the initial draft; later edits append a new dated line.
- Keep each line to a single sentence summarizing what changed.

## Report scope

Keep each report small and focused — one report covers ONE topic/direction, not many.

- A report should answer a single question; if it spans multiple concerns, split it into several narrow reports.
- Prefer many small reports over one broad report.
- Do not duplicate content already covered by an existing report. Check `Documentation/reports/` first; if a topic is already covered, append a focused section to that report or cross-reference it instead of re-explaining.
- Cross-reference related reports by filename (e.g. 《260611-report-namespace-vs-assembly》) so a narrow report can lean on others rather than absorbing them.
- When a broad "study" (e.g. analyzing another codebase) touches several topics, distribute its findings into the relevant topical reports rather than making one catch-all report.

## Task management

Task location: `Documentation/tasks/`.
Task format: `YYMMDD-task-{description}.txt` — a plain .txt like a report, with a
revision-log block and a Status line in the title block.
Status values: pending / active / done (or blocked / obsolete).

### Task status

- Status lives in the Status line inside the task file's title block, NOT in the
  filename — this project has no .pending/.active/.done folders.
- pending → active: when you pick a task to work on.
- active → done: when the work is delivered (e.g. a PR is opened, or the change lands);
  if a PR is opened, put its number/link on the Status line.
- If the work is reverted / unmerged, set Status back to pending or active as appropriate.
- Update the Status line (and append a revision-log line) in the same commit as the code change.
