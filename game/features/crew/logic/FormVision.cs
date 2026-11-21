using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Crew;

/// <summary>Which vision a drone's form plays (form-vision): what it shows the hero the form does.</summary>
public enum VisionForm { Scan, Lamp, Alarms }

/// <summary>
/// A thing shown in the void (form-vision): the game's own picture (<c>res://art/hd/{Picture}.png</c>), <see cref="W"/> by <see cref="H"/> of
/// its pixels drawn <see cref="Scale"/> times, and the lines its diagram is traced in, picture px: its outline traced from the picture's alpha,
/// its main lines placed by hand, a cracked one's cracks, a lamp's bulb.
/// </summary>
public sealed record VisionProp(string Picture, double W, double H, double Scale, Vec[] Outline, Vec[][] Lines, Vec[][] Cracks, Vec Bulb = default);

/// <summary>
/// A prop as shown: where its middle is and how it is turned (pick px, radians), how far it has come out of the dark, 0..1, how far its
/// diagram has run over it, 0..1 («Проступает схема»: the picture darkens as it does), and whether its cracks show.
/// </summary>
public readonly record struct PropShown(VisionProp Prop, Vec At, double Angle, double Alpha, double Trace, bool Cracked);

/// <summary>
/// The vision's drone: where its eye looks (pick px angle), the shape its sparks form, the thought's picture in <see cref="SwarmShape.Thought"/>,
/// how red an alarm turns it, 0..1, and how far it darts off its place, pick px.
/// </summary>
public readonly record struct VisionDrone(double Look, SwarmShape Shape, DroneThought? Thought, double Red, Vec Dart);

/// <summary>What the scanner reads in the vision: piece <see cref="Piece"/>'s hit points left of the most, the middle of its top, pick px.</summary>
public readonly record struct VisionRead(int Piece, int Left, int Max, Vec Top);

/// <summary>
/// The lamp's vision: the cable from its pivot to the lamp's top, the bulb, the beam's way and how lit it is, how much of the crate it covers, how much
/// of the crate's diagram has been seen (it stays), how the drone studies the lamp (a thread from its eye), and the lamp's diagram run, 0..1.
/// </summary>
public readonly record struct VisionLamp(Vec Pivot, Vec Top, Vec Bulb, double Beam, double Light, double InCone, double Seen, double Study, double Trace);

/// <summary>The cold creeping in from the screen's edge the hero faces: its front, pick px from that edge; how much shows; its front's diagram run.</summary>
public readonly record struct VisionFrost(double Front, double Alpha, double Trace);

/// <summary>The meteor crossing: how far along its path (0 entering the pick's frame, 1 leaving it), its spin, its diagram run.</summary>
public readonly record struct VisionMeteor(double Along, double Spin, double Trace);

/// <summary>
/// The vision at a moment (form-vision): how dark the void is (<see cref="Veil"/>) and how much HUD shows, the drone, and what the form's scene
/// holds now. Pick px (the page's, four to a world px), the hero's feet at <see cref="FormVision.Feet"/>, facing left: the view mirrors it for a
/// hero facing right.
/// </summary>
public readonly record struct VisionFrame(VisionForm Form, double U, double Veil, double Hud, VisionDrone Drone, PropShown? First, PropShown? Second,
    VisionRead? Read, VisionLamp? Lamp, VisionFrost? Frost, VisionMeteor? Meteor);

/// <summary>
/// The vision of a form the light drone learned (form-vision, the pick «Проступает схема»): for 5 s the world goes, the hero floats held, and the
/// drone before him shows what the form does. Its frame is shared: 0.6 s in, 3.8 shown, 0.6 out; each form's scene is a method over it. The scanner:
/// two wreck pieces drift in, the reticle reads the whole one, a full row, then hops to the cracked one, half a row. The lamp: a lamp hangs in the
/// dark, the drone copies its dish and its beam finds a crate no one could see. The alarms: cold creeps from an edge, a snowflake; a meteor crosses,
/// the drone darts red with its glyph. Props are the game's pictures until the drone takes them: a thread of sparks runs over each, the picture
/// darkens to about 35% and its diagram stays. The page's numbers (tools/pick-kit/pages/form-vision.js), pick px and seconds; client only.
/// </summary>
public static class FormVision
{
    /// <summary>The vision's frame, s: coming in, the whole of it, going out.</summary>
    public const double In = 0.6, Length = 5, Out = 0.6;

