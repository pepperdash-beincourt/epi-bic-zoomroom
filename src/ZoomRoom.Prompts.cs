using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using PepperDash.Core.Logging;
using PepperDash.ZoomRoom.Sdk;
using PepperDash.ZoomRoom.Sdk.EventArgs;

namespace PepperDash.Essentials.Plugins
{
	/// <summary>
	/// One dialog the Zoom Room wants answered (or just seen) during a call, normalized for the UI:
	/// title, body, the button texts, and whether an answer goes back to the SDK. The native details
	/// needed to answer it stay on the plugin side ([JsonIgnore]).
	/// </summary>
	public class ZoomPrompt
	{
		[JsonProperty("id")] public int Id { get; set; }

		/// <summary>
		/// audioUnmuteRequest | videoStartRequest | cameraControlRequest | meetingReminder |
		/// customizedReminder | combinedConsent | consent | privacyAlert | inactiveWarning |
		/// messageEvent | boSwitchRequest | boReturnInvite | webinarRoleChanged | roleChanged
		/// </summary>
		[JsonProperty("kind")] public string Kind { get; set; }

		/// <summary>Native sub-type name where one exists (e.g. JoinWebinarAsPanelist, PromotedToPanelist).</summary>
		[JsonProperty("subType")] public string SubType { get; set; }

		[JsonProperty("title")] public string Title { get; set; }
		[JsonProperty("message")] public string Message { get; set; }

		/// <summary>Text for the accept button; empty means no accept button.</summary>
		[JsonProperty("positiveText")] public string PositiveText { get; set; }

		/// <summary>Text for the decline button; empty means no decline button.</summary>
		[JsonProperty("negativeText")] public string NegativeText { get; set; }

		[JsonProperty("linkUrl", NullValueHandling = NullValueHandling.Ignore)] public string LinkUrl { get; set; }
		[JsonProperty("linkText", NullValueHandling = NullValueHandling.Ignore)] public string LinkText { get; set; }

		/// <summary>True when nothing is sent back to the SDK: the UI only shows it and dismisses it.</summary>
		[JsonProperty("informational")] public bool Informational { get; set; }

		/// <summary>Inactive warning: UTC seconds at which the meeting ends on its own.</summary>
		[JsonProperty("autoEndTime", NullValueHandling = NullValueHandling.Ignore)] public long? AutoEndTime { get; set; }

		[JsonProperty("receivedAt")] public DateTime ReceivedAt { get; set; }

		[JsonIgnore] internal PromptSource Source { get; set; }
		[JsonIgnore] internal ZrcPromptKind NativeKind { get; set; }
		[JsonIgnore] internal int NativeType { get; set; }
		[JsonIgnore] internal long NativeType64 { get; set; }
		[JsonIgnore] internal string ConsentId { get; set; }
		[JsonIgnore] internal int UserId { get; set; }

		/// <summary>Identity used to replace / remove a prompt when the Zoom Room re-sends or closes it.</summary>
		[JsonIgnore] internal string DedupKey => $"{Source}:{NativeKind}:{NativeType}:{NativeType64}:{ConsentId}:{UserId}";
	}

	internal enum PromptSource { Native, AudioUnmute, CameraControl, RoleNotice }

	public partial class ZoomRoom
	{
		private readonly object _promptLock = new object();
		private readonly List<ZoomPrompt> _prompts = new List<ZoomPrompt>();
		private int _nextPromptId = 1;
		private bool _noticedIsHost;
		private bool _noticedIsCoHost;

		/// <summary>Raised whenever <see cref="ActivePrompts"/> changes (added, answered, dismissed, cleared).</summary>
		public event EventHandler PromptsChanged;

		/// <summary>Snapshot of the prompts currently waiting on the UI, oldest first.</summary>
		public IReadOnlyList<ZoomPrompt> ActivePrompts
		{
			get { lock (_promptLock) return _prompts.ToList(); }
		}

