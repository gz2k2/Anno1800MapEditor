using AnnoMapEditor.DataArchives.Assets.Models;
using AnnoMapEditor.MapTemplates.Models;
using AnnoMapEditor.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Collections.ObjectModel;
using System.Windows.Data;
using System.Windows.Media;

namespace AnnoMapEditor.UI.Overlays.SelectSlots
{
    public class SlotAssignmentViewModel : ObservableBase
    {
        private const long GOLD_ORE_GUID = 1010507;

        public static readonly SlotAsset EmptySlot = new()
        {
            DisplayName = "Empty"
        };


        public SlotAssignment SlotAssignment { get; init; }

        public SlotAsset AssignedSlot
        {
            get => SlotAssignment.AssignedSlot ?? EmptySlot;
            set
            {
                SlotAssignment.AssignedSlot = value != EmptySlot ? value : null;
                OnPropertyChanged();

                UpdateBrushes();
            }
        }

        public ObservableCollection<SlotAsset> SlotItems { get; init; }

        public bool HasFocus
        {
            get => _hasFocus;
            private set => SetProperty(ref _hasFocus, value);
        }
        private bool _hasFocus;

        public int PathThickness
        {
            get => _pathThickness;
            private set => SetProperty(ref _pathThickness, value);
        }
        private int _pathThickness = 1;

        public Brush PinBrush
        {
            get => _pinBrush;
            private set => SetProperty(ref _pinBrush, value);
        }
        private Brush _pinBrush;

        public Brush BackgroundBrush
        {
            get => _backgroundBrush;
            private set => SetProperty(ref _backgroundBrush, value);
        }
        private Brush _backgroundBrush;

        public RegionAsset SelectedRegion
        {
            get => _selectedRegion;
            set
            {
                SetProperty(ref _selectedRegion, value);
                UpdateFilter();
            }
        }
        private RegionAsset _selectedRegion;

        private readonly HashSet<SlotAsset> _originalSlotItems;

        public bool NewWorldAllOres
        {
            get => _newWorldAllOres;
            set
            {
                SetProperty(ref _newWorldAllOres, value);
                UpdateFilter();
            }
        }
        private bool _newWorldAllOres;


        public SlotAssignmentViewModel(SlotAssignment slotAssignment, RegionAsset selectedRegion)
        {
            SlotAssignment = slotAssignment;
            _selectedRegion = selectedRegion;

            ICollection<SlotAsset> originalSlotItems = slotAssignment.Slot.SlotAsset!.ReplacementSlotAssets;
            _originalSlotItems = new(originalSlotItems);

            IEnumerable<SlotAsset> additionalOptions = IsMineSlot ? GetNewWorldOreOptions() : Enumerable.Empty<SlotAsset>();
            SlotItems = new(originalSlotItems.Concat(additionalOptions).Distinct());
            SlotItems.Add(EmptySlot);

            CollectionView fertilitiesView = (CollectionView)CollectionViewSource.GetDefaultView(SlotItems);
            fertilitiesView.Filter = SlotAssetFilter;

            UpdateBrushes();
        }


        private bool SlotAssetFilter(object item)
        {
            if (item is not SlotAsset slotAsset)
                throw new ArgumentException();

            if (slotAsset == EmptySlot || slotAsset == AssignedSlot)
                return true;

            if (NewWorldAllOres && _selectedRegion == RegionAsset.SouthAmerica && IsMineSlot)
                return slotAsset.GUID == GOLD_ORE_GUID || slotAsset.AssociatedRegions.Contains(RegionAsset.SouthAmerica);

            if (!_originalSlotItems.Contains(slotAsset))
                return false;

            // Warning: Hardcoding
            // SouthAmerica uses both random mine slots 614 and 1000029. However, none of the
            // replacements for 1000029 contain the New World as an AssociatedRegion. Despite of
            // this, it is possible to have both 1010502 Iron Deposit and 1010507 Gold Deposit in
            // the New World.
            if (_selectedRegion == RegionAsset.SouthAmerica
                && SlotAssignment.Slot.ObjectGuid == SlotAsset.RANDOM_MINE_OLD_WORLD_GUID
                && (slotAsset.GUID == 1010501 || slotAsset.GUID == GOLD_ORE_GUID))
                return true;

            if (slotAsset.AssociatedRegions.Contains(_selectedRegion))
                return true;

            return false;
        }

        private bool IsMineSlot => SlotAssignment.Slot.ObjectGuid == SlotAsset.RANDOM_MINE_OLD_WORLD_GUID
            || SlotAssignment.Slot.ObjectGuid == SlotAsset.RANDOM_MINE_NEW_WORLD_GUID
            || SlotAssignment.Slot.ObjectGuid == SlotAsset.RANDOM_MINE_ARCTIC_GUID;

        private static IEnumerable<SlotAsset> GetNewWorldOreOptions()
        {
            return new[] { SlotAsset.RandomMineOldWorld, SlotAsset.RandomMineNewWorld, SlotAsset.RandomMineArctic }
                .SelectMany(mineSlot => mineSlot.ReplacementSlotAssets)
                .Where(slot => slot.GUID == GOLD_ORE_GUID || slot.AssociatedRegions.Contains(RegionAsset.SouthAmerica))
                .DistinctBy(slot => slot.GUID);
        }

        private void UpdateFilter()
        {
            CollectionViewSource.GetDefaultView(SlotItems).Refresh();
        }

        public void OnGotFocus()
        {
            HasFocus = true;
            PathThickness = 3;
        }

        public void OnLostFocus()
        {
            HasFocus = false;
            PathThickness = 1;
        }

        private void UpdateBrushes()
        {
            if (AssignedSlot == EmptySlot)
            {
                PinBrush = Brushes.LightGray;
                BackgroundBrush = Brushes.Transparent;
            }
            else
            {
                long randomSlotGuid = SlotAssignment.Slot.ObjectGuid;

                if (randomSlotGuid == SlotAsset.RandomMineOldWorld.GUID
                    || randomSlotGuid == SlotAsset.RandomMineNewWorld.GUID
                    || randomSlotGuid == SlotAsset.RandomMineArctic.GUID)
                    PinBrush = Brushes.Gray;
                else if (randomSlotGuid == SlotAsset.RandomClay.GUID)
                    PinBrush = Brushes.Orange;
                else if (randomSlotGuid == SlotAsset.RandomOil.GUID)
                    PinBrush = Brushes.Blue;
                else
                    PinBrush = Brushes.Red;

                BackgroundBrush = PinBrush;
            }
        }
    }
}
