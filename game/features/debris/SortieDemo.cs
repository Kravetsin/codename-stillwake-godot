using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Debris;

/// <summary>
/// <c>-- --sortie</c>: the big world (0048), the hero far out above the hull, in the far field's outermost tier
/// (0052, 0056), with a jetpack on, the drill on the hotbar's first slot, the fire axe on its second, canisters
/// and two full batteries on the belt; one of each kind the sortie finds there floats round it, still: the
/// engine block beside it, the cargo hold, the pod cluster and the small, large and rich asteroids about.
/// A script played through Godot's own input, for recordings with <c>--write-movie</c>: the drill held on
/// the engine block till it breaks into its loot, a canister breathed whenever the suit runs half empty. With
/// <c>--manual</c> the script stays off and the hands are the player's.
/// </summary>
public partial class SortieDemo : CanvasLayer
{
    /// <summary>Rows above the top of what is built the hero starts: past 256, so the far field round it is its outermost tier.</summary>
    private const int Above = 280;

    /// <summary>The pieces: their kind, and cells right of the hero's middle (left when negative) and up from its cell's top, in thousandths, to their middles.</summary>
    private static readonly (string Item, int Right, int Up)[] Finds =
    [
        ("engineBlock", 7_800, 600),
        ("cargoHold", -24_000, 8_000),
        ("podCluster", 8_000, 16_000),
        ("asteroidLarge", 30_000, -6_000),
        ("asteroidRich", -12_000, -10_000),
        ("asteroidSmall", -30_000, -8_000),
    ];

    /// <summary>Where the hero starts: <see cref="Above"/> rows over the top of what is built, above the spawn.</summary>
    public static int StartOf(Registry m) => Math.Max(0, m.Grid.Built.Top - Above) * m.Grid.Width + m.Crew.Spawn % m.Grid.Width;

    /// <summary>
    /// <paramref name="world"/>, read through <paramref name="m"/>: peer 0 out at <see cref="StartOf"/> with the drill,
    /// the axe, canisters, a jetpack and two full batteries, and the finds round it, put in through its save.
    /// </summary>
    public static World Seed(World world, Registry m, Func<IReadOnlyList<Module>> modules)
    {
        var root = JsonNode.Parse(world.Save())!;
        var equipment = Spec.Inventory.Equipment.ToList();
        int belt = Spec.Inventory.Slots + equipment.IndexOf(Spec.Charge.Belt), back = Spec.Inventory.Slots + equipment.IndexOf("back");
        var slots = new JsonArray();
        for (int i = 0; i < Spec.Inventory.Total; i++)
            slots.Add(i == 0 ? new JsonObject { ["item"] = Spec.Weapons.Drill.Item, ["count"] = 1 }
                : i == 1 ? new JsonObject { ["item"] = Spec.Weapons.Axe.Item, ["count"] = 1 }
                : i is 9 or 10 ? new JsonObject { ["item"] = Spec.Crew.Canister, ["count"] = 5 }
                : i == back ? new JsonObject { ["item"] = Spec.Jetpack.Item, ["count"] = 1 }
                : i == Spec.Inventory.Total - 2 ? new JsonObject { ["item"] = Spec.Suit.Basic, ["count"] = 1 }
                : i == Spec.Inventory.Slots ? new JsonObject { ["item"] = Spec.Helmet.Basic, ["count"] = 1 }
                : i == belt || i == belt + 1 ? new JsonObject { ["item"] = "battery", ["count"] = 1, ["charge"] = Spec.Charge.Battery }
                : null);
        root["slices"]!["items"]!["inventories"] = new JsonArray(new JsonObject { ["id"] = 0, ["slots"] = slots });
        int start = StartOf(m), sx = start % m.Grid.Width, sy = start / m.Grid.Width;
        root["slices"]!["crew"]!["heroes"] = new JsonArray(new JsonObject
        {
            ["id"] = 0,
            ["cell"] = start,
            ["air"] = Spec.Items.Get(Spec.Suit.Basic).Tank,
            ["out"] = 0,
            ["exerting"] = false,
            ["thrusting"] = false,
            ["thrustTicks"] = 0,
        });
        var debris = root["slices"]!["debris"]!;
        var list = debris["pieces"]!.AsArray();
        int next = (int)debris["nextId"]!, clock = (int)debris["clock"]!;
        foreach (var (item, right, up) in Finds)
        {
            int x = sx * 1000 + 500 + right;
            list.Add(new JsonObject
            {
                ["id"] = next++,
                ["zone"] = Spec.Debris.Far.Id,
                ["item"] = item,
                ["x"] = x,
                ["y"] = sy * 1000 - up,
                // Still, so they wait to be tried, and the script's hands stay on the engine.
                ["speed"] = 0,
                ["start"] = x,
                ["end"] = 0,
                ["damage"] = 0,
                ["reel"] = null,
                ["seenAt"] = clock,
            });
        }
        debris["nextId"] = next;
        return World.Load(root.ToJsonString(), modules());
    }

