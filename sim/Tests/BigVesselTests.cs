using Stillwake.Content;
using Xunit;
using Xunit.Abstractions;

namespace Stillwake.Sim.Tests;

/// <summary>The liner (big-vessels): rooms several decks tall, doors where floors meet, hatches with ladders, raised rooms, its interiors.</summary>
public class BigVesselTests(ITestOutputHelper output)
{
    /// <summary>FNV-1a over the text, 64 bits.</summary>
    private static ulong Fnv(string text)
    {
        ulong h = 14695981039346656037UL;
        foreach (char c in text)
        {
            h ^= c;
            h *= 1099511628211UL;
        }
        return h;
    }

    /// <summary>Everything <paramref name="cls"/> makes over seeds 1–64, described and stamped, as one hash.</summary>
    private static ulong Fingerprint(string cls)
    {
        var c = Content.Vessels.Class(cls)!;
        var text = new System.Text.StringBuilder();
        for (uint seed = 1; seed <= 64; seed++)
        {
            var v = Shipyard.Describe(c, seed);
            text.Append($"{v.Width},{v.Height}|");
            foreach (var s in v.Sections) text.Append(s).Append('|');
            foreach (var d in v.Decks) text.Append(d.Floor).Append('|');
            foreach (var s in v.Shafts) text.Append(s).Append('|');
            foreach (var d in v.Doors) text.Append(d).Append('|');
            foreach (var h in v.Hatches) text.Append(h).Append('|');
            foreach (var r in v.Compartments) text.Append($"{r.Deck},{r.Left},{r.Width},{r.Top},{r.Role},{r.Interior}|");
            foreach (var f in v.Fittings.Concat(v.Outside)) text.Append($"{f.Kind},{f.X},{f.Y},{string.Join("+", f.Loot)}|");
            foreach (var cause in (VesselCause[])[VesselCause.None, VesselCause.Breach])
                foreach (var p in Shipyard.Make(c, seed, cause))
                    text.Append($"{p.X},{p.Y},{p.Layer},{p.Kind},{p.Material},{p.Open},{string.Join("+", p.Holds ?? [])};");
        }
        return Fnv(text.ToString());
    }

    [Theory]
    [InlineData("shuttle", 0x649E21D68D96DC46UL)]
    [InlineData("freighter", 0xFE3B6FEDE7D90D02UL)]
    public void TheShuttleAndTheFreighterAreMadeAsBeforeTheLiner(string cls, ulong pinned)
    {
        ulong made = Fingerprint(cls);
        output.WriteLine($"{cls}: 0x{made:X16}UL");
        Assert.Equal(pinned, made);
    }
}
