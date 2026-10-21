using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim.Items;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// One slot, <see cref="Slots.SizeOf"/> square (<c>SlotBox</c> in Inventory.tsx): tinted by what it holds, a
/// tool amber and a worn item green (<see cref="SlotLooks"/>), edged near-white when chosen, faded while its
/// stack is picked, its icon inset 4 px and its count in the corner; on the hotbar the key that chooses it in
/// the top-left corner, and in the top-right its mark (<see cref="SlotMark"/>), a glowing line icon (0097): a flame, a
/// crucible or a sprout while the open station burns, smelts or grows its item, or a module's tech icon where the open
/// machine takes it (0086), else a violet "?" while its item is not analysed; in the bottom-left a tool's badge, the tech icon
/// of the module fitted into it (0098). Empty, a station's slot shows the faint ghost of the mark of what goes in it
/// (<see cref="GhostMark"/>), lit while what is carried would.
/// </summary>
public partial class SlotBox : Panel
{
    // Equipment (0047), as the body mockup: an empty slot darker and its edge faint, a worn one edged a little
    // green, and one that would wear what is carried lit green while the others dim.
    private static readonly StyleBoxFlat Unworn = Style.Box(Style.Slate800 with { A = 0.55f }, Style.Slate600 with { A = 0.6f });
    private static readonly StyleBoxFlat Worn = Style.Box(Style.Slate800 with { A = 0.9f }, Style.Slate700.Lerp(SlotLooks.Green, 0.4f));
    private static readonly StyleBoxFlat EquipHover = Style.Box(Style.Slate700, Style.Slate400);
    private static readonly StyleBoxFlat Takes = Lit();
    /// <summary>What a slot that would not wear what is carried is dimmed to.</summary>
    private const float Refused = 0.45f;
    private static readonly LabelSettings CountText = Style.Text(Style.Mono, 10, Colors.White, shadow: true);
    private static readonly LabelSettings KeyText = Style.Text(Style.Mono, 9, Style.Slate400, shadow: true);

    public Place Place { get; }

    /// <summary>
    /// The mark of what goes in this station's slot (0097), its faint ghost while the slot is empty, unless drones keep it
    /// (<see cref="Kept"/>): a furnace's crucible and flame, a grow capsule's sprout, a drone bay's drone. None for the rest.
    /// </summary>
    public SlotMark GhostMark { get; set; }

    /// <summary>
    /// The item drones keep this input full of (0062): a small sky dot in its top-left corner, and while it is empty a
    /// faint ghost of the item. Null for a slot drones keep nothing in.
    /// </summary>
    public string? Kept { get; set; }

    /// <summary>How much of a kept item's own picture its ghost shows.</summary>
    private const float KeptAlpha = 0.32f;
    private static readonly Color KeptDot = Style.Sky400;

    /// <summary>A press on this slot: the right button, shift held.</summary>
    public event Action<Place, bool, bool>? Pressed;

    private readonly int _size;
    private readonly TextureRect _icon = new()
    {
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.Scale,
        TextureFilter = TextureFilterEnum.Nearest,
        MouseFilter = MouseFilterEnum.Ignore,
    };
    private readonly Label _count = new()
    {
        HorizontalAlignment = HorizontalAlignment.Right,
        VerticalAlignment = VerticalAlignment.Bottom,
        LabelSettings = CountText,
        MouseFilter = MouseFilterEnum.Ignore,
    };
    /// <summary>A kept item's faint picture while the slot is empty.</summary>
    private readonly TextureRect _kept = new()
    {
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.Scale,
        TextureFilter = TextureFilterEnum.Nearest,
        MouseFilter = MouseFilterEnum.Ignore,
        Modulate = new Color(1, 1, 1, KeptAlpha),
    };
    /// <summary>Its <see cref="GhostMark"/>'s icon, filling the inset; made when first shown.</summary>
    private GlowIcon? _ghost;
    /// <summary>Its corner's mark, the open station's or a module's tech icon or the "?"; made when first shown.</summary>
    private GlowIcon? _corner;
    /// <summary>A fitted tool's badge (0098): its module's tech icon in the bottom-left corner; made when first shown.</summary>
    private GlowIcon? _badge;
    /// <summary>A battery's charge, a thin bar along the icon's foot (0053): no numbers.</summary>
    private readonly ColorRect _track = new() { Color = new Color("0b1220"), MouseFilter = MouseFilterEnum.Ignore, Visible = false };
    private readonly ColorRect _fill = new() { MouseFilter = MouseFilterEnum.Ignore };
    private readonly Panel _keptDot = new() { MouseFilter = MouseFilterEnum.Ignore, Visible = false, Position = new Vector2(3, 3), Size = new Vector2(6, 6) };

