using Godot;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A limb part's picture drawn bent as one piece with its limb (<see cref="BentLimb"/>, exp/bent-limbs) in place of its rigid
/// sprite: a child of that sprite, drawn where it is in the figure's order with its material, the same picture, offset and cut.
/// Its strip is made again only when the picture or the seam changes, and drawn again only when the bend does.
/// </summary>
public partial class BentPicture : Node2D
{
    private readonly Sprite2D _source;
    private readonly double _joint;
    private readonly bool _lower;
    private BentLimb? _mesh;
    private Texture2D? _texture;
    private Rect2 _cut;
    private (double From, double To) _seam;
    private Vector2[] _points = [], _uvs = [];
    private Color[] _colours = [];

    /// <param name="source">The rigid sprite it replaces, which it hangs from.</param>
    /// <param name="joint">Where the elbow or knee is down the sprite's frame.</param>
    /// <param name="lower">Whether the sprite hangs from the lower bone.</param>
    public BentPicture(Sprite2D source, double joint, bool lower)
    {
        (_source, _joint, _lower) = (source, joint, lower);
        UseParentMaterial = true;
        Visible = false;
    }

    /// <summary>Drawn bent <paramref name="bend"/>, the limb's <see cref="Bend.Lower"/>, meeting the limb's other picture at <paramref name="seam"/>.</summary>
    public void Show(double bend, (double From, double To) seam)
    {
        var texture = _source.Texture;
        var cut = texture == null ? default : new Rect2(_source.Offset, _source.RegionEnabled ? _source.RegionRect.Size : texture.GetSize());
        bool remade = texture != _texture || cut != _cut || seam != _seam;
        if (remade) Remake(texture, cut, seam);
        if (_mesh == null || !_mesh.Bend(bend) && !remade) return;
        for (int i = 0; i < _points.Length; i++) _points[i] = new Vector2((float)_mesh.Points[i].X, (float)_mesh.Points[i].Y);
        QueueRedraw();
    }

    private void Remake(Texture2D? texture, Rect2 cut, (double From, double To) seam)
    {
        (_texture, _cut, _seam) = (texture, cut, seam);
        _mesh = texture == null ? null : new BentLimb(cut.Position.X, cut.Position.Y, cut.Size.X, cut.Size.Y, _joint, _lower, seam);
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
