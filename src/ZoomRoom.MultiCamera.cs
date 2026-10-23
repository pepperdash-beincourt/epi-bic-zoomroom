using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;
using PepperDash.Core.Logging;
using PepperDash.ZoomRoom.Sdk;
using PepperDash.ZoomRoom.Sdk.EventArgs;

namespace PepperDash.Essentials.Plugins
{
	/// <summary>
	/// Which cameras take part in multi-camera - the Zoom Room setting the admin portal shows as a
	/// tick per camera. Unticking a camera removes its tile from the meeting; ticking it brings the
	/// tile back. The room asks for this per camera input (<see cref="SetCameraInputInMultiCamera"/>)
	/// as cameras are enabled and disabled; config <c>multiCameraInputs</c> says which Zoom Room camera
	/// is on each input. The setting persists in the Zoom Room, so the wanted state is applied again
	/// whenever the controller connects. Console: "zoomMultiCamera [deviceKey] list | &lt;n&gt; on|off".
	/// </summary>
	public partial class ZoomRoom
	{
		// Only long enough to leave the caller's thread and gather a burst (all cameras muted at once).
		private const int MultiCameraApplyDelayMs = 150;

		// After a select in a meeting, the longest the next select waits for that camera's tile to join;
		// it goes as soon as the tile has joined (LearnCameraTileInputs).
		private const int MultiCameraJoinWaitMs = 4000;

		private readonly object _multiCameraLock = new object();
		private readonly Dictionary<string, bool> _multiCameraWanted = new Dictionary<string, bool>(StringComparer.OrdinalIgnoreCase);
		private CTimer _multiCameraTimer;

		/// <summary>
		/// Turns the camera on camera input <paramref name="inputPortKey"/> (e.g. "usbIn2") on or off
		/// for multi-camera. Returns false when config <c>multiCameraInputs</c> does not list the input,
		/// in which case nothing changes. Applied a moment later from a timer thread (SDK calls made
		/// inside SDK event handlers fail - see ScheduleFixedAudioLevels), and again on every connect.
		/// </summary>
		public bool SetCameraInputInMultiCamera(string inputPortKey, bool selected)
		{
			if (string.IsNullOrEmpty(inputPortKey) || _props.MultiCameraInputs == null || !_props.MultiCameraInputs.ContainsKey(inputPortKey))
			{
				this.LogDebug("Multi-camera: camera input '{Input}' is not in multiCameraInputs; left as it is", inputPortKey);
				return false;
			}

			lock (_multiCameraLock)
				_multiCameraWanted[inputPortKey] = selected;

			this.LogInformation("Multi-camera: camera input '{Input}' {State}", inputPortKey, selected ? "on" : "off");
			ScheduleMultiCameraApply(MultiCameraApplyDelayMs);
			return true;
		}

		private void ScheduleMultiCameraApply(int delayMs)
		{
			lock (_multiCameraLock)
			{
				if (_multiCameraWanted.Count == 0) return;
				_multiCameraTimer?.Stop();
				_multiCameraTimer = new CTimer(_ => ApplyMultiCameraSelection(), delayMs);
			}
		}

		private void ApplyMultiCameraSelection()
		{
			if ((ConnectionState)_controller.GetConnectionState() != ConnectionState.Connected) return;

			// Deselects first. In a meeting, selects go one at a time so the tile that joins next is
			// known to be that camera (ExpectCameraTile); the rest follow once it has had time to join.
			KeyValuePair<string, bool>[] wanted;
			lock (_multiCameraLock) wanted = _multiCameraWanted.OrderBy(e => e.Value).ToArray();

			var cameras = _controller.GetCameras() ?? new CameraDevice[0];
			foreach (var entry in wanted)
			{
				var match = _props.MultiCameraInputs[entry.Key];
				var camera = FindCamera(cameras, match);
				if (camera == null)
				{
					this.LogWarning("Multi-camera: no Zoom Room camera matches '{Match}' (input '{Input}'); see zoomMultiCamera list", match, entry.Key);
					continue;
				}

				if (camera.IsSelectedAsMultiDevice == entry.Value) continue;

				if (entry.Value) ExpectCameraTile(entry.Key);
				var code = _controller.SelectMultipleCamera(camera.Id, entry.Value);
				this.LogInformation("Multi-camera: {Action} \"{Camera}\" (input '{Input}') result={Code}",
					entry.Value ? "select" : "deselect", CameraLabel(camera), entry.Key, code);

				if (!entry.Value) continue;
				if (code != 0)
				{
					ForgetExpectedCameraTile(entry.Key);
					continue;
				}
				if (!IsInCall) continue;
				ScheduleMultiCameraApply(MultiCameraJoinWaitMs);
				return;
			}
		}

