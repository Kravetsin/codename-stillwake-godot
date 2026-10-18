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
/// look (<c>Handbook.DebrisRows</c>), those the world has found in their own picture, the rest black silhouettes, and
/// how many are found; beside them the kind chosen: its picture, name, hit points or that it is caught by hand, and its
/// loot slot by slot, each with its range in the corner and a pie of its chance, an item none has come out of yet a
/// silhouette with its pie alone. Names, "???" and chances are in the one tooltip. Reads <c>DebrisModule.Found</c>
/// on a timer while shown, and builds again only when the record changes.
/// </summary>
public partial class HandbookPage : VBoxContainer
{
    private const double RefreshMs = 250;
    /// <summary>A kind's slot, the picture's room in it, a loot slot, and the entry's width and picture's height.</summary>
    private const int KindSide = 56, KindRoom = 48, LootSide = 40, EntryWidth = 380, PictureHeight = 124;

    private static readonly StyleBoxFlat Unfound = Style.Box(Style.Slate800 with { A = 0.55f }, Style.Slate600 with { A = 0.6f }, radius: 3);
    private static readonly StyleBoxFlat EntryBox = Edged(Style.Slate950 with { A = 0.95f }, Style.Slate600);
    private static readonly StyleBoxFlat PictureBox = Edged(Style.Slate800 with { A = 0.45f }, Style.Slate700);
    private static readonly LabelSettings HeadText = Style.Text(Style.Bold, 13, Style.Slate100);
    private static readonly LabelSettings TallyText = Style.Text(Style.Mono, 11, Style.Slate300);
    private static readonly LabelSettings NameText = Style.Text(Style.Bold, 14, Style.Slate100);
    private static readonly LabelSettings HpText = Style.Text(Style.Mono, 12, Style.Slate100);
    private static readonly LabelSettings CornerText = Style.Text(Style.Mono, 10, Colors.White, shadow: true);
    private static readonly LabelSettings EmptyText = Style.Text(Style.Sans, 12, Style.Slate400);
    private static readonly Color PieGround = new("0b1220");

    /// <summary>The section's icon in the Handbook's side list: a cargo crate as it drifts.</summary>
    public static Texture2D SectionIcon => GD.Load<Texture2D>(DebrisLooks.PictureOf("cargoCrate", 0));

    private readonly Ui _ui;
    private readonly Func<DebrisModule?> _debris;
    private readonly TipSpots _tips;
    private readonly Label _tally = new() { LabelSettings = TallyText, VerticalAlignment = VerticalAlignment.Center };
    private readonly ColorRect _tallyFill = new() { Color = SlotLooks.Green, MouseFilter = MouseFilterEnum.Ignore };
    private readonly PanelContainer _entry = new() { CustomMinimumSize = new Vector2(EntryWidth, 0), SizeFlagsVertical = SizeFlags.ShrinkBegin };
    private readonly List<(Panel Slot, TextureRect Picture, string Kind)> _kinds = [];
    /// <summary>The entry's loot slots by their item, and how its kind is had: for scripted runs.</summary>
    private readonly List<(Panel Slot, string Item)> _loot = [];
    private Control? _how;
    /// <summary>The record as last read: each kind found and the items seen come out of it.</summary>
    private Dictionary<string, string[]> _found = new(StringComparer.Ordinal);
    private string? _read, _chosen, _hovered;
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

    /// <summary>"Debris", and how many of its kinds are found: "10/18" over a thin green bar.</summary>
    private HBoxContainer Head()
    {
        var head = Style.Gap(new HBoxContainer(), 8);
        head.AddChild(new Label { Text = T("handbook.debris"), LabelSettings = HeadText, SizeFlagsHorizontal = SizeFlags.ExpandFill });
        head.AddChild(_tally);
        var track = new Panel { CustomMinimumSize = new Vector2(64, 7), SizeFlagsVertical = SizeFlags.ShrinkCenter, MouseFilter = MouseFilterEnum.Ignore };
        track.AddThemeStyleboxOverride("panel", Style.Box(PieGround, Style.Slate700, radius: 0));
        track.AddChild(_tallyFill);
        _tallyFill.SetAnchorsPreset(LayoutPreset.FullRect);
        (_tallyFill.OffsetLeft, _tallyFill.OffsetTop, _tallyFill.OffsetBottom) = (1, 1, -1);
        head.AddChild(track);
        return head;
    }

