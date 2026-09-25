using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;
using Newtonsoft.Json;
using PepperDash.Core.Logging;
using PepperDash.ZoomRoom.Sdk;
using PepperDash.ZoomRoom.Sdk.EventArgs;

namespace PepperDash.Essentials.Plugins
{
	/// <summary>A webinar attendee as the participants page sees them.</summary>
	public class WebinarAttendeeState
	{
		[JsonProperty("userId")] public int UserId { get; set; }
		[JsonProperty("name")] public string Name { get; set; }
		/// <summary>The host has allowed this attendee to talk.</summary>
		[JsonProperty("canTalk")] public bool CanTalk { get; set; }
		[JsonProperty("handRaised")] public bool HandRaised { get; set; }
		[JsonProperty("audioMuted")] public bool AudioMuted { get; set; }
	}

	/// <summary>
	/// Webinar state for the participants page: whether the meeting is a webinar, and its attendees.
	/// Attendees are not in the meeting roster - in a webinar the roster holds the host, co-hosts and
	/// panelists - so they are asked for separately (the first 100, or a name search) and asked for
	/// again when the attendee count moves or after an action on one of them.
	/// See ZoomRoomWebinarMessenger for the Mobile Control contract.
	/// </summary>
	public partial class ZoomRoom
	{
		private const int AttendeeRefreshDelayMs = 1000;

		private readonly object _webinarLock = new object();
		private readonly Dictionary<int, WebinarAttendeeState> _attendees = new Dictionary<int, WebinarAttendeeState>();
		private bool _isWebinar;
		private int _attendeeTotal = -1;
		private int _panelistCount = -1;
		private int _raisedHandCount = -1;
		// What the current list answers (from the SDK) vs. what was last asked for (used to refresh).
		private string _attendeeKeywords = string.Empty;
		private string _attendeeKeywordsRequested = string.Empty;
		private bool _attendeesRequested;
		private CTimer _attendeeRefreshTimer;
		// Non-null while the SimulateWebinar console hook is active.
		private List<WebinarAttendeeState> _simulatedAttendees;

		/// <summary>Raised when anything the webinar part of the participants page shows changes.</summary>
		public event EventHandler WebinarChanged;

		public bool IsWebinar => _isWebinar;
		/// <summary>Attendees in the whole webinar; -1 until reported.</summary>
		public int WebinarAttendeeTotal => _attendeeTotal;
		/// <summary>-1 until reported.</summary>
		public int WebinarPanelistCount => _panelistCount;
		/// <summary>-1 until reported.</summary>
		public int WebinarRaisedHandCount => _raisedHandCount;
		/// <summary>The search the attendee list answers; empty for the first-100 list.</summary>
		public string WebinarAttendeeKeywords => _attendeeKeywords;

		private void SubscribeWebinarEvents()
		{
			_controller.WebinarAttendeeListReceived += (s, e) => ApplyAttendeeList(e);
			_controller.WebinarCountsChanged += (s, e) => ApplyWebinarCounts(e);
		}

		/// <summary>Attendees ordered by name.</summary>
		public List<WebinarAttendeeState> GetWebinarAttendeesSnapshot()
		{
			lock (_webinarLock)
				return _attendees.Values.OrderBy(a => a.Name, StringComparer.OrdinalIgnoreCase).ToList();
		}

		/// <summary>
		/// Re-reads whether the meeting is a webinar and, when <paramref name="requestAttendees"/>, asks for
		/// the attendees again with the last search. Called on meeting admission (no list) and whenever the
		/// participants page opens for a host / co-host (with the list).
		/// </summary>
		public void RefreshWebinar(bool requestAttendees)
		{
			if (_simulatedAttendees == null)
			{
				var isWebinar = _controller.IsWebinarMeeting();
				if (isWebinar.HasValue) SetIsWebinar(isWebinar.Value);
			}
			if (requestAttendees && _isWebinar) RequestWebinarAttendees(_attendeeKeywordsRequested);
		}