		private void SubscribePromptEvents()
		{
			_controller.PromptReceived += (s, e) => HandleNativePrompt(e);
			_controller.AudioUnmuteRequested += (s, e) => HandleAudioUnmuteRequest();
			_controller.FarEndCameraControlRequested += (s, e) => HandleFarEndCameraControlRequest(e?.ErrorCode ?? 0);
		}

		// ── Inbound ─────────────────────────────────────────────────────────────

		private void HandleNativePrompt(PromptEventArgs e)
		{
			if (e == null) return;
			this.LogInformation("Prompt from SDK: kind={Kind} type={Type} showing={Showing} title=\"{Title}\"",
				e.Kind, e.Type, e.IsShowing, e.Title);

			var p = new ZoomPrompt
			{
				Source = PromptSource.Native,
				NativeKind = e.Kind,
				NativeType = e.Type,
				NativeType64 = e.Type64,
				ConsentId = e.ConsentId ?? string.Empty,
				UserId = e.UserId,
				Title = e.Title ?? string.Empty,
				Message = e.Message ?? string.Empty,
				PositiveText = e.PositiveText ?? string.Empty,
				NegativeText = e.NegativeText ?? string.Empty,
				LinkUrl = string.IsNullOrEmpty(e.LinkUrl) ? null : e.LinkUrl,
				LinkText = string.IsNullOrEmpty(e.LinkText) ? null : e.LinkText,
			};

			switch (e.Kind)
			{
				case ZrcPromptKind.MeetingReminder:
					p.Kind = "meetingReminder";
					p.SubType = ((MeetingReminderType)e.Type).ToString();
					ApplyReminderDefaults(p, (MeetingReminderType)e.Type);
					break;
				case ZrcPromptKind.CustomizedReminder:
					p.Kind = "customizedReminder";
					p.SubType = e.Type.ToString();
					Default(p, "Notice", string.Empty, "Continue", string.Empty);
					break;
				case ZrcPromptKind.CombinedConsent:
					p.Kind = "combinedConsent";
					p.SubType = e.Type64.ToString();
					Default(p, "Consent Required", string.Empty, "Agree", "Decline");
					break;
				case ZrcPromptKind.Consent:
					p.Kind = "consent";
					p.SubType = ((ConsentType)e.Type).ToString();
					ApplyConsentDefaults(p, (ConsentType)e.Type);
					break;
				case ZrcPromptKind.PrivacyAlert:
					p.Kind = "privacyAlert";
					p.SubType = ((PrivacyAlertType)e.Type).ToString();
					p.Informational = true;
					Default(p, (PrivacyAlertType)e.Type == PrivacyAlertType.LiveTranscription ? "Live Transcription" : "Captions",
						"A privacy notice is showing on the Zoom Room.", "OK", string.Empty);
					break;
				case ZrcPromptKind.InactiveDetection:
					p.Kind = "inactiveWarning";
					p.AutoEndTime = e.AutoEndTime;
					Default(p, "Meeting Ending Soon",
						"No one has been detected in the room for a while. The meeting will end automatically unless you keep it running.",
						"Keep Meeting", string.Empty);
					break;
				case ZrcPromptKind.MessageEvent:
					p.Kind = "messageEvent";
					p.SubType = ((MeetingMessageEvent)e.Type).ToString();
					p.Informational = true;
					Default(p, "Video Could Not Start", MessageEventText((MeetingMessageEvent)e.Type), "OK", string.Empty);
					break;
				case ZrcPromptKind.AskStartVideo:
					p.Kind = "videoStartRequest";
					Default(p, "Start Video?", "The host is asking this room to start its video.", "Start Video", "Keep Off");
					break;
				case ZrcPromptKind.BOSwitchRequest:
					p.Kind = "boSwitchRequest";
					Default(p, "Breakout Room",
						$"{Who(e.FromUser, "The host")} is moving this room to breakout room \"{e.SessionName}\".",
						"Join", "Not Now");
					break;
				case ZrcPromptKind.BOReturnToMainInvite:
					p.Kind = "boReturnInvite";
					Default(p, "Return to Main Session",
						$"{Who(e.FromUser, "The host")} is inviting this room back to the main session.",
						"Return", "Stay");
					break;
				case ZrcPromptKind.WebinarRoleChanged:
					p.Kind = "webinarRoleChanged";
					p.SubType = ((WebinarRoleChangedState)e.Type).ToString();
					p.Informational = true;
					Default(p, "Webinar Role Changed",
						(WebinarRoleChangedState)e.Type == WebinarRoleChangedState.Promote
							? "This room is now a panelist."
							: "This room is now an attendee.",
						"OK", string.Empty);
					break;
				default:
					this.LogWarning("Prompt kind {Kind} not handled", e.Kind);
					return;
			}

			if (!e.IsShowing)
			{
				// The Zoom Room closed the dialog (answered on its own controller, timed out, or resolved
				// by the meeting); drop ours without answering.
				RemovePrompt(p.DedupKey, "closed by the Zoom Room");
				return;
			}

			AddOrReplacePrompt(p);
		}

