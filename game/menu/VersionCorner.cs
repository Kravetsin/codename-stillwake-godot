using System;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Menu;

/// <summary>
/// The build in the screen's bottom-right corner, faint (VersionBar.tsx of the TypeScript game): which
/// version a player runs, to tell a friend or a bug report, without opening anything. Playing together,
/// how long this player's actions take to happen follows it, read twice a second.
/// </summary>
public partial class VersionCorner : CanvasLayer
{
    private const double ReadMs = 500;

    private readonly string _version;
    private readonly Func<double?> _lag;
    private readonly Label _label;
    private double _sinceRead = ReadMs;

    /// <param name="lag">How long an action of this player's takes to happen, in ms; none playing alone.</param>
    public VersionCorner(string version, Func<double?> lag)
    {
        (_version, _lag) = (version, lag);
        Layer = ScreenLayers.Bars;
        _label = new Label
        {
            Text = version,
            LabelSettings = Style.Text(Style.Mono, 10, Style.Slate600),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        _label.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomRight, Control.LayoutPresetMode.KeepSize, 8);
        _label.GrowHorizontal = Control.GrowDirection.Begin;
        _label.GrowVertical = Control.GrowDirection.Begin;
        AddChild(_label);
    }

    public override void _Process(double delta)
    {
        _sinceRead += delta * 1000;
        if (_sinceRead < ReadMs) return;
        _sinceRead = 0;
        _label.Text = _lag() is double ms ? $"{ms:0} ms · {_version}" : _version;
    }
}
