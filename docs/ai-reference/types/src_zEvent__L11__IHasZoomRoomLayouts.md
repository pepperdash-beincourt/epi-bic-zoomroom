# IHasZoomRoomLayouts

> **Public-source boundary.** This reference is derived only from this public repository’s source declarations and source-local relationships. Do not add customer context, private repository names, deployment topology, credentials, or confidential cross-repository rationale.


| Source fact | Value |
|---|---|
| Source file | [`src/zEvent.cs`](../../../src/zEvent.cs) |
| Declaration | `interface IHasZoomRoomLayouts` with `IHasCodecLayouts` |
| Accessibility | `public` |
| Namespace/module | `PDT.Plugins.Zoom.Room` |
| Declaration line | `11` |
| Generated from | `main` at `865411fe61f040bd21bb55e38dbac7d8a85bfcfa` |

## What it is

`IHasZoomRoomLayouts` is a `interface` whose source-local responsibility is defined by the declaration, its members, and the collaborators listed below. This reference does not infer behavior beyond those source facts.

## Why it exists

The nearby source documentation describes it as: **Defines the requirements for Zoom Room layout control** Its declared base/contract list is `IHasCodecLayouts`. The implementation text directly references **Feedback**.

## How it works

### Declared behavior

No method signatures were extracted from this declaration span; read the linked source file and declaration context.

### Source-local collaborators

- `Feedback`

### Configuration and feedback seams

**Configuration markers**

No configuration marker was extracted from this declaration span.

**Feedback, event, or action markers**

- `feedback declaration or update`

### TypeScript imports

Not applicable to C#.

## Dependencies and verification

### Known source usages

- `src/MobileControlMessenger/ZoomRoomMessenger.cs`
- `src/ZoomRoom.cs`

### Known test files

No tracked test reference was found by the generator search. This does not prove the behavior is untested.

## When and how to change it

This is a compatibility boundary: enumerate every implementation and consumer before changing a member. Validate both the originating control/action and the resulting feedback/status path.

## Source authority

The linked source file at line `11` is authoritative. Regenerate this file after any declaration, source-path, public-member, or source-relationship change; the provenance above must match the branch being reviewed.
