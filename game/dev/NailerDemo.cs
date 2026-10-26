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
/// <c>-- --strip --nailer</c> (shots-are-the-sims): the hero with the nailer in its first slot and a stack of nails, a loper let out
/// sixteen columns left on the open ground once it stands on the strip; then, as a player's mouse would, the nailer taken in hand
/// and fired up and left at 45°, at the loper, and level at the ground ahead, three and a half seconds apart. Aboard (no
/// <c>--strip</c>) the same shots fly straight. Never the sim but the loper: only the input a player gives.
/// </summary>
public partial class NailerDemo(Func<Hero?> hero) : Node
{
    /// <summary>Seconds on the surface before each shot.</summary>
    private static readonly double[] ShotsAt = [2.5, 6, 9.5, 13];

    private double _since;
    private int _fired;
    private bool _wielded, _released;

    /// <summary>The nailer in the first slot and a full stack of nails in the second, put into the save.</summary>
    public static World Seed(World world, Func<System.Collections.Generic.IReadOnlyList<Module>> modules)
    {
        var root = JsonNode.Parse(world.Save())!;
        var slots = new JsonArray();
        for (int i = 0; i < Spec.Inventory.Total; i++)
            slots.Add(i == 0 ? new JsonObject { ["item"] = Spec.Shots.Nailer.Item, ["count"] = 1 }
                : i == 1 ? new JsonObject { ["item"] = Spec.Shots.Nailer.Ammo, ["count"] = 100 }
                : null);
        root["slices"]!["items"]!["inventories"] = new JsonArray(new JsonObject { ["id"] = 0, ["slots"] = slots });
        return World.Load(root.ToJsonString(), modules());
    }

    /// <summary>A frame of the run: once the hero stands off the ship, a loper let out ahead of it.</summary>
    public void Frame(Client client, Registry m)
    {
        if (_released || client.World.SurfaceOf(client.Id) == 0 || m.Crew.HeroOf(client.Id) is not { } me) return;
        _released = true;
        client.Send(new Sim.Creatures.Release(Spec.Creatures.Loper.Id, me.Cell - 16));
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
        if (_fired >= ShotsAt.Length) return;
        // Up and left at 45°, then at the loper's height fourteen columns on, then level at the ground ahead.
        var aim = _fired switch
        {
            0 => (-10.0, -10.0),
            1 => (-14.0, -1.5),
            _ => (-20.0, 0.5),
        };
        var at = GetViewport().GetCanvasTransform() * (new Vector2((float)(me.X / (double)Hero.Unit + aim.Item1), (float)(me.Y / (double)Hero.Unit - 2.5 + aim.Item2)) * (float)ShipCamera.Cell);
        Input.ParseInputEvent(new InputEventMouseMotion { Position = at, GlobalPosition = at });
        if (_since < ShotsAt[_fired]) return;
        _fired++;
        foreach (bool down in (bool[])[true, false])
            Input.ParseInputEvent(new InputEventMouseButton { ButtonIndex = MouseButton.Left, Pressed = down, ButtonMask = down ? MouseButtonMask.Left : 0, Position = at, GlobalPosition = at });
    }

    private static void Press(Key key, bool down) => Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = down });
}
