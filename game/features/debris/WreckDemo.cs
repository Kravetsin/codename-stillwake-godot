using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Debris;

/// <summary>
/// <c>-- --wreck</c>: the hero floats out in space with a fire axe in the second hotbar slot, a hull
/// section drifting slowly up to it, three more behind it already chopped a third, a half and four
/// fifths of the way to show the cracks; and a script played through Godot's own input, for recordings
/// with <c>--write-movie</c>: the pointer on the section, bare-handed (outlined red, too big), the axe
/// chosen with 2, the left button held till the section breaks, then the hero swims through what it
/// broke into. It draws the pointer, which a movie does not.
/// </summary>
public partial class WreckDemo : CanvasLayer
{
    /// <summary>A scrap of hull far to the left, so debris has a ship to drift past; the hero wakes out in space at S.</summary>
    public static readonly Layout Map = Layout.Parse(
        [
            new string(' ', 120),
            "",
            "",
            "",
            "",
            "  #####",
            "  #...#",
            "  #...#                                S",
            "  #####",
            "",
            "",
            "",
            "",
            "",
            "",
            "",
        ]) with
    { Known = Spec.Research.StartKnown };

    /// <summary>The section chopped, and the others: id, cells right of and below the hero's cell, damage.</summary>
    private const int Target = 1;
    private static readonly (int Id, string Item, double Right, int Below, int Damage)[] Drifting =
    [
        (Target, "hullSection", 4.5, -1, 0),
        (2, "hullSection", 10.5, -5, 5),
        (3, "hullSection", 11.5, -1, 8),
        (4, "hullSection", 10.5, 3, 12),
        (5, "scrapChunk", 7.5, -4, 0),
        (6, "hullPlate", 15.5, 1, 0),
    ];
    /// <summary>How fast they drift, slower than the sim's own, so the section stays in reach as it is chopped.</summary>
    private const int Speed = 12;

    /// <summary>
    /// <paramref name="world"/>, on <see cref="Map"/>, with the pieces out and the axe in peer 0's second
    /// hotbar slot: put into its save, as no command makes items or places debris.
    /// </summary>
    public static World Seed(World world, Func<IReadOnlyList<Module>> modules)
    {
        var root = JsonNode.Parse(world.Save())!;
        int width = Map.Width, spawn = Map.CellsOf('S').First();
        int sx = spawn % width, sy = spawn / width;
        const int Right = 6, Left = 2, Lead = Spec.Debris.Lead;
        int start = Math.Min(width - 1, Right + Lead) * 1000 + 500, end = Math.Max(0, Left - Lead) * 1000;
        var pieces = new JsonArray();
        foreach (var (id, item, right, below, damage) in Drifting)
            pieces.Add(new JsonObject
            {
                ["id"] = id,
                ["item"] = item,
                ["x"] = (int)((sx + right) * 1000),
                ["y"] = (sy + below) * 1000 + 500,
                ["speed"] = Speed,
                ["start"] = start,
                ["end"] = end,
                ["damage"] = damage,
            });
        // Waking in space, the hero leaves no back wall where it woke.
        var grid = root["slices"]!["grid"]!;
        grid["back"]![spawn] = 0;
        grid["backMaterial"]![spawn] = 0;
        var debris = root["slices"]!["debris"]!;
        debris["pieces"] = pieces;
        debris["nextId"] = Drifting.Length + 1;
        var slots = new JsonArray();
        for (int i = 0; i < Spec.Inventory.Slots; i++)
            slots.Add(i == 1 ? new JsonObject { ["item"] = Spec.Axe.Item, ["count"] = 1 } : null);
        root["slices"]!["items"]!["inventories"] = new JsonArray(new JsonObject { ["id"] = 0, ["slots"] = slots });
        return World.Load(root.ToJsonString(), modules());
    }

    private readonly DebrisView _view;
    private readonly Ui _ui;
    /// <summary>The pointer, drawn over everything on the screen.</summary>
    private readonly Node2D _arrow = new();
    private Vector2 _pos = new(640, 300);
    private double _t;
    private bool _chose, _holding, _broken;
    private double _brokeAt;
    private bool _swimming, _swum, _done;

    public WreckDemo(DebrisView view, Ui ui)
    {
        _view = view;
        _ui = ui;
        Layer = ScreenLayers.Pointer;
        AddChild(_arrow);
        _arrow.Draw += () =>
        {
            Vector2[] arrow = [_pos, _pos + new Vector2(0, 14), _pos + new Vector2(4, 11), _pos + new Vector2(9, 11)];
            _arrow.DrawColoredPolygon(arrow, Colors.White);
            _arrow.DrawPolyline([.. arrow, _pos], Colors.Black, 1);
        };
    }

    /// <summary>Where piece <paramref name="id"/> is on the screen; none once it is gone.</summary>
    private Vector2? OnScreen(int id) =>
        _view.Pieces.FirstOrDefault(p => p.Id == id) is { } p
            ? _view.GetViewport().GetCanvasTransform() * (new Vector2(p.X, p.Y) / 1000f * ShipCamera.Cell)
            : null;

    private static void Press(Key key, bool pressed) =>
        Input.ParseInputEvent(new InputEventKey { Keycode = key, PhysicalKeycode = key, Pressed = pressed });

    private void Mouse(bool held) =>
        Input.ParseInputEvent(new InputEventMouseButton
        {
            ButtonIndex = MouseButton.Left,
            Pressed = held,
            ButtonMask = held ? MouseButtonMask.Left : 0,
            Position = _pos,
            GlobalPosition = _pos,
        });

    public override void _Process(double delta)
    {
        if (_done) return;
        _t += delta;
        var target = OnScreen(Target);
        if (target is { } at && !_broken) _pos = _pos.MoveToward(at, (float)(1500 * delta));
        // Bare-handed first, outlined red; then the axe.
        if (!_chose && _t >= 2.2)
        {
            Press(Key.Key2, true);
            Press(Key.Key2, false);
            _chose = true;
            GD.Print($"[demo] axe chosen at {_t:0.00} s");
        }
        if (_chose && !_holding && !_broken && _t >= 3.0 && _ui.AimedPiece.Get() is (Target, false))
        {
            Mouse(true);
            _holding = true;
            GD.Print($"[demo] chopping from {_t:0.00} s");
        }
        if (_holding && target == null)
        {
            Mouse(false);
            (_holding, _broken, _brokeAt) = (false, true, _t);
            GD.Print($"[demo] section broken at {_t:0.00} s");
        }
        // Then it swims to what the section broke into, drifting back past the ship toward it.
        if (_broken && !_swimming && !_swum && _t >= _brokeAt + 0.6)
        {
            _swimming = true;
            Press(Key.D, true);
        }
        if (_swimming && _t >= _brokeAt + 2.6)
        {
            Press(Key.D, false);
            (_swimming, _swum) = (false, true);
        }
        // The script played, the mouse is the player's again: it no longer pins the pointer where it ended.
        if (_swum && _t >= _brokeAt + 3.5)
        {
            _done = true;
            _arrow.Visible = false;
            GD.Print($"[demo] done at {_t:0.00} s: the mouse is the player's");
            return;
        }
        Input.ParseInputEvent(new InputEventMouseMotion { Position = _pos, GlobalPosition = _pos, ButtonMask = _holding ? MouseButtonMask.Left : 0 });
        _arrow.QueueRedraw();
    }
}
