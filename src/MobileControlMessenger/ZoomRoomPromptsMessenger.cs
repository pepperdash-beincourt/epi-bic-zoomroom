using System;
using System.Collections.Generic;
using System.Linq;
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
    /// Mobile Control messenger for the Zoom Room's in-call prompts (see <see cref="ZoomRoom.ActivePrompts"/>):
    /// pushes the full list of prompts waiting on the UI whenever it changes, and takes answers back.
    /// Status: <c>{ "prompts": [ZoomPrompt, ...] }</c>. Actions: <c>/answerPrompt {id, accept}</c>,
    /// <c>/dismissPrompt {id}</c>.
    /// </summary>
    public class ZoomRoomPromptsMessenger : MessengerBase
    {
        private readonly ZoomRoomDevice _codec;

        public ZoomRoomPromptsMessenger(string key, string messagePath, ZoomRoomDevice codec)
            : base(key, messagePath, codec)
        {
            _codec = codec ?? throw new ArgumentNullException(nameof(codec));
        }

        protected override void RegisterActions()
        {
            base.RegisterActions();

            AddAction("/fullStatus", (id, content) => SendFullStatus(id));

            AddAction("/answerPrompt", (id, content) =>
            {
                var c = content?.ToObject<PromptAnswerContent>();
                if (c == null || c.Id <= 0) return;
                _codec.AnswerPrompt(c.Id, c.Accept);
            });

            AddAction("/dismissPrompt", (id, content) =>
            {
                var c = content?.ToObject<PromptAnswerContent>();
                if (c == null || c.Id <= 0) return;
                _codec.DismissPrompt(c.Id);
            });

            _codec.PromptsChanged += (s, e) => Task.Run(() => PostStatusMessage(BuildStatus()));
        }

        private void SendFullStatus(string clientId = null)
        {
            Task.Run(() =>
            {
                try { PostStatusMessage(BuildStatus(), clientId); }
                catch (Exception ex) { this.LogError(ex, "Error sending ZoomRoom prompts status"); }
            });
        }

        private ZoomRoomPromptsStateMessage BuildStatus() =>
            new ZoomRoomPromptsStateMessage { Prompts = _codec.ActivePrompts.ToList() };
    }

    public class ZoomRoomPromptsStateMessage : DeviceStateMessageBase
    {
        /// <summary>Every prompt waiting on the UI, oldest first. An empty list means nothing is pending.</summary>
        [JsonProperty("prompts")]
        public List<ZoomPrompt> Prompts { get; set; } = new List<ZoomPrompt>();
    }

    public class PromptAnswerContent
    {
        [JsonProperty("id")] public int Id { get; set; }
        [JsonProperty("accept")] public bool Accept { get; set; }
    }
}
