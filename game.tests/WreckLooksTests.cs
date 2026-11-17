using Stillwake.Content;
using Stillwake.Game.Dev;
using Stillwake.Game.Features.Light;
using Stillwake.Game.Features.Wrecked;
using Stillwake.Sim;
using Xunit;
using V = Stillwake.Content.Vessels;

namespace Stillwake.Game.Tests;

/// <summary>A wreck's looks (vessel-wrecks W-2): which event leaves which, the cells each lies on, old groups dulled, lamps by event.</summary>
public class WreckLooksTests
{
    private static Built Built(string cls, uint seed, string chain) => Shipyard.Build(V.Class(cls)!, seed, WreckChain.Parse(chain));

    private static (WreckScene Scene, List<WreckMark> Marks) Looks(Built b) => Looks(new WreckScene(b));

    private static (WreckScene Scene, List<WreckMark> Marks) Looks(WreckScene scene) => (scene, WreckLooks.Of(scene, 0xaeb6b7, _ => null));

    /// <summary>Every cell's centre each mark of <paramref name="look"/> laid paint on, painted alone, chunk by chunk.</summary>
    private static HashSet<(int X, int Y)> PaintedBy(WreckScene scene, List<WreckMark> marks, string look)
    {
        var cells = new HashSet<(int, int)>();
        var paint = new WreckPaint();
        foreach (var mark in marks.Where(m => m.Look == look))
            for (int cy = WreckPaint.FloorDiv((int)Math.Floor(mark.Y0), WreckPaint.ChunkCells); cy <= WreckPaint.FloorDiv((int)Math.Ceiling(mark.Y1), WreckPaint.ChunkCells); cy++)
                for (int cx = WreckPaint.FloorDiv((int)Math.Floor(mark.X0), WreckPaint.ChunkCells); cx <= WreckPaint.FloorDiv((int)Math.Ceiling(mark.X1), WreckPaint.ChunkCells); cx++)
                {
                    paint.Begin(cx, cy);
                    mark.Draw(paint);
                    if (!paint.Painted) continue;
                    var (x0, y0, x1, y1) = paint.Cells;
                    for (int y = y0; y < y1; y++)
                        for (int x = x0; x < x1; x++)
                            for (int k = 0; k < 4; k++)
                                if (paint.AlphaAt(x + 0.25 + 0.5 * (k % 2), y + 0.25 + 0.5 * (k / 2)) > 0)
                                {
                                    cells.Add((x, y));
                                    break;
                                }
                }
        return cells;
    }

    [Fact]
    public void EachEventLeavesItsOwnLooks()
    {
        var names = Looks(Built("liner", 7, "blast,fire,rock")).Marks.Select(m => m.Look).ToHashSet();
        foreach (var look in (string[])["soot", "streaks", "heat", "scorch", "beads", "petal", "cable", "burnRoom", "smoke", "hoar", "iceFan", "hullFrost", "icicle"])
            Assert.Contains(look, names);
        Assert.DoesNotContain("pits", names);
        var overload = Looks(Built("freighter", 3, "break")).Marks.Select(m => m.Look).ToHashSet();
        Assert.Contains("folds", overload);
        Assert.Contains("stress", overload);
        Assert.Contains("pipe", overload);
        var power = Looks(Built("liner", 7, "ice")).Marks.Select(m => m.Look).ToHashSet();
        Assert.Contains("frostGlass", power);
        Assert.Contains("hoar", power);
        Assert.DoesNotContain("iceFan", power);
        var scavenged = Looks(Built("freighter", 7, "rock:old")).Marks.Select(m => m.Look).ToHashSet();
        Assert.Contains("pits", scavenged);
        Assert.DoesNotContain("smoke", Looks(Built("liner", 7, "fire:old")).Marks.Select(m => m.Look));
    }

    [Fact]
    public void SootAndStreaksLieInTheRoomsTheAirLeftHeatAndFrostOnTheHullRimeInTheFrozenRoomsPitsOnTheSkin()
    {
        var (scene, marks) = Looks(Built("liner", 7, "blast,fire,rock"));
        foreach (var look in (string[])["soot", "streaks"])
        {
            var cells = PaintedBy(scene, marks, look);
            Assert.NotEmpty(cells);
            Assert.All(cells, c => Assert.True(scene.Vented(c.X, c.Y), $"{look} at {c}"));
        }
        foreach (var look in (string[])["heat", "scorch", "hullFrost"])
        {
            var cells = PaintedBy(scene, marks, look);
            Assert.NotEmpty(cells);
            Assert.All(cells, c => Assert.True(scene.Walled(c.X, c.Y), $"{look} at {c}"));
        }
        var frozen = scene.Wreck.Events[2].Compartments.ToHashSet();
        var rime = PaintedBy(scene, marks, "hoar");
        Assert.NotEmpty(rime);
        Assert.All(rime, c => Assert.True(scene.Open(c.X, c.Y) && frozen.Contains(scene.RoomAt(c.X, c.Y)), $"hoar at {c}"));
        var burnt = scene.Wreck.Events[1].Compartments.ToHashSet();
        Assert.All(PaintedBy(scene, marks, "burnRoom"), c => Assert.True(scene.Open(c.X, c.Y), $"burn at {c}"));
        Assert.Contains(PaintedBy(scene, marks, "burnRoom"), c => burnt.Contains(scene.RoomAt(c.X, c.Y)));
        var (old, oldMarks) = Looks(Built("freighter", 7, "rock:old"));
        var pits = PaintedBy(old, oldMarks, "pits");
        Assert.NotEmpty(pits);
        Assert.All(pits, c => Assert.True(old.Skin(c.X, c.Y), $"pit at {c}"));
    }

