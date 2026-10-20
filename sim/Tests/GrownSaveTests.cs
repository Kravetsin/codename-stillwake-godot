using System.Text.Json.Nodes;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Sim.Building;
using Stillwake.Sim.Crew;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Xunit;

namespace Stillwake.Sim.Tests;

/// <summary>
/// A grown surface is saved as its seed and its edits (0077): the ground grows again as the save loads, and only the cells
/// changed since are written. And its caves are outside, never rooms, so a dig costs what is round it.
/// </summary>
public class GrownSaveTests
{
    private const string Ship = """
        ......
        .S....
        ######
        """;

    /// <summary>A ship with hero 0 beamed down to a planet of <paramref name="kind"/>, creative mode on, so it builds for nothing.</summary>
    private static Scenario Down(Content.Surfaces.Kind kind)
    {
        var w = new Scenario(Ship);
        w.Do(new Enter(w.M.Crew.Spawn)).Do(new SetCreative(true)).Run();
        w.Do(new OpenSurface(kind.Id)).Run();
        return w.Do(new Transfer(1)).Run();
    }

    private static JsonNode GridOf(string save, int surface) => JsonNode.Parse(save)!["surfaces"]![surface - 1]!["slices"]!["grid"]!;

    [Fact]
    public void AFreshPlanetSavesItsSeedAndNoCellsAtAll()
    {
        var w = Down(Content.Surfaces.Planet);
        string save = w.World.Save();
        var grid = GridOf(save, 1);
        Assert.Empty(grid["edits"]!.AsArray());
        Assert.Null(grid["structure"]);
        Assert.True(save.Length < 16 * 1024, $"{save.Length} bytes");
        // Loaded, it is the same ground, cell for cell.
        var before = w.On(1).Grid;
        int[][] Layers(IGridQueries g) => [g.StructureLayer.ToArray(), g.MaterialLayer.ToArray(), g.BackLayer.ToArray(), g.BackMaterialLayer.ToArray(), g.ShapeLayer.ToArray()];
        var layers = Layers(before);
        w.Reload();
        var after = Layers(w.On(1).Grid);
        Assert.All(Enumerable.Range(0, layers.Length), l => Assert.True(layers[l].AsSpan().SequenceEqual(after[l]), $"layer {l}"));
        Assert.Equal(save, w.World.Save());
    }

    [Fact]
    public void ASaveHoldsTheCellsDugOrBuiltAndNoneChangedBack()
    {
        var w = Down(Content.Surfaces.SmallPlanet);
        var m = w.On(1);
        int spawn = m.Crew.Spawn, under = spawn + m.Grid.Width;
        w.Do(new Dig(under, "fireAxe")).Do(new Dig(under, "fireAxe")).Run();
        Assert.Null(m.Grid.WallAt(under));
        var edits = GridOf(w.World.Save(), 1)["edits"]!.AsArray().Select(n => (int)n!).ToArray();
        Assert.Equal(7, edits.Length);
        Assert.Equal(under, edits[0]);
        Assert.Equal(Structure.Empty, edits[1]);
        // The same wall built back, the hero stepped aside (0077: a wall does not finish on one): the cell is as it grew, and
        // no edit is saved for it.
        w.Do(new Enter(spawn - 3)).Run();
        w.Do(new PlacePlan(under, Placement.Wall(m.Grid.BackAt(under)!.Id))).Run();
        int plan = m.Building.Plans.Single().Id;
        for (int i = 0; i < 200 && m.Building.Plans.Count > 0; i++) w.Do(new Work(plan)).Run();
        Assert.Empty(GridOf(w.World.Save(), 1)["edits"]!.AsArray());
    }

