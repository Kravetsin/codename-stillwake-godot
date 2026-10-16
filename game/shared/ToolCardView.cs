using Godot;
using Stillwake.Game.Render;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Shared;

/// <summary>
/// A <see cref="ToolCard"/> in Control nodes, as variant C of the card mockup (the plaque): a 2 px slate
/// edge round a dark ring; a head darkening downward with the icon large in a frame, the name and the kind;
/// three tiles, damage a second in amber; then chips and the hint, centred; a violet band while unknown.
/// </summary>
public static class ToolCardView
{
    /// <summary>The card's inside, the mockup's 230 px; its edge and ring come round it.</summary>
    private const int Inside = 228, Pad = 10;
    private static readonly Color Amber = new("f2b84b"), Ring = new("000000");

    private static readonly Font BoldMono = new FontVariation { BaseFont = Style.Mono, VariationEmbolden = 0.9f };
    private static readonly Font Spaced = new FontVariation { BaseFont = Style.Mono, SpacingGlyph = 1 };
    private static readonly LabelSettings NameText = Style.Text(Style.Bold, 14, Style.Slate100);
    private static readonly LabelSettings KindText = Style.Text(Style.Mono, 11, Style.Slate400);
    private static readonly LabelSettings UseText = Style.Text(Style.Mono, 11, Style.Slate300);
    private static readonly LabelSettings HintText = Style.Text(Style.Mono, 11, Style.Slate500);
    private static readonly LabelSettings KeyText = Style.Text(BoldMono, 10, Style.Slate300);
    private static readonly LabelSettings UnknownText = Style.Text(Style.Mono, 11, Style.Violet400);

    /// <summary>The card of <paramref name="card"/>, sized to its content.</summary>
    public static Control Build(ToolCard card)
    {
        // border-2 border-slate-600, a 1 px black ring inside, a soft drop shadow.
        var edge = Style.Box(Colors.Transparent, Style.Slate600, radius: 2);
        edge.SetBorderWidthAll(2);
        edge.SetContentMarginAll(2);
        (edge.ShadowColor, edge.ShadowSize, edge.ShadowOffset) = (new Color(0, 0, 0, 0.45f), 7, new Vector2(0, 4));
        var ring = Style.Box(Style.Slate950 with { A = 0.95f }, Ring, padding: 1, radius: 0);
        var column = Column(0, Head(card), Tiles(card), Body(card));
        if (card.Unknown) column.AddChild(UnknownBand());
        var outer = Boxed(edge, Boxed(ring, column));
        outer.CustomMinimumSize = new Vector2(Inside + 2 * (2 + 1), 0);
        return outer;
    }

    private static Control Head(ToolCard card)
    {
        var dark = new Gradient { Colors = [Style.Slate900, Style.Slate950 with { A = 0.95f }], Offsets = [0, 1] };
        var box = new StyleBoxTexture
        {
            Texture = new GradientTexture2D { Gradient = dark, FillFrom = Vector2.Zero, FillTo = Vector2.Down, Width = 4, Height = 64 },
        };
        (box.ContentMarginLeft, box.ContentMarginRight, box.ContentMarginTop, box.ContentMarginBottom) = (Pad, Pad, 10, 6);
        var frameBox = Style.Box(Style.Slate900, Style.Slate700, radius: 3);
        frameBox.SetBorderWidthAll(2);
        frameBox.SetContentMarginAll(2);
        var frame = Boxed(frameBox, new TextureRect
        {
            Texture = ItemIcons.Icon(card.Item),
            CustomMinimumSize = new Vector2(40, 40),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });
        frame.CustomMinimumSize = new Vector2(52, 52);
        frame.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        var name = Text(card.Name, NameText);
        return Boxed(box, Column(2, frame, Spacer(4), name, Text(card.Kind, KindText)));
    }

    /// <summary><c>grid-cols-3 border-y-2 border-slate-800</c>, each tile after the first ruled on its left.</summary>
    private static Control Tiles(ToolCard card)
    {
        var rules = new StyleBoxFlat { BgColor = Colors.Transparent, BorderColor = Style.Slate800, BorderWidthTop = 2, BorderWidthBottom = 2 };
        var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", 0);
        row.AddChild(Tile(card.Dps, T("card.dps"), first: true, Amber, 19));
        row.AddChild(Tile(card.Damage, T("card.damage"), first: false, null, 16));
        row.AddChild(Tile(card.Hit, T("card.hit"), first: false, null, 16));
        return Boxed(rules, row);
    }

    /// <param name="accent">The damage a second's amber, its ground faint amber; null for a plain tile.</param>
    private static Control Tile(string value, string label, bool first, Color? accent, int size)
    {
        var box = new StyleBoxFlat
        {
            BgColor = accent is { } a ? a with { A = 0.1f } : Colors.Transparent,
            BorderColor = Style.Slate800,
            BorderWidthLeft = first ? 0 : 2,
            ContentMarginTop = 6,
            ContentMarginBottom = 3,
        };
        var number = Text(value, Style.Text(BoldMono, size, accent ?? Style.Slate100));
        var under = Text(label, Style.Text(Spaced, 9, accent ?? Style.Slate500));
        var tile = Boxed(box, Column(3, number, under));
        tile.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        return tile;
    }

