using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim.Debris;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Debris;

/// <summary>
/// The Handbook's debris section (0066), its body in the Handbook's window: every kind of debris in rows by where to
/// look (<c>Handbook.DebrisRows</c>), those the world has found in the first look found, the rest black silhouettes,
/// and how many looks are found of all there are; beside them the kind chosen: the look chosen big, a strip of all its
/// looks (those not found black) with how many are found, its name, hit points or that it is caught by hand, and its
/// loot slot by slot, each with its range in the corner and its chance as a percent, an item none has come out of yet a
/// silhouette with its percent alone. Names, "???" and chances are in the one tooltip. Reads <c>DebrisModule.Found</c>
/// on a timer while shown, and builds again only when the record changes.
/// </summary>
public partial class HandbookPage : VBoxContainer
{
    private const double RefreshMs = 250;
    /// <summary>A kind's slot, the picture's room in it, a loot slot, and the entry's width and picture's height.</summary>
    private const int KindSide = 56, KindRoom = 48, LootSide = 40, EntryWidth = 380, PictureHeight = 124;
    /// <summary>A look's thumbnail in the strip, the picture's room in it, and the gap between them: twelve and a count fit across.</summary>
    private const int ThumbSide = 24, ThumbRoom = 20, ThumbGap = 2;

    private static readonly StyleBoxFlat Unfound = Style.Box(Style.Slate800 with { A = 0.55f }, Style.Slate600 with { A = 0.6f }, radius: 3);
    private static readonly StyleBoxFlat EntryBox = Edged(Style.Slate950 with { A = 0.95f }, Style.Slate600);
    private static readonly StyleBoxFlat PictureBox = Edged(Style.Slate800 with { A = 0.45f }, Style.Slate700);
    private static readonly LabelSettings HeadText = Style.Text(Style.Bold, 13, Style.Slate100);
    private static readonly LabelSettings TallyText = Style.Text(Style.Mono, 11, Style.Slate300);
    private static readonly LabelSettings NameText = Style.Text(Style.Bold, 14, Style.Slate100);
    private static readonly LabelSettings HpText = Style.Text(Style.Mono, 12, Style.Slate100);
    private static readonly LabelSettings HowText = Style.Text(Style.Mono, 11, Style.Slate400);
    private static readonly LabelSettings CornerText = Style.Text(Style.Mono, 10, Colors.White, shadow: true);
    private static readonly LabelSettings EmptyText = Style.Text(Style.Sans, 12, Style.Slate400);
    private static readonly Color TrackGround = new("0b1220");

    /// <summary>The section's icon in the Handbook's side list: a cargo crate as it drifts.</summary>
    public static Texture2D SectionIcon => GD.Load<Texture2D>(DebrisLooks.PictureOf("cargoCrate", 0));

    private readonly Ui _ui;
    private readonly Func<DebrisModule?> _debris;
    private readonly TipSpots _tips;
    private readonly Label _tally = new() { LabelSettings = TallyText, VerticalAlignment = VerticalAlignment.Center };
    private readonly ColorRect _tallyFill = new() { Color = SlotLooks.Green, MouseFilter = MouseFilterEnum.Ignore };
    private readonly PanelContainer _entry = new() { CustomMinimumSize = new Vector2(EntryWidth, 0), SizeFlagsVertical = SizeFlags.ShrinkBegin };
    private readonly List<(Panel Slot, TextureRect Picture, string Kind)> _kinds = [];
    /// <summary>The entry's loot slots by their item, its looks' thumbnails, and how its kind is had: for scripted runs.</summary>
    private readonly List<(Panel Slot, string Item)> _loot = [];
    private readonly List<(Panel Slot, int Look)> _thumbs = [];
    private Control? _how, _tallyBox;
    /// <summary>The record as last read: each kind found and the items seen come out of it, and its looks found.</summary>
    private Dictionary<string, string[]> _found = new(StringComparer.Ordinal);
    private Dictionary<string, int[]> _looks = new(StringComparer.Ordinal);
    private string? _read, _chosen, _hovered;
    /// <summary>The chosen kind's look clicked in its strip, shown big; none for its first found.</summary>
    private int? _look;
    private double _sinceRead = RefreshMs;
    private Vector2 _mouse;

