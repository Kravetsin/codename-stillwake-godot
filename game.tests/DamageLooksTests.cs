using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Game;
using Stillwake.Game.Features.Building;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Features.Debris;
using Stillwake.Game.Render;
using Xunit;
using static Stillwake.Game.Render.Ink.Draw;

namespace Stillwake.Game.Tests;

/// <summary>The picked damage and work looks (damage-looks) drawn as the page drew them.</summary>
public class DamageLooksTests
{
    [Fact]
    public void Randomness_is_the_pick_pages_seed_for_seed()
    {
        // Values the page's own mulberry32 and hash give, from node.
        var r = new Rng(7);
        Assert.Equal(0.011704753153026104, r.Next(), 12);
        Assert.Equal(0.06195825757458806, r.Next(), 12);
        Assert.Equal(0.7892992813140154, Hash(3, 1), 12);
        Assert.Equal(0.0839808254968375, Hash(-5, 7), 12);
    }

    private static (double Left, double Top, double Right, double Bottom) Bounds(Ink ink) =>
        (ink.Points.Min(p => p.X), ink.Points.Min(p => p.Y), ink.Points.Max(p => p.X), ink.Points.Max(p => p.Y));

    private static double Inked(Ink ink) => ink.Tones.Sum(t => t.A);

    [Fact]
    public void A_pen_line_is_drawn_along_it_as_wide_as_asked()
    {
        var ink = new Ink { Feather = 0.1f };
        ink.Pen([(0, 0), (10, 0)], 1, Ink.Tone.Of(255, 255, 255, 1));
        Assert.False(ink.Empty);
        var (left, top, right, bottom) = Bounds(ink);
        Assert.InRange(left, -0.01, 0.5);
        Assert.InRange(right, 9.5, 10.01);
        Assert.InRange(bottom - top, 0.8, 1.6);
        Assert.True(Inked(ink) > 10);
    }

    [Fact]
    public void A_segment_is_drawn_between_its_ends()
    {
        var ink = new Ink { Feather = 0.1f };
        ink.Segment((1, 1), (3, 1), 0.32, Ink.Tone.Of(255, 255, 255, 1));
        var (left, top, right, bottom) = Bounds(ink);
        Assert.Equal((1, 3), (Math.Round(left), Math.Round(right)));
        Assert.InRange(bottom - top, 0.3, 0.5);
        Assert.True(Inked(ink) > 1);
    }

    [Fact]
    public void A_wrecks_cracks_grow_out_of_each_blow_as_it_loses_hit_points()
    {
        List<Blow> blows = [new(0, 0, 0)];
        var little = new Ink();
        WreckCracks.Draw(little, blows, 0.05, 3);
        var much = new Ink();
        WreckCracks.Draw(much, blows, 0.6, 3);
        var (l, t, r, b) = Bounds(little);
        var (L, T, R, B) = Bounds(much);
        Assert.True((R - L) * (B - T) > 4 * (r - l) * (b - t), "the cracks run farther as more is lost");
        Assert.True(R - L <= 2 * (DamageLooks.DentLongest + 1));
    }

    [Fact]
    public void A_blow_beside_an_earlier_one_deepens_it_and_a_wreck_shows_its_most()
    {
        List<Blow> blows = [];
        WreckCracks.Land(blows, 0, 0, 0);
        WreckCracks.Land(blows, 1, 1, 0.1);
        Assert.Single(blows);
        for (int i = 1; i < 20; i++) WreckCracks.Land(blows, i * 4, 0, 0.1);
        Assert.Equal(DamageLooks.MostBlows, blows.Count);
    }

    [Fact]
    public void A_fault_stays_in_its_cell_and_opens_at_its_last_stage()
    {
        for (int stage = 0; stage < 3; stage++)
        {
            var ink = new Ink();
            BlockFault.Draw(ink, stage, BlockFault.SeedOf(41, false), DamageLooks.Stone, 16, 24);
            var (left, top, right, bottom) = Bounds(ink);
            Assert.True(left >= 16 && right <= 24 && top >= 24 && bottom <= 32);
            Assert.True(bottom - top > 7, "it runs across the whole cell");
        }
        Assert.NotEqual(BlockFault.SeedOf(41, false), BlockFault.SeedOf(41, true));
    }

    [Fact]
    public void Sparks_fly_out_of_where_a_tool_touches_and_burn_out()
    {
        var sparks = new ToolSparks();
        for (int frame = 0; frame < 10; frame++)
        {
            sparks.Begin(1000.0 / 60);
            sparks.Touch(1, Content.Weapons.Drill.Item, (100, 50), (-1, 0), 1000.0 / 60);
            sparks.End();
        }
        Assert.InRange(sparks.Count, 20, 30);
        var ink = new Ink();
        sparks.Draw(ink);
        Assert.True(ink.Points.Min(p => p.X) < 95, "they fly back out of the face, toward the tool");
        for (int frame = 0; frame < 40; frame++)
        {
            sparks.Begin(1000.0 / 60);
            sparks.End();
        }
        Assert.Equal(0, sparks.Count);
    }

    [Fact]
    public void The_grinders_wheel_throws_down_past_it_whichever_way_it_is_held()
    {
        Assert.True(ToolSparks.Wheel((-1, 0)).Y > 0.8);
        Assert.True(ToolSparks.Wheel((1, 0)).Y > 0.8);
        Assert.True(ToolSparks.Wheel((-1, 0)).X < 0);
    }

    [Fact]
    public void The_jet_runs_from_the_nozzle_to_what_it_cuts()
    {
        var ink = new Ink();
        CutterJet.Draw(ink, (0, 0), (40, 0), hits: true, 100);
        var (left, _, right, _) = Bounds(ink);
        Assert.InRange(left, -0.5, 0.5);
        Assert.InRange(right, 40, 43.5);
    }

    [Fact]
    public void The_trail_stays_on_the_blade_swung_straight_up()
    {
        var trail = new SwingTrail();
        // The edge swung through straight up and past it, where an angle would wrap from -pi to pi.
        double[] angles = [-1.2, -1.45, -1.7, -1.95, -2.2];
        foreach (double a in angles)
            trail.Step(1000.0 / 60, (Math.Cos(a) * 10, Math.Sin(a) * 10), (Math.Cos(a) * 18, Math.Sin(a) * 18), striking: true, downMs: 70);
        var ink = new Ink { Feather = 0.05f };
        trail.Draw(ink);
        Assert.False(ink.Empty);
        // Every point of it lies on the circle the blade swept, out past its edge, above the shoulder.
        foreach (var (x, y) in ink.Points)
        {
            Assert.InRange(Math.Sqrt(x * x + y * y), 9, 21);
            Assert.True(y < 0);
        }
    }
}
