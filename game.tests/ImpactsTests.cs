using System;
using System.Linq;
using Stillwake.Game.Features.Effects;
using Stillwake.Game.Render;
using Stillwake.Sim;
using Xunit;
using S = Stillwake.Game.Sound;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>A hit seen (0094): what it strikes decides how it looks, it lands on the face turned to the hero, a fast tool throws a share.</summary>
public class ImpactsTests
{
    private static readonly string Axe = Spec.Weapons.Axe.Item, Drill = Spec.Weapons.Drill.Item;
    /// <summary>A hit of the drill, plain and with the arc module fitted (0098).</summary>
    private static readonly int Bite = Spec.Weapons.Drill.Damage, ArcBite = Sim.Items.Boost.Gain(Bite, Spec.Modules.Arc.Adds(Spec.Weapons.Drill.Item, Spec.Stat.Damage));

    /// <summary>
    /// Row 1: soil, two soil walls side by side, stone, plaster, alloy, soil; row 2 soil underneath. Over row 1
    /// a tuft, a rock and a tree standing on row 2 at the right.
    /// </summary>
    private static readonly Layout Ground = Layout.Parse("""
        ............
        .##.#.#.#.#.
        ############
        """).WithMaterials("""
        ............
        .ss.t.p.a.s.
        ssssssssssss
        """);

    private static Registry M => new TestWorld(Ground).M;

    private const int W = 12;

    private static int At(int x, int y) => y * W + x;

    [Fact]
    public void AHitLooksAsTheWallItLandsOnByItsMaterial()
    {
        var m = M;
        Assert.Equal(Impact.Soil, Impacts.At(m.Grid, m.Building, At(1, 1)));
        Assert.Equal(Impact.Stone, Impacts.At(m.Grid, m.Building, At(4, 1)));
        Assert.Equal(Impact.Plaster, Impacts.At(m.Grid, m.Building, At(6, 1)));
        Assert.Equal(Impact.Alloy, Impacts.At(m.Grid, m.Building, At(8, 1)));
        Assert.Null(Impacts.At(m.Grid, m.Building, At(0, 0)));
    }

    [Fact]
    public void AHitOnAGrowthLooksAsTheGrowthAndTheRockAroundAGrowthInItLooksAsItsStone()
    {
        var layout = Layout.Parse("""
              ......
            ########
            """).WithMaterials("""
            ........
            tttttttt
            """) with { Flora = [(2, "bush"), (3, "rock"), (4, "acacia"), (8 + 6, "moss")] };
        var m = new TestWorld(layout).M;
        Assert.Equal(Impact.Leaves, Impacts.At(m.Grid, m.Building, 2));
        Assert.Equal(Impact.Stone, Impacts.At(m.Grid, m.Building, 3));
        Assert.Equal(Impact.Tree, Impacts.At(m.Grid, m.Building, 4));
        Assert.Equal(Impact.Stone, Impacts.At(m.Grid, m.Building, 8 + 6));
        Assert.Equal(Impact.Crystal, HitLooks.Growths["crystalIce"]);
        Assert.Equal(Impact.Spores, HitLooks.Growths["glowcapTeal"]);
        Assert.Equal(Impact.Cloth, HitLooks.Growths["tapestry"]);
        Assert.Equal(Impact.Glass, HitLooks.Growths["ceilLamp"]);
        Assert.Equal(Impact.Wood, HitLooks.Growths["chair"]);
        Assert.Equal(Impact.Alloy, HitLooks.Growths["kettle"]);
        Assert.Equal(Impact.Ceramic, HitLooks.Growths["dishes"]);
    }

    [Fact]
    public void EveryGrowthHasItsLookOrIsInTheRockNeverBoth()
    {
        foreach (var growth in Spec.Flora.Every)
            Assert.True(HitLooks.Growths.ContainsKey(growth.Id) ^ S.Unheard.Contains(growth.Id), $"{growth.Id} has no hit look nor is in the rock, or both");
        foreach (string kind in HitLooks.Growths.Keys) Assert.NotNull(Spec.Flora.Get(kind));
    }

