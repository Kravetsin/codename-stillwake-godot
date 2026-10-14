using System;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Net;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Research;
using Stillwake.Sim.Rooms;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// The player's items (features/items): the hotbar always, the rest and an open crate on demand
/// (<see cref="InventoryPanel"/>); the stacks lying on the floor (<see cref="FloorLayer"/>), picked up
/// as the hero walks over them, saying so over its head; E on a crate, a furnace or an analyzer within
/// reach opens it beside the inventory, outlined while it would, and it closes out of reach.
/// </summary>
public partial class ItemsView : Node2D
{
    private static readonly Color AimReady = new("7fe08a");

    private readonly FloorLayer _floor = new();
    private readonly PickUpPopups _popups = new();
    private bool _use;
    private Vector2 _mouse = new(float.NaN, float.NaN);
    private int? _aimed;
    private int _width;

    /// <summary>Where the hero stood last frame, and the grid read then: for scripted runs.</summary>
    public HeroSpot? Hero { get; private set; }
    public IGridQueries? Grid { get; private set; }

    /// <summary>The hotbar, the inventory and the open crate.</summary>
    public InventoryPanel Panel { get; } = new();

    private readonly bool _demo;

    /// <param name="demo">Plays <see cref="ItemsDemo"/>'s script, for recordings.</param>
    public ItemsView(bool demo = false) => _demo = demo;

    public override void _Ready()
    {
        // Over the machines, so a stack beside one and the outline of an aimed crate show; the outline unlit.
        ZIndex = 4;
        Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
        AddChild(_floor);
        var overWorld = new CanvasLayer();
        overWorld.AddChild(_popups);
        AddChild(overWorld);
        AddChild(Panel);
        if (_demo) Panel.AddChild(new ItemsDemo(this));
    }

    /// <summary>
    /// Shows the client's world through the current registry's queries, a resync bringing new ones;
    /// <paramref name="hero"/> is none until the hero is placed. <paramref name="progress"/> is how far
    /// the world is between its last tick and the next.
    /// </summary>
    public void Frame(
        Client client, IGridQueries grid, IRoomsQueries rooms, IItemsQueries items, IResearchQueries research,
        HeroSpot? hero, double progress, double elapsedMs)
    {
        (_width, Hero, Grid) = (grid.Width, hero, grid);
        var taking = Reach.PickingUp(client.InFlight);
        // What the hero walks over, it picks up, saying so over its head at once.
        if (hero is { } spot)
        {
            foreach (int id in Reach.PickUpsOf(items, grid.Width, client.Id, spot.X, spot.Y, taking))
            {
                var stack = items.Floor.First(s => s.Id == id);
                _popups.Add(stack.Item, items.PickUpAmount(client.Id, id));
                client.Send(new PickUp(id));
            }
            _popups.Update(elapsedMs, spot.X, spot.Y);
        }
        _floor.Update(items, rooms, Reach.PickingUp(client.InFlight), progress, elapsedMs);

        // E on what holds items, within reach, opens it; it stays open while the hero can reach it.
        _aimed = null;
        if (hero is { } h && Pointer() is { } p)
        {
            int x = (int)Math.Floor(p.X), y = (int)Math.Floor(p.Y);
            int cell = y * grid.Width + x;
            if (x >= 0 && y >= 0 && x < grid.Width && y < grid.Height && h.Reaches(cell) && Reach.CrateAt(grid, cell) is int crate)
            {
                _aimed = cell;
                if (_use) Panel.Open(crate);
            }
        }
        _use = false;
        if (Panel.Crate is int open && (hero is not { } at || !Reach.CrateInReach(grid, open, at.Reaches))) Panel.CloseCrate();
        Panel.Frame(client, items, grid, research, hero, elapsedMs);
        QueueRedraw();
    }

    /// <summary>The world point under the pointer, in cells; none before the pointer has moved over the window.</summary>
    private Vector2? Pointer() =>
        float.IsNaN(_mouse.X) ? null : GetViewport().GetCanvasTransform().AffineInverse() * _mouse / ShipCamera.Cell;

    public override void _Input(InputEvent e)
    {
        if (e is InputEventMouse mouse) _mouse = mouse.Position;
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.E }) _use = true;
    }

    public override void _Draw()
    {
        if (_aimed is not int cell) return;
        float size = ShipCamera.Cell;
        var at = new Vector2(cell % _width, cell / _width) * size;
        DrawRect(new Rect2(at + Vector2.One, new Vector2(size - 2, size - 2)), AimReady, false, 2);
    }
}
