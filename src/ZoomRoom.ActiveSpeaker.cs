using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Crestron.SimplSharp;
using PepperDash.Core.Logging;
using PepperDash.ZoomRoom.Sdk.EventArgs;

namespace PepperDash.Essentials.Plugins
{
	/// <summary>
	/// Multi-camera active speaker. In multi-camera mode each of the room's cameras is its own tile in
	/// the meeting: tile 1 is the room itself, and every further tile is a child participant whose
	/// ParentUserID is the room's user ID (Zoom names them "Room - 2", "Room - 3", ...). A controller
	/// that knows who is talking picks the tile to show as active speaker with
	/// <see cref="SetActiveSpeakerCamera"/>.
	/// </summary>
	public partial class ZoomRoom
	{
		private static readonly Regex CameraTileSuffix = new Regex(@"\s-\s(\d+)\s*$");

		/// <summary>
		/// The room's camera tiles in tile order: index 0 is the room itself (tile 1), then its child
		/// tiles ordered by the number Zoom appends to their name, or by user ID when there is none.
		/// Empty when not in a meeting.
		/// </summary>
		public List<ParticipantInfo> GetCameraTiles()
		{
			lock (_participantLock)
			{
				var self = _participantInfoByUserId.Values.FirstOrDefault(p => p.IsMySelf);
				if (self == null) return new List<ParticipantInfo>();

				var tiles = new List<ParticipantInfo> { self };
				tiles.AddRange(_participantInfoByUserId.Values
					.Where(p => !p.IsMySelf && p.ParentUserID != 0 && p.ParentUserID == self.UserID)
					.OrderBy(p => CameraTileNumber(p))
					.ThenBy(p => p.UserID));
				return tiles;
			}
		}

		private static int CameraTileNumber(ParticipantInfo info)
		{
			var match = CameraTileSuffix.Match(info.UserName ?? string.Empty);
			return match.Success ? int.Parse(match.Groups[1].Value) : int.MaxValue;
		}

		/// <summary>
		/// Makes camera tile <paramref name="tile"/> (1 = the room itself, 2.. = its extra camera tiles)
		/// the meeting's active speaker. Returns false when not in a meeting, when the tile does not
		/// exist, or when the SDK rejects the call.
		/// </summary>
		public bool SetActiveSpeakerCamera(int tile)
		{
			if (!IsInCall)
			{
				this.LogDebug("SetActiveSpeakerCamera({Tile}) ignored: not in a meeting", tile);
				return false;
			}

			var tiles = GetCameraTiles();
			if (tile < 1 || tile > tiles.Count)
			{
				this.LogWarning("SetActiveSpeakerCamera({Tile}) ignored: the room has {Count} camera tile(s)", tile, tiles.Count);
				return false;
			}

			var target = tiles[tile - 1];
			var code = tile == 1
				? _controller.SetMySelfAsActiveSpeaker()
				: _controller.SetMyChildAsActiveSpeaker(target.UserID);

			this.LogDebug("SetActiveSpeakerCamera tile={Tile} userId={UserId} name=\"{Name}\" result={Code}",
				tile, target.UserID, target.UserName, code);
			return code == 0;
		}

		private void ActiveSpeakerConsoleCommand(string args)
		{
			var arg = (args ?? string.Empty).Trim();

			int tile;
			if (int.TryParse(arg, out tile))
			{
				CrestronConsole.ConsoleCommandResponse("Set camera tile {0} as active speaker: {1}\r\n",
					tile, SetActiveSpeakerCamera(tile) ? "ok" : "failed (see log)");
				return;
			}

			var tiles = GetCameraTiles();
			if (tiles.Count == 0)
			{
				CrestronConsole.ConsoleCommandResponse("No camera tiles - the room is not in a meeting\r\n");
				return;
			}

			for (var i = 0; i < tiles.Count; i++)
				CrestronConsole.ConsoleCommandResponse("tile {0}: userId={1} parent={2} name=\"{3}\"\r\n",
					i + 1, tiles[i].UserID, tiles[i].ParentUserID, tiles[i].UserName);
		}
	}
}
