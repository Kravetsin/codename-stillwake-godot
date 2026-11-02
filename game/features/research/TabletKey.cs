using System;
using System.Linq;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// The tablet's key on the HUD (docs/plans/board.md), one seam right of the gauge row: a press takes the tablet out, as T does, and its cap, a frosted lens with a bulb's shade
/// in it, lights from within when the board or the market has something new ([key-lens](../../../docs/decisions/2026-11-02-key-lens.md)). It reads for itself, for the tablet reads nothing while away.
/// </summary>
public partial class TabletKey : CanvasLayer
{
    private const double RefreshS = 0.25;
    /// <summary>The market's orders: a sale moves no version, so they are read on the timer.</summary>
    private static readonly Content.TechNode[] Orders = [.. Content.Research.Nodes.Where(n => n is { Gate: true, Tab: Content.Research.Market })];

    private readonly Ui _ui;
    private readonly Func<bool> _inWorld;
    private readonly Func<Registry?> _world;
    private readonly Func<uint> _worldId;
    private readonly BoardKeep _keep;
    private readonly BulbSignal _bulb;
    private readonly Key _key;
    private readonly TipSpots _tips;
    private (object Knowledge, int Version)? _readAt;
    private double _sinceRead = RefreshS;

    /// <param name="ui">The slots' size, which its place follows, and the one tooltip.</param>
    /// <param name="inWorld">Whether a world is played: with none it is hidden.</param>
    /// <param name="world">The world the hero stands in; null with none played.</param>
    /// <param name="worldId">Which world is played: what the board is kept under.</param>
    /// <param name="keep">What is kept of the board, shared with the page.</param>
    /// <param name="bulb">The bulb over the hero, told what is new.</param>
    /// <param name="open">Takes the tablet out.</param>
    public TabletKey(Ui ui, Func<bool> inWorld, Func<Registry?> world, Func<uint> worldId, BoardKeep keep, BulbSignal bulb, Action open)
    {
        (_ui, _inWorld, _world, _worldId, _keep, _bulb) = (ui, inWorld, world, worldId, keep, bulb);
        _key = new Key(open) { Visible = false };
        void Place()
        {
            var row = Slots.GaugeRowOf(ui.Slot);
            _key.Position = new Vector2(row.EnergyX + row.EnergyWidth + WristLook.TabGap, Slots.GaugeTop(ui.Slot));
        }
        Place();
        ui.SlotScale.Subscribe(Place);
        Layer = ScreenLayers.Hud;
        AddChild(_key);
        _tips = new TipSpots(ui.Tip);
        _tips.Add(_key, () => new Tip(T("research.key")));
    }

    /// <summary>Where it lies on the screen.</summary>
    public Rect2 Covers => _key.GetGlobalRect();

    public override void _Process(double delta)
    {
        _tips.Frame(_key.IsVisibleInTree() && !_ui.TreeOpen.Get() && !_ui.MenuOpen.Get() ? _key.GetViewport().GetMousePosition() : null);
        _sinceRead += delta;
        if (_sinceRead < RefreshS) return;
        _sinceRead = 0;
        var m = _world();
        _key.Visible = m != null && _inWorld();
        if (m == null) return;
        _keep.Of(_worldId());
        var at = ((object)m.Knowledge, m.Knowledge.Version);
        if (_readAt != at)
        {
            _readAt = at;
            var read = BoardPage.ReadOf(m);
            _keep.Seen.Read(BoardView.NotesOf(BoardSlots.Of(read.Found, BoardThings.All, read.Granted), read));
        }
        _keep.Seen.ReadOrders(Orders.Count(m.Research.Done));
        _key.Lit = _keep.Seen.Lit;
        _bulb.Read(_keep.Seen, _keep.Seen.Unread.Count, _keep.Seen.Open);
    }

    public override void _ExitTree() => _tips.Show(null);

    /// <summary>A wrist key whose cap is the lens, lit by <see cref="KeyGlow"/>, with the soft light it casts on what is round it.</summary>
    private sealed partial class Key : WristKey
    {
        /// <summary>The colour of the light it casts, how far it reaches and how much of it is whole, as picked.</summary>
        private static readonly Color Light = new(250 / 255f, 200 / 255f, 120 / 255f, 0.17f);
        private const float Reach = 108, Core = 0.1f;
        /// <summary>How much brighter the light and the lens are under the pointer, and how much less while held; and how the dark lens cools under the pointer.</summary>
        private const float HoverBy = 1.15f, HeldBy = 0.75f, LensHover = 1.14f;
        private static readonly Color CoolHover = new(1.17f, 1.3f, 1.42f);

        private readonly Action _pressed;
        private readonly KeyGlow _glow = new();
        private readonly TextureRect _light = ScreenGlow.Of(Light, Core);

        public Key(Action pressed) : base(new Vector2(WristLook.TabHeight, WristLook.TabHeight))
        {
            _pressed = pressed;
            TextureFilter = TextureFilterEnum.Nearest;
            _light.Size = new Vector2(Reach, Reach);
            _light.Position = (CustomMinimumSize - _light.Size) / 2;
            _light.Visible = false;
            AddChild(_light);
        }

        public bool Lit { private get; set; }

        protected override void Pressed() => _pressed();

        public override void _Process(double delta)
        {
            base._Process(delta);
            if (_glow.Step(delta, Lit)) QueueRedraw();
            float lit = (float)_glow.Lit, by = lit * (State switch { KeyState.Hover => HoverBy, KeyState.Down => HeldBy, _ => 1 });
            _light.Visible = lit > 0;
            // Added to what is under it: its colour scales the light, as a wrist screen's lamp flares.
            _light.SelfModulate = new Color(by, by, by);
        }

        protected override void DrawCap(Rect2 face)
        {
            string down = State == KeyState.Down ? "_down" : "";
            float lit = (float)_glow.Lit;
            var tint = State == KeyState.Hover ? CoolHover.Lerp(new Color(LensHover, LensHover, LensHover), lit) : Colors.White;
            DrawRect(face.Grow(1), new Color("0e1014"));
            DrawTextureRect(PixelArt.Picture($"key_lens_dark{down}"), face, false, tint);
            if (lit > 0) DrawTextureRect(PixelArt.Picture($"key_lens_lit{down}"), face, false, tint with { A = lit });
        }
    }
}
