using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PepperDash.Core.Logging;
using PepperDash.Essentials.AppServer;
using ZoomRoomDevice = PepperDash.Essentials.Plugins.ZoomRoom;

namespace PepperDash.Essentials.AppServer.Messengers
{
    /// <summary>
    /// Mobile Control messenger for breakout rooms (see ZoomRoom.Breakout.cs).
    /// Status: <c>{ breakoutStatus }</c> = unknown | invalid | edit | started | stopping | ended.
    /// Actions: <c>/startBreakoutRooms</c>, <c>/stopBreakoutRooms</c>,
    /// <c>/broadcastToBreakoutRooms {value}</c>, <c>/joinBreakoutRoom</c>, <c>/leaveBreakoutRoom</c>,
    /// <c>/askForHelpInBreakoutRoom</c>.
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

        private ZoomRoomBreakoutStateMessage BuildStatus() =>
            new ZoomRoomBreakoutStateMessage { BreakoutStatus = _codec.BreakoutStatus };
    }

    public class ZoomRoomBreakoutStateMessage : DeviceStateMessageBase
    {
        [JsonProperty("breakoutStatus")] public string BreakoutStatus { get; set; }
    }
}