    /// <summary>Pick px a world px (the kit's zoom).</summary>
    public const double PerWorldPx = 4;

    /// <summary>Where the hero's feet are in the pick's frame, and the pick's frame's size.</summary>
    public static readonly Vec Feet = new(780, 470), Frame = new(1000, 540);

    /// <summary>Where the drone hangs in the void before the hero.</summary>
    public static readonly Vec Void = new(672, 292);

    /// <summary>The beam's half-angle (the scan-form's cone), radians.</summary>
    public const double Half = 0.26;

    /// <summary>How long a prop's diagram takes to run over it, s, and how dark its picture goes under it.</summary>
    public const double TraceS = 0.7, TraceDark = 0.65;

    /// <summary>The cracked piece's hit points left of its most, and the whole one's: its row half lit, the other full.</summary>
    public static readonly (int Left, int Max)[] Reads = [(520, 520), (150, 300)];

    /// <summary>The scanner's beats, s: the reticle forms, reads the first, hops to the second, its dots home, it lets go.</summary>
    public const double ScanForm = 1.0, ScanRead = 1.75, ScanHop = 2.75, ScanHome = 3.6, ScanLet = 4.15;

    /// <summary>The lamp's beats, s: the dish forms, the light on, the sweep from the lamp to the crate, the light off, it lets go.</summary>
    public const double LampAt = 1.3, LightOn = 1.9, SweepFrom = 2.0, SweepTo = 3.0, LightOff = 4.0, LampLet = 4.15;

    /// <summary>The meteor crossing the pick's frame, s: from, for.</summary>
    public const double MeteorFrom = 2.6, MeteorFor = 0.8;

    /// <summary>The meteor's path across the pick's frame.</summary>
    public static readonly Vec MeteorIn = new(1060, 20), MeteorOutAt = new(-80, 300);

    /// <summary>The alarms' two dangers in order: each glyph's start, coming in, held and going, s.</summary>
    public static readonly (DroneThought Glyph, double From, double ComeIn, double Hold, double GoOut)[] Alarms =
        [(DroneThought.Cold, 1.0, 0.4, 0.9, 0.3), (DroneThought.Meteor, 2.6, 0.4, 0.9, 0.3)];

    /// <summary>The drone's eye with nothing to look at.</summary>
    public static readonly double Rest = Math.Atan2(46, -22);

    /// <summary>The vision <paramref name="form"/> plays; none for a form with none (a test holds every lesson's form to have one).</summary>
    public static VisionForm? Of(string form) =>
        form == Content.LightDrone.ScannerMode ? VisionForm.Scan
        : form == Content.LightDrone.LampMode ? VisionForm.Lamp
        : form == Content.LightDrone.AlarmsMode ? VisionForm.Alarms : null;

    /// <summary>How dark the void is <paramref name="u"/> s in, 0..1: in over <see cref="In"/>, whole while shown, out over <see cref="Out"/>.</summary>
    public static double Veil(double u) => u < In ? Sm(u / In) : u > Length - Out ? 1 - Sm((u - (Length - Out)) / Out) : 1;

    /// <summary>How much of the HUD shows <paramref name="u"/> s in, 0..1: gone in the first 0.3 s, back in the last.</summary>
    public static double Hud(double u) => u < 0.3 ? 1 - Sm(u / 0.3) : u > Length - 0.3 ? Sm((u - (Length - 0.3)) / 0.3) : 0;

    /// <summary>Whether the vision is still on <paramref name="u"/> s in.</summary>
    public static bool Playing(double u) => u >= 0 && u < Length;

