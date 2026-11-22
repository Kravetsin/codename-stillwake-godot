using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Shared;
using I = Stillwake.Game.Shared.DroneInk;

namespace Stillwake.Game.Features.Crew;

/// <summary>Which vision a drone's form plays (form-vision): what it shows the hero the form does.</summary>
public enum VisionForm { Scan, Lamp, Alarms }

/// <summary>A beat of a vision, s from its start: a thing arrives, is read, is taken, its result is read, and it goes, a beat each.</summary>
public readonly record struct Beat(double From, double To)
{
    /// <summary>The first beat: from <paramref name="from"/>, <paramref name="lasts"/> long.</summary>
    public static Beat First(double from, double lasts) => new(from, from + lasts);

    /// <summary>The beat after this one, <paramref name="lasts"/> long.</summary>
    public Beat Then(double lasts) => new(To, To + lasts);

    /// <summary>How far through it <paramref name="u"/> is, 0..1; an empty one steps.</summary>
    public double Run(double u) => To <= From ? u >= From ? 1 : 0 : Math.Clamp((u - From) / (To - From), 0, 1);

    /// <summary>The same, eased.</summary>
    public double Eased(double u) => FormVision.Sm(Run(u));
}

/// <summary>
/// A thing shown in the void (form-vision): the game's own picture (<c>res://art/hd/{Picture}.png</c>), <see cref="W"/> by <see cref="H"/> of
/// its pixels drawn <see cref="Scale"/> times, and the lines its diagram is traced in, picture px: its outline traced from the picture's alpha,
/// its main lines placed by hand, a cracked one's cracks and their blow, a lamp's bulb.
/// </summary>
public sealed record VisionProp(string Picture, double W, double H, double Scale, Vec[] Outline, Vec[][] Lines, Vec[][] Cracks, Vec Bulb = default, Vec Blow = default);

/// <summary>
/// A prop as shown: where its middle is and how it is turned (pick px, radians), how far it has come out of the dark, 0..1, how far its
/// diagram has run over it, 0..1 («Проступает схема»: the picture darkens as it does), and whether its cracks show.
/// </summary>
public readonly record struct PropShown(VisionProp Prop, Vec At, double Angle, double Alpha, double Trace, bool Cracked);

/// <summary>
/// The vision's drone: where it hangs and where its eye looks (pick px, radians), the shape its sparks form and the s they take into it, or out
/// of the one before, the thought's picture in <see cref="SwarmShape.Thought"/>, how red an alarm turns it, 0..1, and whether its reticle is open.
/// </summary>
public readonly record struct VisionDrone(Vec At, double Look, SwarmShape Shape, double ShapeOver, DroneThought? Thought, double Red, bool Reads);

/// <summary>The frame that follows («Кадр следует»): the pick's point at the screen's middle and how far it has glided in, 1 at rest.</summary>
public readonly record struct VisionCam(Vec Middle, double Zoom);

/// <summary>
/// A dot of the scanner's gauge out of its mark: flown <see cref="Along"/> its way from <see cref="From"/> to <see cref="To"/> (a piece, or
/// <see cref="FormVision.Seat"/>), how far out it is, 0..1, and how lit.
/// </summary>
public readonly record struct VisionDot(int From, int To, double Along, double Away, double Lit);

/// <summary>
/// The lamp's vision: the cable from its pivot to the lamp's top, the bulb, how lit the beam is (it lies along the drone's look), how much of the
/// crate it covers, and how the drone studies the lamp's dish: a thread from its eye, and a spark that far home along it, 0..1.
/// </summary>
public readonly record struct VisionLamp(Vec Pivot, Vec Top, Vec Bulb, double Light, double InCone, double Study, double StudyRun);

/// <summary>The cold creeping in from the edge the hero faces: its front, pick px; how much shows; its front's diagram run.</summary>
public readonly record struct VisionFrost(double Front, double Alpha, double Trace);

/// <summary>The meteor: its head and way, pick px, how large it is drawn, how much shows, its spin, its diagram run.</summary>
public readonly record struct VisionMeteor(Vec Head, Vec Way, double Scale, double Alpha, double Spin, double Trace);

/// <summary>
/// The vision at a moment (form-vision): how dark the void is (<see cref="Veil"/>) and how much HUD shows, the frame, the drone, and what the
/// form's scene holds now. Pick px (the page's, four to a world px), the hero's feet at <see cref="FormVision.Feet"/>, facing left: the view
/// mirrors it for a hero facing right.
/// </summary>
public readonly record struct VisionFrame(VisionForm Form, double U, double Veil, double Hud, VisionCam Cam, VisionDrone Drone, PropShown? First,
    PropShown? Second, VisionLamp? Lamp, VisionFrost? Frost, VisionMeteor? Meteor);

