using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Content;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>Generated vessels described, stamped and wrecked (generated-vessels): pure, so every peer makes the same one.</summary>
public class VesselTests
{
    /// <summary>A class and a seed, every class drawn.</summary>
    private static Arbitrary<(VesselClass Class, uint Seed)> Vessels() =>
        Gen.Zip(Gen.Elements(Content.Vessels.Classes.ToArray()), ArbMap.Default.GeneratorFor<uint>()).ToArbitrary();

    /// <summary>The parts as text, every value of each in order: what two makings compare by.</summary>
    private static string Key(IReadOnlyList<VesselPart> parts) =>
        string.Join(";", parts.Select(p => $"{p.X},{p.Y},{p.Layer},{p.Kind},{p.Material},{string.Join("+", p.Holds ?? [])}"));

    [Property]
    public Property TheSameClassAndSeedMakeTheSameVesselIntactAndWrecked() => Prop.ForAll(Vessels(), v =>
    {
        foreach (var cause in (VesselCause[])[VesselCause.None, VesselCause.Breach])
            Assert.Equal(Key(Shipyard.Make(v.Class, v.Seed, cause)), Key(Shipyard.Make(v.Class, v.Seed, cause)));
    });

    [Fact]
    public void SeedsMakeVesselsThatDiffer()
    {
        var freighter = Content.Vessels.Class("freighter")!;
        var made = Enumerable.Range(1, 8).Select(s => Key(Shipyard.Make(freighter, (uint)s, VesselCause.None))).Distinct().Count();
        Assert.True(made > 4, $"{made} kinds of 8");
    }

    [Property]
    public Property AVesselFitsItsClassFrameAndEveryPartItsBoxOnceASlot() => Prop.ForAll(Vessels(), v =>
    {
        var vessel = Shipyard.Describe(v.Class, v.Seed);
        var frame = v.Class.Frame;
        Assert.InRange(vessel.Width, 1, frame.Width);
        Assert.InRange(vessel.Height, 1, frame.Height);
        var taken = new HashSet<(int, int, bool)>();
        foreach (var part in Shipyard.Stamp(vessel))
            foreach (var (x, y) in Shipyard.Covers(part))
            {
                Assert.InRange(x, 0, vessel.Width - 1);
                Assert.InRange(y, 0, vessel.Height - 1);
                Assert.True(taken.Add((x, y, part.Layer == "back")), $"{part} over another at ({x}, {y})");
            }
    });

    [Property]
    public Property EveryDeckIsOpenAndJoinedAndTheFirstHasAWayIn() => Prop.ForAll(Vessels(), v =>
    {
        var vessel = Shipyard.Describe(v.Class, v.Seed);
        Assert.Null(Unreached(vessel, Shipyard.Stamp(vessel)));
    });

    [Fact]
    public void ADeckWithNoShaftToItIsUnreached()
    {
        var freighter = Content.Vessels.Class("freighter")!;
        var vessel = Shipyard.Describe(freighter, 7);
        Assert.Null(Unreached(vessel, Shipyard.Stamp(vessel)));
        var cut = vessel with { Shafts = vessel.Shafts.Skip(1).ToList() };
        Assert.NotNull(Unreached(cut, Shipyard.Stamp(cut)));
    }

    /// <summary>
    /// Why a hero could not reach every compartment of <paramref name="v"/> from its door, by construction: a deck's open rows with a
    /// wall across them, two decks or the bridge with no ladder between them, no way in on the first deck; null when it could.
    /// </summary>
    private static string? Unreached(Vessel v, IReadOnlyList<VesselPart> parts)
    {
        var walls = parts.Where(p => p.Kind == "wall").Select(p => (p.X, p.Y)).ToHashSet();
        foreach (var c in v.Compartments)
            for (int y = c.Top; y < c.Top + Content.Vessels.Open; y++)
                for (int x = c.Left; x < c.Left + c.Width; x++)
                    if (walls.Contains((x, y))) return $"deck {c.Deck} walled at ({x}, {y})";
        bool Joined(int top, int bottom) => v.Shafts.Any(s => s.Top <= top && s.Bottom >= bottom
            && parts.Count(p => p.Kind == "ladder" && p.X == s.Column && p.Y >= s.Top && p.Y <= s.Bottom) == (s.Bottom - s.Top + 1) / 2);
        for (int d = 0; d + 1 < v.Decks.Count; d++)
            if (!Joined(v.Decks[d].Top + 1, v.Decks[d + 1].Floor - 1)) return $"decks {d} and {d + 1} not joined";
        foreach (var bridge in v.Compartments.Where(c => c.Deck < 0))
            if (!Joined(bridge.Top + Content.Vessels.Open - 1, v.Decks[0].Floor - 1)) return "the bridge not joined";
        return v.Doors.Any(d => d.Y == v.Decks[0].Top) ? null : "no way in";
    }

