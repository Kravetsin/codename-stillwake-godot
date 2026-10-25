using Stillwake.Game.Features.Crew;
using Stillwake.Sim;
using Stillwake.Sim.Building;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>
/// A tool aimed at the ground (the fan of rays): the axe or the drill cast from the hero's shoulder toward the pointer, what it
/// would strike the sim's own query's.
/// </summary>
public class FanAimTests
{
    private const int Unit = Hero.Unit;
    private static readonly string Axe = Spec.Weapons.Axe.Item, Drill = Spec.Weapons.Drill.Item;
    private const int W = 24;

    /// <summary>A cave in soil a character a cell: open from column 1 to 22, rows 1 to 8, over stone and bedrock.</summary>
    private static Layout CaveLayout => Layout.Parse("""
        ########################
        #......................#
        #......................#
        #......................#
        #......................#
        #......................#
        #......................#
        #......................#
        #......................#
        ########################
        ########################
        ########################
        """).WithMaterials("""
        ssssssssssssssssssssssss
        s......................s
        s......................s
        s......................s
        s......................s
        s......................s
        s......................s
        s......................s
        s......................s
        ssssssssssssssssssssssss
        tttttttttttttttttttttttt
        bbbbbbbbbbbbbbbbbbbbbbbb
        """);

    private static TestWorld Cave() => new(CaveLayout);

    private static int At(int x, int y) => y * W + x;

    /// <summary>A hero standing on the cave's floor in column <paramref name="x"/>, its feet on row 9.</summary>
    private static Hero Standing(int x) => new(x * Unit + Unit / 2, 9 * Unit, 0, 0, false, 1);

    private static (double X, double Y) Centre(int x, int y) => (x + 0.5, y + 0.5);

    [Fact]
    public void TheAxeAimsItsFanAtTheWallAndStrikesWhatTheSimSaysAimedAtTheCellNearestThePointer()
    {
        var m = Cave().M;
        var hero = Standing(20);
        var aim = Aiming.AimAt(m, 0, hero, Centre(23, 6), axe: true);
        var hand = Aiming.HandOf(hero);
        var fan = aim!.Fan!;
        Assert.Equal(new Hew(Axe, hand.X, hand.Y, Aiming.HeadingOf(hand, Aiming.PointOf(Centre(23, 6)))), fan.Ground);
        Assert.Equal(m.Building.Struck(Axe, hand.X, hand.Y, fan.Ground.Heading).Cells, fan.Hewn.Cells);
        Assert.Contains(At(23, 6), fan.Hewn.Cells);
        Assert.Equal(new Aim(At(23, 6), true, new Use(fan.Ground, null), Fan: fan), aim);
        Assert.Equal(new Prompt("use.dig", null), Prompts.Of(m, aim));
        // A press never digs: the swing does.
        Assert.Empty(Aiming.UsesOf(aim, pressed: true, held: false, tick: 1, workedOnTick: -1).Commands);
        // A drill with no battery is refused flat before its run is asked.
        Assert.Equal("items.flat", Aiming.AimAt(m, 0, hero, Centre(23, 6), drill: Drill)?.Press?.Problem);
        // Bare hands aim no fan.
        Assert.Null(Aiming.AimAt(m, 0, hero, Centre(23, 6))?.Fan);
    }

    [Fact]
    public void AFanOutOfRangeOfEverythingAimsAtNothingButStillShowsItsReach()
    {
        var m = Cave().M;
        // Up from the middle of the cave: the ceiling is farther than the axe reaches.
        var aim = Aiming.AimAt(m, 0, Standing(11), Centre(11, 1), axe: true);
        Assert.NotNull(aim?.Fan);
        Assert.True(aim!.Fan!.IsEmpty);
        Assert.Null(aim.Press);
        Assert.Null(Prompts.Of(m, aim));
    }

