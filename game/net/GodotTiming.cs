using System;
using Godot;
using Stillwake.Net;

namespace Stillwake.Game.Net;

/// <summary>Real time for a bad network made on purpose (<c>--lag</c>, <c>--jitter</c>): the scene tree's timers.</summary>
public sealed class GodotTiming(SceneTree tree) : ITiming
{
    public void Wait(double ms, Action then) => tree.CreateTimer(ms / 1000).Timeout += then;
    public double Random() => GD.Randf();
}
