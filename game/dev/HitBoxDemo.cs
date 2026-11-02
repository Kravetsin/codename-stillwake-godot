using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Creatures;

namespace Stillwake.Game.Dev;

/// <summary>
/// <c>-- --hitboxes</c>, with whatever shows creatures: the box each creature is hit in, drawn over it, to hold against its picture.
/// </summary>
public partial class HitBoxDemo(Func<(Client Client, Registry M)?> world) : Node2D
{
    private static readonly Color Line = new("ff3df0");

    public override void _Ready()
    {
        ZIndex = 4000;
        Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
    }

    public override void _Process(double delta) => QueueRedraw();

    public override void _Draw()
    {
        if (world() is not { } w) return;
        float pixels = (float)ShipCamera.Cell / CreaturesModule.Unit;
        foreach (var c in w.M.Creatures.Creatures)
        {
            var body = Content.Creatures.Get(c.Species).Body;
            var size = new Vector2(body.Width, body.Height) * CreaturesModule.Unit * pixels;
            DrawRect(new Rect2(new Vector2(c.X, c.Y) * pixels - new Vector2(size.X / 2, size.Y), size), Line, false, 0.5f);
        }
    }
}
