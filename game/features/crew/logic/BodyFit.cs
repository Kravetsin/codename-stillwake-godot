using System;
using System.Collections.Generic;
using System.Linq;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Crew;

/// <summary>How a helmet answers its hero looking up and down (helmets-fit-the-suit): each its own way, by its shape.</summary>
public enum HelmetTurn
{
    /// <summary>
    /// Soft: its top turns with the head, its base stays on the shoulders, the band between bends (<see cref="BentLimb"/>): a hood's
    /// cloth, the basic helmet's rubber seal.
    /// </summary>
    Soft,
    /// <summary>Bolted to the suit: it never turns; the face inside does, and the lamp's beam.</summary>
    Bolted,
    /// <summary>A ball turning on a neck ring that stays with the torso, as on a bearing: the basic helmet's first way, rejected in play.</summary>
    Ring,
    /// <summary>Bolted, the face behind its glass still too; only the lamp's beam looks: tried for the bell, rejected in play.</summary>
    BoltedStill,
}

/// <summary>
/// How a helmet sits on the suit (helmets-fit-the-suit): how it <paramref name="Turn"/>s, about the pivot
/// (<paramref name="PivotX"/>, <paramref name="PivotY"/>), and from which row its shell is the collar (<paramref name="Cut"/>),
/// drawn over the near arm's shoulder while the rest stays under the arm. All in the helmet picture's pixels, its 13 × 12 box,
/// the neck at <see cref="BodyFit.Neck"/>. With no cut, the collar is a picture of its own (<c>hero_&lt;helmet&gt;_collar</c>).
/// Its shell's bottom fades out into the suit over the rows <paramref name="Fade"/>, whole at the first, none at the second, if given.
/// A soft one bends in a <paramref name="Band"/> that tall round its pivot: what is above it turns whole, what is below stays. A
/// <paramref name="Sealed"/> one has a rubber seal of its own between shell and collar (<c>hero_&lt;helmet&gt;_seal</c>), bent in
/// that band as its shell turns: the bell's gasket.
/// </summary>
public sealed record HelmetFit(
    HelmetTurn Turn, double PivotX, double PivotY, double? Cut, (double Whole, double None)? Fade = null, double Band = BentLimb.Band, bool Sealed = false)
{
    /// <summary>
    /// The angles its parts are turned by as the head looks <paramref name="look"/> (radians, as the figure turns its nodes), each
    /// about the pivot: the head and face inside; the shell as a whole; the shell's and collar's pictures bent in the band round the
    /// pivot (<see cref="BentLimb.Warp"/>, lower), the head and face inside a soft one with them so none shows through the cloth; and
    /// what rides on its crown and glass, the lamp, the scanner and goggles.
    /// </summary>
    public (double Face, double Shell, double Bend, double Crown) TurnsBy(double look) => Turn switch
    {
        HelmetTurn.Soft => (0, 0, look, look),
        HelmetTurn.Bolted => (look, 0, 0, 0),
        HelmetTurn.Ring => (look, look, 0, look),
        _ => (0, 0, 0, 0),
    };

    /// <summary>
    /// How far its seal (<see cref="Sealed"/>) is bent as the head looks <paramref name="look"/>, about the pivot in its
    /// <see cref="Band"/>: as far as its shell turns over it, or with the rest of a soft one; its base stays with the collar.
    /// </summary>
    public double SealBy(double look) => TurnsBy(look) is var (_, shell, bend, _) ? shell + bend : 0;
}

/// <summary>How the suit's parts fit together where they meet (helmets-fit-the-suit): client art, not tunables.</summary>
public static class BodyFit
{
    /// <summary>Where the neck is on a helmet's picture, its 13 × 12 box's pixels: the joint it is hung from.</summary>
    public const double NeckX = 6.5, NeckY = 12;

    /// <summary>
    /// The basic helmet's ball, the circle its outline fits (tools/anime-art helmets.js, <c>BALL</c>): it turns about its middle on its
    /// ring (<see cref="HelmetTurn.Ring"/>), its picture kept within the circle under the ring, so it turns in place.
    /// </summary>
    private const double BallX = 6.7, BallY = 6;

