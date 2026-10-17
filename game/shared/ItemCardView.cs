using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Shared;

/// <summary>
/// An <see cref="ItemCard"/> in Control nodes, as the card mockup (the plaque): a 2 px slate edge round a
/// dark ring, amber-ish for a tool; a head darkening downward with the icon large in a frame, the name and
/// the kind; a tool's tiles, the first in amber; then chips and the hint, centred, or "Goes
/// into" and its chips; a violet band while unknown; a station's block at the bottom.
/// </summary>
public static class ItemCardView
{
    /// <summary>The card's inside, the mockup's 230 px; its edge and ring come round it.</summary>
    private const int Inside = 228, Pad = 10;
    /// <summary>A tool's colour: its first tile, its card's edge and kind, its recipe's tile.</summary>
    public static readonly Color Amber = new("f2b84b");
    private static readonly Color Ring = new("000000");

    private static readonly Font BoldMono = new FontVariation { BaseFont = Style.Mono, VariationEmbolden = 0.9f };
    private static readonly Font Spaced = new FontVariation { BaseFont = Style.Mono, SpacingGlyph = 1 };
    private static readonly LabelSettings NameText = Style.Text(Style.Bold, 14, Style.Slate100);
    private static readonly LabelSettings KindText = Style.Text(Style.Mono, 11, Style.Slate400);
    private static readonly LabelSettings ToolKindText = Style.Text(Style.Mono, 11, Amber);
    private static readonly LabelSettings UseText = Style.Text(Style.Mono, 11, Style.Slate300);
    private static readonly LabelSettings SmallText = Style.Text(Spaced, 10, Style.Slate500);
    private static readonly LabelSettings HintText = Style.Text(Style.Mono, 11, Style.Slate500);
    private static readonly LabelSettings KeyText = Style.Text(BoldMono, 10, Style.Slate300);
    private static readonly LabelSettings UnknownText = Style.Text(Style.Mono, 11, Style.Violet400);
    private static readonly LabelSettings CraftText = Style.Text(new FontVariation { BaseFont = BoldMono, SpacingGlyph = 1 }, 10, Style.Slate400);
    private static readonly LabelSettings TimeText = Style.Text(Style.Mono, 10, Style.Slate300);
    private static readonly LabelSettings CostText = Style.Text(Style.Mono, 12, Style.Slate300);
    private static readonly LabelSettings HaveText = Style.Text(Style.Mono, 11, Style.Slate500);
    private static readonly LabelSettings EnoughText = Style.Text(Style.Mono, 12, Style.Green400);
    private static readonly LabelSettings ShortText = Style.Text(Style.Mono, 12, Style.Red300);
    private static readonly LabelSettings FromText = Style.Text(Style.Mono, 10, Style.Slate500);
    /// <summary>Charge's green: the meter's words and its bar (the charge mockup).</summary>
    public static readonly Color Green = new(Charges.Green);
    private static readonly LabelSettings ChargeText = Style.Text(BoldMono, 10, Green);

    /// <summary>The card of <paramref name="card"/>, sized to its content.</summary>
    public static Control Build(ItemCard card)
    {
        // border-2 border-slate-600, amber-ish for a tool; a 1 px black ring inside, a soft drop shadow.
        var edge = Style.Box(Colors.Transparent, card.Tool ? Style.Slate600.Lerp(Amber, 0.55f) : Style.Slate600, radius: 2);
        edge.SetBorderWidthAll(2);
        edge.SetContentMarginAll(2);
        (edge.ShadowColor, edge.ShadowSize, edge.ShadowOffset) = (new Color(0, 0, 0, 0.45f), 7, new Vector2(0, 4));
        var ring = Style.Box(Style.Slate950 with { A = 0.95f }, Ring, padding: 1, radius: 0);
        var column = Column(0, Head(card));
        if (card.Charge is { } charge) column.AddChild(Meter(charge));
        if (card.Tiles is { } tiles) column.AddChild(Tiles(tiles));
        if (Body(card) is { } body) column.AddChild(body);
        if (card.Unknown) column.AddChild(UnknownBand());
        if (card.Station is { } station) column.AddChild(Station(station));
        var outer = Boxed(edge, Boxed(ring, column));
        outer.CustomMinimumSize = new Vector2(Inside + 2 * (2 + 1), 0);
        return outer;
    }

