using Stillwake.Game.Menu;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Settings' categories (settings-menu): the same on the title and in a world, the hero's last and only in a world.</summary>
public class SettingsCategoriesTests
{
    [Fact]
    public void TheTitleListsEveryCategoryButTheHero() =>
        Assert.Equal([SettingsCategory.Sound, SettingsCategory.Graphics, SettingsCategory.Game, SettingsCategory.Controls], SettingsCategories.Of(inWorld: false));

    [Fact]
    public void AWorldAddsTheHeroLast() =>
        Assert.Equal([.. SettingsCategories.Of(inWorld: false), SettingsCategory.Hero], SettingsCategories.Of(inWorld: true));

    [Fact]
    public void EveryCategoryHasItsOwnName()
    {
        var keys = new System.Collections.Generic.HashSet<string>();
        foreach (var category in SettingsCategories.Of(inWorld: true)) Assert.True(keys.Add(SettingsCategories.Key(category)));
    }
}
