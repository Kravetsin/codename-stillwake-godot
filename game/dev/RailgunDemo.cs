using System;
using System.Text.Json.Nodes;
using Godot;
using Stillwake.Game.Features.Crew;
using Stillwake.Game.Render;
using Stillwake.Net;
using Stillwake.Sim;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --strip --railgun</c> (2026-10-27-the-railgun): the hero with the railgun in its first slot, nails in its second and a full
/// battery on its belt, two lopers let out ahead of it, one behind the other, once it stands on the strip; then, as a player's mouse
/// would, the railgun taken in hand and held to charge, let go full at the lopers, straight up, and down into the ground ahead,
/// and once let go early, firing nothing. Never the sim but the lopers: only the input a player gives.
/// </summary>
public partial class RailgunDemo(Func<Hero?> hero) : Node
{
    /// <summary>Each shot: when the button goes down and up, in seconds on the surface, and where it aims, in cells from the hero's middle.</summary>
    private static readonly (double Down, double Up, double X, double Y)[] Shots =
    [
        (2.0, 3.2, -16, -0.5), (5.6, 6.0, -16, -0.5), (6.5, 7.7, 0.5, -16), (10.0, 11.2, -10, 8),
    ];

    private double _since;
    private int _shot;
    private bool _wielded, _released, _held;

    /// <summary>The railgun in the first slot, a full stack of nails in the second and a full battery on the belt, put into the save.</summary>
    public static World Seed(World world, Func<System.Collections.Generic.IReadOnlyList<Module>> modules)
    {
        var root = JsonNode.Parse(world.Save())!;
        var slots = new JsonArray();
        for (int i = 0; i < Spec.Inventory.Total; i++)
            slots.Add(i == 0 ? new JsonObject { ["item"] = Spec.Shots.Railgun.Item, ["count"] = 1 }
                : i == 1 ? new JsonObject { ["item"] = Spec.Shots.Nail.Id, ["count"] = 100 }
                : i == Spec.Inventory.Slots + 2 ? new JsonObject { ["item"] = "battery", ["count"] = 1, ["charge"] = Spec.Charge.Battery }
                : null);
        root["slices"]!["items"]!["inventories"] = new JsonArray(new JsonObject { ["id"] = 0, ["slots"] = slots });
        return World.Load(root.ToJsonString(), modules());
    }

    /// <summary>A frame of the run: once the hero stands off the ship, two lopers let out ahead of it.</summary>
    public void Frame(Client client, Registry m)
    {
        if (_released || client.World.SurfaceOf(client.Id) == 0 || m.Crew.HeroOf(client.Id) is not { } me) return;
        _released = true;
        client.Send(new Sim.Creatures.Release(Spec.Creatures.Loper.Id, me.Cell - 12));
        client.Send(new Sim.Creatures.Release(Spec.Creatures.Loper.Id, me.Cell - 17));
    }

    public override void _Process(double delta)
    {
        if (hero() is not { } me) return;
        _since += delta;
        if (!_wielded && _since > 1)
        {
            _wielded = true;
            Press(Key.Key1, true);
            Press(Key.Key1, false);
        }
        if (_shot >= Shots.Length) return;
        var (down, up, dx, dy) = Shots[_shot];
        var at = GetViewport().GetCanvasTransform() * (new Vector2((float)(me.X / (double)Hero.Unit + dx), (float)(me.Y / (double)Hero.Unit - 2.5 + dy)) * (float)ShipCamera.Cell);
        Input.ParseInputEvent(new InputEventMouseMotion { Position = at, GlobalPosition = at, ButtonMask = _held ? MouseButtonMask.Left : 0 });
        if (!_held && _since >= down) Button(at, _held = true);
        else if (_held && _since >= up)
        {
            Button(at, _held = false);
            _shot++;
        }
    }

    private static void Button(Vector2 at, bool down) =>
        Input.ParseInputEvent(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = down, ButtonMask = down ? MouseButtonMask.Left : 0, Position = at, GlobalPosition = at });

    private static void Press(Key key, bool down) => Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = down });
}