    /// <summary><c>px-2.5 pt-[7px] pb-2 gap-1.5</c>, a pixel more where Godot sets text tighter, centred: effects, what it works on, the hint.</summary>
    private static Control Body(ToolCard card)
    {
        var box = new StyleBoxEmpty { ContentMarginLeft = Pad, ContentMarginRight = Pad, ContentMarginTop = 8, ContentMarginBottom = 8 };
        var column = Column(7);
        if (card.Effects.Count > 0)
        {
            var effects = Chips();
            foreach (var effect in card.Effects)
            {
                var colour = new Color(effect.Colour);
                effects.AddChild(Chip(effect.Name, Style.Text(BoldMono, 11, colour), new Color(effect.Ground), colour with { A = 0.45f }));
            }
            column.AddChild(effects);
            foreach (var effect in card.Effects) column.AddChild(EffectLine(effect));
        }
        var uses = Chips();
        foreach (var use in card.Uses) uses.AddChild(Chip(use, UseText, Style.Slate800, Style.Slate700));
        column.AddChild(uses);
        column.AddChild(Hint(card.Key, card.Hint));
        return Boxed(box, column);
    }

    private static HFlowContainer Chips()
    {
        var chips = Style.Gap(new HFlowContainer { Alignment = FlowContainer.AlignmentMode.Center, MouseFilter = Control.MouseFilterEnum.Ignore }, 4);
        return chips;
    }

    /// <summary><c>rounded-[3px] border px-1.5 py-px</c> in mono 11.</summary>
    private static Control Chip(string text, LabelSettings words, Color ground, Color border)
    {
        var box = Style.Box(ground, border, radius: 3);
        (box.ContentMarginLeft, box.ContentMarginRight, box.ContentMarginTop, box.ContentMarginBottom) = (4, 6, 2, 2);
        return Boxed(box, Text(text, words));
    }

    /// <summary>The effect's name in bold in its colour, then what it does.</summary>
    private static Control EffectLine(CardEffect effect)
    {
        var line = new RichTextLabel
        {
            BbcodeEnabled = true,
            FitContent = true,
            ScrollActive = false,
            AutowrapMode = TextServer.AutowrapMode.WordSmart,
            CustomMinimumSize = new Vector2(Inside - 2 * Pad, 0),
            MouseFilter = Control.MouseFilterEnum.Ignore,
            Text = $"[center][b][color=#{effect.Colour}]{effect.Name}[/color][/b] — {effect.About}[/center]",
        };
        line.AddThemeFontOverride("normal_font", Style.Sans);
        line.AddThemeFontOverride("bold_font", Style.Bold);
        line.AddThemeFontSizeOverride("normal_font_size", 11);
        line.AddThemeFontSizeOverride("bold_font_size", 11);
        line.AddThemeColorOverride("default_color", Style.Slate300);
        return line;
    }

    /// <summary>A key cap, <c>border border-b-2 border-slate-600</c>, then how the tool is used.</summary>
    private static Control Hint(string key, string hint)
    {
        var cap = Style.Box(Colors.Transparent, Style.Slate600, radius: 3);
        cap.BorderWidthBottom = 2;
        (cap.ContentMarginLeft, cap.ContentMarginRight, cap.ContentMarginTop, cap.ContentMarginBottom) = (4, 4, 1, 2);
        var row = Style.Gap(new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = Control.MouseFilterEnum.Ignore }, 6);
        var keyCap = Boxed(cap, Text(key, KeyText));
        keyCap.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        row.AddChild(keyCap);
        row.AddChild(Text(hint, HintText));
        return row;
    }

    /// <summary><c>bg-violet/14 border-t border-violet/40 px-2.5 py-1</c>: not analysed yet.</summary>
    private static Control UnknownBand()
    {
        var box = new StyleBoxFlat
        {
            BgColor = Style.Violet400 with { A = 0.14f },
            BorderColor = Style.Violet400 with { A = 0.4f },
            BorderWidthTop = 1,
            ContentMarginLeft = Pad,
            ContentMarginRight = Pad,
            ContentMarginTop = 5,
            ContentMarginBottom = 5,
        };
        var words = Text(T("card.notAnalysed"), UnknownText);
        words.HorizontalAlignment = HorizontalAlignment.Left;
        return Boxed(box, words);
    }

    private static VBoxContainer Column(int gap, params Control[] children)
    {
        var column = new VBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        column.AddThemeConstantOverride("separation", gap);
        foreach (var child in children) column.AddChild(child);
        return column;
    }

    private static PanelContainer Boxed(StyleBox box, Control child)
    {
        var panel = new PanelContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        panel.AddThemeStyleboxOverride("panel", box);
        panel.AddChild(child);
        return panel;
    }

    private static Control Spacer(int height) => new() { CustomMinimumSize = new Vector2(0, height), MouseFilter = Control.MouseFilterEnum.Ignore };

    private static Label Text(string text, LabelSettings settings) => new()
    {
        Text = text,
        LabelSettings = settings,
        HorizontalAlignment = HorizontalAlignment.Center,
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };
}
