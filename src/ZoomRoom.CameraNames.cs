using Crestron.SimplSharp;
using PepperDash.Core.Logging;

namespace PepperDash.Essentials.Plugins
{
	/// <summary>
	/// Camera display names in the Zoom Room's settings (persistent across meetings and restarts).
	/// Console: "zoomCameraName [deviceKey] list" numbers the room's cameras; "zoomCameraName
	/// [deviceKey] &lt;n&gt; &lt;name&gt;" renames camera n.
	/// </summary>
	public partial class ZoomRoom
	{
		/// <summary>
		/// Renames a camera in the Zoom Room's settings. Returns false when the camera is unknown or
		/// the SDK rejects the name (code 141 = this camera's display name can't be set).
		/// </summary>
		public bool SetCameraDisplayName(string deviceId, string displayName)
		{
			var code = _controller.SetCameraDisplayName(deviceId, displayName);
			this.LogInformation("SetCameraDisplayName deviceId=\"{DeviceId}\" name=\"{Name}\" result={Code}", deviceId, displayName, code);
			return code == 0;
		}

		private void CameraNameConsoleCommand(string args)
		{
			var text = (args ?? string.Empty).Trim();
			var cameras = _controller.GetCameras();

			if (text.Length == 0 || text.Equals("list", System.StringComparison.OrdinalIgnoreCase))
			{
				if (cameras.Length == 0)
				{
					CrestronConsole.ConsoleCommandResponse("No cameras reported (is the Zoom Room connected?)\r\n");
					return;
				}

				for (var i = 0; i < cameras.Length; i++)
				{
					var c = cameras[i];
					CrestronConsole.ConsoleCommandResponse("{0}: displayName=\"{1}\" name=\"{2}\" alias=\"{3}\" selected={4} multiCamera={5} id=\"{6}\"\r\n",
						i + 1, c.DisplayName, c.Name, c.Alias, c.IsSelected, c.IsSelectedAsMultiDevice, c.Id);
				}
				return;
			}

			var space = text.IndexOf(' ');
			int number;
			if (space < 0 || !int.TryParse(text.Substring(0, space), out number) || number < 1 || number > cameras.Length)
			{
				CrestronConsole.ConsoleCommandResponse("Usage: zoomCameraName [deviceKey] list | <n> <name>  (n from the list, 1-{0})\r\n", cameras.Length);
				return;
			}

			var name = text.Substring(space + 1).Trim();
			var ok = SetCameraDisplayName(cameras[number - 1].Id, name);
			CrestronConsole.ConsoleCommandResponse("Camera {0} -> \"{1}\": {2}\r\n", number, name, ok ? "ok" : "failed (see log)");
		}
	}
}
