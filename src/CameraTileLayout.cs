using System.Collections.Generic;
using System.Linq;

namespace PepperDash.Essentials.Plugins
{
	/// <summary>
	/// Puts the room's child camera tiles on tile numbers. Tile N is the camera on the codec's Nth
	/// camera input (tile 1, the room itself, is never a child). Zoom's own number on a child ("Room 2")
	/// is not tied to a camera input, so a child whose input is known takes that input's tile, and the
	/// rest are placed by Zoom's number: number K prefers tile K + 1, and a child whose preferred tile is
	/// taken, or that has no number, takes the lowest free tile. A tile with no child is left empty.
	/// No Crestron or SDK dependencies, so it is unit tested.
	/// </summary>
	internal static class CameraTileLayout
	{
		internal sealed class Child
		{
			public Child(int userId, int zoomNumber, int knownTile)
			{
				UserId = userId;
				ZoomNumber = zoomNumber;
				KnownTile = knownTile;
			}

			public int UserId { get; }

			/// <summary>The number Zoom appended to the tile's name; int.MaxValue when there is none.</summary>
			public int ZoomNumber { get; }

			/// <summary>The tile of the camera input this child is known to show; 0 when not known.</summary>
			public int KnownTile { get; }
		}

		/// <summary>Returns each child's tile number (2 and up) by user ID.</summary>
		public static Dictionary<int, int> Assign(IEnumerable<Child> children)
		{
			var all = (children ?? Enumerable.Empty<Child>()).Where(c => c != null).ToList();
			var tileByUser = new Dictionary<int, int>();
			var taken = new HashSet<int> { 1 };

			// Two children claiming one input: the later one (higher user ID) is the camera now.
			foreach (var child in all.Where(c => c.KnownTile >= 2).OrderByDescending(c => c.UserId))
			{
				if (!taken.Add(child.KnownTile)) continue;
				tileByUser[child.UserId] = child.KnownTile;
			}

			var unplaced = all.Where(c => !tileByUser.ContainsKey(c.UserId))
				.OrderBy(c => c.ZoomNumber)
				.ThenBy(c => c.UserId)
				.ToList();

			foreach (var child in unplaced.Where(c => c.ZoomNumber >= 1 && c.ZoomNumber < int.MaxValue).ToList())
			{
				if (!taken.Add(child.ZoomNumber + 1)) continue;
				tileByUser[child.UserId] = child.ZoomNumber + 1;
				unplaced.Remove(child);
			}

			var next = 2;
			foreach (var child in unplaced)
			{
				while (taken.Contains(next)) next++;
				taken.Add(next);
				tileByUser[child.UserId] = next;
			}

			return tileByUser;
		}
	}
}
