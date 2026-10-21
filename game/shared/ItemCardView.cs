using System.Collections.Generic;
using System.Globalization;
using Godot;
using Stillwake.Game.Render;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Shared;

/// <summary>
/// An <see cref="ItemCard"/> in Control nodes, by what the item is (the item card mockup), each a little screen of the
/// wrist computer: dark, a faint sheen across its top, a shadow inside its edge. A tool's (layout A) edged amber: its
/// held picture large and live on top (<see cref="ToolPicture"/>), its name and kind, its tiles, the first in amber, its
/// draw, its key and hint. A worn item's, the medium card, edged green: its picture (<see cref="WornPicture"/>) beside its
/// name, kind and one number, the hint under both. Anything else's, the compact card: its name and kind on one line. Under
/// any of them what the open station does with it, by its slot's mark, with what it makes there; a violet band while
/// unknown; a station's block at the bottom.
/// </summary>
public static class ItemCardView
{
    /// <summary>The old card's inside, wide enough for a station's block, and its padding.</summary>
    private const int Inside = 228, Pad = 10;
    /// <summary>A tool's card and a worn item's, edges included, as the mockup draws them.</summary>
    private const int ToolWidth = 234, WornWidth = 284;
    /// <summary>A tool's colour: its first tile, its card's edge and kind, its recipe's tile.</summary>
    public static readonly Color Amber = new("f2b84b");
    /// <summary>Charge's green: the meter's words and its bar (the charge mockup), a worn item's kind.</summary>
    public static readonly Color Green = new(Charges.Green);
    private static readonly Color Ground = new("0f1729"), Rule = new("1b2538"), Hint = new("8a97ab");
    private static readonly Color ToolEdge = new("a58b58"), WornEdge = new("65a36a");

    // The screen's sheen, the mockup's linear-gradient(162deg, rgba(200,225,255,.055), transparent 38%).
    private static readonly GradientTexture2D Sheen = new()
    {
        Width = 64,
        Height = 64,
        FillFrom = new Vector2(0.35f, 0),
        FillTo = new Vector2(0.65f, 1),
        Gradient = new Gradient { Offsets = [0f, 0.38f], Colors = [new Color(200 / 255f, 225 / 255f, 1, 0.055f), new Color(200 / 255f, 225 / 255f, 1, 0)] },
    };

    private static readonly Font BoldMono = new FontVariation { BaseFont = Style.Mono, VariationEmbolden = 0.9f };
    private static readonly Font Spaced = new FontVariation { BaseFont = Style.Mono, SpacingGlyph = 1 };
    private static readonly LabelSettings NameText = Style.Text(Style.Bold, 14, Style.Slate100);
    private static readonly LabelSettings LabelName = Style.Text(Style.Bold, 13, Style.Slate100);
    private static readonly LabelSettings KindText = Style.Text(Style.Mono, 11, Style.Slate400);
    private static readonly LabelSettings ToolKindText = Style.Text(Style.Mono, 11, Amber);
    private static readonly LabelSettings WornKindText = Style.Text(Style.Mono, 11, Green);
    private static readonly LabelSettings HintText = Style.Text(Style.Mono, 11, Hint);
    private static readonly LabelSettings DotText = Style.Text(Style.Mono, 11, Style.Slate500);
    private static readonly LabelSettings KeyText = Style.Text(BoldMono, 10, Style.Slate300);
    private static readonly LabelSettings UnknownText = Style.Text(Style.Mono, 11, Style.Violet400);
    private static readonly LabelSettings FireText = Style.Text(Style.Mono, 11, Style.Orange400);
    private static readonly LabelSettings SmeltText = Style.Text(Style.Mono, 11, Style.Amber400);
    private static readonly LabelSettings GrowText = Style.Text(Style.Mono, 11, Style.Green400);
    private static readonly LabelSettings TechText = Style.Text(Style.Mono, 11, new Color(ItemCards.ModuleColour));
    private static readonly LabelSettings YieldText = Style.Text(Style.Mono, 11, Style.Slate300);
    private static readonly LabelSettings TimeDim = Style.Text(Style.Mono, 11, Style.Slate500);
    private static readonly LabelSettings CraftText = Style.Text(new FontVariation { BaseFont = BoldMono, SpacingGlyph = 1 }, 10, Style.Slate400);
    private static readonly LabelSettings TimeText = Style.Text(Style.Mono, 10, Style.Slate300);
    private static readonly LabelSettings CostText = Style.Text(Style.Mono, 12, Style.Slate300);
    private static readonly LabelSettings HaveText = Style.Text(Style.Mono, 11, Style.Slate500);
    private static readonly LabelSettings EnoughText = Style.Text(Style.Mono, 12, Style.Green400);
    private static readonly LabelSettings ShortText = Style.Text(Style.Mono, 12, Style.Red300);
    private static readonly LabelSettings FromText = Style.Text(Style.Mono, 10, Style.Slate500);
    private static readonly LabelSettings ChargeText = Style.Text(BoldMono, 10, Green);
    private static readonly LabelSettings StoredText = Style.Text(Style.Mono, 10, Style.Slate300);
    private static readonly LabelSettings DrawText = Style.Text(BoldMono, 16, Style.Amber400);
    private static readonly LabelSettings UnitText = Style.Text(Spaced, 9, Style.Slate500);
    private static readonly LabelSettings DetailText = Style.Text(Style.Mono, 10, Hint);

