using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PepperDash.Core.Logging;
using PepperDash.Essentials.AppServer;
using PepperDash.Essentials.Plugins;
using ZoomRoomDevice = PepperDash.Essentials.Plugins.ZoomRoom;

namespace PepperDash.Essentials.AppServer.Messengers
{
    /// <summary>
    /// Mobile Control messenger for breakout rooms (see ZoomRoom.Breakout.cs).
    /// Status: <c>{ breakoutStatus, breakoutRooms: [{id, name, participants: [...]}], breakoutUnassigned: [...],
    /// breakoutOptions, breakoutMyStatus, breakoutMyRoomId, breakoutTimerRemaining }</c>.
    /// Actions (host): /refreshBreakout, /createBreakoutRooms {count, assignType}, /addBreakoutRoom,
    /// /deleteBreakoutRoom {value: id}, /renameBreakoutRoom {id, name}, /assignToBreakoutRoom {userGuids: [], roomId},
    /// /inviteBackToMain {value: guid}, /joinBreakoutRoomById {value: id}, /setBreakoutOptions {...},
    /// /startBreakoutRooms, /stopBreakoutRooms, /broadcastToBreakoutRooms {value}.
    /// Actions (this room): /joinBreakoutRoom, /leaveBreakoutRoom, /askForHelpInBreakoutRoom.
    /// </summary>
    public class ZoomRoomBreakoutMessenger : MessengerBase
    {
        private readonly ZoomRoomDevice _codec;

        public ZoomRoomBreakoutMessenger(string key, string messagePath, ZoomRoomDevice codec)
            : base(key, messagePath, codec)
        {
            _codec = codec ?? throw new ArgumentNullException(nameof(codec));
        }

        protected override void RegisterActions()
        {
            base.RegisterActions();

            AddAction("/fullStatus", (id, content) => SendFullStatus(id));
            AddAction("/refreshBreakout", (id, content) => _codec.RefreshBreakout());

            AddAction("/createBreakoutRooms", (id, content) =>
            {
                var c = content?.ToObject<CreateRoomsContent>();
                if (c != null) _codec.CreateBreakoutRooms(c.Count, c.AssignType);
            });
            AddAction("/addBreakoutRoom", (id, content) => _codec.AddBreakoutRoom());
            AddAction("/deleteBreakoutRoom", (id, content) =>
            {
                var s = content?.ToObject<MobileControlSimpleContent<string>>();
                if (s != null) _codec.DeleteBreakoutRoom(s.Value);
            });
            AddAction("/renameBreakoutRoom", (id, content) =>
            {
                var c = content?.ToObject<RenameRoomContent>();
                if (c != null) _codec.RenameBreakoutRoom(c.Id, c.Name);
            });
            AddAction("/assignToBreakoutRoom", (id, content) =>
            {
                var c = content?.ToObject<AssignContent>();
                if (c != null) _codec.AssignToBreakoutRoom(c.UserGuids, c.RoomId);
            });
            AddAction("/inviteBackToMain", (id, content) =>
            {
                var s = content?.ToObject<MobileControlSimpleContent<string>>();
                if (s != null) _codec.InviteBackToMainSession(s.Value);
            });
            AddAction("/joinBreakoutRoomById", (id, content) =>
            {
                var s = content?.ToObject<MobileControlSimpleContent<string>>();
                if (s != null) _codec.JoinBreakoutRoomById(s.Value);
            });
            AddAction("/setBreakoutOptions", (id, content) =>
            {
                var o = content?.ToObject<BreakoutOptionsState>();
                if (o != null) _codec.SetBreakoutOptions(o);
            });

            AddAction("/startBreakoutRooms", (id, content) => _codec.StartBreakoutRooms());
            AddAction("/stopBreakoutRooms", (id, content) => _codec.StopBreakoutRooms());
            AddAction("/broadcastToBreakoutRooms", (id, content) =>
            {
                var s = content?.ToObject<MobileControlSimpleContent<string>>();
                if (!string.IsNullOrWhiteSpace(s?.Value)) _codec.BroadcastToBreakoutRooms(s.Value);
            });

            AddAction("/joinBreakoutRoom", (id, content) => _codec.JoinAssignedBreakoutRoom());
            AddAction("/leaveBreakoutRoom", (id, content) => _codec.LeaveBreakoutRoom());
            AddAction("/askForHelpInBreakoutRoom", (id, content) => _codec.AskForHelpInBreakoutRoom());

            _codec.BreakoutChanged += (s, e) => Task.Run(() => PostStatusMessage(BuildStatus()));
        }

        private void SendFullStatus(string clientId = null)
        {
            Task.Run(() =>
            {
                try { PostStatusMessage(BuildStatus(), clientId); }
                catch (Exception ex) { this.LogError(ex, "Error sending ZoomRoom breakout status"); }
            });
        }

        private ZoomRoomBreakoutStateMessage BuildStatus()
        {
            _codec.GetBreakoutSnapshot(out var rooms, out var unassigned);
            return new ZoomRoomBreakoutStateMessage
            {
                BreakoutStatus = _codec.BreakoutStatus,
                BreakoutRooms = rooms,
                BreakoutUnassigned = unassigned,
                BreakoutOptions = _codec.BreakoutOptions,
                BreakoutMyStatus = _codec.BreakoutMyStatus,
                BreakoutMyRoomId = _codec.BreakoutMyRoomId,
                BreakoutTimerRemaining = _codec.BreakoutTimerRemaining,
            };
        }
    }

    public class ZoomRoomBreakoutStateMessage : DeviceStateMessageBase
    {
        [JsonProperty("breakoutStatus")] public string BreakoutStatus { get; set; }
        [JsonProperty("breakoutRooms")] public List<BreakoutRoomState> BreakoutRooms { get; set; } = new List<BreakoutRoomState>();
        [JsonProperty("breakoutUnassigned")] public List<BreakoutParticipantState> BreakoutUnassigned { get; set; } = new List<BreakoutParticipantState>();
        /// <summary>Null until the SDK has reported options.</summary>
        [JsonProperty("breakoutOptions")] public BreakoutOptionsState BreakoutOptions { get; set; }
        [JsonProperty("breakoutMyStatus")] public string BreakoutMyStatus { get; set; }
        [JsonProperty("breakoutMyRoomId")] public string BreakoutMyRoomId { get; set; }
        /// <summary>Seconds left on the breakout timer; -1 when no timer is running.</summary>
        [JsonProperty("breakoutTimerRemaining")] public int BreakoutTimerRemaining { get; set; }
    }

    public class CreateRoomsContent
    {
        [JsonProperty("count")] public int Count { get; set; }
        /// <summary>0 automatically, 1 manually, 2 let participants choose.</summary>
        [JsonProperty("assignType")] public int AssignType { get; set; }
    }

    public class RenameRoomContent
    {
        [JsonProperty("id")] public string Id { get; set; }
        [JsonProperty("name")] public string Name { get; set; }
    }

    public class AssignContent
    {
        [JsonProperty("userGuids")] public List<string> UserGuids { get; set; } = new List<string>();
        [JsonProperty("roomId")] public string RoomId { get; set; }
    }
}