		private static CameraDevice FindCamera(IEnumerable<CameraDevice> cameras, string match)
		{
			if (string.IsNullOrWhiteSpace(match)) return null;
			var m = match.Trim();
			return cameras.FirstOrDefault(c =>
				string.Equals(c.Id, m, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(c.Name, m, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(c.Alias, m, StringComparison.OrdinalIgnoreCase) ||
				string.Equals(c.DisplayName, m, StringComparison.OrdinalIgnoreCase));
		}

		private static string CameraLabel(CameraDevice c) =>
			!string.IsNullOrEmpty(c.DisplayName) ? c.DisplayName : !string.IsNullOrEmpty(c.Name) ? c.Name : c.Id;

		private void MultiCameraConsoleCommand(string args)
		{
			var parts = (args ?? string.Empty).Trim().Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			var cameras = _controller.GetCameras() ?? new CameraDevice[0];

			if (parts.Length == 0 || parts[0].Equals("list", StringComparison.OrdinalIgnoreCase))
			{
				if (cameras.Length == 0)
				{
					CrestronConsole.ConsoleCommandResponse("No cameras reported (is the Zoom Room connected?)\r\n");
					return;
				}

				for (var i = 0; i < cameras.Length; i++)
				{
					var c = cameras[i];
					var input = _props.MultiCameraInputs?.FirstOrDefault(kv => FindCamera(new[] { c }, kv.Value) != null).Key;
					CrestronConsole.ConsoleCommandResponse("{0}: multiCamera={1} main={2} displayName=\"{3}\" name=\"{4}\" alias=\"{5}\" input={6} id=\"{7}\"\r\n",
						i + 1, c.IsSelectedAsMultiDevice, c.IsSelected, c.DisplayName, c.Name, c.Alias, input ?? "-", c.Id);
				}
				return;
			}

			int number;
			if (parts.Length != 2 || !int.TryParse(parts[0], out number) || number < 1 || number > cameras.Length ||
				!(parts[1].Equals("on", StringComparison.OrdinalIgnoreCase) || parts[1].Equals("off", StringComparison.OrdinalIgnoreCase)))
			{
				CrestronConsole.ConsoleCommandResponse("Usage: zoomMultiCamera [deviceKey] list | <n> on|off  (n from the list, 1-{0})\r\n", cameras.Length);
				return;
			}

			var camera = cameras[number - 1];
			var on = parts[1].Equals("on", StringComparison.OrdinalIgnoreCase);
			var inputKey = _props.MultiCameraInputs?.FirstOrDefault(kv => FindCamera(new[] { camera }, kv.Value) != null).Key;
			if (on) ExpectCameraTile(inputKey);
			var code = _controller.SelectMultipleCamera(camera.Id, on);
			this.LogInformation("zoomMultiCamera: {Action} \"{Camera}\" result={Code}", on ? "select" : "deselect", CameraLabel(camera), code);
			if (on && code != 0) ForgetExpectedCameraTile(inputKey);
			CrestronConsole.ConsoleCommandResponse("Camera {0} \"{1}\" multi-camera {2}: result {3}{4}\r\n", number, CameraLabel(camera), on ? "on" : "off", code,
				code == 0 ? " (ok)" : code == 136 ? " (multi-camera not enabled)" : code == 137 ? " (multi-camera count at its maximum)" : code == 138 ? " (this camera can't be selected)" : " (see log)");
		}
	}
}
