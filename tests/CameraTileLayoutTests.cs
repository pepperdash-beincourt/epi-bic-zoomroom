using FluentAssertions;
using PepperDash.Essentials.Plugins;

namespace PepperDash.Essentials.Plugins.Zoom.Room.Tests;

/// <summary>
/// CameraTileLayout is compiled into this project from the plugin source (it has no Crestron or SDK
/// dependencies), so its placement rules are run here rather than only inspected.
/// </summary>
public class CameraTileLayoutTests
{
    private const int NoNumber = int.MaxValue;

    private static CameraTileLayout.Child Child(int userId, int zoomNumber, int knownTile = 0) =>
        new CameraTileLayout.Child(userId, zoomNumber, knownTile);

    [Fact]
    public void Nothing_Known_Places_Children_By_Zoom_Number()
    {
        var tiles = CameraTileLayout.Assign(new[] { Child(30, 3), Child(10, 1), Child(20, 2) });

        tiles.Should().Equal(new Dictionary<int, int> { [10] = 2, [20] = 3, [30] = 4 });
    }

    [Fact]
    public void Known_Camera_Takes_Its_Input_Tile_And_Moves_The_Child_That_Had_It()
    {
        // Zoom numbered the camera on input 4 as 1. The camera on input 2 left, came back with
        // number 3, and is known; the child Zoom numbered 1 moves to the tile left free.
        var tiles = CameraTileLayout.Assign(new[] { Child(10, 1), Child(11, 2), Child(20, 3, knownTile: 2) });

        tiles.Should().Equal(new Dictionary<int, int> { [20] = 2, [11] = 3, [10] = 4 });
    }

    [Fact]
    public void Tile_Of_A_Camera_Not_In_The_Meeting_Stays_Empty()
    {
        var tiles = CameraTileLayout.Assign(new[] { Child(11, 1, knownTile: 3), Child(12, 2, knownTile: 4) });

        tiles.Should().Equal(new Dictionary<int, int> { [11] = 3, [12] = 4 });
    }

    [Fact]
    public void Later_Child_Wins_When_Two_Claim_One_Input()
    {
        var tiles = CameraTileLayout.Assign(new[] { Child(10, 1, knownTile: 2), Child(20, 2, knownTile: 2) });

        tiles[20].Should().Be(2);
        tiles[10].Should().Be(3, "the earlier child is placed as if its camera were not known");
    }

    [Fact]
    public void Child_Without_A_Number_Takes_The_Lowest_Free_Tile()
    {
        var tiles = CameraTileLayout.Assign(new[] { Child(10, 1), Child(20, NoNumber), Child(30, 3) });

        tiles.Should().Equal(new Dictionary<int, int> { [10] = 2, [30] = 4, [20] = 3 });
    }

    [Fact]
    public void Known_Tile_1_Is_Ignored_Because_Tile_1_Is_The_Room()
    {
        var tiles = CameraTileLayout.Assign(new[] { Child(10, 1, knownTile: 1) });

        tiles.Should().Equal(new Dictionary<int, int> { [10] = 2 });
    }

    [Fact]
    public void No_Children_Gives_No_Tiles()
    {
        CameraTileLayout.Assign(Array.Empty<CameraTileLayout.Child>()).Should().BeEmpty();
    }
}
