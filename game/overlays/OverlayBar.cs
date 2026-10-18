using Godot;
using Stillwake.Game.I18n;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Overlays;

/// <summary>
/// The air overlay's chip at the bottom left, lit while air shows (overlays/OverlayBar.tsx, 0030): how a
/// player finds out it exists. O switches it, in building or out of it; the pipes and cables overlays
/// come and go with their build tabs and have no key of their own.
/// </summary>
public partial class OverlayBar : CanvasLayer
{
    private const string Air = "atmosphere";

    private static readonly Color Slate900 = new("0f172a"), Slate700 = new("334155"), Slate400 = new("94a3b8"),
        Sky900 = new("0c4a6e"), Sky400 = new("38bdf8"), Sky100 = new("e0f2fe");

    private readonly Ui _ui;
    private readonly PanelContainer _chip = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly Label _label = new() { Text = $"O {En.T("overlay.air")}" };
    private bool? _shown;

    public OverlayBar(Ui ui)
    {
        _ui = ui;
        Layer = ScreenLayers.Bars;
        _chip.AnchorTop = _chip.AnchorBottom = 1;
        _chip.GrowVertical = Control.GrowDirection.Begin;
        _chip.OffsetLeft = 12;
        _chip.OffsetBottom = -12;
        _chip.AddChild(_label);
        AddChild(_chip);
    }

    /// <summary>Where the chip lies on the screen: what the crew's markers keep out of.</summary>
    public Rect2 Covers => _chip.GetGlobalRect();

    public override void _UnhandledInput(InputEvent e)
    {
        if (e is not InputEventKey { Pressed: true, Echo: false, PhysicalKeycode: Key.O }) return;
        _ui.AirOn.Set(!_ui.AirOn.Get());
        GetViewport().SetInputAsHandled();
    }

    public override void _Process(double delta)
    {
        // Lit while air shows: under a build tab with its own overlay, O waits for the menu to close.
        bool on = _ui.OverlayOn == Air;
        if (on == _shown) return;
        _shown = on;
        var box = new StyleBoxFlat { BgColor = (on ? Sky900 : Slate900) with { A = 0.8f }, BorderColor = on ? Sky400 : Slate700 };
        box.SetBorderWidthAll(1);
        box.SetCornerRadiusAll(4);
        box.SetContentMarginAll(4);
        box.ContentMarginLeft = box.ContentMarginRight = 8;
        _chip.AddThemeStyleboxOverride("panel", box);
        _label.LabelSettings = new LabelSettings { Font = Style.Mono, FontSize = 12, FontColor = on ? Sky100 : Slate400 };
    }
}
