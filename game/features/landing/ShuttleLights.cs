using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim.Grid;
using Stillwake.Sim.Places;

namespace Stillwake.Game.Features.Landing;

/// <summary>
/// Each shuttle's course lamp, without a word: lit while a course is set (shuttle-flight), a picture of the shuttle's whole frame
/// with only the lamp painted (<c>crewShuttle_course</c>). Its canopy never changes, whoever sits behind it (shuttle-desant). The
/// course and the shuttles are compared every frame, a handful at most, and drawn again only on a change.
/// </summary>
public partial class ShuttleLights : Node2D
{
    /// <summary>The kind of machine with a course lamp: the crew shuttle.</summary>
    private const string Shuttle = "crewShuttle";
    private static readonly Texture2D Course = PixelArt.Picture($"{Shuttle}_course");

    private readonly IGridQueries _grid;
    private readonly PlacesModule _places;
    private int[] _shuttles = [];
    private bool _course;

    public ShuttleLights(IGridQueries grid, PlacesModule places)
    {
        _grid = grid;
        _places = places;
        ZIndex = Layers.Screens;
        // Lit from inside: the lamp glows in the dark as in the lamp's light.
        Material = new CanvasItemMaterial { LightMode = CanvasItemMaterial.LightModeEnum.Unshaded };
    }

    public override void _Process(double delta)
    {
        bool course = _places.Course != null;
        var shuttles = _grid.MachinesOf(Shuttle);
        if (course == _course && shuttles.Select(m => m.Cell).SequenceEqual(_shuttles)) return;
        (_course, _shuttles) = (course, [.. shuttles.Select(m => m.Cell)]);
        QueueRedraw();
    }

    /// <summary>Where the picture of a machine of <paramref name="kind"/> standing from <paramref name="cell"/> has its corner: it rises over its footprint as a tall machine's does (0099).</summary>
    private Vector2 Corner(string kind, int cell) => new Vector2(cell % _grid.Width, cell / _grid.Width - ShipTiles.RiseOf(kind)) * ShipCamera.Cell;

    public override void _Draw()
    {
        if (_course)
            foreach (int cell in _shuttles) DrawTexture(Course, Corner(Shuttle, cell));
    }
}
