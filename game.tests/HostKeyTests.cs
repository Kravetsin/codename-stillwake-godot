using Stillwake.Game.Menu;
using Xunit;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Tests;

/// <summary>The code a host gives out as its world opens and in the Esc menu.</summary>
public class HostKeyTests
{
    private static readonly TogetherState Hosting = new(TogetherMode.Hosting, "KQ7M", [0x3b82f6], null, false);

    [Fact]
    public void ShowsTheCodeToCopyOnceTheSessionIsOpen()
    {
        Assert.Equal("KQ7M", HostKey.Code(Hosting));
        Assert.True(HostKey.Copyable(Hosting));
        Assert.Equal(T("menu.waiting"), HostKey.Line(Hosting));
    }

    [Fact]
    public void ShowsDotsAndNothingToCopyWhileTheRelayOpensIt()
    {
        var opening = Hosting with { Mode = TogetherMode.Opening };
        Assert.Equal("····", HostKey.Code(opening));
        Assert.False(HostKey.Copyable(opening));
        Assert.Equal(T("menu.opening"), HostKey.Line(opening));
    }

    [Fact]
    public void CountsTheFriendsAboardNotTheHost()
    {
        Assert.Equal(T("menu.oneAboard"), HostKey.Line(Hosting with { Suits = [1, 2] }));
        Assert.Equal(T("menu.manyAboard", ("count", 3)), HostKey.Line(Hosting with { Suits = [1, 2, 3, 4] }));
    }

    [Fact]
    public void ShowsNoCodeButWhyWhenTheSessionDidNotOpen()
    {
        var refused = TogetherState.Alone with { Problem = "relay: 401", PasswordRefused = true };
        Assert.Null(HostKey.Code(refused));
        Assert.False(HostKey.Copyable(refused));
        Assert.Equal(T("menu.passRefused"), HostKey.Line(refused));
        Assert.Equal("relay: gone", HostKey.Line(TogetherState.Alone with { Problem = "relay: gone" }));
    }

    [Fact]
    public void ShowsNoCodeToAGuest()
    {
        Assert.Null(HostKey.Code(new TogetherState(TogetherMode.Joined, "KQ7M", [], null, false)));
        Assert.Null(HostKey.Code(TogetherState.Alone));
    }
}
