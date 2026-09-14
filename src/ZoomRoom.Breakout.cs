using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using PepperDash.Core.Logging;
using PepperDash.ZoomRoom.Sdk;
using PepperDash.ZoomRoom.Sdk.EventArgs;

namespace PepperDash.Essentials.Plugins
{
	/// <summary>A breakout room and who is assigned to / currently in it.</summary>
	public class BreakoutRoomState
	{
		[JsonProperty("id")] public string Id { get; set; }
		[JsonProperty("name")] public string Name { get; set; }
		[JsonProperty("participants")] public List<BreakoutParticipantState> Participants { get; set; } = new List<BreakoutParticipantState>();
	}

	/// <summary>A meeting participant as the breakout page sees them.</summary>
	public class BreakoutParticipantState
	{
		[JsonProperty("userId")] public int UserId { get; set; }
		[JsonProperty("userGuid")] public string UserGuid { get; set; }
		[JsonProperty("name")] public string Name { get; set; }
		[JsonProperty("isMyself")] public bool IsMyself { get; set; }
		[JsonProperty("isHost")] public bool IsHost { get; set; }
		[JsonProperty("isCoHost")] public bool IsCoHost { get; set; }
		/// <summary>Room this participant is assigned to (empty = unassigned).</summary>
		[JsonProperty("roomId")] public string RoomId { get; set; }
		/// <summary>inMain | inRoom | left | unknown</summary>
		[JsonProperty("status")] public string Status { get; set; }
	}

	/// <summary>Editable breakout options (mirrors the SDK's BOOptions; read-only fields included for display).</summary>
	public class BreakoutOptionsState
	{
		[JsonProperty("participantsCanChooseRoom")] public bool ParticipantsCanChooseRoom { get; set; }
		[JsonProperty("participantsCanReturnToMain")] public bool ParticipantsCanReturnToMain { get; set; } = true;
		[JsonProperty("autoMoveAssignedParticipants")] public bool AutoMoveAssignedParticipants { get; set; }
		[JsonProperty("timerEnabled")] public bool TimerEnabled { get; set; }
		[JsonProperty("timerMinutes")] public int TimerMinutes { get; set; } = 30;
		[JsonProperty("notifyWhenTimeIsUp")] public bool NotifyWhenTimeIsUp { get; set; }
		/// <summary>0 none, 10, 15, 30, 60, 120 seconds.</summary>
		[JsonProperty("countdownSeconds")] public int CountdownSeconds { get; set; } = 60;
		[JsonProperty("preAssignEnabled")] public bool PreAssignEnabled { get; set; }
		[JsonProperty("maxRoomCount")] public int MaxRoomCount { get; set; } = 100;
	}

	/// <summary>
	/// Breakout rooms. Status, room list, options, who is where, and every host action the SDK offers:
	/// create / add / rename / delete rooms, assign and move participants, open / close, broadcast,
	/// invite back, visit a room, answer help requests; plus this room's own join / return / ask for help.
	/// See ZoomRoomBreakoutMessenger for the Mobile Control contract.
	/// </summary>
	public partial class ZoomRoom
	{
		private readonly object _breakoutLock = new object();
		private string _sdkBreakoutStatus = "unknown";
		private List<BORoom> _boRooms = new List<BORoom>();
		private readonly Dictionary<string, ParticipantInfo> _boRosterByGuid = new Dictionary<string, ParticipantInfo>();
		private BreakoutOptionsState _boOptions;
		private string _boMyStatus = "unknown";
		private string _boMyRoomId = string.Empty;
		private int _boTimerRemaining = -1;

		/// <summary>Raised when anything the breakout page shows changes.</summary>
		public event EventHandler BreakoutChanged;

		/// <summary>unknown | invalid | edit | started | stopping | ended</summary>
		public string BreakoutStatus => _sdkBreakoutStatus;
		public string BreakoutMyStatus => _boMyStatus;
		public string BreakoutMyRoomId => _boMyRoomId;
		public int BreakoutTimerRemaining => _boTimerRemaining;
		public BreakoutOptionsState BreakoutOptions => _boOptions;

