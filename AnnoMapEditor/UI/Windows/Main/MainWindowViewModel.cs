using System.Collections.Specialized;
using System.Collections.Generic;
using AnnoMapEditor.DataArchives;
using AnnoMapEditor.DataArchives.Assets.Models;
using AnnoMapEditor.DataArchives.Assets;
using AnnoMapEditor.MapTemplates.Models;
using AnnoMapEditor.MapTemplates.Enums;
using AnnoMapEditor.MapTemplates.Serializing;
using AnnoMapEditor.UI.Controls;
using AnnoMapEditor.UI.Controls.IslandProperties;
using AnnoMapEditor.UI.Controls.MapTemplates;
using AnnoMapEditor.UI.Overlays.SelectIsland;
using AnnoMapEditor.Utilities;
using AnnoMapEditor.Utilities.UndoRedo;
using Microsoft.Win32;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Input;
using AnnoMapEditor.UI.Controls.Toolbar;
using AnnoMapEditor.UI.Overlays;
using AnnoMapEditor.UI.Overlays.ExportAsMod;
using MapTemplate = AnnoMapEditor.MapTemplates.Models.MapTemplate;
using System;
using System.Linq;

namespace AnnoMapEditor.UI.Windows.Main
{
    public class MainWindowViewModel : ObservableBase
    {
        public DataManager DataManager => DataManager.Instance;

        public UndoRedoStack UndoRedoStack => UndoRedoStack.Instance;

        public MapTemplate MapTemplate
        {
            get => _mapTemplate;
            private set
            {
                if (value != _mapTemplate)
                {
                    SetProperty(ref _mapTemplate, value);
                    SelectedIsland = null;
                    value.Elements.CollectionChanged += MapTemplate_ElementsChanged;

                    MapTemplateProperties = new(value);
                    MapTemplateChecker = new(value);

                    // A new map has been loaded or created. Clear Stack
                    UndoRedoStack.ClearStacks();
                }
            }
        }
        private MapTemplate _mapTemplate;
        
        public ToolbarViewModel ToolbarViewModel => new ToolbarViewModel();

        public MapTemplatePropertiesViewModel MapTemplateProperties 
        { 
            get => _mapTemplateProperties; 
            private set => SetProperty(ref _mapTemplateProperties, value); 
        }
        private MapTemplatePropertiesViewModel _mapTemplateProperties;

        public MapTemplateCheckerViewModel MapTemplateChecker
        {
            get => _mapTemplateChecker;
            private set => SetProperty(ref _mapTemplateChecker, value);
        }
        private MapTemplateCheckerViewModel _mapTemplateChecker;

        public string Status { 
            get => _status; 
            set 
            { 
                OnPropertyChanged(nameof(Status)); 
                _status = value;
            } 
        }
        private string _status = "Unknown Status";

        public IslandElement? SelectedIsland
        {
            get => _selectedIsland;
            set
            {
                SetProperty(ref _selectedIsland, value);
                ToolbarService.Instance.SelectedIslandActionsEnabled = value != null;
                SelectedUnifiedIslandPropertiesViewModel = value == null ? null : new UnifiedIslandPropertiesViewModel(value, MapTemplate);
            }
        }
        private IslandElement? _selectedIsland;

        private IslandElement? _copiedIsland;
        private int _pasteCount;

        /**
         * Make sure to notice if an element has been remove from the canvas. If it was selected, deselect it.
         */
        private void MapTemplate_ElementsChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Elements list changed!");
            if (SelectedIsland == null && SelectedUnifiedIslandPropertiesViewModel != null)
                SelectedUnifiedIslandPropertiesViewModel = null;
            
            if (SelectedIsland != null && !_mapTemplate.Elements.Contains(SelectedIsland))
            {
                System.Diagnostics.Debug.WriteLine("Selected Island is not contained in the map elements list!");
                SelectedIsland = null;
            }
        }
        
        public UnifiedIslandPropertiesViewModel? SelectedUnifiedIslandPropertiesViewModel
        {
            get => _selectedUnifiedIslandPropertiesViewModel;
            set => SetProperty(ref _selectedUnifiedIslandPropertiesViewModel, value);
        }

        private UnifiedIslandPropertiesViewModel? _selectedUnifiedIslandPropertiesViewModel;

        public SelectIslandViewModel? SelectIslandViewModel
        {
            get => _selectIslandViewModel;
            set
            {
                SetProperty(ref _selectIslandViewModel, value);
                OnPropertyChanged(nameof(ShowOverlay));
            }
        }
        private SelectIslandViewModel? _selectIslandViewModel;

        public bool ShowOverlay => SelectIslandViewModel != null;

