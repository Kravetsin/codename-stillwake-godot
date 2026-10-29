using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim.Debris;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Items;
using Stillwake.Sim.Research;
using Stillwake.Sim.Rooms;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// The player's items (features/items): the hotbar always, the rest and an open crate on demand
/// (<see cref="InventoryPanel"/>); the stacks lying on the floor (<see cref="FloorLayer"/>), picked up
/// as the hero walks over them, saying so over its head, as it says what its own catches of debris gave
/// (<see cref="OwnCatches"/>), each flying home to its chest on a ribbon of light (<see cref="PickUpFlights"/>).
/// A crate, a furnace or an analyzer is opened by the crew's E, through <see cref="Ui.OpenCrate"/>.
/// </summary>
public partial class ItemsView : Node2D
{
    private readonly FloorLayer _floor = new();
    private readonly PickUpPopups _popups = new();
    private readonly PickUpFlights _flights = new();
    private readonly OwnCatches _catches = new();
    private readonly ActSound _pick = new();

    /// <summary>Where the hero stood last frame, and the grid read then: for scripted runs.</summary>
    public HeroSpot? Hero { get; private set; }
    public IGridQueries? Grid { get; private set; }

    /// <summary>The hotbar, the inventory and the open crate.</summary>
    public InventoryPanel Panel { get; }

    private bool _wasOut;

    /// <param name="ui">The hand, which the hotbar frees, and the crate open.</param>
    public ItemsView(Ui ui) => Panel = new InventoryPanel(ui);

    public override void _Ready()
    {
        // Over the machines, so a stack beside one shows, and under the heroes walking over it.
        ZIndex = Layers.FloorItems;
        AddChild(_floor);
        AddChild(_flights);
        AddChild(_pick);
        var overWorld = new CanvasLayer { Layer = ScreenLayers.Hud };
        overWorld.AddChild(_popups);
        AddChild(overWorld);
        AddChild(Panel);
    }

    /// <summary>
    /// Shows the client's world through the current registry's queries, a resync bringing new ones;
    /// <paramref name="hero"/> is none until the hero is placed. <paramref name="progress"/> is how far
    /// the world is between its last tick and the next.
    /// </summary>
    public void Frame(
        Client client, IGridQueries grid, IRoomsQueries rooms, IItemsQueries items, IResearchQueries research, DebrisModule debris,
        HeroSpot? hero, double progress, double elapsedMs)
    {
        (Hero, Grid) = (hero, grid);
        var taking = Reach.PickingUp(client.InFlight);
        // Passing out closes the inventory and lets go of the stack in hand.
        if (hero is { Out: true } && !_wasOut) Panel.Close();
        _wasOut = hero?.Out ?? false;
        // What the hero walks over, it picks up, saying so over its head at once; passed out, nothing.
        // What its own catches just put into the inventory, over its head too: a grab's loot is drawn as it runs.
        var caught = _catches.Read(client.InFlight, debris.CaughtFrom, piece => DrawnAt(debris, piece, progress));
        if (hero is { } spot)
        {
            // A catch's loot flies home from where the piece was, one icon after another (0075).
            for (int i = 0; i < caught.Count; i++)
            {
                var (item, count, from) = caught[i];
                _popups.Add(item, count);
                if (from is var (fx, fy)) _flights.Fly(item, new Vector2((float)fx, (float)fy), i);
            }
            IReadOnlyList<int> walkedOver = spot.Out ? [] : Reach.PickUpsOf(items, grid.Width, client.Id, spot.X, spot.Y, taking);
            // A body gives all it holds at once (2026-10-29-body-bag): one line for it all, its things streaming out of its chest.
            int fromBody = 0, streamed = 0;
            foreach (int id in walkedOver)
            {
                var stack = items.Floor.First(s => s.Id == id);
                int amount = items.PickUpAmount(client.Id, id);
                var from = new Vector2(stack.X, stack.Y) / 1000 * ShipCamera.Cell - new Vector2(0, 0.8f * ShipCamera.Cell);
                if (stack.Body != null)
                {
                    fromBody += amount;
                    _flights.Fly(stack.Item, from, streamed++);
                }
                else
                {
                    _popups.Add(stack.Item, amount);
                    _flights.Fly(stack.Item, from);
                }
                client.Send(new PickUp(id));
            }
            if (fromBody > 0) _popups.AddFromBody(fromBody);
            if (walkedOver.Count > 0) _pick.Play(spot.Floats);
            _popups.Update(elapsedMs, spot.X, spot.Y);
            _flights.Update(new Vector2(spot.X, spot.Y - Content.Hero.Height * 0.6f) / 1000 * ShipCamera.Cell, elapsedMs);
        }
        _floor.Update(grid, items, rooms, Reach.PickingUp(client.InFlight), client.World.Tick, progress, elapsedMs);
        Panel.Frame(client, items, grid, research, hero, elapsedMs);
    }

    /// <summary>Where piece <paramref name="id"/> is drawn now, in pixels of the world; none once it is gone.</summary>
    private static (double X, double Y)? DrawnAt(DebrisModule debris, int id, double progress)
    {
        var pieces = debris.Pieces;
        for (int i = 0; i < pieces.Count; i++)
        {
            if (pieces[i].Id != id) continue;
            var (x, y) = PieceDrawn.At(pieces[i], progress);
            return (x / DebrisModule.Unit * ShipCamera.Cell, y / DebrisModule.Unit * ShipCamera.Cell);
        }
        return null;
    }
}
