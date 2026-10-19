using System.Linq;
using Godot;
using Stillwake.Game.I18n;
using Stillwake.Game.Render;

namespace Stillwake.Game.Shared;

/// <summary>
/// Words by the pointer: why the hand's card is refused there (<see cref="Ui.Reason"/>), written in the
/// world beside it as the building and power layers wrote it; and the one tooltip, Terraria's: shown at
/// once, following the pointer, a name in bold and lines in colour under it. It says what a panel says
/// of what the pointer is on (<see cref="Ui.Tip"/>), else, over the world, what an overlay says of the
/// cell (<see cref="Ui.WorldTip"/>), the first line naming it, or a blueprint under it: what it builds,
/// then what is carried against what it still needs, or a mark: what it is, then what taking it down gives back
/// (<see cref="Ui.CostTip"/>). A tip with an item's card shows the card (<see cref="ItemCardView"/>)
/// in place of its lines. Text on demand, only while there is something to say.
/// </summary>
public partial class PointerTip : CanvasLayer
{
    /// <summary>The reason's size and its outline, in pixels of the world: it grows and shrinks with the zoom, as world text does.</summary>
    private const float ReasonSize = 10, ReasonStroke = 3, ReasonOffset = 12;
    /// <summary>The tip's widest, its padding and its gap from the pointer and the screen's edge, in pixels.</summary>
    private const float Widest = 260, PadX = 8, Edge = 6;
    private static readonly Vector2 FromPointer = new(14, 16);
    private static readonly Color ReasonColour = new("ffe4e4"), Outline = new("0b0f17");

    private readonly Ui _ui;
    private readonly Label _reason = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly LabelSettings _reasonText = new() { Font = Style.Mono, FontColor = ReasonColour, OutlineColor = Outline };
    private readonly PanelContainer _panel = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly VBoxContainer _lines = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private static readonly LabelSettings NameText = Style.Text(Style.Bold, 12, Style.Slate100);
    private static readonly StyleBoxEmpty CardBox = new();
    private readonly StyleBoxFlat _box;
    private Tip? _shown;

    public PointerTip(Ui ui)
    {
        _ui = ui;
        Layer = ScreenLayers.Tips;
        _reason.LabelSettings = _reasonText;
        AddChild(_reason);
        // rounded border border-slate-600/80 bg-slate-950/90 px-2 pt-[3px] pb-1, a faint dark ring.
        var box = _box = Style.Box(Style.Slate950 with { A = 0.92f }, Style.Slate600 with { A = 0.8f }, radius: 3);
        (box.ContentMarginLeft, box.ContentMarginRight, box.ContentMarginTop, box.ContentMarginBottom) = (PadX, PadX, 3, 4);
        (box.ShadowColor, box.ShadowSize) = (new Color(0, 0, 0, 0.18f), 3);
        _panel.AddThemeStyleboxOverride("panel", box);
        _lines.AddThemeConstantOverride("separation", 0);
        _panel.AddChild(_lines);
        _panel.Visible = false;
        AddChild(_panel);
    }

    /// <summary>A line's colour by its tone.</summary>
    private static Color Of(Tone tone) => tone switch
    {
        Tone.Dim => Style.Slate400,
        Tone.Good => Style.Green400,
        Tone.Bad => Style.Red300,
        Tone.Unknown => Style.Violet400,
        _ => Style.Slate300,
    };

    /// <summary>
    /// Follows the pointer at <paramref name="pointer"/>, in cells of the world, wherever a script or the
    /// mouse puts it; hidden off the window and behind the menu.
    /// </summary>
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

        // A panel's word first; what the world says needs the world under the pointer.
        var tip = _ui.Tip.Get() ?? (GetViewport().GuiGetHoveredControl() == null ? OfWorld() : null);
        if (!Equals(tip, _shown))
        {
            _shown = tip;
            if (tip != null) Fill(tip);
        }
        _panel.Visible = tip != null;
        if (tip == null) return;
        // Beside the pointer, kept inside the screen; up and to the left while a stack is carried below and right of it.
        var screen = GetViewport().GetVisibleRect().Size;
        var at = _ui.Carrying.Get() ? toScreen * cell - FromPointer - _panel.Size : toScreen * cell + FromPointer;
        _panel.Position = new Vector2(
            Mathf.Max(Edge, Mathf.Min(at.X, screen.X - _panel.Size.X - Edge)),
            Mathf.Max(Edge, Mathf.Min(at.Y, screen.Y - _panel.Size.Y - Edge)));
    }

    /// <summary>An overlay's words, the first naming the cell; or else a plan's: a blueprint's cost, a mark's refund.</summary>
    private Tip? OfWorld()
    {
        if (_ui.WorldTip.Get() is { Count: > 0 } world)
            return new Tip(world[0], world.Skip(1).Select(line => new TipLine(line)).ToList());
        return _ui.CostTip.Get();
    }

    private void Fill(Tip tip)
    {
        foreach (var child in _lines.GetChildren())
        {
            _lines.RemoveChild(child);
            child.QueueFree();
        }
        // An item's card draws its own edge.
        _panel.AddThemeStyleboxOverride("panel", tip.Card != null ? CardBox : _box);
        if (tip.Card != null)
        {
            _lines.AddChild(ItemCardView.Build(tip.Card));
            _panel.Size = _panel.GetCombinedMinimumSize();
            return;
        }
        if (tip.Picture is { } placement)
        {
            // What a blueprint builds, as its build card shows it, beside the card's name.
            var head = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, 7);
            head.AddChild(Icon(BlueprintArt.Thumbnail(placement), Large));
            if (tip.Name != null) head.AddChild(Text(tip.Name, NameText));
            _lines.AddChild(head);
        }
        else if (tip.Name != null) _lines.AddChild(Text(tip.Name, NameText));
        foreach (var line in tip.Lines) _lines.AddChild(LineOf(line));
        _panel.Size = _panel.GetCombinedMinimumSize();
    }

    /// <summary>An icon's side: a line's, and a large line's, twice the 16 pixels of an item's picture.</summary>
    private const int Small = 14, Large = 32;

    /// <summary>
    /// <c>flex items-center gap-[5px]</c> in mono 11: the item's icon, then the words, wrapped past the tip's
    /// widest; a large line's icon at 32 and its words in mono 14.
    /// </summary>
    private static Control LineOf(TipLine line)
    {
        int size = line.Large ? 14 : 11, icon = line.Large ? Large : Small, gap = line.Large ? 7 : 5;
        var text = Text(line.Text, Style.Text(Style.Mono, size, Of(line.Tone)));
        float room = Widest - 2 * PadX - (line.Icon != null ? icon + gap : 0);
        if (Style.Mono.GetStringSize(line.Text, fontSize: size).X > room)
        {
            text.AutowrapMode = TextServer.AutowrapMode.WordSmart;
            text.CustomMinimumSize = new Vector2(room, 0);
        }
        if (line.Icon == null) return text;
        var row = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, gap);
        var picture = Icon(ItemIcons.Icon(line.Icon), icon);
        if (line.Faded) picture.Modulate = new Color(1, 1, 1, 0.4f);
        row.AddChild(picture);
        row.AddChild(text);
        return row;
    }

    private static TextureRect Icon(Texture2D texture, int size) => new()
    {
        Texture = texture,
        CustomMinimumSize = new Vector2(size, size),
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };

    private static Label Text(string text, LabelSettings settings) => new()
    {
        Text = text,
        LabelSettings = settings,
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };
}
