namespace Stillwake.Game.Render;

/// <summary>Where a tech icon is drawn (0086), and so how: each as the icon page draws it.</summary>
public enum TechShow
{
    /// <summary>A slot's mark, 14 px: lit, on a soft dark halo so it reads over any item.</summary>
    Mark,
    /// <summary>An empty socket's ghost: only the rim, faint, still.</summary>
    Ghost,
    /// <summary>A socket a carried module would go in: lit, a little softer than lit, crackling.</summary>
    Hot,
    /// <summary>On the module's card: glow, rim and core.</summary>
    Lit,
}
