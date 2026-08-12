# ZoomRoomCamera

> **Public-source boundary.** This reference is derived only from this public repository’s source declarations and source-local relationships. Do not add customer context, private repository names, deployment topology, credentials, or confidential cross-repository rationale.


| Source fact | Value |
|---|---|
| Source file | [`src/ZoomRoomCamera.cs`](../../../src/ZoomRoomCamera.cs) |
| Declaration | `class ZoomRoomCamera` with `CameraBase, IHasCameraPtzControl, IBridgeAdvanced` |
| Accessibility | `public` |
| Namespace/module | `PDT.Plugins.Zoom.Room` |
| Declaration line | `10` |
| Generated from | `main` at `865411fe61f040bd21bb55e38dbac7d8a85bfcfa` |

## What it is

`ZoomRoomCamera` is a `class` whose source-local responsibility is defined by the declaration, its members, and the collaborators listed below. This reference does not infer behavior beyond those source facts.

## Why it exists

Its declared base/contract list is `CameraBase, IHasCameraPtzControl, IBridgeAdvanced`. It directly participates in the lifecycle through `LinkToApi()`.

## How it works

### Declared behavior

- `PositionHome`
- `PanLeft`
- `PanRight`
- `PanStop`
- `TiltDown`
- `TiltUp`
- `TiltStop`
- `ZoomIn`
- `ZoomOut`
- `ZoomStop`
- `LinkToApi`

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
- `src/ZoomRoomFarEndCamera.cs`

### Known test files

No tracked test reference was found by the generator search. This does not prove the behavior is untested.

## When and how to change it

Read the declared source span, all source usages, and relevant tests before modifying behavior. Preserve lifecycle ordering; do not move hardware-dependent work earlier than communication readiness. Preserve serialized/configuration names unless every configuration producer and consumer is updated deliberately.

## Source authority

The linked source file at line `10` is authoritative. Regenerate this file after any declaration, source-path, public-member, or source-relationship change; the provenance above must match the branch being reviewed.