		private void SubscribeBreakoutEvents()
		{
			_controller.BreakoutStatusChanged += (s, e) => ApplyBreakoutStatus(e?.ErrorCode ?? 0, "SDK");
			_controller.BreakoutRoomListUpdated += (s, rooms) =>
			{
				lock (_breakoutLock) _boRooms = (rooms ?? Array.Empty<BORoom>()).ToList();
				this.LogDebug("Breakout room list: {Count} room(s)", rooms?.Length ?? 0);
				BreakoutChanged?.Invoke(this, EventArgs.Empty);
			};
			_controller.BreakoutOptionsChanged += (s, o) =>
			{
				_boOptions = ToOptionsState(o);
				BreakoutChanged?.Invoke(this, EventArgs.Empty);
			};
			_controller.BreakoutUserStatusChanged += (s, e) =>
			{
				_boMyStatus = UserStatusText(e?.ErrorCode ?? -1);
				_boMyRoomId = e?.Message ?? string.Empty;
				this.LogInformation("Breakout: this room is {Status} {Room}", _boMyStatus, _boMyRoomId);
				BreakoutChanged?.Invoke(this, EventArgs.Empty);
			};
			_controller.BreakoutTimerTick += (s, e) =>
			{
				_boTimerRemaining = e?.ErrorCode ?? -1;
				BreakoutChanged?.Invoke(this, EventArgs.Empty);
			};
			_controller.BreakoutParticipantsUpdated += (s, e) => ApplyBreakoutRoster(e);
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
			if (text == "ended" || text == "invalid") _boTimerRemaining = -1;
			BreakoutChanged?.Invoke(this, EventArgs.Empty);
			// The SDK does not always push the room list on a status change; ask.
			if (source == "SDK" && (text == "edit" || text == "started")) RefreshBreakout();
		}

