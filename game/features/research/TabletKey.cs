using System;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim;
using static Stillwake.Game.I18n.En;

namespace Stillwake.Game.Features.Research;

/// <summary>
/// The tablet's key on the HUD (docs/plans/board.md), one seam right of the gauge row: a press takes the tablet out, as T does, and a dot on it says
/// the board has something new. It reads for itself, for the tablet reads nothing while away.
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

    /// <summary>A wrist key whose face shows the tablet, lit as the sort key is, and in its corner the dot.</summary>
    private sealed partial class Key(Action pressed) : WristKey(new Vector2(WristLook.TabHeight, WristLook.TabHeight))
    {
        private static readonly Color Ink = new("8b98ad"), Afterglow = new("e0f2fe"), Hover = new("bae6fd"), Sky = new("38bdf8"), Held = new("7dd3fc"), Dot = new("f2b84b"), Rim = new("0e1014");

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
            var at = new Vector2(Mathf.Round(face.Position.X + (face.Size.X - 12) / 2), Mathf.Round(face.Position.Y + (face.Size.Y - 14) / 2) + (State == KeyState.Down ? 1 : 0));
            if (strength > 0)
            {
                Glyph(at, glow with { A = 0.12f * strength }, 2);
                Glyph(at, glow with { A = 0.3f * strength }, 1);
            }
            Glyph(at, ink, 0);
            if (!_lit) return;
            var dot = new Rect2(Size.X - 10, 2, 8, 8);
            DrawRect(dot, Rim);
            DrawRect(dot.Grow(-2), Dot);
        }

        /// <summary>A tablet: its frame, two lines of writing on its screen; <paramref name="grow"/> wider for its glow.</summary>
        private void Glyph(Vector2 at, Color ink, float grow)
        {
            float x = at.X, y = at.Y;
            DrawRect(new Rect2(x, y, 12, 2).Grow(grow), ink);
            DrawRect(new Rect2(x, y + 12, 12, 2).Grow(grow), ink);
            DrawRect(new Rect2(x, y, 2, 14).Grow(grow), ink);
            DrawRect(new Rect2(x + 10, y, 2, 14).Grow(grow), ink);
            DrawRect(new Rect2(x + 3, y + 4, 6, 2).Grow(grow), ink);
            DrawRect(new Rect2(x + 3, y + 8, 4, 2).Grow(grow), ink);
        }
    }
}