/// <summary>
/// The vision of a form the light drone learned (form-vision, the picks «Проступает схема» and «Кадр следует»): the world goes, the hero floats
/// held, and the drone before him shows what the form does, one thing at a time: a thing arrives alone and holds while it is read, the drone
/// takes it, the result holds, it goes, and while anything moves nothing new appears. The scanner: a whole piece, the gauge's row full, then a
/// cracked one, the row half lit. The lamp: the drone copies a lamp's dish and its beam finds a crate in the dark. The alarms: the cold and a
/// snowflake, then a far meteor creeping nearer and the drone darting red at it. A thread of sparks runs over what the drone takes, its picture
/// darkens and its diagram stays; the view glides in to each thing, takes in the drone as it acts, and glides back, no shake. The page's numbers
/// (tools/pick-kit/pages/form-vision.js), pick px and seconds; client only.
/// </summary>
public static class FormVision
{
    /// <summary>The world going and coming back, s.</summary>
    public const double In = 0.6, Out = 0.6;

    /// <summary>The HUD going and coming back, s.</summary>
    private const double HudS = 0.3;

    /// <summary>The scanner's beats.</summary>
    public static class ScanBeats
    {
        public static readonly Beat AIn = Beat.First(0.4, 1.1), ARead = AIn.Then(1.25), Forms = ARead.Then(0.5), FormHold = Forms.Then(0.4),
            ATrace = FormHold.Then(0.7), ADots = ATrace.Then(1.3), AResult = ADots.Then(1.15), AOut = AResult.Then(0.7), AGap = AOut.Then(0.25),
            BIn = AGap.Then(1.1), BRead = BIn.Then(1.2), BAim = BRead.Then(0.3), BTrace = BAim.Then(0.7), BDots = BTrace.Then(1.05),
            BResult = BDots.Then(1.1), End = BResult.Then(Out);
    }

    /// <summary>The lamp's beats.</summary>
    public static class LampBeats
    {
        public static readonly Beat LIn = Beat.First(0.5, 1.2), LRead = LIn.Then(1.3), Go = LRead.Then(1.0), LTrace = Go.Then(0.8),
            Dish = LTrace.Then(0.6), LResult = Dish.Then(1.2), LOut = LResult.Then(1.0), LGap = LOut.Then(0.25), LightOn = LGap.Then(0.3),
            Sweep = LightOn.Then(2.0), CTrace = Sweep.Then(0.7), CResult = CTrace.Then(1.25), End = CResult.Then(Out);
    }

    /// <summary>The alarms' beats.</summary>
    public static class AlarmBeats
    {
        public static readonly Beat CIn = Beat.First(0.6, 1.4), CRead = CIn.Then(1.2), CGlyph = CRead.Then(0.6), CHold = CGlyph.Then(0.4),
            CTrace = CHold.Then(0.6), CResult = CTrace.Then(1.1), COut = CResult.Then(0.9), MGap = COut.Then(0.25), MIn = MGap.Then(0.8),
            MRead = MIn.Then(1.2), Dart = MRead.Then(0.6), MHold = Dart.Then(0.4), MTrace = MHold.Then(0.6), MResult = MTrace.Then(1.1),
            Pass = MResult.Then(1.15), End = Pass.Then(Out);
    }

    /// <summary>Pick px a world px (the kit's zoom).</summary>
    public const double PerWorldPx = 4;

    /// <summary>How tall the hero stands, world px: about five cells (0104).</summary>
    public const double HeroTall = 40;

    /// <summary>
    /// The vision's zoom at rest on a screen <paramref name="height"/> px tall, screen px a world px: a whole number of them a picture's px (the
    /// pick's <see cref="PerWorldPx"/>), so its pictures stay crisp, the hero about a quarter of the screen's height; 4 on a 720 px screen, 8 on 1080.
    /// </summary>
    public static float Zoom(double height) => (float)(PerWorldPx * Math.Max(1, Math.Round(height / 4 / HeroTall / PerWorldPx)));

    /// <summary>Where the hero's feet are in the pick's frame, and the pick's frame's size.</summary>
    public static readonly Vec Feet = new(780, 470), Frame = new(1000, 540);

    /// <summary>The pick's frame's middle: where the view rests.</summary>
    public static readonly Vec Middle = new(Frame.X / 2, Frame.Y / 2);

    /// <summary>Where the drone hangs in the void before the hero.</summary>
    public static readonly Vec Void = new(672, 292);

    /// <summary>The beam's half-angle (the scan-form's cone), radians.</summary>
    public const double Half = 0.26;

    /// <summary>How dark a picture goes under its diagram; the lamp's, only half.</summary>
    public const double TraceDark = 0.65, LampDark = 0.5;

    /// <summary>The pieces' hit points left of their most: the whole one's row full, the cracked one's 13 of 26.</summary>
    public static readonly (int Left, int Max)[] Reads = [(520, 520), (150, 300)];

    private static readonly Vec PieceA = new(330, 236), PieceB = new(345, 250);

    /// <summary>The middle of the gauge's row over each piece at rest.</summary>
    public static readonly Vec[] Rows = [RowOver(PieceA, Props.Section), RowOver(PieceB, Props.Machine)];

    /// <summary>A lamp's cable from its pivot over the frame, and how far under the cable's end the lamp hangs.</summary>
    private const double Cable = 132, Hang = 30;

