using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Diet = Stillwake.Content.Diet;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The hero's three meals under the suit's gauge (food plan, A2): each food's icon, pulsing in its last minute; empty
/// slots when there is room, a red rim round all three when hungry; what each does on hover. A plain stand-in until the
/// gauges' redesign places food on the HUD. Reads the sim four times a second.
/// </summary>
public partial class MealSlots : CanvasLayer
{
    private const double RefreshS = 0.25;
    private const int Side = 16, Gap = 2, Below = 1;

    private readonly Func<(IReadOnlyList<(string Food, int Left)> Meals, bool Hungry)?> _read;
    private readonly Slot[] _slots = new Slot[Diet.Meals];
    private readonly TipSpots _tips;
    private readonly Ui _ui;
    private double _sinceRead = RefreshS, _clock;
    private bool _hungry;

    /// <param name="read">This player's hero's meals with their ticks left, and whether it is hungry; null without a hero, which hides the slots.</param>
    /// <param name="ui">The slots' size the gauge row follows, and the one tooltip.</param>
    public MealSlots(Func<(IReadOnlyList<(string Food, int Left)> Meals, bool Hungry)?> read, Ui ui)
    {
        (_read, _ui) = (read, ui);
        _tips = new TipSpots(ui.Tip);
        Layer = ScreenLayers.Hud;
        for (int i = 0; i < _slots.Length; i++)
        {
            var slot = _slots[i] = new Slot { Size = new Vector2(Side, Side), MouseFilter = Control.MouseFilterEnum.Ignore };
            AddChild(slot);
            _tips.Add(slot, () => slot.Food is { } food ? MealTips.Of(food, slot.Left) : MealTips.Empty(_hungry));
        }
        void Place()
        {
            var row = Slots.GaugeRowOf(ui.Slot);
            float top = Slots.GaugeTop(ui.Slot) + WristLook.TabHeight + Below;
            for (int i = 0; i < _slots.Length; i++) _slots[i].Position = new Vector2(row.SuitX + i * (Side + Gap), top);
        }
        Place();
        ui.SlotScale.Subscribe(Place);
    }

    public override void _Process(double delta)
    {
        _clock += delta;
        _sinceRead += delta;
        if (_sinceRead >= RefreshS)
        {
            _sinceRead = 0;
            var read = _read();
            Visible = read != null;
            if (read is var (meals, hungry))
            {
                _hungry = hungry;
                for (int i = 0; i < _slots.Length; i++)
                    (_slots[i].Food, _slots[i].Left) = i < meals.Count ? meals[i] : (null, 0);
            }
        }
        // A slow breath of light, once a second and a half, while a meal is in its last minute.
        float pulse = 0.55f + 0.45f * (float)Math.Cos(_clock * Math.Tau / 1.5);
        foreach (var slot in _slots)
        {
            slot.Alpha = slot.Food != null && MealTips.Pulsing(slot.Left) ? pulse : 1;
            slot.Hungry = _hungry;
            slot.QueueRedraw();
        }
        _tips.Frame(Visible && !_ui.MenuOpen.Get() ? GetViewport().GetMousePosition() : null);
    }

    private sealed partial class Slot : Control
    {
        public string? Food;
        public int Left;
        public float Alpha = 1;
        public bool Hungry;

        public override void _Draw()
        {
            var box = new Rect2(Vector2.Zero, Size);
            DrawRect(box, Style.Slate900 with { A = 0.55f });
            DrawRect(box, Hungry ? Style.Red300 with { A = 0.8f } : Style.Slate600 with { A = 0.6f }, filled: false, width: 1);
            if (Food is { } food) DrawTextureRect(ItemIcons.Icon(food), box, tile: false, modulate: new Color(1, 1, 1, Alpha));
        }
    }
}
