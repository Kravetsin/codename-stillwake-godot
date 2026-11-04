namespace Stillwake.Game.Platform;

/// <summary>How the game's window takes the screen, chosen in Settings.</summary>
public enum ScreenMode
{
    /// <summary>A window with its frame, sized and moved by the player.</summary>
    Windowed,
    /// <summary>The whole screen, the game alone on it (exclusive full screen).</summary>
    Fullscreen,
    /// <summary>A window without a frame covering the whole screen: another window a click or Alt+Tab away.</summary>
    Borderless,
}

/// <summary>The screen modes as the settings file names them.</summary>
public static class Screens
{
    /// <summary>The word a mode is kept as.</summary>
    public static string NameOf(ScreenMode mode) => mode switch
    {
        ScreenMode.Fullscreen => "fullscreen",
        ScreenMode.Borderless => "borderless",
        _ => "windowed",
    };

    /// <summary>The mode a word in the settings file names; none, or a word this build does not know, is <see cref="ScreenMode.Windowed"/>.</summary>
    public static ScreenMode Named(string? name) => name switch
    {
        "fullscreen" => ScreenMode.Fullscreen,
        "borderless" => ScreenMode.Borderless,
        _ => ScreenMode.Windowed,
    };
}