    private static readonly Vec Pivot = new(470, -12), LampPlace = new(470, 150), ByLamp = new(560, 204), CratePlace = new(300, 420);
    private static readonly Vec ColdPlace = new(140, 270);
    private static readonly Vec MeteorFar = new(905, 70), MeteorNear = new(893, 79), MeteorPast = new(-150, 390);
    private static readonly Vec MeteorWay = Unit(MeteorNear, MeteorPast), DartTo = Unit(Void, MeteorFar);

    /// <summary>How far the drone darts at the meteor.</summary>
    private const double DartFar = 40;

    /// <summary>The drone's eye with nothing to look at.</summary>
    public static readonly double Rest = Math.Atan2(46, -22);

    /// <summary>The beam from the void toward where the lamp hung, and down to the crate.</summary>
    private static readonly double SweepFromLook = To(Void, LampPlace), SweepToLook = To(Void, CratePlace);

    /// <summary>The vision <paramref name="form"/> plays; none for a form with none (a test holds every lesson's form to have one).</summary>
    public static VisionForm? Of(string form) =>
        form == Content.LightDrone.ScannerMode ? VisionForm.Scan
        : form == Content.LightDrone.LampMode ? VisionForm.Lamp
        : form == Content.LightDrone.AlarmsMode ? VisionForm.Alarms : null;

    /// <summary>How long <paramref name="form"/>'s vision lasts, s: the scanner's 13.8, the lamp's 12.7, the alarms' 13.5.</summary>
    public static double SecondsOf(VisionForm form) => form switch
    {
        VisionForm.Scan => ScanBeats.End.To,
        VisionForm.Lamp => LampBeats.End.To,
        _ => AlarmBeats.End.To,
    };

    /// <summary>How dark the void is <paramref name="u"/> s into <paramref name="form"/>'s, 0..1: in over <see cref="In"/>, whole while shown, out over <see cref="Out"/>.</summary>
    public static double Veil(VisionForm form, double u)
    {
        double back = SecondsOf(form) - Out;
        return u < In ? Sm(u / In) : u > back ? 1 - Sm((u - back) / Out) : 1;
    }

    /// <summary>How much of the HUD shows <paramref name="u"/> s into <paramref name="form"/>'s, 0..1: gone in the first 0.3 s, back in the last.</summary>
    public static double Hud(VisionForm form, double u)
    {
        double back = SecondsOf(form) - HudS;
        return u < HudS ? 1 - Sm(u / HudS) : u > back ? Sm((u - back) / HudS) : 0;
    }

    /// <summary>Whether <paramref name="form"/>'s vision is still on <paramref name="u"/> s in.</summary>
    public static bool Playing(VisionForm form, double u) => u >= 0 && u < SecondsOf(form);

    /// <summary>How the hero floats <paramref name="u"/> s in: risen, world px; its knees and its lean, the void's share of them.</summary>
    public static (double Lift, double Kneel, double Lean) Float(VisionForm form, double u)
    {
        double v = Veil(form, u);
        return (v * (12 - 3 * Math.Sin(u * 0.9)) / PerWorldPx, 0.16 * v, (-0.07 + 0.03 * Math.Sin(u * 0.53)) * v);
    }

    /// <summary>The vision of <paramref name="form"/> <paramref name="u"/> s in.</summary>
    public static VisionFrame At(VisionForm form, double u) => form switch
    {
        VisionForm.Scan => Scan(u),
        VisionForm.Lamp => Lamp(u),
        _ => Alarms(u),
    };

    /// <summary>A thing at rest still floats a little: its <paramref name="k"/>-th way.</summary>
    private static Vec Drift(double u, int k) => new(2 * Math.Sin(u * 0.5 + k), 2 * Math.Sin(u * 0.7 + k * 1.3));

    // ---------- the scanner ----------

    private static VisionFrame Scan(double u)
    {
        double aIn = ScanBeats.AIn.Eased(u), aOut = ScanBeats.AOut.Eased(u), bIn = ScanBeats.BIn.Eased(u);
        Vec fa = Drift(u, 0), fb = Drift(u, 2);
        var a = new PropShown(Props.Section, new Vec(Lerp(-160, PieceA.X, aIn) - 60 * aOut + fa.X, Lerp(190, PieceA.Y, aIn) + 50 * aOut + fa.Y),
            Lerp(-0.3, -0.05, aIn) - 0.1 * aOut, Sm(ScanBeats.AIn.Run(u) / 0.7) * (1 - aOut), ScanBeats.ATrace.Eased(u), false);
        var b = new PropShown(Props.Machine, new Vec(Lerp(190, PieceB.X, bIn) + fb.X, Lerp(650, PieceB.Y, bIn) + fb.Y),
            Lerp(0.25, 0.05, bIn), Sm(ScanBeats.BIn.Run(u) / 0.7), ScanBeats.BTrace.Eased(u), true);
        double look = Aim(u, (ScanBeats.Forms, To(Void, PieceA)), (ScanBeats.BAim, To(Void, PieceB)), (ScanBeats.End, Rest));
        bool formed = u >= ScanBeats.Forms.From && u < ScanBeats.End.From;
        var drone = new VisionDrone(Void, look, formed ? SwarmShape.Scan : SwarmShape.Idle, formed ? ScanBeats.Forms.To - ScanBeats.Forms.From : Out,
            null, 0, u >= ScanBeats.ATrace.From && u < ScanBeats.End.From);
        var cam = Follow(u, Void, 120,
            (ScanBeats.AIn, Lead.Thing, PieceA), (ScanBeats.Forms, Lead.Act, PieceA), (ScanBeats.AOut, Lead.Home, PieceA),
            (ScanBeats.BIn, Lead.Thing, PieceB), (ScanBeats.BAim, Lead.Act, PieceB), (ScanBeats.End, Lead.Home, PieceB));
        return new VisionFrame(VisionForm.Scan, u, Veil(VisionForm.Scan, u), Hud(VisionForm.Scan, u), cam, drone, a, b, null, null, null);
    }

