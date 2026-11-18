using Stillwake.Game.Features.Crew;
using Stillwake.Game.Shared;
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
        .. Stillwake.Content.Items.All.Where(i => i.Equip == Stillwake.Content.Helmet.Slot).Select(i => i.Id),
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
        Assert.Equal(HelmetTurn.Soft, BodyFit.Of(Stillwake.Content.Helmet.Basic).Turn);
        Assert.Equal(HelmetTurn.Ring, BodyFit.Of(Stillwake.Content.Helmet.Heavy).Turn);
        // The raw painted helmets (gear-roles) sit and nod as the basic ball does.
        foreach (string helmet in (string[])[Stillwake.Content.Helmet.Heated, Stillwake.Content.Helmet.HeatProof, "hideHelmet", "boilerHelmet"])
            Assert.Same(BodyFit.Of(Stillwake.Content.Helmet.Basic), BodyFit.Of(helmet));
        // The dust wrap's filter hangs below the seal: it turns whole on a seal of its own, as the skins do, so the band never stretches it.
        Assert.Same(BodyFit.Of("skullHelmet"), BodyFit.Of("dustHelmet"));
    }

    [Fact]
    public void TheBellTurnsWholeOnItsGasketAsTheSkinsDoWithNoShoulderPiece()
    {
        // Its shoulder piece met no suit well: the bell sits as the helmet skins do, its dome on its own rubber seal.
        Assert.Same(BodyFit.Of("brassHelmet"), BodyFit.Of(Stillwake.Content.Helmet.Heavy));
        Assert.Null(BodyFit.FadeOf("heavyHelmet_collar"));
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

    [Fact]
    public void ARingedHelmetsBallTurnsWholeAndItsCollarNever()
    {
        // No helmet turns so now (the basic one's ring read as broken in play), but F8 still tries it, as the bare head does.
        var fit = BodyFit.Whole;
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
        // Its crown (lamp, scanner, goggles) turns about the same point its cloth bends at, so they move as one: the figure bends
        // its pictures there, about the neck's line.
        Assert.Equal(BodyFit.NeckX, fit.PivotX);
        double joint = fit.PivotY - BodyFit.NeckY;
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
    public void TheBasicHelmetNodsOnItsSealBendingAsTheHoodsClothDoes()
    {
        var fit = BodyFit.Of(Stillwake.Content.Helmet.Basic);
        Assert.Equal((HelmetTurn.Soft, BodyFit.NeckX, BodyFit.SealY), (fit.Turn, fit.PivotX, fit.PivotY));
        // Its seal is its own picture over the near arm, the ball whole above it: nothing cut off the shell.
        Assert.Null(fit.Cut);
    }

    [Fact]
    public void ATrialTurnsEveryHelmetThatWayAndItsOwnComesBack()
    {
        try
        {
            foreach (var turn in Enum.GetValues<HelmetTurn>())
            {
                BodyFit.Trial = turn;
                foreach (string helmet in BodyFit.Fitted)
                    Assert.Equal(turn, BodyFit.Of(helmet).Turn);
                Assert.Same(BodyFit.Whole, BodyFit.Of(null));
            }
        }
        finally
        {
            BodyFit.Trial = null;
        }
        Assert.Equal(HelmetTurn.Ring, BodyFit.Of(Stillwake.Content.Helmet.Heavy).Turn);
    }

    [Fact]
    public void AnArmsTopFadesInSoNoRoundEndShowsAtTheShoulder()
    {
        foreach (string picture in (string[])["arm", "arm_lit", "regenSuit_arm", "heatSuit_arm_lit", "outfit_1_arm"])
            Assert.Equal(BodyFit.Shoulder, BodyFit.FadeOf(picture));
        var (whole, none) = BodyFit.Shoulder;
        Assert.Equal(0, BodyFit.Alpha(none, whole, none));
        Assert.Equal(1, BodyFit.Alpha(whole, whole, none));
        // The shoulder joint itself (2.5) is half drawn at most: the arm's round top never shows whole over the torso.
        Assert.InRange(BodyFit.Alpha(2.5, whole, none), 0.01, 0.5);
        Assert.Null(BodyFit.FadeOf("leg"));
    }

    [Fact]
    public void ACatchersArmKeepsItsTopForTheReceiverStrappedThere()
    {
        foreach (var gear in CreatureLooks.Heroes.Values.SelectMany(look => look.Gear))
        {
            Assert.Null(BodyFit.FadeOf($"{gear.Body}_arm"));
        }
    }

    [Fact]
    public void ACollarCoversTheNearArmOnlyWhileItHangs()
    {
        // Hanging, swung back or held out level: the shoulder piece over its shoulder.
        Assert.True(BodyFit.CollarOverArm(new Bend(0, 0)));
        Assert.True(BodyFit.CollarOverArm(new Bend(-0.5, 0.3)));
        Assert.True(BodyFit.CollarOverArm(new Bend(Math.PI / 2 - 0.05, 0)));
        // Raised past the face, forward, straight up or back over the head: the arm over the helmet's lower part.
        Assert.False(BodyFit.CollarOverArm(Rig.Reach(new Vec(20, -70), Rig.Arm, 1)));
        Assert.False(BodyFit.CollarOverArm(new Bend(Math.PI, 0)));
        Assert.False(BodyFit.CollarOverArm(new Bend(-2.5, 0)));
        // The elbow low but the fist folded up past the shoulder: its forearm crosses the collar too.
        Assert.False(BodyFit.CollarOverArm(new Bend(0.2, 2.9)));
    }
}
