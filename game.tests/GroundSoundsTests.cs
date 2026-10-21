using System.Linq;
using Stillwake.Game.Features.Sound;
using Stillwake.Sim;
using Stillwake.Sim.Building;
using Xunit;
using Footing = Stillwake.Game.Sound.Footing;
using S = Stillwake.Game.Sound;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>A planet's ground heard (0093): the boots by what they stand on, a hit by what it lands on, a break or a fall by what it was.</summary>
public class GroundSoundsTests
{
    private const int Unit = 1000;
    private static readonly string Axe = Spec.Weapons.Axe.Item, Drill = Spec.Weapons.Drill.Item;

    /// <summary>
    /// Row 1 left to right: soil under the sky, a gap, soil, stone, stone with moss, two floors, plaster and alloy indoors,
    /// soil indoors, a scrap hull wall; open sky over the left half, a room's back wall over the right.
    /// </summary>
    private static readonly Layout Ground = Layout.Parse("""
                ........
        ## #####==######
        ################
        """).WithMaterials("""
        ................
        ss stttt  ppaas.
        ssssssssssssssss
        """) with { Flora = [(1 * 16 + 6, "moss")] };

    private static Registry M => new TestWorld(Ground).M;

    private static Footing? At(Registry m, int x, int y) => GroundSounds.FootingOf(m.Grid, m.Building.Plants, x, y);

    /// <summary>Feet in the middle of column <paramref name="column"/>, on top of row 1.</summary>
    private static Footing? On(Registry m, int column) => At(m, column * Unit + Unit / 2, Unit);

    [Fact]
    public void TheBootsStandOnGrassUnderTheSkySoilIndoorsStoneMossAlloyAndAnythingBuiltIsTheDeck()
    {
        var m = M;
        Assert.Equal(Footing.Grass, On(m, 0));
        Assert.Equal(Footing.Stone, On(m, 4));
        Assert.Equal(Footing.Moss, On(m, 6));
        Assert.Equal(Footing.Deck, On(m, 8));
        // The homes' plaster is stone underfoot.
        Assert.Equal(Footing.Stone, On(m, 10));
        Assert.Equal(Footing.Alloy, On(m, 12));
        Assert.Equal(Footing.Soil, On(m, 14));
        Assert.Equal(Footing.Deck, On(m, 15));
    }

    [Fact]
    public void OverAGapTheBootsStandOnWhatAnEdgeOfTheFeetIsOnAndPushingOffOnWhatIsJustUnder()
    {
        var m = M;
        // The middle over the gap in column 2, the left foot on the grass of column 1.
        Assert.Equal(Footing.Grass, At(m, 2 * Unit + 200, Unit));
        // A jump a third of a cell up already still pushes off the stone under it.
        Assert.Equal(Footing.Stone, At(m, 4 * Unit + 500, Unit - 300));
        // High in the air, nothing: the caller keeps what it last stood on.
        Assert.Null(At(m, 4 * Unit + 500, -1));
        Assert.Null(At(new TestWorld(Layout.Parse("""
            ....
            ....
            ....
            ####
            """)).M, 1500, 1000));
    }

    [Fact]
    public void EachFootingHasItsStepAndAJumpPushesOffWithIt()
    {
        Assert.Same(S.Step, GroundSounds.StepOf(Footing.Deck));
        Assert.Same(S.StepGrass, GroundSounds.StepOf(Footing.Grass));
        Assert.Same(S.StepSoil, GroundSounds.StepOf(Footing.Soil));
        Assert.Same(S.StepStone, GroundSounds.StepOf(Footing.Stone));
        Assert.Same(S.StepAlloy, GroundSounds.StepOf(Footing.Alloy));
        // Moss is the grass step, quieter.
        Assert.Equal(S.StepGrass.Files, GroundSounds.StepOf(Footing.Moss).Files);
        Assert.True(GroundSounds.StepOf(Footing.Moss).VolumeDb < S.StepGrass.VolumeDb);
        Assert.Same(S.Jump, GroundSounds.JumpOf(Footing.Deck));
        Assert.Same(S.StepStone, GroundSounds.JumpOf(Footing.Stone));
        Assert.Same(S.StepGrass, GroundSounds.JumpOf(Footing.Grass));
    }