    /// <summary>A kind's slot: its picture, whole pixels where it fits; chosen by a click once found.</summary>
    private Panel KindSlot(string kind)
    {
        var slot = new Panel { CustomMinimumSize = new Vector2(KindSide, KindSide), MouseFilter = MouseFilterEnum.Stop };
        var picture = Picture(PictureOf(kind), KindRoom, KindRoom);
        slot.AddChild(picture);
        picture.Position = ((new Vector2(KindSide, KindSide) - picture.Size) / 2).Round();
        slot.GuiInput += e =>
        {
            if (e is not InputEventMouseButton { Pressed: true, ButtonIndex: MouseButton.Left } || !_found.ContainsKey(kind)) return;
            _chosen = kind;
            Present();
        };
        _tips.Add(slot, () => Finds.KindTip(kind, _found.GetValueOrDefault(kind)));
        _kinds.Add((slot, picture, kind));
        return slot;
    }

    /// <summary>The picture a piece of <paramref name="kind"/> wears first, or its item's for a kind with none.</summary>
    private static Texture2D PictureOf(string kind) =>
        DebrisLooks.PictureOf(kind, 0) is { } path ? GD.Load<Texture2D>(path) : ItemIcons.Icon(kind);

    /// <summary><paramref name="texture"/> sized to fit <paramref name="maxW"/> × <paramref name="maxH"/> (<see cref="Finds.Fit"/>).</summary>
    private static TextureRect Picture(Texture2D texture, int maxW, int maxH)
    {
        var size = texture.GetSize();
        float scale = Finds.Fit((int)size.X, (int)size.Y, maxW, maxH);
        return new TextureRect
        {
            Texture = texture,
            ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
            StretchMode = TextureRect.StretchModeEnum.Scale,
            TextureFilter = TextureFilterEnum.Nearest,
            Size = (size * scale).Round(),
            CustomMinimumSize = (size * scale).Round(),
            MouseFilter = MouseFilterEnum.Ignore,
        };
    }

    /// <summary>The middle of <paramref name="kind"/>'s slot on screen, for scripted runs.</summary>
    public Vector2? CenterOfKind(string kind) => _kinds.Find(k => k.Kind == kind).Slot?.GetGlobalRect().GetCenter();

    /// <summary>The middle of the entry's loot slot of <paramref name="item"/> on screen, for scripted runs.</summary>
    public Vector2? CenterOfLoot(string item) => _loot.Find(l => l.Item == item).Slot?.GetGlobalRect().GetCenter();

