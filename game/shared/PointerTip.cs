using System.Collections.Generic;
using Godot;
using Stillwake.Game.I18n;
using Stillwake.Game.Render;

namespace Stillwake.Game.Shared;

/// <summary>
/// Words by the pointer: why the hand's card is refused there (<see cref="Ui.Reason"/>), written in the
/// world beside it as the building and power layers wrote it; and, beside the pointer on the screen
/// (session/PointerTip.tsx), what an overlay says of the cell (<see cref="Ui.WorldTip"/>), the first line
/// naming it, or else a blueprint's cost delivered against needed (<see cref="Ui.CostTip"/>). The
/// world's words need the world under the pointer: over a panel they hide. Text on demand, only while
/// there is something to say.
/// </summary>
public partial class PointerTip : CanvasLayer
{
    /// <summary>The reason's size and its outline, in pixels of the world: it grows and shrinks with the zoom, as world text does.</summary>
    private const float ReasonSize = 10, ReasonStroke = 3, ReasonOffset = 12;
    private static readonly Color ReasonColour = new("ffe4e4"), Outline = new("0b0f17"), Red400 = new("f87171");

    private readonly Ui _ui;
    private readonly Label _reason = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly LabelSettings _reasonText = new() { Font = Style.Mono, FontColor = ReasonColour, OutlineColor = Outline };
    private readonly PanelContainer _panel = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly VBoxContainer _lines = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly StyleBoxFlat _plain = Edge(Style.Slate600 with { A = 0.8f }), _cost = Edge(Style.Amber700 with { A = 0.7f });
    private string _shown = "";

    public PointerTip(Ui ui)
    {
        _ui = ui;
        Layer = ScreenLayers.Tips;
        _reason.LabelSettings = _reasonText;
        AddChild(_reason);
        _lines.AddThemeConstantOverride("separation", 0);
        _panel.AddChild(_lines);
        AddChild(_panel);
    }

    /// <summary><c>rounded border bg-slate-950/90 px-2 py-1 shadow</c> with the given edge.</summary>
    private static StyleBoxFlat Edge(Color border)
    {
        var box = Style.Box(Style.Slate950 with { A = 0.9f }, border);
        (box.ContentMarginLeft, box.ContentMarginRight, box.ContentMarginTop, box.ContentMarginBottom) = (8, 8, 4, 4);
        (box.ShadowColor, box.ShadowSize) = (new Color(0, 0, 0, 0.3f), 3);
        return box;
    }

    private static Label Line(string text, Font font, int size, Color colour) => new()
    {
        Text = text,
        LabelSettings = new LabelSettings { Font = font, FontSize = size, FontColor = colour },
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };

    /// <summary>Follows the pointer at <paramref name="pointer"/>, in cells of the world; hidden off the world.</summary>
    public void Frame((double X, double Y)? pointer)
    {
        if (pointer is not { } p)
        {
            _reason.Visible = _panel.Visible = false;
            return;
        }
        var toScreen = GetViewport().GetCanvasTransform();
        float zoom = toScreen.Scale.X;
        var cell = new Vector2((float)p.X, (float)p.Y) * ShipCamera.Cell;
        string? reason = _ui.Reason.Get();
        _reason.Visible = reason != null;
        _reason.Text = reason != null ? En.T(reason) : "";
        _reasonText.FontSize = Mathf.Max(1, Mathf.RoundToInt(ReasonSize * zoom));
        _reasonText.OutlineSize = Mathf.RoundToInt(ReasonStroke * zoom);
        _reason.Position = toScreen * (cell + Vector2.One * ReasonOffset);

        // What the world says needs the world under the pointer; the cost shows when nothing else is said.
        bool overWorld = GetViewport().GuiGetHoveredControl() == null;
        var world = overWorld ? _ui.WorldTip.Get() : null;
        var cost = overWorld && world == null ? _ui.CostTip.Get() : null;
        string key = $"{string.Join("\n", world ?? [])}|{string.Join(",", cost ?? [])}";
        if (key != _shown)
        {
            _shown = key;
            Fill(world, cost);
        }
        _panel.Visible = world != null || cost != null;
        _panel.Position = toScreen * cell + new Vector2(16, 16);
    }

    private void Fill(IReadOnlyList<string>? world, IReadOnlyList<CostLine>? cost)
    {
        foreach (var child in _lines.GetChildren())
        {
            _lines.RemoveChild(child);
            child.QueueFree();
        }
        _panel.AddThemeStyleboxOverride("panel", world == null && cost != null ? _cost : _plain);
        for (int i = 0; i < (world?.Count ?? 0); i++)
            _lines.AddChild(i == 0 ? Line(world![i], Style.Bold, 12, Style.Slate100) : Line(world![i], Style.Sans, 12, Style.Slate300));
        if (world == null && cost != null)
        {
            var row = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, 8);
            foreach (var c in cost)
            {
                var item = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, 2);
                item.AddChild(new TextureRect
                {
                    Texture = ItemIcons.Icon(c.Item),
                    CustomMinimumSize = new Vector2(16, 16),
                    ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                    StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
                    TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
                    MouseFilter = Control.MouseFilterEnum.Ignore,
                });
                item.AddChild(Line($"{c.Have}/{c.Need}", Style.Mono, 10, c.Have < c.Need ? Red400 : Style.Slate300));
                row.AddChild(item);
            }
            _lines.AddChild(row);
        }
        _panel.Size = _panel.GetCombinedMinimumSize();
    }
}
