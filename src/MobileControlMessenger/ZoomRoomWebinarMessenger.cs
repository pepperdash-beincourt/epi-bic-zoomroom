using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Newtonsoft.Json;
using PepperDash.Core.Logging;
using PepperDash.Essentials.AppServer;
using PepperDash.Essentials.Plugins;
using ZoomRoomDevice = PepperDash.Essentials.Plugins.ZoomRoom;

namespace PepperDash.Essentials.AppServer.Messengers
{
    /// <summary>
    /// Mobile Control messenger for webinar state (see ZoomRoom.Webinar.cs).
    /// Status: <c>{ isWebinar, webinarAttendees: [{userId, name, canTalk, handRaised, audioMuted}],
    /// webinarAttendeeTotal, webinarAttendeeKeywords, webinarPanelistCount, webinarRaisedHandCount }</c>
    /// (counts are -1 until reported). Actions: <c>/refreshWebinar</c>, <c>/listWebinarAttendees {value: keywords}</c>.
    /// Promote / demote / allow to talk / remove live on IHasParticipantsMessenger.
    /// </summary>
    public class ZoomRoomWebinarMessenger : MessengerBase
    {
        private readonly ZoomRoomDevice _codec;

        public ZoomRoomWebinarMessenger(string key, string messagePath, ZoomRoomDevice codec)
            : base(key, messagePath, codec)
        {
            _codec = codec ?? throw new ArgumentNullException(nameof(codec));
        }

        protected override void RegisterActions()
        {
            base.RegisterActions();

            AddAction("/fullStatus", (id, content) => SendFullStatus(id));
            AddAction("/refreshWebinar", (id, content) => _codec.RefreshWebinar(requestAttendees: true));
            AddAction("/listWebinarAttendees", (id, content) =>
            {
                var s = content?.ToObject<MobileControlSimpleContent<string>>();
                _codec.RequestWebinarAttendees(s?.Value ?? string.Empty);
            });

            _codec.WebinarChanged += (s, e) => Task.Run(() => PostStatusMessage(BuildStatus()));
        }

        private void SendFullStatus(string clientId = null)
        {
            Task.Run(() =>
            {
                try { PostStatusMessage(BuildStatus(), clientId); }
                catch (Exception ex) { this.LogError(ex, "Error sending ZoomRoom webinar status"); }
            });
        }

        private ZoomRoomWebinarStateMessage BuildStatus() => new ZoomRoomWebinarStateMessage
        {
            IsWebinar = _codec.IsWebinar,
            WebinarAttendees = _codec.GetWebinarAttendeesSnapshot(),
            WebinarAttendeeTotal = _codec.WebinarAttendeeTotal,
            WebinarAttendeeKeywords = _codec.WebinarAttendeeKeywords,
            WebinarPanelistCount = _codec.WebinarPanelistCount,
            WebinarRaisedHandCount = _codec.WebinarRaisedHandCount,
        };
    }

    public class ZoomRoomWebinarStateMessage : DeviceStateMessageBase
    {
        [JsonProperty("isWebinar")] public bool IsWebinar { get; set; }
        [JsonProperty("webinarAttendees")] public List<WebinarAttendeeState> WebinarAttendees { get; set; } = new List<WebinarAttendeeState>();
        /// <summary>Attendees in the whole webinar; the list may hold fewer (the first 100, or a search result). -1 until reported.</summary>
        [JsonProperty("webinarAttendeeTotal")] public int WebinarAttendeeTotal { get; set; }
        /// <summary>The search the attendee list answers; empty for the first-100 list.</summary>
        [JsonProperty("webinarAttendeeKeywords")] public string WebinarAttendeeKeywords { get; set; }
        [JsonProperty("webinarPanelistCount")] public int WebinarPanelistCount { get; set; }
        [JsonProperty("webinarRaisedHandCount")] public int WebinarRaisedHandCount { get; set; }
    }
}
