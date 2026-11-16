namespace Stillwake.Sim;

/// <summary>The world's random state; saved and hashed with the world.</summary>
public sealed class RngState
{
    public uint S { get; set; }
}

/// <summary>mulberry32 (packages/sim/src/core/rng.ts): integer-only, so every peer draws alike.</summary>
public sealed class Rng(RngState state)
{
    private const ulong Span = 4294967296;

    private uint Next()
    {
        unchecked
        {
            uint t = state.S += 0x6d2b79f5;
            t = (t ^ (t >> 15)) * (t | 1);
            t ^= t + (t ^ (t >> 7)) * (t | 61);
            return t ^ (t >> 14);
        }
    }

    /// <summary>A uniform integer in [0, n), n at most 2^32; rejection keeps it unbiased.</summary>
    public uint Below(ulong n)
    {
        if (n < 1 || n > Span) throw new ArgumentOutOfRangeException(nameof(n));
        ulong limit = Span - Span % n;
        uint v = Next();
        while (v >= limit) v = Next();
        return (uint)(v % n);
    }

    /// <summary>
    /// Another stream, begun from this one's state and <paramref name="salt"/>, drawing nothing from this one: for draws made once
    /// a world that must not move every draw after them (planet-variety).
    /// </summary>
    public Rng Fork(uint salt) => new(new RngState { S = state.S ^ salt });
}
