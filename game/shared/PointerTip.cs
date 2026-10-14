using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;

namespace Stillwake.Game.Shared;

/// <summary>
/// Words beside the pointer (session/PointerTip.tsx): why the hand's card is refused there
/// (<see cref="Ui.Reason"/>), what an overlay says of the cell (<see cref="Ui.WorldTip"/>), and a
/// blueprint's cost delivered against needed (<see cref="Ui.CostTip"/>). Text on demand, only while there
/// is something to say.
/// </summary>
public partial class PointerTip : CanvasLayer
{
    private static readonly Color Slate950 = new("020617"), Slate600 = new("475569"), Slate300 = new("cbd5e1"),
        Slate100 = new("f1f5f9"), Red400 = new("f87171"), ReasonColour = new("ffe4e4"), Outline = new("0b0f17");
    private static readonly Font Mono = new SystemFont { FontNames = ["Consolas", "Menlo", "monospace"] };

    private readonly Ui _ui;
    private readonly Label _reason = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly PanelContainer _panel = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly VBoxContainer _lines = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private string _shown = "";

    public PointerTip(Ui ui)
    {
        _ui = ui;
        Layer = 11;
        _reason.LabelSettings = new LabelSettings { Font = Mono, FontSize = 12, FontColor = ReasonColour, OutlineSize = 4, OutlineColor = Outline };
        AddChild(_reason);
        var box = new StyleBoxFlat { BgColor = Slate950 with { A = 0.9f }, BorderColor = Slate600 };
        box.SetBorderWidthAll(1);
        box.SetCornerRadiusAll(4);
        box.SetContentMarginAll(6);
        _panel.AddThemeStyleboxOverride("panel", box);
        _lines.AddThemeConstantOverride("separation", 2);
        _panel.AddChild(_lines);
        AddChild(_panel);
    }

    private static Label Line(string text, Color colour, int size = 12) => new()
    {
        Text = text,
        LabelSettings = new LabelSettings { Font = Mono, FontSize = size, FontColor = colour },
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };

    /// <summary>Follows the pointer at <paramref name="pointer"/>, in cells of the world; hidden off the world.</summary>
    public void Frame((double X, double Y)? pointer)
    {
        string? reason = _ui.Reason.Get();
        var world = _ui.WorldTip.Get();
        var cost = _ui.CostTip.Get();
        if (pointer is not { } p)
        {
            _reason.Visible = _panel.Visible = false;
            return;
        }
        var screen = GetViewport().GetCanvasTransform() * (new Vector2((float)p.X, (float)p.Y) * ShipCamera.Cell);
        _reason.Visible = reason != null;
        _reason.Text = reason != null ? Words.T(reason) : "";
        _reason.Position = screen + new Vector2(24, 24);
        string key = $"{string.Join("\n", world ?? [])}|{string.Join(",", cost ?? [])}";
        if (key != _shown)
        {
            _shown = key;
            foreach (var child in _lines.GetChildren())
            {
                _lines.RemoveChild(child);
                child.QueueFree();
            }
            foreach (var line in world ?? []) _lines.AddChild(Line(line, Slate100));
            if (cost != null)
            {
                var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
                row.AddThemeConstantOverride("separation", 8);
                foreach (var c in cost)
                {
                    row.AddChild(new TextureRect
                    {
                        Texture = PixelArt.Picture($"items/{c.Item}"),
                        CustomMinimumSize = new Vector2(16, 16),
                        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    });
                    row.AddChild(Line($"{c.Have}/{c.Need}", c.Have < c.Need ? Red400 : Slate300, 10));
                }
                _lines.AddChild(row);
            }
            _panel.Size = _panel.GetCombinedMinimumSize();
        }
        _panel.Visible = world != null || cost != null;
        _panel.Position = screen + new Vector2(24, reason != null ? 44 : 24);
    }
}
