using System;
using I = Stillwake.Game.Shared.DroneInk;

namespace Stillwake.Game.Shared;

/// <summary>
/// A drone window's clock (drone-windows, the pick «Уголки»): opening, a spark from the drone's eye reaches each corner in
/// <see cref="DroneInk.Reach"/>, each next corner a little later, and the screen comes in from <see cref="DroneInk.ScreenFrom"/> over
/// <see cref="DroneInk.ScreenIn"/>, readable at about a tenth of a second; shut, the screen goes in <see cref="DroneInk.ScreenOut"/> as the
/// corners fly home in <see cref="DroneInk.Home"/>. Turned back midway, the corners go on from where they are. Client only.
/// </summary>
public sealed class CornerClock
{
    /// <summary>Seconds since it opened, as far as its corners are concerned; and since it shut.</summary>
    private double _opened, _shut = I.Home, _shutFor = I.ScreenOut;
    /// <summary>How much of the screen showed as it shut.</summary>
    private float _screenAtShut;

    /// <summary>Whether it is open, coming out, rather than shut.</summary>
    public bool Opening { get; private set; }

    /// <summary>Whether anything of it shows.</summary>
    public bool Shown => Opening || Flight(0) > 0 || Screen > 0;

    /// <summary>The screen's alpha, its glass and contents alike: 0 gone, 1 whole.</summary>
    public float Screen => Opening
        ? Clamp((_opened - I.ScreenFrom) / I.ScreenIn)
        : _screenAtShut * (1 - Clamp(_shutFor / I.ScreenOut));

    /// <summary>How far corner <paramref name="k"/>'s spark is along its flight: 0 at the eye, 1 at its corner.</summary>
    public float Flight(int k) => Opening ? Clamp((_opened - I.ReachLag * k) / I.Reach) : 1 - Clamp(_shut / I.Home);

    /// <summary>How far corner <paramref name="k"/> is past its landing, 0..1: its halo fades over it.</summary>
    public float Landed(int k) => Opening ? Clamp((_opened - I.Reach - I.ReachLag * k) / I.Landing) : 1;

    /// <summary>Steps it <paramref name="seconds"/> on, <paramref name="open"/> or not.</summary>
    public void Step(double seconds, bool open)
    {
        if (open && !Opening)
        {
            // From where the corners are: home, on their way, or still at the window, landed long since.
            float flight = Flight(0);
            _opened = flight >= 1 ? I.Reach + 3 * I.ReachLag + I.Landing : flight * I.Reach;
            Opening = true;
        }
        else if (!open && Opening)
        {
            float flight = Flight(0);
            _screenAtShut = Screen;
            (_shut, _shutFor, Opening) = ((1 - flight) * I.Home, 0, false);
        }
        if (Opening) _opened += seconds;
        else
        {
            _shut += seconds;
            _shutFor += seconds;
        }
    }

    private static float Clamp(double v) => (float)Math.Clamp(v, 0, 1);
}

/// <summary>
/// The drone's frame round a drone window (drone-windows, the pick «Уголки»), screen px: no border, four brackets in the player's colour just
/// outside its corners, each made by an ember shot from the drone's eye, sparks burning into the colour along its arms (drone-salts). The pick's faint threads from the eye to
/// the corners and the shots' lines back to it show only when asked (<see cref="Threads"/>, a dev key): what stretches across the
/// screen from the drone is to go. Client only.
/// </summary>
public static class CornerFrame
{
    /// <summary>Whether the eye's threads to the corners show, and each shot's line back to it: off, to compare in game by a dev key.</summary>
    public static bool Threads { get; set; }

    /// <summary>Corner <paramref name="k"/> of <paramref name="window"/>, clockwise from its top left, <see cref="DroneInk.CornerOut"/> out of it.</summary>
    public static (float X, float Y) Corner((float X, float Y, float W, float H) window, int k)
    {
        const float O = I.CornerOut;
        var (x, y, w, h) = window;
        return k switch
        {
            0 => (x - O, y - O),
            1 => (x + w + O, y - O),
            2 => (x + w + O, y + h + O),
            _ => (x - O, y + h + O),
        };
    }

    /// <summary>The ends of corner <paramref name="k"/>'s bracket's arms: along the window's top or foot, and down or up its side.</summary>
    public static ((float X, float Y) Across, (float X, float Y) Along) Arms((float X, float Y) corner, int k)
    {
        float sx = k is 0 or 3 ? 1 : -1, sy = k < 2 ? 1 : -1;
        return ((corner.X + sx * I.Arm, corner.Y), (corner.X, corner.Y + sy * I.Arm));
    }

