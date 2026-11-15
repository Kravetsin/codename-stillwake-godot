using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Flora;

/// <summary>
/// A felled tree falling over (plant-stirs): pivoted at its foot, it tips as a rod does, slowly and then fast, to
/// <see cref="Lie"/> from upright, rebounds once a little and lies still. It meets no ground on the way: a slope or a wall
/// beside it is passed into for the seconds it lies (accepted in the ADR). Plain C#.
/// </summary>
public sealed class Topple
{
    /// <summary>How far from upright it lies, in radians: a little short of flat, its crown resting on its leaves.</summary>
    public const float Lie = 1.45f;

    /// <summary>How far it leans as it starts, in radians: a hair, so the first moment is slow.</summary>
    public const float Start = 0.05f;

    /// <summary>The share of its speed it rebounds with on landing.</summary>
    public const float Rebound = 0.22f;

    /// <summary>1 to fall to the right, -1 to the left.</summary>
    public int Way { get; }

    /// <summary>How far it leans now, in radians, toward <see cref="Way"/>: 0 upright.</summary>
    public float Angle { get; private set; } = Start;

    /// <summary>Whether it lies still.</summary>
    public bool Lying { get; private set; }

    private float _speed;
    private bool _bounced;
    private readonly float _pull;

    /// <summary>A tree falling toward <paramref name="way"/>, gaining <paramref name="pull"/> radians a second each second a radian it leans.</summary>
    public Topple(int way, float pull)
    {
        Way = way < 0 ? -1 : 1;
        _pull = pull;
    }

    /// <summary>Its rotation as drawn, in radians, clockwise: positive falling right.</summary>
    public float Rotation => Way * Angle;

    /// <summary>Falls on for <paramref name="seconds"/>; true the moment its crown first strikes the ground.</summary>
    public bool Advance(double seconds)
    {
        if (Lying) return false;
        float dt = (float)Math.Min(seconds, 0.1);
        int steps = Math.Max(1, (int)MathF.Ceiling(dt * 240));
        float h = dt / steps;
        bool struck = false;
        for (int i = 0; i < steps && !Lying; i++)
        {
            _speed += _pull * MathF.Sin(Angle) * h;
            Angle += _speed * h;
            if (Angle < Lie) continue;
            Angle = Lie;
            if (_bounced)
            {
                _speed = 0;
                Lying = true;
                continue;
            }
            struck = true;
            _bounced = true;
            _speed = -_speed * Rebound;
        }
        return struck;
    }

    /// <summary>
    /// Which way a tree standing at <paramref name="x"/> falls: away from the nearest of <paramref name="heroes"/> (their x) within
    /// <paramref name="reach"/> pixels, so away from whoever felled it; to the right with none near, or one straight under it.
    /// </summary>
    public static int Away(float x, IEnumerable<float> heroes, float reach)
    {
        float? nearest = null;
        foreach (float hero in heroes)
            if (Math.Abs(hero - x) <= reach && (nearest is not { } n || Math.Abs(hero - x) < Math.Abs(n - x))) nearest = hero;
        return nearest is { } near && near > x ? -1 : 1;
    }
}
