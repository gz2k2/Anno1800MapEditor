using AnnoMapEditor.DataArchives.Assets.Models;
using AnnoMapEditor.DataArchives.Assets.Repositories;
using AnnoMapEditor.MapTemplates.Enums;
using AnnoMapEditor.Utilities;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace AnnoMapEditor.DataArchives.Assets
{
    /// <summary>Discovers self-contained custom-island folders selected by the user.</summary>
    public class CustomIslandCatalog : ObservableBase
    {
        public static CustomIslandCatalog Instance { get; } = new();

        public ObservableCollection<IslandAsset> Islands { get; } = new();

        private CustomIslandCatalog() { }

        public async Task RefreshAsync()
        {
            Islands.Clear();
            string? root = Settings.Instance.CustomIslandsPath;
            if (string.IsNullOrWhiteSpace(root) || !Directory.Exists(root) || !DataManager.Instance.IsInitialized)
                return;

            // Search recursively: creators commonly group islands by region or author below the
            // configured root. An island is identified by its a7m and matching sidecars, not by
            // the name of its parent directory.
            foreach (string a7m in Directory.EnumerateFiles(root, "*.a7m", SearchOption.AllDirectories))
            {
                string directory = Path.GetDirectoryName(a7m)!;
                string name = Path.GetFileNameWithoutExtension(a7m);
                string a7minfo = Path.ChangeExtension(a7m, ".a7minfo");
                // .a7me is optional for custom islands; the editor only needs the map and its
                // info sidecar to determine size and slots.
                if (!File.Exists(a7minfo))
                    continue;

                try
                {
                    // Reuse the proven a7minfo/thumbnail reader with the custom root as archive.
                    string relativeA7m = Path.GetRelativePath(root, a7m);
                    FixedIslandRepository reader = new(new FolderDataArchive(root));
                    FixedIslandAsset fixedIsland = await reader.LoadIslandAssetAsync(relativeA7m);
                    string relativeDirectory = Path.GetRelativePath(root, directory);
                    string virtualPath = Path.Combine("data", "ame", "sessions", "islands", "pool", "custom", relativeDirectory, name + ".a7m");
                    IslandAsset island = new()
                    {
                        FilePath = virtualPath,
                        CustomSourceDirectory = directory,
                        DisplayName = name,
                        Thumbnail = fixedIsland.Thumbnail,
                        Region = RegionAsset.DetectFromPath(name),
                        IslandDifficulty = new[] { IslandDifficulty.Normal },
                        IslandType = new[] { IslandType.Normal },
                        IslandSize = new[] { IslandSize.All.FirstOrDefault(s => fixedIsland.SizeInTiles <= s.DefaultSizeInTiles)! },
                        SizeInTiles = fixedIsland.SizeInTiles,
                        Slots = fixedIsland.Slots
                    };
                    Islands.Add(island);
                    DataManager.Instance.IslandRepository.AddCustomIsland(island);
                }
                catch (Exception ex)
                {
                    DataManager._logger.LogWarning($"Could not load custom island '{directory}': {ex.Message}");
                }
            }
        }
    }
}