		/// <summary>Asks for attendees: an empty <paramref name="keywords"/> for the first 100, otherwise a name search.</summary>
		public void RequestWebinarAttendees(string keywords)
		{
			var kw = (keywords ?? string.Empty).Trim();
			_attendeeKeywordsRequested = kw;
			_attendeesRequested = true;
			if (_simulatedAttendees != null)
			{
				ApplySimulatedAttendees();
				return;
			}
			this.LogDebug("ListWebinarAttendees \"{Keywords}\"", kw);
			_controller.ListWebinarAttendees(kw);
		}

		private void SetIsWebinar(bool value)
		{
			if (_isWebinar == value) return;
			_isWebinar = value;
			this.LogInformation("Meeting is {Kind}", value ? "a webinar" : "not a webinar");
			WebinarChanged?.Invoke(this, EventArgs.Empty);
		}

		private void ApplyAttendeeList(WebinarAttendeeListEventArgs e)
		{
			if (e == null) return;
			if (e.Result != 0)
			{
				this.LogWarning("Webinar attendee list failed [{Result}] for \"{Keywords}\"", e.Result, e.Keywords);
				return;
			}
			if (_simulatedAttendees != null)
			{
				this.LogInformation("A real webinar attendee list arrived - ending the simulated webinar");
				_simulatedAttendees = null;
			}
			lock (_webinarLock)
			{
				if (e.StartIndex <= 0) _attendees.Clear();
				foreach (var a in e.Attendees ?? Array.Empty<ParticipantInfo>())
					_attendees[a.UserID] = ToAttendee(a);
			}
			_attendeeTotal = e.Total;
			_attendeeKeywords = e.Keywords ?? string.Empty;
			// Only a webinar answers this.
			_isWebinar = true;
			this.LogDebug("Webinar attendees: {Count} listed of {Total} (\"{Keywords}\", from {Start})",
				e.Attendees?.Length ?? 0, e.Total, _attendeeKeywords, e.StartIndex);
			WebinarChanged?.Invoke(this, EventArgs.Empty);
		}

		private static WebinarAttendeeState ToAttendee(ParticipantInfo p) => new WebinarAttendeeState
		{
			UserId = p.UserID,
			Name = p.UserName ?? string.Empty,
			CanTalk = p.IsViewOnlyUserCanTalk,
			HandRaised = p.HandRaised,
			AudioMuted = p.AudioMuted,
		};

		private void ApplyWebinarCounts(WebinarCountsEventArgs e)
		{
			if (e == null) return;
			var attendeesMoved = e.AttendeeCount >= 0 && e.AttendeeCount != _attendeeTotal;
			var handsMoved = e.RaisedHandCount >= 0 && e.RaisedHandCount != _raisedHandCount;
			var panelistsMoved = e.PanelistCount >= 0 && e.PanelistCount != _panelistCount;
			if (!attendeesMoved && !handsMoved && !panelistsMoved) return;

			if (e.AttendeeCount >= 0) _attendeeTotal = e.AttendeeCount;
			if (e.RaisedHandCount >= 0) _raisedHandCount = e.RaisedHandCount;
			if (e.PanelistCount >= 0) _panelistCount = e.PanelistCount;
			WebinarChanged?.Invoke(this, EventArgs.Empty);

			// Someone joined, left or raised a hand: the list the page shows is out of date.
			if (attendeesMoved || handsMoved) ScheduleAttendeeRefresh();
		}

		/// <summary>
		/// Asks for the attendee list again shortly (debounced), with the last search. No-op unless this is
		/// a webinar whose attendees the page has asked for.
		/// </summary>
		private void ScheduleAttendeeRefresh()
		{
			if (!_isWebinar || !_attendeesRequested || _simulatedAttendees != null) return;
			lock (_webinarLock)
			{
				_attendeeRefreshTimer?.Stop();
				_attendeeRefreshTimer = new CTimer(_ => RequestWebinarAttendees(_attendeeKeywordsRequested), AttendeeRefreshDelayMs);
			}
		}

