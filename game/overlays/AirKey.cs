using Godot;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Overlays;

/// <summary>
/// The air overlay's key on the tablet's edge, lines of wind lit while air shows (0030): how a player finds out it exists. It and O switch
/// it, in building or out of it, under any build tab; the pipes and cables overlays come and go with their build tabs, drawn over the air,
/// and have no key of their own.
/// </summary>
public partial class AirKey : Node
{
    private readonly Ui _ui;

    public AirKey(Ui ui)
    {
        _ui = ui;
        Cap = new EdgeKey("air", Switch, ui.AirOn.Get);
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
