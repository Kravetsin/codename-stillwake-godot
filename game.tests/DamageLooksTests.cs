using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Game;
using Stillwake.Game.Features.Building;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Features.Debris;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
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

    private static bool Round(double x, double y) => x * x + y * y <= 20 * 20;

    [Fact]
    public void A_wrecks_crack_network_runs_on_with_the_share_lost_reads_early_and_covers_it_by_the_break()
    {
        List<Blow> blows = [new(-18, 0, 0)];
        var net = WreckCracks.Network(blows[0], Round, 3);
        // Laid out once, by its seed: the same wreck cracks the same way.
        Assert.Equal(net.Lines.Count, WreckCracks.Network(blows[0], Round, 3).Lines.Count);
        double Spread(double share)
        {
            var ink = new Ink();
            WreckCracks.Draw(ink, blows, net, share, 3);
            var (l, t, r, b) = Bounds(ink);
            return (r - l) * (b - t);
        }
        // A twentieth lost (ten axe swings on a 200-hit-point asteroid) already runs well past the dent.
        Assert.True(Spread(0.05) > 10 * Spread(0), "the cracks read within the first blows");
        Assert.True(Spread(0.5) > 2 * Spread(0.05), "and run on as more is lost");
        // By the break it has run its whole length over the picture.
        Assert.True(WreckCracks.Reach(net, DamageLooks.NetFull) >= net.Far - 1e-9);
        Assert.True(Spread(0.95) > 0.6 * 40 * 40, "it covers most of the wreck");
    }

    [Fact]
    public void A_wrecks_cracks_stay_on_its_picture_but_for_a_ragged_edge()
    {
        var net = WreckCracks.Network(new(-18, 0, 0), Round, 7);
        foreach (var line in net.Lines)
            foreach (var (x, y) in line.Points)
                Assert.True(x * x + y * y <= 24 * 24, $"({x:F1}, {y:F1}) runs off the wreck");
    }

    [Fact]
    public void A_blow_beside_an_earlier_one_deepens_it_and_every_other_gets_its_own_dent()
    {
        List<Blow> blows = [];
        WreckCracks.Land(blows, 0, 0, 0);
        WreckCracks.Land(blows, 1, 1, 0.1);
        Assert.Equal(2, Assert.Single(blows).Hits);
        for (int i = 1; i < 20; i++) WreckCracks.Land(blows, i * 4, 0, 0.1);
        Assert.Equal(20, blows.Count);
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
            sparks.Touch((1, 1), Content.Weapons.Drill.Item, (100, 50), (-1, 0), 1000.0 / 60);
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
    public void A_tool_touching_two_wrecks_throws_a_whole_stream_off_each()
    {
        var one = new ToolSparks();
        var two = new ToolSparks();
        for (int frame = 0; frame < 10; frame++)
        {
            one.Begin(1000.0 / 60);
            two.Begin(1000.0 / 60);
            one.Touch((1, 1), Content.Weapons.Drill.Item, (100, 50), (-1, 0), 1000.0 / 60);
            two.Touch((1, 1), Content.Weapons.Drill.Item, (100, 50), (-1, 0), 1000.0 / 60);
            two.Touch((1, 2), Content.Weapons.Drill.Item, (140, 50), (-1, 0), 1000.0 / 60);
            one.End();
            two.End();
        }
        Assert.Equal(2 * one.Count, two.Count);
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
}