		private void HandleAudioUnmuteRequest()
		{
			this.LogInformation("Host asked this room to unmute audio");
			var p = new ZoomPrompt { Source = PromptSource.AudioUnmute, Kind = "audioUnmuteRequest" };
			Default(p, "Unmute?", "The host is asking this room to unmute its microphone.", "Unmute", "Stay Muted");
			AddOrReplacePrompt(p);
		}

		private void HandleFarEndCameraControlRequest(int userId)
		{
			var name = ParticipantDisplayName(userId);
			this.LogInformation("Far-end camera control requested by userId={UserId} ({Name})", userId, name);
			var p = new ZoomPrompt { Source = PromptSource.CameraControl, Kind = "cameraControlRequest", UserId = userId };
			Default(p, "Camera Control Request", $"{Who(name, "A participant")} is asking to control this room's camera.", "Allow", "Deny");
			AddOrReplacePrompt(p);
		}

		/// <summary>
		/// Called after <c>_sdkIsHost</c> / <c>_sdkIsCoHost</c> change. Emits an informational prompt for
		/// a role change that happens mid-meeting; the roles the room joined with are not announced.
		/// </summary>
		private void NoteRoleChange()
		{
			if (!_hasConfirmedRosterAdmission)
			{
				_noticedIsHost = _sdkIsHost;
				_noticedIsCoHost = _sdkIsCoHost;
				return;
			}

			if (_sdkIsHost != _noticedIsHost)
			{
				_noticedIsHost = _sdkIsHost;
				RoleNotice("host", _sdkIsHost ? "This room is now the meeting host." : "This room is no longer the meeting host.");
			}
			if (_sdkIsCoHost != _noticedIsCoHost)
			{
				_noticedIsCoHost = _sdkIsCoHost;
				RoleNotice("coHost", _sdkIsCoHost ? "This room is now a co-host." : "This room is no longer a co-host.");
			}
		}

		private void RoleNotice(string subType, string message)
		{
			this.LogInformation("Role notice: {Message}", message);
			var p = new ZoomPrompt { Source = PromptSource.RoleNotice, Kind = "roleChanged", SubType = subType, Informational = true };
			Default(p, "Meeting Role Changed", message, "OK", string.Empty);
			AddOrReplacePrompt(p);
		}

		// ── Outbound ────────────────────────────────────────────────────────────

