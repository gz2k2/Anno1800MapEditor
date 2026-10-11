using AnnoMapEditor.DataArchives.Assets.Models;
using AnnoMapEditor.MapTemplates.Models;
using AnnoMapEditor.UI.Overlays.SelectSlots;
using System.Reflection;
using System.Windows.Media;

namespace AnnoMapEditor.Tests;

public class SlotAssignmentViewModelTests
{
    [Theory]
    [InlineData(SlotAsset.RANDOM_MINE_NEW_WORLD_GUID, 0x808080)]
    [InlineData(SlotAsset.RANDOM_CLAY_GUID, 0xFFA500)]
    [InlineData(SlotAsset.RANDOM_OIL_GUID, 0xADD8E6)]
    public void RandomSlotTypeUsesDistinctLineColor(long randomSlotGuid, int expectedColor)
    {
        SetStaticRandomSlots();

        var slotAssignmentViewModel = new SlotAssignmentViewModel(
            new SlotAssignment
            {
                Slot = new Slot
                {
                    ObjectGuid = (int)randomSlotGuid,
                    SlotAsset = new SlotAsset
                    {
                        ReplacementSlotAssets = Array.Empty<SlotAsset>(),
                        AssociatedRegions = Array.Empty<RegionAsset>()
                    }
                }
            },
            null!);

        var pinColor = ((SolidColorBrush)slotAssignmentViewModel.PinBrush).Color;
        Assert.Equal(Color.FromRgb(
            (byte)((expectedColor >> 16) & 0xFF),
            (byte)((expectedColor >> 8) & 0xFF),
            (byte)(expectedColor & 0xFF)),
            pinColor);
    }

    private static void SetStaticRandomSlots()
    {
        SetStaticRandomSlot(nameof(SlotAsset.RandomMineOldWorld), SlotAsset.RANDOM_MINE_OLD_WORLD_GUID);
        SetStaticRandomSlot(nameof(SlotAsset.RandomMineNewWorld), SlotAsset.RANDOM_MINE_NEW_WORLD_GUID);
        SetStaticRandomSlot(nameof(SlotAsset.RandomMineArctic), SlotAsset.RANDOM_MINE_ARCTIC_GUID);
        SetStaticRandomSlot(nameof(SlotAsset.RandomClay), SlotAsset.RANDOM_CLAY_GUID);
        SetStaticRandomSlot(nameof(SlotAsset.RandomOil), SlotAsset.RANDOM_OIL_GUID);
    }

    private static void SetStaticRandomSlot(string propertyName, long guid)
    {
        var property = typeof(SlotAsset).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(property);
        property!.SetValue(null, new SlotAsset { GUID = guid });
    }
}
