using AnnoMapEditor.DataArchives.Assets.Models;
using AnnoMapEditor.MapTemplates.Models;
using AnnoMapEditor.UI.Overlays.SelectSlots;
using System.Reflection;
using System.Windows.Data;
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

    [Fact]
    public void NewWorldAllOresToggleAddsNewWorldOresAndGoldToEveryMineSlot()
    {
        SetStaticRandomSlots();
        var originalRandomMineOldWorld = SlotAsset.RandomMineOldWorld;
        var originalRandomMineNewWorld = SlotAsset.RandomMineNewWorld;

        var southAmericaProperty = typeof(RegionAsset).GetProperty(nameof(RegionAsset.SouthAmerica), BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(southAmericaProperty);
        var originalSouthAmerica = (RegionAsset?)southAmericaProperty!.GetValue(null);
        var southAmerica = new RegionAsset(new System.Xml.Linq.XElement("Values",
            new System.Xml.Linq.XElement("Standard", new System.Xml.Linq.XElement("GUID", RegionAsset.REGION_SOUTHAMERICA_GUID)),
            new System.Xml.Linq.XElement("Region", new System.Xml.Linq.XElement("RegionID", "Colony01")),
            new System.Xml.Linq.XElement("Text", new System.Xml.Linq.XElement("LocaText",
                new System.Xml.Linq.XElement("English", new System.Xml.Linq.XElement("Text", "The New World"))))));
        southAmericaProperty.SetValue(null, southAmerica);

        try
        {
            var newWorldOre = new SlotAsset { GUID = 1372, AssociatedRegions = new[] { southAmerica } };
            var goldOre = new SlotAsset { GUID = 1010507, AssociatedRegions = Array.Empty<RegionAsset>() };
            SetStaticRandomSlot(nameof(SlotAsset.RandomMineNewWorld), new SlotAsset
            {
                GUID = SlotAsset.RANDOM_MINE_NEW_WORLD_GUID,
                ReplacementSlotAssets = new[] { newWorldOre }
            });
            SetStaticRandomSlot(nameof(SlotAsset.RandomMineOldWorld), new SlotAsset
            {
                GUID = SlotAsset.RANDOM_MINE_OLD_WORLD_GUID,
                ReplacementSlotAssets = new[] { goldOre }
            });

            foreach (long mineGuid in new[]
            {
                SlotAsset.RANDOM_MINE_OLD_WORLD_GUID,
                SlotAsset.RANDOM_MINE_NEW_WORLD_GUID,
                SlotAsset.RANDOM_MINE_ARCTIC_GUID
            })
            {
                var unrelatedOre = new SlotAsset { GUID = 1234, AssociatedRegions = Array.Empty<RegionAsset>() };
                var viewModel = new SlotAssignmentViewModel(
                    new SlotAssignment
                    {
                        Slot = new Slot
                        {
                            ObjectGuid = (int)mineGuid,
                            SlotAsset = new SlotAsset
                            {
                                ReplacementSlotAssets = new[] { unrelatedOre },
                                AssociatedRegions = Array.Empty<RegionAsset>()
                            }
                        }
                    },
                    southAmerica);

                var visibleSlots = CollectionViewSource.GetDefaultView(viewModel.SlotItems).Cast<SlotAsset>().ToArray();
                Assert.DoesNotContain(newWorldOre, visibleSlots);
                Assert.DoesNotContain(goldOre, visibleSlots);

                viewModel.NewWorldAllOres = true;
                visibleSlots = CollectionViewSource.GetDefaultView(viewModel.SlotItems).Cast<SlotAsset>().ToArray();
                Assert.Contains(newWorldOre, visibleSlots);
                Assert.Contains(goldOre, visibleSlots);
                Assert.DoesNotContain(unrelatedOre, visibleSlots);

                viewModel.SelectedRegion = new RegionAsset(new System.Xml.Linq.XElement("Values",
                    new System.Xml.Linq.XElement("Standard", new System.Xml.Linq.XElement("GUID", RegionAsset.REGION_MODERATE_GUID)),
                    new System.Xml.Linq.XElement("Region", new System.Xml.Linq.XElement("RegionID", RegionAsset.REGION_MODERATE_REGIONID)),
                    new System.Xml.Linq.XElement("Text", new System.Xml.Linq.XElement("LocaText",
                        new System.Xml.Linq.XElement("English", new System.Xml.Linq.XElement("Text", "The Old World"))))));
                visibleSlots = CollectionViewSource.GetDefaultView(viewModel.SlotItems).Cast<SlotAsset>().ToArray();
                Assert.DoesNotContain(newWorldOre, visibleSlots);
                Assert.DoesNotContain(goldOre, visibleSlots);

                viewModel.NewWorldAllOres = false;
                visibleSlots = CollectionViewSource.GetDefaultView(viewModel.SlotItems).Cast<SlotAsset>().ToArray();
                Assert.DoesNotContain(newWorldOre, visibleSlots);
                Assert.DoesNotContain(goldOre, visibleSlots);
            }
        }
        finally
        {
            southAmericaProperty.SetValue(null, originalSouthAmerica);
            SetStaticRandomSlot(nameof(SlotAsset.RandomMineOldWorld), originalRandomMineOldWorld);
            SetStaticRandomSlot(nameof(SlotAsset.RandomMineNewWorld), originalRandomMineNewWorld);
        }
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
        SetStaticRandomSlot(propertyName, new SlotAsset { GUID = guid });
    }

    private static void SetStaticRandomSlot(string propertyName, SlotAsset slot)
    {
        var property = typeof(SlotAsset).GetProperty(propertyName, BindingFlags.Public | BindingFlags.Static);
        Assert.NotNull(property);
        property!.SetValue(null, slot);
    }
}