    private readonly DebrisView _view;
    private readonly Ui _ui;
    private readonly Func<(Client Client, Registry M)?> _world;
    private readonly Node2D _arrow = new();
    private Vector2 _pos = new(640, 300);
    private double _t, _breathed = -10, _stageAt;
    private int _stage, _engine = -1;
    private bool _holding;

    public SortieDemo(DebrisView view, Ui ui, Func<(Client Client, Registry M)?> world)
    {
        (_view, _ui, _world) = (view, ui, world);
        Layer = ScreenLayers.Pointer;
        AddChild(_arrow);
        _arrow.Draw += () =>
        {
            Vector2[] arrow = [_pos, _pos + new Vector2(0, 14), _pos + new Vector2(4, 11), _pos + new Vector2(9, 11)];
            _arrow.DrawColoredPolygon(arrow, Colors.White);
            _arrow.DrawPolyline([.. arrow, _pos], Colors.Black, 1);
        };
    }

    /// <summary>Where the engine block is on the screen; none once it is broken.</summary>
    private Vector2? Engine()
    {
        if (_engine < 0 && _view.Pieces.FirstOrDefault(p => p.Item == "engineBlock") is { } first) _engine = first.Id;
        return _view.Pieces.FirstOrDefault(p => p.Id == _engine) is { } p
            ? _view.GetViewport().GetCanvasTransform() * (new Vector2(p.X, p.Y) / 1000f * ShipCamera.Cell)
            : null;
    }

    private void Hold(bool held)
    {
        if (held == _holding) return;
        _holding = held;
        Input.ParseInputEvent(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            Pressed = held,
            ButtonMask = held ? MouseButtonMask.Left : 0,
            Position = _pos,
            GlobalPosition = _pos,
        });
    }

    private void Next(string said)
    {
        GD.Print($"[demo] {said} at {_t:0.00} s");
        (_stage, _stageAt) = (_stage + 1, _t);
    }

    public override void _Process(double delta)
    {
        _t += delta;
        if (_world() is var (client, m) && m.Crew.HeroOf(client.Id) is { } hero && hero.Air < m.Crew.CapacityOf(client.Id) / 2 && _t - _breathed > 2)
        {
            client.Send(new Sim.Crew.Refill());
            _breathed = _t;
        }
        if (_stage > 2) return;
        // On the engine's near end, where the bit reaches it.
        if (Engine() is { } at) _pos = _pos.MoveToward(at + new Vector2(-24, 0), (float)(1200 * delta));
        switch (_stage)
        {
            case 0 when _t >= 1.5:
                Input.ParseInputEvent(new InputEventKey { Keycode = Key.Key1, PhysicalKeycode = Key.Key1, Pressed = true });
                Input.ParseInputEvent(new InputEventKey { Keycode = Key.Key1, PhysicalKeycode = Key.Key1, Pressed = false });
                Next("drill chosen");
                break;
            case 1 when _t >= _stageAt + 1.0 && _ui.AimedPiece.Get() is (_, false):
                Hold(true);
                Next("drilling the engine block");
                break;
            case 2 when Engine() == null:
                Hold(false);
                _arrow.Visible = false;
                Next("the engine block broken: done, the mouse is the player's");
                return;
        }
        Input.ParseInputEvent(new InputEventMouseMotion { Position = _pos, GlobalPosition = _pos, ButtonMask = _holding ? MouseButtonMask.Left : 0 });
        _arrow.QueueRedraw();
    }
}
