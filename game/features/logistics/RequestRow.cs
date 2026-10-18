using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using Stillwake.Sim.Items;
using Stillwake.Sim.Logistics;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Logistics;

/// <summary>What a request row reads, from whatever registry the session runs now: the logistics module, the items, this peer's commands in flight.</summary>
public sealed record RequestWorld(LogisticsModule Logistics, IItemsQueries Items, IReadOnlyList<Command> InFlight);

/// <summary>
/// A machine's request slots (0062, the logistics mockup), as many as it may hold, under a heading: each the item's
/// ghost and its count, dim when met, edged amber when short, a faint "+" when empty. A click with a stack on the cursor
/// sets the item at the stack's count, the wheel changes it by one (Shift ten), a right-click clears it. Hidden while
/// the machine may hold none: a crate that is no requester. Read on a timer, and at once after its own command; what
/// it says is in the one tooltip.
/// </summary>
public partial class RequestRow : VBoxContainer
{
    private const double RefreshMs = 100;
    private static readonly StyleBoxFlat Short = Edge(Style.Amber400, 2);
    private static readonly StyleBoxFlat Plain = Edge(Style.Slate600, 1);
    private static readonly StyleBoxFlat Hover = Edge(Style.Slate400, 1);
    private static readonly StyleBoxFlat Blank = Style.Box(Style.Slate800 with { A = 0.45f }, Style.Slate600 with { A = 0.55f }, radius: 3);
    private static readonly LabelSettings HeadingText = Style.Text(Style.Bold, 11, Style.Slate400);
    private static readonly LabelSettings CountText = Style.Text(Style.Mono, 10, Style.Slate100, shadow: true);
    private static readonly LabelSettings PlusText = Style.Text(Style.Bold, 18, Style.Slate500);

    private readonly int _machine, _size;
    private readonly Func<RequestWorld?> _world;
    private readonly Action<Command> _send;
    private readonly Func<Stack?> _carried;
    private readonly TipSpots _tips;
    private readonly GridContainer _grid;
    private readonly List<(Panel Box, TextureRect Icon, Label Count, Label Plus)> _slots = [];
    private IReadOnlyList<Request> _shown = [];
    private RequestWorld? _read;
    private double _sinceRead = RefreshMs;
    private int _hovered = -1;
    private Vector2 _mouse;

    /// <param name="world">The world read, null while there is none.</param>
    /// <param name="carried">The stack on the cursor, whose item a click requests.</param>
    public RequestRow(int machine, int size, Func<RequestWorld?> world, Action<Command> send, Func<Stack?> carried, Store<Tip?> tip)
    {
        (_machine, _size, _world, _send, _carried) = (machine, size, world, send, carried);
        _tips = new TipSpots(tip);
        MouseFilter = MouseFilterEnum.Ignore;
        Style.Gap(this, 2);
        AddChild(new Label { Text = T("logistics.requests"), LabelSettings = HeadingText, MouseFilter = MouseFilterEnum.Ignore });
        AddChild(_grid = Style.Gap(new GridContainer { Columns = 9, MouseFilter = MouseFilterEnum.Ignore }, Slots.Gap));
        Visible = false;
        Read();
    }

    private static StyleBoxFlat Edge(Color border, int width)
    {
        var box = Style.Box(Style.Slate900 with { A = 0.7f }, border, radius: 3);
        box.SetBorderWidthAll(width);
        return box;
    }

    public override void _Input(InputEvent e)
    {
        if (e is InputEventMouse mouse) _mouse = mouse.Position;
    }

    public override void _Process(double delta)
    {
        _sinceRead += delta * 1000;
        if (_sinceRead >= RefreshMs) Read();
        int hovered = Visible ? _slots.FindIndex(s => s.Box.GetGlobalRect().HasPoint(_mouse)) : -1;
        if (hovered != _hovered)
        {
            _hovered = hovered;
            Paint();
        }
        _tips.Frame(IsVisibleInTree() ? _mouse : null);
    }

    public override void _ExitTree() => _tips.Show(null);

    private void Read()
    {
        _sinceRead = 0;
        _read = _world();
        int most = _read?.Logistics.RequestsMost(_machine) ?? 0;
        Visible = most > 0;
        if (most != _slots.Count) Build(most);
        _shown = _read == null ? [] : Requests.Shown(_read.Logistics.RequestsOf(_machine), _machine, _read.InFlight);
        Paint();
    }

