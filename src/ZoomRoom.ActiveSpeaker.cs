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
	/// ParentUserID is the room's user ID (Zoom names them after the room with a number, e.g.
	/// "JSC Lab Room 1"). A controller that knows who is talking picks the tile to show as active
	/// speaker with <see cref="SetActiveSpeakerCamera"/>.
	/// </summary>
	public partial class ZoomRoom
	{
		// "Room 2" (seen on hardware) or "Room - 2".
		private static readonly Regex CameraTileSuffix = new Regex(@"\s(?:-\s)?(\d+)\s*$");

		// Each child tile's number from the name Zoom gave it, kept for the meeting so a renamed tile
		// keeps its place. Guarded by _participantLock.
		private readonly Dictionary<int, int> _cameraTileNumberByUserId = new Dictionary<int, int>();

		// The name Zoom gave each child tile when it joined, before any rename - shown by
		// "zoomActiveSpeaker list" to work out which camera a tile is. Guarded by _participantLock.
		private readonly Dictionary<int, string> _cameraTileZoomNameByUserId = new Dictionary<int, string>();

		/// <summary>
		/// The room's camera tiles in tile order: index 0 is the room itself (tile 1), then its child
		/// tiles ordered by the number Zoom appended to their name when they joined, or by user ID when
		/// there is none. Empty when not in a meeting.
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

		// Caller holds _participantLock.
		private int CameraTileNumber(ParticipantInfo info)
		{
			int number;
			if (_cameraTileNumberByUserId.TryGetValue(info.UserID, out number)) return number;

			var match = CameraTileSuffix.Match(info.UserName ?? string.Empty);
			number = match.Success ? int.Parse(match.Groups[1].Value) : int.MaxValue;
			_cameraTileNumberByUserId[info.UserID] = number;
			_cameraTileZoomNameByUserId[info.UserID] = info.UserName ?? string.Empty;
			return number;
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
			{
				string zoomName;
				lock (_participantLock)
					if (!_cameraTileZoomNameByUserId.TryGetValue(tiles[i].UserID, out zoomName)) zoomName = tiles[i].UserName;
				CrestronConsole.ConsoleCommandResponse("tile {0}: userId={1} parent={2} name=\"{3}\" zoomName=\"{4}\" guid=\"{5}\" jid=\"{6}\"\r\n",
					i + 1, tiles[i].UserID, tiles[i].ParentUserID, tiles[i].UserName, zoomName, tiles[i].UserGUID, tiles[i].AttendeeJid);
			}
		}
	}
}