    private static Vec RowOver(Vec at, VisionProp prop) => new(at.X, at.Y - prop.H / 2 - I.DotsAbove);

    /// <summary>Where a gauge's dot comes from or goes to when it is the reticle's mark.</summary>
    public const int Seat = -1;

    /// <summary>The gauge's four flights: when the first dot leaves, how long after each next does, a share of lag by the dot's hash, how long one flies, between what.</summary>
    private static readonly (double At, double Each, double Lag, double Fly, int From, int To)[] Flights =
    [
        (ScanBeats.ADots.From, EachOf(ScanBeats.ADots), 0, DotFly, Seat, 0),
        (ScanBeats.AOut.From, 0, 0.25, 0.45, 0, Seat),
        (ScanBeats.BDots.From, EachOf(ScanBeats.BDots), 0, DotFly, Seat, 1),
        (ScanBeats.End.From, 0, 0.2, 0.4, 1, Seat),
    ];

    /// <summary>How long a dot flies out to a row, s: one by one, the last there as its beat ends.</summary>
    private const double DotFly = 0.3;

    private static double EachOf(Beat beat) => (beat.To - beat.From - DotFly) / (I.Dots - 1);

    /// <summary>Gauge dot <paramref name="j"/> <paramref name="u"/> s into the scanner's vision; none while it sits in its mark, a spark of the drone's.</summary>
    public static VisionDot? DotAt(int j, double u)
    {
        VisionDot? now = null;
        foreach (var (at, each, lag, fly, from, to) in Flights)
        {
            double leaves = at + j * each + DroneSwarm.Hash(j + 2) * lag;
            if (u < leaves) continue;
            double q = Math.Min(1, (u - leaves) / fly), e = Sm(q);
            if (to == Seat) now = q >= 1 ? null : new VisionDot(from, to, e, 1 - e, LitOf(from, j) * (1 - e));
            else now = new VisionDot(from, to, e, e, LitOf(to, j) * e);
        }
        return now;
    }

    /// <summary>How lit piece <paramref name="piece"/>'s dot <paramref name="j"/> is, each a 26th of its most.</summary>
    private static double LitOf(int piece, int j) => Math.Clamp(Reads[piece].Left * (double)I.Dots / Reads[piece].Max - j, 0, 1);

    // ---------- the lamp ----------

    private static VisionFrame Lamp(double u)
    {
        double sway = 0.03 * Math.Sin(u * 1.1);
        var top = new Vec(Pivot.X - Math.Sin(sway) * Cable, Pivot.Y + Math.Cos(sway) * Cable);
        var lamp = new PropShown(Props.Lamp, new Vec(top.X - Math.Sin(sway) * Hang, top.Y + Math.Cos(sway) * Hang), sway,
            LampBeats.LIn.Eased(u) * (1 - LampBeats.LOut.Eased(u)), LampBeats.LTrace.Eased(u), false);
        var f = Drift(u, 4);
        var crate = new PropShown(Props.Crate, new Vec(CratePlace.X + f.X, CratePlace.Y + f.Y), 0.12, 1, LampBeats.CTrace.Eased(u), false);
        var at = Lerp(Void, ByLamp, LampBeats.Go.Eased(u) * (1 - LampBeats.LOut.Eased(u)));
        double end = LampBeats.End.From;
        double beam = Aim(u, (LampBeats.Go, To(ByLamp, LampPlace)), (LampBeats.LOut, SweepFromLook), (LampBeats.Sweep, SweepToLook));
        double look = AngLerp(beam, Rest, new Beat(end + LightOffS, LampBeats.End.To).Eased(u));
        double light = LampBeats.LightOn.Eased(u) * (1 - Sm((u - end) / LightOffS));
        double inCone = Sm(1 - AngDiff(look, To(at, CratePlace)) / Half);
        var dish = LampBeats.Dish;
        var shown = new VisionLamp(Pivot, top, Placed(lamp, Props.Lamp.Bulb), light, inCone, Bump(u, dish.From, dish.From + 0.15, dish.To), dish.Run(u));
        bool formed = u >= dish.From && u < end + DishOffS;
        var drone = new VisionDrone(at, look, formed ? SwarmShape.Lamp : SwarmShape.Idle, formed ? dish.To - dish.From : DishOffS, null, 0, false);
        double reach = Dist(Void, CratePlace);
        // The frame follows the beam, not the eye turning home after the light is out: the glide back would jump.
        var beamTip = new Vec(at.X + Math.Cos(beam) * reach, at.Y + Math.Sin(beam) * reach);
        var cam = Follow(u, at, 70,
            (LampBeats.LIn, Lead.Thing, LampPlace), (LampBeats.Go, Lead.Act, LampPlace), (LampBeats.LOut, Lead.Drone, LampPlace),
            (new Beat(LampBeats.LightOn.From, LampBeats.Sweep.To), Lead.Beam, beamTip), (LampBeats.End, Lead.Home, CratePlace));
        return new VisionFrame(VisionForm.Lamp, u, Veil(VisionForm.Lamp, u), Hud(VisionForm.Lamp, u), cam, drone, lamp, crate, shown, null, null);
    }