		private void ResetWebinar()
		{
			bool changed;
			lock (_webinarLock)
			{
				_attendeeRefreshTimer?.Stop();
				_attendeeRefreshTimer = null;
				changed = _isWebinar || _attendees.Count > 0 || _attendeeTotal >= 0;
				_attendees.Clear();
			}
			_isWebinar = false;
			_attendeeTotal = -1;
			_panelistCount = -1;
			_raisedHandCount = -1;
			_attendeeKeywords = string.Empty;
			_attendeeKeywordsRequested = string.Empty;
			_attendeesRequested = false;
			_simulatedAttendees = null;
			if (changed) WebinarChanged?.Invoke(this, EventArgs.Empty);
		}

		// ── Test hook ──────────────────────────────────────────────────────────

		private static readonly string[] SimFirstNames =
		{
			"Avery", "Blake", "Carmen", "Dana", "Elliot", "Farah", "Gavin", "Hana", "Isaac", "Jada", "Kofi", "Lena", "Mateo",
			"Nia", "Omar", "Priya", "Quinn", "Rosa", "Samir", "Tess", "Uma", "Victor", "Wren", "Xavier", "Yara", "Zane",
		};
		private static readonly string[] SimLastNames =
		{
			"Adams", "Brooks", "Chen", "Diaz", "Evans", "Foster", "Garcia", "Hughes", "Ito", "Jensen", "Khan", "Lopez",
			"Moreno", "Nguyen", "Okafor", "Patel",
		};

		/// <summary>
		/// <c>devjson:1 {"deviceKey":"zoomRoom","methodName":"SimulateWebinar","params":[40]}</c> makes the room
		/// treat the meeting as a webinar with that many fake attendees, so the Attendees view can be tried
		/// without a real webinar. Allow to Talk, Promote and Remove act on the fake list only. 0 ends the
		/// simulation; a real attendee list, or the meeting ending, also ends it.
		/// </summary>
		public void SimulateWebinar(int attendeeCount)
		{
			if (attendeeCount <= 0)
			{
				this.LogWarning("SIMULATED webinar ended (console test hook)");
				_simulatedAttendees = null;
				lock (_webinarLock) _attendees.Clear();
				_attendeeTotal = -1;
				_raisedHandCount = -1;
				_isWebinar = _controller.IsWebinarMeeting() ?? false;
				WebinarChanged?.Invoke(this, EventArgs.Empty);
				return;
			}

			this.LogWarning("SIMULATED webinar with {Count} attendee(s) (console test hook)", attendeeCount);
			_simulatedAttendees = Enumerable.Range(0, attendeeCount).Select(i => new WebinarAttendeeState
			{
				UserId = 900000 + i,
				Name = $"{SimFirstNames[i % SimFirstNames.Length]} {SimLastNames[(i / SimFirstNames.Length + i) % SimLastNames.Length]}",
				HandRaised = i % 7 == 3,
				AudioMuted = true,
			}).ToList();
			_isWebinar = true;
			_attendeesRequested = true;
			ApplySimulatedAttendees();
		}

		/// <summary>Shows the simulated attendees the way the SDK would: the first 100, or the search matches.</summary>
		private void ApplySimulatedAttendees()
		{
			var all = _simulatedAttendees;
			if (all == null) return;
			var kw = _attendeeKeywordsRequested;
			var shown = all.Where(a => kw.Length == 0 || a.Name.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0).Take(100).ToList();
			lock (_webinarLock)
			{
				_attendees.Clear();
				foreach (var a in shown) _attendees[a.UserId] = a;
			}
			_attendeeTotal = all.Count;
			_raisedHandCount = all.Count(a => a.HandRaised);
			_attendeeKeywords = kw;
			WebinarChanged?.Invoke(this, EventArgs.Empty);
		}

		/// <summary>Applies an action to a simulated attendee; false when the id is not one (a real participant).</summary>
		private bool TrySimulatedAttendee(int userId, string action, Action<WebinarAttendeeState> apply)
		{
			var a = _simulatedAttendees?.FirstOrDefault(x => x.UserId == userId);
			if (a == null) return false;
			this.LogWarning("SIMULATED webinar: {Action} \"{Name}\"", action, a.Name);
			apply(a);
			ApplySimulatedAttendees();
			return true;
		}
	}
}
