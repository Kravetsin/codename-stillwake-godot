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
    public void Branches_fork_off_the_channel_low_enough_to_be_seen_and_go_down()
    {
        var random = new Random(3);
        var channel = BoltLook.Channel(random, (0, -400), (0, 0));
        var branches = BoltLook.Branches(random, channel, 320).ToList();
        Assert.InRange(branches.Count, 3, 5);
        Assert.All(branches, b => Assert.True(b[^1].Y > b[0].Y && b[0].Y >= -320 && b[0].Y <= -50));
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
    public void The_channel_lights_the_scene_hardest_in_a_stroke_and_lets_go_by_its_tail()
    {
        Assert.Equal(1, BoltLook.ChannelLight(0.01));
        Assert.InRange(BoltLook.ChannelLight(0.12), 0.3, 0.6);
        Assert.Equal(0, BoltLook.ChannelLight(BoltLook.LightTail));
        Assert.Equal(0, BoltLook.ChannelLight(-0.1));
    }

    [Fact]
    public void The_burn_holds_about_five_seconds_then_is_gone()
    {
        Assert.Equal(1, BoltLook.Burn(0.5));
        Assert.Equal(1, BoltLook.Burn(3.9));
        Assert.InRange(BoltLook.Burn(5), 0.1, 0.9);
        Assert.Equal(0, BoltLook.Burn(BoltLook.BurnDone));
        Assert.True(BoltLook.Edge(0.01) > 0.9 && BoltLook.Edge(BoltLook.EdgeFlash) == 0);
    }

    [Fact]
    public void A_jag_runs_between_its_ends_and_the_hash_stays_in_one()
    {
        var jag = BoltLook.Jag(new Random(1), (0, 0), (40, 0), 0.3, 3);
        Assert.Equal((0.0, 0.0), jag[0]);
        Assert.Equal((40.0, 0.0), jag[^1]);
        Assert.Contains(jag, p => p.Y != 0);
        Assert.All(Enumerable.Range(0, 500), i => Assert.InRange(BoltLook.Hash(i, 12), 0, 0.99999999));
    }
}
