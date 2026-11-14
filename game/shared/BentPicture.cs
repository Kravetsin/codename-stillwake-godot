using Godot;

namespace Stillwake.Game.Shared;

/// <summary>
/// A limb part's picture drawn bent as one piece with its limb (<see cref="BentLimb"/>, bent-limbs) in place of its sprite: a child
/// of that sprite, which carries the picture, offset, ankle cut and material and draws nothing itself, drawn where the sprite is in
/// the figure's order. A hero's limbs and helmets, a whole leg bent at its ankle too (raw-basic-suit), a creature's legs, neck and
/// tail (creatures-bend). Its grid is made again only
/// when the picture or the seam changes, and drawn again only when the bend does.
/// </summary>
public partial class BentPicture : Node2D
{
    private readonly Sprite2D _source;
    private double _joint, _band = BentLimb.Band;
    private readonly bool _lower;
    private readonly double _axis;
    private readonly double? _ankle;
    private BentLimb? _mesh;
    private Texture2D? _texture;
    private Rect2 _cut;
    private (double From, double To) _seam = (double.NaN, double.NaN);
    private Vector2[] _points = [], _uvs = [];
    private Color[] _colours = [];

    /// <param name="source">The sprite it draws in place of, which it hangs from.</param>
    /// <param name="joint">Where the elbow or knee is down the sprite's frame.</param>
    /// <param name="lower">Whether the sprite hangs from the lower bone.</param>
    /// <param name="axis">Which way its bone runs in the sprite's frame (<see cref="BentLimb"/>): straight down for a limb.</param>
    /// <param name="ankle">Where a whole leg's ankle is down the sprite's frame, bent there too (<see cref="BentLimb"/>); none for any other.</param>
    public BentPicture(Sprite2D source, double joint, bool lower, double axis = 0, double? ankle = null)
    {
        (_source, _joint, _lower, _axis, _ankle) = (source, joint, lower, axis, ankle);
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

    /// <summary>
    /// Drawn bent <paramref name="bend"/>, a whole leg's ankle <paramref name="foot"/> (<see cref="BentLimb.Bend"/>), meeting the limb's
    /// other picture at <paramref name="seam"/> (<see cref="BentLimb.SeamOf"/>).
    /// </summary>
    public void Show(double bend, (double From, double To) seam, double foot = 0)
    {
        var texture = _source.Texture;
        var cut = texture == null ? default : new Rect2(_source.Offset, _source.RegionEnabled ? _source.RegionRect.Size : texture.GetSize());
        bool remade = texture != _texture || cut != _cut || seam != _seam;
        if (remade) Remake(texture, cut, seam);
        if (_mesh == null || !_mesh.Bend(bend, foot) && !remade) return;
        for (int i = 0; i < _points.Length; i++) _points[i] = new Vector2((float)_mesh.Points[i].X, (float)_mesh.Points[i].Y);
        QueueRedraw();
    }

    private void Remake(Texture2D? texture, Rect2 cut, (double From, double To) seam)
    {
        (_texture, _cut, _seam) = (texture, cut, seam);
        _mesh = texture == null ? null : new BentLimb(cut.Position.X, cut.Position.Y, cut.Size.X, cut.Size.Y, _joint, _lower, seam, _band, _axis, _ankle);
        int n = _mesh?.Points.Length ?? 0;
        (_points, _uvs, _colours) = (new Vector2[n], new Vector2[n], new Color[n]);
        if (_mesh == null || texture == null) return;
        var size = texture.GetSize();
        float from = _source.RegionEnabled ? _source.RegionRect.Position.Y : 0;
        for (int i = 0; i < n; i++)
        {
            _uvs[i] = new Vector2((float)_mesh.Uvs[i].X / size.X, ((float)_mesh.Uvs[i].Y + from) / size.Y);
            _colours[i] = new Color(1, 1, 1, (float)_mesh.Alphas[i]);
        }
    }

    public override void _Draw()
    {
        if (_mesh == null || _texture == null || _points.Length == 0) return;
        RenderingServer.CanvasItemAddTriangleArray(GetCanvasItem(), _mesh.Triangles, _points, _colours, _uvs, texture: _texture.GetRid());
    }
}
