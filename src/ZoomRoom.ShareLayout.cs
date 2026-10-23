using System;
using Crestron.SimplSharp;
using PepperDash.Core.Logging;

namespace PepperDash.Essentials.Plugins
{
	/// <summary>
	/// A layout held while this room shares content. <see cref="SetLayoutWhileSharing"/> with a layout
	/// name (e.g. "Dynamic" = Dynamic Gallery) remembers the layout in use and switches to the named one
	/// once the share is up - Zoom offers some layouts only while content is shared. With no name it puts
	/// the remembered layout back if this plugin changed it. Applied from a timer thread (SDK calls made
	/// inside SDK event handlers fail - see ScheduleFixedAudioLevels).
	/// </summary>
	public partial class ZoomRoom
	{
		private const int ShareLayoutDelayMs = 500;

		private readonly object _shareLayoutLock = new object();
		private zConfiguration.eLayoutStyle _shareLayout = zConfiguration.eLayoutStyle.None;
		private zConfiguration.eLayoutStyle _layoutBeforeShareLayout = zConfiguration.eLayoutStyle.None;
		// Set once the held layout has been applied during the current share; cleared when the share stops.
		private bool _shareLayoutAppliedThisShare;
		// Set when this plugin changed the layout, so clearing the held layout puts the old one back.
		private bool _shareLayoutChangedLayout;
		private CTimer _shareLayoutTimer;

		/// <summary>
		/// Holds layout <paramref name="layout"/> (a layout style name, e.g. "Dynamic") while this room
		/// shares content, or with a null or empty name stops holding one and restores the layout from
		/// before. Returns false for an unknown layout name.
		/// </summary>
		public bool SetLayoutWhileSharing(string layout)
		{
			var style = zConfiguration.eLayoutStyle.None;
			if (!string.IsNullOrWhiteSpace(layout) && !Enum.TryParse(layout.Trim(), true, out style))
			{
				this.LogWarning("SetLayoutWhileSharing: unknown layout '{Layout}'", layout);
				return false;
			}

			lock (_shareLayoutLock)
			{
				if (style == _shareLayout) return true;

				if (_shareLayout == zConfiguration.eLayoutStyle.None)
					_layoutBeforeShareLayout = LastSelectedLayout;
				_shareLayout = style;
				_shareLayoutAppliedThisShare = false;
			}

			this.LogInformation("Layout while sharing: {Layout}", style == zConfiguration.eLayoutStyle.None ? "none (restore)" : style.ToString());
			ScheduleShareLayout();
			return true;
		}

		private void ScheduleShareLayout()
		{
			lock (_shareLayoutLock)
			{
				_shareLayoutTimer?.Stop();
				_shareLayoutTimer = new CTimer(_ => ApplyShareLayout(), ShareLayoutDelayMs);
			}
		}

		// From the SDK's sharing and screen-layout notifications: the share starting (or Zoom now offering
		// the layout) is when the held layout can be applied.
		private void OnShareStateForShareLayout()
		{
			bool pending;
			lock (_shareLayoutLock)
			{
				if (_sdkSharingState == 0) _shareLayoutAppliedThisShare = false;
				pending = _shareLayout != zConfiguration.eLayoutStyle.None && !_shareLayoutAppliedThisShare && _sdkSharingState != 0;
			}
			if (pending) ScheduleShareLayout();
		}

		private void ApplyShareLayout()
		{
			if (!IsInCall) return;

			zConfiguration.eLayoutStyle wanted, before;
			lock (_shareLayoutLock)
			{
				wanted = _shareLayout;
				before = _layoutBeforeShareLayout;
			}

			if (wanted != zConfiguration.eLayoutStyle.None)
			{
				// Wait for the share; OnShareStateForShareLayout schedules this again when it starts.
				if (_sdkSharingState == 0) return;

				lock (_shareLayoutLock)
				{
					if (_shareLayoutAppliedThisShare) return;
					_shareLayoutAppliedThisShare = true;
					if (LastSelectedLayout == wanted) return;
					_shareLayoutChangedLayout = true;
				}

				this.LogInformation("Layout while sharing: {From} -> {To}", LastSelectedLayout, wanted);
				SetLayout(wanted);
				return;
			}

			bool restore;
			lock (_shareLayoutLock)
			{
				restore = _shareLayoutChangedLayout;
				_shareLayoutChangedLayout = false;
			}
			if (!restore || before == zConfiguration.eLayoutStyle.None || before == LastSelectedLayout) return;

			this.LogInformation("Layout while sharing ended: {From} -> {To}", LastSelectedLayout, before);
			SetLayout(before);
		}

		// Meeting reset: a new meeting starts from Zoom's own layout, so there is nothing to put back.
		private void ResetShareLayout()
		{
			lock (_shareLayoutLock)
			{
				_shareLayoutTimer?.Stop();
				_shareLayoutTimer = null;
				_shareLayout = zConfiguration.eLayoutStyle.None;
				_layoutBeforeShareLayout = zConfiguration.eLayoutStyle.None;
				_shareLayoutAppliedThisShare = false;
				_shareLayoutChangedLayout = false;
			}
		}
	}
}
