using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>
/// The light drone's modes on the drone key (light-drone): its wheel, lamp above and scanner below, a shut side never picked; a tap on to
/// the next open mode; the scanner open with its node; the mode sent with the hero for friends to draw.
/// </summary>
public class DroneModesTests
{
    private static bool ScannerKnown(string node) => node == Stillwake.Content.LightDrone.ScannerMode;
    private static bool NothingKnown(string node) => false;

    private static Wheel<DroneMode> NewWheel(bool scanner) =>
        new(DroneModes.Sides) { Opens = mode => DroneModes.Opened(mode, scanner ? ScannerKnown : NothingKnown) };

    private static DroneMode? Step(Wheel<DroneMode> wheel, bool held, double ms, (double, double) at, DroneMode now = DroneMode.Lamp) =>
        wheel.Step(held, ms, at, Wheel.Middle, DroneModes.Next(now, wheel.Opens));

    [Fact]
    public void TheLampIsAboveTheScannerBelowAndTheScannerOpensWithItsNode()
    {
        Assert.Equal([DroneMode.Lamp, DroneMode.Scan], DroneModes.Sides);
        Assert.True(DroneModes.Opened(DroneMode.Lamp, NothingKnown));
        Assert.False(DroneModes.Opened(DroneMode.Scan, NothingKnown));
        Assert.True(DroneModes.Opened(DroneMode.Scan, ScannerKnown));
    }

    [Theory]
    [InlineData(true, DroneMode.Scan)]
    [InlineData(false, null)]
    public void LettingGoOnTheScannerPicksItOnlyOpenAShutSideNeverEvenFlicked(bool known, DroneMode? picked)
    {
        var wheel = NewWheel(known);
        Step(wheel, true, 16, (100, 100));
        Step(wheel, true, Wheel.TapMs, (102, 140));
        Assert.True(wheel.Open);
        Assert.Equal(1, wheel.TowardAt);
        Assert.Equal(picked, wheel.Toward);
        Assert.Equal(picked, Step(wheel, false, 16, (102, 140)));

        // A quick flick toward it, the wheel not yet open: no tap either.
        Step(wheel, true, 16, (100, 100));
        Assert.Equal(picked, Step(wheel, false, 50, (100, 140)));
    }

    [Fact]
    public void LettingGoOnTheLampPicksItAndInTheMiddleNothing()
    {
        var wheel = NewWheel(true);
        Step(wheel, true, 16, (100, 100));
        Step(wheel, true, Wheel.TapMs, (100, 60));
        Assert.Equal(DroneMode.Lamp, Step(wheel, false, 16, (100, 60), DroneMode.Scan));
        Step(wheel, true, 16, (100, 100));
        Step(wheel, true, Wheel.TapMs, (104, 100));
        Assert.Null(Step(wheel, false, 16, (104, 100), DroneMode.Scan));
    }

    [Fact]
    public void ATapGoesOnToTheNextOpenModeRoundTheWheelAndWithTheLampAloneDoesNothing()
    {
        var open = NewWheel(true);
        Step(open, true, 16, (100, 100));
        Assert.Equal(DroneMode.Scan, Step(open, false, 16, (101, 100), DroneMode.Lamp));
        Step(open, true, 16, (100, 100));
        Assert.Equal(DroneMode.Lamp, Step(open, false, 16, (101, 100), DroneMode.Scan));

        var shut = NewWheel(false);
        Step(shut, true, 16, (100, 100));
        Assert.Null(Step(shut, false, 16, (101, 100), DroneMode.Lamp));
    }

    [Fact]
    public void TheLampShinesTheScannerScansWithTheLightOutAndNeitherWithoutTheDrone()
    {
        Assert.Equal((true, false), DroneModes.Shown(true, DroneMode.Lamp));
        Assert.Equal((true, false), DroneModes.Shown(true, null));
        Assert.Equal((false, true), DroneModes.Shown(true, DroneMode.Scan));
        Assert.Equal((false, false), DroneModes.Shown(false, DroneMode.Scan));
        Assert.Equal((false, false), DroneModes.Shown(false, null));
    }

    [Fact]
    public void TheModeGoesOverTheWireWithTheHeroTheLampAsNothing()
    {
        var hero = new Hero(1200, 3400, 0, 0, false, 1);
        var scanning = new SentHero(new Seen(hero, Drone: DroneModes.Sent(DroneMode.Scan)), 1250);
        Assert.Equal(scanning, SentHero.Decode(scanning.Encode()));
        Assert.Equal(DroneMode.Scan, SentHero.Decode(scanning.Encode())!.Value.Seen.Drone);
        Assert.Null(DroneModes.Sent(DroneMode.Lamp));
        var lit = new SentHero(new Seen(hero, Drone: DroneModes.Sent(DroneMode.Lamp)), 1250);
        Assert.Null(SentHero.Decode(lit.Encode())!.Value.Seen.Drone);
        Assert.True(lit.Encode().Length < scanning.Encode().Length);
    }
}
