using System;
using Godot;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --flash</c>: the flash along the tablet's edge as notes start waiting (edge-flash): a find at 1 s, three close together at 4 s that flash once,
/// and one more at 8 s. Each find is printed with its time.
/// </summary>
public partial class FlashDemo(Func<Registry> m) : Node
{
    private static readonly double[] Finds = [1, 4, 4.15, 4.3, 8];

    private double _t;
    private int _found;

    public override void _Process(double delta)
    {
        _t += delta;
        if (_found >= Finds.Length || _t < Finds[_found]) return;
        _found++;
        GD.Print($"{_t:0.0}s: a find");
        BoardDemo.FindOn(m().Knowledge, 1);
    }
}