    /// <summary>The light going out as the world returns, and then the dish, s.</summary>
    private const double LightOffS = 0.25, DishOffS = 0.3;

    // ---------- the alarms ----------

    /// <summary>The two dangers in order: each glyph's start, coming in, held till, and going out.</summary>
    private static readonly (DroneThought Glyph, double From, double ComeIn, double Until, double GoOut)[] Glyphs =
    [
        (DroneThought.Cold, AlarmBeats.CGlyph.From, 0.6, AlarmBeats.COut.From, 0.6),
        (DroneThought.Meteor, AlarmBeats.Dart.From, 0.6, AlarmBeats.Pass.From, 1),
    ];

    /// <summary>From this much of its pass on the meteor fades, out of the pick's frame by then: a wider screen sees it go, not vanish.</summary>
    private const double MeteorFades = 0.94;

    private static VisionFrame Alarms(double u)
    {
        DroneThought? glyph = null;
        double red = 0, over = 0.6;
        foreach (var (kind, from, comeIn, until, goOut) in Glyphs)
        {
            if (u < from || u >= until + goOut) continue;
            bool held = u < until;
            double f = u < from + comeIn ? Sm((u - from) / comeIn) : held ? 1 : 1 - Sm((u - until) / goOut);
            if (held) glyph = kind;
            over = held ? comeIn : goOut;
            red = f * (I.AlarmLow + I.AlarmSwing * Math.Sin(u * Math.Tau * I.AlarmBeats));
            break;
        }
        double dart = AlarmBeats.Dart.Eased(u) * (1 - Sm(AlarmBeats.Pass.Run(u) / 0.6));
        var at = new Vec(Void.X + DartTo.X * DartFar * dart, Void.Y + DartTo.Y * DartFar * dart);
        double look = Aim(u, (AlarmBeats.CGlyph, Math.PI), (AlarmBeats.COut, Rest), (AlarmBeats.Dart, To(Void, MeteorFar)), (AlarmBeats.Pass, Rest));
        var drone = new VisionDrone(at, look, glyph != null ? SwarmShape.Thought : SwarmShape.Idle, over, glyph, red, false);

        var frost = new VisionFrost(Lerp(-10, 260, AlarmBeats.CIn.Eased(u)) - 270 * AlarmBeats.COut.Eased(u),
            Sm(AlarmBeats.CIn.Run(u) * 2.5) * (1 - Sm((AlarmBeats.COut.Run(u) - 0.4) / 0.6)), AlarmBeats.CTrace.Eased(u));

        // A glint far off creeping nearer, then it passes.
        VisionMeteor? meteor = null;
        if (u >= AlarmBeats.MIn.From && u < AlarmBeats.Pass.To)
        {
            double c = Math.Clamp((u - AlarmBeats.MIn.From) / (AlarmBeats.Pass.From - AlarmBeats.MIn.From), 0, 1), e = AlarmBeats.Pass.Eased(u);
            meteor = new VisionMeteor(e > 0 ? Lerp(MeteorNear, MeteorPast, e) : Lerp(MeteorFar, MeteorNear, c), MeteorWay,
                Lerp(0.32, 0.4, c) + 0.35 * e, AlarmBeats.MIn.Eased(u) * (1 - Sm((e - MeteorFades) / (1 - MeteorFades))), u * 0.8, AlarmBeats.MTrace.Eased(u));
        }

        var cam = Follow(u, at, 190,
            (AlarmBeats.CIn, Lead.Thing, ColdPlace), (AlarmBeats.CGlyph, Lead.Act, ColdPlace), (AlarmBeats.COut, Lead.Home, ColdPlace),
            (AlarmBeats.MIn, Lead.Thing, MeteorFar), (AlarmBeats.Dart, Lead.Meteor, MeteorFar), (AlarmBeats.Pass, Lead.Home, MeteorFar));
        return new VisionFrame(VisionForm.Alarms, u, Veil(VisionForm.Alarms, u), Hud(VisionForm.Alarms, u), cam, drone, null, null, null, frost, meteor);
    }

    // ---------- the frame that follows («Кадр следует») ----------

    /// <summary>What the view frames: the whole; a thing alone; a thing and the drone acting on it, the beam's end or the meteor too; the drone.</summary>
    private enum Lead { Home, Thing, Act, Beam, Meteor, Drone }

    /// <summary>How close the view comes to a thing, to the drone alone.</summary>
    private const double ThingZoom = 1.3, DroneZoom = 1.15;

