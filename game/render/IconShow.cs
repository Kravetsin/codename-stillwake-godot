namespace Stillwake.Game.Render;

/// <summary>Where a glowing line icon is drawn (0086, 0095), and so how: each as the arc icon page draws it.</summary>
public enum IconShow
{
    /// <summary>A slot's mark, 14 px: lit, on a soft dark halo so it reads over any item.</summary>
    Mark,
    /// <summary>An empty socket's or station slot's ghost: only the rim, faint, still.</summary>
    Ghost,
    /// <summary>A socket or slot what is carried would go in: lit, a little softer than lit, moving.</summary>
    Hot,
    /// <summary>On a card: glow, rim and core.</summary>
    Lit,
}
