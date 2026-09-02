# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Overview

Unity 6 (6000.4.9f1) 2D deck-building roguelike (Slay-the-Spire-like). **Networked
multiplayer is a hard requirement**, and the topology is decided: **listen server** —
one player hosts and is the single authority; the host process runs the authoritative
simulation plus its own local client, other players are pure clients, and there is no
dedicated server. The host's own input must go through the same command entry point as
remote players' (no host-privileged path that mutates authoritative state directly).
Gameplay mode (co-op / versus) is still TBD; Lua hot-reload is under consideration. All battle state must therefore stay serializable
and deterministic: plain data fields only (no delegates/callbacks stored in state), fixed
iteration order, timing by turn/trigger counts (never `Time.time`), randomness only via
`RandomManager`.
The project is at an early, design-heavy stage: combat is still being designed and
`systems/BattleSystem` is a skeleton. Design *rationale* lives as Chinese reports under
`Documentation/reports/`, but the codebase is small — **read the code directly for what it
does; use reports only for the *why* (decisions, rejected alternatives), and treat report
contents as claims to verify against the code, not as facts** (see 《Grounding &
Verification》). The decompiled STS2 C# at `D:\Decompiled Projects\Slay the Spire 2` is the
reference cited throughout those reports.

## Build, Run & Test

- Built and run through the **Unity Editor 6000.4.9f1** — there are no CLI build
  scripts, no test framework, and no single-test runner set up yet. Do not invent
  build/test commands.
- No `.asmdef` under `Assets/`: all gameplay code compiles into a single
  `Assembly-CSharp`. **Namespace == folder path** (a deliberate choice; see
  《260611-report-namespace-vs-assembly》). New files must match folder to namespace.
- Entry point: `Assets/Scripts/mono/UnityBoostrap.cs` — the one scene MonoBehaviour.
  Card data is loaded as JSON from `StreamingAssets`, never from a ScriptableObject.
- `MyAssert` is `[Conditional("UNITY_EDITOR")]` + `[Conditional("DEVELOPMENT_BUILD")]`:
  the "crash on invalid data" guarantee only holds in editor / development builds;
  **release builds silently skip every assert**.

## Architecture

Read the code for structure — `GameState` (`gameStates/persistant/GameState.cs`) is the
root container built once at startup, and the managers hang off it. What follows is only
the part the code cannot state on its own.

**Three data layers: Authoring → Definition → Instance.** The central pattern (cards
today, mirrored for enemies/items). Authoring types are editor/designer-facing and are
converted to runtime types at load. A Definition is an immutable runtime template, built
once at startup, looked up by Id, and **never mutated during play**. An Instance is the
per-run object that references a Definition and carries the mutable per-entity state.

**Persistent vs transient state** (`gameStates/persistant` vs `gameStates/transient`) —
the key lifecycle split. Persistent (`PlayerState`: Hp, gold, energy limits; the deck in
`CardInstanceManager`; seeds) survives across battles and is the save/replay source.
Transient (`BattleState`, `BattleCardState`: the five card piles, enemies, energy, block,
status, turn) is rebuilt at battle start and discarded at battle end. Litmus test: a value
that must survive the battle is persistent; otherwise it is transient. **Battle code reaches
persistent state only through the combat-actor facade**: `BattlePlayerState` implements
`ICombatActor` and forwards `Hp`/`Id` to its `PlayerInfo`, which is the single storage — it
holds no copy, so there is no second source of truth and no "write back at battle end" step.
Every other persistent field (gold, deck, energy limits) stays off limits mid-battle;
`RandomManager` is the one other sanctioned exception.

**Data-driven effects.** A card's behavior is a `CardEffect[]`, where each `CardEffect`
is a struct `{EffectType, TargetType, StatusType, Value}`. Effects are interpreted by enum
dispatch, not per-card subclasses.

**Typed Id structs.** Every entity is keyed by a `readonly struct` Id (`CardDefinitionId`
wraps a string name, `CardInstanceId` wraps a uint, `EnemyDefinitionId`…) with `IEquatable`
+ operator overloads, used as dictionary keys. Never pass raw strings/ints as identifiers.

**Managers own collections and are the access point.** Lookups go through `Manager.Get(id)`,
which `MyAssert`s on a miss rather than returning null.

**Deterministic randomness (replay- and network-critical).** `SeedManager` derives one
seed per domain from a single `MainSeed`; `RandomManager` holds one `System.Random` + a
call counter per domain. All gameplay randomness **must** go through the matching
`RandomManager.<Domain>NextInt` — never `UnityEngine.Random` or an unseeded `Random` — so
runs stay reproducible for replay and consistent across networked clients.

