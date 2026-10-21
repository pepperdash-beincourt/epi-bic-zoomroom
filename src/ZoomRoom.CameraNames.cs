using Crestron.SimplSharp;
using PepperDash.Core.Logging;

namespace PepperDash.Essentials.Plugins
{
	/// <summary>
	/// Camera display names in the Zoom Room's settings (persistent across meetings and restarts).
	/// Console: "zoomCameraName [deviceKey] list" numbers the room's cameras; "zoomCameraName
	/// [deviceKey] &lt;n&gt; &lt;name&gt;" renames camera n. That name is NOT what the far end sees on a
	/// multi-camera tile (checked on hardware): those tiles are the room's child participants, named
	/// after the room. "zoomRenameTile [deviceKey] list | &lt;tile&gt; &lt;name&gt;" renames a tile for the
	/// current meeting instead (spike).
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

		/// <summary>
		/// Renames camera tile <paramref name="tile"/> (1 = the room itself, 2.. = its extra camera tiles,
		/// as in <see cref="GetCameraTiles"/>) for the current meeting - the name the far end sees on that
		/// tile. Spike: Zoom may refuse it for a multi-camera tile (402) or without host/co-host. Returns
		/// the SDK result code, or -1 when not in a meeting or the tile does not exist.
		/// </summary>
		public int RenameCameraTile(int tile, string name)
		{
			var tiles = GetCameraTiles();
			if (!IsInCall || tile < 1 || tile > tiles.Count)
			{
				this.LogWarning("RenameCameraTile({Tile}) ignored: in a meeting={InCall}, the room has {Count} camera tile(s)", tile, IsInCall, tiles.Count);
				return -1;
			}

			var target = tiles[tile - 1];
			var code = _controller.RenameUser(target.UserID, name);
			this.LogInformation("RenameCameraTile tile={Tile} userId={UserId} \"{From}\" -> \"{To}\" result={Code} (host={IsHost} cohost={IsCoHost})",
				tile, target.UserID, target.UserName, name, code, _sdkIsHost, _sdkIsCoHost);
			return code;
		}

		private void RenameTileConsoleCommand(string args)
		{
			var text = (args ?? string.Empty).Trim();
			var tiles = GetCameraTiles();

			if (text.Length == 0 || text.Equals("list", System.StringComparison.OrdinalIgnoreCase))
			{
				if (tiles.Count == 0)
				{
					CrestronConsole.ConsoleCommandResponse("No camera tiles - the room is not in a meeting\r\n");
					return;
				}

				for (var i = 0; i < tiles.Count; i++)
					CrestronConsole.ConsoleCommandResponse("tile {0}: userId={1} name=\"{2}\"\r\n", i + 1, tiles[i].UserID, tiles[i].UserName);
				return;
			}

			var space = text.IndexOf(' ');
			int tile;
			if (space < 0 || !int.TryParse(text.Substring(0, space), out tile))
			{
				CrestronConsole.ConsoleCommandResponse("Usage: zoomRenameTile [deviceKey] list | <tile> <name>  (tile from the list)\r\n");
				return;
			}

			var name = text.Substring(space + 1).Trim();
			var code = RenameCameraTile(tile, name);
			CrestronConsole.ConsoleCommandResponse("Tile {0} -> \"{1}\": result {2}{3}\r\n", tile, name, code,
				code == 0 ? " (ok)" : code == 402 ? " (Zoom does not allow this for a multi-camera tile)" : " (see log)");
		}
	}
}
