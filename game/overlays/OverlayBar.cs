using System.Linq;
using Godot;
using Stillwake.Game.I18n;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Overlays;

/// <summary>
/// The overlays and their keys along the bottom left, the one on lit: how a player finds out they exist
/// (overlays/OverlayBar.tsx). Their keys, O, P and L, switch them through <see cref="OverlaySwitch"/>.
/// </summary>
public partial class OverlayBar : CanvasLayer
{
    private static readonly Color Slate900 = new("0f172a"), Slate700 = new("334155"), Slate400 = new("94a3b8"),
        Sky900 = new("0c4a6e"), Sky400 = new("38bdf8"), Sky100 = new("e0f2fe");
    private static readonly Font Mono = new SystemFont { FontNames = ["Consolas", "Menlo", "monospace"] };

    private readonly OverlaySwitch _switch;
    private readonly HBoxContainer _row = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private string? _shown = "";

    public OverlayBar(OverlaySwitch overlays)
    {
        _switch = overlays;
        Layer = 10;
        _row.AddThemeConstantOverride("separation", 8);
        _row.AnchorTop = _row.AnchorBottom = 1;
        _row.GrowVertical = Control.GrowDirection.Begin;
        _row.OffsetLeft = 12;
        _row.OffsetBottom = -12;
        AddChild(_row);
    }

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is not InputEventKey { Pressed: true } key) return;
        string letter = OS.GetKeycodeString(key.PhysicalKeycode);
        if (!_switch.Overlays.Any(o => o.Key == letter)) return;
        _switch.Press(letter, key.Echo);
        GetViewport().SetInputAsHandled();
    }

    public override void _Process(double delta)
    {
        if (_switch.Active == _shown) return;
        _shown = _switch.Active;
        foreach (var child in _row.GetChildren()) child.QueueFree();
        foreach (var overlay in _switch.Overlays)
        {
            bool on = overlay.Id == _switch.Active;
            var box = new StyleBoxFlat { BgColor = (on ? Sky900 : Slate900) with { A = 0.8f }, BorderColor = on ? Sky400 : Slate700 };
            box.SetBorderWidthAll(1);
            box.SetCornerRadiusAll(4);
            box.SetContentMarginAll(4);
            box.ContentMarginLeft = box.ContentMarginRight = 8;
            var panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
            panel.AddThemeStyleboxOverride("panel", box);
            panel.AddChild(new Label
            {
                Text = $"{overlay.Key} {En.T(overlay.Name)}",
                LabelSettings = new LabelSettings { Font = Mono, FontSize = 12, FontColor = on ? Sky100 : Slate400 },
            });
            _row.AddChild(panel);
        }
    }
}
