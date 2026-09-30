using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using PepperDash.Essentials.Core;

namespace PepperDash.Essentials.Plugins
{
    public class ZoomRoomPropertiesConfig
    {
        [JsonProperty("communicationMonitorProperties")]
        public CommunicationMonitorConfig CommunicationMonitorProperties { get; set; }

        [JsonProperty("disablePhonebookAutoDownload")]
        public bool DisablePhonebookAutoDownload { get; set; }

        [JsonProperty("supportsCameraAutoMode")]
        public bool SupportsCameraAutoMode { get; set; }

        [JsonProperty("supportsCameraOff")]
        public bool SupportsCameraOff { get; set; }

        /// <summary>
        /// Whether to offer "Multi-Speaker" as a selectable layout. The ZRC SDK exposes no command to
        /// enter Multi-Speaker (it maps to ScreenLayoutSourceTypeNone/-1, which SetScreenLayout ignores),
        /// so the button is a no-op if pressed. Defaults to false (hidden); Multi-Speaker is still
        /// reported as the current layout when the Zoom controller selects it.
        /// </summary>
        [JsonProperty("showMultiSpeakerLayout")]
        public bool ShowMultiSpeakerLayout { get; set; }

        //if true, the layouts will be set automatically when sharing starts/ends or a call is joined
        [JsonProperty("autoDefaultLayouts")]
        public bool AutoDefaultLayouts { get; set; }

        /* This layout will be selected when Sharing starts (either from Far end or locally)*/
        [JsonProperty("defaultSharingLayout")]
        [JsonConverter(typeof(StringEnumConverter))]
        public zConfiguration.eLayoutStyle DefaultSharingLayout { get; set; }

        //This layout will be selected when a call is connected and no content is being shared
        [JsonProperty("defaultCallLayout")]
        [JsonConverter(typeof(StringEnumConverter))]
        public zConfiguration.eLayoutStyle DefaultCallLayout { get; set; }

        [JsonProperty("minutesBeforeMeetingStart")]
        public int MinutesBeforeMeetingStart { get; set; }

        /// <summary>
        /// How long (ms) a ringing meeting invite is allowed to sit unanswered before it's dropped
        /// from ActiveCalls. Fallback safety net for invites that are silently ignored -- the SDK
        /// otherwise only notifies via MeetingInviteTreated (answered/declined/expired elsewhere).
        /// Defaults to 45000 (45s) when unset/0.
        /// </summary>
        [JsonProperty("meetingInviteTimeoutMs")]
        public int MeetingInviteTimeoutMs { get; set; }

        [JsonProperty("activationCode")]
        public string ActivationCode { get; set; }

        /// <summary>
        /// How <c>DialPhoneCall</c> places outbound calls. <c>Sip</c> (default) uses the SDK's SIP
        /// phone service; <c>Pstn</c> uses PSTN call-out (not yet implemented).
        /// </summary>
        [JsonProperty("phoneDialMode")]
        [JsonConverter(typeof(StringEnumConverter))]
        public ePhoneDialMode PhoneDialMode { get; set; } = ePhoneDialMode.Sip;

        /// <summary>
        /// Path passed to ZrcSdk.Initialize(). Defaults to /user/zrcsdk (writable, persistent volume).
        /// </summary>
        [JsonProperty("sdkConfigPath")]
        public string SdkConfigPath { get; set; } = "/user/zrcsdk";

        /// <summary>
        /// Identifies this Zoom Room to the ZRC SDK when one program controls more than one Zoom Room.
        /// Pairing is stored per ID, and the ID is shown as the controller's serial number in the Zoom
        /// web portal. Leave unset for a single Zoom Room: that uses the SDK's default ID, which is
        /// what an existing pairing was stored under. With several Zoom Room devices each needs its
        /// own ID, so at most one of them may leave this unset; a repeated ID fails to initialize.
        /// </summary>
        [JsonProperty("sdkRoomId")]
        public string SdkRoomId { get; set; }

        /// <summary>
        /// Names of directory contacts this room must not reach. A contact whose name matches (case
        /// and surrounding spaces ignored) is left out of the directory, is never invited, and is
        /// declined automatically if it invites this room. Used for the other Zoom Rooms serving the
        /// same space, which must never share a meeting with this one.
        /// </summary>
        [JsonProperty("hiddenContactNames")]
        public System.Collections.Generic.List<string> HiddenContactNames { get; set; }

        // communicationMonitorProperties is kept for back-compat but is no longer used —
        // the SDK is event-driven and requires no polling.
    }

    /// <summary>Outbound phone dialing transport for <c>DialPhoneCall</c>.</summary>
    public enum ePhoneDialMode
    {
        /// <summary>SIP phone service (default).</summary>
        Sip,
        /// <summary>PSTN call-out (not yet implemented).</summary>
        Pstn,
    }
}