**`systems/` = stateless logic over state.** Systems (e.g. `BattleSystem`) receive state
objects as parameters and mutate them; they hold no game data themselves ("systems do
logic, state holds data").

## Grounding & Verification

Code is the source of truth; everything else (reports, memory, reference implementations)
is a claim to verify. This codebase is small enough to read end-to-end — prefer reading the
actual `.cs` over reading *about* it. (Complements 《Evidence requirement》 below, which
governs what reports may assert.)

- **Reports are claims, not facts.** The code is the *what*; a report is the *why*. When a
  report disagrees with the code, the code wins — fix the report.
- **A tool returning empty / error / "not found" / 0 hits is WEAK evidence, never proof of
  absence.** Never fill the gap with plausible-looking content. Stop and confirm the fact
  through a second, independent channel before building on it (e.g. `ls` or Python
  `os.listdir` for existence; a different reader for content). Suspicious signals to halt
  on: implausibly small line counts, garbled output, repeated Read/Grep failures.
- **Decompiled STS2 source is UTF-8-with-BOM / CRLF / very long lines.** Search tools give
  false 0-hits on it — read it with Python (`open(p, encoding='utf-8-sig')`) and treat a
  Grep "no match" there as unverified, not as "absent".
- **When unsure, say so.** Flag uncertainty explicitly; never present a guess as a finding.

## Documentation & Tasks

Documentation lives under `Documentation/` (reports, rules, tasks, default logs).
**Before creating or editing any file under `Documentation/`, load the `docs-and-tasks`
skill** — it carries the directory layout, the `YYMMDD-{type}-{description}.txt` naming
convention, the report structure and revision-log rules, report scope rules, and task
Status values. The evidence rule below stays here because it applies always, not only
when that skill is loaded.

### Evidence requirement (no memory-based claims)
Reports MUST be grounded in actual source code, read and cited (file path +
line numbers), not in memory or recollection of how a system "usually" works.
- Before claiming "reference implementation X does Y", open X's source and confirm.
- Distinguish "this is X's actual behavior (cited)" from "this is a candidate
  design for this project" — never present the latter as the former.
- If the source is unavailable locally (e.g. a different version), say so
  explicitly and mark the statement as unverified rather than asserting it.
- Exception: only when the user explicitly says to answer from memory/general
  knowledge may a report rely on unverified recollection.

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

## Language & Response Quality

Communicate with specificity, exactness, and conciseness. Every sentence must be
self-contained and parseable with no external context. Ban ungrounded/vague language
and undefined referents ("it", "this", a bare "objects"). This section complements
《Disambiguation in explanations》 above — that rule separates *similar concepts*;
this one makes *every claim* concrete.

### Name things by type
- State the type of every referent: `CardInstanceId refs`, not "refs"; "76 `CardInstance`
  instances", not "76 objects". Say what is being counted (instances in memory, files on
  disk, list elements), never bare "objects".
- Field-level issues use `Type.field` (arrays: `Type.array[*].field`): "`Effect.Value`
  is negative", not "Effect is wrong"; "`BattlePlayerState.DrawPile` is null", not
  "BattlePlayerState is broken".
- Identify entities by name/path + Id, never the raw Id alone: "`CardInstance` (Id: 42,
  Definition: 'Strike')", not "instance 42".

### Quantify with context
- All aggregate counts use "N of M": "3 of 5 card piles are empty", not "3 piles affected".
- Break totals down by type: "12 dangling refs (DrawPile: 8, DiscardPile: 4)", not "12 refs".
- Replace "affected"/"errors"/"broken" with a specific verb phrase + the failing field:
  "`DamageAction.Value` is uninitialized (defaults to 0)", not "DamageAction has issues".

### Describe structures one concept per line
- "`<Type>` is a struct/class/enum."
- "`<Type>` has field `<FieldType> <fieldName>` (purpose)." — one field per line.
- "Function `<Name>(<ParamType> p)` returns `<ReturnType>`; it looks up X using Y."
- No compressed shorthand ("maps to", "relates to", "A ↔ B") without stating the
  mechanism and the types involved.

### Paths in prose
- Reference code as `Assets/...` path relative to the repo root, with `:line` appended
  (e.g. `Assets/Scripts/action/ActionQueue.cs:21`). Never absolute, never "…"-abbreviated.
- Tools (Read/Edit/Grep) still take absolute paths internally; convert only for display.

### Plain Chinese (中文回复用朴实语言)
When replying in 中文, use plain, everyday wording — not stiff translationese or abstract
jargon. Banned examples the user called out: "宿主", "地基缺口", "行为分发", "设计基线",
"收口" (and similar: "心智模型", "正交", "一等公民", "护栏"). Use concrete plain phrasing
instead (e.g. "挂在玩家/怪物/卡牌上" not "宿主"; "总结" not "收口"; "决定调用哪段逻辑"
not "行为分发"). Technical terms and code identifiers (GameAction, IHookListener, …) keep
their original form.

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