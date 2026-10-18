using Stillwake.Game.Features.Crew;
using Xunit;
using Spec = Stillwake.Content.Harpoon;

namespace Stillwake.Game.Tests;

/// <summary>
/// Another hero's harpoon as this peer plays it from what it sends (0065): the rod flying from the gun drawn
/// here, sticking where it was sent to, riding its piece as drawn here, snapping back, the cues for its sounds
/// and the gun drawn loaded only once the shooter's cooldown is over.
/// </summary>
public class RemoteRodTests
{
    /// <summary>The rod covers this many thousandths of a cell a ms in flight, and this many on its way back.</summary>
    private const double Flies = Spec.RodSpeed / 50.0, Returns = Spec.ReturnSpeed / 50.0;

    private static readonly (double X, double Y) Gun = (1_000, 2_000);
    private static readonly RodSeen Flying = new(Rod.Flying, new Point(1_000 + Spec.Range, 2_000));

    /// <summary>Pieces as drawn here, by id; one not in it is gone.</summary>
    private readonly Dictionary<int, (double X, double Y)> _pieces = new();

    private (double X, double Y)? PieceAt(int id) => _pieces.TryGetValue(id, out var at) ? at : null;

    private RodCues Step(RemoteRod rod, double ms, RodSeen? sent, (double X, double Y)? gun = null) =>
        rod.Step(ms, sent, gun ?? Gun, PieceAt);

    /// <summary>Steps <paramref name="rod"/> <paramref name="ms"/> in frames of 10 ms as <paramref name="sent"/> says, its cues gathered.</summary>
    private List<RodCues> Run(RemoteRod rod, double ms, RodSeen? sent)
    {
        var cues = new List<RodCues>();
        for (double t = 0; t < ms; t += 10) cues.Add(Step(rod, 10, sent));
        return cues;
    }

    [Fact]
    public void FliesFromTheGunDrawnHereTowardTheEndOfTheRangeSentAtTheShootersSpeedFiredOnce()
    {
        var rod = new RemoteRod();
        Assert.Equal(new RodCues(Fired: true), Step(rod, 10, Flying));
        Assert.Equal((Rod.Flying, (1.0, 0.0)), (rod.Rod, rod.Heading));
        Assert.Equal(1_000 + 10 * Flies, rod.Tip.X, 6);
        var cues = Run(rod, 100, Flying);
        Assert.All(cues, c => Assert.Equal(default, c));
        Assert.Equal(1_000 + 110 * Flies, rod.Tip.X, 6);
        // Never past its range, however long the shooter is heard flying it.
        Run(rod, 1_000, Flying);
        Assert.Equal((1_000.0 + Spec.Range, 2_000.0), rod.Tip);
        Assert.False(rod.Taut);
    }

    [Fact]
    public void SticksInThePieceItWasSentToRidingItAsDrawnHereAndBitesOnce()
    {
        var rod = new RemoteRod();
        _pieces[4] = (6_000, 2_100);
        Step(rod, 10, Flying);
        var hooked = new RodSeen(Rod.Hooked, new Point(-500, -100), 4);
        Assert.Equal(new RodCues(Bit: true), Step(rod, 10, hooked));
        Assert.Equal((Rod.Hooked, (5_500.0, 2_000.0), 4), (rod.Rod, rod.Tip, rod.Piece));
        Assert.Equal(default, Step(rod, 10, hooked));
        // The sim reels it in here: the rod comes with it, the line taut, the drum turning.
        _pieces[4] = (4_000, 2_100);
        Assert.Equal(default, Step(rod, 10, hooked with { Rod = Rod.Reeling }));
        Assert.Equal((Rod.Reeling, (3_500.0, 2_000.0)), (rod.Rod, rod.Tip));
        Assert.True(rod.Taut && rod.Drums);
    }