    /// <summary>How the hero floats <paramref name="u"/> s in: risen, world px; its knees and its lean, the void's share of them.</summary>
    public static (double Lift, double Kneel, double Lean) Float(double u)
    {
        double v = Veil(u);
        return (v * (12 - 3 * Math.Sin(u * 0.9)) / PerWorldPx, 0.16 * v, (-0.07 + 0.03 * Math.Sin(u * 0.53)) * v);
    }

    /// <summary>The vision of <paramref name="form"/> <paramref name="u"/> s in.</summary>
    public static VisionFrame At(VisionForm form, double u) => form switch
    {
        VisionForm.Scan => Scan(u),
        VisionForm.Lamp => Lamp(u),
        _ => AlarmsAt(u),
    };

    /// <summary>How far a diagram has run over a prop the drone took at <paramref name="from"/> s, <paramref name="u"/> s in.</summary>
    public static double Traced(double u, double from) => Sm((u - from) / TraceS);

    // ---------- the scanner ----------

    private static VisionFrame Scan(double u)
    {
        double eA = Sm((u - 0.2) / 1.1), eB = Sm((u - 0.4) / 1.1);
        var a = new PropShown(Props.Section, new Vec(Lerp(-140, 250, eA) + 4 * Math.Sin(u * 0.5), Lerp(150, 200, eA) + 3 * Math.Sin(u * 0.7)),
            Lerp(-0.3, -0.05, eA), Sm((u - 0.15) / 0.8), Traced(u, ScanRead - 0.15), false);
        var b = new PropShown(Props.Machine, new Vec(Lerp(330, 455, eB) + 3 * Math.Sin(u * 0.6 + 1), Lerp(640, 402, eB) + 3 * Math.Sin(u * 0.8)),
            Lerp(0.25, 0.05, eB), Sm((u - 0.35) / 0.8), Traced(u, ScanHop), true);
        double look = Aim(u, (0, () => Rest), (0.9, () => To(Void, a.At)), (ScanHop, () => To(Void, b.At)), (3.75, () => Rest));
        var shape = u >= ScanForm && u < ScanLet ? SwarmShape.Scan : SwarmShape.Idle;
        int? piece = u >= ScanRead && u < ScanHop ? 0 : u >= ScanHop && u < ScanHome ? 1 : null;
        VisionRead? read = piece is int p ? new VisionRead(p, Reads[p].Left, Reads[p].Max, TopOf(p == 0 ? a : b)) : null;
        return new VisionFrame(VisionForm.Scan, u, Veil(u), Hud(u), new VisionDrone(look, shape, null, 0, default), a, b, read, null, null, null);
    }

    /// <summary>The middle of a prop's top as it stands, pick px: where the scanner's row hangs over.</summary>
    private static Vec TopOf(PropShown q) => new(q.At.X, q.At.Y - q.Prop.H * q.Prop.Scale / 2);

    // ---------- the lamp ----------

    /// <summary>The lamp's cable's pivot, out of the frame's top, and its length; the lamp hangs this far under its end.</summary>
    private static readonly Vec Pivot = new(470, -12);

    private const double Cable = 132, Hang = 30;

