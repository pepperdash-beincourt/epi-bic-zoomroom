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
	}
}
