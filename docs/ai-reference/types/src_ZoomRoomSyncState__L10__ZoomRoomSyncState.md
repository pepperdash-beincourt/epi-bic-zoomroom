# ZoomRoomSyncState

> **Public-source boundary.** This reference is derived only from this public repository’s source declarations and source-local relationships. Do not add customer context, private repository names, deployment topology, credentials, or confidential cross-repository rationale.


| Source fact | Value |
|---|---|
| Source file | [`src/ZoomRoomSyncState.cs`](../../../src/ZoomRoomSyncState.cs) |
| Declaration | `class ZoomRoomSyncState` with `IKeyed` |
| Accessibility | `public` |
| Namespace/module | `PDT.Plugins.Zoom.Room` |
| Declaration line | `10` |
| Generated from | `main` at `865411fe61f040bd21bb55e38dbac7d8a85bfcfa` |

## What it is

`ZoomRoomSyncState` is a `class` whose source-local responsibility is defined by the declaration, its members, and the collaborators listed below. This reference does not infer behavior beyond those source facts.

## Why it exists

The nearby source documentation describes it as: **Tracks the initial sycnronization state when establishing a new connection** Its declared base/contract list is `IKeyed`. The implementation text directly references **event subscription**.

## How it works

### Declared behavior

- `StartSync`
- `DequeueQueries`
- `AddQueryToQueue`
- `LoginResponseReceived`
- `ReceivedFirstJsonResponse`
- `InitialQueryMessagesSent`
- `LastQueryResponseReceived`
- `CamerasSetUp`
- `CodecDisconnected`
- `CheckSyncStatus`

### Source-local collaborators

- `event subscription`

### Configuration and feedback seams

**Configuration markers**

No configuration marker was extracted from this declaration span.

**Feedback, event, or action markers**

- `event or status subscription`

### TypeScript imports

Not applicable to C#.

## Dependencies and verification

### Known source usages

- `src/ZoomRoom.cs`

### Known test files

No tracked test reference was found by the generator search. This does not prove the behavior is untested.

## When and how to change it

Read the declared source span, all source usages, and relevant tests before modifying behavior. Validate both the originating control/action and the resulting feedback/status path.

## Source authority

The linked source file at line `10` is authoritative. Regenerate this file after any declaration, source-path, public-member, or source-relationship change; the provenance above must match the branch being reviewed.
