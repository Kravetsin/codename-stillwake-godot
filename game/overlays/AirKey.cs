using Godot;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Overlays;

/// <summary>
/// The air overlay's key on the tablet's edge, lit while air shows (0030): how a player finds out it exists. It and O switch it, in building or out of it, under any build tab;
/// the pipes and cables overlays come and go with their build tabs, drawn over the air, and have no key of their own.
/// </summary>
public partial class AirKey : Node
{
    /// <summary>Lines of wind, the top one curling back and the bottom one under, in a square of 14.</summary>
    private static readonly Rect2[] Wind =
    [
        new(0, 4, 12, 2), new(10, 1, 2, 3), new(6, 0, 5, 2), new(6, 2, 2, 1),
        new(0, 7, 14, 2),
        new(0, 10, 9, 2), new(7, 12, 2, 1), new(3, 13, 6, 2), new(3, 12, 2, 1),
    ];

    private readonly Ui _ui;

    public AirKey(Ui ui)
    {
        _ui = ui;
        Cap = new EdgeKey(Wind, Switch, ui.AirOn.Get);
    }

    /// <summary>The key itself, for the edge to set in place.</summary>
    public EdgeKey Cap { get; }

    private void Switch() => _ui.AirOn.Set(!_ui.AirOn.Get());

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is not InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.O }) return;
        Switch();
        GetViewport().SetInputAsHandled();
    }
}
