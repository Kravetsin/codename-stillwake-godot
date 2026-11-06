using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The items kept in the bag when stashing, and how the settings file keeps them (2026-11-06-quick-stash).</summary>
public class FavouritesTests
{
    [Fact]
    public void ToggledMarksThenUnmarksAndKeepsTheIdsSortedOrdinally()
    {
        var f = Favourites.None.Toggled("wire").Toggled("canister").Toggled("battery");
        Assert.Equal(["battery", "canister", "wire"], f.Kept);
        Assert.True(f.Has("canister"));
        var less = f.Toggled("canister");
        Assert.Equal(["battery", "wire"], less.Kept);
        Assert.False(less.Has("canister"));
        // A value: the one toggled is untouched, and two alike are equal.
        Assert.True(f.Has("canister"));
        Assert.Equal(less, Favourites.None.Toggled("wire").Toggled("battery"));
    }

    [Fact]
    public void WrittenIsReadBackAlike()
    {
        var f = Favourites.None.Toggled("scrap").Toggled("canister");
        Assert.Equal(f, Favourites.Read(f.Written()));
        Assert.Equal(Favourites.None, Favourites.Read(Favourites.None.Written()));
    }

    [Theory]
    [InlineData("")]
    [InlineData(",,,")]
    [InlineData("noSuchItem")]
    [InlineData("  , noSuchItem ,")]
    public void ABrokenLineOrAnIdThatIsNoItemGivesNoMark(string kept) => Assert.Equal(Favourites.None, Favourites.Read(kept));

    [Fact]
    public void ReadKeepsTheItemsAmongWhatIsNotAndEachOnce() =>
        Assert.Equal(["canister", "scrap"], Favourites.Read("scrap, gone,canister,scrap").Kept);
}
