using System.Text.Json;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Creatures;
using Stillwake.Sim.Debris;
using Xunit;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Tests;

/// <summary>
/// Tumbling pieces as the client draws and aims at them (debris-physics, step 1): the pose drawn between ticks, and every aim, the
/// rod's and the hands', on the sim's turned box (<see cref="TurnedBox"/>), a sentry's body turned with its wreck too.
/// </summary>
public class TurnedAimTests
{
    private const int Unit = Hero.Unit, Quarter = 16384;
    private static readonly JsonSerializerOptions Loose = new() { PropertyNameCaseInsensitive = true };
    private static readonly Func<Piece, double, double, bool> Clear = (_, _, _) => false;

    /// <summary>A piece of <paramref name="item"/> at (<paramref name="x"/>, <paramref name="y"/>), in thousandths of a cell, turned and tumbling as given.</summary>
    private static Piece Of(string item, int x, int y, int angle = 0, int spin = 0, int speed = 0) =>
        JsonSerializer.Deserialize<Piece>($$"""{"id":1,"item":"{{item}}","x":{{x}},"y":{{y}},"speed":{{speed}},"angle":{{angle}},"spin":{{spin}},"end":-100000}""", Loose)!;

    [Fact]
    public void APieceIsDrawnTurnedByItsSpinBetweenTicksItsBoxWrappedRoundTheTurn()
    {
        var piece = Of("cableRack", 10_000, 5_000, angle: 65_500, spin: 100, speed: 50);
        Assert.Equal(new PiecePose(9_975, 5_000, 65_550), PieceDrawn.Pose(piece, 0.5));
        Assert.Equal(65_600, PieceDrawn.Pose(piece, 1).Angle);
        Assert.Equal(TurnedBox.Of(1, 9_950, 5_000, 2_000, 2_000, 64), PieceDrawn.Shape(piece, 1, 2_000, 2_000));
        // Turning back past upright, and a quarter turn in radians.
        var back = Of("cableRack", 10_000, 5_000, angle: 10, spin: -40);
        Assert.Equal(TurnedBox.Of(1, 10_000, 5_000, 2_000, 2_000, 65_526), PieceDrawn.Shape(back, 0.5, 2_000, 2_000));
        Assert.Equal(Math.PI / 2, PieceDrawn.Pose(Of("cableRack", 0, 0, angle: Quarter), 0).Radians, 9);
    }

    [Fact]
    public void TheHandsPointAtAWreckByItsTurnedBox()
    {
        // A cargo hold, 12 cells by 6, stood on its end: five cells below its middle is on it, five to its right is not.
        var upright = Of("cargoHold", 40_000, 20_000);
        var turned = Of("cargoHold", 40_000, 20_000, angle: Quarter);
        Assert.Null(WreckAim.Under([upright], (40, 25)));
        Assert.Equal(turned, WreckAim.Under([turned], (40, 25)));
        Assert.Equal(upright, WreckAim.Under([upright], (45, 20)));
        Assert.Null(WreckAim.Under([turned], (45, 20)));
    }

    /// <summary>
    /// Open space 48 cells by 28 with a hull block in the corner, peer 0's hero out in it, and one still piece of <paramref name="item"/>
    /// at (<paramref name="x"/>, <paramref name="y"/>), turned by <paramref name="angle"/>, id 1.
    /// </summary>
    private static TestWorld Field(string item, int x, int y, int angle)
    {
        var w = new TestWorld(Layout.Parse(["#".PadRight(24), .. Enumerable.Repeat(new string(' ', 24), 13)]).Doubled());
        for (int i = 0; i < Spec.Debris.Every; i++) w.Step();
        w.Edit(root =>
        {
            var debris = root["slices"]!["debris"]!;
            var pieces = debris["pieces"]!.AsArray();
            var piece = pieces[0]!.DeepClone();
            pieces.Clear();
            (piece["id"], piece["item"], piece["x"], piece["y"], piece["speed"], piece["angle"], piece["spin"]) = (1, item, x, y, 0, angle, 0);
            pieces.Add(piece);
            debris["nextId"] = 2;
        });
        return w.Step(new Sim.Crew.Enter(15 * 48 + 13));
    }

    [Fact]
    public void TheRodMeetsAWreckByItsTurnedBoxAndSticksInItsTurnedPicture()
    {
        // Along row 10.5: over the upright hold's box (its margin from 11.5), into the one stood on its end at its left side, 21.
        (double, double) from = (13_000, 10_500), to = (40_000, 10_500);
        Assert.Null(Aiming.Hooks(Field("cargoHold", 24_000, 15_000, 0).M, 0, from, to, 0));
        var hit = Aiming.Hooks(Field("cargoHold", 24_000, 15_000, Quarter).M, 0, from, to, 0);
        Assert.Equal(1, hit?.Piece);
        Assert.Equal((21_300.0, 10_500.0), hit!.Value.At);
    }

    [Fact]
    public void AStrokeMeetsASentryByItsBodyTurnedWithItsWreck()
    {
        var w = Field("engineBlock", 24_000, 15_000, 0);
        w.Step(new Release(Spec.Creatures.Sentry.Id, 13 * 48 + 24));
        var sentry = w.M.Creatures.Creatures.Single(c => w.M.Creatures.ClingsTo(c) == 1);
        Assert.Equal(0, w.M.Creatures.StanceOf(sentry));
        w.Edit(root => root["slices"]!["debris"]!["pieces"]![0]!["angle"] = Quarter).Step();
        sentry = w.M.Creatures.Creatures.Single(c => c.Id == sentry.Id);
        Assert.Equal(Quarter, w.M.Creatures.StanceOf(sentry));
        // Stood on the wreck's top, now its right side: its body runs right of its feet, not up from them.
        int fx = sentry.X, fy = sentry.Y;
        Assert.Equal(27_000, fx);
        var box = Aiming.BoxOf(w.M, sentry);
        Assert.True(Aiming.Inside(box, fx + 2_500, fy + 1_000));
        Assert.False(Aiming.Inside(box, fx, fy - 2_500));
        var right = ToolRays.Segment(w.M, (fx + 2_500, fy - 5_000), (fx + 2_500, fy + 5_000), outside: true, 0, Clear).Hit;
        Assert.Contains(sentry, right.Creatures);
        var above = ToolRays.Segment(w.M, (fx - 1_000, fy - 3_500), (fx - 1_000, fy - 1_000), outside: true, 0, Clear).Hit;
        Assert.DoesNotContain(sentry, above.Creatures);
    }
}
