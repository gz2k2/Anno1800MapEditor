using AnnoMapEditor.DataArchives.Assets.Models;
using AnnoMapEditor.MapTemplates.Enums;
using AnnoMapEditor.UI.Windows.Main;

namespace AnnoMapEditor.Tests;

public class CustomIslandMenuGroupingTests
{
    [Fact]
    public void RootLevelIslandHasNoGroup()
    {
        string root = Path.Combine(Path.GetTempPath(), "custom-islands");
        IslandAsset island = CreateIsland(Path.Combine(root, "colony01_l_01"));

        var entry = Assert.Single(MainWindowViewModel.GroupCustomIslands(new[] { island }, root));

        Assert.Null(entry.GroupPath);
        Assert.Null(entry.GroupName);
        Assert.Same(island, entry.Island);
    }

    [Fact]
    public void NestedIslandsUseOnlyTheirImmediateParentAsGroup()
    {
        string root = Path.Combine(Path.GetTempPath(), "custom-islands");
        string asia = Path.Combine(root, "author", "maps", "asia");
        IslandAsset firstIsland = CreateIsland(Path.Combine(asia, "colony01_l_01"));
        IslandAsset secondIsland = CreateIsland(Path.Combine(asia, "colony01_l_02"));

        var entries = MainWindowViewModel.GroupCustomIslands(new[] { firstIsland, secondIsland }, root).ToArray();

        Assert.Equal(2, entries.Length);
        Assert.All(entries, entry =>
        {
            Assert.Equal(asia, entry.GroupPath);
            Assert.Equal("asia", entry.GroupName);
        });
    }

    [Fact]
    public void SameNamedParentFoldersRemainSeparateGroups()
    {
        string root = Path.Combine(Path.GetTempPath(), "custom-islands");
        IslandAsset firstIsland = CreateIsland(Path.Combine(root, "author-a", "asia", "colony01_l_01"));
        IslandAsset secondIsland = CreateIsland(Path.Combine(root, "author-b", "asia", "colony01_l_02"));

        var entries = MainWindowViewModel.GroupCustomIslands(new[] { firstIsland, secondIsland }, root).ToArray();

        Assert.Equal("asia", entries[0].GroupName);
        Assert.Equal("asia", entries[1].GroupName);
        Assert.NotEqual(entries[0].GroupPath, entries[1].GroupPath);
    }

    private static IslandAsset CreateIsland(string sourceDirectory) => new()
    {
        DisplayName = Path.GetFileName(sourceDirectory),
        FilePath = Path.Combine(sourceDirectory, Path.GetFileName(sourceDirectory) + ".a7m"),
        Region = null!,
        IslandDifficulty = Array.Empty<IslandDifficulty>(),
        IslandType = new[] { IslandType.Normal },
        IslandSize = new[] { IslandSize.Small },
        SizeInTiles = IslandSize.Small.DefaultSizeInTiles,
        Slots = new Dictionary<long, Slot>(),
        CustomSourceDirectory = sourceDirectory
    };
}