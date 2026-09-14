using System;
using PepperDash.Core.Logging;
using PepperDash.ZoomRoom.Sdk;

namespace PepperDash.Essentials.Plugins
{
	/// <summary>
	/// Breakout rooms. The SDK reports the session status (edit / started / stopping / ended) and
	/// accepts start / stop / broadcast from a host or co-host, and join assigned room / return to
	/// main / ask for help from an attendee. Rooms are created and assigned in Zoom's scheduler or
	/// on the Zoom Room itself; creating them from here needs the creator helper, which the native
	/// wrapper does not expose yet (neither does it deliver the room list).
	/// </summary>
	public partial class ZoomRoom
	{
		private string _sdkBreakoutStatus = "unknown";

		/// <summary>Raised when the breakout status changes.</summary>
		public event EventHandler BreakoutChanged;

		/// <summary>unknown | invalid | edit | started | stopping | ended</summary>
		public string BreakoutStatus => _sdkBreakoutStatus;

		private void SubscribeBreakoutEvents()
		{
			_controller.BreakoutStatusChanged += (s, e) => ApplyBreakoutStatus(e?.ErrorCode ?? 0, "SDK");
		}

		private void ApplyBreakoutStatus(int status, string source)
		{
			string text;
			switch (status)
			{
				case 1: text = "edit"; break;
				case 2: text = "started"; break;
				case 3: text = "stopping"; break;
				case 4: text = "ended"; break;
				default: text = "invalid"; break;
			}
			if (text == _sdkBreakoutStatus) return;
			_sdkBreakoutStatus = text;
			this.LogInformation("Breakout status ({Source}): {Status}", source, text);
			BreakoutChanged?.Invoke(this, EventArgs.Empty);
		}

		private void ResetBreakout()
		{
			if (_sdkBreakoutStatus == "unknown") return;
			_sdkBreakoutStatus = "unknown";
			BreakoutChanged?.Invoke(this, EventArgs.Empty);
		}

		public void StartBreakoutRooms()
		{
			this.LogInformation("StartBreakoutRooms (isHost={IsHost}, isCoHost={IsCoHost})", _sdkIsHost, _sdkIsCoHost);
			_controller.StartBreakoutRooms();
		}

		public void StopBreakoutRooms()
		{
			this.LogInformation("StopBreakoutRooms (isHost={IsHost}, isCoHost={IsCoHost})", _sdkIsHost, _sdkIsCoHost);
			_controller.StopBreakoutRooms();
		}

		public void BroadcastToBreakoutRooms(string message)
		{
			if (string.IsNullOrWhiteSpace(message)) return;
			this.LogInformation("BroadcastToBreakoutRooms: \"{Message}\"", message);
			_controller.BroadcastMessageToBreakoutRooms(message.Trim());
		}

		/// <summary>Joins the breakout room this room was assigned to.</summary>
		public void JoinAssignedBreakoutRoom()
		{
			this.LogInformation("JoinAssignedBreakoutRoom");
			_controller.JoinBreakoutRoom();
		}

		/// <summary>Leaves the current breakout room and returns to the main session.</summary>
		public void LeaveBreakoutRoom()
		{
			this.LogInformation("LeaveBreakoutRoom");
			_controller.LeaveBreakoutRoom();
		}

		public void AskForHelpInBreakoutRoom()
		{
			this.LogInformation("AskForHelpInBreakoutRoom");
			_controller.AskForHelpInBreakoutRoom();
		}

		/// <summary>
		/// Test hook: <c>devjson:1 {"deviceKey":"zoomRoom","methodName":"SimulateBreakoutStatus","params":[2]}</c>
		/// (1 edit, 2 started, 3 stopping, 4 ended, 0 invalid).
		/// </summary>
		public void SimulateBreakoutStatus(int status)
		{
			this.LogWarning("SIMULATED breakout status (console test hook): {Status}", status);
			ApplyBreakoutStatus(status, "simulated");
		}
	}
}