    [Fact]
    public void SticksInTheStructureWhereSentKeepingTheWayItFlewPulledAndHanging()
    {
        var rod = new RemoteRod();
        Step(rod, 10, Flying);
        var wall = new RodSeen(Rod.Hooked, new Point(7_000, 2_000));
        Assert.True(Step(rod, 10, wall).Bit);
        Assert.Equal(((7_000.0, 2_000.0), (1.0, 0.0)), (rod.Tip, rod.Heading));
        Assert.False(Step(rod, 10, wall with { Rod = Rod.Pulling }).Bit);
        Assert.True(rod.Drums);
        Step(rod, 10, wall with { Rod = Rod.Hanging });
        Assert.True(rod.Taut && !rod.Drums);
    }

    [Fact]
    public void SnapsBackToTheGunAtTheShootersSpeedOnceFreeThenIsInIt()
    {
        var rod = new RemoteRod();
        Step(rod, 10, Flying);
        Run(rod, Spec.Range / Flies, Flying);
        var back = new RodSeen(Rod.Returning);
        Step(rod, 100, back);
        Assert.Equal((Rod.Returning, 1_000 + Spec.Range - 100 * Returns), (rod.Rod, rod.Tip.X));
        Assert.True(rod.Taut);
        Run(rod, Spec.Range / Returns, back);
        Assert.Equal(Rod.Seated, rod.Rod);
    }

    [Fact]
    public void ComesFreeHereOnceItsPieceIsGoneHereBeforeItsShooterSaysSo()
    {
        var rod = new RemoteRod();
        _pieces[4] = (6_000, 2_000);
        var reeling = new RodSeen(Rod.Reeling, new Point(-500, 0), 4);
        Step(rod, 10, Flying);
        Step(rod, 10, reeling);
        _pieces.Remove(4);
        Assert.Equal(default, Step(rod, 10, reeling));
        Assert.Equal(Rod.Returning, rod.Rod);
        Run(rod, 200, reeling);
        Assert.Equal(Rod.Seated, rod.Rod);
    }

    [Fact]
    public void IsInTheGunAtOnceWhenSentSoOrWithNoGunDrawn()
    {
        var rod = new RemoteRod();
        Step(rod, 10, Flying);
        Step(rod, 10, null);
        Assert.Equal(Rod.Seated, rod.Rod);
        Step(rod, 10, Flying);
        rod.Step(10, Flying, null, PieceAt);
        Assert.Equal(Rod.Seated, rod.Rod);
    }

    [Fact]
    public void FiresAndBitesAtOnceWhenFirstHeardStuckPointingFromTheGun()
    {
        var rod = new RemoteRod();
        Assert.Equal(new RodCues(Fired: true, Bit: true), Step(rod, 10, new RodSeen(Rod.Hooked, new Point(1_000, 5_000))));
        Assert.Equal((0.0, 1.0), rod.Heading);
    }

    [Fact]
    public void IsNoShotWhenFirstHeardOnItsWayBack()
    {
        var rod = new RemoteRod();
        Assert.Equal(default, Step(rod, 10, new RodSeen(Rod.Returning)));
        Assert.Equal(Rod.Seated, rod.Rod);
        Assert.True(rod.Loaded);
    }

    [Fact]
    public void IsDrawnLoadedOnlyOnceTheCooldownSinceTheShotSeenIsOver()
    {
        var rod = new RemoteRod();
        Assert.True(rod.Loaded);
        Step(rod, 10, Flying);
        Step(rod, 10, null);
        Assert.False(rod.Loaded);
        Run(rod, HarpoonShot.CooldownMs - 30, null);
        Assert.False(rod.Loaded);
        Run(rod, 20, null);
        Assert.True(rod.Loaded);
    }

    [Fact]
    public void AimsTheGunWhereTheRodFliesSticksOrIsAndNowhereInTheGun()
    {
        var rod = new RemoteRod();
        _pieces[4] = (6_000, 2_000);
        Assert.Null(rod.AimAt(null, PieceAt));
        Assert.Equal((1_000.0 + Spec.Range, 2_000.0), rod.AimAt(Flying, PieceAt));
        Assert.Equal((5_500.0, 2_000.0), rod.AimAt(new RodSeen(Rod.Hooked, new Point(-500, 0), 4), PieceAt));
        Step(rod, 10, Flying);
        Run(rod, 100, Flying);
        Assert.Equal(rod.Tip, rod.AimAt(new RodSeen(Rod.Returning), PieceAt));
    }
}