    /// <summary>The share of the frame a thing and the drone are fitted into; how much room the beam's end and the meteor take, pick px round them.</summary>
    private const double Fitted = 0.8, SmallRadius = 80;

    /// <summary>
    /// The view <paramref name="u"/> s in: eased from key to key inside beats that move anyway, the drone at <paramref name="drone"/>, a thing
    /// <paramref name="radius"/> px round; kept in the pick's frame.
    /// </summary>
    private static VisionCam Follow(double u, Vec drone, double radius, params (Beat In, Lead Lead, Vec At)[] keys)
    {
        var (middle, zoom) = (Middle, 1.0);
        foreach (var (beat, lead, at) in keys)
        {
            if (u < beat.From) break;
            var (to, close) = (Middle, 1.0);
            if (lead == Lead.Thing) (to, close) = (at, ThingZoom);
            else if (lead == Lead.Drone) (to, close) = (drone, DroneZoom);
            else if (lead is Lead.Act or Lead.Beam or Lead.Meteor)
            {
                double round = 2 * (lead == Lead.Act ? radius : SmallRadius);
                to = Lerp(at, drone, 0.5);
                close = Math.Clamp(Math.Min(Frame.X * Fitted / (Math.Abs(at.X - drone.X) + round), Frame.Y * Fitted / (Math.Abs(at.Y - drone.Y) + round)), 1, ThingZoom);
            }
            double e = beat.Eased(u);
            (middle, zoom) = (Lerp(middle, to, e), Lerp(zoom, close, e));
        }
        double halfW = Frame.X / 2 / zoom, halfH = Frame.Y / 2 / zoom;
        // At rest the halves are the whole: Min and Max rather than Clamp, whose bounds may cross by a rounding.
        return new VisionCam(new Vec(Math.Min(Math.Max(middle.X, halfW), Frame.X - halfW), Math.Min(Math.Max(middle.Y, halfH), Frame.Y - halfH)), zoom);
    }

    /// <summary>The cold's crystals: where each sits in the pick's frame, its size, turn and twinkle (the page's 46, by the kit's rng).</summary>
    public static readonly (double X, double Y, double Size, double Turn, double Twinkle)[] Crystals = CrystalsOf();

    private static (double, double, double, double, double)[] CrystalsOf()
    {
        var r = new KitRng(321);
        var all = new (double, double, double, double, double)[46];
        for (int i = 0; i < all.Length; i++) all[i] = (4 + Math.Pow(r.Next(), 1.4) * 280, 10 + r.Next() * 520, 4 + r.Next() * 7, r.Next() * Math.Tau, r.Next() * Math.Tau);
        return all;
    }

    /// <summary>The void's stars over the pick's frame: where, their side, how bright (the page's 140).</summary>
    public static readonly (double X, double Y, double Side, double Alpha)[] Stars = StarsOf();

    private static (double, double, double, double)[] StarsOf()
    {
        var r = new KitRng(77);
        var all = new (double, double, double, double)[140];
        for (int i = 0; i < all.Length; i++)
        {
            double b = r.Next();
            all[i] = (r.Next() * Frame.X, r.Next() * Frame.Y, b < 0.08 ? 1.5 : 1, 0.1 + r.Next() * 0.3);
        }
        return all;
    }

    /// <summary>The front of the cold at height <paramref name="y"/>: wavy, pick px from the edge.</summary>
    public static double EdgeX(double x, double y) => x + 16 * Math.Sin(y * 0.045 + 1.3) + 9 * Math.Sin(y * 0.13);

    // ---------- the props, the game's pictures, their lines traced from them (picture px) ----------

    /// <summary>A point of <paramref name="q"/>'s picture, picture px, where it is shown, pick px.</summary>
    public static Vec Placed(PropShown q, Vec v)
    {
        double x = (v.X - q.Prop.W / 2) * q.Prop.Scale, y = (v.Y - q.Prop.H / 2) * q.Prop.Scale, c = Math.Cos(q.Angle), s = Math.Sin(q.Angle);
        return new Vec(q.At.X + x * c - y * s, q.At.Y + x * s + y * c);
    }

    /// <summary>The props the visions show.</summary>
    public static class Props
    {
        public static readonly VisionProp Section = new("debris/section_01", 192, 128, 1,
            P(93.5, .5, 151.5, 1.5, 153.5, 14.5, 145.5, 24.5, 155.5, 38.5, 155.5, 50.5, 151.5, 53.5, 155.5, 55.5, 155.5, 67.5, 153.5, 69.5, 155.5, 72.5, 155.5, 106.5, 152.5, 119.5, 148.5, 121.5, 101.5, 121.5, 97.5, 123.5, 81.5, 121.5, 73.5, 123.5, 67.5, 121.5, 12.5, 123.5, 3.5, 112.5, 2.5, 104.5, 5.5, 91.5, 2.5, 75.5, 4.5, 74.5, 4.5, 7.5, 15.5, 1.5, 93.5, .5),
            [Circ(29, 37, 25, 28), Circ(29, 37, 17, 22), P(60, 5, 60, 76), P(111, 5, 111, 76), P(60, 43, 148, 43), P(4, 78, 156, 78), P(4, 92, 156, 92), P(80, 95, 80, 120), Box(122, 13, 137, 28),
                P(155, 67, 168, 70, 183, 83), P(155, 72, 166, 82, 174, 104), P(156, 84, 171, 90, 188, 103)],
            []);