		/// <summary>Answers a prompt (accept = positive button) and removes it. Informational prompts are just removed.</summary>
		public void AnswerPrompt(int id, bool accept)
		{
			ZoomPrompt p;
			lock (_promptLock)
			{
				p = _prompts.FirstOrDefault(x => x.Id == id);
				if (p != null) _prompts.Remove(p);
			}
			if (p == null)
			{
				this.LogWarning("AnswerPrompt: no prompt with id {Id}", id);
				return;
			}
			PromptsChanged?.Invoke(this, EventArgs.Empty);

			this.LogInformation("Prompt {Id} ({Kind}/{SubType}) answered: accept={Accept}", p.Id, p.Kind, p.SubType, accept);
			if (p.Informational && p.Source != PromptSource.Native) return;

			try
			{
				switch (p.Source)
				{
					case PromptSource.AudioUnmute:
						_controller.AnswerUnmuteRequest(accept);
						break;
					case PromptSource.CameraControl:
						_controller.RespondRemoteCameraControl(p.UserId, accept);
						break;
					case PromptSource.Native:
						AnswerNativePrompt(p, accept);
						break;
				}
			}
			catch (Exception ex)
			{
				this.LogException(ex, "AnswerPrompt {Id} threw", id);
			}
		}

		/// <summary>Removes a prompt without answering it (informational prompts, or the UI giving up).</summary>
		public void DismissPrompt(int id)
		{
			bool removed;
			lock (_promptLock) removed = _prompts.RemoveAll(x => x.Id == id) > 0;
			if (!removed) return;
			this.LogDebug("Prompt {Id} dismissed", id);
			PromptsChanged?.Invoke(this, EventArgs.Empty);
		}

		private void AnswerNativePrompt(ZoomPrompt p, bool accept)
		{
			switch (p.NativeKind)
			{
				case ZrcPromptKind.MeetingReminder:
					_controller.ConfirmMeetingReminder(accept, p.NativeType);
					break;
				case ZrcPromptKind.CustomizedReminder:
					_controller.ConfirmCustomizedMeetingReminder(accept, p.NativeType);
					break;
				case ZrcPromptKind.Consent:
					_controller.ConfirmConsent(accept, p.NativeType, p.ConsentId);
					break;
				case ZrcPromptKind.CombinedConsent:
					_controller.ConfirmCombinedConsent(accept, p.NativeType64);
					break;
				case ZrcPromptKind.PrivacyAlert:
					_controller.HandlePrivacyAlert((int)PrivacyAlertAction.Close, p.NativeType);
					break;
				case ZrcPromptKind.InactiveDetection:
					if (accept) _controller.ContinueMeetingOnInactivity();
					break;
				case ZrcPromptKind.AskStartVideo:
					_controller.AnswerHostRequestUnmuteVideo(accept);
					break;
				case ZrcPromptKind.BOSwitchRequest:
					if (accept) _controller.JoinBreakoutRoom();
					break;
				case ZrcPromptKind.BOReturnToMainInvite:
					_controller.ResponseHostInviteToMainSession(accept);
					break;
				// MessageEvent / WebinarRoleChanged: informational, nothing to send.
			}
		}

		// ── Test hook ───────────────────────────────────────────────────────────

