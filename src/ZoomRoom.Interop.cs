using System;
using System.Linq;
using Crestron.SimplSharp;
using PepperDash.Core.Logging;
using PepperDash.ZoomRoom.Sdk;

namespace PepperDash.Essentials.Plugins
{
	/// <summary>
	/// Third-party (integration) meetings: the Zoom Room joins a Teams, Webex, Google (...) meeting by
	/// its ID. Spike: driven from the console ("zoomInterop") and every SDK event is logged, to prove on
	/// hardware what the room supports before any panel work.
	/// Console: "zoomInterop [deviceKey] info" - what the room may join and the current state;
	/// "zoomInterop [deviceKey] join teams 123456789 [passcode]"; "leave"; "mute on|off"; "video on|off".
	/// </summary>
	public partial class ZoomRoom
	{
		private IntegrationMeetingState _integrationState = IntegrationMeetingState.None;
		private ThirdPartyMeetingProvider _integrationProvider = ThirdPartyMeetingProvider.Invalid;
		private bool? _supportsTeams;
		private bool? _supportsTeamsAdhoc;
		private bool? _supportsGoogleMeet;

		private void SubscribeIntegrationEvents()
		{
			_controller.IntegrationEvent += (s, e) => OnIntegrationEvent(e);
		}

		private void OnIntegrationEvent(IntegrationEventArgs e)
		{
			switch (e.Kind)
			{
				case IntegrationEventKind.StartResult:
					this.LogInformation("Integration meeting start result {Result} (provider {Provider})", e.Code, (ThirdPartyMeetingProvider)e.Value);
					break;
				case IntegrationEventKind.MeetingInfo:
					_integrationState = (IntegrationMeetingState)e.Code;
					_integrationProvider = (ThirdPartyMeetingProvider)e.Value;
					this.LogInformation("Integration meeting {Provider} state {State} title=\"{Title}\" id={Id} audioMuted={Audio} videoMuted={Video}",
						_integrationProvider, _integrationState, Field(e, 0), Field(e, 1), Field(e, 2), Field(e, 3));
					break;
				case IntegrationEventKind.Error:
					this.LogWarning("Integration meeting error {Code}: {Message}", e.Code, Field(e, 0));
					break;
				case IntegrationEventKind.SupportsTeams:
					_supportsTeams = e.Code != 0;
					this.LogInformation("Zoom Room supports joining Teams meetings: {Supported}", _supportsTeams);
					break;
				case IntegrationEventKind.SupportsTeamsAdhoc:
					_supportsTeamsAdhoc = e.Code != 0;
					this.LogInformation("Zoom Room supports ad hoc Teams meetings: {Supported}", _supportsTeamsAdhoc);
					break;
				case IntegrationEventKind.SupportsGoogleMeet:
					_supportsGoogleMeet = e.Code != 0;
					this.LogInformation("Zoom Room supports joining Google Meet meetings: {Supported}", _supportsGoogleMeet);
					break;
				case IntegrationEventKind.ContentShare:
					this.LogInformation("Integration meeting content share state {State}, HDMI share available {Hdmi}", e.Code, e.Value != 0);
					break;
				case IntegrationEventKind.Layout:
					this.LogInformation("Integration meeting layout {Selected} (available mask {Available})", e.Code, e.Value);
					break;
				case IntegrationEventKind.AskToJoin:
					this.LogInformation("Zoom Room asks to join third-party meeting \"{Name}\" ({Number})", Field(e, 0), Field(e, 1));
					break;
				default:
					this.LogDebug("Integration event {Kind} code={Code} value={Value}", e.Kind, e.Code, e.Value);
					break;
			}
		}

		private static string Field(IntegrationEventArgs e, int index) =>
			e.Fields != null && index < e.Fields.Length ? e.Fields[index] : string.Empty;

