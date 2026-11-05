using System;
using Godot;
using Stillwake.Sim;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --bulb</c>: the bulb at the screen's top as a note starts waiting (hologram-keys), a find a second in; with <c>=two</c> another 0.9 s after it,
/// once the bulb has burst; with <c>=open</c> T while the find flies down, and T again a second later, so nothing of it is left. Each step is printed with its time.
/// </summary>
public partial class BulbDemo(Func<Registry> m, string? mode) : Node
{
    private const double First = 1, Second = 1.9, Opens = 1.9, Closes = 2.9;

    private double _t;
    private int _step;

    public override void _Process(double delta)
    {
        _t += delta;
        if (_step == 0 && _t >= First) Find();
        else if (_step == 1 && mode == "two" && _t >= Second) Find();
        else if (_step == 1 && mode == "open" && _t >= Opens) Press("T mid-flight: the bulb and the find put out");
        else if (_step == 2 && mode == "open" && _t >= Closes) Press("T again: nothing of the bulb is left");
    }

    private void Find()
    {
        _step++;
        GD.Print($"{_t:0.0}s: a find");
        BoardDemo.FindOn(m().Knowledge, 1);
    }

    private void Press(string what)
    {
        _step++;
        GD.Print($"{_t:0.0}s: {what}");
        TabletHands.Press(Key.T);
    }
}