		private void ApplyBreakoutRoster(BOParticipantListEventArgs e)
		{
			if (e == null) return;
			lock (_breakoutLock)
			{
				if (e.EventType == 1) _boRosterByGuid.Clear();
				foreach (var p in e.Participants ?? Array.Empty<ParticipantInfo>())
				{
					if (string.IsNullOrEmpty(p.UserGUID)) continue;
					if (e.EventType == 2) _boRosterByGuid.Remove(p.UserGUID);
					else _boRosterByGuid[p.UserGUID] = p;
				}
			}
			BreakoutChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>Called after every main-roster change so assignment changes reach the page.</summary>
		private void RefreshBreakoutRoster()
		{
			if (_sdkBreakoutStatus == "unknown" || _sdkBreakoutStatus == "invalid") return;
			BreakoutChanged?.Invoke(this, EventArgs.Empty);
		}

		private void ResetBreakout()
		{
			var changed = _sdkBreakoutStatus != "unknown" || _boRooms.Count > 0 || _boRosterByGuid.Count > 0 || _boOptions != null;
			_sdkBreakoutStatus = "unknown";
			_boMyStatus = "unknown";
			_boMyRoomId = string.Empty;
			_boTimerRemaining = -1;
			_boOptions = null;
			lock (_breakoutLock)
			{
				_boRooms = new List<BORoom>();
				_boRosterByGuid.Clear();
			}
			if (changed) BreakoutChanged?.Invoke(this, EventArgs.Empty);
		}

		// ── Snapshot for the messenger ─────────────────────────────────────────

		/// <summary>Rooms with their assigned participants, plus everyone not assigned to a room.</summary>
		public void GetBreakoutSnapshot(out List<BreakoutRoomState> rooms, out List<BreakoutParticipantState> unassigned)
		{
			List<BORoom> roomList;
			Dictionary<string, ParticipantInfo> boRoster;
			List<ParticipantInfo> mainRoster;
			lock (_breakoutLock)
			{
				roomList = _boRooms.ToList();
				boRoster = new Dictionary<string, ParticipantInfo>(_boRosterByGuid);
			}
			lock (_participantLock)
				mainRoster = _participantInfoByUserId.Values.ToList();

			// The BO roster (host only) is authoritative when present; the main roster carries the same
			// assignment on BoSessionBID / BoUserStatus for everyone else.
			var people = new Dictionary<string, BreakoutParticipantState>();
			foreach (var p in mainRoster)
			{
				if (string.IsNullOrEmpty(p.UserGUID)) continue;
				people[p.UserGUID] = ToBreakoutParticipant(p);
			}
			foreach (var kv in boRoster)
				people[kv.Key] = ToBreakoutParticipant(kv.Value);

			rooms = roomList.Select(r => new BreakoutRoomState
			{
				Id = r.SessionBID,
				Name = r.SessionName,
				Participants = people.Values.Where(x => x.RoomId == r.SessionBID).OrderBy(x => x.Name).ToList(),
			}).ToList();
			var roomIds = new HashSet<string>(roomList.Select(r => r.SessionBID));
			unassigned = people.Values.Where(x => string.IsNullOrEmpty(x.RoomId) || !roomIds.Contains(x.RoomId)).OrderBy(x => x.Name).ToList();
		}

		private static BreakoutParticipantState ToBreakoutParticipant(ParticipantInfo p) => new BreakoutParticipantState
		{
			UserId = p.UserID,
			UserGuid = p.UserGUID ?? string.Empty,
			Name = p.UserName ?? string.Empty,
			IsMyself = p.IsMySelf,
			IsHost = p.IsHost,
			IsCoHost = p.IsCohost,
			RoomId = p.BoSessionBID ?? string.Empty,
			Status = UserStatusText((int)p.BoUserStatus),
		};

		private static string UserStatusText(int status)
		{
			switch (status)
			{
				case 1: return "inMain";
				case 2: return "inRoom";
				case 3: return "left";
				default: return "unknown";
			}
		}

		private static BreakoutOptionsState ToOptionsState(BOOptionsInfo o) => o == null ? null : new BreakoutOptionsState
		{
			ParticipantsCanChooseRoom = o.IsParticipantCanChooseRoom,
			ParticipantsCanReturnToMain = o.IsParticipantCanReturnToMainSessionAtAnyTime,
			AutoMoveAssignedParticipants = o.IsAutoMoveAllAssignedParticipantsEnabled,
			TimerEnabled = o.IsBOTimerEnabled,
			TimerMinutes = (int)Math.Max(1, (o.IsBOTimerEnabled && o.BOTimerDuration > 0 ? o.BOTimerDuration : o.DefaultBOTimerDuration) / 60),
			NotifyWhenTimeIsUp = o.IsNotifyMeWhenTimeIsUp,
			CountdownSeconds = CountdownToSeconds(o.CountdownSeconds),
			PreAssignEnabled = o.IsPreAssignEnabled,
			MaxRoomCount = o.MaxRoomCount,
		};

		private static int CountdownToSeconds(BOStopCountdown c)
		{
			switch (c)
			{
				case BOStopCountdown.Seconds10: return 10;
				case BOStopCountdown.Seconds15: return 15;
				case BOStopCountdown.Seconds30: return 30;
				case BOStopCountdown.Seconds60: return 60;
				case BOStopCountdown.Seconds120: return 120;
				default: return 0;
			}
		}

		private static BOStopCountdown SecondsToCountdown(int s)
		{
			if (s >= 120) return BOStopCountdown.Seconds120;
			if (s >= 60) return BOStopCountdown.Seconds60;
			if (s >= 30) return BOStopCountdown.Seconds30;
			if (s >= 15) return BOStopCountdown.Seconds15;
			if (s >= 10) return BOStopCountdown.Seconds10;
			return BOStopCountdown.None;
		}

		// ── Host actions ───────────────────────────────────────────────────────

		/// <summary>Asks the SDK for the room list, roster and options (used when the page opens).</summary>
		public void RefreshBreakout()
		{
			_controller.RequestBreakoutRoomList();
			_controller.RequestBreakoutRoomUserList();
			_controller.RequestBOOptions();
		}

		/// <summary>assignType: 0 automatically, 1 manually, 2 let participants choose.</summary>
		public void CreateBreakoutRooms(int count, int assignType)
		{
			this.LogInformation("CreateBreakoutRooms count={Count} assign={Assign}", count, assignType);
			_controller.CreateBreakoutRooms(Math.Max(1, count), assignType);
		}

		public void AddBreakoutRoom()
		{
			this.LogInformation("AddBreakoutRoom");
			_controller.AddBreakoutRoom();
		}

		public void DeleteBreakoutRoom(string roomId)
		{
			if (string.IsNullOrEmpty(roomId)) return;
			this.LogInformation("DeleteBreakoutRoom {Room}", roomId);
			_controller.DeleteBreakoutRoom(roomId);
		}

		public void RenameBreakoutRoom(string roomId, string name)
		{
			if (string.IsNullOrEmpty(roomId) || string.IsNullOrWhiteSpace(name)) return;
			this.LogInformation("RenameBreakoutRoom {Room} -> \"{Name}\"", roomId, name);
			_controller.RenameBreakoutRoom(roomId, name.Trim());
		}

		/// <summary>Assigns (rooms being edited) or moves (rooms open) participants to a room.</summary>
		public void AssignToBreakoutRoom(IEnumerable<string> userGuids, string roomId)
		{
			var guids = (userGuids ?? Enumerable.Empty<string>()).Where(g => !string.IsNullOrEmpty(g)).ToList();
			if (guids.Count == 0 || string.IsNullOrEmpty(roomId)) return;
			if (_sdkBreakoutStatus == "started")
			{
				this.LogInformation("MoveToBreakoutRoom {Count} participant(s) -> {Room}", guids.Count, roomId);
				foreach (var g in guids) _controller.MoveUserToBreakoutRoom(g, roomId);
			}
			else
			{
				this.LogInformation("AssignToBreakoutRoom {Count} participant(s) -> {Room}", guids.Count, roomId);
				_controller.AssignUsersToBreakoutRoom(guids, roomId);
			}
		}

		public void InviteBackToMainSession(string userGuid)
		{
			if (string.IsNullOrEmpty(userGuid)) return;
			this.LogInformation("InviteBackToMainSession {Guid}", userGuid);
			_controller.InviteBOUserReturnToMainSession(userGuid);
		}

		/// <summary>Host / co-host visits a specific room.</summary>
		public void JoinBreakoutRoomById(string roomId)
		{
			if (string.IsNullOrEmpty(roomId)) return;
			this.LogInformation("JoinBreakoutRoomById {Room}", roomId);
			_controller.JoinBreakoutRoomByBID(roomId);
		}

		public void SetBreakoutOptions(BreakoutOptionsState o)
		{
			if (o == null) return;
			this.LogInformation("SetBreakoutOptions choose={Choose} return={Return} autoMove={Auto} timer={Timer}/{Minutes}m notify={Notify} countdown={Countdown}s",
				o.ParticipantsCanChooseRoom, o.ParticipantsCanReturnToMain, o.AutoMoveAssignedParticipants, o.TimerEnabled, o.TimerMinutes, o.NotifyWhenTimeIsUp, o.CountdownSeconds);
			_controller.SetBOOptions(new BOOptionsInfo
			{
				IsParticipantCanChooseRoom = o.ParticipantsCanChooseRoom,
				IsParticipantCanReturnToMainSessionAtAnyTime = o.ParticipantsCanReturnToMain,
				IsAutoMoveAllAssignedParticipantsEnabled = o.AutoMoveAssignedParticipants,
				IsBOTimerEnabled = o.TimerEnabled,
				BOTimerDuration = Math.Max(1, o.TimerMinutes) * 60L,
				IsNotifyMeWhenTimeIsUp = o.NotifyWhenTimeIsUp,
				CountdownSeconds = SecondsToCountdown(o.CountdownSeconds),
			});
			_controller.RequestBOOptions();
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

		// ── This room ──────────────────────────────────────────────────────────

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

		// ── Test hooks ─────────────────────────────────────────────────────────

		/// <summary>
		/// <c>devjson:1 {"deviceKey":"zoomRoom","methodName":"SimulateBreakoutStatus","params":[2]}</c>
		/// (1 edit, 2 started, 3 stopping, 4 ended, 0 invalid). Also seeds two fake rooms so the page
		/// has something to show; the fake rooms clear on the next real room list or meeting reset.
		/// </summary>
		public void SimulateBreakoutStatus(int status)
		{
			this.LogWarning("SIMULATED breakout status (console test hook): {Status}", status);
			lock (_breakoutLock)
			{
				if (_boRooms.Count == 0 && status != 0 && status != 4)
					_boRooms = new List<BORoom>
					{
						new BORoom { SessionBID = "sim-1", SessionName = "Room 1" },
						new BORoom { SessionBID = "sim-2", SessionName = "Room 2" },
					};
			}
			ApplyBreakoutStatus(status, "simulated");
			BreakoutChanged?.Invoke(this, EventArgs.Empty);
		}
	}
}