    /// <param name="ui">The one tooltip, and the menu, under which nothing is pointed at.</param>
    /// <param name="debris">The debris module of the world played now; none on the title.</param>
    public HandbookPage(Ui ui, Func<DebrisModule?> debris)
    {
        (_ui, _debris) = (ui, debris);
        _tips = new TipSpots(ui.Tip);
        Style.Gap(this, 10);
        AddChild(Head());
        var body = Style.Gap(new HBoxContainer(), 20);
        var rows = Style.Gap(new VBoxContainer(), 4);
        foreach (var row in Stillwake.Game.Handbook.DebrisRows)
        {
            var line = Style.Gap(new HBoxContainer(), 4);
            foreach (var kind in row.Kinds) line.AddChild(KindSlot(kind));
            rows.AddChild(line);
        }
        body.AddChild(rows);
        _entry.AddThemeStyleboxOverride("panel", EntryBox);
        body.AddChild(_entry);
        AddChild(body);
    }

    /// <summary>A box with a 2 px edge, as an item's card: the entry and its picture's frame.</summary>
    private static StyleBoxFlat Edged(Color bg, Color edge)
    {
        var box = Style.Box(bg, edge, 0, 3);
        box.SetBorderWidthAll(2);
        return box;
    }

    /// <summary>
    /// "Debris", and how many looks are found of all there are, "23/81" over a thin green bar: every look is one to
    /// collect, and the kinds found show in the rows themselves; both counts in its tooltip.
    /// </summary>
    private HBoxContainer Head()
    {
        var head = Style.Gap(new HBoxContainer(), 8);
        head.AddChild(new Label { Text = T("handbook.debris"), LabelSettings = HeadText, SizeFlagsHorizontal = SizeFlags.ExpandFill });
        var tally = _tallyBox = Style.Gap(new HBoxContainer { MouseFilter = MouseFilterEnum.Stop }, 8);
        tally.AddChild(_tally);
        var track = new Panel { CustomMinimumSize = new Vector2(64, 7), SizeFlagsVertical = SizeFlags.ShrinkCenter, MouseFilter = MouseFilterEnum.Ignore };
        track.AddThemeStyleboxOverride("panel", Style.Box(TrackGround, Style.Slate700, radius: 0));
        track.AddChild(_tallyFill);
        _tallyFill.SetAnchorsPreset(LayoutPreset.FullRect);
        (_tallyFill.OffsetLeft, _tallyFill.OffsetTop, _tallyFill.OffsetBottom) = (1, 1, -1);
        tally.AddChild(track);
        _tips.Add(tally, () => Finds.TallyTip(Finds.Tally(_found.Keys), Finds.LookTally(_looks)));
        head.AddChild(tally);
        return head;
    }

