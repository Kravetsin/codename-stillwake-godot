using System;
using Godot;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --find-scan</c>: the drone scanning each find over the hero's head (find-scan), over and over for a player to watch: a find at 3 s, three found
/// together at 7 s, one more at 11 s, then again every 12 s. Each find is printed with its time.
/// </summary>
public partial class FindScanDemo(Func<Registry> m) : Node
{
    private const double Every = 12;
    private static readonly (double At, int Count)[] Finds = [(3, 1), (7, 3), (11, 1)];

    private double _t;
    private int _found;

    public override void _Process(double delta)
    {
        _t += delta;
        var (at, count) = Finds[_found % Finds.Length];
        if (_t < Math.Floor(_found / (double)Finds.Length) * Every + at) return;
        _found++;
        GD.Print($"{_t:0.0}s: {count} found");
        BoardDemo.FindOn(m().Knowledge, count);
    }
}