    [Fact]
    public void ALandingOnTheGroundIsSoftOrHeavyAsItsPoseAndLouderAndLowerTheHarderTheFall()
    {
        int heavy = HeroMotion.Landing.Heavy;
        Assert.Same(S.LandSoft, GroundSounds.LandOf(Footing.Grass, heavy - 1).Cue);
        Assert.Same(S.LandHard, GroundSounds.LandOf(Footing.Grass, heavy).Cue);
        Assert.Same(S.LandHard, GroundSounds.LandOf(Footing.Stone, heavy).Cue);
        Assert.Equal((S.LandSoft, S.LandSoftestDb, 1f), GroundSounds.LandOf(Footing.Soil, S.LandSoftestFall));
        Assert.Equal((S.LandHard, 0f, S.LandHardestTune), GroundSounds.LandOf(Footing.Moss, S.LandHardestFall * 2));
        var (_, louder, lower) = GroundSounds.LandOf(Footing.Stone, (S.LandSoftestFall + S.LandHardestFall) / 2);
        Assert.InRange(louder, S.LandSoftestDb + 0.1f, -0.1f);
        Assert.InRange(lower, S.LandHardestTune + 0.01f, 0.99f);
    }

    [Fact]
    public void OnTheDeckAndOnAlloyALandingIsTheirStepLouderAndLowerWhateverTheFall()
    {
        Assert.Equal((S.Land, 0f, 1f), GroundSounds.LandOf(Footing.Deck, 100));
        Assert.Equal((S.Land, 0f, 1f), GroundSounds.LandOf(Footing.Deck, 1000));
        Assert.Equal((S.LandAlloy, 0f, 1f), GroundSounds.LandOf(Footing.Alloy, 1000));
        Assert.Equal(S.StepAlloy.Files, S.LandAlloy.Files);
        Assert.True(S.LandAlloy.VolumeDb > S.StepAlloy.VolumeDb && S.LandAlloy.Tune < 1);
    }

    [Fact]
    public void AWallIsHitAndBreaksByItsMaterialAndWhatNeverBreaksIsNeverHeard()
    {
        static (S.Cue? Hit, S.Cue? Break) Of(string id) =>
            (GroundSounds.HitOf(Spec.Materials.Get(id)), GroundSounds.BreakOf(Spec.Materials.Get(id)));
        Assert.Equal((S.HitSoil, S.BreakSoil), Of("soil"));
        Assert.Equal((S.HitStone, S.BreakStone), Of("stone"));
        Assert.Equal((S.HitStone, S.BreakStone), Of("plaster"));
        Assert.Equal((S.Chop, S.Break), Of("alloy"));
        Assert.Equal((null, null), Of("bedrock"));
        Assert.Equal((null, null), Of(Spec.Lava.Material));
        // Every natural material a weapon breaks sounds.
        foreach (var material in Spec.Materials.All.Where(m => m.Breaks))
            Assert.NotNull(GroundSounds.BreakOf(material));
    }

    [Fact]
    public void ADrillsHitsAreQuieterThanTheAxesEachAtItsOwnVolume()
    {
        // Every hit is heard, the drill's 10 a second too: a run of them, overlapping, about as loud as the axe's one by one.
        Assert.Equal(S.DrillHitDb, GroundSounds.HitDbOf(Drill));
        Assert.Equal(S.DrillHitDb, GroundSounds.HitDbOf(Spec.Weapons.ArcDrill.Item));
        Assert.Equal(0, GroundSounds.HitDbOf(Axe));
    }

