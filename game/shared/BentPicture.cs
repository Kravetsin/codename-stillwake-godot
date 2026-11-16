using Godot;

namespace Stillwake.Game.Shared;

/// <summary>
/// A limb's picture drawn bent as one piece at its joint (<see cref="BentLimb"/>, bent-limbs, whole-limbs) in place of its sprite: a
/// child of that sprite, which carries the picture, offset, ankle cut and material and draws nothing itself, drawn where the sprite
/// is in the figure's order. A hero's arms, legs and helmets, a creature's legs, neck and tail (creatures-bend). Its grid is made
/// again only when the picture changes, and drawn again only when the bend does.
/// </summary>
public partial class BentPicture : Node2D
{
    private readonly Sprite2D _source;
    private double _joint, _band = BentLimb.Band;
    private readonly bool _lower;
    private readonly double _axis;
    private BentLimb? _mesh;
    private Texture2D? _texture;
    private Rect2 _cut;
    private Vector2[] _points = [], _uvs = [];
    private Color[] _colours = [];

    /// <param name="source">The sprite it draws in place of, which it hangs from.</param>
    /// <param name="joint">Where the elbow or knee is down the sprite's frame.</param>
    /// <param name="lower">Whether the sprite hangs from the lower bone.</param>
    /// <param name="axis">Which way its bone runs in the sprite's frame (<see cref="BentLimb"/>): straight down for a limb.</param>
    public BentPicture(Sprite2D source, double joint, bool lower, double axis = 0)
    {
        (_source, _joint, _lower, _axis) = (source, joint, lower, axis);
        UseParentMaterial = true;
        source.SelfModulate = Colors.Transparent;
    }

    /// <summary>Where the joint is down the sprite's frame: moved, the grid is made again at the next <see cref="Show"/>.</summary>
    public double Joint
    {
        get => _joint;
        set
        {
            if (value == _joint) return;
            _joint = value;
            _texture = null;
        }
    }

    /// <summary>How tall the band it bends in is (<see cref="BentLimb"/>): changed, the grid is made again at the next <see cref="Show"/>.</summary>
    public double Band
    {
        get => _band;
        set
        {
            if (value == _band) return;
            _band = value;
            _texture = null;
        }
    }

    /// <summary>Drawn bent <paramref name="bend"/> (<see cref="BentLimb.Bend"/>).</summary>
    public void Show(double bend)
    {
        var texture = _source.Texture;
        var cut = texture == null ? default : new Rect2(_source.Offset, _source.RegionEnabled ? _source.RegionRect.Size : texture.GetSize());
        bool remade = texture != _texture || cut != _cut;
        if (remade) Remake(texture, cut);
        if (_mesh == null || !_mesh.Bend(bend) && !remade) return;
        for (int i = 0; i < _points.Length; i++) _points[i] = new Vector2((float)_mesh.Points[i].X, (float)_mesh.Points[i].Y);
        QueueRedraw();
    }

    private void Remake(Texture2D? texture, Rect2 cut)
    {
        (_texture, _cut) = (texture, cut);
        _mesh = texture == null ? null : new BentLimb(cut.Position.X, cut.Position.Y, cut.Size.X, cut.Size.Y, _joint, _lower, _band, _axis);
        int n = _mesh?.Points.Length ?? 0;
        (_points, _uvs, _colours) = (new Vector2[n], new Vector2[n], new Color[n]);
        if (_mesh == null || texture == null) return;
        var size = texture.GetSize();
        float from = _source.RegionEnabled ? _source.RegionRect.Position.Y : 0;
        for (int i = 0; i < n; i++)
        {
            _uvs[i] = new Vector2((float)_mesh.Uvs[i].X / size.X, ((float)_mesh.Uvs[i].Y + from) / size.Y);
            _colours[i] = Colors.White;
        }
    }

    public override void _Draw()
    {
        if (_mesh == null || _texture == null || _points.Length == 0) return;
        RenderingServer.CanvasItemAddTriangleArray(GetCanvasItem(), _mesh.Triangles, _points, _colours, _uvs, texture: _texture.GetRid());
    }
}
