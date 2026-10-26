using System.Text.Json;
using Stillwake.Game.Features.Creatures;
using Stillwake.Game.Features.Crew;
using Stillwake.Sim;
using Stillwake.Sim.Creatures;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>
/// A ram as this hero's client meets it (0103): the touch of a charging creature, the client's guard, the knock it is thrown by
/// and keeps through the keys' reel, and the flinch and the wind-up crouch drawn.
/// </summary>
public class RamsTests
{
    private const int Unit = Hero.Unit, Block = HeroTests.Block;

    /// <summary>A room of open cells, its floor row 13, with a tapir standing with its feet's middle at the point given, in <paramref name="mode"/>, running <paramref name="vx"/>.</summary>
    private static Creature Tapir(int x, int y, Posture mode, int vx = 0)
    {
        var w = new TestWorld(Layout.Parse([.. Enumerable.Repeat(new string('.', 24), 13), new string('#', 24)]));
        w.Edit(root =>
        {
            var creatures = root["slices"]!["creatures"]!;
            var c = JsonSerializer.SerializeToNode(new Creature(), w.World.Json)!.AsObject();
            (c["id"], c["species"], c["x"], c["y"], c["vx"]) = (1, Spec.Creatures.Tapir.Id, x, y, vx);
            // The tapir's brain: retaliate, flee, escape, graze; its ram a lunge, wound up or charging.
            (c["drive"], c["step"], c["driveLeft"]) = mode switch
            {
                Posture.Idle => (3, 0, 5),
                Posture.Wander => (3, 1, 5),
                Posture.Flee => (1, 0, 5),
                _ => (0, 0, 5),
            };
            if (mode is Posture.WindUp or Posture.Charge)
            {
                var act = JsonSerializer.SerializeToNode(new Act(), w.World.Json)!.AsObject();
                (act["phase"], act["left"]) = (mode == Posture.Charge ? (int)ActPhase.Active : (int)ActPhase.WindUp, 5);
                c["act"] = act;
            }
            creatures["creatures"]!.AsArray().Add(c);
            creatures["nextId"] = 2;
        });
        return w.M.Creatures.Creatures[0];
    }

    private static readonly Hero Standing = new(6 * Unit, 13 * Unit, 0, 0, false, 1);

    [Fact]
    public void OnlyACreatureTheSimSaysChargesTouchesTheHeroBodyToBody()
    {
        // Its body's edge a margin short of the hero's: touched; a little more off: not.
        int edge = Standing.X + Spec.Hero.Width / 2 + Spec.Creatures.Tapir.Body.Width * Unit / 2;
        Assert.Equal(1, Rams.Contact([Tapir(edge + Rams.Margin - 1, 13 * Unit, Posture.Charge, -1400)], Standing)?.Id);
        Assert.Null(Rams.Contact([Tapir(edge + Rams.Margin + 1, 13 * Unit, Posture.Charge, -1400)], Standing));
        foreach (var mode in new[] { Posture.Idle, Posture.Wander, Posture.Flee, Posture.WindUp })
            Assert.Null(Rams.Contact([Tapir(Standing.X, 13 * Unit, mode)], Standing));
        // Jumped clear over it: the feet above its back and the margin.
        var over = Standing with { Y = 13 * Unit - Spec.Creatures.Tapir.Body.Height * Unit - Rams.Margin - 1 };
        Assert.Null(Rams.Contact([Tapir(Standing.X, 13 * Unit, Posture.Charge, 1400)], over));
    }

    [Fact]
    public void AHeroIsThrownTheWayTheCreatureChargesOffItsLadderAndHarderByABiggerRam()
    {
        var climbing = Standing with { Climbing = true, LetGo = 9 };
        var thrown = Rams.Knocked(climbing, Rams.Away(Tapir(Standing.X + 600, 13 * Unit, Posture.Charge, -1400), climbing), Spec.Creatures.Tapir.Moves[0].Damage);
        Assert.False(thrown.Climbing);
        Assert.Equal(0, thrown.LetGo);
        Assert.True(thrown.Vx < 0 && thrown.Vy < 0, $"thrown at ({thrown.Vx}, {thrown.Vy})");
        var lighter = Rams.Knocked(Standing, -1, Spec.Creatures.Loper.Moves[0].Damage);
        Assert.True(Math.Abs(lighter.Vx) < Math.Abs(thrown.Vx) && lighter.Vy > thrown.Vy);
        // Still, away from its middle.
        Assert.Equal(1, Rams.Away(Tapir(Standing.X - 600, 13 * Unit, Posture.Charge), Standing));
    }

