using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Crew;

/// <summary>What the drone does on the right button (drone-abilities): the throw, or a gun form it has learned.</summary>
public enum DroneActive { Throw, Nails }

/// <summary>The drone wheel's two sides (drone-abilities): what the right button does, and what the drone does meanwhile.</summary>
public enum DroneBranch { Actives, Modes }

/// <summary>A side of the drone's wheel: a mode, or else an active.</summary>
public readonly record struct DroneSide(DroneMode? Mode, DroneActive Active = DroneActive.Throw)
{
    public static DroneSide Of(DroneMode mode) => new(mode);
    public static DroneSide Of(DroneActive active) => new(null, active);
}

/// <summary>
/// The drone's wheel on its key (drone-abilities): two branches, the right button switching them while it is open, and it opens on the
/// one used last. The actives show the throw and every gun form learned; the modes, as <see cref="DroneModes"/>. The chosen active and
/// mode are the client's, never commands and never saved: a new game or a reload starts on the lamp and the throw.
/// </summary>
public sealed class DroneWheel
{
    private static readonly DroneSide[] ModeSides = [DroneSide.Of(DroneMode.Lamp), DroneSide.Of(DroneMode.Scan)];
    private static readonly DroneActive[] Actives = Enum.GetValues<DroneActive>();
    private readonly List<DroneSide> _actives = [];

    /// <summary>The branch the wheel shows: the one used last, the modes at first.</summary>
    public DroneBranch Branch { get; private set; } = DroneBranch.Modes;

    /// <summary>What the right button does.</summary>
    public DroneActive Active { get; private set; } = DroneActive.Throw;

    /// <summary>What the drone does meanwhile.</summary>
    public DroneMode Mode { get; set; } = DroneMode.Lamp;

    /// <summary>Goes to the other branch.</summary>
    public void Switch() => Branch = Branch == DroneBranch.Modes ? DroneBranch.Actives : DroneBranch.Modes;

    /// <summary>Takes <paramref name="side"/> picked on the wheel.</summary>
    public void Pick(DroneSide side)
    {
        if (side.Mode is { } mode) Mode = mode;
        else Active = side.Active;
    }

    /// <summary>The branch's sides, from the top clockwise: the actives only the throw and those <paramref name="known"/>. The same list each time.</summary>
    public IReadOnlyList<DroneSide> SidesOf(Func<DroneActive, bool> known)
    {
        if (Active != DroneActive.Throw && !known(Active)) Active = DroneActive.Throw;
        if (Branch == DroneBranch.Modes) return ModeSides;
        _actives.Clear();
        foreach (var active in Actives)
            if (active == DroneActive.Throw || known(active)) _actives.Add(DroneSide.Of(active));
        return _actives;
    }

    /// <summary>Whether <paramref name="side"/> is what is chosen now: its branch's mode or active.</summary>
    public bool Chosen(DroneSide side) => side.Mode is { } mode ? mode == Mode : side.Active == Active;

    /// <summary>The drone's gun form <paramref name="active"/> is, its form's id too; none for the throw.</summary>
    public static Content.Gun? GunOf(DroneActive active) => active == DroneActive.Nails ? Content.Shots.DroneNailer : null;

    /// <summary>A side's thread icon (thread-icons).</summary>
    public static Shared.IconLines IconOf(DroneSide side) =>
        side.Mode is { } mode ? DroneModes.IconOf(mode) : side.Active == DroneActive.Nails ? NailsIcon : ThrowIcon;

    // The wheel's own icons: stand-ins drawn here, in the thread icons' box -1..1, y down, until a pick exports them with the rest.

    /// <summary>The throw: an arc flung out from the hand, its head an arrow.</summary>
    public static readonly Shared.IconLines ThrowIcon = Of(Arc(-0.75f, 0.7f, -0.55f, -0.95f, 0.8f, -0.45f, 14), Arrow(-0.55f, -0.95f, 0.8f, -0.45f), Ring(-0.75f, 0.7f, 0.16f, 12));

    /// <summary>The nail form: three nails, heads back, flying the same way.</summary>
    public static readonly Shared.IconLines NailsIcon = Of(Nail(-0.8f, -0.35f), Nail(-0.62f, 0.1f), Nail(-0.8f, 0.55f));

    /// <summary>The right button, held open on the wheel to switch its branches: a mouse, its right button struck through.</summary>
    public static readonly Shared.IconLines RightButton = Of(Ring(0, 0, 0.58f, 0.92f, 24), [-0.58f, -0.12f, 0.58f, -0.12f], [0, -0.92f, 0, -0.12f],
        [0.12f, -0.62f, 0.46f, -0.62f], [0.12f, -0.38f, 0.54f, -0.38f]);

    private static Shared.IconLines Of(params float[][] parts)
    {
        var one = new List<float>();
        var bridges = new List<int>();
        foreach (var part in parts)
        {
            if (one.Count > 0) bridges.Add(one.Count / 2);
            one.AddRange(part);
        }
        return new Shared.IconLines(parts, [.. one], [.. bridges]);
    }

    private static float[] Nail(float x, float y) => [x, y - 0.16f, x, y + 0.16f, x, y, x + 1.45f, y, x + 1.6f, y];

    private static float[] Arc(float ax, float ay, float cx, float cy, float bx, float by, int steps)
    {
        var points = new float[(steps + 1) * 2];
        for (int i = 0; i <= steps; i++)
        {
            float t = i / (float)steps, u = 1 - t;
            points[2 * i] = u * u * ax + 2 * u * t * cx + t * t * bx;
            points[2 * i + 1] = u * u * ay + 2 * u * t * cy + t * t * by;
        }
        return points;
    }

    private static float[] Arrow(float cx, float cy, float tx, float ty)
    {
        float dx = tx - cx, dy = ty - cy, len = MathF.Sqrt(dx * dx + dy * dy), size = 0.36f;
        (dx, dy) = (dx / len, dy / len);
        float c = MathF.Cos(0.5f), s = MathF.Sin(0.5f);
        return [tx - size * (dx * c - dy * s), ty - size * (dx * s + dy * c), tx, ty, tx - size * (dx * c + dy * s), ty - size * (-dx * s + dy * c)];
    }

    private static float[] Ring(float x, float y, float r, int steps) => Ring(x, y, r, r, steps);

    private static float[] Ring(float x, float y, float rx, float ry, int steps)
    {
        var points = new float[(steps + 1) * 2];
        for (int i = 0; i <= steps; i++)
        {
            float a = i * MathF.Tau / steps;
            points[2 * i] = x + rx * MathF.Sin(a);
            points[2 * i + 1] = y - ry * MathF.Cos(a);
        }
        return points;
    }
}
