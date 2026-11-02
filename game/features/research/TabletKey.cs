using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// The tablet's key on the HUD (docs/plans/board.md), one seam right of the gauge row: a press takes the tablet out, as T does, and a lamp in its metal says
/// the board has something new ([tablet-key](../../../docs/decisions/2026-10-31-tablet-key.md)). It reads for itself, for the tablet reads nothing while away.
/// </summary>
public partial class TabletKey : CanvasLayer
{
    private const double RefreshS = 0.25;

    private readonly Ui _ui;
    private readonly Func<bool> _inWorld;
    private readonly Func<Registry?> _world;
    private readonly Func<uint> _worldId;
    private readonly BoardKeep _keep;
    private readonly Key _key;
    private readonly TipSpots _tips;
    private (object Knowledge, int Version)? _readAt;
    private double _sinceRead = RefreshS;

    /// <param name="ui">The slots' size, which its place follows, and the one tooltip.</param>
    /// <param name="inWorld">Whether a world is played: with none it is hidden.</param>
    /// <param name="world">The world the hero stands in; null with none played.</param>
    /// <param name="worldId">Which world is played: what the board is kept under.</param>
    /// <param name="keep">What is kept of the board, shared with the page.</param>
    /// <param name="open">Takes the tablet out.</param>
    public TabletKey(Ui ui, Func<bool> inWorld, Func<Registry?> world, Func<uint> worldId, BoardKeep keep, Action open)
    {
        (_ui, _inWorld, _world, _worldId, _keep) = (ui, inWorld, world, worldId, keep);
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
        _key.Lit = _keep.Seen.Lit;
    }

    public override void _ExitTree() => _tips.Show(null);

    /// <summary>A wrist key whose face shows the board, lit as the sort key is, and above the face the lamp, dark or lit.</summary>
    private sealed partial class Key(Action pressed) : WristKey(new Vector2(WristLook.TabHeight, WristLook.TabHeight))
    {
        private static readonly Color Ink = new("8b98ad"), Afterglow = new("e0f2fe"), Hover = new("bae6fd"), Sky = new("38bdf8"), Held = new("7dd3fc"), LampLit = new("f2b84b"), LampOff = new("3a2f18"), Rim = new("0e1014");

        /// <summary>Three cards and the threads between them, from the glyph's corner; 14 wide, 16 tall.</summary>
        private static readonly Rect2[] Cards =
        [
            new(0, 0, 5, 5), new(9, 5, 5, 5), new(2, 11, 5, 5),
            new(5, 2, 1, 1), new(6, 3, 1, 1), new(7, 4, 1, 1), new(8, 5, 1, 1), new(3, 5, 1, 6),
        ];

        private bool _lit;

        public bool Lit
        {
            set
            {
                if (value == _lit) return;
                _lit = value;
                QueueRedraw();
            }
        }

        protected override void Pressed() => pressed();

        protected override void DrawCap(Rect2 face)
        {
            Face(face);
            var (ink, glow, strength) = State switch
            {
                KeyState.Down => (Colors.White, Held, 1f),
                KeyState.Hover => (Hover, Sky, 0.6f),
                _ => (Ink.Lerp(Afterglow, After), Sky, After),
            };
            var at = new Vector2(Mathf.Round(face.Position.X + (face.Size.X - 14) / 2), Mathf.Round(face.Position.Y + (face.Size.Y - 16) / 2) + (State == KeyState.Down ? 1 : 0));
            if (strength > 0)
            {
                Glyph(at, glow with { A = 0.12f * strength }, 2);
                Glyph(at, glow with { A = 0.3f * strength }, 1);
            }
            Glyph(at, ink, 0);
            var lamp = new Rect2(Size.X - 13, 0, 10, 4);
            DrawRect(lamp, Rim);
            DrawRect(lamp.Grow(-1), _lit ? LampLit : LampOff);
        }

        /// <summary>The cards and threads at <paramref name="at"/>; <paramref name="grow"/> wider for their glow.</summary>
        private void Glyph(Vector2 at, Color ink, float grow)
        {
            foreach (var r in Cards) DrawRect(new Rect2(at + r.Position, r.Size).Grow(grow), ink);
        }
    }
}
