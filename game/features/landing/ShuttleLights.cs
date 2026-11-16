using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Landing;
using Stillwake.Sim.Places;

namespace Stillwake.Game.Features.Landing;

/// <summary>
/// Each shuttle's slits, without a word: slit i lit warm while place i is taken, its rider never seen behind the frost; and its
/// course lamp, lit while a course is set (shuttle-flight). Placeholders till they are picked: a warm bar over each slit, a small
/// round lamp. The seats, the course and the shuttles are compared every frame, a handful at most, and drawn again only on a change.
/// </summary>
public partial class ShuttleLights : Node2D
{
    /// <summary>The slits' left edges in the shuttle's picture, in pixels from its corner, slit i place i (<c>tools/anime-art/subjects/shuttle.js</c>).</summary>
    private static readonly float[] Slits = [99.4f, 115.8f, 132.2f, 148.6f];
    private static readonly Vector2 SlitSize = new(13, 4.6f);
    private const float SlitTop = 23.6f;
    private static readonly Color Warm = new(1f, 0.74f, 0.38f, 0.9f), Lamp = new("6fe08a"), Dark = new("2a3a30");
    /// <summary>The course lamp's middle in the shuttle's picture, in pixels from its corner, and its radius: a placeholder spot by its nose.</summary>
    private static readonly Vector2 LampAt = new(176, 24);
    private const float LampRadius = 3;

    private readonly IGridQueries _grid;
    private readonly LandingModule _landing;
    private readonly PlacesModule _places;
    private Seat[] _seen = [];
    private int[] _shuttles = [];
    private bool _course;

    public ShuttleLights(IGridQueries grid, LandingModule landing, PlacesModule places)
    {
        _grid = grid;
        _landing = landing;
        _places = places;
        ZIndex = Layers.Screens;
        // Lit from inside: the slits glow in the dark as in the lamp's light.
        Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
    }

    public override void _Process(double delta)
    {
        bool course = _places.Course != null;
        var shuttles = _grid.MachinesOf(Shuttle);
        if (_landing.Seats.SequenceEqual(_seen) && course == _course && shuttles.Select(m => m.Cell).SequenceEqual(_shuttles)) return;
        (_seen, _course, _shuttles) = ([.. _landing.Seats], course, [.. shuttles.Select(m => m.Cell)]);
        QueueRedraw();
    }

    /// <summary>The kind of machine with a course lamp: the crew shuttle.</summary>
    private const string Shuttle = "crewShuttle";

    /// <summary>Where the picture of a machine of <paramref name="kind"/> standing from <paramref name="cell"/> has its corner: it rises over its footprint as a tall machine's does (0099).</summary>
    private Vector2 Corner(string kind, int cell) => new Vector2(cell % _grid.Width, cell / _grid.Width - ShipTiles.RiseOf(kind)) * ShipCamera.Cell;

    public override void _Draw()
    {
        foreach (int cell in _shuttles)
        {
            var at = Corner(Shuttle, cell) + LampAt;
            DrawCircle(at, LampRadius + 1, Colors.Black with { A = 0.6f });
            DrawCircle(at, LampRadius, _course ? Lamp : Dark);
        }
        foreach (var seat in _seen)
        {
            if (_grid.MachineById(seat.Pod) is not { } shuttle || Stillwake.Content.Machines.Get(shuttle.Kind).Riders == 1 || seat.Place >= Slits.Length) continue;
            var corner = Corner(shuttle.Kind, shuttle.Cell);
            DrawRect(new Rect2(corner + new Vector2(Slits[seat.Place], SlitTop), SlitSize), Warm);
        }
    }
}