    /// <summary>The middle of the basic helmet's rubber seal (rows 10 to 13.5): it bends there, the ball nodding on it.</summary>
    public const double SealY = 11.75;

    /// <summary>The head's middle (hero_head.png): a bolted helmet's head turns about it inside (<see cref="HelmetTurn.Bolted"/>), not out through the shell.</summary>
    private const double HeadX = 5.5, HeadY = 6.5;

    /// <summary>
    /// Where a soft helmet bends, below the neck (<see cref="BentLimb.Band"/> round it): low enough that a mask or plate over the chin
    /// turns whole, not sheared by the band.
    /// </summary>
    public const double SoftY = NeckY + 1;

    /// <summary>Soft hoods: their top turns, their base on the shoulders from row 12.5 drawn over the near arm.</summary>
    private static readonly HelmetFit Hood = new(HelmetTurn.Soft, NeckX, SoftY, Cut: 12.5);

    /// <summary>The basic helmet's ball, nodding whole on its rubber seal.</summary>
    private static readonly HelmetFit Ball = new(HelmetTurn.Soft, NeckX, SealY, Cut: null);

    /// <summary>
    /// A helmet skin turning whole on the basic helmet's seal: a beak, a jaw or a neck guard below the seal would bend with it
    /// as the ball's rubber does, so none bends; a rubber seal of its own, the basic collar's ring, bends between it and the collar.
    /// </summary>
    private static readonly HelmetFit Shell = new(HelmetTurn.Ring, NeckX, SealY, Cut: null, Sealed: true);

    /// <summary>Each helmet's fit by its item, the catchers' by their pictures (<see cref="CreatureLooks.Heroes"/>).</summary>
    private static readonly Dictionary<string, HelmetFit> Fits = new()
    {
        // The ball nods whole as the hood's top does, its rubber seal bending: the ring read as broken in play.
        [Stillwake.Content.Helmet.Basic] = Ball,
        // The helmet skins (hero-skins) are painted over the basic helmet's opening and turn whole on its seal.
        ["brassHelmet"] = Shell,
        ["beakHelmet"] = Shell,
        ["roninHelmet"] = Shell,
        ["knightHelmet"] = Shell,
        ["reliquaryHelmet"] = Shell,
        ["skullHelmet"] = Shell,
        ["ramHelmet"] = Shell,
        ["bathysphereHelmet"] = Shell,
        ["paradeHelmet"] = Shell,
        // The bell turns whole on its rubber gasket as the skins do on theirs, with no shoulder piece: it met no suit well.
        [Stillwake.Content.Helmet.Heavy] = Shell,
        ["catcherHood"] = Hood,
        ["catcherCap"] = Hood,
        ["catcherBeak"] = Hood,
        // The welded helm on its padded jack, its neck guard over the arm.
        ["catcherHelm"] = new(HelmetTurn.Bolted, HeadX, HeadY, Cut: 11.5),
    };

    /// <summary>Turning whole about the neck, as every helmet once did: the bare head, and a helmet with no fit of its own.</summary>
    public static readonly HelmetFit Whole = new(HelmetTurn.Ring, NeckX, NeckY, Cut: double.PositiveInfinity);

    /// <summary>
    /// The fit of <paramref name="helmet"/>, an item or a catcher's helmet, turning as <see cref="Trial"/> says if it is set;
    /// <see cref="Whole"/> for none or any other.
    /// </summary>
    public static HelmetFit Of(string? helmet) =>
        helmet == null || FitOf(helmet) is not { } fit ? Whole : Trial is { } turn ? As(helmet, fit, turn) : fit;

    private static HelmetFit? FitOf(string helmet) => Fits.TryGetValue(helmet, out var fit) ? fit : IsSocket(helmet) ? Shell : null;

