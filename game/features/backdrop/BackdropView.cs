using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Content;
using Stillwake.Game.Render;

namespace Stillwake.Game.Features.Backdrop;

/// <summary>
/// A planet's land behind its ground (composed-backdrops), drawn by <see cref="Stars"/> over its sky (<see cref="PlanetSky"/>):
/// <see cref="BackdropPlan"/>'s layers far to near, each following its share of the camera across and from the ground line,
/// in screen space, a pixel of a layer <c>zoom</c> pixels of the screen.
/// </summary>
public partial class BackdropView : Node2D
{
    private readonly List<BackdropLayer> _layers = [];
    private readonly IReadOnlyList<BackdropPlan.Layer> _plan;

    private BackdropView(IReadOnlyList<BackdropPlan.Layer> plan, (float R, float G, float B) haze)
    {
        _plan = plan;
        var atlas = new BackdropAtlas(plan.SelectMany(l => l.Draws).OfType<BackdropPlan.Picture>().Select(p => p.Name));
        foreach (var layer in plan)
        {
            var drawn = new BackdropLayer(layer, atlas, BackdropLooks.First.Tile, haze, BackdropLooks.First.Dark);
            AddChild(drawn);
            _layers.Add(drawn);
        }
    }

    /// <summary>The land of a surface of <paramref name="type"/> grown from <paramref name="seed"/>, veiled toward <paramref name="look"/>'s fog.</summary>
    public static BackdropView Of(PlanetType type, PlanetLooks.Look look, uint seed)
    {
        var sizes = new Dictionary<string, (int W, int H)?>();
        (int W, int H)? Size(string name)
        {
            if (sizes.TryGetValue(name, out var known)) return known;
            if (!HiRes.Has(name)) return sizes[name] = null;
            var size = (Vector2I)PixelArt.Picture(name).GetSize();
            return sizes[name] = (size.X, size.Y);
        }
        var grass = BackdropPlan.GrassOf(BackdropPlan.GrassPictures(type).Select(HiRes.Load).OfType<Image>().Select(i => i.GetData()));
        return new BackdropView(BackdropPlan.Compose(type, seed, Size, grass), look.Fog);
    }

    /// <summary>The day's colour every layer is multiplied by, after its veil.</summary>
    public (float R, float G, float B) Tint
    {
        set
        {
            foreach (var layer in _layers) layer.Tint = value;
        }
    }

    /// <summary>
    /// Lays the layers for a camera looking at <paramref name="centre"/> at <paramref name="zoom"/> on a <paramref name="screen"/>,
    /// the land's ground line at row <paramref name="ground"/> of the world, in pixels: a layer's ground row lies on the screen
    /// where the line does, moved by its share of the camera's height from it.
    /// </summary>
    public void Follow(Vector2 centre, float zoom, Vector2 screen, float ground)
    {
        float tile = (float)BackdropLooks.First.Tile, across = screen.X / zoom;
        for (int i = 0; i < _layers.Count; i++)
        {
            var depth = _plan[i].Depth;
            float line = screen.Y / 2 + (ground - centre.Y) * zoom * depth.Follows;
            float left = Mathf.PosMod(centre.X * depth.Follows - across / 2, tile);
            _layers[i].Position = new Vector2(-left * zoom, line - (float)depth.GroundRow * zoom);
            _layers[i].Scale = Vector2.One * zoom;
        }
    }
}