    [Fact]
    public void EveryNaturalMaterialThatBreaksHasItsLookAndEveryObjectALookToThrow()
    {
        foreach (var material in Spec.Materials.All.Where(x => x.Breaks))
            Assert.True(HitLooks.Ground.ContainsKey(material.Id), $"{material.Id} breaks but has no hit look");
        foreach (Impact impact in Enum.GetValues<Impact>())
            Assert.NotEmpty(HitLooks.Of[impact].Throws);
    }

    [Fact]
    public void MetalSparksAndGlintsStoneThrowsAFewSparksSoilAndPlantsNone()
    {
        int Sparks(Impact impact) => HitLooks.Of[impact].Throws.Where(t => t.Look.Light).Sum(t => t.Count);
        Assert.True(Sparks(Impact.Wreck) >= 12 && HitLooks.Of[Impact.Wreck].Glint > 0);
        Assert.True(Sparks(Impact.Alloy) >= 10 && HitLooks.Of[Impact.Alloy].Glint > 0);
        Assert.InRange(Sparks(Impact.Stone), 1, Sparks(Impact.Alloy) / 2);
        Assert.Equal(0, HitLooks.Of[Impact.Stone].Glint);
        foreach (var soft in (Impact[])[Impact.Soil, Impact.Plaster, Impact.Wood, Impact.Tree, Impact.Leaves, Impact.Spores, Impact.Cloth, Impact.Ceramic])
            Assert.Equal(0, Sparks(soft));
        // Crystal and glass glint, coldly.
        Assert.True(HitLooks.Of[Impact.Crystal].Glint > 0 && HitLooks.Of[Impact.Glass].Glint > 0);
    }

    [Fact]
    public void AFastToolThrowsAShareOfAHitSoASecondOfItIsAsDenseAsASecondOfChopping()
    {
        Assert.Equal(1, Impacts.ShareOf(Axe));
        Assert.Equal(0.2, Impacts.ShareOf(Drill), 9);
        Assert.Equal(1, Impacts.ShareOf(null));
        double axePerSecond = (double)World.TicksPerSecond / Spec.Weapons.Axe.Pace;
        foreach (var weapon in Spec.Weapons.All)
            Assert.Equal(axePerSecond, Impacts.ShareOf(weapon.Item) * World.TicksPerSecond / weapon.Pace, 9);
    }

    [Fact]
    public void TheHitThatBreaksOrFellsAnObjectThrowsItsWholeLookWhateverTheTool()
    {
        var layout = Layout.Parse("""
              ......
            ########
            """).WithMaterials("""
            ........
            ssssssss
            """) with { Flora = [(2, "bush"), (3, "tuft")] };
        var world = new TestWorld(layout);
        var m = world.M;
        Assert.Equal(0.2, Impacts.ShareAt(m.Grid, m.Building, 8 + 1, Drill, Bite), 9);
        // A tuft falls to any hit: that hit is its only one.
        Assert.Equal(1, Impacts.ShareAt(m.Grid, m.Building, 3, Drill, Bite));
        world.Step(new Sim.Building.Dig(8 + 1, Axe) { From = 0 }, new Sim.Building.Dig(2, Axe) { From = 0 });
        // Soil of 4 cracked 2: the drill's 1 leaves it standing, its 2 with the arc fitted breaks it out.
        Assert.Equal(0.2, Impacts.ShareAt(m.Grid, m.Building, 8 + 1, Drill, Bite), 9);
        Assert.Equal(1, Impacts.ShareAt(m.Grid, m.Building, 8 + 1, Drill, ArcBite));
        Assert.Equal(0.2, Impacts.ShareAt(m.Grid, m.Building, 2, Drill, Bite), 9);
        Assert.Equal(1, Impacts.ShareAt(m.Grid, m.Building, 2, Drill, ArcBite));
    }

