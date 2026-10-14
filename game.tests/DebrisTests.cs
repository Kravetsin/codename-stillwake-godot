using Stillwake.Game.Features.Crew;
using Stillwake.Game.Features.Debris;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Grid;
using Xunit;
using Spec = Stillwake.Content.Debris;

namespace Stillwake.Game.Tests;

/// <summary>
/// The client's debris: fade.test.ts, the debris tests of crew/aim.test.ts and of
/// session/predicted.test.ts, ported.
/// </summary>
public class DebrisTests
{
    private const int Unit = Hero.Unit;

    [Fact]
    public void APieceComesOutOfTheDarkOverTheStartOfItsPathAndGoesBackIntoItAtTheEnd()
    {
        double Fade(int x) => Catching.FadeOf(x, 100_000, 10_000);
        Assert.Equal([0, 0.5, 1, 0.5, 0], new[] { Fade(100_000), Fade(96_000), Fade(50_000), Fade(14_000), Fade(9_000) });
    }

    // A piece of debris under the pointer (aim.test.ts).

    /// <summary>A hero whose body is centred on the middle of cell (x, y).</summary>
    private static Hero HeroOn(int x, int y) => new(x * Unit + Unit / 2, y * Unit + Unit / 2 + Hero.MiddleUp, 0, 0, false, 1);

    private static (double X, double Y) Centre(int x, int y) => (x + 0.5, y + 0.5);

    /// <summary>A hull of one cell in the corner, for the debris to fly past, run until the first piece is out.</summary>
    private static (Registry M, World World) Corner()
    {
        var m = new Registry();
        var world = World.Create(1, Layout.Parse(["#".PadRight(12), .. Enumerable.Repeat(new string(' ', 12), 7)]), m.All);
        for (int i = 0; i < Spec.Every; i++) world.Step([]);
        return (m, world);
    }

    private static Catch? AimAt(Registry m, int peer, Hero hero, (double X, double Y) point, IReadOnlySet<int>? passOver = null)
    {
        int width = m.Grid.Width;
        return Catching.AimAt(m.Debris, m.Rooms, width, peer, hero.BodyCell(width), cell => Aiming.Reaches(hero, cell, width), point, passOver);
    }

    [Fact]
    public void IsGrabbedOnAPressOrCarriesWhyTheSimWouldRefuseIt()
    {
        var (m, world) = Corner();
        var piece = m.Debris.Pieces[0];
        int x = piece.X / Unit, y = piece.Y / Unit;
        // The sim knows no hero outside yet.
        Assert.Equal(new Catch(piece.Id, y * 12 + x, true, "debris.notOutside"), AimAt(m, 0, HeroOn(x - 2, y), Centre(x, y)));
        world.Step([new Sim.Crew.Enter(y * 12 + x - 2)]);
        var aim = AimAt(m, 0, HeroOn(x - 2, y), Centre(x, y));
        Assert.Equal(new Catch(piece.Id, y * 12 + x, true, null), aim);
        Assert.Equal(new Grab(piece.Id), Catching.GrabOf(aim, pressed: true));
        Assert.Null(Catching.GrabOf(aim, pressed: false));
        // Grabbed, its grab in flight: the aim passes over it, so E cannot send it twice.
        Assert.Null(AimAt(m, 0, HeroOn(x - 2, y), Centre(x, y), new HashSet<int> { piece.Id }));
    }

    [Fact]
    public void IsAimedAtFromBesideItWithinARadiusOfThePointerNotFromFarther()
    {
        var (m, _) = Corner();
        var piece = m.Debris.Pieces[0];
        var hero = HeroOn(piece.X / Unit - 2, piece.Y / Unit);
        // Off to its left, inside the grid: it came in from the right edge.
        (double, double) Off(int by) => ((double)(piece.X - by) / Unit, (double)piece.Y / Unit);
        Assert.Equal(piece.Id, AimAt(m, 0, hero, Off(Spec.AimRadius - 100))?.Piece);
        Assert.Null(AimAt(m, 0, hero, Off(Spec.AimRadius + 100)));
    }

    [Fact]
    public void IsNotAimedAtWhileTheHeroIsAboardItDriftsPastBehindTheHull()
    {
        var m = new Registry();
        var world = World.Create(1, Registry.Wreck, m.All);
        for (int i = 0; i < 4 * Spec.Every && m.Debris.Pieces.Count == 0; i++) world.Step([]);
        var piece = m.Debris.Pieces[0];
        var aboard = Hero.At(Ship.Of(m.Grid, m.Rooms), Registry.Wreck.CellsOf('S').First());
        Assert.Null(AimAt(m, 0, aboard, ((double)piece.X / Unit, (double)piece.Y / Unit)));
    }

    // The debris this peer is grabbing (predicted.test.ts).

    [Fact]
    public void IsEachPieceWithAGrabInFlightAndNoneOnceItHasRun()
    {
        var link = new Fed();
        var client = new Client(World.Create(1, Layout.Parse("#D#\n#D#\n..."), new Registry().All), link, 1);
        Command[] sent = [new Grab(4), new ToggleDoor(1), new Grab(9)];
        foreach (var c in sent) client.Send(c);
        Assert.Equal([4, 9], Catching.Grabbing(client.InFlight).Order());
        link.Receive(new TickMessage(client.World.Tick, sent.Select(c => new Stamped(1, client.World.Encode(c))).ToList()));
        client.Update();
        Assert.Empty(Catching.Grabbing(client.InFlight));
    }
}
