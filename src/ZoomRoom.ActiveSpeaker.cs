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

		// How long after this plugin turns a camera on for multi-camera its tile may join and be taken
		// as that camera (seen on hardware: about a second).
		private const int CameraTileJoinWindowMs = 10000;

		// The camera input each child tile shows, learned when the tile joined just after this plugin
		// turned that input's camera on. Zoom's number on a child is not tied to a camera. Guarded by
		// _participantLock.
		private readonly Dictionary<int, string> _cameraTileInputByUserId = new Dictionary<int, string>();

		// Camera inputs turned on for multi-camera whose tile has not joined yet, oldest first.
		// Guarded by _participantLock.
		private readonly List<KeyValuePair<string, DateTime>> _expectedCameraTileInputs = new List<KeyValuePair<string, DateTime>>();

		/// <summary>
		/// The room's camera tiles by tile number: index 0 is the room itself (tile 1), index N - 1 the
		/// camera on the codec's Nth camera input (usbInN). A child tile whose camera input is known
		/// takes that input's tile; the others are placed by the number Zoom appended to their name
		/// (see <see cref="CameraTileLayout"/>). An entry is null for a tile with no camera in the
		/// meeting. Empty when not in a meeting.
		/// </summary>
		public List<ParticipantInfo> GetCameraTiles()
		{
			lock (_participantLock)
			{
				var self = _participantInfoByUserId.Values.FirstOrDefault(p => p.IsMySelf);
				if (self == null) return new List<ParticipantInfo>();

				var children = _participantInfoByUserId.Values
					.Where(p => !p.IsMySelf && p.ParentUserID != 0 && p.ParentUserID == self.UserID)
					.ToList();
				var tileByUser = CameraTileLayout.Assign(children.Select(p =>
					new CameraTileLayout.Child(p.UserID, CameraTileNumber(p), KnownCameraTile(p.UserID))));

				var tiles = new List<ParticipantInfo>(new ParticipantInfo[tileByUser.Count == 0 ? 1 : tileByUser.Values.Max()]);
				tiles[0] = self;
				foreach (var child in children)
					tiles[tileByUser[child.UserID] - 1] = child;
				return tiles;
			}
		}

		// Caller holds _participantLock.
		private int KnownCameraTile(int userId)
		{
			string input;
			return _cameraTileInputByUserId.TryGetValue(userId, out input) ? CameraInputTile(input) : 0;
		}

		// 1 for usbIn1 .. 4 for usbIn4; 0 for anything else.
		private int CameraInputTile(string inputKey)
		{
			var inputs = new[] { CamInput1, CamInput2, CamInput3, CamInput4 };
			for (var i = 0; i < inputs.Length; i++)
				if (inputs[i] != null && string.Equals(inputs[i].Key, inputKey, StringComparison.OrdinalIgnoreCase))
					return i + 1;
			return 0;
		}

		private void ForgetExpectedCameraTile(string inputKey)
		{
			if (string.IsNullOrEmpty(inputKey)) return;
			lock (_participantLock)
				_expectedCameraTileInputs.RemoveAll(e => string.Equals(e.Key, inputKey, StringComparison.OrdinalIgnoreCase));
		}

		/// <summary>
		/// The next child tile to join is the camera on <paramref name="inputKey"/> - called just before
		/// that camera is turned on for multi-camera in a meeting.
		/// </summary>
		private void ExpectCameraTile(string inputKey)
		{
			if (!IsInCall || string.IsNullOrEmpty(inputKey)) return;
			lock (_participantLock)
			{
				// Already in the meeting: selecting it again brings no new tile.
				if (_cameraTileInputByUserId.Values.Any(v => string.Equals(v, inputKey, StringComparison.OrdinalIgnoreCase))) return;

				_expectedCameraTileInputs.RemoveAll(e => string.Equals(e.Key, inputKey, StringComparison.OrdinalIgnoreCase));
				_expectedCameraTileInputs.Add(new KeyValuePair<string, DateTime>(inputKey, DateTime.UtcNow.AddMilliseconds(CameraTileJoinWindowMs)));
			}
		}

		/// <summary>
		/// Gives each child tile among <paramref name="joinedUserIds"/> (participants new to the
		/// meeting) the oldest camera input still expecting its tile. Caller holds _participantLock.
		/// </summary>
		private void LearnCameraTileInputs(IEnumerable<int> joinedUserIds)
		{
			var now = DateTime.UtcNow;
			_expectedCameraTileInputs.RemoveAll(e => e.Value < now);
			if (_expectedCameraTileInputs.Count == 0) return;

			var self = _participantInfoByUserId.Values.FirstOrDefault(p => p.IsMySelf);
			if (self == null) return;

			foreach (var userId in joinedUserIds.OrderBy(id => id))
			{
				ParticipantInfo info;
				if (_expectedCameraTileInputs.Count == 0) break;
				if (!_participantInfoByUserId.TryGetValue(userId, out info) || info.IsMySelf || info.ParentUserID != self.UserID) continue;

				var input = _expectedCameraTileInputs[0].Key;
				_expectedCameraTileInputs.RemoveAt(0);
				_cameraTileInputByUserId[userId] = input;
				this.LogInformation("Multi-camera: tile userId={UserId} \"{Name}\" is the camera on input '{Input}' (tile {Tile})",
					userId, info.UserName, input, CameraInputTile(input));
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
			if (tile < 1 || tile > tiles.Count || tiles[tile - 1] == null)
			{
				this.LogWarning("SetActiveSpeakerCamera({Tile}) ignored: that camera tile is not in the meeting ({Count} tile(s))",
					tile, tiles.Count(t => t != null));
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
				if (tiles[i] == null)
				{
					CrestronConsole.ConsoleCommandResponse("tile {0}: no camera in the meeting\r\n", i + 1);
					continue;
				}

				string zoomName, input;
				lock (_participantLock)
				{
					if (!_cameraTileZoomNameByUserId.TryGetValue(tiles[i].UserID, out zoomName)) zoomName = tiles[i].UserName;
					if (!_cameraTileInputByUserId.TryGetValue(tiles[i].UserID, out input)) input = i == 0 ? "room" : "by Zoom number";
				}
				CrestronConsole.ConsoleCommandResponse("tile {0}: userId={1} parent={2} name=\"{3}\" zoomName=\"{4}\" input={5} guid=\"{6}\" jid=\"{7}\"\r\n",
					i + 1, tiles[i].UserID, tiles[i].ParentUserID, tiles[i].UserName, zoomName, input, tiles[i].UserGUID, tiles[i].AttendeeJid);
			}
		}
	}
}
