using System;
using System.Linq;
using Godot;
using Stillwake.Game.Features.Crew;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Items;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --cape</c>: the hero puts on a cape (2026-10-27-the-cape-is-worn) by commands, as a player would: creative mode on, a
/// cape spawned into the bag and moved into the cape slot, so the figure wears it (<see cref="Cape"/>); the
/// <see cref="HeroAutopilot.CapeRun"/> runs the hero about the hall with the axe and then the drill, pressing the hotbar keys
/// it takes as a player would, or the keys do with <c>--manual</c>.
/// </summary>
public partial class CapeDemo : Node
{
    /// <summary>The cape slot, its place in the inventory.</summary>
    public static readonly int CapeSlot = Spec.Inventory.Slots + Spec.Inventory.Equipment.ToList().IndexOf(Spec.Cape.Slot);

    private readonly HeroAutopilot? _script;
    private readonly Func<(Client Client, Registry M)?> _world;
    private double _sinceLook = 1;

    public CapeDemo(HeroAutopilot? script, Func<(Client Client, Registry M)?> world) => (_script, _world) = (script, world);

    public override void _Process(double delta)
    {
        if (_script?.TakeSlot() is int slot)
        {
            var key = Key.Key0 + slot;
            foreach (bool pressed in new[] { true, false })
                Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = pressed });
        }
        _sinceLook += delta;
        if (_sinceLook < 0.5 || _world() is not var (client, m)) return;
        _sinceLook = 0;
        if (m.Crew.HeroOf(client.Id) == null || m.Items.Equipped(client.Id, Spec.Cape.Item)) return;
        int held = m.Items.Inventory(client.Id).ToList().FindIndex(s => s?.Item == Spec.Cape.Item);
        if (held >= 0) client.Send(new Move(new Place(held), new Target(CapeSlot), 1));
        else if (!m.Items.Creative) client.Send(new SetCreative(true));
        else client.Send(new Spawn(Spec.Cape.Item, 1));
    }
}
