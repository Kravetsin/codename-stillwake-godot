using System.Collections.Generic;

namespace Stillwake.Game.Menu;

/// <summary>A category of Settings (settings-menu), each a page of rows beside the column of them.</summary>
public enum SettingsCategory
{
    /// <summary>Master, Effects and Music.</summary>
    Sound,
    /// <summary>The screen, heat shimmer.</summary>
    Graphics,
    /// <summary>The language, the slots' size, the logs folder.</summary>
    Game,
    /// <summary>The keys.</summary>
    Controls,
    /// <summary>The suit's colour and the look: only in a world, which has a hero.</summary>
    Hero,
}

/// <summary>Which categories Settings lists, in order: the title's and a world's alike but for the hero, last so the rest stand alike.</summary>
public static class SettingsCategories
{
    private static readonly SettingsCategory[] OnTitle = [SettingsCategory.Sound, SettingsCategory.Graphics, SettingsCategory.Game, SettingsCategory.Controls];
    private static readonly SettingsCategory[] InWorld = [.. OnTitle, SettingsCategory.Hero];

    /// <param name="inWorld">Whether a world is played: the title has no hero to colour or dress.</param>
    public static IReadOnlyList<SettingsCategory> Of(bool inWorld) => inWorld ? InWorld : OnTitle;

    /// <summary>The i18n key of <paramref name="category"/>'s name.</summary>
    public static string Key(SettingsCategory category) => category switch
    {
        SettingsCategory.Sound => "menu.catSound",
        SettingsCategory.Graphics => "menu.catGraphics",
        SettingsCategory.Game => "menu.catGame",
        SettingsCategory.Controls => "menu.catControls",
        _ => "menu.catHero",
    };
}