    [Fact]
    public void TheGuardSendsNothingForItsTicksAfterItsOwnHit()
    {
        var guard = new Rams.Guard();
        Assert.True(guard.Open(100));
        guard.Sent(100);
        Assert.False(guard.Open(100 + Spec.Hero.RamGuard - 1));
        Assert.True(guard.Open(100 + Spec.Hero.RamGuard));
        // A world loaded or another surface, its clock behind: open again.
        Assert.True(guard.Open(50));
    }

    /// <summary>A room 30 characters wide, its floor row 8, a ladder three tall standing on it in column 14.</summary>
    private const string Floor = """
        ##############################
        #............................#
        #............................#
        #............................#
        #............................#
        #.............H..............#
        #.............H..............#
        #.............H..............#
        ##############################
        """;

    [Fact]
    public void ReelingTheKnockIsKeptAloftAndNoHeldKeyWalksOrTakesALadder()
    {
        var ship = HeroTests.ShipFrom(Floor);
        var hero = Rams.Knocked(new Hero(12 * Block + 1000, 8 * Block, 0, 0, false, 1), +1, Spec.Creatures.Tapir.Moves[0].Damage);
        var keys = new HeroInput(-1, -1, true, JumpHeld: true, Sprint: true, Reeling: true);
        int vx = hero.Vx;
        var h = hero;
        for (int i = 0; i < 8; i++)
        {
            h = h.Step(keys, ship);
            Assert.False(h.Climbing, "a held Up took the ladder");
            Assert.Equal(vx, h.Vx);
        }
        // Thrown past the ladder and down again, then braked on the floor.
        h = HeroTests.Until(ship, h, keys, x => x.Vy == 0 && x.Y == 8 * Block);
        Assert.True(h.X > 15 * Block, $"landed at {h.X}");
        h = HeroTests.Until(ship, h, keys, x => x.Vx == 0);
        Assert.Equal(8 * Block, h.Y);

        // Not reeling, the air's control eats the same knock in a few steps toward what the keys hold.
        var walked = hero;
        for (int i = 0; i < 8; i++) walked = walked.Step(HeroTests.Still, ship);
        Assert.True(walked.Vx < vx);
    }

    [Fact]
    public void AFlinchIsThrownInFastAndEasesOutTheBodyTiltedAwayFromTheBlow()
    {
        Assert.Equal(0, Poses.FlinchOf(-1));
        Assert.Equal(1, Poses.FlinchOf(HeroMotion.Flinch.Ms * HeroMotion.Flinch.In), 6);
        Assert.Equal(0, Poses.FlinchOf(HeroMotion.Flinch.Ms));
        var stand = Poses.LimbsOf(Pose.Stand, Standing, new Moment(0, 0));
        Assert.True(Poses.Flinched(stand, 1, ahead: 1, Pose.Stand).Tilt < stand.Tilt - 0.3, "not thrown back from a blow before it");
        Assert.True(Poses.Flinched(stand, 1, ahead: -1, Pose.Stand).Tilt > stand.Tilt + 0.3, "not thrown forward from one behind");
        Assert.Equal(stand, Poses.Flinched(stand, 0, 1, Pose.Stand));
        var lying = Poses.LimbsOf(Pose.Lie, Standing, new Moment(0, 0));
        Assert.Equal(lying, Poses.Flinched(lying, 1, 1, Pose.Lie));
    }

    [Fact]
    public void AWindUpCrouchesTheCreatureLowAndWideItsEarsPinnedAndLetsGoAsItCharges()
    {
        var m = new CreatureMotion { Bracing = true };
        for (int i = 0; i < 20; i++) m.Step(1000.0 / 60, 0, false, null);
        var (x, y) = m.Scale(0);
        Assert.Equal(1, m.Brace);
        Assert.True(x > 1.1 && y < 0.85, $"crouched to ({x:F2}, {y:F2})");
        Assert.Equal(1, m.Ears(0));
        m.Bracing = false;
        for (int i = 0; i < 10; i++) m.Step(1000.0 / 60, 0, false, null);
        Assert.Equal(0, m.Brace);
    }
}