        public string? MapTemplateFilePath
        {
            get => _mapTemplateFilePath;
            private set => SetProperty(ref _mapTemplateFilePath, value);
        }
        private string? _mapTemplateFilePath;

        public MainWindowViewModel(MapTemplate mapTemplate)
        {
            MapTemplate = mapTemplate;
            CommonConstructor();
        }

        public MainWindowViewModel()
        {
            CreateNewMap();
            CommonConstructor();
        }

        private void CommonConstructor()
        {
            if (DataManager.IsInitialized)
            {
                Status = $"Game Path: {Settings.Instance.GamePath}";
                _ = CustomIslandCatalog.Instance.RefreshAsync();
            }
            else
                Status = "Game files are not loaded. Island placement is unavailable.";
            
            MapTemplate.ShowLabels = true;
            
            // Toolbar button events to listen for
            ToolbarService.Instance.ButtonClicked += (sender, e) =>
            {
                switch (e.ButtonType)
                {
                    case ToolbarButtonType.DeleteIsland:
                        DeleteSelectedIsland();
                        break;
                    case ToolbarButtonType.CopyIsland:
                        CopySelectedIsland();
                        break;
                    case ToolbarButtonType.PasteIsland:
                        PasteIsland();
                        break;
                    case ToolbarButtonType.NewMap:
                        CreateNewMap();
                        break;
                    case ToolbarButtonType.ExportMap:
                        // TODO: To be replaced by a proper window.
                        OverlayService.Instance.Show(new ExportAsModViewModel(MapTemplate));
                        break;
                    case ToolbarButtonType.LoadMap:
                        OpenMapFileDialog();
                        break;
                    case ToolbarButtonType.ImportMap:
                        if (sender is Button { ContextMenu: not null } button) PopulateOpenMapMenu(button.ContextMenu, true);
                        break;
                    case ToolbarButtonType.SaveMap:
                        SaveMapFileDialog();
                        break;
                    case ToolbarButtonType.OpenSettingsWindow:
                        if (sender is Button { ContextMenu: not null } customIslandsButton)
                            PopulateCustomIslandMenu(customIslandsButton.ContextMenu);
                        break;
                    default: break;
                }
            };
        }

        public async Task OpenMap(string a7tinfoPath, bool fromArchive = false)
        {
            await CustomIslandCatalog.Instance.RefreshAsync();
            MapTemplateFilePath = Path.GetFileName(a7tinfoPath);
            MapTemplateReader mapTemplateReader = new();

            if (fromArchive)
                MapTemplate = await mapTemplateReader.FromDataArchiveAsync(a7tinfoPath);

            else
                MapTemplate = await mapTemplateReader.FromFileAsync(a7tinfoPath);

            // TODO: Find a better solution for this "Hack"
            ToolbarService.Instance.ButtonClick(ToolbarButtonType.ZoomReset);
        }

        public void CreateNewMap()
        {
            const int DEFAULT_MAP_SIZE = 2560;
            const int DEFAULT_PLAYABLE_SIZE = 2160;

            MapTemplateFilePath = null;
            MapTemplate = new MapTemplate(DEFAULT_MAP_SIZE, DEFAULT_PLAYABLE_SIZE, SessionAsset.OldWorld); 
            // TODO: Find a better solution for this "Hack"
            ToolbarService.Instance.ButtonClick(ToolbarButtonType.ZoomReset);
        }

        public async Task SaveMap(string filePath)
        {
            if (MapTemplate is null)
                return;

            MapTemplateFilePath = Path.GetFileName(filePath);
            MapTemplateWriter mapTemplateWriter = new();

            if (Path.GetExtension(filePath).ToLower() == ".a7tinfo")
                await mapTemplateWriter.WriteA7tinfoAsync(MapTemplate, filePath);
            else
                await mapTemplateWriter.WriteXmlAsync(MapTemplate, filePath);
        }

        public void PopulateOpenMapMenu(ContextMenu menu, bool import_only = false)
        {
            menu.Items.Clear();

            if (!import_only) 
            { 
                MenuItem openMapFile = new() { Header = "Open file..." };
                openMapFile.Click += (_, _) => OpenMapFileDialog();
                menu.Items.Add(openMapFile);
                menu.Items.Add(new Separator());

                MenuItem newFile = new() { Header = "New Map file" };
                newFile.Click += (_, _) => CreateNewMap();
                menu.Items.Add(newFile);
                menu.Items.Add(new Separator());
            }
            else 
            {
                menu.Items.Add(new MenuItem()
                {
                    Header = "Load map template from game...",
                    IsEnabled = false
                }); 
                menu.Items.Add(new Separator());
            }

            foreach (MapGroup group in DataManager.Instance.MapGroupRepository.MapGroups)
            {
                MenuItem groupMenu = new() { Header = group.Name };

                foreach (MapInfo map in group.Maps)
                {
                    MenuItem mapMenu = new() { Header = map.Name, DataContext = map };
                    mapMenu.Click += (_, _) => _ = OpenMap(map.FileName!, true);
                    groupMenu.Items.Add(mapMenu);
                }

                menu.Items.Add(groupMenu);
            }
        }

