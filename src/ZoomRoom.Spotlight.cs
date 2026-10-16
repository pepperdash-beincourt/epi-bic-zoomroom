using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using PepperDash.Core.Logging;

namespace PepperDash.Essentials.Plugins
{
	/// <summary>
	/// Spotlight (for everyone in the meeting; host / co-host) and the pinned / spotlighted
	/// participant lists the panel shows in its participant menu. The SDK reports neither state, so
	/// both are tracked from the calls made here, the same way pins are tracked in
	/// <c>_pinnedUserScreens</c>, and cleared when the meeting resets or the participant leaves.
	/// </summary>
	public partial class ZoomRoom
	{
		private readonly HashSet<int> _spotlightedUserIds = new HashSet<int>();

		/// <summary>Raised when the pinned or spotlighted participants change.</summary>
		public event EventHandler PinSpotlightChanged;

		/// <summary>Participants this room has pinned on its own screen.</summary>
		public int[] PinnedUserIds
		{
			get { lock (_participantLock) return _pinnedUserScreens.Keys.ToArray(); }
		}

		/// <summary>Participants this room has spotlighted for everyone.</summary>
		public int[] SpotlightedUserIds
		{
			get { lock (_participantLock) return _spotlightedUserIds.ToArray(); }
		}

		public void SpotlightParticipant(int userId)
		{
			if (!IsCurrentParticipant(userId, nameof(SpotlightParticipant))) return;
			if (!_controller.SpotlightUser(userId)) return;
			lock (_participantLock) _spotlightedUserIds.Add(userId);
			RaisePinSpotlightChanged();
		}

		public void UnspotlightParticipant(int userId)
		{
			if (!_controller.CancelSpotlightUser(userId)) return;
			lock (_participantLock) _spotlightedUserIds.Remove(userId);
			RaisePinSpotlightChanged();
		}

		public void ToggleParticipantSpotlight(int userId)
		{
			bool spotlighted;
			lock (_participantLock) spotlighted = _spotlightedUserIds.Contains(userId);
			if (spotlighted)
				UnspotlightParticipant(userId);
			else
				SpotlightParticipant(userId);
		}

		// Spotlight may target any current participant, including this room.
		private bool IsCurrentParticipant(int userId, string op)
		{
			bool present;
			lock (_participantLock)
				present = Participants.CurrentParticipants.Any(p => p.UserId == userId);
			if (!present)
				this.LogWarning("{Op}: userId {UserId} is not a current meeting participant", op, userId);
			return present;
		}

		// Caller holds _participantLock. Drops a departed participant's pin and spotlight.
		private void ForgetPinSpotlight(IEnumerable<int> userIds)
		{
			var changed = false;
			foreach (var id in userIds)
			{
				changed |= _pinnedUserScreens.Remove(id);
				changed |= _spotlightedUserIds.Remove(id);
			}
			if (changed)
				Task.Run(RaisePinSpotlightChanged);
		}

		private void RaisePinSpotlightChanged()
		{
			try { PinSpotlightChanged?.Invoke(this, EventArgs.Empty); }
			catch (Exception ex) { this.LogError(ex, "PinSpotlightChanged handler failed: {Message}", ex.Message); }
		}
	}
}
