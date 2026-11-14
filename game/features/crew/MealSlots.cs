using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Diet = Stillwake.Content.Diet;
using Foods = Stillwake.Content.Foods;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The hero's three meals, the vitals' bottom row (hud-vitals, food-and-the-body): each the dish's own picture, the hotbar's,
/// over a thin time light that drains as the meal runs out, both pulsing in its last minute. Eaten, the picture pops in;
/// refused, red light behind and a jolt; ended, it greys and fades. Hungry, the empty time lights breathe ochre. What a meal
/// does is on hover, its food card with the time left (2026-11-14-food-and-gear-cards). Reads the sim four times a second.
/// </summary>
public partial class MealSlots : CanvasLayer
{
    private const double RefreshS = 0.25, EatS = 0.6, EndS = 0.8, HungerS = 2.6;

    private readonly Func<(IReadOnlyList<(string Food, int Left)> Meals, bool Hungry)?> _read;
    private readonly Control _row = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly Slot[] _slots = new Slot[Diet.Meals];
    private readonly TipSpots _tips;
    private readonly Ui _ui;
    private double _sinceRead = RefreshS, _clock;
    private bool _hungry;

    /// <param name="read">This player's hero's meals with their ticks left, and whether it is hungry; null without a hero, which hides the slots.</param>
    /// <param name="ui">The slots' size the block follows, the one tooltip, and a meal refused.</param>
    public MealSlots(Func<(IReadOnlyList<(string Food, int Left)> Meals, bool Hungry)?> read, Ui ui)
    {
        (_read, _ui) = (read, ui);
        _tips = new TipSpots(ui.Tip);
        Layer = ScreenLayers.Hud;
        AddChild(_row);
        for (int i = 0; i < _slots.Length; i++)
        {
            var slot = _slots[i] = new Slot
            {
                Position = new Vector2(Vitals.RowLeft + i * Vitals.MealPitch, Vitals.MealTop),
                Size = new Vector2(Vitals.MealSide, Vitals.TimeTop + Vitals.TimeHeight - Vitals.MealTop),
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Variant = i,
            };
            _row.AddChild(slot);
            _tips.Add(slot, () => slot.Food is { } food ? MealTips.Of(food, slot.Left) : MealTips.Empty(_hungry));
        }
        ui.MealRefused += Refused;
    }

    /// <summary>A meal refused: full, every slot blinks red; the same dish too soon, only its slot.</summary>
    private void Refused(string food, string problem)
    {
        foreach (var slot in _slots)
            if (problem == "crew.mealsFull" || slot.Food == food) slot.Blink.Press();
    }

    public override void _Process(double delta)
    {
        _clock += delta;
        var (x, y) = Vitals.Origin(GetViewport().GetVisibleRect().Size.Y, _ui.Slot);
        float scale = Vitals.ScaleOf(_ui.Slot);
        (_row.Position, _row.Scale) = (new Vector2(x, y), new Vector2(scale, scale));
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
                {
                    var (food, left) = i < meals.Count ? meals[i] : (null, 0);
                    _slots[i].Read(food, left);
                }
            }
        }
        float hunger = _hungry ? 0.45f + 0.35f * (0.5f - 0.5f * Mathf.Cos((float)(_clock * Mathf.Tau / HungerS))) : 0;
        foreach (var slot in _slots)
        {
            slot.Step(delta, _clock, hunger);
            slot.QueueRedraw();
        }
        _tips.Frame(Visible && !_ui.MenuOpen.Get() ? GetViewport().GetMousePosition() : null);
    }

    private sealed partial class Slot : Control
    {
        private static readonly Color Red = new(1, 70 / 255f, 60 / 255f), Eaten = new("dcd6ea");
        private const float TimeY = Vitals.TimeTop - Vitals.MealTop;

        public string? Food;
        public int Left;
        public int Variant;
        public FlatBlink Blink { get; } = new();
        private string? _ended;
        private double _eat = -1, _end = -1;
        private float _pulse = 1, _hunger;

        /// <summary>A read: a dish newly in the slot pops in; one gone greys and fades.</summary>
        public void Read(string? food, int left)
        {
            if (food != Food)
            {
                if (Food != null) (_ended, _end) = (Food, 0);
                if (food != null) _eat = 0;
            }
            else if (food != null && left > Left + Diet.RefreshWithin) _eat = 0;
            (Food, Left) = (food, left);
        }

        public void Step(double delta, double clock, float hunger)
        {
            Blink.Step(delta);
            if (_eat >= 0) _eat = _eat + delta < EatS ? _eat + delta : -1;
            if (_end >= 0) _end = _end + delta < EndS ? _end + delta : -1;
            _pulse = Food != null && MealTips.Pulsing(Left) ? Vitals.Pulse(clock * 2.4) : 1;
            _hunger = Food == null ? hunger : 0;
        }

        public override void _Draw()
        {
            var time = new Rect2(0, TimeY, Vitals.MealSide, Vitals.TimeHeight);
            LightStrip.Stretched(this, LightStrip.Thin("darkMeal", Variant), time, Colors.White with { A = 0.95f });
            if (_hunger > 0) LightStrip.Stretched(this, LightStrip.Thin("hunger", Variant), time, Colors.White with { A = _hunger });
            var jolt = new Vector2(Blink.Jolt.X * 1.6f, 0);
            if (Blink.Red > 0) Glow(new Vector2(Vitals.MealSide / 2, Vitals.MealSide / 2) + jolt, 16, Red with { A = 0.7f * (float)Blink.Red });
            if (_end >= 0 && _ended != null && Food == null)
                DrawTextureRect(Greyed.Of(ItemIcons.Icon(_ended)), new Rect2(Vector2.Zero, new Vector2(Vitals.MealSide, Vitals.MealSide)), false, Colors.White with { A = 1 - (float)(_end / EndS) });
            if (Food is not { } food) return;
            float pop = _eat >= 0 ? 1 + 0.3f * (1 - Mathf.SmoothStep(0, 1, (float)Math.Min(1, _eat / 0.35))) : 1;
            if (_eat >= 0) Glow(new Vector2(Vitals.MealSide / 2, Vitals.MealSide / 2), 14, Eaten with { A = 0.6f * (1 - (float)(_eat / EatS)) });
            float side = Vitals.MealSide * pop;
            var at = new Vector2((Vitals.MealSide - side) / 2, (Vitals.MealSide - side) / 2) + jolt;
            DrawTextureRect(ItemIcons.Icon(food), new Rect2(at, new Vector2(side, side)), false, Colors.White with { A = 0.65f + 0.35f * _pulse });
            int lasts = Foods.Of(food)?.Lasts ?? 1;
            float lit = Vitals.MealSide * Math.Clamp((float)Left / lasts, 0, 1);
            LightStrip.Stretched(this, LightStrip.Thin("time", Variant), time, Colors.White with { A = _pulse }, lit);
        }

        private void Glow(Vector2 at, float radius, Color colour) =>
            DrawTextureRect(PixelArt.Glow, new Rect2(at - new Vector2(radius, radius), new Vector2(2 * radius, 2 * radius)), false, colour);
    }
}
