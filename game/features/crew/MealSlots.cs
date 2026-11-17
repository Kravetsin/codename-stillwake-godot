using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using static Stillwake.Game.I18n.En;
using Diet = Stillwake.Content.Diet;
using Foods = Stillwake.Content.Foods;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// The hero's three meals, the vitals' third to fifth dials (hud-vitals, food-and-the-body, the pick «dials» A1): each the dish's own
/// picture, the hotbar's, in a ring of light that empties back to the top as the meal runs out, both pulsing in its last minute, the
/// time it has left under it (<see cref="MealTips.Clock"/>). Eaten, the picture pops in and the ring flashes; refused, red light
/// behind and a jolt; ended, it greys and fades. An empty slot is a faint ring; hungry, the empty rings breathe ochre and hunger's sign
/// lights in the first, its costs on hover. What a meal does is on hover, its food card with the time left
/// (2026-11-14-food-and-gear-cards). The purge spot is the sixth dial. Reads the sim four times a second.
/// </summary>
public partial class MealSlots : CanvasLayer
{
    private const double RefreshS = 0.25, EatS = 0.6, EndS = 0.8, HungerS = 2.6, HungerFadeS = 0.4;
    private const float R = Vitals.RingRadius, Light = Vitals.RingLight;
    private static readonly Vector2 Half = new(Vitals.Dial / 2, Vitals.Dial / 2);

    private readonly Func<(IReadOnlyList<(string Food, int Left)> Meals, bool Hungry)?> _read;
    private readonly Control _row = new() { MouseFilter = Control.MouseFilterEnum.Ignore };
    private readonly Slot[] _slots = new Slot[Diet.Meals];
    private readonly TipSpots _tips;
    private readonly Ui _ui;
    private double _sinceRead = RefreshS, _clock, _hungerLit;
    private bool _hungry;

    /// <param name="read">This player's hero's meals with their ticks left, and whether it is hungry; null without a hero, which hides the slots.</param>
    /// <param name="purge">Throws up every meal (food-tuning), held to the end on the purge spot: the refusal, or null once sent.</param>
    /// <param name="ui">The slots' size the block follows, the one tooltip, and a meal refused.</param>
    public MealSlots(Func<(IReadOnlyList<(string Food, int Left)> Meals, bool Hungry)?> read, Func<string?> purge, Ui ui)
    {
        (_read, _ui) = (read, ui);
        _purge = new PurgeSpot(purge)
        {
            Position = new Vector2(Vitals.Column(Vitals.PurgeDial), Vitals.RowY) - Half,
            Size = new Vector2(Vitals.Dial, Vitals.Dial),
        };
        _tips = new TipSpots(ui.Tip);
        Layer = ScreenLayers.Hud;
        AddChild(_row);
        for (int i = 0; i < _slots.Length; i++)
        {
            var slot = _slots[i] = new Slot
            {
                Position = new Vector2(Vitals.Column(Vitals.FirstMealDial + i), Vitals.RowY) - Half,
                Size = new Vector2(Vitals.Dial, Vitals.Dial + Vitals.CircleGap),
                MouseFilter = Control.MouseFilterEnum.Ignore,
                Variant = i,
            };
            _row.AddChild(slot);
            _tips.Add(slot, () => slot.Food is { } food ? MealTips.Of(food, slot.Left) : MealTips.Empty(_hungry));
        }
        _row.AddChild(_light = new AddedLayer(on =>
        {
            foreach (var slot in _slots) slot.DrawLight(on);
        }));
        _row.AddChild(_purge);
        _tips.Add(_purge, () => new Tip(T("hud.purge"), []));
        ui.MealRefused += Refused;
    }

