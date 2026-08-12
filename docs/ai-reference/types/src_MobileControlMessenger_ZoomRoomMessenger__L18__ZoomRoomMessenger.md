# ZoomRoomMessenger

> **Public-source boundary.** This reference is derived only from this public repository’s source declarations and source-local relationships. Do not add customer context, private repository names, deployment topology, credentials, or confidential cross-repository rationale.


| Source fact | Value |
|---|---|
| Source file | [`src/MobileControlMessenger/ZoomRoomMessenger.cs`](../../../src/MobileControlMessenger/ZoomRoomMessenger.cs) |
| Declaration | `class ZoomRoomMessenger` with `VideoCodecBaseMessenger` |
| Accessibility | `public` |
| Namespace/module | `PepperDash.Essentials.AppServer.Messengers` |
| Declaration line | `18` |
| Generated from | `main` at `865411fe61f040bd21bb55e38dbac7d8a85bfcfa` |

## What it is

`ZoomRoomMessenger` is a `class` whose source-local responsibility is defined by the declaration, its members, and the collaborators listed below. This reference does not infer behavior beyond those source facts.

## Why it exists

Its declared base/contract list is `VideoCodecBaseMessenger`. The implementation text directly references **event subscription**.

## How it works

### Declared behavior

- `SharingInfo_ShareInfoChanged`
- `CodecSchedule_MeetingsListHasChanged`
- `RecordConsentPromptIsVisible_OutputChange`
- `dirCodec_DirectoryResultReturned`
- `PostMeetingInfo`
- `SendFullStatus`

### Source-local collaborators

- `event subscription`

### Configuration and feedback seams

**Configuration markers**

No configuration marker was extracted from this declaration span.

**Feedback, event, or action markers**

- `event or status subscription`
- `Mobile Control action/status handling`

### TypeScript imports

Not applicable to C#.

## Dependencies and verification

### Known source usages

No additional tracked source usage was found by the generator search.

### Known test files

No tracked test reference was found by the generator search. This does not prove the behavior is untested.

## When and how to change it

Read the declared source span, all source usages, and relevant tests before modifying behavior. Validate both the originating control/action and the resulting feedback/status path.

## Source authority

The linked source file at line `18` is authoritative. Regenerate this file after any declaration, source-path, public-member, or source-relationship change; the provenance above must match the branch being reviewed.
