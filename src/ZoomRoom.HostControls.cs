using System;
using PepperDash.Core.Logging;
using PepperDash.ZoomRoom.Sdk;

namespace PepperDash.Essentials.Plugins
{
	/// <summary>
	/// Host / co-host meeting-wide audio and video settings: mute on entry, whether attendees may
	/// unmute themselves, whether attendees may start video, plus the momentary "mute all" and
	/// "ask all to unmute" actions. Each setting is null until the Zoom Room has reported it.
	/// See ZoomRoomHostControlsMessenger for the Mobile Control contract.
	/// </summary>
	public partial class ZoomRoom
	{
		private bool? _sdkMuteOnEntry;
		private bool? _sdkAllowAttendeesUnmute;
		private bool? _sdkAllowAttendeesStartVideo;

		/// <summary>Raised when any of the three settings changes.</summary>
		public event EventHandler HostControlsChanged;

		public bool? MuteOnEntry => _sdkMuteOnEntry;
		public bool? AllowAttendeesUnmuteThemselves => _sdkAllowAttendeesUnmute;
		public bool? AllowAttendeesStartVideo => _sdkAllowAttendeesStartVideo;

		private void SubscribeHostControlEvents()
		{
			_controller.MuteOnEntryChanged += (s, e) => SetHostControl(ref _sdkMuteOnEntry, e?.ErrorCode == 1, "muteOnEntry");
			_controller.AllowAttendeesUnmuteChanged += (s, e) => SetHostControl(ref _sdkAllowAttendeesUnmute, e?.ErrorCode == 1, "allowAttendeesUnmute");
			_controller.AllowAttendeesVideoChanged += (s, e) => SetHostControl(ref _sdkAllowAttendeesStartVideo, e?.ErrorCode == 1, "allowAttendeesStartVideo");
		}

		private void SetHostControl(ref bool? field, bool value, string name)
		{
			if (field == value) return;
			field = value;
			this.LogDebug("Host control {Name}={Value}", name, value);
			HostControlsChanged?.Invoke(this, EventArgs.Empty);
		}

		private void ResetHostControls()
		{
			var changed = _sdkMuteOnEntry != null || _sdkAllowAttendeesUnmute != null || _sdkAllowAttendeesStartVideo != null;
			_sdkMuteOnEntry = null;
			_sdkAllowAttendeesUnmute = null;
			_sdkAllowAttendeesStartVideo = null;
			if (changed) HostControlsChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>Mutes every participant's microphone (host / co-host).</summary>
		public void MuteAllParticipants()
		{
			this.LogInformation("MuteAllParticipants (isHost={IsHost}, isCoHost={IsCoHost})", _sdkIsHost, _sdkIsCoHost);
			_controller.MuteAllAudio(true);
		}

		/// <summary>Sends every participant a request to unmute (host / co-host).</summary>
		public void AskAllToUnmute()
		{
			this.LogInformation("AskAllToUnmute (isHost={IsHost}, isCoHost={IsCoHost})", _sdkIsHost, _sdkIsCoHost);
			_controller.MuteAllAudio(false);
		}

		public void SetMuteOnEntry(bool enabled)
		{
			this.LogInformation("SetMuteOnEntry {Enabled}", enabled);
			_controller.SetMuteOnEntry(enabled);
		}

		public void SetAllowAttendeesUnmute(bool allow)
		{
			this.LogInformation("SetAllowAttendeesUnmute {Allow}", allow);
			_controller.AllowAttendeesUnmute(allow);
		}

		public void SetAllowAttendeesStartVideo(bool allow)
		{
			this.LogInformation("SetAllowAttendeesStartVideo {Allow}", allow);
			_controller.AllowAttendeesStartVideo(allow);
		}
	

		// ── Roles ──────────────────────────────────────────────────────────────

		/// <summary>Claims host of the current meeting with the host key; the result shows up as a host change.</summary>
		public void ClaimHost(string hostKey)
		{
			if (string.IsNullOrWhiteSpace(hostKey)) return;
			this.LogInformation("ClaimHost with a {Length}-digit key", hostKey.Trim().Length);
			_controller.ClaimHost(hostKey.Trim());
		}

		public void SetParticipantAsCoHost(int userId, bool assign)
		{
			this.LogInformation("SetParticipantAsCoHost {UserId} assign={Assign}", userId, assign);
			_controller.AssignCohost(userId, assign);
		}

		// Promote / demote / allow to talk change the attendee list, so each asks for it again shortly
		// (see ZoomRoom.Webinar.cs); on the SimulateWebinar fake list they act locally instead.

		public void PromoteToPanelist(int userId)
		{
			this.LogInformation("PromoteToPanelist {UserId}", userId);
			if (TrySimulatedAttendee(userId, "promote to panelist (leaves the fake list)", a => _simulatedAttendees.Remove(a))) return;
			_controller.PromoteAttendeeToPanelist(userId);
			ScheduleAttendeeRefresh();
		}

		public void DemoteToAttendee(int userId)
		{
			this.LogInformation("DemoteToAttendee {UserId}", userId);
			_controller.DemotePanelistToAttendee(userId);
			ScheduleAttendeeRefresh();
		}

		public void AllowAttendeeTalk(int userId, bool allow)
		{
			this.LogInformation("AllowAttendeeTalk {UserId} allow={Allow}", userId, allow);
			if (TrySimulatedAttendee(userId, allow ? "allow to talk" : "disallow talk", a => a.CanTalk = allow)) return;
			_controller.AllowWebinarAttendeeTalk(userId, allow);
			ScheduleAttendeeRefresh();
		}

		// ── Cloud recording pause / resume ─────────────────────────────────────

		private bool _sdkRecordingPaused;
		private bool _sdkRecordingConnecting;

		/// <summary>Raised when the paused / connecting state of the cloud recording changes.</summary>
		public event EventHandler RecordingExtrasChanged;

		public bool IsRecordingPaused => _sdkRecordingPaused;
		public bool IsRecordingConnecting => _sdkRecordingConnecting;

		public void PauseRecording()
		{
			this.LogInformation("PauseRecording");
			_controller.PauseRecording();
		}

		public void ResumeRecording()
		{
			this.LogInformation("ResumeRecording");
			_controller.ResumeRecording();
		}
	}
}
