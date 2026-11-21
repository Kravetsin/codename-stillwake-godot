namespace Stillwake.Game.Platform;

/// <summary>How much the edges of what is drawn are smoothed (2D multisampling), chosen in Settings; pictures inside are untouched.</summary>
public enum Smoothing
{
    Off,
    Two,
    Four,
    Eight,
}

/// <summary>The smoothings as the settings file and the command line name them.</summary>
public static class Smoothings
{
    /// <summary>The word a smoothing is kept as: its samples, or off.</summary>
    public static string NameOf(Smoothing smoothing) => smoothing switch
    {
        Smoothing.Two => "2",
        Smoothing.Four => "4",
        Smoothing.Eight => "8",
        _ => "off",
    };

    /// <summary>The smoothing a word names; none, or a word this build does not know, is <see cref="Smoothing.Off"/>.</summary>
    public static Smoothing Named(string? name) => name switch
    {
        "2" => Smoothing.Two,
        "4" => Smoothing.Four,
        "8" => Smoothing.Eight,
        _ => Smoothing.Off,
    };
}