		/// <summary>
		/// Raises a prompt through the same path the SDK would, so the UI flow can be exercised without
		/// Zoom. From the console:
		/// <c>devjson:1 {"deviceKey":"zoomRoom","methodName":"SimulatePrompt","params":["panelist", ""]}</c>
		/// kinds: audioUnmute, videoStart, cameraControl, panelist, consent, inactive, boSwitch, boReturn,
		/// webinarPromote, webinarDemote, message, host, coHost. The second parameter is a name / room /
		/// title where the kind uses one. Answers go to the SDK and are logged (an error code outside a
		/// meeting is expected).
		/// </summary>
		public void SimulatePrompt(string kind, string text)
		{
			text = text ?? string.Empty;
			this.LogWarning("SIMULATED prompt (console test hook): kind={Kind} text=\"{Text}\"", kind, text);
			switch ((kind ?? string.Empty).Trim().ToLowerInvariant())
			{
				case "audiounmute": HandleAudioUnmuteRequest(); break;
				case "cameracontrol": HandleFarEndCameraControlRequest(0); break;
				case "videostart": HandleNativePrompt(new PromptEventArgs { Kind = ZrcPromptKind.AskStartVideo, IsShowing = true }); break;
				case "panelist":
					HandleNativePrompt(new PromptEventArgs { Kind = ZrcPromptKind.MeetingReminder, Type = (int)MeetingReminderType.JoinWebinarAsPanelist, IsShowing = true });
					break;
				case "consent":
					HandleNativePrompt(new PromptEventArgs { Kind = ZrcPromptKind.Consent, Type = (int)ConsentType.Common, ConsentId = "sim", IsShowing = true, Title = string.IsNullOrEmpty(text) ? "Simulated Consent" : text, Message = "This is a simulated consent dialog.", PositiveText = "Agree", NegativeText = "Decline" });
					break;
				case "inactive":
					HandleNativePrompt(new PromptEventArgs { Kind = ZrcPromptKind.InactiveDetection, IsShowing = true, AutoEndTime = DateTimeOffset.UtcNow.ToUnixTimeSeconds() + 120 });
					break;
				case "boswitch":
					HandleNativePrompt(new PromptEventArgs { Kind = ZrcPromptKind.BOSwitchRequest, IsShowing = true, FromUser = "Host", SessionBID = "sim", SessionName = string.IsNullOrEmpty(text) ? "Room 1" : text });
					break;
				case "boreturn":
					HandleNativePrompt(new PromptEventArgs { Kind = ZrcPromptKind.BOReturnToMainInvite, IsShowing = true, FromUser = string.IsNullOrEmpty(text) ? "Host" : text });
					break;
				case "webinarpromote": HandleNativePrompt(new PromptEventArgs { Kind = ZrcPromptKind.WebinarRoleChanged, Type = (int)WebinarRoleChangedState.Promote, IsShowing = true }); break;
				case "webinardemote": HandleNativePrompt(new PromptEventArgs { Kind = ZrcPromptKind.WebinarRoleChanged, Type = (int)WebinarRoleChangedState.Demote, IsShowing = true }); break;
				case "message": HandleNativePrompt(new PromptEventArgs { Kind = ZrcPromptKind.MessageEvent, Type = (int)MeetingMessageEvent.OpenVideoFailForHostStop, IsShowing = true }); break;
				case "host": RoleNotice("host", "This room is now the meeting host."); break;
				case "cohost": RoleNotice("coHost", "This room is now a co-host."); break;
				default: this.LogWarning("SimulatePrompt: unknown kind \"{Kind}\"", kind); break;
			}
		}

		// ── Helpers ─────────────────────────────────────────────────────────────

		private void AddOrReplacePrompt(ZoomPrompt p)
		{
			lock (_promptLock)
			{
				var existing = _prompts.FirstOrDefault(x => x.DedupKey == p.DedupKey);
				if (existing != null)
				{
					p.Id = existing.Id;
					p.ReceivedAt = existing.ReceivedAt;
					_prompts[_prompts.IndexOf(existing)] = p;
				}
				else
				{
					p.Id = _nextPromptId++;
					p.ReceivedAt = DateTime.UtcNow;
					_prompts.Add(p);
				}
			}
			PromptsChanged?.Invoke(this, EventArgs.Empty);
		}

		private void RemovePrompt(string dedupKey, string why)
		{
			bool removed;
			lock (_promptLock) removed = _prompts.RemoveAll(x => x.DedupKey == dedupKey) > 0;
			if (!removed) return;
			this.LogDebug("Prompt {Key} removed: {Why}", dedupKey, why);
			PromptsChanged?.Invoke(this, EventArgs.Empty);
		}

		private void ClearPrompts(string why)
		{
			bool any;
			lock (_promptLock)
			{
				any = _prompts.Count > 0;
				_prompts.Clear();
			}
			_noticedIsHost = false;
			_noticedIsCoHost = false;
			if (!any) return;
			this.LogDebug("Prompts cleared: {Why}", why);
			PromptsChanged?.Invoke(this, EventArgs.Empty);
		}