    [Property]
    public Property ABreachTakesThingsWholeAndAddsNothing() => Prop.ForAll(Vessels(), v =>
    {
        var vessel = Shipyard.Describe(v.Class, v.Seed);
        var intact = Shipyard.Stamp(vessel);
        var wrecked = Shipyard.Damage(intact, vessel, VesselCause.Breach);
        Assert.True(wrecked.Count < intact.Count);
        Assert.All(wrecked, p => Assert.Contains(p, intact));
    });

    // Content.

    [Fact]
    public void EveryInteriorIsSixRowsOfOpenCellsFloorsAndWhitelistedMachinesItsFirstTwoColumnsOpen()
    {
        foreach (var interior in Content.Vessels.Interiors)
        {
            Assert.Equal(Content.Vessels.Open, interior.Rows.Count);
            Assert.All(interior.Rows, r => Assert.Equal(interior.Width, r.Length));
            Assert.All(interior.Rows, r => Assert.Equal("..", r[..2]));
            foreach (char c in string.Concat(interior.Rows))
                Assert.True(c is '.' or '=' || Legend.MachineOf(c) is { } kind && Content.Vessels.Machines.Contains(kind), $"{interior.Id}: '{c}'");
            Assert.All(Layout.Parse(interior.Rows).Machines(), m => Assert.Contains(m.Kind, Content.Vessels.Machines));
        }
    }

    [Fact]
    public void AVesselsMachinesKeepNoStateElsewhereAndAreNeitherTheShipsOwnNorItsDearest()
    {
        foreach (var kind in Content.Vessels.Machines)
        {
            var spec = Machines.Get(kind);
            Assert.False(spec.Fixed, kind);
            Assert.Null(spec.Store);
            Assert.Null(spec.Makes);
        }
    }

    [Fact]
    public void EveryClassFurnishesEveryCompartmentAndFitsBesideAFullZone()
    {
        int beside = (Content.World.Width - Content.World.ZoneWidth) / 2;
        foreach (var cls in Content.Vessels.Classes)
        {
            Assert.True(cls.ArmourDepth >= 2, $"{cls.Id}: a door is two cells wide");
            Assert.True(cls.Frame.Width + Content.Vessels.Gap + Content.Vessels.EdgeClear <= beside, cls.Id);
            Assert.True(cls.Frame.Height + 2 * Content.Vessels.EdgeClear <= Content.World.Height, cls.Id);
            if (cls.Bridge > 0) Assert.True(cls.Mids.Min >= 2 && cls.Bridge <= cls.MidWidth + cls.ArmourDepth, $"{cls.Id}: the bridge over the last midsection");
            for (uint seed = 1; seed <= 40; seed++)
            {
                var vessel = Shipyard.Describe(cls, seed);
                Assert.All(vessel.Compartments, c => Assert.NotNull(c.Interior));
                Assert.Contains(vessel.Compartments, c => c.Role == VesselRole.Engine);
                Assert.Equal(cls.Bridge > 0, vessel.Compartments.Any(c => c.Role == VesselRole.Bridge));
            }
        }
    }

    [Fact]
    public void NoDroneBayOfTheShipReachesAVessel() => Assert.True(Content.Vessels.Gap > Content.Logistics.Reach);

    [Fact]
    public void EachClassGivesWhatItsBandSaysTakenApartWhole()
    {
        foreach (var cls in Content.Vessels.Classes)
            for (uint seed = 1; seed <= 40; seed++)
                Assert.InRange(Yield(Shipyard.Make(cls, seed, VesselCause.None)), cls.Yield.Min, cls.Yield.Max);
    }

    /// <summary>Whole items <paramref name="parts"/> give taken apart, every kind counted alike: walls, back walls, pieces, machines, what crates hold.</summary>
    private static int Yield(IReadOnlyList<VesselPart> parts) => parts.Sum(p =>
        p.Layer == "machine" ? Machines.Get(p.Kind).Cost.Sum(c => c.Count) + (p.Holds?.Sum(h => h.Count) ?? 0)
        : p.Kind is "wall" or "backWall" ? Materials.Get(p.Material!).Cost.Sum(c => c.Count)
        : Builds.Get(p.Kind).Cost.Sum(c => c.Count));
}