    private static VisionFrame Lamp(double u)
    {
        double sway = 0.05 * Math.Sin(u * 1.4);
        var top = new Vec(Pivot.X - Math.Sin(sway) * Cable, Pivot.Y + Math.Cos(sway) * Cable);
        var lamp = new PropShown(Props.Lamp, new Vec(top.X - Math.Sin(sway) * Hang, top.Y + Math.Cos(sway) * Hang), sway, Sm((u - 0.25) / 0.7), Traced(u, 1.0), false);
        var crate = new PropShown(Props.Crate, new Vec(300 + 3 * Math.Sin(u * 0.6), 420 + 2 * Math.Sin(u * 0.9)), 0.12, 1, 0, false);
        double toLamp = To(Void, lamp.At), toCrate = To(Void, crate.At);
        double beam = AngLerp(toLamp, toCrate, Sm((u - SweepFrom) / (SweepTo - SweepFrom)));
        double light = Sm((u - LightOn) / 0.15) * (1 - Sm((u - LightOff) / 0.15));
        double inCone = Sm(1 - AngDiff(beam, toCrate) / Half);
        // The sweep closes in on the crate and rests there: its diagram, once seen, stays.
        double seen = u >= SweepTo ? 1 : u < LightOn ? 0 : inCone;
        double look = Aim(u, (0, () => Rest), (0.9, () => toLamp), (LightOn, () => beam));
        var shape = u >= LampAt && u < LampLet ? SwarmShape.Lamp : SwarmShape.Idle;
        var shown = new VisionLamp(Pivot, top, Placed(lamp, Props.Lamp.Bulb), beam, light, inCone, seen, Bump(u, 1.0, 1.15, 1.6), lamp.Trace);
        return new VisionFrame(VisionForm.Lamp, u, Veil(u), Hud(u), new VisionDrone(look, shape, null, 0, default), lamp, crate with { Trace = seen }, null, shown, null, null);
    }

    // ---------- the alarms ----------

    private static VisionFrame AlarmsAt(double u)
    {
        DroneThought? glyph = null;
        double red = 0;
        foreach (var (kind, from, comeIn, hold, goOut) in Alarms)
        {
            double held = from + comeIn + hold;
            if (u < from || u >= held + goOut) continue;
            double f = u < from + comeIn ? Sm((u - from) / comeIn) : u < held ? 1 : 1 - Sm((u - held) / goOut);
            // The glyph while it is held; its red fading after (DroneInk's alarm swing).
            if (u < held) glyph = kind;
            red = f * (0.6 + 0.4 * Math.Sin(u * Math.Tau * 2.4));
            break;
        }
        double dart = Bump(u, 2.6, 2.68, 3.05);
        var meteorAt = () => To(Void, Lerp(MeteorIn, MeteorOutAt, Math.Clamp((u - MeteorFrom) / MeteorFor, 0, 1)));
        double look = Aim(u, (0, () => Rest), (0.8, () => Math.PI), (2.5, meteorAt), (3.5, () => Rest));
        var drone = new VisionDrone(look, glyph != null ? SwarmShape.Thought : SwarmShape.Idle, glyph, red, new Vec(9 * dart, 10 * dart));
        var frost = new VisionFrost(Lerp(-10, 300, Sm((u - 0.7) / 1.5)), Sm((u - 0.6) / 0.3) * (1 - Sm((u - 2.8) / 0.7)), Traced(u, 1.0));
        // Entering a little before the pick's frame and leaving after, so it crosses a wider screen whole.
        double along = (u - MeteorFrom) / MeteorFor;
        VisionMeteor? meteor = along >= -MeteorBeyond && along <= 1 + MeteorBeyond ? new VisionMeteor(along, u * 5, Traced(u, 2.65)) : null;
        return new VisionFrame(VisionForm.Alarms, u, Veil(u), Hud(u), drone, null, null, null, null, frost, meteor);
    }

    /// <summary>How far past the pick's frame the meteor flies on either side, shares of its crossing.</summary>
    public const double MeteorBeyond = 0.45;

    /// <summary>The meteor's head <paramref name="along"/> its path.</summary>
    public static Vec MeteorHead(double along) => Lerp(MeteorIn, MeteorOutAt, along);

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
            [P(70, 82, 60, 76, 52, 74, 44, 69), P(70, 82, 80, 88, 90, 86, 99, 93), P(70, 82, 66, 94, 72, 104, 68, 116), P(60, 76, 61, 70), P(80, 88, 83, 97), P(132, 50, 141, 58, 147, 57)]);

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

    /// <summary>The eye's way through keyed looks: each eased in over 0.3 s from where the one before had it.</summary>
    private static double Aim(double u, params (double From, Func<double> Look)[] keys)
    {
        double a = keys[0].Look();
        for (int i = 1; i < keys.Length && u >= keys[i].From; i++) a = AngLerp(a, keys[i].Look(), Sm((u - keys[i].From) / 0.3));
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
