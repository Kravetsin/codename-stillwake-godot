using System;
using System.Linq;
using Stillwake.Game.Features.Hazards.Logic;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The lightning's «Ступенчатый лидер» (surface-storms): a channel onto its cell, a leader in steps, three strokes, then out.</summary>
public class BoltLookTests
{
    [Fact]
    public void The_channel_runs_from_its_top_onto_the_struck_cell_straight_at_the_end()
    {
        var channel = BoltLook.Channel(new Random(7), (100, -400), (100, 0));
        Assert.Equal((100.0, -400.0), channel[0]);
        Assert.Equal((100.0, 0.0), channel[^1]);
        Assert.True(channel.Count > 30);
        Assert.Contains(channel, p => Math.Abs(p.X - 100) > 4);
        Assert.All(channel.Where(p => p.Y > -20), p => Assert.InRange(p.X, 99, 101));
    }

    [Fact]
    public void Branches_fork_off_the_upper_channel_and_go_down()
    {
        var random = new Random(3);
        var channel = BoltLook.Channel(random, (0, -400), (0, 0));
        var branches = BoltLook.Branches(random, channel).ToList();
        Assert.InRange(branches.Count, 3, 5);
        Assert.All(branches, b => Assert.True(b[^1].Y > b[0].Y && b[0].Y < -100));
    }

    [Fact]
    public void The_leader_steps_down_in_five_jumps_and_reaches_the_ground_at_the_strike()
    {
        Assert.Equal(0, BoltLook.LeaderReach(100, BoltLook.Leader));
        var reached = Enumerable.Range(0, 181).Select(ms => BoltLook.LeaderReach(100, BoltLook.Leader - ms / 1000.0)).Distinct().ToList();
        Assert.Equal([0, 20, 40, 60, 80, 100], reached);
    }

    [Fact]
    public void The_return_stroke_climbs_the_channel_in_twelve_ms()
    {
        Assert.Equal(2, BoltLook.ReturnReach(100, 0));
        Assert.Equal(50, BoltLook.ReturnReach(100, 0.0061));
        Assert.Equal(100, BoltLook.ReturnReach(100, BoltLook.Climb));
    }

    [Fact]
    public void Three_strokes_burn_with_a_dim_glow_between_then_it_fades_out()
    {
        Assert.Equal(1, BoltLook.Brightness(0.01));
        Assert.Equal(BoltLook.Afterglow, BoltLook.Brightness(0.12));
        Assert.Equal(0.9, BoltLook.Brightness(0.17));
        Assert.Equal(0.75, BoltLook.Brightness(0.29));
        Assert.InRange(BoltLook.Brightness(0.45), 0.01, BoltLook.Afterglow);
        Assert.Equal(0, BoltLook.Brightness(BoltLook.Done));
        Assert.True(BoltLook.Branched(0.05) && !BoltLook.Branched(0.17));
    }

    [Fact]
    public void The_glow_crackles_more_as_it_grows_most_on_the_struck_cell_least_at_the_zone_s_edge()
    {
        Assert.True(BoltLook.GlowRate(1, 0, false) > BoltLook.GlowRate(0, 0, false));
        Assert.True(BoltLook.GlowRate(0.5, 0, true) > BoltLook.GlowRate(0.5, 0, false));
        Assert.True(BoltLook.GlowRate(0.5, 0, false) > BoltLook.GlowRate(0.5, 1, false));
    }
}
