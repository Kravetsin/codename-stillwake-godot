using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Menu;

/// <summary>
/// The build in the screen's bottom-right corner, faint (VersionBar.tsx of the TypeScript game): which
/// version a player runs, to tell a friend or a bug report, without opening anything.
/// </summary>
public partial class VersionCorner : CanvasLayer
{
    public VersionCorner(string version)
    {
        Layer = ScreenLayers.Bars;
        var label = new Label
        {
            Text = version,
            LabelSettings = Style.Text(Style.Mono, 10, Style.Slate600),
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        };
        label.SetAnchorsAndOffsetsPreset(Control.LayoutPreset.BottomRight, Control.LayoutPresetMode.KeepSize, 8);
        label.GrowHorizontal = Control.GrowDirection.Begin;
        label.GrowVertical = Control.GrowDirection.Begin;
        AddChild(label);
    }
}
