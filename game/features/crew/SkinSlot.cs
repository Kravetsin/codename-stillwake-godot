using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// One slot of the Appearance tab (skins), as an equipment slot looks: a skin's card (a suit's cut from the hero wearing it,
/// <c>skins/&lt;id&gt;</c>), a weapon effect's own swing caught still (<see cref="BladeFx"/>), or for none a slashed ring; a skin this player may not pick dimmed under a padlock.
/// </summary>
public partial class SkinSlot : Control
{
    private static readonly Color Mark = Style.Slate500, Lock = new("e2e8f0"), LockShade = new("0b1220");
    private static readonly Dictionary<string, Texture2D?> Cards = new();
    /// <summary>The room round a card inside the slot, px.</summary>
    private const float Inset = 3;

    private readonly int _size;
    private string? _id;
    private bool _owned = true, _chosen, _hovered;
    private EffectPreview? _preview;
    private Texture2D? _card;

    /// <summary>A left click on it.</summary>
    public event Action? Pressed;

    public SkinSlot(int size)
    {
        _size = size;
        CustomMinimumSize = new Vector2(size, size);
        Size = CustomMinimumSize;
        MouseFilter = MouseFilterEnum.Stop;
        MouseEntered += () => Hover(true);
        MouseExited += () => Hover(false);
        GuiInput += e =>
        {
            if (e is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true }) return;
            AcceptEvent();
            Pressed?.Invoke();
        };
    }

    /// <summary>Shows skin <paramref name="id"/>, none for none; whether this player may pick it, and whether it is the one chosen.</summary>
    public void Show(string? id, bool owned, bool chosen)
    {
        if (id != _id)
        {
            _preview?.QueueFree();
            _preview = id != null && SkinEffects.Of(id) is { } look ? new EffectPreview(look, _size) : null;
            if (_preview != null) AddChild(_preview);
            _card = id == null || _preview != null ? null : CardOf(id);
        }
        (_id, _owned, _chosen) = (id, owned, chosen);
        if (_preview != null) _preview.Modulate = new Color(1, 1, 1, owned ? 1 : 0.4f);
        QueueRedraw();
    }

    private void Hover(bool on)
    {
        _hovered = on;
        QueueRedraw();
    }

    public override void _Draw()
    {
        DrawStyleBox(SlotLooks.Of(_chosen ? SlotTint.Worn : SlotTint.Plain, _hovered, false), new Rect2(Vector2.Zero, Size));
        if (_card != null) DrawTextureRect(_card, new Rect2(new Vector2(Inset, Inset), Size - new Vector2(2 * Inset, 2 * Inset)), false, new Color(1, 1, 1, _owned ? 1 : 0.4f));
        var middle = Size / 2;
        if (_id == null)
        {
            float r = _size * 0.22f;
            DrawArc(middle, r, 0, Mathf.Tau, 24, Mark, 1.5f, true);
            DrawLine(middle + new Vector2(-r, r) * 0.7f, middle + new Vector2(r, -r) * 0.7f, Mark, 1.5f, true);
        }
        if (!_owned) Padlock(new Vector2(Size.X - 9, Size.Y - 9));
    }

    private static Texture2D? CardOf(string id)
    {
        if (!Cards.TryGetValue(id, out var card)) Cards[id] = card = HiRes.Load($"skins/{id}") is { } image ? HiRes.Texture(image) : null;
        return card;
    }

    // A padlock 8 px wide round its middle at the slot's foot corner: a shackle over a body, on a dark halo.
    private void Padlock(Vector2 at)
    {
        DrawCircle(at, 6.5f, LockShade with { A = 0.8f });
        DrawArc(at + new Vector2(0, -1.5f), 2.4f, Mathf.Pi, Mathf.Tau, 10, Lock, 1.4f, true);
        DrawRect(new Rect2(at + new Vector2(-3.5f, -1.5f), new Vector2(7, 5.5f)), Lock);
        DrawRect(new Rect2(at + new Vector2(-0.5f, 0.3f), new Vector2(1, 2)), LockShade);
    }

    /// <summary>
    /// A weapon effect caught mid-swing on a blade drawn by its edge alone, in its own three layers as the hero's is
    /// (<see cref="BladeFxView"/>): what the effect is, not a stand-in for it.
    /// </summary>
    private sealed partial class EffectPreview : Control
    {
        /// <summary>The swing: its pivot, the edge's near and far reach from it (px of the world), the turn it sweeps (radians) and in how many frames.</summary>
        private const double Near = 9, Far = 21, From = -2.1, To = -0.25;
        private const int Frames = 8;
        private const double Frame = 1000.0 / 60, Across = 30;

        public EffectPreview(EffectLook look, int size)
        {
            MouseFilter = MouseFilterEnum.Ignore;
            Size = new Vector2(size, size);
            var fx = BladeFx.Of(look);
            BladeFx.Held? held = null;
            for (int i = 0; i <= Frames; i++)
            {
                double a = From + (To - From) * i / Frames;
                var edge = new (double X, double Y)[HeldLook.EdgePoints];
                var lip = new EdgeLip.Point[HeldLook.EdgePoints];
                for (int j = 0; j < edge.Length; j++)
                {
                    double r = Near + (Far - Near) * j / (edge.Length - 1);
                    edge[j] = (Math.Cos(a) * r, Math.Sin(a) * r);
                    // The lip faces the way the blade swings, as an axe's edge leads it.
                    lip[j] = new(edge[j].X, edge[j].Y, -Math.Sin(a), Math.Cos(a), (double)j / (edge.Length - 1));
                }
                held = new(p => p, lip, edge);
                fx.Step(Frame, held, edge);
            }
            // The swing's pivot at the slot's foot left: it sweeps up over and down to the right.
            float k = size / (float)Across;
            var origin = new Vector2(size * 0.3f, size * 0.8f);
            var inks = (Under: new Ink { Feather = 1 / k }, Dark: new Ink { Feather = 1 / k }, Over: new Ink { Feather = 1 / k });
            fx.Draw(inks.Under, inks.Dark, inks.Over, p => p);
            foreach (var (ink, blend) in new[] { (inks.Under, CanvasItemMaterial.BlendModeEnum.Add), (inks.Dark, CanvasItemMaterial.BlendModeEnum.Mix), (inks.Over, CanvasItemMaterial.BlendModeEnum.Add) })
                AddChild(new Layer(ink) { Position = origin, Scale = new Vector2(k, k), Material = new CanvasItemMaterial { BlendMode = blend } });
            ClipContents = true;
        }

        private sealed partial class Layer(Ink ink) : Control
        {
            private readonly InkCanvas _canvas = new();

            public override void _Ready() => MouseFilter = MouseFilterEnum.Ignore;

            public override void _Draw() => _canvas.Draw(this, ink);
        }
    }
}
