using Stillwake.Game.Features.Items;
using Stillwake.Sim.Items;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>The equipment block (0047): which slot is which, and which light up while a stack is carried.</summary>
public class EquipmentTests
{
    private static readonly Place Head = new(Spec.Inventory.Slots), Back = new(Spec.Inventory.Slots + 1), Belt = new(Spec.Inventory.Slots + 2);

    [Fact]
    public void KnowsTheEquipmentSlotsByTheBodyAfterTheInventorysOwn()
    {
        Assert.Equal([Head], Equipment.Of("head"));
        Assert.Equal([Back], Equipment.Of("back"));
        Assert.Equal(3, Equipment.Of("belt").Count());
        Assert.False(Equipment.Is(new Place(Spec.Inventory.Slots - 1)));
        Assert.False(Equipment.Is(new Place(Spec.Inventory.Slots + 1, 4)));
        Assert.False(Equipment.Is(new Place(Spec.Inventory.Total)));
        Assert.Equal("back", Equipment.KindOf(Back));
    }

    [Fact]
    public void LightsOnlyTheSlotsThatWouldWearWhatIsCarriedAndDimsTheRest()
    {
        Assert.Equal(SlotGlow.Takes, Equipment.GlowOf(Back, "jetpack"));
        Assert.Equal(SlotGlow.Refuses, Equipment.GlowOf(Head, "jetpack"));
        Assert.Equal(SlotGlow.Refuses, Equipment.GlowOf(Belt, "jetpack"));
        Assert.All(new[] { Head, Back, Belt }, p => Assert.Equal(SlotGlow.Refuses, Equipment.GlowOf(p, "scrap")));
        Assert.All(new[] { Head, Back, Belt }, p => Assert.Equal(SlotGlow.None, Equipment.GlowOf(p, null)));
        Assert.Equal(SlotGlow.None, Equipment.GlowOf(new Place(3), "jetpack"));
    }
}