    /// <summary>The most alpha <paramref name="look"/>'s first mark lays anywhere.</summary>
    private static float Most(Built b, string look)
    {
        var (_, marks) = Looks(b);
        var mark = marks.First(m => m.Look == look);
        var paint = new WreckPaint();
        float most = 0;
        for (int cy = WreckPaint.FloorDiv((int)Math.Floor(mark.Y0), WreckPaint.ChunkCells); cy <= WreckPaint.FloorDiv((int)Math.Ceiling(mark.Y1), WreckPaint.ChunkCells); cy++)
            for (int cx = WreckPaint.FloorDiv((int)Math.Floor(mark.X0), WreckPaint.ChunkCells); cx <= WreckPaint.FloorDiv((int)Math.Ceiling(mark.X1), WreckPaint.ChunkCells); cx++)
            {
                paint.Begin(cx, cy);
                mark.Draw(paint);
                var (x0, y0, x1, y1) = paint.Cells;
                for (double y = y0; y < y1; y += 0.25)
                    for (double x = x0; x < x1; x += 0.25) most = Math.Max(most, paint.AlphaAt(x, y));
            }
        return most;
    }

    [Fact]
    public void AnOldGroupsSootAndHeatAreDulledAndItsFrostPaler()
    {
        var fresh = Built("liner", 7, "blast");
        var old = Built("liner", 7, "blast:old");
        // Where only soot's outer step lies, whole: 0.27 fresh, dulled old.
        var (fs, fm) = Looks(fresh);
        var (os, om) = Looks(old);
        var (f1, o1) = (fm.First(m => m.Look == "soot"), om.First(m => m.Look == "soot"));
        var (pf, po) = (new WreckPaint(), new WreckPaint());
        int compared = 0;
        for (int cy = WreckPaint.FloorDiv((int)f1.Y0, WreckPaint.ChunkCells); cy <= WreckPaint.FloorDiv((int)f1.Y1, WreckPaint.ChunkCells); cy++)
            for (int cx = WreckPaint.FloorDiv((int)f1.X0, WreckPaint.ChunkCells); cx <= WreckPaint.FloorDiv((int)f1.X1, WreckPaint.ChunkCells); cx++)
            {
                pf.Begin(cx, cy);
                po.Begin(cx, cy);
                f1.Draw(pf);
                o1.Draw(po);
                var (x0, y0, x1, y1) = pf.Cells;
                for (double y = y0 + 0.5; y < y1; y++)
                    for (double x = x0 + 0.5; x < x1; x++)
                    {
                        float a = pf.AlphaAt(x, y);
                        if (Math.Abs(a - 0.27f) > 0.005f) continue;
                        Assert.InRange(po.AlphaAt(x, y) / a, WreckLooks.Dull - 0.02, WreckLooks.Dull + 0.02);
                        compared++;
                    }
            }
        Assert.True(compared > 10, $"{compared} cells compared");
        Assert.True(Most(old, "heat") < Most(fresh, "heat"));
        _ = (fs, os);
        Assert.True(Most(Built("liner", 7, "rock:old"), "iceFan") < Most(Built("liner", 7, "rock"), "iceFan"));
        // An old first group was scavenged after: its hull pitted.
        Assert.Contains(Looks(old).Marks, m => m.Look == "pits");
    }

    [Fact]
    public void AChunkPaintsTheSameEachTimeAndACutWallTakesItsHeatWithIt()
    {
        var (scene, marks) = Looks(Built("liner", 7, "blast"));
        var e = scene.Wreck.Events[0];
        var heat = PaintedBy(scene, marks, "heat");
        var (x, y) = heat.OrderBy(c => c.Y).ThenBy(c => c.X).First();
        var (cx, cy) = WreckPaint.ChunkOf(x, y);
        byte[] Paint()
        {
            var paint = new WreckPaint();
            paint.Begin(cx, cy);
            foreach (var m in marks) m.Draw(paint);
            return paint.Bytes();
        }
        Assert.Equal(Paint(), Paint());
        scene.Set(x, y, WreckScene.Space);
        Assert.DoesNotContain((x, y), PaintedBy(scene, marks, "heat"));
        _ = e;
    }

