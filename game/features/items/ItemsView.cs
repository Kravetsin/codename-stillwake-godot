using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Research;
using Stillwake.Sim.Rooms;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// The player's items (features/items): the hotbar always, the rest and an open crate on demand
/// (<see cref="InventoryPanel"/>); the stacks lying on the floor (<see cref="FloorLayer"/>), picked up
/// as the hero walks over them, saying so over its head. A crate, a furnace or an analyzer is opened by
/// the crew's E, through <see cref="Ui.OpenCrate"/>.
/// </summary>
public partial class ItemsView : Node2D
{
    private readonly FloorLayer _floor = new();
    private readonly PickUpPopups _popups = new();
    private readonly ActSound _pick = new();

    /// <summary>Where the hero stood last frame, and the grid read then: for scripted runs.</summary>
    public HeroSpot? Hero { get; private set; }
    public IGridQueries? Grid { get; private set; }

    /// <summary>The hotbar, the inventory and the open crate.</summary>
    public InventoryPanel Panel { get; }

    private readonly bool _demo;

    /// <param name="ui">The hand, which the hotbar frees, and the crate open.</param>
    /// <param name="demo">Plays <see cref="ItemsDemo"/>'s script, for recordings.</param>
    public ItemsView(Ui ui, bool demo = false)
    {
        Panel = new InventoryPanel(ui);
        _demo = demo;
    }

    public override void _Ready()
    {
        // Over the machines, so a stack beside one shows, and under the heroes walking over it.
        ZIndex = Layers.FloorItems;
        AddChild(_floor);
        AddChild(_pick);
        var overWorld = new CanvasLayer { Layer = ScreenLayers.Hud };
        overWorld.AddChild(_popups);
        AddChild(overWorld);
        AddChild(Panel);
        if (!_demo) return;
        var pointer = new CanvasLayer { Layer = ScreenLayers.Pointer };
        pointer.AddChild(new ItemsDemo(this));
        AddChild(pointer);
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
        (Hero, Grid) = (hero, grid);
        var taking = Reach.PickingUp(client.InFlight);
        // What the hero walks over, it picks up, saying so over its head at once.
        if (hero is { } spot)
        {
            foreach (int id in Reach.PickUpsOf(items, grid.Width, client.Id, spot.X, spot.Y, taking))
            {
                var stack = items.Floor.First(s => s.Id == id);
                _popups.Add(stack.Item, items.PickUpAmount(client.Id, id));
                client.Send(new PickUp(id));
                _pick.Play(spot.Floats);
            }
            _popups.Update(elapsedMs, spot.X, spot.Y);
        }
        _floor.Update(items, rooms, Reach.PickingUp(client.InFlight), progress, elapsedMs);
        Panel.Frame(client, items, grid, research, hero, elapsedMs);
    }
}
