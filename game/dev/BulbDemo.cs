using System;
using Godot;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --bulb</c>: the bulb at the screen's top as a note starts waiting (hologram-keys), a find a second in and with <c>=two</c> another 0.9 s after it,
/// once the bulb has burst. Each find is printed with its time.
/// </summary>
public partial class BulbDemo(Func<Registry> m, bool two) : Node
{
    private const double First = 1, Second = 1.9;

    private double _t;
    private int _found;

    public override void _Process(double delta)
    {
        _t += delta;
        if (_found == 0 && _t >= First) Find();
        else if (_found == 1 && two && _t >= Second) Find();
    }

    private void Find()
    {
        _found++;
        GD.Print($"{_t:0.0}s: a find");
        BoardDemo.FindOn(m().Knowledge, 1);
    }
}
