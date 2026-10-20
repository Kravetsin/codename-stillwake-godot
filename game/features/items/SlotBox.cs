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
/// the top-left corner, and in the top-right its mark (<see cref="SlotMark"/>): a small flame or sprout while the open
/// station smelts, burns or grows its item, else a violet "?" while its item is not analysed. Empty, it may show a faint
/// silhouette of what goes in it (<see cref="Ghost"/>).
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
    private static readonly LabelSettings MarkText = new()
    {
        Font = Style.Bold,
        FontSize = 16,
        FontColor = Style.Violet400,
        OutlineSize = 3,
        OutlineColor = new Color("0b0f17"),
    };

    public Place Place { get; }

    /// <summary>What goes in this slot, shown as a faint silhouette while it is empty: a grow capsule's seed (0060).</summary>
    public string? Ghost { get; set; }

    /// <summary>How much of its white a ghost shows.</summary>
    private const float GhostAlpha = 0.16f;

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
    private readonly TextureRect _ghost = new()
    {
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.Scale,
        TextureFilter = TextureFilterEnum.Nearest,
        MouseFilter = MouseFilterEnum.Ignore,
        Modulate = new Color(1, 1, 1, GhostAlpha),
    };
    private readonly Label _mark;
    /// <summary>The open station's mark, a flame or a sprout, where the "?" sits and before it.</summary>
    private readonly TextureRect _sign;
    /// <summary>A battery's charge, a thin bar along the icon's foot (0053): no numbers.</summary>
    private readonly ColorRect _track = new() { Color = new Color("0b1220"), MouseFilter = MouseFilterEnum.Ignore, Visible = false };
    private readonly ColorRect _fill = new() { MouseFilter = MouseFilterEnum.Ignore };
    private readonly Panel _kept = new() { MouseFilter = MouseFilterEnum.Ignore, Visible = false, Position = new Vector2(3, 3), Size = new Vector2(6, 6) };

    public SlotBox() : this(new Place(0), Slots.SizeOf(SlotScale.Normal)) { }

    /// <param name="size">Its side in pixels, <see cref="Ui.Slot"/>.</param>
    /// <param name="key">The key that chooses it, drawn small in its top-left corner: a hotbar slot's digit; none for any other.</param>
    public SlotBox(Place place, int size, string? key = null)
    {
        Place = place;
        _size = size;
        CustomMinimumSize = new Vector2(size, size);
        AddChild(_ghost);
        _ghost.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize, Slots.Inset);
        AddChild(_icon);
        _icon.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize, Slots.Inset);
        if (key != null) AddChild(new Label { Text = key, LabelSettings = KeyText, Position = new Vector2(3, -1), MouseFilter = MouseFilterEnum.Ignore });
        AddChild(_mark = Mark(size));
        AddChild(_sign = Sign(size));
        var dot = new StyleBoxFlat { BgColor = KeptDot, ShadowColor = KeptDot with { A = 0.6f }, ShadowSize = 3 };
        dot.SetCornerRadiusAll(3);
        _kept.AddThemeStyleboxOverride("panel", dot);
        AddChild(_kept);
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

    /// <summary>The violet "?" of an item not analysed yet, bold, outlined dark, for the top-right corner of a slot <paramref name="size"/> wide: the top-left is the hotbar's digit.</summary>
    public static Label Mark(int size) => new()
    {
        Text = "?",
        LabelSettings = MarkText,
        Position = new Vector2(size - 12, -4),
        Visible = false,
        MouseFilter = MouseFilterEnum.Ignore,
    };

    /// <summary>The open station's mark, its 7 px picture drawn twice its size, where the "?" sits in a slot <paramref name="size"/> wide.</summary>
    private static TextureRect Sign(int size) => new()
    {
        Position = new Vector2(size - 16, 2),
        Size = new Vector2(14, 14),
        ExpandMode = TextureRect.ExpandModeEnum.IgnoreSize,
        StretchMode = TextureRect.StretchModeEnum.Scale,
        TextureFilter = TextureFilterEnum.Nearest,
        MouseFilter = MouseFilterEnum.Ignore,
    };

    /// <summary>A slot lit green with a soft glow around and inside its edge: it would wear what is carried.</summary>
    private static StyleBoxFlat Lit()
    {
        var box = Style.Box(Style.Slate800 with { A = 0.9f }, SlotLooks.Green);
        box.ShadowColor = SlotLooks.Green with { A = 0.35f };
        box.ShadowSize = 3;
        return box;
    }

    /// <param name="mark">Its corner's mark: the open station's flame or sprout, or the "?" of an item not analysed yet.</param>
    /// <param name="glow">An equipment slot's look while a stack is carried; null for any other slot.</param>
    public void Show(Stack? stack, bool chosen, bool picked, bool hovered, SlotMark mark, SlotGlow? glow = null)
    {
        _mark.Visible = stack != null && mark == SlotMark.Unknown;
        _sign.Texture = stack != null ? ItemCardView.MarkPicture(mark) : null;
        var box = glow switch
        {
            null => SlotLooks.Of(stack == null ? SlotTint.Plain : Slots.TintOf(stack.Item), hovered, chosen),
            SlotGlow.Takes => Takes,
            _ => hovered ? EquipHover : stack != null ? Worn : Unworn,
        };
        AddThemeStyleboxOverride("panel", box);
        _icon.Texture = stack == null ? null : ItemIcons.Icon(Charges.Picture(stack.Item, stack.Charge));
        _ghost.Texture = stack != null ? null : Kept != null ? ItemIcons.Icon(Kept) : Ghost != null ? ItemIcons.Silhouette(Ghost) : null;
        _ghost.Modulate = new Color(1, 1, 1, Kept != null ? KeptAlpha : GhostAlpha);
        _kept.Visible = Kept != null;
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

    /// <summary>Whether a stack shifted here still flies to it (<see cref="SlotFlights"/>): its icon and count hidden till it lands.</summary>
    public void Awaiting(bool flying)
    {
        var shown = new Color(1, 1, 1, flying ? 0 : 1);
        _icon.SelfModulate = shown;
        _count.SelfModulate = shown;
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
