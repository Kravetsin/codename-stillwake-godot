using System;
using Godot;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --flash</c>: the flash along the tablet's edge as notes start waiting (edge-flash), over and over for a player to watch: a find at 3 s, three close
/// together at 7 s that flash once, one more at 11 s, then again every 12 s. Each find is printed with its time.
/// </summary>
public partial class FlashDemo(Func<Registry> m) : Node
{
    private const double Every = 12;
    private static readonly double[] Finds = [3, 7, 7.15, 7.3, 11];

    private double _t;
    private int _found;

    public override void _Process(double delta)
    {
        _t += delta;
        double round = Math.Floor(_found / (double)Finds.Length) * Every;
        if (_t < round + Finds[_found % Finds.Length]) return;
        _found++;
        GD.Print($"{_t:0.0}s: a find");
        BoardDemo.FindOn(m().Knowledge, 1);
    }
}