		private static void Default(ZoomPrompt p, string title, string message, string positive, string negative)
		{
			if (string.IsNullOrWhiteSpace(p.Title)) p.Title = title;
			if (string.IsNullOrWhiteSpace(p.Message)) p.Message = message;
			if (string.IsNullOrWhiteSpace(p.PositiveText)) p.PositiveText = positive;
			if (string.IsNullOrWhiteSpace(p.NegativeText)) p.NegativeText = negative;
		}

		private static void ApplyReminderDefaults(ZoomPrompt p, MeetingReminderType type)
		{
			switch (type)
			{
				case MeetingReminderType.JoinWebinarAsPanelist:
					Default(p, "Panelist Invitation", "The host has invited this room to join the webinar as a panelist.", "Join as Panelist", "Decline");
					break;
				case MeetingReminderType.RecordingReminder:
				case MeetingReminderType.RecordingDisclaimer:
					Default(p, "Recording Notice", "This meeting is being recorded.", "Continue", string.Empty);
					break;
				case MeetingReminderType.ArchivingFail:
					p.Informational = true;
					Default(p, "Archiving Failed", "Meeting archiving has failed.", "OK", string.Empty);
					break;
				default:
					Default(p, "Meeting Notice", string.Empty, "Continue", "Cancel");
					break;
			}
		}

		private static void ApplyConsentDefaults(ZoomPrompt p, ConsentType type)
		{
			switch (type)
			{
				case ConsentType.PromotedToPanelist:
					Default(p, "Promoted to Panelist", "The host has promoted this room to panelist.", "Accept", "Decline");
					break;
				case ConsentType.LiveStreaming:
					Default(p, "Live Streaming", "This meeting is being live streamed.", "Agree", "Decline");
					break;
				case ConsentType.Archiving:
					Default(p, "Archiving", "This meeting is being archived.", "Agree", "Decline");
					break;
				case ConsentType.Ndi:
					Default(p, "NDI Output", "This meeting's video is being sent out over NDI.", "Agree", "Decline");
					break;
				case ConsentType.FocusModeStart:
				case ConsentType.FocusModeEnding:
					p.Informational = type == ConsentType.FocusModeEnding;
					Default(p, "Focus Mode", type == ConsentType.FocusModeStart ? "The host has started focus mode." : "Focus mode is ending.", "OK", string.Empty);
					break;
				case ConsentType.MeetingSummary:
				case ConsentType.MeetingQuery:
				case ConsentType.CustomAiCompanion:
					Default(p, "AI Companion", "AI Companion features are active in this meeting.", "Agree", "Decline");
					break;
				case ConsentType.CustomRecording:
					Default(p, "Recording Notice", "This meeting is being recorded.", "Agree", "Decline");
					break;
				case ConsentType.HdmiConnected:
					p.Informational = true;
					Default(p, "HDMI Connected", "An HDMI source has been connected.", "OK", string.Empty);
					break;
				default:
					Default(p, "Consent Required", string.Empty, "Agree", "Decline");
					break;
			}
		}

		private static string MessageEventText(MeetingMessageEvent e)
		{
			switch (e)
			{
				case MeetingMessageEvent.OpenVideoFailForHostStop: return "The host has stopped this room's video.";
				case MeetingMessageEvent.OpenVideoFailForForceVBEnabledButUserOptionDisabled:
				case MeetingMessageEvent.OpenVideoFailForForceVBEnabledButUserNoGreenScreen:
				case MeetingMessageEvent.OpenVideoFailForForceVBEnabledButDeviceNotSupport:
					return "A virtual background is required in this meeting and this room cannot provide one.";
				default: return "Video could not be started.";
			}
		}

		private static string Who(string name, string fallback) => string.IsNullOrWhiteSpace(name) ? fallback : name.Trim();

		private string ParticipantDisplayName(int userId)
		{
			lock (_participantLock)
				return Participants.CurrentParticipants.FirstOrDefault(x => x.UserId == userId)?.Name ?? string.Empty;
		}
	}
}
