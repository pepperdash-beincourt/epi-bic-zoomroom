# NotifiableObject

> **Public-source boundary.** This reference is derived only from this public repository’s source declarations and source-local relationships. Do not add customer context, private repository names, deployment topology, credentials, or confidential cross-repository rationale.


| Source fact | Value |
|---|---|
| Source file | [`src/NotifiableObject.cs`](../../../src/NotifiableObject.cs) |
| Declaration | `class NotifiableObject` with `INotifyPropertyChanged` |
| Accessibility | `public` |
| Namespace/module | `PDT.Plugins.Zoom.Room` |
| Declaration line | `6` |
| Generated from | `main` at `865411fe61f040bd21bb55e38dbac7d8a85bfcfa` |

## What it is

`NotifiableObject` is a `class` whose source-local responsibility is defined by the declaration, its members, and the collaborators listed below. This reference does not infer behavior beyond those source facts.

## Why it exists

Its declared base/contract list is `INotifyPropertyChanged`.

## How it works

### Declared behavior

- `NotifyPropertyChanged`

### Source-local collaborators

No named collaboration seam was extracted automatically; inspect the linked source and its usage files.

### Configuration and feedback seams

**Configuration markers**

No configuration marker was extracted from this declaration span.

**Feedback, event, or action markers**

No feedback/action marker was extracted from this declaration span.

### TypeScript imports

Not applicable to C#.

## Dependencies and verification

### Known source usages

- `src/zConfiguration.cs`
- `src/zEvent.cs`
- `src/zStatus.cs`

### Known test files

No tracked test reference was found by the generator search. This does not prove the behavior is untested.

## When and how to change it

Read the declared source span, all source usages, and relevant tests before modifying behavior.

## Source authority

The linked source file at line `6` is authoritative. Regenerate this file after any declaration, source-path, public-member, or source-relationship change; the provenance above must match the branch being reviewed.