    /// <summary>
    /// The frame of <paramref name="window"/> at <paramref name="clock"/>'s time, <paramref name="seconds"/> into its sparks' running,
    /// shot from the eye at <paramref name="eye"/> (none: the corners appear and go where they stand), into <paramref name="into"/>.
    /// </summary>
    public static void Draw(EmberMarks into, CornerClock clock, (float X, float Y, float W, float H) window, (float X, float Y)? eye, float seconds)
    {
        for (int k = 0; k < 4; k++)
        {
            var corner = Corner(window, k);
            float flight = clock.Flight(k);
            if (flight <= 0 || eye == null && !clock.Opening) continue;
            if (flight < 1 && eye is { } shotFrom)
            {
                Shot(into, clock.Opening, shotFrom, corner, flight);
                continue;
            }
            Bracket(into, corner, k, clock.Landed(k), seconds);
            if (Threads && eye is { } from) Thread(into, from, corner, k, seconds);
        }
    }

    /// <summary>A spark on its way, <paramref name="flight"/> from the eye to its corner, heading there opening and home closing.</summary>
    private static void Shot(EmberMarks into, bool opening, (float X, float Y) eye, (float X, float Y) corner, float flight)
    {
        float e = flight * flight * (3 - 2 * flight), wx = corner.X - eye.X, wy = corner.Y - eye.Y, d = MathF.Max(1e-4f, MathF.Sqrt(wx * wx + wy * wy));
        float x = eye.X + wx * e, y = eye.Y + wy * e, way = opening ? 1 : -1;
        if (Threads) into.Segment(eye.X, eye.Y, x, y, I.ShotLine, 0, I.ShotAlpha * (opening ? 1 : flight));
        into.Spark(x, y, way * wx / d, way * wy / d, 1, I.Shot, burn: 0);
    }

    /// <summary>A landed corner: its bracket's lines, the sparks running out along its arms, its glowing square, its landing's halo.</summary>
    private static void Bracket(EmberMarks into, (float X, float Y) corner, int k, float landed, float seconds)
    {
        var (across, along) = Arms(corner, k);
        foreach (var (width, alpha) in (ReadOnlySpan<(float, float)>)[(I.BracketWide, I.BracketWideAlpha), (I.BracketFine, I.BracketFineAlpha)])
        {
            into.Begin();
            into.To(across.X, across.Y);
            into.To(corner.X, corner.Y);
            into.To(along.X, along.Y);
            into.End(width, 0, alpha);
        }
        foreach (var end in (ReadOnlySpan<(float X, float Y)>)[across, along])
        {
            float dx = (end.X - corner.X) / I.Arm, dy = (end.Y - corner.Y) / I.Arm;
            for (int m = 0; m < I.ArmSparks; m++)
            {
                float s = Frac(seconds * I.ArmPace + m / (float)I.ArmSparks + k * I.ArmLater);
                into.Spark(corner.X + dx * I.Arm * s, corner.Y + dy * I.Arm * s, dx, dy, I.ArmHeat * (1 - s * I.ArmFade), burn: s);
            }
        }
        into.Glow(corner.X, corner.Y, I.CornerSquare, I.CornerHeat, burn: 0);
        into.Add(new EmberMarks.Halo(corner.X, corner.Y, I.LandHalo, 0, I.LandHaloAlpha * (1 - landed)));
    }

    /// <summary>The faint thread from the eye to a landed corner, a spark creeping along it.</summary>
    private static void Thread(EmberMarks into, (float X, float Y) eye, (float X, float Y) corner, int k, float seconds)
    {
        into.Segment(eye.X, eye.Y, corner.X, corner.Y, I.CornerThread, 0, I.CornerThreadAlpha);
        float wx = corner.X - eye.X, wy = corner.Y - eye.Y, d = MathF.Max(1e-4f, MathF.Sqrt(wx * wx + wy * wy)), s = Frac(seconds * I.Creep + k * I.CreepLater);
        into.Spark(eye.X + wx * s, eye.Y + wy * s, wx / d, wy / d, I.CreepHeat, burn: s);
    }

    private static float Frac(float v) => v - MathF.Floor(v);
}