    /// <summary>
    /// After any digs, builds and marks on a small planet, and after a load: the ground is what grows from its seed with the
    /// save's edits laid on it, the edits are exactly the cells that differ, and the rooms found round each change are those
    /// a look at the whole grid finds.
    /// </summary>
    [Property(MaxTest = 25)]
    public Property TheGroundIsItsSeedsWithItsEditsAndItsRoomsAreThoseFoundWhole()
    {
        string[] builds = ["wall:scrap", "wall:soil", "back:scrap", "door", "ladder"];
        var gen =
            from steps in (from x in Gen.Choose(1, 62) from y in Gen.Choose(0, 46) from what in Gen.Choose(0, builds.Length + 2) select (x, y, what)).ListOf()
            from reload in Gen.Choose(0, 40)
            select (steps: steps.Take(60).ToList(), reload);
        return Prop.ForAll(gen.ToArbitrary(), t =>
        {
            var w = Down(Content.Surfaces.SmallPlanet);
            for (int i = 0; i < t.steps.Count; i++)
            {
                var m = w.On(1);
                var (x, y, what) = t.steps[i];
                int cell = y * m.Grid.Width + x;
                if (what >= builds.Length)
                    w.Do(new Dig(cell, "fireAxe")).Do(new Dig(cell, "fireAxe")).Do(new Dig(cell, "fireAxe")).Do(new Dig(cell, "fireAxe"))
                        .Do(new Dig(cell, "fireAxe")).Do(new Dig(cell, "fireAxe")).Do(new Deconstruct(cell, "structure"));
                else
                {
                    var place = builds[what].Split(':') switch
                    {
                    ["wall", var material] => Placement.Wall(material),
                    ["back", var material] => Placement.BackWall(material),
                    [var kind] => new Placement("structure", kind),
                        _ => throw new InvalidOperationException(),
                    };
                    w.Do(new PlacePlan(cell, place));
                }
                w.Run();
                foreach (var p in m.Building.Plans.ToList()) for (int k = 0; k < 40; k++) w.Do(new Work(p.Id)).Run();
                if (i == t.reload) w.Reload();
                if (!Grown(w) || !RoomsWhole(w.On(1))) return false;
            }
            w.Reload();
            return Grown(w) && RoomsWhole(w.On(1));
        });
    }

    /// <summary>Whether the planet's grid is its seed's ground with its save's edits on it, and those edits the cells that differ.</summary>
    private static bool Grown(Scenario w)
    {
        var grid = w.On(1).Grid;
        var surface = w.World.Surfaces[1];
        var ground = new GridModule();
        ground.Create(Terrain.Generate(Content.Ground.Small, surface.Seed));
        int[][] Layers(GridModule g) => [g.StructureLayer.ToArray(), g.MaterialLayer.ToArray(), g.ConduitLayer.ToArray(), g.BackLayer.ToArray(), g.BackMaterialLayer.ToArray(), g.ShapeLayer.ToArray()];
        var (now, grown) = (Layers(grid), Layers(ground));
        var differ = Enumerable.Range(0, grid.Width * grid.Height).Where(c => Enumerable.Range(0, 6).Any(l => now[l][c] != grown[l][c])).ToList();
        var edits = GridOf(w.World.Save(), 1)["edits"]!.AsArray().Select(n => (int)n!).ToArray();
        var named = Enumerable.Range(0, edits.Length / 7).Select(i => edits[i * 7]).ToList();
        if (!named.SequenceEqual(differ)) return false;
        for (int i = 0; i < named.Count; i++)
            for (int l = 0; l < 6; l++)
                if (edits[i * 7 + 1 + l] != now[l][named[i]]) return false;
        return true;
    }

    /// <summary>Whether the rooms as kept round the changes are those found over the whole grid again.</summary>
    private static bool RoomsWhole(Registry m)
    {
        var kept = m.Rooms.RoomOfCells.ToArray();
        var rooms = m.Rooms.All.ToList();
        m.Rooms.Rebuild();
        return kept.SequenceEqual(m.Rooms.RoomOfCells.ToArray()) && rooms.Count == m.Rooms.All.Count
            && rooms.Zip(m.Rooms.All).All(p => p.First.Id == p.Second.Id && p.First.Volume == p.Second.Volume
                && p.First.Hull.SequenceEqual(p.Second.Hull) && p.First.Openings.SequenceEqual(p.Second.Openings));
    }

    [Fact]
    public void ACaveIsOutsideNeverARoomAndARoomBuiltUnderTheSkyIsOne()
    {
        var w = Down(Content.Surfaces.SmallPlanet);
        var m = w.On(1);
        var g = m.Grid;
        // Every open cell under the surface, with its natural back wall, is outside.
        for (int c = 0; c < g.Width * g.Height; c++)
            if (g.StructureAt(c) == Structure.Empty) Assert.Equal(Rooms.RoomsModule.Space, m.Rooms.RoomAt(c));
        Assert.Empty(m.Rooms.All);
    }
}
