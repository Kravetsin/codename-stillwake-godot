using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Landing;
using Stillwake.Sim.Places;

namespace Stillwake.Game.Features.Landing;

/// <summary>
/// Each shuttle's canopy, without a word: pane i lit warm while place i is taken, its rider never seen behind the tint; and its
/// course lamp, lit while a course is set (shuttle-flight). Each is a picture of the shuttle's whole frame with only that light
/// painted (<c>crewShuttle_seat_{i}</c>, <c>crewShuttle_course</c>). The seats, the course and the shuttles are compared every
/// frame, a handful at most, and drawn again only on a change. A shuttle held by <see cref="Arrivals"/>, or in flight, is drawn
/// moving by <see cref="PodFlights"/>, its panes with it: not here.
/// </summary>
public partial class ShuttleLights : Node2D
{
    /// <summary>The kind of machine with these lights: the crew shuttle.</summary>
    private const string Shuttle = "crewShuttle";
    private static readonly Texture2D[] Seats = [.. Enumerable.Range(0, 4).Select(i => PixelArt.Picture($"{Shuttle}_seat_{i}"))];
    private static readonly Texture2D Course = PixelArt.Picture($"{Shuttle}_course");

    private readonly IGridQueries _grid;
    private readonly LandingModule _landing;
    private readonly PlacesModule _places;
    private readonly Arrivals? _arrivals;
    private Seat[] _seen = [];
    private int[] _shuttles = [];
    private bool _course;
    private int _held;

    public ShuttleLights(IGridQueries grid, LandingModule landing, PlacesModule places, Arrivals? arrivals = null)
    {
        _arrivals = arrivals;
        _grid = grid;
        _landing = landing;
        _places = places;
        ZIndex = Layers.Screens;
        // Lit from inside: the panes glow in the dark as in the lamp's light.
        Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
    }

    public override void _Process(double delta)
    {
        bool course = _places.Course != null;
        var shuttles = _grid.MachinesOf(Shuttle);
        int held = _arrivals?.Version ?? 0;
        if (_landing.Seats.SequenceEqual(_seen) && course == _course && held == _held && shuttles.Select(m => m.Cell).SequenceEqual(_shuttles)) return;
        (_seen, _course, _held, _shuttles) = ([.. _landing.Seats], course, held, [.. shuttles.Select(m => m.Cell)]);
        QueueRedraw();
    }

    /// <summary>Where the picture of a machine of <paramref name="kind"/> standing from <paramref name="cell"/> has its corner: it rises over its footprint as a tall machine's does (0099).</summary>
    private Vector2 Corner(string kind, int cell) => new Vector2(cell % _grid.Width, cell / _grid.Width - ShipTiles.RiseOf(kind)) * ShipCamera.Cell;

    public override void _Draw()
    {
        if (_course)
            foreach (int cell in _shuttles)
                if (_grid.MachineAt(cell) is not { } m || _arrivals?.Holds(m.Id) != true) DrawTexture(Course, Corner(Shuttle, cell));
        foreach (var seat in _seen)
        {
            if (_grid.MachineById(seat.Pod) is not { Kind: Shuttle } shuttle || seat.Place >= Seats.Length || _arrivals?.Holds(shuttle.Id) == true) continue;
            DrawTexture(Seats[seat.Place], Corner(shuttle.Kind, shuttle.Cell));
        }
    }
}
