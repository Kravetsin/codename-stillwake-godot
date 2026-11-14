using Stillwake.Game.Features.Crew;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Helmets sit on the suit as one body, each turning its own way (helmets-fit-the-suit).</summary>
public class BodyFitTests
{
    private static readonly double[] Looks = [-0.6, -0.2, 0, 0.3, 0.5];

    private static void Near(Vec expected, Vec actual)
    {
        Assert.InRange(actual.X, expected.X - 1e-9, expected.X + 1e-9);
        Assert.InRange(actual.Y, expected.Y - 1e-9, expected.Y + 1e-9);
    }

    /// <summary>Every helmet a hero wears, the items and the catchers' alike.</summary>
    public static TheoryData<string> Helmets() =>
    [
        Stillwake.Content.Helmet.Basic, Stillwake.Content.Helmet.Quilted, Stillwake.Content.Helmet.Heavy,
        .. CreatureLooks.Heroes.Values.SelectMany(look => look.Gear).Select(gear => gear.Helmet).Distinct(),
    ];

    [Theory]
    [MemberData(nameof(Helmets))]
    public void EveryHelmetWornHasAFitOfItsOwn(string helmet)
    {
        Assert.Contains(helmet, BodyFit.Fitted);
        Assert.NotSame(BodyFit.Whole, BodyFit.Of(helmet));
    }

    [Fact]
    public void TheModesAreThePickedOnes()
    {
        Assert.Equal(HelmetTurn.Ring, BodyFit.Of(Stillwake.Content.Helmet.Basic).Turn);
        Assert.Equal(HelmetTurn.Soft, BodyFit.Of(Stillwake.Content.Helmet.Quilted).Turn);
        Assert.Equal(HelmetTurn.Bolted, BodyFit.Of(Stillwake.Content.Helmet.Heavy).Turn);
    }

    [Theory]
    [MemberData(nameof(Helmets))]
    public void ABoltedHelmetNeverTurnsOnlyTheFaceInsideDoes(string helmet)
    {
        var fit = BodyFit.Of(helmet);
        if (fit.Turn != HelmetTurn.Bolted) return;
        foreach (double look in Looks)
            Assert.Equal((look, 0d, 0d, 0d), fit.TurnsBy(look));
    }

    [Theory]
    [MemberData(nameof(Helmets))]
    public void ARingedHelmetsBallTurnsWholeAndItsCollarNever(string helmet)
    {
        var fit = BodyFit.Of(helmet);
        if (fit.Turn != HelmetTurn.Ring) return;
        foreach (double look in Looks)
        {
            var (face, shell, bend, crown) = fit.TurnsBy(look);
            Assert.Equal((look, look, look), (face, shell, crown));
            // Nothing is bent: the collar's picture stays as the torso holds it.
            Assert.Equal(0, bend);
        }
    }

    [Theory]
    [MemberData(nameof(Helmets))]
    public void ASoftHelmetBendsOnlyInItsBandItsTopTurningWithItsCrown(string helmet)
    {
        var fit = BodyFit.Of(helmet);
        if (fit.Turn != HelmetTurn.Soft) return;
        // Its crown (lamp, scanner, goggles) turns about the same point its cloth bends at, so they move as one.
        Assert.Equal((BodyFit.NeckX, BodyFit.SoftY), (fit.PivotX, fit.PivotY));
        double joint = BodyFit.SoftY - BodyFit.NeckY;
        foreach (double look in Looks)
        {
            var (face, shell, bend, crown) = fit.TurnsBy(look);
            Assert.Equal((0d, 0d, look, look), (face, shell, bend, crown));
            double c = Math.Cos(look), s = Math.Sin(look);
            for (double x = -6; x <= 6; x += 1.5)
            {
                // Below the band (its base on the shoulders) the picture stays where it is.
                var low = new Vec(x, joint + BentLimb.Band / 2 + 0.25);
                Near(low, BentLimb.Warp(low, joint, bend, lower: true));
                // Above it, it turns whole by the look about the pivot, as the crown does.
                var high = new Vec(x, joint - BentLimb.Band / 2 - 0.25);
                double y = high.Y - joint;
                Near(new Vec(x * c - y * s, joint + x * s + y * c), BentLimb.Warp(high, joint, bend, lower: true));
            }
        }
    }

    [Fact]
    public void AnUpperArmsTopFadesInSoNoRoundEndShowsAtTheShoulder()
    {
        foreach (string picture in (string[])["upperArm", "upperArm_lit", "regenSuit_upperArm", "heatSuit_upperArm_lit"])
            Assert.Equal(BodyFit.Shoulder, BodyFit.FadeOf(picture));
        var (whole, none) = BodyFit.Shoulder;
        Assert.Equal(0, BodyFit.Alpha(none, whole, none));
        Assert.Equal(1, BodyFit.Alpha(whole, whole, none));
        // The shoulder joint itself (2.5) is half drawn at most: the arm's round top never shows whole over the torso.
        Assert.InRange(BodyFit.Alpha(2.5, whole, none), 0.01, 0.5);
        Assert.Null(BodyFit.FadeOf("lowerArm"));
    }

    [Fact]
    public void ACatchersUpperArmKeepsItsTopForTheReceiverStrappedThere()
    {
        foreach (var gear in CreatureLooks.Heroes.Values.SelectMany(look => look.Gear))
            Assert.Null(BodyFit.FadeOf($"{gear.Body}_upperArm"));
    }
}
