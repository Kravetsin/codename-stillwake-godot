using System;
using Godot;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Items;

namespace Stillwake.Game.Features.Power;

/// <summary>
/// For recordings (<c>-- --store</c>, 0101): plays <see cref="StoreWorld"/>, the bank filling while the smelter stands empty;
/// at <see cref="FeedAtS"/> seconds the hero puts its quartz into the smelter, which works with the arc, and the bank gives out.
/// </summary>
public partial class StoreDemo : Node
{
    /// <summary>When the quartz goes into the smelter, in seconds of play.</summary>
    public const double FeedAtS = 12;

    private readonly Func<(Client Client, Registry M)?> _session;
    private double _t;
    private bool _fed;

    public StoreDemo(Func<(Client Client, Registry M)?> session) => _session = session;

    public override void _Process(double delta)
    {
        _t += delta;
        if (_session() is not var (client, m)) return;
        // The pointer rests on the bank, so its tip tells what it holds as it fills and empties.
        var bank = m.Grid.MachinesOf("capacitorBank")[0].Cell;
        var at = GetViewport().GetCanvasTransform() * (new Vector2(bank % m.Grid.Width + 2, bank / m.Grid.Width + 2) * Render.ShipCamera.Cell);
        Input.ParseInputEvent(new InputEventMouseMotion { Position = at, GlobalPosition = at });
        if (_fed || _t < FeedAtS) return;
        _fed = true;
        client.Send(new Move(new Place(StoreWorld.QuartzSlot), new Target(0, m.Grid.MachinesOf("smelter")[0].Id), StoreWorld.Quartz));
    }
}