    /// <summary>
    /// The socket helmets (helmet-socket): only a painted dome of their own, on the basic helmet's rim, rubber seal and glass,
    /// turning whole on that seal so nothing painted ever bends. Every new helmet is one.
    /// </summary>
    public static readonly IReadOnlyList<string> Sockets =
        [Stillwake.Content.Helmet.Heated, Stillwake.Content.Helmet.HeatProof, "dustHelmet", "hideHelmet", "boilerHelmet"];

    /// <summary>Whether <paramref name="helmet"/> is one of the <see cref="Sockets"/>.</summary>
    public static bool IsSocket(string? helmet) => helmet != null && Sockets.Contains(helmet);

    /// <summary>Every helmet turning this way instead of its own, to compare them in game (the dev key, <c>HelmetTurnKey</c>); none in play.</summary>
    public static HelmetTurn? Trial { get; set; }

    /// <summary><paramref name="helmet"/>'s <paramref name="fit"/> turning <paramref name="turn"/>, about the pivot that way takes.</summary>
    private static HelmetFit As(string helmet, HelmetFit fit, HelmetTurn turn) => turn == fit.Turn ? fit : turn switch
    {
        HelmetTurn.Soft => fit with { Turn = turn, PivotX = NeckX, PivotY = SoftY, Band = BentLimb.Band },
        HelmetTurn.Ring when helmet == Stillwake.Content.Helmet.Basic => fit with { Turn = turn, PivotX = BallX, PivotY = BallY },
        HelmetTurn.Ring => fit with { Turn = turn, PivotX = NeckX, PivotY = NeckY },
        _ => fit with { Turn = turn, PivotX = HeadX, PivotY = HeadY },
    };

    /// <summary>The helmets fitted, by name.</summary>
    public static IEnumerable<string> Fitted => Fits.Keys.Concat(Sockets);

    /// <summary>
    /// The rows of an arm's picture, from its box's top (its shoulder joint at 2.5), over which its top fades in from nothing:
    /// no round end shows at the shoulder, over the torso or a collar, however the arm turns.
    /// </summary>
    public static readonly (double Whole, double None) Shoulder = (4.5, 1.5);

    /// <summary>
    /// Whether a helmet's collar is drawn over the near arm bent <paramref name="arm"/>: while its elbow and fist are no higher than
    /// its shoulder, the shoulder piece over the shoulder; raised past it, the arm over the helmet's lower part as it crosses it.
    /// </summary>
    public static bool CollarOverArm(Bend arm) => Math.Cos(arm.Upper) >= 0 && Rig.EndOf(arm, Rig.Arm).Y >= 0;

    /// <summary>Bodies whose arm keeps its top: a catcher's receiver is strapped there (tracker-catchers).</summary>
    private static readonly HashSet<string> Receivers = CreatureLooks.Heroes.Values.SelectMany(look => look.Gear).Select(gear => gear.Body).ToHashSet();

    /// <summary>
    /// The rows of the part's picture <paramref name="picture"/> (<c>hero_</c> left off, its light <c>_lit</c> too) that fade, from
    /// whole to none, in its box's pixels: an arm's top, a helmet's shell's bottom; none for any other.
    /// </summary>
    public static (double Whole, double None)? FadeOf(string picture)
    {
        string name = picture.EndsWith("_lit", StringComparison.Ordinal) ? picture[..^4] : picture;
        if (name == "arm" || name.EndsWith("_arm", StringComparison.Ordinal))
            return Receivers.Contains(name.Length > 4 ? name[..^4] : "") ? null : Shoulder;
        string? helmet = name.EndsWith("_shell", StringComparison.Ordinal) ? name[..^6] : name.EndsWith("_collar", StringComparison.Ordinal) ? name[..^7] : null;
        return helmet != null && Fits.TryGetValue(helmet, out var fit) ? fit.Fade : null;
    }

    /// <summary>How much of a row <paramref name="y"/> shows, fading from whole at <paramref name="whole"/> to none at <paramref name="none"/>, smoothly.</summary>
    public static double Alpha(double y, double whole, double none)
    {
        double t = Math.Clamp((none - y) / (none - whole), 0, 1);
        return t * t * (3 - 2 * t);
    }
}