        public void PopulateCustomIslandMenu(ContextMenu menu)
        {
            menu.Items.Clear();

            MenuItem selectFolder = new() { Header = "Custom-island folder..." };
            selectFolder.Click += (_, _) => SelectCustomIslandFolder();
            menu.Items.Add(selectFolder);
            menu.Items.Add(new Separator());

            if (CustomIslandCatalog.Instance.Islands.Count == 0)
            {
                menu.Items.Add(new MenuItem { Header = "no Custom Islands loaded", IsEnabled = false });
                return;
            }

            Dictionary<string, MenuItem> groupMenus = new(StringComparer.OrdinalIgnoreCase);
            foreach ((string? groupPath, string? groupName, IslandAsset island) in GroupCustomIslands(
                CustomIslandCatalog.Instance.Islands,
                Settings.Instance.CustomIslandsPath))
            {
                if (groupPath is null)
                {
                    AddCustomIslandMenuItem(menu, island);
                    continue;
                }

                if (!groupMenus.TryGetValue(groupPath, out MenuItem? groupMenu))
                {
                    groupMenu = new MenuItem { Header = groupName };
                    groupMenus.Add(groupPath, groupMenu);
                    menu.Items.Add(groupMenu);
                }

                AddCustomIslandMenuItem(groupMenu, island);
            }
        }

        internal static IEnumerable<(string? GroupPath, string? GroupName, IslandAsset Island)> GroupCustomIslands(
            IEnumerable<IslandAsset> islands,
            string? rootPath)
        {
            string? fullRootPath = string.IsNullOrWhiteSpace(rootPath)
                ? null
                : Path.TrimEndingDirectorySeparator(Path.GetFullPath(rootPath));

            foreach (IslandAsset island in islands)
            {
                if (fullRootPath is null || string.IsNullOrWhiteSpace(island.CustomSourceDirectory))
                {
                    yield return (null, null, island);
                    continue;
                }

                string islandDirectory = Path.TrimEndingDirectorySeparator(
                    Path.GetFullPath(island.CustomSourceDirectory));
                string? parentDirectory = Path.GetDirectoryName(islandDirectory);
                if (parentDirectory is null)
                {
                    yield return (null, null, island);
                    continue;
                }

                string groupPath = Path.TrimEndingDirectorySeparator(Path.GetFullPath(parentDirectory));
                if (Path.GetRelativePath(fullRootPath, groupPath) == ".")
                {
                    yield return (null, null, island);
                    continue;
                }

                yield return (groupPath, Path.GetFileName(groupPath), island);
            }
        }

        private void AddCustomIslandMenuItem(ItemsControl parentMenu, IslandAsset island)
        {
            MenuItem islandMenu = new() { Header = island.DisplayName, DataContext = island };
            islandMenu.Click += (_, _) => AddCustomIsland(island);
            parentMenu.Items.Add(islandMenu);
        }

        private async void SelectCustomIslandFolder()
        {
            var picker = new Ookii.Dialogs.Wpf.VistaFolderBrowserDialog
            {
                UseDescriptionForTitle = true,
                Description = "Select folder with Custom-Island subfolders"
            };
            if (!string.IsNullOrWhiteSpace(Settings.Instance.CustomIslandsPath))
                picker.SelectedPath = Settings.Instance.CustomIslandsPath;

            if (true == picker.ShowDialog())
            {
                Settings.Instance.CustomIslandsPath = picker.SelectedPath;
                await CustomIslandCatalog.Instance.RefreshAsync();
            }
        }

        private void AddCustomIsland(IslandAsset island)
        {
            IslandType islandType = island.IslandType.FirstOrDefault() ?? IslandType.Normal;
            FixedIslandElement element = new(island, islandType)
            {
                Label = island.DisplayName,
                Position = new((MapTemplate.Size.X - island.SizeInTiles) / 2, (MapTemplate.Size.Y - island.SizeInTiles) / 2)
            };
            MapTemplate.Elements.Add(element);
            UndoRedoStack.Instance.Do(new IslandAddStackEntry(element, MapTemplate));
            SelectedIsland = element;
        }