    /// <summary>The card of <paramref name="card"/>, sized to its content.</summary>
    public static Control Build(ItemCard card) => card.Tool ? ToolCard(card) : card.Worn ? WornCard(card) : Compact(card);

    /// <summary>Name and kind on one line; a key where it has one, a drone's charge; then the bands and a station's block.</summary>
    private static Control Compact(ItemCard card)
    {
        var line = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, 8);
        line.AddChild(Text(card.Name, LabelName, HorizontalAlignment.Left));
        var kind = Text(card.Kind, KindText, HorizontalAlignment.Left);
        kind.SizeFlagsVertical = Control.SizeFlags.ShrinkEnd;
        line.AddChild(kind);
        var column = Column(0, Padded(line, 9, 5, 9, 6));
        // A canister is breathed with a key of its own: the one thing its slot cannot say.
        if (card.Use is { } how) column.AddChild(Ruled(UseRow(how, null)));
        if (card.Charge is { } charge) column.AddChild(Meter(charge));
        Bottom(card, column);
        return Screen(column, Style.Slate600, 1, card.Station != null ? Inside + 6 : 0);
    }

    /// <summary>Layout A: the held picture on top, the name and kind, the tiles and the draw, the key and its hint.</summary>
    private static Control ToolCard(ItemCard card)
    {
        var column = Column(0);
        if (ToolPicture.Of(card.Item) is { } picture)
        {
            picture.SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter;
            column.AddChild(Padded(picture, 5, 5, 7, 0));
        }
        column.AddChild(Padded(Title(card, ToolKindText), 8, 6, 10, 7));
        column.AddChild(Tiles(card.Tiles!, card.Draw));
        if (card.Effects.Count > 0) column.AddChild(Padded(Effects(card.Effects), Pad, 8, Pad, 0));
        if (card.Use is { } how) column.AddChild(UseRow(how, ToolWidth));
        Bottom(card, column);
        return Screen(column, ToolEdge, 2, ToolWidth);
    }

    /// <summary>The medium card: the picture beside the name, kind and one number; the key and its hint under both.</summary>
    private static Control WornCard(ItemCard card)
    {
        var side = Column(5, Title(card, WornKindText));
        if (card.Charge is { } charge)
        {
            var held = Powered(T("card.stored", ("n", charge.Stored), ("all", charge.Capacity)), DetailText);
            side.AddChild(KeyNumber(T("card.percent", ("n", charge.Percent)), T("card.charge").ToUpperInvariant(), Green, Gauge(charge.Share, 8), held));
        }
        else if (card.Headline is { } headline)
        {
            var (r, g, b) = Light.HeadLamp;
            var detail = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, 8);
            if (card.Detail is { } words) detail.AddChild(Text(words, DetailText, HorizontalAlignment.Left));
            // What it draws, nothing for the lamp: a bolt and 0 says it is free.
            detail.AddChild(Powered((card.Draw ?? 0).ToString(CultureInfo.InvariantCulture), DetailText));
            side.AddChild(KeyNumber(headline.Value, headline.Label, new Color(r, g, b), null, detail));
        }
        side.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        var top = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, 10);
        var picture = new WornPicture(card) { SizeFlagsVertical = Control.SizeFlags.ShrinkBegin };
        top.AddChild(picture);
        top.AddChild(side);
        var column = Column(0, Padded(top, 5, 5, 10, 0));
        if (card.Use is { } how)
        {
            column.AddChild(Spacer(8));
            column.AddChild(Ruled(UseRow(how, WornWidth)));
        }
        else column.AddChild(Spacer(8));
        Bottom(card, column);
        return Screen(column, WornEdge, 2, WornWidth);
    }

    /// <summary>Under any card: what the open station does with it, the band while unknown, a station's block.</summary>
    private static void Bottom(ItemCard card, VBoxContainer column)
    {
        // A module says what it does, its tech's icon before it; the slot's mark says it fits here, no words needed (0086).
        if (card.Tech is { } tech) column.AddChild(TechStrip(tech));
        if (card.Here is not (SlotMark.None or SlotMark.Fits)) column.AddChild(HereStrip(card.Here, card.Output));
        if (card.Unknown) column.AddChild(UnknownStrip());
        if (card.Station is { } station) column.AddChild(Station(station));
    }

    /// <summary>
    /// The little screen round a card (the mockup's <c>.nc</c>): its edge <paramref name="edge"/>, <paramref name="width"/>
    /// px, round a dark ground with a faint sheen, a black ring and a shadow along its top and left inside; a soft drop
    /// shadow; at least <paramref name="least"/> px wide.
    /// </summary>
    private static Control Screen(Control content, Color edge, int width, int least)
    {
        var outer = Style.Box(Ground, edge, radius: 3);
        outer.SetBorderWidthAll(width);
        (outer.ShadowColor, outer.ShadowSize, outer.ShadowOffset) = (new Color(0, 0, 0, 0.45f), 7, new Vector2(0, 4));
        var ring = new StyleBoxFlat { BgColor = Colors.Transparent, BorderColor = new Color(0, 0, 0, 0.9f) };
        ring.SetBorderWidthAll(1);
        var inset = new StyleBoxFlat { BgColor = Colors.Transparent, BorderColor = new Color(0, 0, 0, 0.38f), BorderWidthTop = 2, BorderWidthLeft = 2 };
        var screen = Boxed(outer, Boxed(new StyleBoxTexture { Texture = Sheen }, Boxed(ring, Boxed(inset, content))));
        screen.CustomMinimumSize = new Vector2(least, 0);
        return screen;
    }

    /// <summary>The name over the kind, in the card's colour.</summary>
    private static Control Title(ItemCard card, LabelSettings kind) =>
        Column(1, Text(card.Name, NameText, HorizontalAlignment.Left), Text(card.Kind, kind, HorizontalAlignment.Left));

    /// <summary>A worn item's one number, large in its colour with what it is beside it; under it a bar and a line, where it has them.</summary>
    private static Control KeyNumber(string value, string label, Color colour, Control? bar, Control? under)
    {
        var big = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, 5);
        big.AddChild(Text(value, Style.Text(BoldMono, 20, colour), HorizontalAlignment.Left));
        var what = Text(label, UnitText, HorizontalAlignment.Left);
        what.SizeFlagsVertical = Control.SizeFlags.ShrinkEnd;
        big.AddChild(what);
        var column = Column(3, big);
        if (bar != null) column.AddChild(bar);
        if (under != null) column.AddChild(under);
        var box = new StyleBoxFlat { BgColor = Colors.Transparent, BorderColor = Rule, BorderWidthTop = 2, ContentMarginTop = 6 };
        return Boxed(box, column);
    }

    /// <summary>
    /// The charge mockup's meter, <c>border-t-2 border-slate-800 px-2.5 py-2 gap-1</c>: "CHARGE" and, in green,
    /// the percent of full; under them an 8 px bar of what it holds, and a bolt with what it holds and holds
    /// full in the units machines draw in: a drone's, on its compact card. How long it would run is never said.
    /// </summary>
    private static Control Meter(ChargeMeter charge)
    {
        var box = new StyleBoxFlat
        {
            BgColor = Colors.Transparent,
            BorderColor = Rule,
            BorderWidthTop = 2,
            ContentMarginLeft = 9,
            ContentMarginRight = 9,
            ContentMarginTop = 6,
            ContentMarginBottom = 7,
        };
        var top = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        var name = Text(T("card.charge").ToUpperInvariant(), CraftText, HorizontalAlignment.Left);
        name.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        top.AddChild(name);
        top.AddChild(Text(T("card.percent", ("n", charge.Percent)), ChargeText));
        var held = Powered(T("card.stored", ("n", charge.Stored), ("all", charge.Capacity)), StoredText);
        held.Alignment = BoxContainer.AlignmentMode.End;
        return Boxed(box, Column(4, top, Gauge(charge.Share, 8), held));
    }

    /// <summary>The power bolt before <paramref name="words"/>: a quantity in the units machines draw in.</summary>
    private static HBoxContainer Powered(string words, LabelSettings settings)
    {
        var row = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, 4);
        row.AddChild(new TextureRect
        {
            Texture = AsciiArt.Bolt(false),
            CustomMinimumSize = new Vector2(7, 9),
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
            TextureFilter = CanvasItem.TextureFilterEnum.Nearest,
            SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
            MouseFilter = Control.MouseFilterEnum.Ignore,
        });
        row.AddChild(Text(words, settings, HorizontalAlignment.Left));
        return row;
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

    /// <summary>The tiles in a row ruled above and below, each after the first ruled on its left, the first in amber; an electric tool's draw last, a bolt and the number.</summary>
    private static Control Tiles(IReadOnlyList<CardTile> tiles, int? draw)
    {
        var rules = new StyleBoxFlat { BgColor = Colors.Transparent, BorderColor = Rule, BorderWidthTop = 2, BorderWidthBottom = 2 };
        var row = new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
        row.AddThemeConstantOverride("separation", 0);
        for (int i = 0; i < tiles.Count; i++)
        {
            var accent = i == 0 ? Amber : (Color?)null;
            var number = Text(tiles[i].Value, Style.Text(BoldMono, i == 0 ? 19 : 16, accent ?? Style.Slate100));
            row.AddChild(Tile(number, tiles[i].Label, first: i == 0, accent));
        }
        if (draw is int power)
        {
            var drawn = Powered(power.ToString(CultureInfo.InvariantCulture), DrawText);
            drawn.Alignment = BoxContainer.AlignmentMode.Center;
            row.AddChild(Tile(drawn, T("card.draw"), first: false, null));
        }
        return Boxed(rules, row);
    }

    /// <param name="accent">The first tile's amber, its ground faint amber; null for a plain tile.</param>
    private static Control Tile(Control number, string label, bool first, Color? accent)
    {
        var box = new StyleBoxFlat
        {
            BgColor = accent is { } a ? a with { A = 0.1f } : Colors.Transparent,
            BorderColor = Rule,
            BorderWidthLeft = first ? 0 : 2,
            ContentMarginTop = 6,
            ContentMarginBottom = 4,
        };
        var under = Text(label, Style.Text(Spaced, 9, accent ?? Style.Slate500));
        var tile = Boxed(box, Column(3, number, under));
        tile.SizeFlagsHorizontal = Control.SizeFlags.ExpandFill;
        return tile;
    }

    /// <summary>A tool's effects as chips in their colours, then a line telling each.</summary>
    private static Control Effects(IReadOnlyList<CardEffect> effects)
    {
        var column = Column(5);
        var chips = Style.Gap(new HFlowContainer { Alignment = FlowContainer.AlignmentMode.Center, MouseFilter = Control.MouseFilterEnum.Ignore }, 4);
        foreach (var effect in effects)
        {
            var colour = new Color(effect.Colour);
            var box = Style.Box(new Color(effect.Ground), colour with { A = 0.45f }, radius: 3);
            (box.ContentMarginLeft, box.ContentMarginRight, box.ContentMarginTop, box.ContentMarginBottom) = (4, 6, 2, 2);
            chips.AddChild(Boxed(box, Text(effect.Name, Style.Text(BoldMono, 11, colour))));
        }
        column.AddChild(chips);
        foreach (var effect in effects) column.AddChild(EffectLine(effect));
        return column;
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
            CustomMinimumSize = new Vector2(ToolWidth - 6 - 2 * Pad, 0),
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

    /// <summary>
    /// The key cap and its hint, centred (the mockup's <c>.use</c>): on a card <paramref name="card"/> px wide the hint
    /// wraps, read from the left, past what the card leaves it; on a compact card it never does.
    /// </summary>
    private static Control UseRow(CardKey how, int? card)
    {
        var row = Style.Gap(new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = Control.MouseFilterEnum.Ignore }, 6);
        var cap = Cap(how.Key);
        cap.SizeFlagsVertical = Control.SizeFlags.ShrinkBegin;
        row.AddChild(cap);
        var hint = Text(how.Words, HintText, HorizontalAlignment.Left);
        if (card is int wide)
        {
            // The card's frame, the row's padding, the cap and the gap.
            float room = wide - 6 - 2 * Pad - (BoldMono.GetStringSize(how.Key, fontSize: 10).X + 8) - 6;
            if (Style.Mono.GetStringSize(how.Words, fontSize: 11).X > room)
            {
                hint.AutowrapMode = TextServer.AutowrapMode.WordSmart;
                hint.CustomMinimumSize = new Vector2(room, 0);
            }
        }
        row.AddChild(hint);
        return Padded(row, Pad, 7, Pad, 8);
    }

    /// <summary>A key cap, <c>border border-b-2 border-slate-600</c>.</summary>
    private static Control Cap(string key)
    {
        var cap = Style.Box(Colors.Transparent, Style.Slate600, radius: 3);
        cap.BorderWidthBottom = 2;
        (cap.ContentMarginLeft, cap.ContentMarginRight, cap.ContentMarginTop, cap.ContentMarginBottom) = (4, 4, 1, 2);
        return Boxed(cap, Text(key, KeyText));
    }

    /// <summary>Key caps, each before what it does, " · " between them, centred: a station's.</summary>
    private static Control Keys(IReadOnlyList<CardKey> keys)
    {
        var row = Style.Gap(new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, MouseFilter = Control.MouseFilterEnum.Ignore }, 6);
        for (int i = 0; i < keys.Count; i++)
        {
            if (i > 0) row.AddChild(Text("·", DotText));
            var keyCap = Cap(keys[i].Key);
            keyCap.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
            row.AddChild(keyCap);
            row.AddChild(Text(keys[i].Words, DotText));
        }
        return row;
    }

    /// <summary>A strip across the card's foot in <paramref name="colour"/>, ruled above (the mockup's <c>.strip</c>).</summary>
    private static Control Strip(Color colour, Control content)
    {
        var box = new StyleBoxFlat
        {
            BgColor = colour with { A = 0.12f },
            BorderColor = colour with { A = 0.35f },
            BorderWidthTop = 1,
            ContentMarginLeft = 9,
            ContentMarginRight = 9,
            ContentMarginTop = 4,
            ContentMarginBottom = 5,
        };
        return Boxed(box, content);
    }

    /// <summary>Not analysed yet: the slot's "?" mark, lit, and "Not analysed" in violet.</summary>
    private static Control UnknownStrip()
    {
        var row = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, StripGap);
        row.AddChild(StripIcon(MarkLooks.Unknown));
        row.AddChild(Text(T("card.notAnalysed"), UnknownText, HorizontalAlignment.Left));
        return Strip(Style.Violet400, Centred(row));
    }

    /// <summary>A strip's icon's side and its gap to the words, in pixels: big enough to read the mark's motion.</summary>
    private const int StripSide = 24, StripGap = 7;

    /// <summary>A strip's icon, lit and moving as the slots' marks do (0086, 0097): a module's tech, a station's mark.</summary>
    private static GlowIcon StripIcon(IconLooks.Look look) => new(look, StripSide, IconShow.Lit)
    {
        SizeFlagsHorizontal = Control.SizeFlags.ShrinkCenter,
        SizeFlagsVertical = Control.SizeFlags.ShrinkCenter,
    };

    /// <summary>A module's strip (0086): its technology's icon, then what it does, in the arcs' violet-white.</summary>
    private static Control TechStrip(CardTech tech)
    {
        var row = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, StripGap);
        row.AddChild(StripIcon(TechLooks.Of(tech.Tech)));
        row.AddChild(Text(tech.About, TechText, HorizontalAlignment.Left));
        return Strip(new Color(ItemCards.ModuleColour), Centred(row));
    }

    /// <summary>
    /// What the open station does with the item, in the mark's colour: its slot's mark, then "Smelts here" or the like,
    /// then what it makes there, "→" each yield's icon and "×N", and how long one takes, dim.
    /// </summary>
    private static Control HereStrip(SlotMark here, CardOutput? output)
    {
        var (colour, text, key) = here switch
        {
            SlotMark.Grows => (Style.Green400, GrowText, "card.growsHere"),
            SlotMark.Burns => (Style.Orange400, FireText, "card.burnsHere"),
            _ => (Style.Amber400, SmeltText, "card.smeltsHere"),
        };
        var row = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, StripGap);
        row.AddChild(StripIcon(MarkLooks.Of(here)));
        row.AddChild(Text(T(key), text));
        if (output != null)
        {
            var made = Style.Gap(new HBoxContainer { MouseFilter = Control.MouseFilterEnum.Ignore }, 3);
            made.AddChild(Text("→", YieldText));
            foreach (var (item, count) in output.Items)
            {
                made.AddChild(Picture(ItemIcons.Icon(item), 16));
                made.AddChild(Text(T("card.many", ("n", count)), YieldText));
            }
            made.AddChild(Text($" · {output.Time}", TimeDim));
            row.AddChild(made);
        }
        return Strip(colour, Centred(row));
    }

    /// <summary>A strip's row with its words and yields on the middle of its icon, not its top.</summary>
    private static Control Centred(HBoxContainer row)
    {
        foreach (var child in row.GetChildren())
            if (child is Control control) control.SizeFlagsVertical = Control.SizeFlags.ShrinkCenter;
        return row;
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
            BorderColor = Rule,
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
            var craft = Text(T("card.craft").ToUpperInvariant(), CraftText, HorizontalAlignment.Left);
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
                // Under the name, past the icon's column.
                var indent = new MarginContainer { MouseFilter = Control.MouseFilterEnum.Ignore };
                indent.AddThemeConstantOverride("margin_left", 24);
                indent.AddChild(Text(from, FromText, HorizontalAlignment.Left));
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
        icon.AddChild(Picture(ItemIcons.Icon(row.Item), 16));
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

    private static TextureRect Picture(Texture2D texture, int size) => new()
    {
        Texture = texture,
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

    /// <summary><paramref name="child"/> with room round it: left, top, right, bottom.</summary>
    private static Control Padded(Control child, int left, int top, int right, int bottom) =>
        Boxed(new StyleBoxEmpty { ContentMarginLeft = left, ContentMarginTop = top, ContentMarginRight = right, ContentMarginBottom = bottom }, child);

    /// <summary><paramref name="child"/> under a 2 px rule.</summary>
    private static Control Ruled(Control child) =>
        Boxed(new StyleBoxFlat { BgColor = Colors.Transparent, BorderColor = Rule, BorderWidthTop = 2 }, child);

    private static Control Spacer(int height) => new() { CustomMinimumSize = new Vector2(0, height), MouseFilter = Control.MouseFilterEnum.Ignore };

    private static Label Text(string text, LabelSettings settings, HorizontalAlignment align = HorizontalAlignment.Center) => new()
    {
        Text = text,
        LabelSettings = settings,
        HorizontalAlignment = align,
        MouseFilter = Control.MouseFilterEnum.Ignore,
    };
}