        public static readonly VisionProp Machine = new("debris/machine_01", 192, 128, 1,
            P(39.5, .5, 57.5, 4.5, 58.5, 12.5, 85.5, 13.5, 99.5, 11.5, 104.5, 20.5, 102.5, 22.5, 103.5, 24.5, 108.5, 22.5, 111.5, 10.5, 123.5, 16.5, 132.5, 6.5, 140.5, 18.5, 153.5, 15.5, 153.5, 28.5, 161.5, 34.5, 166.5, 34.5, 157.5, 44.5, 166.5, 57.5, 153.5, 63.5, 154.5, 76.5, 141.5, 73.5, 133.5, 77.5, 124.5, 76.5, 117.5, 80.5, 151.5, 81.5, 153.5, 91.5, 174.5, 91.5, 176.5, 84.5, 189.5, 85.5, 189.5, 106.5, 175.5, 108.5, 174.5, 100.5, 152.5, 100.5, 151.5, 113.5, 113.5, 113.5, 99.5, 126.5, 13.5, 125.5, 3.5, 116.5, 3.5, 108.5, 5.5, 106.5, 5.5, 99.5, 3.5, 98.5, 3.5, 70.5, 5.5, 70.5, 3.5, 59.5, 5.5, 50.5, 3.5, 22.5, 12.5, 13.5, 35.5, 13.5, 39.5, .5),
            [P(4, 67, 103, 67), Box(21, 24, 66, 60), .. new[] { 30, 37, 43, 49, 55 }.Select(y => P(24, y, 63, y)), Circ(125, 43, 13, 20), Circ(125, 43, 4, 10),
                Box(110, 84, 152, 113), .. new[] { 118, 127, 136, 145 }.Select(x => P(x, 87, x, 110)), P(152, 94, 176, 94), P(152, 99, 176, 99), .. new[] { 16, 34, 52 }.Select(x => P(x, 105, x + 9, 90))],
            // The cracked one's network out of its blow, as a wreck's cracks are drawn (WreckCracks).
            [P(70, 82, 60, 76, 52, 74, 44, 69), P(70, 82, 80, 88, 90, 86, 99, 93), P(70, 82, 66, 94, 72, 104, 68, 116), P(60, 76, 61, 70), P(80, 88, 83, 97), P(132, 50, 141, 58, 147, 57)], Blow: new Vec(70, 82));

        public static readonly VisionProp Lamp = new("machine_lamp", 64, 64, 1,
            P(17.5, 1.5, 46.5, 1.5, 45.5, 8.5, 35.5, 9.5, 35.5, 14.5, 41.5, 15.5, 41.5, 22.5, 46.5, 30.5, 45.5, 41.5, 41.5, 46.5, 35.5, 49.5, 28.5, 49.5, 22.5, 46.5, 17.5, 39.5, 17.5, 30.5, 19.5, 25.5, 22.5, 22.5, 22.5, 15.5, 29.5, 14.5, 28.5, 9.5, 17.5, 9.5, 17.5, 1.5),
            [P(18, 9, 46, 9), Box(23, 15, 41, 22), Circ(32, 35, 11, 18), Arc(32, 35, 7, 3.6, 4.9, 6)],
            [], new Vec(32, 35));

        public static readonly VisionProp Crate = new("debris/crate_03", 68, 68, 1,
            P(3.5, 1.5, 6.5, 3.5, 12.5, 3.5, 12.5, 1.5, 66.5, 2.5, 65.5, 66.5, 1.5, 65.5, 1.5, 3.5, 3.5, 1.5),
            [P(2, 13, 66, 13), P(2, 20, 66, 20), P(2, 33, 66, 33), P(2, 46, 66, 46), P(2, 52, 66, 52)],
            []);

        public static readonly VisionProp Rock = new("debris/smallrock_03", 96, 96, 0.55,
            P(57.5, 4.5, 62.5, 4.5, 76.5, 13.5, 82.5, 28.5, 85.5, 49.5, 74.5, 67.5, 56.5, 82.5, 38.5, 85.5, 33.5, 84.5, 16.5, 75.5, 5.5, 57.5, 8.5, 33.5, 42.5, 7.5, 57.5, 4.5),
            [P(42, 8, 50, 40, 83, 30), P(50, 40, 38, 84), Circ(30, 62, 6, 10)],
            []);

        /// <summary>Every prop, for loading their pictures once.</summary>
        public static readonly VisionProp[] All = [Section, Machine, Lamp, Crate, Rock];

        private static Vec[] P(params double[] xy)
        {
            var points = new Vec[xy.Length / 2];
            for (int i = 0; i < points.Length; i++) points[i] = new Vec(xy[2 * i], xy[2 * i + 1]);
            return points;
        }

