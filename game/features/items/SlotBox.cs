using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim.Items;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// One slot, 36 px square (<c>SlotBox</c> in Inventory.tsx): <c>rounded border bg-slate-800/90</c>, an
/// amber edge when chosen, faded while its stack is picked, its icon inset 4 px and its count in the corner;
/// a violet "?" in the top-left corner while its item is not analysed. Its fill is left for rarity.
/// </summary>
public partial class SlotBox : Panel
{
    private static readonly StyleBoxFlat Plain = Style.Box(Style.Slate800 with { A = 0.9f }, Style.Slate600);
    private static readonly StyleBoxFlat Chosen = Style.Box(Style.Slate800 with { A = 0.9f }, Style.Amber300);
    private static readonly StyleBoxFlat Hover = Style.Box(Style.Slate700, Style.Slate400);
    private static readonly StyleBoxFlat HoverChosen = Style.Box(Style.Slate700, Style.Amber300);
    // Equipment (0047), as the body mockup: an empty slot darker and its edge faint, a worn one edged a little
    // green, and one that would wear what is carried lit green while the others dim.
    private static readonly Color Charge = new("7ee36b");
    private static readonly StyleBoxFlat Unworn = Style.Box(Style.Slate800 with { A = 0.55f }, Style.Slate600 with { A = 0.6f });
    private static readonly StyleBoxFlat Worn = Style.Box(Style.Slate800 with { A = 0.9f }, Style.Slate700.Lerp(Charge, 0.4f));
    private static readonly StyleBoxFlat Takes = Lit();
    /// <summary>What a slot that would not wear what is carried is dimmed to.</summary>
    private const float Refused = 0.45f;
    private static readonly LabelSettings CountText = Style.Text(Style.Mono, 10, Colors.White, shadow: true);
    private static readonly LabelSettings MarkText = new()
    {
        Font = Style.Bold,
        FontSize = 16,
        FontColor = Style.Violet400,
        OutlineSize = 3,
        OutlineColor = new Color("0b0f17"),
    };

    public Place Place { get; }

    /// <summary>A press on this slot: the right button, shift held.</summary>
    public event Action<Place, bool, bool>? Pressed;

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
    private readonly Label _mark = Mark();

    public SlotBox() : this(new Place(0)) { }

    public SlotBox(Place place)
    {
        Place = place;
        CustomMinimumSize = new Vector2(36, 36);
        AddChild(_icon);
        _icon.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize, 4);
        AddChild(_mark);
        AddChild(_count);
        _count.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _count.OffsetRight = -2;
        _count.OffsetBottom = 2;
        GuiInput += OnGuiInput;
    }

    /// <summary>The violet "?" of an item not analysed yet, bold, outlined dark, for the top-left corner of a 36 px slot.</summary>
    public static Label Mark() => new()
    {
        Text = "?",
        LabelSettings = MarkText,
        Position = new Vector2(3, -4),
        Visible = false,
        MouseFilter = MouseFilterEnum.Ignore,
    };

    /// <summary>A slot lit green with a soft glow around and inside its edge: it would wear what is carried.</summary>
    private static StyleBoxFlat Lit()
    {
        var box = Style.Box(Style.Slate800 with { A = 0.9f }, Charge);
        box.ShadowColor = Charge with { A = 0.35f };
        box.ShadowSize = 3;
        return box;
    }

    /// <param name="unknown">Whether its item is not analysed yet: the "?" shows.</param>
    /// <param name="glow">An equipment slot's look while a stack is carried; null for any other slot.</param>
    public void Show(Stack? stack, bool chosen, bool picked, bool hovered, bool unknown, SlotGlow? glow = null)
    {
        _mark.Visible = stack != null && unknown;
        var box = glow switch
        {
            null => hovered ? (chosen ? HoverChosen : Hover) : chosen ? Chosen : Plain,
            SlotGlow.Takes => Takes,
            _ => hovered ? Hover : stack != null ? Worn : Unworn,
        };
        AddThemeStyleboxOverride("panel", box);
        _icon.Texture = stack == null ? null : ItemIcons.Icon(stack.Item);
        _count.Text = stack is { Count: > 1 } ? stack.Count.ToString() : "";
        Modulate = picked ? new Color(1, 1, 1, 0.4f) : glow == SlotGlow.Refuses ? new Color(1, 1, 1, Refused) : Colors.White;
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