    [Fact]
    public void AGrowthFallsAsWhatItIs()
    {
        Assert.Same(S.TreeFall, GroundSounds.FallOf("acacia"));
        Assert.Same(S.Bush, GroundSounds.FallOf("bush"));
        Assert.Same(S.BreakStone, GroundSounds.FallOf("rock"));
        Assert.Same(S.BreakStone, GroundSounds.FallOf("stalagmite"));
        Assert.Same(S.Grass, GroundSounds.FallOf("tuft"));
        Assert.Same(S.Grass, GroundSounds.FallOf("bellAmber"));
        Assert.Same(S.Crystal, GroundSounds.FallOf("crystalIceHang"));
        Assert.Same(S.Crystal, GroundSounds.FallOf("geode"));
        Assert.Same(S.Glowcap, GroundSounds.FallOf("hangcap"));
        Assert.Same(S.BushSoft, GroundSounds.FallOf("roots"));
        // A ruin's piece by its material; the spire's cache, cradle and lens as metal and glass.
        Assert.Same(S.WoodBreak, GroundSounds.FallOf("chair"));
        Assert.Same(S.ClothTear, GroundSounds.FallOf("tapestry"));
        Assert.Same(S.GlassBreak, GroundSounds.FallOf("ceilLamp"));
        Assert.Same(S.MetalBreak, GroundSounds.FallOf("kettle"));
        Assert.Same(S.CeramicBreak, GroundSounds.FallOf("dishes"));
        Assert.Same(S.MetalBreak, GroundSounds.FallOf(Spec.Ruins.SpireCache));
        Assert.Same(S.MetalBreak, GroundSounds.FallOf(Spec.Ruins.Cradle));
        Assert.Same(S.GlassBreak, GroundSounds.FallOf(Spec.Ruins.SpireLamp));
        // What lies in the rock goes with its stone, whose break speaks.
        Assert.Null(GroundSounds.FallOf("moss"));
        Assert.Null(GroundSounds.FallOf("veinCopper"));
    }

    [Fact]
    public void EveryGrowthHasItsSoundOrIsListedAsUnheardNeverBoth()
    {
        foreach (var growth in Spec.Flora.Every)
            Assert.True(S.Growths.ContainsKey(growth.Id) ^ S.Unheard.Contains(growth.Id), $"{growth.Id} is in neither list or in both");
        foreach (string kind in S.Growths.Keys.Concat(S.Unheard))
            Assert.NotNull(Spec.Flora.Get(kind));
    }

    [Fact]
    public void AHitSoundsAsTheWallOrTheGrowthItLandsOnButNotTwiceWhereTheFallSoundsAsTheHit()
    {
        var layout = Layout.Parse("""
              ......
            ########
            """).WithMaterials("""
            ........
            ssssssss
            """) with { Flora = [(2, "bush"), (3, "tuft"), (4, "acacia")] };
        var world = new TestWorld(layout);
        var m = world.M;
        Assert.Same(S.HitSoil, GroundSounds.HitAt(m.Grid, m.Building, 8 + 1, Axe));
        // A tuft falls to any hit: its fall alone is heard.
        Assert.Null(GroundSounds.HitAt(m.Grid, m.Building, 3, Axe));
        // A tree's last hit chops as it falls.
        Assert.Same(S.ChopWood, GroundSounds.HitAt(m.Grid, m.Building, 4, Axe));
        // A bush takes two of the axe's hits: the first is heard, the second fells it and its fall says so.
        Assert.Same(S.Bush, GroundSounds.HitAt(m.Grid, m.Building, 2, Axe));
        // Any tool's hit sounds as what it hits: the drill's on soil, a tree, a bush as the axe's.
        Assert.Same(S.HitSoil, GroundSounds.HitAt(m.Grid, m.Building, 8 + 1, Drill));
        Assert.Same(S.ChopWood, GroundSounds.HitAt(m.Grid, m.Building, 4, Drill));
        Assert.Same(S.Bush, GroundSounds.HitAt(m.Grid, m.Building, 2, Drill));
        Assert.Null(GroundSounds.HitAt(m.Grid, m.Building, 3, Drill));
        world.Step(new Dig(2, Axe) { From = 0 });
        Assert.Contains(m.Building.Cracks, c => c.Cell == 2);
        Assert.Null(GroundSounds.HitAt(m.Grid, m.Building, 2, Axe));
        // The drill's smaller bite leaves it standing one hit more: that hit is heard.
        Assert.Same(S.Bush, GroundSounds.HitAt(m.Grid, m.Building, 2, Drill));
        // Nothing there, nothing heard.
        Assert.Null(GroundSounds.HitAt(m.Grid, m.Building, 6, Axe));
    }
}