		private static bool TryParseProvider(string text, out ThirdPartyMeetingProvider provider)
		{
			switch ((text ?? string.Empty).Trim().ToLowerInvariant())
			{
				case "teams": provider = ThirdPartyMeetingProvider.Teams; return true;
				case "webex": provider = ThirdPartyMeetingProvider.Webex; return true;
				case "google": case "meet": case "googlemeet": provider = ThirdPartyMeetingProvider.GoogleHangouts; return true;
				case "goto": case "gotomeeting": provider = ThirdPartyMeetingProvider.GoToMeeting; return true;
				case "bluejeans": provider = ThirdPartyMeetingProvider.Bluejeans; return true;
				case "skype": provider = ThirdPartyMeetingProvider.Skype; return true;
				default: provider = ThirdPartyMeetingProvider.Invalid; return false;
			}
		}

		private void InteropConsoleCommand(string args)
		{
			var words = (args ?? string.Empty).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
			var verb = words.Length > 0 ? words[0].ToLowerInvariant() : "info";

			switch (verb)
			{
				case "info":
					CrestronConsole.ConsoleCommandResponse("Integration meeting: {0} state {1}; supports Teams={2} Teams ad hoc={3} Google Meet={4}\r\n",
						_integrationProvider, _integrationState,
						_supportsTeams?.ToString() ?? "unknown", _supportsTeamsAdhoc?.ToString() ?? "unknown", _supportsGoogleMeet?.ToString() ?? "unknown");
					foreach (var p in new[] { ThirdPartyMeetingProvider.Teams, ThirdPartyMeetingProvider.Webex, ThirdPartyMeetingProvider.GoogleHangouts })
					{
						var i = _controller.GetInterOperabilityInfo(p);
						if (i == null)
							CrestronConsole.ConsoleCommandResponse("  {0}: no interop info\r\n", p);
						else
							CrestronConsole.ConsoleCommandResponse("  {0}: join={1} webClient={2} sip={3} phone={4} pexip={5}\r\n",
								p, i.SupportJoinMeeting, i.SupportJoinWebClient, i.SupportSipJoin, i.SupportPhoneJoin, i.IsPexipEnabled);
					}
					return;

				case "join":
					if (words.Length < 3 || !TryParseProvider(words[1], out var provider))
					{
						CrestronConsole.ConsoleCommandResponse("Usage: zoomInterop [deviceKey] join <teams|webex|google|goto|bluejeans> <meetingId> [passcode]\r\n");
						return;
					}
					var meetingId = words[2];
					var passcode = words.Length > 3 ? string.Join(" ", words.Skip(3)) : string.Empty;
					var code = _controller.JoinIntegrationMeeting(meetingId, passcode, provider);
					this.LogInformation("JoinIntegrationMeeting {Provider} id={Id} passcode={HasPasscode} result={Code}", provider, meetingId, passcode.Length > 0 ? "yes" : "no", code);
					CrestronConsole.ConsoleCommandResponse("Join {0} meeting {1}: result {2} (state changes follow in the log)\r\n", provider, meetingId, code);
					return;

				case "leave":
					CrestronConsole.ConsoleCommandResponse("Leave integration meeting: result {0}\r\n", _controller.LeaveIntegrationMeeting());
					return;

				case "mute":
				case "video":
					if (words.Length < 2 || (words[1] != "on" && words[1] != "off"))
					{
						CrestronConsole.ConsoleCommandResponse("Usage: zoomInterop [deviceKey] {0} on|off\r\n", verb);
						return;
					}
					var on = words[1] == "on";
					var result = verb == "mute" ? _controller.MuteIntegrationAudio(on) : _controller.StopIntegrationVideo(!on);
					CrestronConsole.ConsoleCommandResponse("{0} {1}: result {2}\r\n", verb, words[1], result);
					return;

				default:
					CrestronConsole.ConsoleCommandResponse("Usage: zoomInterop [deviceKey] info | join <service> <meetingId> [passcode] | leave | mute on|off | video on|off\r\n");
					return;
			}
		}
	}
}
