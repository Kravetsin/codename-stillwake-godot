using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Esc's order: the menu shown first, then what is open or in hand (0022), then the menu opened.</summary>
public class EscapeTests
{
    [Fact]
    public void PutsAwayWhatIsOpenOrInHandBeforeItOpensTheMenu()
    {
        Assert.Equal(EscapeDoes.PutAway, Escape.Of(MenuPage.Closed, busy: true));
        Assert.Equal(EscapeDoes.OpenMenu, Escape.Of(MenuPage.Closed, busy: false));
    }

    [Fact]
    public void ClosesTheMenuItHasOpenedAPageAtATime()
    {
        Assert.Equal(EscapeDoes.Back, Escape.Of(MenuPage.Settings, busy: false));
        Assert.Equal(EscapeDoes.Back, Escape.Of(MenuPage.Coop, busy: false));
        Assert.Equal(EscapeDoes.CloseMenu, Escape.Of(MenuPage.Main, busy: false));
    }

    [Fact]
    public void TheMenuShownTakesEscBeforeAnythingBehindIt()
    {
        // Nothing opens behind the menu, which keeps the keys; were anything open, the menu still goes first.
        Assert.Equal(EscapeDoes.CloseMenu, Escape.Of(MenuPage.Main, busy: true));
        Assert.Equal(EscapeDoes.Back, Escape.Of(MenuPage.Settings, busy: true));
    }

    [Fact]
    public void ThreePressesWithTheInventoryOpenCloseItOpenTheMenuAndCloseIt()
    {
        bool inventory = true;
        var page = MenuPage.Closed;
        void Press()
        {
            switch (Escape.Of(page, inventory))
            {
                case EscapeDoes.PutAway: inventory = false; break;
                case EscapeDoes.OpenMenu or EscapeDoes.Back: page = MenuPage.Main; break;
                case EscapeDoes.CloseMenu: page = MenuPage.Closed; break;
            }
        }
        Press();
        Assert.Equal((false, MenuPage.Closed), (inventory, page));
        Press();
        Assert.Equal((false, MenuPage.Main), (inventory, page));
        Press();
        Assert.Equal((false, MenuPage.Closed), (inventory, page));
    }
}
