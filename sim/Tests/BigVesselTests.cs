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

    /// <summary>Everything <paramref name="cls"/> makes over seeds 1 to <paramref name="seeds"/>, described and stamped, as one hash.</summary>
    private static ulong Fingerprint(string cls, uint seeds = 64)
    {
        var c = Content.Vessels.Class(cls)!;
        var text = new System.Text.StringBuilder();
        for (uint seed = 1; seed <= seeds; seed++)
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

    [Fact]
    public void TheLinerIsMadeAsBeforeTheTanker()
    {
        ulong made = Fingerprint("liner", 16);
        output.WriteLine($"liner: 0x{made:X16}UL");
        Assert.Equal(0x0219098ECBC5BF56UL, made);
    }

    private static readonly VesselClass Liner = Content.Vessels.Class("liner")!;

    private static Vessel Of(uint seed) => Shipyard.Describe(Liner, seed);

    /// <summary>The room holding cell (<paramref name="x"/>, <paramref name="y"/>); null for none.</summary>
    private static Vessel.Compartment? At(Vessel v, int x, int y) => v.Compartments.FirstOrDefault(c => c.Holds(x, y));

    [Fact]
    public void FiveHundredAndTwelveLinersAreDescribedAndStampedWhole()
    {
        for (uint seed = 1; seed <= 512; seed++)
        {
            var v = Of(seed);
            Assert.NotEmpty(Shipyard.Stamp(v));
            Assert.InRange(v.Decks.Count, Liner.Decks.Min, Liner.Decks.Max);
        }
    }

    [Fact]
    public void ADoorStandsOnlyWhereBothFloorsMeetAndTheGapsOtherDecksAreHull()
    {
        int faced = 0;
        for (uint seed = 1; seed <= 24; seed++)
        {
            var v = Of(seed);
            var parts = Shipyard.Stamp(v);
            var walls = parts.Where(p => p.Kind == "wall").Select(p => (p.X, p.Y)).ToHashSet();
            var doors = v.Doors.Skip(1).Select(d => (d.X, d.Y)).ToHashSet();
            var rooms = v.Compartments.Where(c => c.Deck >= 0).ToList();
            foreach (var p in rooms)
                foreach (var q in rooms.Where(q => q.Left == p.Left + p.Width + 2))
                    for (int d = Math.Max(p.Deck, q.Deck); d < Math.Min(p.Deck + p.Decks, q.Deck + q.Decks); d++)
                    {
                        int gap = p.Left + p.Width, top = v.Decks[d].Top;
                        bool meet = d == p.Deck + p.Decks - 1 && d == q.Deck + q.Decks - 1;
                        Assert.Equal(meet, doors.Remove((gap, top)));
                        if (meet) continue;
                        faced++;
                        for (int y = top; y < top + Content.Vessels.Open; y++)
                            for (int x = gap; x < gap + 2; x++) Assert.Contains((x, y), walls);
                    }
            // Every inner door stands between two such rooms.
            Assert.Empty(doors);
        }
        Assert.True(faced > 0, "no tall room faced hull");
    }

    [Fact]
    public void TheWellRunsDownTheServiceBlocksLeftAndARepairHatchReachesTheRoomsTallRoomsCutOff()
    {
        for (uint seed = 1; seed <= 24; seed++)
        {
            var v = Of(seed);
            var aft = v.Compartments.Single(c => c.Deck == 0 && c.Left == v.Doors[0].X + 2);
            int well = aft.Left + aft.Width + 2;
            for (int d = 0; d + 1 < v.Decks.Count; d++)
            {
                var (upper, lower) = (At(v, well, v.Decks[d].Top)!, At(v, well, v.Decks[d + 1].Top)!);
                bool differ = upper != lower && upper.Floor + 1 == lower.Top;
                Assert.Equal(differ, v.Hatches.Contains(new(well, v.Decks[d].Floor)));
            }
            // A room no door nor the well reaches, a tall room beside it on its deck: a hatch from a room over it, two columns in from
            // the right end of the columns they share.
            Assert.Contains(v.Hatches, h => At(v, h.X, h.Y - 1) is { } over && At(v, h.X, h.Y + 1) is { } under
                && h.X == Math.Min(over.Left + over.Width, under.Left + under.Width) - 2 && h.X != well);
        }
    }

    [Fact]
    public void ALadderRunsDownATallRoomInWholePiecesToARowOverItsFloor()
    {
        for (uint seed = 1; seed <= 24; seed++)
        {
            var v = Of(seed);
            var parts = Shipyard.Stamp(v);
            var tall = v.Shafts.Select(s => (Shaft: s, Room: At(v, s.Column, s.Top))).Where(t => t.Room is { Decks: > 1 }).Select(t => (t.Shaft, Room: t.Room!)).ToList();
            Assert.NotEmpty(tall);
            foreach (var (shaft, room) in tall)
            {
                Assert.Equal((room.Top, room.Floor - 2), (shaft.Top, shaft.Bottom));
                Assert.Equal(room.Height / 2, parts.Count(p => p.Kind == "ladder" && p.X == shaft.Column && p.Y >= room.Top && p.Y < room.Floor));
            }
        }
    }

    [Fact]
    public void EachRoomOfALinerIsFurnishedByItsRoleAndEveryHangarHoldsAShuttle()
    {
        var models = new HashSet<string>();
        for (uint seed = 1; seed <= 40; seed++)
        {
            var v = Of(seed);
            foreach (var role in (VesselRole[])[VesselRole.Pool, VesselRole.Restaurant, VesselRole.Lounge, VesselRole.Galley, VesselRole.Infirmary,
                         VesselRole.Hold, VesselRole.FuelBay, VesselRole.Hangar])
                Assert.Contains(v.Compartments, c => c.Role == role);
            Assert.Single(v.Compartments, c => c.Role == VesselRole.Hangar);
            foreach (var c in v.Compartments)
            {
                var inside = v.Fittings.Where(f => c.Holds(f.X, f.Y)).ToList();
                int Count(string kind) => inside.Count(f => Content.Vessels.FirstLook(f.Kind) == kind);
                switch (c.Role)
                {
                    case VesselRole.Pool:
                        Assert.Equal((2, 1, 1), (c.Decks, Count(Content.Vessels.Pool), Count(Content.Vessels.Diving)));
                        break;
                    case VesselRole.Restaurant:
                        Assert.Equal((2, 1, 1, 2), (c.Decks, Count(Content.Vessels.Bar), Count(Content.Vessels.Piano), Count(Content.Vessels.Chandelier)));
                        Assert.True(Count(Content.Vessels.RoundTable) > 0 && Count(Content.Vessels.Chair) == 2 * Count(Content.Vessels.RoundTable));
                        break;
                    case VesselRole.Galley:
                        Assert.Equal((1, 2, 2), (Count(Content.Vessels.Stove), Count(Content.Vessels.Counter), Count("salvageVending")));
                        break;
                    case VesselRole.Infirmary:
                        Assert.Equal((2, 1), (Count(Content.Vessels.Bed), Count(Content.Vessels.MedCab)));
                        break;
                    case VesselRole.Lounge:
                        Assert.True(Count(Content.Vessels.Lounger) > 0, $"a lounge on deck {c.Deck} with no lounger");
                        break;
                    case VesselRole.Hold when c.Decks > 1:
                        Assert.True(Count(Content.Vessels.Cargo) + Count("salvageCrate") > 0);
                        break;
                    case VesselRole.FuelBay:
                        // Its look fuel, the capsule's first.
                        Assert.Single(inside, f => f.Kind == Content.Vessels.Capsule);
                        break;
                    case VesselRole.Hangar:
                        var shuttle = Assert.Single(inside, f => Content.Vessels.Shuttles.Contains(f.Kind));
                        models.Add(shuttle.Kind);
                        Assert.Equal(c.Floor, shuttle.Y + Machines.Get(shuttle.Kind).Height);
                        Assert.Equal(1, Count(Content.Vessels.FuelCart));
                        break;
                }
            }
            Assert.False(v.Doors[0].Open, "the way in stands open");
        }
        Assert.Equal(Content.Vessels.Shuttles.Order(), models.Order());
    }

    [Fact]
    public void ALinerWithItsRepairHatchGoneIsUnreached()
    {
        var v = Of(5);
        Assert.Null(VesselTests.Unreached(v, Shipyard.Stamp(v)));
        int last = v.Hatches.Count - 1;
        var cut = v with { Hatches = [.. v.Hatches.Take(last)], Shafts = [.. v.Shafts.Take(last)] };
        Assert.NotNull(VesselTests.Unreached(cut, Shipyard.Stamp(cut)));
    }
}
