// Main's pictures of worlds: drawn as the camera sees the world kept by this machine, for its card on the title (0031).
using Godot;

namespace Stillwake.Game;

public partial class Main
{
    /// <summary>Draws the pictures of worlds for their cards.</summary>
    private readonly Render.WorldShot _shot = new();

    /// <summary>Draws the world kept by this machine as the camera sees it and keeps the picture for its card on the title.</summary>
    private void TakeShot()
    {
        if (_session == null || _worldId is not string id || _role == "guest") return;
        _shot.Take(_camera.GetScreenCenterPosition(), _camera.Zoom.X, GetViewportRect().Size.X, image =>
        {
            try
            {
                _index.WriteShot(id, image.SavePngToBuffer());
            }
            catch (System.Exception e) when (e is System.IO.IOException or System.UnauthorizedAccessException)
            {
                Log($"the picture of world {id} not kept: {e.Message}");
            }
        });
    }

    /// <summary>World <paramref name="id"/>'s picture as last kept, for its card; none for a world never pictured.</summary>
    private Texture2D? ShotOf(string id)
    {
        string file = _index.ShotOf(id);
        return System.IO.File.Exists(file) && Image.LoadFromFile(file) is { } image ? ImageTexture.CreateFromImage(image) : null;
    }
}
