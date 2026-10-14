using Stillwake.Game.Features.Items;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The wheel on the hotbar: apps/game/src/features/items/hotbar.test.ts.</summary>
public class HotbarTests
{
    [Fact]
    public void TurnsToTheNextSlotDownAndTheOneBeforeUpRoundTheRow()
    {
        Assert.Equal(1, Hotbar.WheelSlot(0, 100));
        Assert.Equal(0, Hotbar.WheelSlot(Hotbar.Row - 1, 3));
        Assert.Equal(Hotbar.Row - 1, Hotbar.WheelSlot(0, -100));
        Assert.Equal(3, Hotbar.WheelSlot(4, -1));
        Assert.Equal(4, Hotbar.WheelSlot(4, 0));
    }
}
