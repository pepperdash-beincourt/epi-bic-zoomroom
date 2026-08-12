# LayoutInfoChangedEventArgs

> **Public-source boundary.** This reference is derived only from this public repository’s source declarations and source-local relationships. Do not add customer context, private repository names, deployment topology, credentials, or confidential cross-repository rationale.


| Source fact | Value |
|---|---|
| Source file | [`src/zEvent.cs`](../../../src/zEvent.cs) |
| Declaration | `class LayoutInfoChangedEventArgs` with `EventArgs` |
| Accessibility | `public` |
| Namespace/module | `PDT.Plugins.Zoom.Room` |
| Declaration line | `30` |
| Generated from | `main` at `865411fe61f040bd21bb55e38dbac7d8a85bfcfa` |

## What it is

`LayoutInfoChangedEventArgs` is a `class` whose source-local responsibility is defined by the declaration, its members, and the collaborators listed below. This reference does not infer behavior beyond those source facts.

## Why it exists

The nearby source documentation describes it as: **Defines the requirements for Zoom Room layout control** Its declared base/contract list is `EventArgs`. The implementation text directly references **event subscription**.

## How it works

### Declared behavior

No method signatures were extracted from this declaration span; read the linked source file and declaration context.

### Source-local collaborators

- `event subscription`

### Configuration and feedback seams

**Configuration markers**

- `serialized JSON metadata`

**Feedback, event, or action markers**

- `event or status subscription`

### TypeScript imports

Not applicable to C#.

## Dependencies and verification

### Known source usages

- `src/MobileControlMessenger/ZoomRoomMessenger.cs`
- `src/ZoomRoom.cs`

### Known test files

No tracked test reference was found by the generator search. This does not prove the behavior is untested.

## When and how to change it

Read the declared source span, all source usages, and relevant tests before modifying behavior. Preserve serialized/configuration names unless every configuration producer and consumer is updated deliberately. Validate both the originating control/action and the resulting feedback/status path.

## Source authority

The linked source file at line `30` is authoritative. Regenerate this file after any declaration, source-path, public-member, or source-relationship change; the provenance above must match the branch being reviewed.
