using System;
using System.Collections.Generic;
using System.Linq;
using Crestron.SimplSharp;

namespace PepperDash.Essentials.Plugins
{
	/// <summary>
	/// Console commands for pairing. They are registered once for the program and take an optional
	/// device key, because the console accepts each command name only once: registering them per
	/// device left every Zoom Room after the first unreachable.
	/// </summary>
	/// <remarks>
	/// With one Zoom Room the device key may be left out, so the commands work as they always have.
	/// With several, the key is required and the commands list the keys when it is missing.
	/// <code>
	/// pairZoomRoom [deviceKey] &lt;activationCode&gt;
	/// repairZoomRoom [deviceKey]
	/// unpairZoomRoom [deviceKey]
	/// forceRepairZoom [deviceKey]
	/// </code>
	/// </remarks>
	public partial class ZoomRoom
	{
		private static readonly object ConsoleLock = new object();
		private static readonly Dictionary<string, ZoomRoom> ConsoleInstances =
			new Dictionary<string, ZoomRoom>(StringComparer.OrdinalIgnoreCase);
		private static bool _consoleCommandsRegistered;

		/// <summary>
		/// Adds this device to the console command targets, registering the commands on first use.
		/// </summary>
		private void RegisterConsoleCommands()
		{
			lock (ConsoleLock)
			{
				ConsoleInstances[Key] = this;

				if (_consoleCommandsRegistered)
				{
					return;
				}

				_consoleCommandsRegistered = true;
			}

			CrestronConsole.AddNewConsoleCommand(PairCommand,
				"pairZoomRoom", "Pair a Zoom Room with an activation code: pairZoomRoom [deviceKey] <code>",
				ConsoleAccessLevelEnum.AccessOperator);

			CrestronConsole.AddNewConsoleCommand(
				args => RunForDevice("repairZoomRoom", args, room => room._controller.RetryPair()),
				"repairZoomRoom", "Reconnect to the last paired Zoom Room: repairZoomRoom [deviceKey]",
				ConsoleAccessLevelEnum.AccessOperator);

			CrestronConsole.AddNewConsoleCommand(
				args => RunForDevice("unpairZoomRoom", args, room => room._controller.Unpair()),
				"unpairZoomRoom", "Unpair from a Zoom Room: unpairZoomRoom [deviceKey]",
				ConsoleAccessLevelEnum.AccessOperator);

			CrestronConsole.AddNewConsoleCommand(
				args => RunForDevice("forceRepairZoom", args, room => room._controller.RepairWithConfiguredCode()),
				"forceRepairZoom",
				"Clear stored credentials and re-pair using the configured activation code: forceRepairZoom [deviceKey]",
				ConsoleAccessLevelEnum.AccessOperator);
		}

		private static void PairCommand(string args)
		{
			var tokens = Split(args);

			// "<code>" with one Zoom Room, or "<deviceKey> <code>".
			string deviceKey = null, code = null;
			if (tokens.Length == 1)
			{
				code = tokens[0];
			}
			else if (tokens.Length == 2)
			{
				deviceKey = tokens[0];
				code = tokens[1];
			}

			if (string.IsNullOrEmpty(code))
			{
				CrestronConsole.ConsoleCommandResponse("Usage: pairZoomRoom [deviceKey] <activationCode>\r\n" + ListKeys());
				return;
			}

			var room = Resolve("pairZoomRoom", deviceKey);
			if (room == null)
			{
				return;
			}

			var ok = room._controller.PairWithActivationCode(code);
			CrestronConsole.ConsoleCommandResponse(string.Format("pairZoomRoom {0}: {1}\r\n", room.Key, ok ? "sent" : "failed"));
		}

		private static void RunForDevice(string command, string args, Func<ZoomRoom, bool> action)
		{
			var tokens = Split(args);
			if (tokens.Length > 1)
			{
				CrestronConsole.ConsoleCommandResponse(string.Format("Usage: {0} [deviceKey]\r\n{1}", command, ListKeys()));
				return;
			}

			var room = Resolve(command, tokens.Length == 1 ? tokens[0] : null);
			if (room == null)
			{
				return;
			}

			var ok = action(room);
			CrestronConsole.ConsoleCommandResponse(string.Format("{0} {1}: {2}\r\n", command, room.Key, ok ? "sent" : "failed"));
		}

		/// <summary>
		/// The Zoom Room a command is for: the named one, or the only one when no key is given.
		/// Reports to the console and returns null when that cannot be decided.
		/// </summary>
		private static ZoomRoom Resolve(string command, string deviceKey)
		{
			lock (ConsoleLock)
			{
				if (!string.IsNullOrEmpty(deviceKey))
				{
					if (ConsoleInstances.TryGetValue(deviceKey, out var named))
					{
						return named;
					}

					CrestronConsole.ConsoleCommandResponse(string.Format("{0}: no Zoom Room with key '{1}'.\r\n{2}",
						command, deviceKey, ListKeysLocked()));
					return null;
				}

				if (ConsoleInstances.Count == 1)
				{
					return ConsoleInstances.Values.First();
				}

				CrestronConsole.ConsoleCommandResponse(string.Format("{0}: {1}. Add the device key: {0} <deviceKey>\r\n{2}",
					command, ConsoleInstances.Count == 0 ? "no Zoom Room is registered" : "more than one Zoom Room",
					ListKeysLocked()));
				return null;
			}
		}

		private static string ListKeys()
		{
			lock (ConsoleLock)
			{
				return ListKeysLocked();
			}
		}

		private static string ListKeysLocked()
		{
			return ConsoleInstances.Count == 0
				? string.Empty
				: "Zoom Rooms: " + string.Join(", ", ConsoleInstances.Keys.OrderBy(k => k)) + "\r\n";
		}

		private static string[] Split(string args)
		{
			return (args ?? string.Empty).Split(new[] { ' ', '\t' }, StringSplitOptions.RemoveEmptyEntries);
		}
	}
}
