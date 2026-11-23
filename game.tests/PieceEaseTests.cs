using System.Text.Json;
using Stillwake.Game.Shared;
using Stillwake.Sim.Debris;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>A guest's pieces (debris-physics): the feed's corrections eased away in the drawing, plain motion drawn as the sim has it.</summary>
public class PieceEaseTests
{
    private const double Frame = 1000.0 / 60;

    /// <summary>The ticks a frame of 60 a second carries a drawing on: a third of one.</summary>
    private const double PerFrame = 20 / 60.0;

    private static readonly JsonSerializerOptions Loose = new() { PropertyNameCaseInsensitive = true };

    /// <summary>A piece as the sim keeps it, moved by hand as its drift and the host player's feed would.</summary>
    private sealed record Sim(int Id, int X, int Y, int Vx, int Vy = 0, int Angle = 0, int Spin = 0)
    {
        public Piece Piece => JsonSerializer.Deserialize<Piece>(
            $$"""{"id":{{Id}},"item":"scrapChunk","x":{{X}},"y":{{Y}},"vx":{{Vx}},"vy":{{Vy}},"angle":{{Angle}},"spin":{{Spin}}}""", Loose)!;

        public Sim Drifted => this with { X = X + Vx, Y = Y + Vy, Angle = (Angle + Spin) & 0xFFFF };

        public PiecePose Carried(double drift) => new(X + Vx * drift, Y + Vy * drift, Angle + Spin * drift);
    }

    /// <summary>A guest drawing pieces at 60 frames a second: the world's tick, the drift into it.</summary>
    private sealed class Guest
    {
        public readonly PieceEase Ease = new();
        public int Tick = 100;
        public double Drift;

        /// <summary><paramref name="frames"/> frames of <paramref name="sim"/> drawn, drifting as each tick passes: where it was drawn in the last.</summary>
        public PiecePose Play(ref Sim sim, int frames = 1)
        {
            var drawn = default(PiecePose);
            for (int i = 0; i < frames; i++)
            {
                Drift += PerFrame;
                while (Drift >= 1) (sim, Tick, Drift) = (sim.Drifted, Tick + 1, Drift - 1);
                Ease.Frame(true, Tick, Drift, Frame);
                drawn = Ease.Drawn(sim.Piece, sim.Carried(Drift));
            }
            return drawn;
        }
    }

    [Fact]
    public void PlainMotionIsDrawnAsTheSimHasIt()
    {
        var guest = new Guest();
        // Turning past a whole turn, too.
        var sim = new Sim(3, 40_000, 9_000, -110, 30, 65_500, 90);
        for (int i = 0; i < 40; i++)
        {
            var drawn = guest.Play(ref sim);
            Assert.Equal(sim.Carried(guest.Drift), drawn);
        }
    }

    [Fact]
    public void AJumpIsKeptOffTheScreenAndEasedOutWithinItsTime()
    {
        var guest = new Guest();
        var sim = new Sim(3, 40_000, 9_000, -110, Spin: 90);
        guest.Play(ref sim, 6);
        var before = sim.Carried(guest.Drift);
        // The host player's feed: a cell down, faster, turned a little.
        sim = sim with { Y = sim.Y + 1_000, Vx = -160, Angle = sim.Angle + 400 };
        var drawn = guest.Play(ref sim);
        // Drawn where it was carried on by its old motion: none of the jump on the screen yet.
        Assert.Equal(before.Y, drawn.Y, 6);
        Assert.Equal(before.X - 110 * PerFrame, drawn.X, 6);
        Assert.Equal(before.Angle + 90 * PerFrame, drawn.Angle, 6);
        // Eased out in its time: a twentieth left at most, and gone a while after.
        drawn = guest.Play(ref sim, (int)Math.Ceiling(PieceEase.EaseMs / Frame));
        var now = sim.Carried(guest.Drift);
        Assert.InRange(now.Y - drawn.Y, 0, 0.05 * 1_000);
        Assert.InRange(drawn.Angle - now.Angle, -0.05 * 400, 0);
        drawn = guest.Play(ref sim, 30);
        Assert.Equal(sim.Carried(guest.Drift), drawn);
    }

    [Fact]
    public void AJumpTooFarToBeACorrectionSnaps()
    {
        var guest = new Guest();
        var sim = new Sim(3, 40_000, 9_000, -110);
        guest.Play(ref sim, 6);
        sim = sim with { X = sim.X + (int)PieceEase.SnapAt + 1_000 };
        var drawn = guest.Play(ref sim);
        Assert.Equal(sim.Carried(guest.Drift), drawn);
    }

    [Fact]
    public void APieceGoneIsForgottenAndOnTheHostPlayersClientNothingEases()
    {
        var guest = new Guest();
        var sim = new Sim(3, 40_000, 9_000, -110);
        var other = new Sim(4, 20_000, 9_000, 0);
        guest.Play(ref sim, 2);
        guest.Ease.Drawn(other.Piece, other.Carried(guest.Drift));
        Assert.Equal(2, guest.Ease.Count);
        // Piece 3 goes: no longer drawn, it is forgotten as the next tick comes.
        guest.Play(ref other, 6);
        Assert.Equal(1, guest.Ease.Count);
        // Off, as on the host player's client: drawn as the sim has it, nothing kept.
        guest.Ease.Frame(false, guest.Tick, guest.Drift, Frame);
        sim = sim with { Y = sim.Y + 1_000 };
        Assert.Equal(sim.Carried(guest.Drift), guest.Ease.Drawn(sim.Piece, sim.Carried(guest.Drift)));
        Assert.Equal(0, guest.Ease.Count);
    }
}
