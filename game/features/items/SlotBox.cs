using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Sim.Items;

namespace Stillwake.Game.Features.Items;

/// <summary>
/// One slot, 36 px square (<c>SlotBox</c> in Inventory.tsx): <c>rounded border bg-slate-800/90</c>, an
/// amber edge when chosen, faded while its stack is picked, its icon inset 4 px and its count in the corner.
/// </summary>
public partial class SlotBox : Panel
{
    private static readonly StyleBoxFlat Plain = Style.Box(Style.Slate800 with { A = 0.9f }, Style.Slate600);
    private static readonly StyleBoxFlat Chosen = Style.Box(Style.Slate800 with { A = 0.9f }, Style.Amber300);
    private static readonly StyleBoxFlat Hover = Style.Box(Style.Slate700, Style.Slate400);
    private static readonly StyleBoxFlat HoverChosen = Style.Box(Style.Slate700, Style.Amber300);
    private static readonly LabelSettings CountText = Style.Text(Style.Mono, 10, Colors.White, shadow: true);

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

    public SlotBox() : this(new Place(0)) { }

    public SlotBox(Place place)
    {
        Place = place;
        CustomMinimumSize = new Vector2(36, 36);
        AddChild(_icon);
        _icon.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect, LayoutPresetMode.Minsize, 4);
        AddChild(_count);
        _count.SetAnchorsAndOffsetsPreset(LayoutPreset.FullRect);
        _count.OffsetRight = -2;
        _count.OffsetBottom = 2;
        GuiInput += OnGuiInput;
    }

    public void Show(Stack? stack, bool chosen, bool picked, bool hovered)
    {
        AddThemeStyleboxOverride("panel", hovered ? (chosen ? HoverChosen : Hover) : chosen ? Chosen : Plain);
        _icon.Texture = stack == null ? null : ItemIcons.Icon(stack.Item);
        _count.Text = stack is { Count: > 1 } ? stack.Count.ToString() : "";
        Modulate = picked ? new Color(1, 1, 1, 0.4f) : Colors.White;
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
