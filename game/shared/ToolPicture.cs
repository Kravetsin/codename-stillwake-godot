using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Shared;

/// <summary>
/// A held tool's picture on its card (2026-11-14-tool-card-pictures), in the same dark window a worn item's is shown in: its fine
/// icon picture, unturned, as large as <see cref="ToolPictureFit"/> lets it, in the middle.
/// </summary>
public partial class ToolPicture : Control
{
    private readonly Texture2D _picture;
    private readonly Vector2 _size;

    public ToolPicture(string item)
    {
        MouseFilter = MouseFilterEnum.Ignore;
        ClipContents = true;
        CustomMinimumSize = new Vector2(ToolPictureFit.Width, ToolPictureFit.Height);
        TextureFilter = TextureFilterEnum.LinearWithMipmaps;
        _picture = ItemIcons.CardPicture(item);
        var world = ItemIcons.Icon(item).GetSize();
        var (w, h) = ToolPictureFit.Of(world.X, world.Y);
        _size = new Vector2(Mathf.Round((float)w), Mathf.Round((float)h));
    }

    public override void _Draw()
    {
        WornPicture.DrawWindow(this, Size);
        DrawTextureRect(_picture, new Rect2(((Size - _size) / 2).Round(), _size), false);
    }
}