    private void Build(int most)
    {
        foreach (var (box, _, _, _) in _slots)
        {
            _grid.RemoveChild(box);
            box.QueueFree();
        }
        _slots.Clear();
        _tips.Clear();
        _grid.Columns = Math.Max(1, Math.Min(most, 9));
        for (int i = 0; i < most; i++)
        {
            int slot = i;
            var box = new Panel { CustomMinimumSize = new Vector2(_size, _size), MouseFilter = MouseFilterEnum.Stop };
            var icon = new TextureRect
            {
                ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
                StretchMode = TextureRect.StretchModeEnum.Scale,
                TextureFilter = TextureFilterEnum.Nearest,
                MouseFilter = MouseFilterEnum.Ignore,
                Modulate = new Color(1, 1, 1, 0.55f),
            };
            box.AddChild(icon);
            icon.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize, Slots.Inset);
            var count = new Label { LabelSettings = CountText, HorizontalAlignment = HorizontalAlignment.Right, VerticalAlignment = VerticalAlignment.Bottom, MouseFilter = MouseFilterEnum.Ignore };
            box.AddChild(count);
            count.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            (count.OffsetRight, count.OffsetBottom) = (-2, 2);
            var plus = new Label { Text = "+", LabelSettings = PlusText, HorizontalAlignment = HorizontalAlignment.Center, VerticalAlignment = VerticalAlignment.Center, MouseFilter = MouseFilterEnum.Ignore };
            box.AddChild(plus);
            plus.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
            box.GuiInput += e => Press(slot, e, box);
            _tips.Add(box, () => TipOf(slot));
            _grid.AddChild(box);
            _slots.Add((box, icon, count, plus));
        }
    }

    /// <summary>A click sets the item on the cursor, a right-click clears, the wheel changes the count.</summary>
    private void Press(int slot, InputEvent e, Panel box)
    {
        if (e is not InputEventMouseButton { Pressed: true } b) return;
        IReadOnlyList<Request>? next = b.ButtonIndex switch
        {
            MouseButton.Left when _carried() is { } stack => Requests.Set(_shown, slot, stack.Item, stack.Count),
            MouseButton.Right => Requests.Clear(_shown, slot),
            MouseButton.WheelUp => Requests.Nudge(_shown, slot, Requests.StepOf(b.ShiftPressed)),
            MouseButton.WheelDown => Requests.Nudge(_shown, slot, -Requests.StepOf(b.ShiftPressed)),
            _ => null,
        };
        // The wheel over a slot is the slot's, not the hotbar's, and a click is no throw.
        box.AcceptEvent();
        if (next == null || ReferenceEquals(next, _shown)) return;
        _send(new SetRequests(_machine, next));
        Read();
    }

    private void Paint()
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            var (box, icon, count, plus) = _slots[i];
            var request = i < _shown.Count ? _shown[i] : null;
            var look = Requests.LookOf(request, request != null && _read != null ? _read.Items.HeldFor(_machine, request.Item) : 0);
            icon.Texture = request == null ? null : ItemIcons.Icon(request.Item);
            count.Text = request == null ? "" : request.Count.ToString();
            plus.Visible = request == null;
            box.AddThemeStyleboxOverride("panel", look switch
            {
                RequestLook.Unmet => Short,
                RequestLook.Empty => i == _hovered ? Hover : Blank,
                _ => i == _hovered ? Hover : Plain,
            });
            box.Modulate = look switch
            {
                RequestLook.Met => new Color(1, 1, 1, 0.55f),
                RequestLook.Empty => new Color(1, 1, 1, 0.6f),
                _ => Colors.White,
            };
        }
    }

    /// <summary>The item and count, then whether it is met, coming, or nothing in reach has it; an empty slot, how to set one.</summary>
    private Tip TipOf(int slot)
    {
        if (slot >= _shown.Count || _read == null)
            return new Tip(T("logistics.empty"), new TipLine(T("logistics.set"), Tone.Dim));
        var request = _shown[slot];
        bool met = _read.Items.HeldFor(_machine, request.Item) >= request.Count;
        var state = met ? new TipLine(T("logistics.met"), Tone.Good)
            : _read.Logistics.InReach(_machine, request.Item) ? new TipLine(T("logistics.coming"))
            : new TipLine(T("logistics.unreached"), Tone.Bad);
        return new Tip(T("logistics.request", ("item", T($"item.{request.Item}")), ("n", request.Count)), state, new TipLine(T("logistics.change"), Tone.Dim));
    }
}