    [Fact]
    public void AFanThroughAPlantStrikesItAndAimsAtIt()
    {
        var m = new TestWorld(CaveLayout with { Flora = [(At(13, 8), "bellTeal")] }).M;
        var plant = m.Building.PlantAt(At(13, 8));
        var aim = Aiming.AimAt(m, 0, Standing(11), Centre(13, 8), axe: true);
        Assert.Contains(plant, aim!.Fan!.Hewn.Plants);
        Assert.Equal(new Prompt("use.dig", null), Prompts.Of(m, aim));
    }

    [Fact]
    public void AStrikeIsHeardAtTheCellItStrikesNearestThePointer()
    {
        var m = Cave().M;
        var fan = Aiming.FanOf(m, Standing(20), Aiming.PointOf(Centre(23, 6)), Axe);
        foreach (int cell in fan.Hewn.Cells)
            Assert.Equal(cell, Aiming.LeadOf(fan, new Point(cell % W * Unit + 500, cell / W * Unit + 500), W));
    }

    [Fact]
    public void TheAimGlowsOnlyInWhatBreaksNearWhereTheRaysTouchItAndDeeperFades()
    {
        var m = Cave().M;
        var fan = Aiming.FanOf(m, Standing(20), Aiming.PointOf(Centre(23, 6)), Axe);
        var hits = AimGlow.HitsOf(m.Grid, m.Building, fan);
        Assert.InRange(hits.Count, 1, Spec.Weapons.Axe.Rays);
        // Every point lies in a struck cell, on its face.
        Assert.All(hits, h => Assert.Contains((int)(h.Y / 8) * W + (int)Math.Min(h.X / 8, W - 1), fan.Hewn.Cells));
        var field = AimGlow.FieldOf(m.Grid, m.Building, fan)!;
        // Bright at the wall's face, dimmer a cell in, none in the air before it.
        int y = 6 * 8 + 4;
        Assert.True(field.At(23 * 8, y) > 0.5f);
        Assert.True(field.At(23 * 8 + 6, y) < field.At(23 * 8, y) / 4);
        Assert.Equal(0, field.At(22 * 8 + 4, y));
    }

    [Fact]
    public void TheGlowSlidesAsTheAimTurnsAStepAndDoesNotJump()
    {
        var m = Cave().M;
        var hero = Standing(20);
        var hand = Aiming.HandOf(hero);
        int heading = Aiming.HeadingOf(hand, Aiming.PointOf(Centre(23, 6)));
        GlowField Field(int h) => AimGlow.FieldOf(m.Grid, m.Building, Aiming.FanOf(m, hero, hand with { X = hand.X + (int)(5000 * Math.Cos(h * Math.Tau / 256)), Y = hand.Y + (int)(5000 * Math.Sin(h * Math.Tau / 256)) }, Axe))!;
        var (a, b) = (Field(heading), Field(heading + 1));
        double moved = 0;
        for (int x = 20 * 8; x < 24 * 8; x++)
            for (int yy = 0; yy < 12 * 8; yy++) moved = Math.Max(moved, Math.Abs(a.At(x, yy) - b.At(x, yy)));
        Assert.True(moved < 0.35, $"a step of the aim changed a pixel by {moved:0.00}");
    }

    [Fact]
    public void WhatIsAimedAtBreathesFromFaintToBrightAndBack()
    {
        Assert.Equal(Stillwake.Game.Render.Breath.Least, Stillwake.Game.Render.Breath.Alpha(Stillwake.Game.Render.Breath.PeriodMs * 0.75), 6);
        Assert.Equal(Stillwake.Game.Render.Breath.Most, Stillwake.Game.Render.Breath.Alpha(Stillwake.Game.Render.Breath.PeriodMs * 0.25), 6);
        Assert.Equal(Stillwake.Game.Render.Breath.Alpha(100), Stillwake.Game.Render.Breath.Alpha(100 + Stillwake.Game.Render.Breath.PeriodMs), 6);
    }

}