        public async void OpenMapFileDialog()
        {
            var picker = new OpenFileDialog
            {
                Filter = "Map templates (*.a7tinfo, *.xml)|*.a7tinfo;*.xml"
            };

            if (true == picker.ShowDialog())
            {
                int end = picker.FileName.IndexOf(@"\data\session");
                if (end == -1)
                    end = picker.FileName.IndexOf(@"\data\dlc");
                if (end != -1)
                    Settings.Instance.GamePath = picker.FileName[..end];

                await OpenMap(picker.FileName);
            }
        }

        public async void SaveMapFileDialog()
        {
            var picker = new SaveFileDialog
            {
                DefaultExt = ".a7tinfo",
                Filter = "Map template (*.a7tinfo)|*.a7tinfo|XML map template (*.xml)|*.xml",
                FilterIndex = Path.GetExtension(MapTemplateFilePath)?.ToLower() == ".xml" ? 2 : 1,
                FileName = Path.GetFileName(MapTemplateFilePath),
                OverwritePrompt = true
            };

            if (true == picker.ShowDialog() && picker.FileName is not null)
            {
                await SaveMap(picker.FileName);
            }
        }

        public void Undo()
        {
            UndoRedoStack.Instance.Undo();
        }

        public void Redo()
        {
            UndoRedoStack.Instance.Redo();
        }

        private void DeleteSelectedIsland()
        {
            if (SelectedIsland == null) return;
            UndoRedoStack.Instance.Do(new IslandRemoveStackEntry(SelectedIsland, MapTemplate));
            MapTemplate.Elements.Remove(SelectedIsland);
            // SelectedIsland = null;
            // SelectedUnifiedIslandPropertiesViewModel = null;
        }

        private void CopySelectedIsland()
        {
            if (SelectedIsland == null) return;

            _copiedIsland = CloneIsland(SelectedIsland);
            _pasteCount = 0;
            ToolbarService.Instance.CanPasteIsland = true;
        }

        private void PasteIsland()
        {
            if (_copiedIsland == null) return;

            const int PASTE_OFFSET = 80;
            IslandElement pastedIsland = CloneIsland(_copiedIsland);
            int offset = PASTE_OFFSET * ++_pasteCount;
            int maxX = Math.Max(0, MapTemplate.Size.X - pastedIsland.SizeInTiles);
            int maxY = Math.Max(0, MapTemplate.Size.Y - pastedIsland.SizeInTiles);
            maxX = maxX / 8 * 8;
            maxY = maxY / 8 * 8;
            pastedIsland.Position = new Vector2(
                Math.Clamp(_copiedIsland.Position.X + offset, 0, maxX),
                Math.Clamp(_copiedIsland.Position.Y + offset, 0, maxY));

            MapTemplate.Elements.Add(pastedIsland);
            UndoRedoStack.Instance.Do(new IslandAddStackEntry(pastedIsland, MapTemplate));
        }

        private static IslandElement CloneIsland(IslandElement island)
        {
            if (island is FixedIslandElement fixedIsland)
            {
                FixedIslandElement clone = new(fixedIsland.IslandAsset, fixedIsland.IslandType)
                {
                    Position = new Vector2(fixedIsland.Position),
                    Label = fixedIsland.Label,
                    IslandDifficulty = fixedIsland.IslandDifficulty,
                    Rotation = fixedIsland.Rotation,
                    RandomizeFertilities = fixedIsland.RandomizeFertilities,
                    RandomizeSlots = fixedIsland.RandomizeSlots
                };

                foreach (var fertility in fixedIsland.Fertilities)
                    clone.Fertilities.Add(fertility);

                foreach (var (objectId, assignment) in fixedIsland.SlotAssignments)
                {
                    if (clone.SlotAssignments.TryGetValue(objectId, out var cloneAssignment))
                        cloneAssignment.AssignedSlot = assignment.AssignedSlot;
                }

                return clone;
            }

            if (island is RandomIslandElement randomIsland)
            {
                return new RandomIslandElement(randomIsland.IslandSize, randomIsland.IslandType)
                {
                    Position = new Vector2(randomIsland.Position),
                    Label = randomIsland.Label,
                    IslandDifficulty = randomIsland.IslandDifficulty
                };
            }

            throw new NotSupportedException($"Cannot copy island type '{island.GetType().Name}'.");
        }

        public ICommand UndoCommand => new ActionCommand(_ => Undo());
        public ICommand RedoCommand => new ActionCommand(_ => Redo());
        public ICommand CopyCommand => new ActionCommand(_ => CopySelectedIsland());
        public ICommand PasteCommand => new ActionCommand(_ => PasteIsland());
        public ICommand RemoveCommand => new ActionCommand(_ => DeleteSelectedIsland());
    }
}