    /// <summary>A kind's slot: its first look found, whole pixels where it fits; chosen by a click once found.</summary>
    private Panel KindSlot(string kind)
    {
        var slot = new Panel { CustomMinimumSize = new Vector2(KindSide, KindSide), MouseFilter = MouseFilterEnum.Stop };
        var picture = Blank();
        Fit(picture, PictureOf(kind, 0), KindSide, KindRoom);
        slot.AddChild(picture);
        slot.GuiInput += e =>
        {
            if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } || !_found.ContainsKey(kind)) return;
            (_chosen, _look) = (kind, null);
            Present();
        };
        _tips.Add(slot, () => Finds.KindTip(kind, _found.GetValueOrDefault(kind), _looks.GetValueOrDefault(kind)));
        _kinds.Add((slot, picture, kind));
        return slot;
    }

    /// <summary>Look <paramref name="look"/> of <paramref name="kind"/> (<c>DebrisLooks</c>), or its item's picture for a kind with none.</summary>
    private static Texture2D PictureOf(string kind, int look) =>
        DebrisLooks.PictureOf(kind, look) is { } path ? GD.Load<Texture2D>(path) : ItemIcons.Icon(kind);

    private static TextureRect Blank() => new()
    {
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.Scale,
        MouseFilter = MouseFilterEnum.Ignore,
    };

    /// <summary>
    /// Puts <paramref name="texture"/> in <paramref name="picture"/>, sized to fit <paramref name="room"/> square
    /// (<see cref="Finds.Fit"/>) and centred in a slot <paramref name="side"/> wide.
    /// </summary>
    private static void Fit(TextureRect picture, Texture2D texture, int side, int room)
    {
        var size = texture.GetSize();
        float scale = Finds.Fit((int)size.X, (int)size.Y, room, room);
        picture.Texture = texture;
        picture.Size = picture.CustomMinimumSize = (size * scale).Round();
        // Scaled down, nearest drops whole rows of pixels: a derelict in a thumbnail is smoothed instead.
        picture.TextureFilter = scale < 1 ? TextureFilterEnum.Linear : TextureFilterEnum.Nearest;
        picture.Position = ((new Vector2(side, side) - picture.Size) / 2).Round();
    }

    /// <summary><paramref name="texture"/> sized to fit <paramref name="maxW"/> × <paramref name="maxH"/> (<see cref="Finds.Fit"/>), for a container to place.</summary>
    private static TextureRect Picture(Texture2D texture, int maxW, int maxH)
    {
        var size = texture.GetSize();
        float scale = Finds.Fit((int)size.X, (int)size.Y, maxW, maxH);
        var picture = Blank();
        picture.Texture = texture;
        picture.TextureFilter = scale < 1 ? TextureFilterEnum.Linear : TextureFilterEnum.Nearest;
        picture.CustomMinimumSize = (size * scale).Round();
        return picture;
    }

    /// <summary>The middle of <paramref name="kind"/>'s slot on screen, for scripted runs.</summary>
    public Vector2? CenterOfKind(string kind) => _kinds.Find(k => k.Kind == kind).Slot?.GetGlobalRect().GetCenter();

    /// <summary>The middle of the entry's loot slot of <paramref name="item"/> on screen, for scripted runs.</summary>
    public Vector2? CenterOfLoot(string item) => _loot.Find(l => l.Item == item).Slot?.GetGlobalRect().GetCenter();

    /// <summary>The middle of look <paramref name="look"/>'s thumbnail in the entry's strip on screen, for scripted runs.</summary>
    public Vector2? CenterOfLook(int look) => _thumbs.Find(l => l.Look == look).Slot?.GetGlobalRect().GetCenter();

    /// <summary>The middle of the counter on screen, for scripted runs.</summary>
    public Vector2? CenterOfTally() => _tallyBox?.GetGlobalRect().GetCenter();

    /// <summary>The middle of the entry's hit points, or its "caught by hand", on screen, for scripted runs.</summary>
    public Vector2? CenterOfHow() => _how?.GetGlobalRect().GetCenter();

    public override void _Input(InputEvent e)
    {
        if (e is InputEventMouse mouse) _mouse = mouse.Position;
    }

    public override void _Process(double delta)
    {
        if (!IsVisibleInTree())
        {
            // Shown again, it reads at once.
            _sinceRead = RefreshMs;
            _tips.Frame(null);
            return;
        }
        _sinceRead += delta * 1000;
        if (_sinceRead >= RefreshMs) Read();
        var hovered = _kinds.FirstOrDefault(k => k.Slot.GetGlobalRect().HasPoint(_mouse)).Kind;
        if (hovered != _hovered)
        {
            _hovered = hovered;
            PaintKinds();
        }
        _tips.Frame(_ui.MenuOpen.Get() ? null : _mouse);
    }

    /// <summary>Reads the record; shows it again only when it has changed, a load or a resync bringing another world's.</summary>
    private void Read()
    {
        _sinceRead = 0;
        var found = _debris()?.Found ?? [];
        string read = string.Join(";", found.Select(f => $"{f.Kind}:{string.Join(",", f.Items)}:{string.Join(",", f.Looks)}"));
        if (read == _read) return;
        _read = read;
        _found = found.ToDictionary(f => f.Kind, f => f.Items.ToArray(), StringComparer.Ordinal);
        _looks = found.ToDictionary(f => f.Kind, f => f.Looks.ToArray(), StringComparer.Ordinal);
        if (_chosen == null || !_found.ContainsKey(_chosen)) (_chosen, _look) = (Finds.FirstFound(_found.Keys), null);
        Present();
    }

    /// <summary>The tally, the kinds and the entry, as the record and the choice are now.</summary>
    private void Present()
    {
        var (count, all) = Finds.LookTally(_looks);
        _tally.Text = $"{count}/{all}";
        _tallyFill.AnchorRight = all == 0 ? 0 : (float)count / all;
        _tallyFill.OffsetRight = count >= all ? -1 : 0;
        PaintKinds();
        ShowEntry();
    }

    /// <summary>Each kind's slot: found, its first look found, edged near-white when chosen; not, its first look a black silhouette in a dark slot.</summary>
    private void PaintKinds()
    {
        foreach (var (slot, picture, kind) in _kinds)
        {
            bool found = _found.ContainsKey(kind);
            slot.AddThemeStyleboxOverride("panel", found ? SlotLooks.Of(SlotTint.Plain, kind == _hovered, kind == _chosen) : Unfound);
            slot.MouseDefaultCursorShape = found ? CursorShape.PointingHand : CursorShape.Arrow;
            picture.Modulate = found ? Colors.White : Colors.Black;
            var texture = PictureOf(kind, Finds.ShownLook(_looks.GetValueOrDefault(kind), null));
            if (picture.Texture != texture) Fit(picture, texture, KindSide, KindRoom);
        }
    }

    /// <summary>The chosen kind's entry, built again: its head, then its loot; with none found, what to do.</summary>
    private void ShowEntry()
    {
        foreach (var child in _entry.GetChildren())
        {
            _entry.RemoveChild(child);
            child.QueueFree();
        }
        _how = null;
        _loot.Clear();
        _thumbs.Clear();
        if (_chosen is not string kind)
        {
            var margin = Margined(new Label
            {
                Text = T("handbook.empty"),
                LabelSettings = EmptyText,
                AutowrapMode = TextServer.AutowrapMode.WordSmart,
                HorizontalAlignment = HorizontalAlignment.Center,
                CustomMinimumSize = new Vector2(EntryWidth - 24, PictureHeight),
                VerticalAlignment = VerticalAlignment.Center,
            });
            _entry.AddChild(margin);
            return;
        }
        var column = Style.Gap(new VBoxContainer(), 0);
        var head = Style.Gap(new VBoxContainer(), 6);
        var frame = new PanelContainer { CustomMinimumSize = new Vector2(0, PictureHeight), MouseFilter = MouseFilterEnum.Ignore };
        frame.AddThemeStyleboxOverride("panel", PictureBox);
        var centre = new CenterContainer { MouseFilter = MouseFilterEnum.Ignore };
        int shown = Finds.ShownLook(_looks[kind], _look);
        centre.AddChild(Picture(PictureOf(kind, shown), EntryWidth - 32, PictureHeight - 8));
        frame.AddChild(centre);
        head.AddChild(frame);
        head.AddChild(Strip(kind, shown));
        head.AddChild(new Label { Text = T($"item.{kind}"), LabelSettings = NameText, HorizontalAlignment = HorizontalAlignment.Center });
        head.AddChild(How(kind));
        column.AddChild(Margined(head));
        column.AddChild(new ColorRect { Color = Style.Slate800, CustomMinimumSize = new Vector2(0, 2), MouseFilter = MouseFilterEnum.Ignore });
        var loot = Style.Gap(new HFlowContainer(), 6);
        foreach (var slot in Finds.SlotsOf(kind, _found[kind])) loot.AddChild(LootSlotOf(slot));
        column.AddChild(Margined(loot));
        _entry.AddChild(column);
    }

    /// <summary>
    /// Every look of <paramref name="kind"/> as a thumbnail, one not found a black silhouette, <paramref name="shown"/>
    /// edged; a click on a found one shows it big; then how many are found. Empty for a kind of one look, as tall.
    /// </summary>
    private Control Strip(string kind, int shown)
    {
        var strip = Style.Gap(new HBoxContainer { CustomMinimumSize = new Vector2(0, ThumbSide) }, ThumbGap);
        var looks = Finds.LooksOf(kind, _looks[kind]);
        if (looks.Count < 2) return strip;
        foreach (var look in looks)
        {
            var thumb = new Panel { CustomMinimumSize = new Vector2(ThumbSide, ThumbSide), MouseFilter = MouseFilterEnum.Stop };
            thumb.AddThemeStyleboxOverride("panel", look.Seen ? SlotLooks.Of(SlotTint.Plain, false, look.Look == shown) : Unfound);
            var picture = Blank();
            Fit(picture, PictureOf(kind, look.Look), ThumbSide, ThumbRoom);
            picture.Modulate = look.Seen ? Colors.White : Colors.Black;
            thumb.AddChild(picture);
            if (look.Seen)
            {
                thumb.MouseDefaultCursorShape = CursorShape.PointingHand;
                thumb.GuiInput += e =>
                {
                    if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left }) return;
                    _look = look.Look;
                    ShowEntry();
                };
            }
            _tips.Add(thumb, () => look.Tip);
            _thumbs.Add((thumb, look.Look));
            strip.AddChild(thumb);
        }
        strip.AddChild(new Label
        {
            Text = Finds.LookCount(looks),
            LabelSettings = TallyText,
            SizeFlagsHorizontal = SizeFlags.ExpandFill,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Center,
        });
        return strip;
    }

    /// <summary>How the kind is had, centred under its name: a crack and its hit points, or "Caught by hand".</summary>
    private Control How(string kind)
    {
        // As tall for hit points as for words, so choosing another kind moves nothing.
        var line = Style.Gap(new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, CustomMinimumSize = new Vector2(0, 18) }, 5);
        var stat = Style.Gap(new HBoxContainer { MouseFilter = MouseFilterEnum.Stop }, 5);
        if (Content.Wreckage.HpOf(kind) is int hp)
        {
            stat.AddChild(Icon(AsciiArt.Crack(), 14));
            stat.AddChild(new Label { Text = hp.ToString(), LabelSettings = HpText });
        }
        else stat.AddChild(new Label { Text = T("handbook.byHand"), LabelSettings = HowText });
        _tips.Add(stat, () => Finds.HowTip(kind));
        line.AddChild(_how = stat);
        return line;
    }

    /// <summary>A loot slot: its item, a black silhouette till one has come out; its range in the corner, its chance as a percent.</summary>
    private Panel LootSlotOf(LootSlot loot)
    {
        var slot = new Panel { CustomMinimumSize = new Vector2(LootSide, LootSide), MouseFilter = MouseFilterEnum.Stop };
        slot.AddThemeStyleboxOverride("panel", loot.Seen ? SlotLooks.Of(Slots.TintOf(loot.Item), false, false) : Unfound);
        _loot.Add((slot, loot.Item));
        var icon = Icon(ItemIcons.Icon(loot.Item), LootSide - 2 * Slots.Inset);
        icon.Position = new Vector2(Slots.Inset, Slots.Inset);
        icon.Modulate = loot.Seen ? Colors.White : Colors.Black;
        slot.AddChild(icon);
        // The chance in words, not a pie, which read as a timer running down.
        if (loot.Percent is { } percent)
            slot.AddChild(new Label { Text = percent, LabelSettings = CornerText, Position = new Vector2(2, 0), MouseFilter = MouseFilterEnum.Ignore });
        var corner = new Label
        {
            Text = loot.Corner,
            LabelSettings = CornerText,
            HorizontalAlignment = HorizontalAlignment.Right,
            VerticalAlignment = VerticalAlignment.Bottom,
            MouseFilter = MouseFilterEnum.Ignore,
        };
        slot.AddChild(corner);
        corner.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        (corner.OffsetRight, corner.OffsetBottom) = (-2, 2);
        _tips.Add(slot, () => loot.Tip);
        return slot;
    }

    private static TextureRect Icon(Texture2D texture, int size) => new()
    {
        Texture = texture,
        CustomMinimumSize = new Vector2(size, size),
        Size = new Vector2(size, size),
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.KeepAspectCentered,
        TextureFilter = TextureFilterEnum.Nearest,
        SizeFlagsVertical = SizeFlags.ShrinkCenter,
        MouseFilter = MouseFilterEnum.Ignore,
    };

    private static MarginContainer Margined(Control child)
    {
        var margin = new MarginContainer { MouseFilter = MouseFilterEnum.Ignore };
        foreach (var side in (string[])["margin_left", "margin_right", "margin_top", "margin_bottom"]) margin.AddThemeConstantOverride(side, 10);
        margin.AddChild(child);
        return margin;
    }
}