    private static Control Head(ItemCard card)
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
        var frame = Boxed(frameBox, Icon(card.Picture, 40));
        frame.CustomMinimumSize = new Vector2(52, 52);
        frame.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
        var name = Text(card.Name, NameText);
        return Boxed(box, Column(2, frame, Spacer(4), name, Text(card.Kind, card.Tool ? ToolKindText : KindText)));
    }

    /// <summary>
    /// The charge mockup's meter, <c>border-t-2 border-slate-800 px-2.5 py-2 gap-1</c>: "CHARGE" and, in green,
    /// the percent and the seconds of drilling left; under them an 8 px bar of what it holds.
    /// </summary>
    private static Control Meter(ChargeMeter charge)
    {
        var box = new StyleBoxFlat
        {
            BgColor = Colors.Transparent,
            BorderColor = Style.Slate800,
            BorderWidthTop = 2,
            ContentMarginLeft = Pad,
            ContentMarginRight = Pad,
            ContentMarginTop = 8,
            ContentMarginBottom = 8,
        };
        var top = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        var name = Text(T("card.charge").ToUpperInvariant(), CraftText);
        name.HorizontalAlignment = HorizontalAlignment.Left;
        name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        top.AddChild(name);
        top.AddChild(Text(T("card.chargeLeft", ("n", charge.Percent), ("s", charge.Seconds)), ChargeText));
        return Boxed(box, Column(4, top, Gauge(charge.Share, 8)));
    }

    /// <summary>A bar <paramref name="height"/> px tall, <c>bg-[#0b1220] border border-slate-700</c>, filled in charge's green to <paramref name="share"/>.</summary>
    public static Control Gauge(float share, int height)
    {
        var track = new Panel { CustomMinimumSize = new Vector2(0, height), MouseFilter = Control.MouseFilterEnum.Ignore };
        track.AddThemeStyleboxOverride("panel", Style.Box(new Color("0b1220"), Style.Slate700, radius: 2));
        var fill = new ColorRect { Color = Green, MouseFilter = Control.MouseFilterEnum.Ignore };
        track.AddChild(fill);
        fill.SetAnchorsPreset(Control.LayoutPreset.FullRect);
        (fill.OffsetLeft, fill.OffsetTop, fill.OffsetBottom) = (1, 1, -1);
        fill.AnchorRight = Mathf.Clamp(share, 0, 1);
        fill.OffsetRight = share >= 1 ? -1 : 0;
        return track;
    }

    /// <summary><c>grid-cols-3 border-y-2 border-slate-800</c>, each tile after the first ruled on its left, the first in amber.</summary>
    private static Control Tiles(IReadOnlyList<CardTile> tiles)
    {
        var rules = new StyleBoxFlat { BgColor = Colors.Transparent, BorderColor = Style.Slate800, BorderWidthTop = 2, BorderWidthBottom = 2 };
        var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", 0);
        for (int i = 0; i < tiles.Count; i++)
            row.AddChild(Tile(tiles[i].Value, tiles[i].Label, first: i == 0, i == 0 ? Amber : null, i == 0 ? 19 : 16));
        return Boxed(rules, row);
    }

    /// <param name="accent">The first tile's amber, its ground faint amber; null for a plain tile.</param>
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

    /// <summary>
    /// <c>px-2.5 pt-[7px] pb-2 gap-1.5</c>, a pixel more where Godot sets text tighter, centred: a tool's
    /// effects, what it works on and its hint; anything else's "Goes into" and its chips, and its key where it
    /// has one (a canister breathed, a jetpack worn). Null for nothing to say.
    /// </summary>
    private static Control? Body(ItemCard card)
    {
        if (!card.Tool && card.Uses.Count == 0 && card.Use == null) return null;
        // Under the tiles their rule is enough; under the head, a rule of its own.
        var box = new StyleBoxFlat
        {
            BgColor = Colors.Transparent,
            BorderColor = Style.Slate800,
            BorderWidthTop = card.Tool ? 0 : 2,
            ContentMarginLeft = Pad,
            ContentMarginRight = Pad,
            ContentMarginTop = 8,
            ContentMarginBottom = 8,
        };
        var column = Column(card.Tool ? 7 : 5);
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
        if (card.Uses.Count > 0)
        {
            if (!card.Tool) column.AddChild(Text(T("card.goesInto").ToUpperInvariant(), SmallText));
            var uses = Chips();
            foreach (var use in card.Uses) uses.AddChild(Chip(use, UseText, Style.Slate800, Style.Slate700));
            column.AddChild(uses);
        }
        if (card.Use is { } how) column.AddChild(Keys([how]));
        return Boxed(box, column);
    }

    private static HFlowContainer Chips() =>
        Style.Gap(new HFlowContainer { Alignment = FlowContainer.AlignmentMode.Center, MouseFilter = Control.MouseFilterEnum.Ignore }, 4);

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

    /// <summary>Key caps, <c>border border-b-2 border-slate-600</c>, each before what it does, " · " between them, centred.</summary>
    private static Control Keys(IReadOnlyList<CardKey> keys)
    {
        var row = Style.Gap(new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = Control.MouseFilterEnum.Ignore }, 6);
        for (int i = 0; i < keys.Count; i++)
        {
            if (i > 0) row.AddChild(Text("·", HintText));
            var cap = Style.Box(Colors.Transparent, Style.Slate600, radius: 3);
            cap.BorderWidthBottom = 2;
            (cap.ContentMarginLeft, cap.ContentMarginRight, cap.ContentMarginTop, cap.ContentMarginBottom) = (4, 4, 1, 2);
            var keyCap = Boxed(cap, Text(keys[i].Key, KeyText));
            keyCap.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            row.AddChild(keyCap);
            row.AddChild(Text(keys[i].Words, HintText));
        }
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

    /// <summary>
    /// <c>border-t-2 border-slate-800 bg-slate-900/70 px-2.5 pt-[7px] pb-2 gap-1</c>: "CRAFT" and the seconds;
    /// a row an item of the cost, its icon, name, what is carried dim, ×N green or red; then the keys.
    /// </summary>
    private static Control Station(StationBlock station)
    {
        var box = new StyleBoxFlat
        {
            BgColor = Style.Slate900 with { A = 0.7f },
            BorderColor = Style.Slate800,
            BorderWidthTop = 2,
            ContentMarginLeft = Pad,
            ContentMarginRight = Pad,
            ContentMarginTop = 7,
            ContentMarginBottom = 8,
        };
        var column = Column(4);
        if (station.Seconds is { } seconds)
        {
            var top = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
            var craft = Text(T("card.craft").ToUpperInvariant(), CraftText);
            craft.HorizontalAlignment = HorizontalAlignment.Left;
            craft.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
            top.AddChild(craft);
            top.AddChild(Text(seconds, TimeText));
            column.AddChild(top);
        }
        foreach (var row in station.Rows)
        {
            column.AddChild(CostRow(row));
            if (row.From is { } from)
            {
                var where = Text(from, FromText);
                where.HorizontalAlignment = HorizontalAlignment.Left;
                // Under the name, past the icon's column.
                var indent = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
                indent.AddThemeConstantOverride("margin_left", 24);
                indent.AddChild(where);
                column.AddChild(indent);
            }
        }
        if (station.Keys.Count > 0)
        {
            if (station.Seconds != null) column.AddChild(Spacer(2));
            column.AddChild(Keys(station.Keys));
        }
        return Boxed(box, column);
    }

    /// <summary><c>grid-cols-[18px_1fr_auto] gap-1.5</c>: the icon, the name and "(have)" dim, then ×N.</summary>
    private static Control CostRow(CraftRow row)
    {
        var line = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, 6);
        var icon = new CenterContainer { CustomMinimumSize = new Vector2(18, 16), MouseFilter = Control.MouseFilterEnum.Ignore };
        icon.AddChild(Icon(row.Item, 16));
        line.AddChild(icon);
        var name = Style.Gap(new HBoxContainer { SizeFlagsHorizontal = Control.SizeFlags.ExpandFill, MouseFilter = Control.MouseFilterEnum.Ignore }, 6);
        name.AddChild(Text(row.Name, CostText));
        var have = Text($"({row.Have})", HaveText);
        have.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        name.AddChild(have);
        line.AddChild(name);
        line.AddChild(Text(T("card.many", ("n", row.Need)), row.Short ? ShortText : EnoughText));
        return line;
    }

    private static TextureRect Icon(string item, int size) => new()
    {
        Texture = ItemIcons.Icon(item),
        CustomMinimumSize = new Vector2(size, size),
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
        SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
        SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };

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
