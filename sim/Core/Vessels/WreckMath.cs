namespace Stillwake.Sim;

/// <summary>Whole-number help for a wreck's geometry (vessel-wrecks).</summary>
internal static class WreckMath
{
    /// <summary><paramref name="a"/> over <paramref name="b"/> (b positive), rounded down, below zero too.</summary>
    public static int FloorDiv(int a, int b) => a >= 0 ? a / b : -((-a + b - 1) / b);
}
