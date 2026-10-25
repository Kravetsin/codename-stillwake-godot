// Main's title scene (0031): the wreck the game starts in, run behind the title but never saved, its hero sitting on the
// torn deck's edge (TitleBackdrop); left the moment the title closes, whichever way it is left.
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Menu;
using Stillwake.Net;
using Stillwake.Sim;

namespace Stillwake.Game;

public partial class Main
{
    /// <summary>The world behind the title, its registry and what it shows beyond the wreck; none once a world is played.</summary>
    private Session? _scene;
    private Registry? _sceneM;
    private TitleBackdrop? _backdrop;
    /// <summary>Draws the pictures of worlds for their cards.</summary>
    private readonly Render.WorldShot _shot = new();

    /// <summary>Starts the title's world: the wreck, the views drawing it, the hero seated, the HUD hidden.</summary>
    private void ShowScene()
    {
        var m = new Registry();
        var world = World.Create(1, Registry.Wreck, m.All);
        _m = _ship = _sceneM = m;
        var (host, client) = LocalSession.Create(world, Build, 0);
        _scene = new Session(host, client);
        ShowWorld();
        SyncGrid(client);
        int w = m.Grid.Width;
        var ledge = TitleScene.Ledge((x, y) => m.Grid.Solid(y * w + x), w, m.Grid.Height) ?? (m.Crew.Spawn % w, m.Crew.Spawn / w);
        AddChild(_backdrop = new TitleBackdrop(TitleScene.Seat(ledge), _camera));
        _camera.SetZoom(TitleScene.Zoom);
        if (_hero != null) _hero.Visible = false;
        // Deferred: the layers made after the title in _Ready are hidden too.
        Callable.From(() =>
        {
            if (_scene != null) HideHud();
        }).CallDeferred();
    }

    /// <summary>A frame of the title's world: its ticks, its grid, light and debris; no hero, hands or items.</summary>
    private void SceneFrame(Session scene, double delta)
    {
        var client = scene.Client;
        int before = client.World.Tick;
        scene.Frame(delta * 1000);
        _stars.Seconds = (client.World.Tick + scene.Progress) / World.TicksPerSecond;
        if (client.World.Tick != before) SyncLight();
        SyncGrid(client);
        _debris.Frame(client, _sceneM!.Debris, _camera.View(), scene.Progress, delta * 1000);
    }

    /// <summary>Leaves the title's world for one played: what it showed let go, the camera's zoom the player's, the HUD back.</summary>
    private void LeaveScene()
    {
        if (_scene == null) return;
        (_scene, _sceneM) = (null, null);
        _backdrop?.QueueFree();
        _backdrop = null;
        _camera.FreeZoom();
        if (_hero != null) _hero.Visible = true;
        foreach (var layer in _hidden) layer.Visible = true;
        _hidden.Clear();
    }

    /// <summary>Every helmet lamp the light draws: the title's hero's while it shows, else the heroes'.</summary>
    private IReadOnlyList<(Vector2 At, Vector2 Toward, Vector2 Head)> HeadLamps() => _backdrop?.HeadLamps() ?? _hero?.HeadLamps() ?? [];

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

    /// <summary>Hides every screen layer showing between the world and the menu: the HUD, the bars, the panels.</summary>
    private void HideHud()
    {
        foreach (var node in GetTree().Root.FindChildren("*", nameof(CanvasLayer), true, false))
            if (node is CanvasLayer { Visible: true, Layer: > Render.ScreenLayers.Stars and < Render.ScreenLayers.Menu } layer)
            {
                layer.Visible = false;
                _hidden.Add(layer);
            }
    }

    /// <summary>The screen layers the title hid, shown again when it is left; those hidden already stay so.</summary>
    private readonly List<CanvasLayer> _hidden = [];
}
