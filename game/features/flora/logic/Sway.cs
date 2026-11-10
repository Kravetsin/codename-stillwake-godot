using System;

namespace Stillwake.Game.Features.Flora;

/// <summary>
/// A bush or a tree swaying about its foot (plant-stirs): a damped spring in how far its top leans, kicked by a runner brushing
/// it or a hit, leaned by the wind while a gust blows, back upright once let go. Plain C#.
/// </summary>
public sealed class Sway(FloraLooks.Swing swing)
{
    /// <summary>How far it leans now, in radians: positive its top to the right.</summary>
    public float Lean { get; private set; }

    /// <summary>Where the wind holds it this frame, in radians; 0 in calm.</summary>
    public float Held { get; set; }

    private float _speed;

    /// <summary>Whether it stands still and upright with nothing holding it: let go.</summary>
    public bool Still => Held == 0 && MathF.Abs(Lean) < 0.0005f && MathF.Abs(_speed) < 0.005f;

    /// <summary>A push its way: <paramref name="way"/> 1 to the right, -1 to the left, <paramref name="share"/> of its swing's kick.</summary>
    public void Kick(int way, float share = 1) => _speed += Math.Sign(way) * swing.Kick * share;

    /// <summary>Sways on for <paramref name="seconds"/>.</summary>
    public void Advance(double seconds)
    {
        float dt = (float)Math.Min(seconds, 0.1), w = 2 * MathF.PI * swing.Hz, z = swing.Damping;
        int steps = Math.Max(1, (int)MathF.Ceiling(dt * 240));
        float h = dt / steps;
        for (int i = 0; i < steps; i++)
        {
            _speed += (w * w * (Held - Lean) - 2 * z * w * _speed) * h;
            Lean += _speed * h;
        }
        Lean = Math.Clamp(Lean, -swing.Most, swing.Most);
    }
}