    [Fact]
    public void AWallIsStruckOnItsOpenFaceTowardTheHeroWhereItIsNearest()
    {
        var m = M;
        // Level with it to the left: its left face, at the hero's height.
        var side = Impacts.On(m.Grid, At(1, 1), 0.2, 1.5);
        Assert.Equal((1.0, 1.5, -1.0, 0.0), (side.X, side.Y, side.Nx, side.Ny));
        // From above and to the left: still its left face, kept off the corner.
        var high = Impacts.On(m.Grid, At(1, 1), 0.2, 0.6);
        Assert.Equal((1.0, 1 + HitLooks.FaceMargin, -1.0, 0.0), (high.X, high.Y, high.Nx, high.Ny));
        // From straight above: its top.
        var top = Impacts.On(m.Grid, At(1, 1), 1.5, -0.5);
        Assert.Equal((1.5, 1.0, 0.0, -1.0), (top.X, top.Y, top.Nx, top.Ny));
    }

    [Fact]
    public void AWallBehindAnotherIsStruckOnAFaceOpenToTheAirNeverInsideTheGround()
    {
        var m = M;
        // The second of two walls, the hero to the left: its left face is against the first, so its top, at its corner nearest.
        var hit = Impacts.On(m.Grid, At(2, 1), 0.2, 1.5);
        Assert.Equal((2 + HitLooks.FaceMargin, 1.0, 0.0, -1.0), (hit.X, hit.Y, hit.Nx, hit.Ny));
        // A wall under the ground, every face closed: the one turned to the hero.
        var buried = Impacts.On(m.Grid, At(4, 2), 4.5, 0.5);
        Assert.Equal((4.5, 2.0, 0.0, -1.0), (buried.X, buried.Y, buried.Nx, buried.Ny));
    }

    [Fact]
    public void APlantIsStruckNearItsMiddleTowardTheHero()
    {
        var layout = Layout.Parse("""
              ......
            ########
            """).WithMaterials("""
            ........
            ssssssss
            """) with { Flora = [(3, "tuft")] };
        var m = new TestWorld(layout).M;
        var hit = Impacts.On(m.Grid, 3, 0.5, 0.5);
        Assert.Equal((3.5 - HitLooks.PlantInset, 0.5, -1.0, 0.0), (hit.X, hit.Y, hit.Nx, hit.Ny));
    }

    [Fact]
    public void ChipsFallToTheFloorUnderTheFaceTheyAreKnockedFrom()
    {
        var m = M;
        // Off a top they land on it; off a side, on the ground at its foot.
        Assert.Equal(1, Impacts.FloorUnder(m.Grid, Impacts.On(m.Grid, At(1, 1), 1.5, -0.5)));
        Assert.Equal(2, Impacts.FloorUnder(m.Grid, Impacts.On(m.Grid, At(1, 1), 0.2, 1.5)));
    }

    [Fact]
    public void AWholeHitThrowsItsCountsAndAShareThrowsThatShareOnAverageOutOfTheFace()
    {
        var random = new Random(7);
        var glims = new Glims(100_000, random.NextDouble);
        var flares = new Flares();
        var burst = new HitBurst(glims, flares);
        var look = HitLooks.Of[Impact.Wreck];
        int whole = look.Throws.Sum(t => t.Count);
        burst.Throw(look, 0, 0, -1, 0, 1);
        Assert.Equal(whole, glims.Live.Count);
        Assert.Single(flares.Glints);
        // Its sparks thrown back out of a face turned to the left, none of them the other way.
        Assert.All(glims.Live.Where(g => g.Look.Light), g => Assert.True(g.Vx < 0));
        glims.Clear();
        int glintsBefore = flares.Glints.Count;
        const int Hits = 2000;
        for (int i = 0; i < Hits; i++) burst.Throw(look, 0, 0, -1, 0, 0.2);
        Assert.InRange(glims.Live.Count, whole * Hits * 0.2 * 0.95, whole * Hits * 0.2 * 1.05);
        Assert.InRange(flares.Glints.Count - glintsBefore, Hits * 0.2 * 0.85, Hits * 0.2 * 1.15);
    }
}