    public SlotBox() : this(new Place(0), Slots.SizeOf(SlotScale.Normal)) { }

    /// <param name="size">Its side in pixels, <see cref="Ui.Slot"/>.</param>
    /// <param name="key">The key that chooses it, drawn small in its top-left corner: a hotbar slot's digit; none for any other.</param>
    public SlotBox(Place place, int size, string? key = null)
    {
        Place = place;
        _size = size;
        CustomMinimumSize = new Vector2(size, size);
        AddChild(_kept);
        _kept.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize, Slots.Inset);
        AddChild(_icon);
        _icon.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize, Slots.Inset);
        if (key != null) AddChild(new Label { Text = key, LabelSettings = KeyText, Position = new Vector2(3, -1), MouseFilter = MouseFilterEnum.Ignore });
        var dot = new StyleBoxFlat { BgColor = KeptDot, ShadowColor = KeptDot with { A = 0.6f }, ShadowSize = 3 };
        dot.SetCornerRadiusAll(3);
        _keptDot.AddThemeStyleboxOverride("panel", dot);
        AddChild(_keptDot);
        AddChild(_track);
        // The charge mockup's 3 px bar, inset along the foot: 4 px from the sides, 3 from its bottom.
        _track.SetAnchorsAndOffsetsPreset(LayoutPreset.BottomWide);
        (_track.OffsetLeft, _track.OffsetRight, _track.OffsetTop, _track.OffsetBottom) = (4, -4, -6, -3);
        _track.AddChild(_fill);
        AddChild(_count);
        _count.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _count.OffsetRight = -2;
        _count.OffsetBottom = 2;
        GuiInput += OnGuiInput;
    }

    /// <summary><paramref name="look"/> as a corner mark of a slot <paramref name="size"/> wide, on its dark halo (<see cref="Slots.MarkAt"/>): the slot's, the stack in hand's.</summary>
    public static GlowIcon Corner(IconLooks.Look look, int size)
    {
        var (x, y) = Slots.MarkAt(size);
        return new GlowIcon(look, Slots.MarkSide, IconShow.Mark) { Position = new Vector2(x, y) };
    }

    /// <summary>The look of mark <paramref name="mark"/> on <paramref name="item"/>: its module's tech's for one that fits, else the mark's own; none for none.</summary>
    public static IconLooks.Look? LookOf(SlotMark mark, string item) => mark switch
    {
        SlotMark.None => null,
        SlotMark.Fits => Content.Modules.TryGet(item, out var module) ? TechLooks.Of(module.Tech) : null,
        _ => MarkLooks.Of(mark),
    };

    /// <summary>The corner's mark as <paramref name="look"/>; none for null.</summary>
    private void ShowCorner(IconLooks.Look? look)
    {
        if (look == null)
        {
            if (_corner != null) _corner.Visible = false;
            return;
        }
        if (_corner == null)
        {
            AddChild(_corner = Corner(look, _size));
            MoveChild(_corner, _keptDot.GetIndex());
        }
        _corner.Set(look, IconShow.Mark);
        _corner.Visible = true;
    }

    /// <summary>The badge of the module fitted into <paramref name="stack"/>'s tool, its tech's icon, bottom-left; none for a plain stack or none.</summary>
    private void ShowBadge(Stack? stack)
    {
        if (stack?.Module is not { } module || !Content.Modules.TryGet(module, out var spec))
        {
            if (_badge != null) _badge.Visible = false;
            return;
        }
        var look = TechLooks.Of(spec.Tech);
        if (_badge == null)
        {
            var (x, y) = Slots.BadgeAt(_size);
            AddChild(_badge = new GlowIcon(look, Slots.MarkSide, IconShow.Mark) { Position = new Vector2(x, y) });
            MoveChild(_badge, _keptDot.GetIndex());
        }
        _badge.Set(look, IconShow.Mark);
        _badge.Visible = true;
    }

    /// <summary>The ghost of <paramref name="mark"/> filling the inset, lit while <paramref name="lit"/>; none for <see cref="SlotMark.None"/>.</summary>
    private void ShowGhost(SlotMark mark, bool lit)
    {
        if (mark == SlotMark.None)
        {
            if (_ghost != null) _ghost.Visible = false;
            return;
        }
        var show = lit ? IconShow.Hot : IconShow.Ghost;
        if (_ghost == null)
        {
            AddChild(_ghost = new GlowIcon(MarkLooks.Of(mark), Slots.GhostSide(_size), show) { Position = Vector2.One * Slots.Inset });
            MoveChild(_ghost, _kept.GetIndex() + 1);
        }
        _ghost.Set(MarkLooks.Of(mark), show);
        _ghost.Visible = true;
    }

    /// <summary>A slot lit green with a soft glow around and inside its edge: it would wear what is carried.</summary>
    private static StyleBoxFlat Lit()
    {
        var box = Style.Box(Style.Slate800 with { A = 0.9f }, SlotLooks.Green);
        box.ShadowColor = SlotLooks.Green with { A = 0.35f };
        box.ShadowSize = 3;
        return box;
    }

    /// <param name="mark">Its corner's mark: the open station's flame, crucible or sprout, a module's tech, or the "?" of an item not analysed yet.</param>
    /// <param name="glow">An equipment slot's look while a stack is carried; null for any other slot.</param>
    /// <param name="lights">Whether what is carried would go in it: its ghost, shown while it is empty, lit and moving.</param>
    public void Show(Stack? stack, bool chosen, bool picked, bool hovered, SlotMark mark, SlotGlow? glow = null, bool lights = false)
    {
        ShowCorner(stack == null ? null : LookOf(mark, stack.Item));
        ShowBadge(stack);
        ShowGhost(stack == null && Kept == null ? GhostMark : SlotMark.None, lights);
        var box = glow switch
        {
            null => SlotLooks.Of(stack == null ? SlotTint.Plain : Slots.TintOf(stack.Item), hovered, chosen),
            SlotGlow.Takes => Takes,
            _ => hovered ? EquipHover : stack != null ? Worn : Unworn,
        };
        AddThemeStyleboxOverride("panel", box);
        _icon.Texture = stack == null ? null : ItemIcons.Icon(Charges.Picture(stack.Item, stack.Charge));
        _kept.Texture = stack == null && Kept != null ? ItemIcons.Icon(Kept) : null;
        _keptDot.Visible = Kept != null;
        ShowCharge(stack);
        _count.Text = stack is { Count: > 1 } ? stack.Count.ToString() : "";
        Modulate = picked ? new Color(1, 1, 1, 0.4f) : glow == SlotGlow.Refuses ? new Color(1, 1, 1, Refused) : Colors.White;
    }

    /// <summary>The thin bar of a battery's charge, green, amber under a quarter, empty at none; hidden for any other item.</summary>
    private void ShowCharge(Stack? stack)
    {
        var bar = stack == null ? null : Charges.BarOf(stack);
        _track.Visible = bar != null;
        if (bar == null) return;
        _fill.Color = bar == ChargeBar.Low ? new Color(Charges.Amber) : new Color(Charges.Green);
        _fill.Visible = bar != ChargeBar.Flat;
        _fill.Size = new Vector2(Mathf.Max(1, Mathf.Round((_size - 8) * Charges.Share(stack!))), 3);
    }

    /// <summary>Whether a stack shifted here still flies to it (<see cref="SlotFlights"/>): its icon, count and mark hidden till it lands.</summary>
    public void Awaiting(bool flying)
    {
        var shown = new Color(1, 1, 1, flying ? 0 : 1);
        _icon.SelfModulate = shown;
        _count.SelfModulate = shown;
        if (_corner != null) _corner.SelfModulate = shown;
        if (_badge != null) _badge.SelfModulate = shown;
    }

    private Tween? _shake;

    /// <summary>A shift-click with nowhere to send its stack: the icon shakes side to side, a fifth of a second, and settles.</summary>
    public void Shake()
    {
        _shake?.Kill();
        _shake = CreateTween();
        _shake.TweenMethod(Callable.From<float>(q =>
            _icon.Position = new Vector2(Slots.Inset + 2.5f * Mathf.Sin(q * Mathf.Tau * 3) * (1 - q), Slots.Inset)), 0f, 1f, 0.22);
    }

    /// <summary>The left button acts as it goes down, so a press can begin a drag; a click is a press let go.</summary>
    private void OnGuiInput(InputEvent e)
    {
        if (e is not InputEventMouseButton { Pressed: true } b) return;
        if (b.ButtonIndex is not (MouseButton.Left or MouseButton.Right)) return;
        AcceptEvent();
        Pressed?.Invoke(Place, b.ButtonIndex == MouseButton.Right, b.ShiftPressed);
    }
}