    private readonly PurgeSpot _purge;
    private readonly AddedLayer _light;

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
                _purge.Full = meals.Count > 0;
            }
        }
        _purge.Step(delta);
        _purge.QueueRedraw();
        // Hunger's ochre and sign fade in and out, breathing while lit.
        _hungerLit = Math.Clamp(_hungerLit + (_hungry ? delta : -delta) / HungerFadeS, 0, 1);
        float breath = 0.45f + 0.35f * (0.5f - 0.5f * Mathf.Cos((float)(_clock * Mathf.Tau / HungerS)));
        int sign = Array.FindIndex(_slots, s => s.Food == null);
        for (int i = 0; i < _slots.Length; i++)
        {
            _slots[i].Step(delta, _clock, (float)_hungerLit, breath, i == sign);
            _slots[i].QueueRedraw();
        }
        _light.QueueRedraw();
        _tips.Frame(Visible && !_ui.MenuOpen.Get() ? GetViewport().GetMousePosition() : null);
    }

    /// <summary>
    /// The purge spot, the sixth dial (food-tuning): held <see cref="HoldS"/>, a bile ring filling round it as round the oxygen sign
    /// while a canister cools, it throws up every meal; let go early, nothing. Dim with nothing eaten; refused, it blinks red. A first
    /// draft of its sign: a mouth spilling three drops.
    /// </summary>
    private sealed partial class PurgeSpot(Func<string?> purge) : Control
    {
        private const double HoldS = 3;
        // The sign was drawn in a ring of 14: it grows with the ring to the dials'.
        private const float K = R / 14;
        private static readonly Color Bile = new("c8c070"), Red = new(1, 70 / 255f, 60 / 255f);

        public bool Full;
        private readonly FlatBlink _blink = new();
        private double _held = -1;

        public override void _Ready() => MouseFilter = MouseFilterEnum.Stop;

        public override void _GuiInput(InputEvent e)
        {
            if (e is not InputEventMouseButton { ButtonIndex: MouseButton.Left } button) return;
            _held = button.Pressed ? 0 : -1;
            AcceptEvent();
        }

        public override void _Notification(int what)
        {
            if (what == NotificationMouseExit) _held = -1;
        }

        public void Step(double delta)
        {
            _blink.Step(delta);
            if (_held < 0) return;
            _held += delta;
            if (_held < HoldS) return;
            _held = -1;
            if (purge() != null) _blink.Press();
        }

        public override void _Draw()
        {
            var centre = Size / 2 + new Vector2(_blink.Jolt.X * 1.6f, 0);
            float alpha = Full ? 0.9f : 0.35f;
            RingLight.Thin(this, centre, R, 0, Mathf.Tau, Style.Slate700 with { A = 0.9f * alpha });
            if (_held > 0)
            {
                float end = -Mathf.Pi / 2 + Mathf.Tau * (float)Math.Min(1, _held / HoldS);
                RingLight.Thin(this, centre, R, -Mathf.Pi / 2, end, Bile with { A = 0.95f });
                DrawRect(new Rect2(centre + new Vector2(Mathf.Cos(end), Mathf.Sin(end)) * R - Vector2.One, new Vector2(2, 2)), Colors.White);
            }
            if (_blink.Red > 0) RingLight.Thin(this, centre, R, 0, Mathf.Tau, Red with { A = 0.9f * (float)_blink.Red });
            // The mouth, and three drops falling from it.
            var ink = Bile with { A = alpha };
            DrawArc(centre + new Vector2(0, -5 * K), 4.5f * K, 0.2f, Mathf.Pi - 0.2f, 10, ink, 1.5f * K, true);
            foreach (var (dx, dy, r) in new[] { (-2.5f, 2f, 1.3f), (1f, 4f, 1.6f), (3f, 0.5f, 1.1f) })
                DrawCircle(centre + new Vector2(dx, dy) * K, r * K, ink);
        }
    }

    private sealed partial class Slot : Control
    {
        private static readonly Color Red = new(1, 70 / 255f, 60 / 255f), Eaten = new("ffe0a0"), Number = new("d6deea");

        public string? Food;
        public int Left;
        public int Variant;
        public FlatBlink Blink { get; } = new();
        private string? _ended;
        private double _eat = -1, _end = -1;
        private float _pulse = 1, _hungerLit, _breath;
        private bool _sign;

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

        /// <param name="hungerLit">How far hunger's ochre and sign are lit, 0 to 1.</param>
        /// <param name="breath">Hunger's breath now.</param>
        /// <param name="sign">Whether hunger's sign stands in this slot: the first empty one.</param>
        public void Step(double delta, double clock, float hungerLit, float breath, bool sign)
        {
            Blink.Step(delta);
            if (_eat >= 0) _eat = _eat + delta < EatS ? _eat + delta : -1;
            if (_end >= 0) _end = _end + delta < EndS ? _end + delta : -1;
            _pulse = Food != null && MealTips.Pulsing(Left) ? Vitals.Pulse(clock * 2.4) : 1;
            (_hungerLit, _breath, _sign) = (Food == null ? hungerLit : 0, breath, sign);
        }

        private float Share => Food is { } food ? Math.Clamp((float)Left / (Foods.Of(food)?.Lasts ?? 1), 0, 1) : 0;

        private void Ring(CanvasItem on, string tone, float share, float alpha) =>
            RingLight.Arc(on, LightStrip.Of(tone, Variant), Half, R, Light, -Mathf.Pi / 2, -Mathf.Pi / 2 + Mathf.Tau * share, Colors.White with { A = alpha });

        public override void _Draw()
        {
            Ring(this, "pale", 1, 0.16f);
            if (_hungerLit > 0) Ring(this, "amber", 1, _hungerLit * _breath * 0.75f);
            var jolt = new Vector2(Blink.Jolt.X * 1.6f, 0);
            if (Blink.Red > 0) RingLight.Spot(this, Half + jolt, 16, Red with { A = 0.7f * (float)Blink.Red });
            var icon = new Vector2(Vitals.Icon, Vitals.Icon);
            if (_end >= 0 && _ended != null && Food == null)
                DrawTextureRect(Greyed.Of(ItemIcons.Icon(_ended)), new Rect2(Half - icon / 2, icon), false, Colors.White with { A = 1 - (float)(_end / EndS) });
            if (_sign && _hungerLit > 0)
                DrawTextureRect(Signs.Hunger(), new Rect2(Half - icon / 2, icon), false, Colors.White with { A = _hungerLit * (0.35f + 0.8f * _breath) });
            if (Food is not { } food) return;
            Ring(this, "pale", Share, 0.9f * _pulse);
            float pop = _eat >= 0 ? 1 + 0.3f * (1 - Mathf.SmoothStep(0, 1, (float)Math.Min(1, _eat / 0.35))) : 1;
            var side = icon * pop;
            DrawTextureRect(ItemIcons.Icon(food), new Rect2(Half - side / 2 + jolt, side), false, Colors.White with { A = 0.65f + 0.35f * _pulse });
            DialNumber.Draw(this, [MealTips.Clock(Left)], new Vector2(Half.X, Vitals.NumberY - Position.Y), GetParent<Control>().Scale.X, Number with { A = _pulse });
        }

        /// <summary>The slot's light, added on the row's layer: the glow under its ring, the flash and warmth of a meal just eaten.</summary>
        public void DrawLight(CanvasItem on)
        {
            if (Food == null) return;
            on.DrawSetTransform(Position);
            float share = Share;
            if (share > 0.002f) RingLight.Glow(on, LightStrip.Of("pale", Variant), Half, R, Light, -Mathf.Pi / 2, -Mathf.Pi / 2 + Mathf.Tau * share, 0.45f * _pulse);
            if (_eat >= 0)
            {
                float fresh = 1 - (float)(_eat / EatS);
                RingLight.Glow(on, LightStrip.Of("white", Variant), Half, R, Light, -Mathf.Pi / 2, 3 * Mathf.Pi / 2, 0.9f * fresh);
                RingLight.Spot(on, Half, 14, Eaten with { A = 0.6f * fresh });
            }
            on.DrawSetTransform(Vector2.Zero);
        }
    }
}
