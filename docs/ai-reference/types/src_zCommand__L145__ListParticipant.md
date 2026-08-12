# ListParticipant

> **Public-source boundary.** This reference is derived only from this public repository’s source declarations and source-local relationships. Do not add customer context, private repository names, deployment topology, credentials, or confidential cross-repository rationale.


| Source fact | Value |
|---|---|
| Source file | [`src/zCommand.cs`](../../../src/zCommand.cs) |
| Declaration | `class ListParticipant` |
| Accessibility | `public` |
| Namespace/module | `PDT.Plugins.Zoom.Room` |
| Declaration line | `145` |
| Generated from | `main` at `865411fe61f040bd21bb55e38dbac7d8a85bfcfa` |

## What it is

`ListParticipant` is a `class` whose source-local responsibility is defined by the declaration, its members, and the collaborators listed below. This reference does not infer behavior beyond those source facts.

## Why it exists

The nearby source documentation describes it as: **Retuns a boolean value if the participant hand state is raised and is valid (both need to be true)**

## How it works

### Declared behavior

- `SortParticipantListByHandStatus`

### Source-local collaborators

No named collaboration seam was extracted automatically; inspect the linked source and its usage files.

### Configuration and feedback seams

**Configuration markers**

- `serialized JSON metadata`

**Feedback, event, or action markers**

No feedback/action marker was extracted from this declaration span.

### TypeScript imports

Not applicable to C#.

## Dependencies and verification

### Known source usages

- `src/ZoomRoom.cs`
- `src/zStatus.cs`

### Known test files

No tracked test reference was found by the generator search. This does not prove the behavior is untested.

## When and how to change it

Read the declared source span, all source usages, and relevant tests before modifying behavior. Preserve serialized/configuration names unless every configuration producer and consumer is updated deliberately.

## Source authority

The linked source file at line `145` is authoritative. Regenerate this file after any declaration, source-path, public-member, or source-relationship change; the provenance above must match the branch being reviewed.
