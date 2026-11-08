namespace Stillwake.Game.Render;

/// <summary>
/// Where a vein's picture shows its ore, for the shine lamp light gives it (<c>render/Lit.cs</c>): the vein is drawn over the very sheet of
/// the material it runs through, so what differs from that sheet is ore. Plain C#, tested.
/// </summary>
public static class OreMask
{
    // A pixel differing by less than From in every colour is the same stone (the finish's fringes); by Whole or more, all ore.
    private const int From = 8, Whole = 40;

    /// <summary>
    /// The mask of <paramref name="vein"/> drawn over <paramref name="host"/>, both RGBA8 of one size: <paramref name="fleck"/> where a pixel
    /// differs from the host's, scaled by how much (none below a faint fringe, whole past a clear difference), black where they match; opaque.
    /// </summary>
    public static byte[] Of(byte[] vein, byte[] host, (int R, int G, int B) fleck)
    {
        if (vein.Length != host.Length) throw new System.ArgumentException($"A vein of {vein.Length} bytes over a host of {host.Length}");
        var mask = new byte[vein.Length];
        for (int p = 0; p < vein.Length; p += 4)
        {
            int d = System.Math.Max(System.Math.Abs(vein[p] - host[p]), System.Math.Max(System.Math.Abs(vein[p + 1] - host[p + 1]), System.Math.Abs(vein[p + 2] - host[p + 2])));
            int share = System.Math.Clamp(d - From, 0, Whole - From);
            mask[p] = (byte)(fleck.R * share / (Whole - From));
            mask[p + 1] = (byte)(fleck.G * share / (Whole - From));
            mask[p + 2] = (byte)(fleck.B * share / (Whole - From));
            mask[p + 3] = 255;
        }
        return mask;
    }
}