        private static Vec[] Arc(double cx, double cy, double r, double a0, double a1, int n)
        {
            var points = new Vec[n + 1];
            for (int i = 0; i <= n; i++)
            {
                double a = a0 + (a1 - a0) * i / n;
                points[i] = new Vec(cx + Math.Cos(a) * r, cy + Math.Sin(a) * r);
            }
            return points;
        }

        private static Vec[] Circ(double cx, double cy, double r, int n) => Arc(cx, cy, r, 0, Math.Tau, n);

        private static Vec[] Box(double x0, double y0, double x1, double y1) => P(x0, y0, x1, y0, x1, y1, x0, y1, x0, y0);
    }

    // ---------- the diagram's lines ----------

    /// <summary>
    /// <paramref name="lines"/> up to share <paramref name="f"/> of their whole length, in order, into <paramref name="into"/>: a diagram drawing
    /// itself. Each line kept is its points, the last cut where the share ends.
    /// </summary>
    public static void Cut(IReadOnlyList<Vec[]> lines, double f, List<Vec[]> into)
    {
        into.Clear();
        if (f <= 0) return;
        double whole = 0;
        foreach (var line in lines) whole += LengthOf(line);
        double left = Math.Min(1, f) * whole;
        foreach (var line in lines)
        {
            if (left <= 1e-6) return;
            double length = LengthOf(line);
            if (length <= left)
            {
                into.Add(line);
                left -= length;
                continue;
            }
            var part = new List<Vec> { line[0] };
            for (int i = 1; i < line.Length; i++)
            {
                double d = Dist(line[i - 1], line[i]);
                if (d >= left)
                {
                    part.Add(Lerp(line[i - 1], line[i], left / (d > 0 ? d : 1)));
                    break;
                }
                part.Add(line[i]);
                left -= d;
            }
            into.Add([.. part]);
            return;
        }
    }

    /// <summary>A polyline's length.</summary>
    public static double LengthOf(Vec[] line)
    {
        double length = 0;
        for (int i = 1; i < line.Length; i++) length += Dist(line[i - 1], line[i]);
        return length;
    }

    /// <summary>The point <paramref name="s"/> of the way round <paramref name="line"/> by its length, and its heading.</summary>
    public static (Vec At, Vec Heading) OnLine(Vec[] line, double length, double s)
    {
        double d = (s % 1 + 1) % 1 * length;
        for (int i = 1; i < line.Length; i++)
        {
            double step = Dist(line[i - 1], line[i]);
            if (step < 1e-6) continue;
            if (d <= step || i == line.Length - 1)
            {
                var heading = new Vec((line[i].X - line[i - 1].X) / step, (line[i].Y - line[i - 1].Y) / step);
                return (Lerp(line[i - 1], line[i], Math.Clamp(d / step, 0, 1)), heading);
            }
            d -= step;
        }
        return (line[0], new Vec(1, 0));
    }

    // ---------- the page's helpers ----------

    /// <summary>The eye's way from its rest through keyed looks, each eased in over its beat from where the one before had it.</summary>
    private static double Aim(double u, params (Beat In, double Look)[] keys)
    {
        double a = Rest;
        foreach (var (beat, look) in keys)
        {
            if (u < beat.From) break;
            a = AngLerp(a, look, beat.Eased(u));
        }
        return a;
    }

    /// <summary>Up from <paramref name="a"/> to the top at <paramref name="b"/> and down by <paramref name="d"/>, smoothly.</summary>
    private static double Bump(double u, double a, double b, double d) => Sm((u - a) / (b - a)) * (1 - Sm((u - b) / (d - b)));

    public static double To(Vec from, Vec to) => Math.Atan2(to.Y - from.Y, to.X - from.X);

    public static double AngLerp(double a, double b, double e)
    {
        double d = b - a;
        while (d > Math.PI) d -= Math.Tau;
        while (d < -Math.PI) d += Math.Tau;
        return a + d * e;
    }

    public static double AngDiff(double a, double b)
    {
        double d = a - b;
        while (d > Math.PI) d -= Math.Tau;
        while (d < -Math.PI) d += Math.Tau;
        return Math.Abs(d);
    }

    public static double Sm(double u)
    {
        u = Math.Clamp(u, 0, 1);
        return u * u * (3 - 2 * u);
    }

    private static double Lerp(double a, double b, double k) => a + (b - a) * k;

    public static Vec Lerp(Vec a, Vec b, double k) => new(a.X + (b.X - a.X) * k, a.Y + (b.Y - a.Y) * k);

    private static Vec Unit(Vec from, Vec to)
    {
        double d = Dist(from, to);
        return new Vec((to.X - from.X) / d, (to.Y - from.Y) / d);
    }

    private static double Dist(Vec a, Vec b) => Math.Sqrt((a.X - b.X) * (a.X - b.X) + (a.Y - b.Y) * (a.Y - b.Y));

    /// <summary>The kit's <c>rng</c>: an xorshift over a seed, so the page's crystals and stars fall where they did.</summary>
    private sealed class KitRng(uint seed)
    {
        private uint _s = unchecked(seed * 2654435761u) is var s && s != 0 ? s : 1;

        public double Next()
        {
            _s ^= _s << 13;
            _s ^= _s >> 17;
            _s ^= _s << 5;
            return _s / 4294967296.0;
        }
    }
}
