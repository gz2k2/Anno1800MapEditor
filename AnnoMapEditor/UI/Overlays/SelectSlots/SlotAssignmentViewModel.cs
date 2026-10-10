using AnnoMapEditor.DataArchives.Assets.Models;
using AnnoMapEditor.MapTemplates.Models;
using AnnoMapEditor.Utilities;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Data;
using System.Windows.Media;

namespace AnnoMapEditor.UI.Overlays.SelectSlots
{
    public class SlotAssignmentViewModel : ObservableBase
    {
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
                UpdateSlotItems();
                UpdateFilter();
            }
        }
        private RegionAsset _selectedRegion;


        public SlotAssignmentViewModel(SlotAssignment slotAssignment, RegionAsset selectedRegion)
        {
            SlotAssignment = slotAssignment;
            _selectedRegion = selectedRegion;

            SlotItems = new();
            UpdateSlotItems();

            CollectionView fertilitiesView = (CollectionView)CollectionViewSource.GetDefaultView(SlotItems);
            fertilitiesView.Filter = SlotAssetFilter;

            UpdateBrushes();
        }


        private bool SlotAssetFilter(object item)
        {
            if (item is not SlotAsset slotAsset)
                throw new ArgumentException();

            if (_selectedRegion == RegionAsset.SouthAmerica && slotAsset.GUID == 1010501)
                return false;

            if (slotAsset == EmptySlot || slotAsset == AssignedSlot)
                return true;

            if (_selectedRegion == RegionAsset.SouthAmerica
                && slotAsset.GUID == 1010507)
                return true;

            if (slotAsset.AssociatedRegions.Contains(_selectedRegion))
                return true;

            return false;
        }

        private void UpdateSlotItems()
        {
            SlotItems.Clear();

            long slotGuid = SlotAssignment.Slot.ObjectGuid;
            SlotAsset? regionalMineSlot = null;
            if (slotGuid == SlotAsset.RANDOM_MINE_OLD_WORLD_GUID
                || slotGuid == SlotAsset.RANDOM_MINE_NEW_WORLD_GUID
                || slotGuid == SlotAsset.RANDOM_MINE_ARCTIC_GUID)
            {
                if (_selectedRegion == RegionAsset.SouthAmerica)
                    regionalMineSlot = SlotAsset.RandomMineNewWorld;
                else if (_selectedRegion == RegionAsset.Arctic)
                    regionalMineSlot = SlotAsset.RandomMineArctic;
                else if (_selectedRegion == RegionAsset.Moderate)
                    regionalMineSlot = SlotAsset.RandomMineOldWorld;
            }

            IEnumerable<SlotAsset> replacements = regionalMineSlot?.ReplacementSlotAssets
                ?? SlotAssignment.Slot.SlotAsset!.ReplacementSlotAssets;

            foreach (SlotAsset replacement in replacements)
                SlotItems.Add(replacement);

            if (_selectedRegion == RegionAsset.SouthAmerica && regionalMineSlot != null)
            {
                foreach (SlotAsset replacement in SlotAsset.RandomMineOldWorld.ReplacementSlotAssets
                             .Where(asset => asset.GUID == 1010507))
                {
                    if (!SlotItems.Contains(replacement))
                        SlotItems.Add(replacement);
                }
            }

            if (AssignedSlot != EmptySlot && !SlotItems.Contains(AssignedSlot))
                SlotItems.Add(AssignedSlot);

            SlotItems.Add(EmptySlot);
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
                    PinBrush = Brushes.SandyBrown;
                else if (randomSlotGuid == SlotAsset.RandomOil.GUID)
                    PinBrush = Brushes.DarkSlateGray;
                else
                    PinBrush = Brushes.Red;

                BackgroundBrush = PinBrush;
            }
        }
    }
}
