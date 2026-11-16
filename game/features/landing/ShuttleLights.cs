using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Places;

namespace Stillwake.Game.Features.Landing;

/// <summary>
/// Each shuttle's course lamp, without a word: lit while a course is set (shuttle-flight), a picture of the shuttle's whole frame
/// with only the lamp painted (<c>crewShuttle_course</c>). Its canopy never changes, whoever sits behind it (shuttle-desant). The
/// course and the shuttles are compared every frame, a handful at most, and drawn again only on a change. A shuttle held by
/// <see cref="Arrivals"/> is drawn moving by <see cref="PodFlights"/>: not here.
/// </summary>
public partial class ShuttleLights : Node2D
{
    /// <summary>The kind of machine with a course lamp: the crew shuttle.</summary>
    private const string Shuttle = "crewShuttle";
    private static readonly Texture2D Course = PixelArt.Picture($"{Shuttle}_course");

    private readonly IGridQueries _grid;
    private readonly PlacesModule _places;
    private readonly Arrivals? _arrivals;
    private int[] _shuttles = [];
    private bool _course;
    private int _held;

    public ShuttleLights(IGridQueries grid, PlacesModule places, Arrivals? arrivals = null)
    {
        _grid = grid;
        _places = places;
        _arrivals = arrivals;
        ZIndex = Layers.Screens;
        // Lit from inside: the lamp glows in the dark as in the lamp's light.
        Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
    }

    public override void _Process(double delta)
    {
        bool course = _places.Course != null;
        var shuttles = _grid.MachinesOf(Shuttle);
        int held = _arrivals?.Version ?? 0;
        if (course == _course && held == _held && shuttles.Select(m => m.Cell).SequenceEqual(_shuttles)) return;
        (_course, _held, _shuttles) = (course, held, [.. shuttles.Select(m => m.Cell)]);
        QueueRedraw();
    }

    /// <summary>Where the picture of a machine of <paramref name="kind"/> standing from <paramref name="cell"/> has its corner: it rises over its footprint as a tall machine's does (0099).</summary>
    private Vector2 Corner(string kind, int cell) => new Vector2(cell % _grid.Width, cell / _grid.Width - ShipTiles.RiseOf(kind)) * ShipCamera.Cell;

    public override void _Draw()
    {
        if (!_course) return;
        foreach (int cell in _shuttles)
            if (_grid.MachineAt(cell) is not { } m || _arrivals?.Holds(m.Id) != true) DrawTexture(Course, Corner(Shuttle, cell));
    }
}
