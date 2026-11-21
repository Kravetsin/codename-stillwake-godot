using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Shared;

/// <summary>
/// The drone's frame round its open windows (drone-frame, the pick «Уголки»), one per client, over the windows: for each window
/// handed to <see cref="Ui.Windows"/> and shown, four ember brackets at its corners, each laid by a spark shot from the drone's eye
/// (<see cref="CornerFrame"/>). It tells the rest whether a drone window is out (<see cref="Ui.DroneWindow"/>). Screen px; drawn
/// each frame only while a window shows.
/// </summary>
public partial class DroneFrame : CanvasLayer
{
    private readonly Ui _ui;
    private readonly Node2D _dark = new(), _light = new() { Material = new CanvasItemMaterial { BlendMode = CanvasItemMaterial.BlendModeEnum.Add } };
    private readonly EmberMarks _marks = new();
    private readonly EmberArt _art = new();
    private double _seconds;
    private bool _drew;

    public DroneFrame(Ui ui)
    {
        _ui = ui;
        Layer = ScreenLayers.DroneFrame;
        _dark.Draw += () => _art.Darks(_marks, _dark);
        _light.Draw += () => _art.Light(_marks, _light);
        AddChild(_dark);
        AddChild(_light);
    }

    public override void _Process(double delta)
    {
        _seconds += delta;
        _ui.DroneWindow = _ui.Windows.Out;
        (float X, float Y)? eye = null;
        if (_ui.DroneLens is var (x, y, w, h))
        {
            var lens = GetViewport().GetCanvasTransform() * new Vector2(x + w / 2, y + h / 2);
            eye = (lens.X, lens.Y);
        }
        _marks.Clear();
        foreach (var window in _ui.Windows.All)
            if (window.Clock.Shown) CornerFrame.Draw(_marks, window.Clock, window.Rect, eye, (float)_seconds);
        bool draws = _marks.Squares.Count > 0 || _marks.Lit;
        if (!draws && !_drew) return;
        _drew = draws;
        _dark.QueueRedraw();
        _light.QueueRedraw();
    }
}
