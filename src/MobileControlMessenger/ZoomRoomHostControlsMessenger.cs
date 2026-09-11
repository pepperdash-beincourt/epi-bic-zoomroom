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
    /// Mobile Control messenger for the host / co-host meeting-wide audio and video settings
    /// (see ZoomRoom.HostControls.cs). Status: <c>{ muteOnEntry, allowAttendeesUnmute,
    /// allowAttendeesStartVideo }</c>, each null until the Zoom Room has reported it. Actions:
    /// <c>/muteAllParticipants</c>, <c>/askAllToUnmute</c>, <c>/setMuteOnEntry {value}</c>,
    /// <c>/setAllowAttendeesUnmute {value}</c>, <c>/setAllowAttendeesStartVideo {value}</c>.
    /// Meeting lock lives on IHasMeetingLockMessenger.
    /// </summary>
    public class ZoomRoomHostControlsMessenger : MessengerBase
    {
        private readonly ZoomRoomDevice _codec;

        public ZoomRoomHostControlsMessenger(string key, string messagePath, ZoomRoomDevice codec)
            : base(key, messagePath, codec)
        {
            _codec = codec ?? throw new ArgumentNullException(nameof(codec));
        }

        protected override void RegisterActions()
        {
            base.RegisterActions();

            AddAction("/fullStatus", (id, content) => SendFullStatus(id));

            AddAction("/muteAllParticipants", (id, content) => _codec.MuteAllParticipants());
            AddAction("/askAllToUnmute", (id, content) => _codec.AskAllToUnmute());

            AddAction("/setMuteOnEntry", (id, content) =>
            {
                var b = content?.ToObject<MobileControlSimpleContent<bool>>();
                if (b != null) _codec.SetMuteOnEntry(b.Value);
            });
            AddAction("/setAllowAttendeesUnmute", (id, content) =>
            {
                var b = content?.ToObject<MobileControlSimpleContent<bool>>();
                if (b != null) _codec.SetAllowAttendeesUnmute(b.Value);
            });
            AddAction("/setAllowAttendeesStartVideo", (id, content) =>
            {
                var b = content?.ToObject<MobileControlSimpleContent<bool>>();
                if (b != null) _codec.SetAllowAttendeesStartVideo(b.Value);
            });

            _codec.HostControlsChanged += (s, e) => Task.Run(() => PostStatusMessage(BuildStatus()));
        }

        private void SendFullStatus(string clientId = null)
        {
            Task.Run(() =>
            {
                try { PostStatusMessage(BuildStatus(), clientId); }
                catch (Exception ex) { this.LogError(ex, "Error sending ZoomRoom host controls status"); }
            });
        }

        private ZoomRoomHostControlsStateMessage BuildStatus() => new ZoomRoomHostControlsStateMessage
        {
            MuteOnEntry = _codec.MuteOnEntry,
            AllowAttendeesUnmute = _codec.AllowAttendeesUnmuteThemselves,
            AllowAttendeesStartVideo = _codec.AllowAttendeesStartVideo,
        };
    }

    /// <summary>
    /// Null values are sent on purpose (no NullValueHandling.Ignore): after a meeting ends the app
    /// must see the settings go back to unknown rather than keep the last meeting's values.
    /// </summary>
    public class ZoomRoomHostControlsStateMessage : DeviceStateMessageBase
    {
        [JsonProperty("muteOnEntry")] public bool? MuteOnEntry { get; set; }
        [JsonProperty("allowAttendeesUnmute")] public bool? AllowAttendeesUnmute { get; set; }
        [JsonProperty("allowAttendeesStartVideo")] public bool? AllowAttendeesStartVideo { get; set; }
    }
}