    [Fact]
    public void ABurntMachinesCharGoesWithTheMachine()
    {
        var b = Built("liner", 7, "fire");
        var scene = new WreckScene(b);
        var picture = new WreckPicture(4, 4, [.. Enumerable.Repeat((byte)255, 64)]);
        var marks = WreckLooks.Of(scene, 0xaeb6b7, _ => (picture, 0, 0));
        Assert.Contains(marks, m => m.Look == "charred");
        var burnt = PaintedBy(scene, marks, "charred");
        Assert.NotEmpty(burnt);
        foreach (var m in scene.Machines) m.Stands = false;
        Assert.Empty(PaintedBy(scene, marks, "charred"));
        Assert.Empty(PaintedBy(scene, marks, "sag"));
    }

    private static LampState[] Lamps(Built b) => [.. new VesselLights(b).Lamps.Select(l => l.State)];

    [Fact]
    public void AWrecksLampsAllGoDarkWhenItsLastGroupIsOldOrItsPowerWasLostFirst()
    {
        // A fire then a strike long after: the last group old.
        Assert.All(Lamps(Built("liner", 7, "fire,rock:old")), s => Assert.Equal(LampState.Dead, s));
        Assert.All(Lamps(Built("liner", 7, "ice,fire")), s => Assert.Equal(LampState.Dead, s));
        Assert.Contains(Lamps(Built("liner", 7, "fire")), s => s is LampState.Red or LampState.Dying);
    }

    [Fact]
    public void ALampInARoomAFireBurntIsDarkAndOneTheAirLeftDarkOrStuttering()
    {
        int checkedLamps = 0;
        foreach (var chain in (string[])["fire", "blast", "rock", "break"])
        {
            var b = Built("liner", 7, chain);
            var lights = new VesselLights(b);
            var burnt = b.Wreck!.Events.Where(e => e.Kind == WreckKind.Fire).SelectMany(e => e.Compartments).ToHashSet();
            foreach (var lamp in lights.Lamps)
            {
                int room = b.Vessel.Compartments.ToList().FindIndex(c => c.Holds(lamp.X + 1, lamp.Y));
                if (room < 0) continue;
                if (chain == "break") continue;
                if (burnt.Contains(room)) Assert.Equal(LampState.Dead, lamp.State);
                else if (b.Wreck.Vented.Contains(room)) Assert.True(lamp.State is LampState.Dead or LampState.Dying, $"{chain}: {lamp.State}");
                else continue;
                checkedLamps++;
            }
        }
        Assert.True(checkedLamps > 5, $"{checkedLamps} lamps");
    }

    [Fact]
    public void AfterABreakTheSternBlinksRedAndTheBowIsDarkButOneSealedRoomStillLit()
    {
        int warm = 0;
        for (uint seed = 1; seed <= 6; seed++)
        {
            var b = Built("liner", seed, "break");
            var cut = b.Wreck!.Events[0].Break!;
            var lamps = new VesselLights(b).Lamps;
            // The stern's blink, but the sealed room still lit if it lies astern.
            Assert.All(lamps.Where(l => l.X < cut.Column), l => Assert.True(l.State is LampState.Blink or LampState.Warm, $"{l.State}"));
            var lit = lamps.Where(l => l.State == LampState.Warm).ToList();
            Assert.All(lamps.Where(l => l.X >= cut.Column && l.State != LampState.Warm), l => Assert.Equal(LampState.Dead, l.State));
            var rooms = lit.Select(l => b.Vessel.Compartments.ToList().FindIndex(c => c.Holds(l.X + 1, l.Y))).Select(i => b.Vessel.Compartments[i].Of >= 0 ? b.Vessel.Compartments[i].Of : i).Distinct().ToList();
            Assert.True(rooms.Count <= 1, $"seed {seed}: {rooms.Count} rooms lit");
            Assert.All(rooms, r => Assert.DoesNotContain(r, b.Wreck.Vented));
            warm += rooms.Count;
        }
        Assert.True(warm > 0);
        Assert.True(VesselLights.Emergency(LampState.Blink));
        Assert.Equal(1f, VesselLights.Shines(LampState.Blink, 0, 0));
        Assert.Equal(0f, VesselLights.Shines(Stillwake.Game.Features.Light.LampState.Blink, VesselLooks.BlinkMs * 0.75, 0));
    }

    [Fact]
    public void AScavengedHullsLiveryIsFaded()
    {
        var livery = VesselLooks.Liveries[0];
        var faded = VesselLooks.Faded(livery);
        Assert.NotEqual(livery, faded);
        // Toward grey: each channel of the hull no farther from its grey than it was.
        int Spread(uint c) => Math.Max(Math.Max((int)(c >> 16) & 255, (int)(c >> 8) & 255), (int)c & 255) - Math.Min(Math.Min((int)(c >> 16) & 255, (int)(c >> 8) & 255), (int)c & 255);
        Assert.True(Spread(faded.Stripe) <= Spread(livery.Stripe));
    }
}
