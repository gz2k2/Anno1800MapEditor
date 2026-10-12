using AnnoMapEditor.DataArchives.Assets.Models;
using AnnoMapEditor.MapTemplates.Enums;
using AnnoMapEditor.MapTemplates.Models;
using AnnoMapEditor.UI.Controls.MapTemplates;
using AnnoMapEditor.UI.Controls.Toolbar;
using AnnoMapEditor.Utilities;

namespace AnnoMapEditor.Tests;

public class FixedIslandViewModelTests
{
    [Fact]
    public void CustomContinentalIslandDoesNotSnapWhenSnapOptionIsEnabled()
    {
        bool originalSnapSetting = ToolbarService.Instance.SnapContinentalIslandsToCorners;
        ToolbarService.Instance.SnapContinentalIslandsToCorners = true;

        try
        {
            var island = CreateContinentalIsland("moderate_c_01_colony01", "custom-islands");
            island.Position = new(50, 50);

            _ = new FixedIslandViewModel(new MapTemplate(1000, 800, null!), island);

            Assert.Equal(new Vector2(50, 50), island.Position);
            Assert.Null(island.Rotation);
        }
        finally
        {
            ToolbarService.Instance.SnapContinentalIslandsToCorners = originalSnapSetting;
        }
    }

    [Fact]
    public void VanillaContinentalIslandStillSnapsWhenSnapOptionIsEnabled()
    {
        bool originalSnapSetting = ToolbarService.Instance.SnapContinentalIslandsToCorners;
        ToolbarService.Instance.SnapContinentalIslandsToCorners = true;

        try
        {
            var island = CreateContinentalIsland("moderate_c_01", null);
            island.Position = new(50, 50);

            _ = new FixedIslandViewModel(new MapTemplate(1000, 800, null!), island);

            Assert.Equal(new Vector2(0, 0), island.Position);
            Assert.Equal((byte)3, island.Rotation);
        }
        finally
        {
            ToolbarService.Instance.SnapContinentalIslandsToCorners = originalSnapSetting;
        }
    }

    private static FixedIslandElement CreateContinentalIsland(string fileName, string? customSourceDirectory)
    {
        var islandAsset = new IslandAsset
        {
            DisplayName = fileName,
            FilePath = $"data/sessions/islands/pool/{fileName}/{fileName}.a7m",
            Thumbnail = null,
            Region = null!,
            IslandDifficulty = Array.Empty<IslandDifficulty>(),
            IslandType = new[] { IslandType.Normal },
            IslandSize = new[] { IslandSize.Continental },
            SizeInTiles = 600,
            Slots = new Dictionary<long, Slot>(),
            CustomSourceDirectory = customSourceDirectory
        };

        return new FixedIslandElement(islandAsset, IslandType.Normal);
    }
}