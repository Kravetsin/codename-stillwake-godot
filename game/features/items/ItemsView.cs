using System;
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
/// as the hero walks over them, counted beside where they lay (<see cref="GainSpots"/>, <see cref="GainSpotsView"/>), as are what its own catches of debris gave
/// (<see cref="OwnCatches"/>) and what its digging, felling and taking apart gave (<see cref="OwnGains"/>), each also flying as a spark into the hero (<see cref="PickUpFlights"/>).
/// A crate or a furnace is opened by the crew's E, through <see cref="Ui.OpenCrate"/>.
/// </summary>
public partial class ItemsView : Node2D
{
    private readonly FloorLayer _floor = new();
    private readonly GainSpots _spots = new();
    private readonly GainSpotsView _spotsView = new();
    private readonly OwnGains _gains = new();
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
    public ItemsView(Ui ui)
    {
        Panel = new InventoryPanel(ui);
        Panel.StashedAway += parts => _stashed = parts;
    }

    /// <summary>What a stash just sent moved, to fly from the chest to its crates on the next frame.</summary>
    private IReadOnlyList<Stashed> _stashed = [];

    public override void _Ready()
    {
        // Over the machines, so a stack beside one shows, and under the heroes walking over it.
        ZIndex = Layers.FloorItems;
        AddChild(_floor);
        AddChild(_flights);
        AddChild(_pick);
        var overWorld = new CanvasLayer { Layer = ScreenLayers.Hud };
        overWorld.AddChild(_spotsView);
        AddChild(overWorld);
        AddChild(Panel);
    }

    /// <summary>
    /// Shows the client's world through the current registry's queries, a resync bringing new ones;
    /// <paramref name="hero"/> is none until the hero is placed. <paramref name="progress"/> is how far
    /// the world is between its last tick and the next; <paramref name="drift"/>, how far debris has drifted past it
    /// (<c>Session.Drift</c>). <paramref name="netHolds"/> is what a net holds, each piece where it is
    /// drawn, for a collect's loot to fly from (<see cref="OwnCatches"/>).
    /// </summary>
    public void Frame(
        Client client, IGridQueries grid, IRoomsQueries rooms, ItemsModule items, DebrisModule debris,
        Func<int, IReadOnlyList<(int Piece, (double X, double Y) At)>> netHolds, HeroSpot? hero, double progress, double drift, double elapsedMs)
    {
        (Hero, Grid) = (hero, grid);
        var taking = Reach.PickingUp(client.InFlight);
        // Passing out closes the inventory and lets go of the stack in hand.
        if (hero is { Out: true } && !_wasOut) Panel.Close();
        _wasOut = hero?.Out ?? false;
        // What the hero walks over, it picks up, saying so where it lay at once; passed out, nothing.
        // What its own catches just put into the inventory, by each piece too: a grab's or a net's collect's loot, drawn as it runs.
        var caught = _catches.Read(client.InFlight, piece => debris.CaughtFrom(piece, client.Id), piece => DrawnAt(debris, piece, drift), netHolds);
        // What digging, felling and taking apart gave, as the sim noted it: read hero or not, so none piles up while it is away.
        var gained = _gains.Read(items, items.Gains, client.Id);
        if (hero is { } spot)
        {
            var chest = new Vector2(spot.X, spot.Y - Content.Hero.Height * 0.6f) / 1000 * ShipCamera.Cell;
            // Every gain is counted beside where it came from and flies into the hero as a spark: icons flying hid the dig and the
            // catch, and a count far off left what came from which piece unclear. A catch's loot from where the piece was (0075).
            for (int i = 0; i < caught.Count; i++)
            {
                var (item, count, from) = caught[i];
                var at = from is var (fx, fy) ? new Vector2((float)fx, (float)fy) : chest;
                Gain(item, item, at, i, count);
            }
            for (int i = 0; i < gained.Count; i++)
            {
                var cell = new Vector2(gained[i].Cell % grid.Width + 0.5f, gained[i].Cell / grid.Width + 0.5f) * ShipCamera.Cell;
                Gain(gained[i].Item, gained[i].Item, cell, caught.Count + i, gained[i].Count);
            }
            IReadOnlyList<int> walkedOver = spot.Out ? [] : Reach.PickUpsOf(items, grid.Width, client.Id, spot.X, spot.Y, taking);
            // A body gives all it holds at once (2026-10-29-body-bag): one line for it all, its things streaming out of its chest.
            int streamed = 0;
            foreach (int id in walkedOver)
            {
                var stack = items.Floor.First(s => s.Id == id);
                int amount = items.PickUpAmount(client.Id, id);
                var from = new Vector2(stack.X, stack.Y) / 1000 * ShipCamera.Cell - new Vector2(0, 0.8f * ShipCamera.Cell);
                if (stack.Body != null) Gain(stack.Item, GainLines.Body, from, streamed++, amount);
                else Gain(stack.Item, stack.Item, from, 0, amount);
                client.Send(new PickUp(id));
            }
            if (walkedOver.Count > 0) _pick.Play(spot.Floats);
            // A stash flies out of the chest, each part to its crate in the plan's order (2026-11-06-quick-stash).
            for (int i = 0; i < _stashed.Count; i++)
                if (CentreOf(grid, _stashed[i].Crate) is { } crate) _flights.Fly(_stashed[i].Item, chest, i, crate);
            _spots.Update(elapsedMs);
            _spotsView.Show(_spots);
            _flights.Update(chest, elapsedMs);
        }
        _stashed = [];
        _floor.Update(grid, items, rooms, Reach.PickingUp(client.InFlight), client.World.Tick, progress, elapsedMs);
        Panel.Frame(client, items, grid, hero, elapsedMs);
    }

    /// <summary>
    /// <paramref name="count"/> of <paramref name="item"/> gained at <paramref name="at"/>, in pixels of the world: counted there on
    /// <paramref name="key"/>'s line, and a spark of it flying into the hero, the <paramref name="nth"/> of its catch.
    /// </summary>
    private void Gain(string item, string key, Vector2 at, int nth, int count)
    {
        _spots.Add(key, count, at.X / ShipCamera.Cell, at.Y / ShipCamera.Cell);
        _flights.Spark(item, at, nth);
    }

    /// <summary>The middle of machine <paramref name="id"/>'s footprint, in pixels of the world; none once it is gone.</summary>
    private static Vector2? CentreOf(IGridQueries grid, int id)
    {
        if (grid.MachineById(id) is not { } m) return null;
        var cells = grid.Footprint(m.Kind, m.Cell) ?? [m.Cell];
        var sum = Vector2.Zero;
        foreach (int c in cells) sum += new Vector2(c % grid.Width + 0.5f, c / grid.Width + 0.5f);
        return sum / cells.Count * ShipCamera.Cell;
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