    /// <summary>The middle of the entry's hit points, or its harpoon, on screen, for scripted runs.</summary>
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
        string read = string.Join(";", found.Select(f => $"{f.Kind}:{string.Join(",", f.Items)}"));
        if (read == _read) return;
        _read = read;
        _found = found.ToDictionary(f => f.Kind, f => f.Items.ToArray(), StringComparer.Ordinal);
        if (_chosen == null || !_found.ContainsKey(_chosen)) _chosen = Finds.FirstFound(_found.Keys);
        Present();
    }

    /// <summary>The tally, the kinds and the entry, as the record and the choice are now.</summary>
    private void Present()
    {
        var (count, all) = Finds.Tally(_found.Keys);
        _tally.Text = $"{count}/{all}";
        _tallyFill.AnchorRight = all == 0 ? 0 : (float)count / all;
        _tallyFill.OffsetRight = count >= all ? -1 : 0;
        PaintKinds();
        ShowEntry();
    }

    /// <summary>Each kind's slot: found, its picture as it is, edged near-white when chosen; not, a black silhouette in a dark slot.</summary>
    private void PaintKinds()
    {
        foreach (var (slot, picture, kind) in _kinds)
        {
            bool found = _found.ContainsKey(kind);
            slot.AddThemeStyleboxOverride("panel", found ? SlotLooks.Of(SlotTint.Plain, kind == _hovered, kind == _chosen) : Unfound);
            slot.MouseDefaultCursorShape = found ? CursorShape.PointingHand : CursorShape.Arrow;
            picture.Modulate = found ? Colors.White : Colors.Black;
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
        centre.AddChild(Picture(PictureOf(kind), EntryWidth - 32, PictureHeight - 8));
        frame.AddChild(centre);
        head.AddChild(frame);
        head.AddChild(new Label { Text = T($"item.{kind}"), LabelSettings = NameText, HorizontalAlignment = HorizontalAlignment.Center });
        head.AddChild(How(kind));
        column.AddChild(Margined(head));
        column.AddChild(new ColorRect { Color = Style.Slate800, CustomMinimumSize = new Vector2(0, 2), MouseFilter = MouseFilterEnum.Ignore });
        var loot = Style.Gap(new HFlowContainer(), 6);
        foreach (var slot in Finds.SlotsOf(kind, _found[kind])) loot.AddChild(LootSlotOf(slot));
        column.AddChild(Margined(loot));
        _entry.AddChild(column);
    }

    /// <summary>How the kind is had, centred under its name: a crack and its hit points, or the harpoon for one caught by hand.</summary>
    private Control How(string kind)
    {
        // As tall for hit points as for the harpoon, so choosing another kind moves nothing.
        var line = Style.Gap(new HBoxContainer { Alignment = BoxContainer.AlignmentMode.Center, CustomMinimumSize = new Vector2(0, 24) }, 5);
        var stat = Style.Gap(new HBoxContainer { MouseFilter = MouseFilterEnum.Stop }, 5);
        if (Content.Wreckage.HpOf(kind) is int hp)
        {
            stat.AddChild(Icon(AsciiArt.Crack(), 14));
            stat.AddChild(new Label { Text = hp.ToString(), LabelSettings = HpText });
        }
        else stat.AddChild(Icon(ItemIcons.Icon(Content.Harpoon.Item), 24));
        _tips.Add(stat, () => Finds.HowTip(kind));
        line.AddChild(_how = stat);
        return line;
    }

    /// <summary>A loot slot: its item, a black silhouette till one has come out; its range in the corner, its chance as a pie.</summary>
    private Panel LootSlotOf(LootSlot loot)
    {
        var slot = new Panel { CustomMinimumSize = new Vector2(LootSide, LootSide), MouseFilter = MouseFilterEnum.Stop };
        slot.AddThemeStyleboxOverride("panel", loot.Seen ? SlotLooks.Of(Slots.TintOf(loot.Item), false, false) : Unfound);
        _loot.Add((slot, loot.Item));
        var icon = Icon(ItemIcons.Icon(loot.Item), LootSide - 2 * Slots.Inset);
        icon.Position = new Vector2(Slots.Inset, Slots.Inset);
        icon.Modulate = loot.Seen ? Colors.White : Colors.Black;
        slot.AddChild(icon);
        if (loot.OneIn is int oneIn) slot.AddChild(Pie(oneIn));
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

    /// <summary>A pie in the slot's top-left corner, its share one in <paramref name="oneIn"/>, from twelve o'clock clockwise.</summary>
    private static Control Pie(int oneIn)
    {
        var pie = new Control { Position = new Vector2(2, 2), Size = new Vector2(11, 11), MouseFilter = MouseFilterEnum.Ignore };
        pie.Draw += () =>
        {
            var c = pie.Size / 2;
            float r = c.X - 0.5f;
            pie.DrawCircle(c, r, PieGround);
            const int Steps = 24;
            var wedge = new Vector2[Steps + 2];
            wedge[0] = c;
            for (int i = 0; i <= Steps; i++)
            {
                float a = -Mathf.Pi / 2 + Mathf.Tau / oneIn * i / Steps;
                wedge[i + 1] = c + new Vector2(Mathf.Cos(a), Mathf.Sin(a)) * r;
            }
            pie.DrawColoredPolygon(wedge, Style.Slate300);
            pie.DrawArc(c, r, 0, Mathf.Tau, 24, Style.Slate300, 1, true);
        };
        return pie;
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
