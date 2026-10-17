using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Nodes;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;

namespace Stillwake.Game.Features.Debris;

/// <summary>
/// <c>-- --autopilot --debris</c>: the wreck with debris already drifting by around it, a cargo crate
/// among it on its way to where the wreck's autopilot leaves the hero, floating outside the airlock;
/// once it waits there, the pointer goes to the crate and E catches it, the crate opening into planks
/// in the hotbar. Played through Godot's own input, for recordings with <c>--write-movie</c>; it draws
/// the pointer, which a movie does not.
/// </summary>
public partial class DebrisDemo : CanvasLayer
{
    /// <summary>The cargo crate the hero catches: its id, row and column at the start, and speed.</summary>
    private const int Crate = 1, CrateRow = 15, CrateSpeed = 40;
    private const double CrateX = 82.5;
    /// <summary>When the autopilot has the hero waiting outside the airlock, in seconds.</summary>
    private const double FollowAt = 12.6;

    /// <summary>
    /// <paramref name="world"/>, the wreck, with pieces of debris out on their way past it: put into its
    /// save, as the sim spawns them only at the right of its lead, far out of sight.
    /// </summary>
    public static World Seed(World world, Func<IReadOnlyList<Module>> modules)
    {
        var root = JsonNode.Parse(world.Save())!;
        int width = Registry.WreckMap.Width;
        // Where the sim would start and end them: the lead right and left of the hull, kept on the grid.
        const int Right = 57, Left = 34, Lead = Stillwake.Content.Debris.Lead;
        int start = Math.Min(width - 1, Right + Lead) * 1000 + 500, end = Math.Max(0, Left - Lead) * 1000;
        (string Item, double X, int Row, int Speed)[] drifting =
        [
            ("cargoCrate", CrateX, CrateRow, CrateSpeed),
            ("scrapChunk", 62.5, 12, 52),
            ("hullPlate", 70.5, 16, 33),
            ("scrapChunk", 40.5, 9, 45),
            ("supplyPod", 76.5, 27, 30),
            ("scrapChunk", 58.5, 30, 55),
            ("hullPlate", 31.5, 25, 38),
            ("scrapChunk", 84.5, 19, 47),
        ];
        var pieces = new JsonArray();
        for (int i = 0; i < drifting.Length; i++)
        {
            var (item, x, row, speed) = drifting[i];
            pieces.Add(new JsonObject
            {
                ["id"] = i + 1,
                ["item"] = item,
                ["x"] = (int)(x * 1000),
                ["y"] = row * 1000 + 500,
                ["speed"] = speed,
                ["start"] = start,
                ["end"] = end,
            });
        }
        var debris = root["slices"]!["debris"]!;
        debris["pieces"] = pieces;
        debris["nextId"] = drifting.Length + 1;
        return World.Load(root.ToJsonString(), modules());
    }

    private readonly DebrisView _view;
    private readonly Ui _ui;
    /// <summary>The pointer, drawn over everything on the screen.</summary>
    private readonly Node2D _arrow = new();
    private Vector2 _pos = new(640, 300), _target = new(640, 300);
    private double _t;
    private bool _caught;

    public DebrisDemo(DebrisView view, Ui ui)
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
    private Vector2? PieceOnScreen(int id) =>
        _view.Pieces.FirstOrDefault(p => p.Id == id) is { } p
            ? _view.GetViewport().GetCanvasTransform() * (new Vector2(p.X, p.Y) / 1000f * ShipCamera.Cell)
            : null;

    public override void _Process(double delta)
    {
        _t += delta;
        // Once the hero waits out by the airlock, the pointer goes to the crate, and E catches it in reach.
        if (_t >= FollowAt && PieceOnScreen(Crate) is { } crate) _target = crate;
        if (!_caught && _ui.AimedPiece.Get() is (Crate, false) && _pos.DistanceTo(_target) < 2)
        {
            Input.ParseInputEvent(new InputEventKey { Keycode = Key.E, PhysicalKeycode = Key.E, Pressed = true });
            Input.ParseInputEvent(new InputEventKey { Keycode = Key.E, PhysicalKeycode = Key.E, Pressed = false });
            _caught = true;
            GD.Print($"[demo] caught cargo crate {Crate} at {_t:0.00} s");
        }
        if (_pos != _target)
        {
            _pos = _pos.MoveToward(_target, (float)(3000 * delta));
            Input.ParseInputEvent(new InputEventMouseMotion { Position = _pos, GlobalPosition = _pos });
        }
        _arrow.QueueRedraw();
    }
}
