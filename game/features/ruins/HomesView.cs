using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Features.Light;
using Stillwake.Game.Features.Sound;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim.Building;
using Stillwake.Sim.Grid;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Ruins;

/// <summary>
/// The arc makers' homes, seen (0091): each piece the sim keeps standing (<see cref="BuildingModule.Plants"/> of a kind in
/// <see cref="Spec.Ruins.Pieces"/>) drawn from one atlas of the page's pictures, lit as the world is, what glows added over it
/// unlit; its lamps' light handed to the light feature (<see cref="Lights"/>); and what the page set moving — arcs, pulses, a
/// sputtering lamp, the mobile, the hologram, the last pixel, twinkling beads, spores, embers, motes and swaying cloth — while
/// it is in view. A hit shakes a piece and knocks chips off it, the music box plays its tune, a felled one puffs dust; the
/// resonator's arc leaps to a hero it zaps. A few hundred pieces on a planet: two canvas items for them all, drawn each frame
/// for those in view.
/// </summary>
public partial class HomesView : Node2D, ILightSource
{
    private const int Cell = ShipCamera.Cell;
    /// <summary>The page's live layer steps at 24 a second: its emitters' rates and lives are in those ticks.</summary>
    private const double TickSeconds = 1.0 / 24;

    private readonly IGridQueries _grid;
    private readonly BuildingModule _building;
    private readonly Func<IReadOnlyList<Vector2>> _heroes;
    private readonly Painter _pictures, _shine;
    private readonly Heard2D _tine, _zap;
    /// <summary>A piece felled heard where it stood, by what it is made of, whoever felled it (0093).</summary>
    private readonly Voices _falls = new(Game.Sound.World, voices: 3);
    private readonly List<Shown> _shown = [];
    private readonly Dictionary<int, Shown> _byCell = new();
    private readonly List<Shown> _inView = [];
    private readonly List<LightSource> _lights = [];
    private readonly List<(LightSource Light, float Far)> _near = [];
    private readonly Dictionary<int, int> _cracks = new();
    private readonly List<(double At, float Pitch, Vector2 Where)> _tune = [];
    private readonly Random _random = new();
    private readonly List<Heart> _hearts = [];
    private int _count = -1;
    private double _seconds, _owed, _zapHeard;
    private Rect2 _view;

    /// <summary>
    /// For recordings (<c>--homes=lit</c>, <c>--homes=dark</c>): every lamp that may go out lit, or all of them out, the page's two
    /// moods; null for the mix by the world's seed. Read as pieces are placed.
    /// </summary>
    public bool? Mood { get; init; }

    /// <summary>The light of the homes' lamps near the view this frame, the nearest its middle first.</summary>
    public IReadOnlyList<LightSource> Lights => _lights;

    /// <param name="heroes">Where each hero's body is on this surface, in pixels of the world: the resonator's arc leaps to it.</param>
    public HomesView(IGridQueries grid, BuildingModule building, Func<IReadOnlyList<Vector2>> heroes)
    {
        (_grid, _building, _heroes) = (grid, building, heroes);
        AddChild(_pictures = new Painter(this, glow: false) { ZIndex = Layers.Flora });
        Lit.Set(_pictures);
        AddChild(_shine = new Painter(this, glow: true) { ZIndex = Layers.Flora, Material = GlowMaterial });
        AddChild(_tine = Cues.Player2D(Game.Sound.MusicBox, Game.Sound.World, voices: 4));
        AddChild(_zap = Cues.Player2D(Game.Sound.Zap, Game.Sound.World, voices: 2));
        AddChild(_falls);
    }

    /// <summary>The mean time this view took a frame since it was last asked, in milliseconds: for <c>--measure</c>.</summary>
    public double TakeCostMs()
    {
        double ms = _frames > 0 ? _spentMs / _frames : 0;
        (_spentMs, _frames) = (0, 0);
        return ms;
    }

    private double _spentMs;
    private int _frames;

    public override void _Process(double delta)
    {
        long start = System.Diagnostics.Stopwatch.GetTimestamp();
        _seconds += delta;
        if (_building.Plants.Count != _count) Follow();
        var view = _view = GetViewport().GetCanvasTransform().AffineInverse() * GetViewportRect();
        _inView.Clear();
        var near = view.Grow(12 * Cell);
        foreach (var s in _shown)
            if (near.Intersects(s.Bounds)) _inView.Add(s);
        Hits(delta);
        // The page's live layer, a 24th of a second at a time, only for what is in view.
        _owed = Math.Min(_owed + delta, 0.25);
        for (; _owed >= TickSeconds; _owed -= TickSeconds)
            foreach (var s in _inView) Tick(s);
        foreach (var s in _inView) Glow(s);
        Light(view);
        Zaps(delta);
        Play();
        _pictures.QueueRedraw();
        _shine.QueueRedraw();
        _spentMs += System.Diagnostics.Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        _frames++;
    }

    /// <summary>
    /// Brings the pieces shown up to those standing: a new one placed, a felled one taken away in a puff of dust; and what a home's
    /// heart feeds lit or dark as its chip stands or is taken (0096), placed again.
    /// </summary>
    private void Follow()
    {
        bool first = _count < 0;
        _count = _building.Plants.Count;
        Hearts();
        foreach (var s in _shown.Where(s => MoodOf(s.Plant, s.Art) != (s.Lit, s.Fed)).ToList())
        {
            _shown.Remove(s);
            _byCell.Remove(s.Plant.Cell);
        }
        var standing = new HashSet<int>();
        // From the bottom up: a table placed before the dishes standing on it.
        for (int i = _building.Plants.Count - 1; i >= 0; i--)
        {
            var plant = _building.Plants[i];
            if (!HomesArt.Of.ContainsKey(plant.Kind)) continue;
            standing.Add(plant.Cell);
            if (!_byCell.ContainsKey(plant.Cell)) Place(plant);
        }
        foreach (var gone in _shown.Where(s => !standing.Contains(s.Plant.Cell)).ToList())
        {
            _shown.Remove(gone);
            _byCell.Remove(gone.Plant.Cell);
            if (!first) Felled(gone);
        }
        // Behind first, then what stands in front, by cell: a stool before the drawings hung behind it.
        _shown.Sort((a, b) => a.Behind != b.Behind ? (a.Behind ? -1 : 1) : a.Plant.Cell.CompareTo(b.Plant.Cell));
    }

    private void Place(Plant plant)
    {
        var spec = Spec.Flora.Get(plant.Kind);
        var art = HomesArt.Of[plant.Kind];
        int w = _grid.Width, x = plant.Cell % w, y = plant.Cell / w;
        int? standsOn = null;
        if (spec.On == Spec.Flora.Anchor.Floor && _building.PlantAt(plant.Cell + w) is { } under && _byCell.TryGetValue(under.Cell, out var below))
            standsOn = (int)below.Box.Y + HomesLooks.SinksInto(under.Kind);
        var (bx, by) = HomesLooks.BoxAt(plant.Kind, spec.On, x, y, spec.Across, spec.Tall, standsOn);
        var (lit, fed) = MoodOf(plant, art);
        int? variant = plant.Kind == "chair" ? Facing(plant.Cell) : null;
        string name = HomesLooks.PictureOf(plant.Kind, plant.Cell, lit, variant, fed);
        var box = new Vector2(bx, by);
        var corner = box - new Vector2(art.OriginX, art.OriginY);
        var shown = new Shown(plant, art, name, box, corner, lit, spec.Behind, (float)(HomesLooks.Hash(plant.Cell) % 1000 / 1000.0 * Mathf.Tau)) { Fed = fed };
        if (plant.Kind == Spec.Ruins.Niche && _hearts.FirstOrDefault(h => h.Niche == plant.Cell) is { } heart)
            shown.Sparks = new RelicSparks(_random, RelicArt.Niche, box, heart.Cable.Where((p, i) => i % 3 == 0 && p.DistanceTo(box) < 6 * Cell).ToList(), heart.Floor, small: true);
        foreach (var e in art.Emitters)
            if (e.Kind == Emits.Motes)
                for (int i = 0; i < e.Count; i++)
                    shown.Motes.Add(new Mote { X = Between(e.X, e.X2), Y = Between(e.Y, e.Y2), Vx = Between(-1.5, 1.5), Vy = Between(-1, 1), Phase = Between(0, 6), Box = e });
        _shown.Add(shown);
        _byCell[plant.Cell] = shown;
    }

    /// <summary>
    /// The homes holding a heart (0096), from the sim's relics: each niche's cell, the cells of what it feeds, its home's box in
    /// cells, whether its chip is taken, and the copper cable from it to the hearth and along the ceiling to the lamp.
    /// </summary>
    private void Hearts()
    {
        int w = _grid.Width;
        foreach (var relic in _building.Relics)
        {
            if (relic.Kind != Spec.Ruins.Niche) continue;
            if (_hearts.FirstOrDefault(h => h.Niche == relic.Cell) is { } known)
            {
                known.Taken = relic.Taken;
                continue;
            }
            var home = Spec.Ruins.Homes.FirstOrDefault(h => h.Pieces.Any(p => p.Kind == Spec.Ruins.Niche));
            if (home == null) continue;
            var (_, nx, ny) = home.Pieces.First(p => p.Kind == Spec.Ruins.Niche);
            int x0 = relic.Cell % w - nx, y0 = relic.Cell / w - ny;
            var feeds = home.Pieces.Where(p => HomesLooks.Fed.Contains(p.Kind)).Select(p => (p.Kind, Cell: (y0 + p.Y) * w + x0 + p.X)).ToList();
            var heart = new Heart(relic.Cell, new Rect2I(x0, y0, home.Wide, home.Tall), [.. feeds.Select(f => f.Cell)], (y0 + home.Tall - 2) * Cell) { Taken = relic.Taken };
            heart.Cable.AddRange(CableOf(relic.Cell, y0, feeds));
            _hearts.Add(heart);
        }
    }

    /// <summary>The cable's pixels, a step apart: from the niche's side to the hearth, from its lintel up to the ceiling and along it to the lamp.</summary>
    private List<Vector2> CableOf(int niche, int homeTop, List<(string Kind, int Cell)> feeds)
    {
        int w = _grid.Width;
        Vector2 Box(string kind, int cell)
        {
            var spec = Spec.Flora.Get(kind);
            var (x, y) = HomesLooks.BoxAt(kind, spec.On, cell % w, cell / w, spec.Across, spec.Tall);
            return new Vector2(x, y);
        }
        var corner = Box(Spec.Ruins.Niche, niche);
        var side = corner + new Vector2(RelicArt.NicheSide.X, RelicArt.NicheSide.Y);
        var top = corner + new Vector2(RelicArt.NicheTop.X, RelicArt.NicheTop.Y);
        float ceiling = (homeTop + 2) * Cell + 1;
        var cable = new List<Vector2>();
        void Run(Vector2 a, Vector2 b)
        {
            int steps = (int)Math.Max(Math.Abs(b.X - a.X), Math.Abs(b.Y - a.Y));
            for (int i = 0; i <= steps; i++) cable.Add((a + (b - a) * (steps == 0 ? 0 : (float)i / steps)).Floor());
        }
        foreach (var (kind, cell) in feeds)
        {
            var box = Box(kind, cell);
            if (kind == "hearth") Run(side, new Vector2(box.X + 4, side.Y));
            else
            {
                Run(top, new Vector2(top.X, ceiling));
                Run(new Vector2(top.X, ceiling), new Vector2(box.X + HomesArt.Of[kind].BoxW / 2, ceiling));
            }
        }
        return cable;
    }

    /// <summary>
    /// Whether <paramref name="plant"/> burns and whether a heart feeds it: what a heart feeds burns while its chip stands, every other
    /// lamp in its home that may go out is dead (0096); elsewhere the mood by the world's seed, or the one asked for.
    /// </summary>
    private (bool Lit, bool Fed) MoodOf(Plant plant, HomesArt.Picture art)
    {
        int w = _grid.Width;
        var heart = _hearts.FirstOrDefault(h => h.Home.HasPoint(new Vector2I(plant.Cell % w, plant.Cell / w)));
        return HomesLooks.MoodOf(plant.Kind, plant.Cell, heart?.Feeds.Contains(plant.Cell) == true, heart != null, heart?.Taken == true, Mood);
    }

    /// <summary>A chair faces its table or desk, the nearest within six columns in its row; else either way, by its cell.</summary>
    private int? Facing(int cell)
    {
        for (int d = 1; d <= 6; d++)
        {
            if (_building.PlantAt(cell + d)?.Kind is "table" or "desk") return 0;
            if (_building.PlantAt(cell - d)?.Kind is "table" or "desk") return 1;
        }
        return null;
    }

    /// <summary>A hit that deepens a piece's crack: a short shake; the music box plays. What it knocks off is thrown where it struck (<c>HitBurst</c>, 0094).</summary>
    private void Hits(double delta)
    {
        foreach (var crack in _building.Cracks)
        {
            if (!_byCell.TryGetValue(crack.Cell, out var s)) continue;
            if (_cracks.TryGetValue(crack.Cell, out int was) && was >= crack.Damage) continue;
            _cracks[crack.Cell] = crack.Damage;
            s.Shake = 0.18;
            if (s.Plant.Kind == "musicBox") Tune(s.Bounds.GetCenter());
        }
        foreach (var s in _inView) s.Shake = Math.Max(0, s.Shake - delta);
    }

    /// <summary>A felled piece's dust, a glowing one's sparks; the music box's last tune.</summary>
    private void Felled(Shown s)
    {
        _cracks.Remove(s.Plant.Cell);
        var middle = s.Bounds.GetCenter();
        var glims = Glims.Shared;
        glims.Spray(Luminous.BreakDust, middle.X, middle.Y, (int)Math.Round(8 * Fx.Count), -Math.PI / 2, (-Math.PI, Math.PI), (6, 22));
        glims.Spray(Chip, middle.X, middle.Y, (int)Math.Round(6 * Fx.Count), -Math.PI / 2, (-1.4, 1.4), (20, 60), s.Box.Y + s.Art.BoxH);
        if (s.Lit && s.Art.Lights.Length > 0) glims.Spray(Luminous.ArcSpark, middle.X, middle.Y, 6, -Math.PI / 2, (-Math.PI, Math.PI), (30, 90));
        if (s.Plant.Kind == "musicBox") Tune(middle);
        if (GroundSounds.FallOf(s.Plant.Kind) is { } fall) _falls.Play(fall, middle);
    }

    /// <summary>Chips knocked off a piece: dust and splinters, falling.</summary>
    private static readonly Luminous Chip = Luminous.Chip with { Ramp = [(150, 118, 92), (96, 74, 58)], Gravity = 380, Bounce = 0.2, Size = 1.3, Low = true };

    /// <summary>The music box's tune from <paramref name="at"/>: its notes queued, a tine each.</summary>
    private void Tune(Vector2 at)
    {
        if (_tune.Count > 0) return;
        for (int i = 0; i < HomesLooks.Tune.Length; i++)
            _tune.Add((_seconds + i * HomesLooks.TuneNoteSeconds, Mathf.Pow(2, HomesLooks.Tune[i] / 12f), at));
    }

    private void Play()
    {
        while (_tune.Count > 0 && _tune[0].At <= _seconds)
        {
            var (_, pitch, at) = _tune[0];
            _tune.RemoveAt(0);
            _tine.PlayAt(at, pitch, 0);
        }
    }

    private double Between(double from, double to) => from + (to - from) * _random.NextDouble();

    /// <summary>One page tick of a piece's emitters (the page's live layer, its numbers): arcs rolled, pulses eased, bursts.</summary>
    private void Tick(Shown s)
    {
        if (!s.Lit) return;
        s.Ticks++;
        var art = s.Art;
        if (s.Sparks is { } sparks)
        {
            if (sparks.Step(_seconds))
            {
                Flares.Shared.Flash(sparks.Core.X, sparks.Core.Y, RelicLooks.Hot, RelicLooks.FlashStrength * RelicLooks.SmallFlash, RelicLooks.FlashReach, RelicLooks.LightKeep, RelicLooks.FlashDecay);
                _zap.PlayAt(sparks.Core, 0.9f + (float)Between(0, 0.2), -4);
            }
            // Arcs crawl along its cable, leading the eye from the lamp to the niche.
            if (_hearts.FirstOrDefault(h => h.Niche == s.Plant.Cell) is { Cable.Count: > 4 } heart && _random.NextDouble() < 0.35)
            {
                int j = _random.Next(heart.Cable.Count - 4);
                var (a, b) = (heart.Cable[j] - new Vector2(0, 1), heart.Cable[Math.Min(heart.Cable.Count - 1, j + 2 + _random.Next(10))] - new Vector2(0, 1));
                Bolts.Shared.Add(() => (a.X, a.Y, b.X, b.Y, 0), (int)Math.Round(Between(2, 4) * 2.5), 0.6, 0.45, tint: BoltTint.Violet);
            }
        }
        var tint = s.Fed ? BoltTint.Violet : null;
        for (int i = 0; i < art.Emitters.Length; i++)
        {
            var e = art.Emitters[i];
            ref var st = ref s.State[i];
            switch (e.Kind)
            {
                case Emits.Arc:
                    {
                        if (_random.NextDouble() >= Crackle.Chance(e, s.Ticks)) break;
                        var a = s.Box + new Vector2(e.X + (float)Between(-0.6, 0.6), e.Y + (float)Between(-0.6, 0.6));
                        Vector2 b;
                        if (e.ToAir)
                        {
                            double angle = Between(-Math.PI * 0.95, -Math.PI * 0.05), reach = e.Reach * Between(0.6, 1);
                            b = new Vector2(e.X + (float)(Math.Cos(angle) * reach), e.Y + (float)(Math.Sin(angle) * reach)) + s.Box;
                        }
                        else b = s.Box + new Vector2(e.X2 + (float)Between(-0.8, 0.8), e.Y2 + (float)Between(-0.8, 0.8));
                        Crackle.Strike(_random, e, a, b, tint);
                        break;
                    }
                case Emits.Pulse:
                    if (s.Ticks % (e.Slow ? 1 : 3) == 0)
                        st.Target = e.Slow ? 1 + e.Amp * Math.Sin(s.Ticks * 0.06) : 1 + e.Amp * Between(-1, 1) * (_random.NextDouble() < 0.08 ? 2.5 : 1);
                    st.Value += ((st.Target == 0 ? 1 : st.Target) - st.Value) * 0.35;
                    break;
                case Emits.Sputter:
                    if (st.Burst > 0)
                    {
                        st.Burst--;
                        st.On = _random.NextDouble() < 0.55;
                        if (st.On && _random.NextDouble() < 0.7)
                        {
                            var a = s.Box + new Vector2(e.From.X, e.From.Y);
                            var b = s.Box + new Vector2(e.X2 + (float)Between(-1, 1), e.Y2 + (float)Between(0, 2));
                            Bolts.Shared.Add(() => (a.X, a.Y, b.X, b.Y, 0), 5, 0.8, 0.5);
                        }
                    }
                    else
                    {
                        st.On = false;
                        if (_random.NextDouble() < 0.012) st.Burst = (int)Math.Round(Between(4, 14));
                    }
                    break;
                case Emits.Twinkle:
                    if (_random.NextDouble() < 0.22 && e.Points.Length > 0) s.Glints.Add((e.Points[_random.Next(e.Points.Length)], 0));
                    break;
                case Emits.Spore:
                    if (_random.NextDouble() < e.Count * 0.012)
                        Glims.Shared.Add(SporeOf(e.Colour), s.Box.X + e.X + Between(-10, 10), s.Box.Y + e.Y + Between(-3, 3), Between(-1.5, 1.5), -Between(4, 9), bright: Between(0.5, 0.9));
                    break;
                case Emits.Embers:
                    if (_random.NextDouble() < 0.22)
                        Glims.Shared.Add(Ember, s.Box.X + e.X + Between(-e.Width / 2, e.Width / 2), s.Box.Y + e.Y, Between(-5, 5), -Between(14, 26), bright: Between(0.5, 0.9));
                    break;
            }
        }
        for (int i = s.Glints.Count - 1; i >= 0; i--)
        {
            var (at, age) = s.Glints[i];
            if (age + 1 >= 8) s.Glints.RemoveAt(i);
            else s.Glints[i] = (at, age + 1);
        }
        foreach (var m in s.Motes)
        {
            var b = m.Box!;
            m.Phase += 0.05;
            m.X += m.Vx * TickSeconds + Math.Sin(m.Phase) * 0.05;
            m.Y += m.Vy * TickSeconds;
            if (m.X < b.X) m.X = b.X2;
            if (m.X > b.X2) m.X = b.X;
            if (m.Y < b.Y) m.Y = b.Y2;
            if (m.Y > b.Y2) m.Y = b.Y;
        }
    }

    /// <summary>The soft halo a lamp's pulse throws when it swells, for this frame.</summary>
    private void Glow(Shown s)
    {
        if (!s.Lit) return;
        for (int i = 0; i < s.Art.Emitters.Length; i++)
        {
            var e = s.Art.Emitters[i];
            var at = s.Box + new Vector2(e.X, e.Y);
            if (e.Kind == Emits.Pulse && s.State[i].Value - 1 is var d and > 0)
                Flares.Shared.Bloom(at.X, at.Y, e.Radius, s.Fed && e.Colour.B > e.Colour.R ? RelicLooks.Glow : e.Colour, Math.Min(0.6, d * 2.2) * Fx.Glow);
            else if (e.Kind == Emits.Sputter && s.State[i].On)
                Flares.Shared.Bloom(at.X, at.Y, e.Radius * 0.9, (120, 200, 255), 0.45 * Fx.Glow);
        }
    }

    /// <summary>How bright piece <paramref name="s"/> is now: its pulse's swell, or a sputtering lamp's flash; 0 dark.</summary>
    private static float BrightOf(Shown s)
    {
        if (!s.Lit) return 0;
        float bright = 1;
        for (int i = 0; i < s.Art.Emitters.Length; i++)
        {
            var e = s.Art.Emitters[i];
            if (e.Kind == Emits.Pulse) bright *= (float)(s.State[i].Value == 0 ? 1 : s.State[i].Value);
            else if (e.Kind == Emits.Sputter) bright *= s.State[i].On ? 1 : 0;
        }
        return bright;
    }

    /// <summary>The light of the lamps near the view, the nearest its middle first, at most <see cref="HomesLooks.MostLights"/>.</summary>
    private void Light(Rect2 view)
    {
        _lights.Clear();
        _near.Clear();
        var middle = view.GetCenter();
        foreach (var s in _inView)
        {
            float bright = BrightOf(s);
            if (bright <= 0) continue;
            foreach (var l in s.Art.Lights)
            {
                bool arc = HomesLooks.IsArc(l.Colour);
                var at = s.Box + new Vector2(l.X, l.Y);
                int reach = (int)Math.Ceiling(l.Reach * HomesLooks.PageCells * (arc ? HomesLooks.ArcReach : 1));
                if (!view.Grow(reach * Cell).HasPoint(at)) continue;
                // Fed by a relic's chip, the makers' arc burns violet-white as the relic does (0096).
                var colour = arc && s.Fed ? RelicLooks.Light : l.Colour;
                var light = new LightSource(at.X, at.Y, colour, l.Strength * (arc ? HomesLooks.ArcStrength : 1) * bright, reach, HomesLooks.Keep, 1, HomesLooks.LightSize);
                _near.Add((light, at.DistanceSquaredTo(middle)));
            }
        }
        if (_near.Count > HomesLooks.MostLights) _near.Sort((a, b) => a.Far.CompareTo(b.Far));
        for (int i = 0; i < Math.Min(_near.Count, HomesLooks.MostLights); i++) _lights.Add(_near[i].Light);
    }

    /// <summary>A hero the sim's resonator zaps (it asks, the rule is the sim's): an arc from its sphere to the hero, crackling.</summary>
    private void Zaps(double delta)
    {
        _zapHeard -= delta;
        var heroes = _heroes();
        if (heroes.Count == 0) return;
        int w = _grid.Width;
        foreach (var s in _inView)
        {
            if (s.Plant.Kind != Spec.Ruins.Resonator) continue;
            var sphere = s.Box + new Vector2(s.Art.BoxW / 2f, 6);
            foreach (var hero in heroes)
            {
                int cell = (int)(hero.Y / Cell) * w + (int)(hero.X / Cell);
                if (hero.X < 0 || hero.Y < 0 || !_building.Zaps(cell) || hero.DistanceTo(sphere) > 8 * Cell) continue;
                if (_random.NextDouble() < delta / 0.12)
                {
                    var to = hero + new Vector2((float)Between(-4, 4), (float)Between(-8, 6));
                    Bolts.Shared.Add(() => (sphere.X, sphere.Y, to.X, to.Y, Between(-0.25, 0.25)), _random.Next(4, 9), 1, 1, branchy: true);
                }
                if (_zapHeard <= 0)
                {
                    _zap.PlayAt(hero, 1 + (float)Between(-0.08, 0.08), 0);
                    _zapHeard = Between(0.35, 0.7);
                }
            }
        }
    }

    /// <summary>The pictures of the pieces in view, their glows, and what the page draws live: dots, scanlines, glints, motes.</summary>
    private void Paint(Painter on, bool glow)
    {
        if (!glow) Cables(on);
        foreach (var s in _inView)
        {
            var region = Atlas.At[s.Name];
            var corner = s.Corner + new Vector2(s.Shake > 0 ? (float)Math.Round(Math.Sin(s.Shake * 90)) : 0, 0);
            if (!glow)
            {
                DrawSwaying(on, Atlas.Pictures, region, corner, s, Colors.White);
                continue;
            }
            float bright = BrightOf(s);
            if (bright > 0 && Atlas.Glows.ContainsKey(s.Name))
                DrawSwaying(on, Atlas.Shine, Atlas.Glows[s.Name], corner, s, new Color(1, 1, 1, Math.Clamp(bright, 0.5f, 1f)));
            if (!s.Lit) continue;
            Live(on, s);
        }
    }

    /// <summary>A heart's copper cable (0096), lit as the world is: bright copper while its chip stands, blackened once it is taken.</summary>
    private void Cables(Painter on)
    {
        foreach (var h in _hearts)
        {
            var box = new Rect2(h.Home.Position * Cell, h.Home.Size * Cell);
            if (!_view.Intersects(box)) continue;
            var (lit, under) = h.Taken ? (new Color(0.24f, 0.17f, 0.13f), new Color(0.1f, 0.07f, 0.06f)) : (new Color(0.73f, 0.47f, 0.23f), new Color(0.18f, 0.09f, 0.03f));
            foreach (var p in h.Cable)
            {
                on.DrawRect(new Rect2(p, Vector2.One), lit);
                on.DrawRect(new Rect2(p + new Vector2(0, 1), Vector2.One), under);
            }
        }
    }

    /// <summary>A picture drawn whole, or by bands of rows each its own whole pixels off where its cloth sways from its pivot.</summary>
    private void DrawSwaying(Painter on, Texture2D texture, Rect2 region, Vector2 corner, Shown s, Color modulate)
    {
        var sway = s.Art.Emitters.FirstOrDefault(e => e.Kind == Emits.Sway);
        if (sway == null)
        {
            on.DrawTextureRectRegion(texture, new Rect2(corner, region.Size), region, modulate);
            return;
        }
        int h = (int)region.Size.Y, pivot = (int)(sway.Y + s.Art.OriginY), rows = h - pivot;
        int Shift(int row) => HomesLooks.SwayAt(row - pivot, rows, _seconds, s.Phase);
        int from = 0, shift = Shift(0);
        for (int row = 1; row <= h; row++)
        {
            int now = row < h ? Shift(row) : int.MinValue;
            if (now == shift) continue;
            on.DrawTextureRectRegion(texture, new Rect2(corner + new Vector2(shift, from), new Vector2(region.Size.X, row - from)),
                new Rect2(region.Position + new Vector2(0, from), new Vector2(region.Size.X, row - from)), modulate);
            (from, shift) = (row, now);
        }
    }

    /// <summary>The page's live drawing for a lit piece, added unlit: the mobile's stars, the scanline, the last pixel, glints, motes.</summary>
    private void Live(Painter on, Shown s)
    {
        for (int i = 0; i < s.Art.Emitters.Length; i++)
        {
            var e = s.Art.Emitters[i];
            var at = s.Box + new Vector2(e.X, e.Y);
            switch (e.Kind)
            {
                case Emits.Mobile:
                    for (int q = 0; q < 4; q++)
                    {
                        double angle = s.Ticks * 0.045 + q * Math.PI / 2;
                        var star = at + new Vector2((float)(Math.Cos(angle) * e.Radius), (float)(Math.Sin(angle) * e.Radius2));
                        on.DrawCircle(star, 1.8f, new Color(0.55f, 0.82f, 1f, 0.35f));
                        on.DrawRect(new Rect2(star.Floor(), Vector2.One), new Color(0.94f, 0.99f, 1f, 0.9f));
                    }
                    break;
                case Emits.Holo:
                    {
                        float band = e.Y + (float)(s.Ticks * 0.5 % e.Height);
                        on.DrawRect(new Rect2(s.Box.X + e.X, s.Box.Y + band, e.Width, 0.6f), new Color(98 / 255f, 228 / 255f, 244 / 255f, 0.38f));
                        if (s.Ticks % 70 < 2) on.DrawRect(new Rect2(at, new Vector2(e.Width, e.Height)), new Color(98 / 255f, 228 / 255f, 244 / 255f, 0.12f));
                        break;
                    }
                case Emits.Pixel:
                    {
                        bool lit = s.Ticks % 60 < 44;
                        on.DrawCircle(at + new Vector2(0.5f, 0.5f), 2.2f, new Color(0.55f, 0.82f, 1f, lit ? 0.35f : 0.05f));
                        if (lit) on.DrawRect(new Rect2(at, Vector2.One), new Color(0.8f, 0.95f, 1f, 0.9f));
                        break;
                    }
                case Emits.Motes:
                    foreach (var m in s.Motes.Where(m => m.Box == e))
                        on.DrawRect(new Rect2(new Vector2((float)Math.Round(s.Box.X + m.X), (float)Math.Round(s.Box.Y + m.Y)), Vector2.One),
                            new Color(e.Colour.R / 255f, e.Colour.G / 255f, e.Colour.B / 255f, (float)(0.25 + 0.2 * Math.Sin(m.Phase * 2))));
                    break;
            }
        }
        foreach (var ((x, y), age) in s.Glints)
        {
            float f = Mathf.Sin(Mathf.Pi * age / 8f), arm = 2.4f * f;
            var c = new Color(0.75f, 0.9f, 1f, f);
            var p = s.Box + new Vector2(x, y);
            on.DrawLine(p - new Vector2(arm, 0), p + new Vector2(arm, 0), c, 0.6f);
            on.DrawLine(p - new Vector2(0, arm), p + new Vector2(0, arm), c, 0.6f);
        }
    }

    /// <summary>A spore of the page's colour, made once a colour.</summary>
    private static Luminous SporeOf((int R, int G, int B) colour)
    {
        if (!Spores.TryGetValue(colour, out var look)) Spores[colour] = look = CaveLooks.Spore with { Ramp = [(236, 255, 246), colour] };
        return look;
    }

    private static readonly Dictionary<(int, int, int), Luminous> Spores = new();

    /// <summary>The hearth's embers: the furnace's, flying up from its coil and fading.</summary>
    private static readonly Luminous Ember = Luminous.Ember with { LifeMs = (600, 1400), Gravity = -4, Drag = 0.6, Light = false };

    /// <summary>A glow's: added over what lies under it, unlit, for it gives light.</summary>
    private static readonly CanvasItemMaterial GlowMaterial = new()
    {
        BlendMode = CanvasItemMaterial.BlendModeEnum.Add,
        LightMode = CanvasItemMaterial.LightModeEnum.Unshaded,
    };

    /// <summary>A piece shown: where its box and picture lie, lit or dark, and its emitters' state.</summary>
    private sealed class Shown(Plant plant, HomesArt.Picture art, string name, Vector2 box, Vector2 corner, bool lit, bool behind, float phase)
    {
        public Plant Plant { get; } = plant;
        public HomesArt.Picture Art { get; } = art;
        public string Name { get; } = name;
        public Vector2 Box { get; } = box;
        public Vector2 Corner { get; } = corner;
        public bool Lit { get; } = lit;
        public bool Behind { get; } = behind;
        public float Phase { get; } = phase;
        public Rect2 Bounds { get; } = new(corner, art.Width, art.Height);
        public State[] State { get; } = new State[art.Emitters.Length];
        public readonly List<((float X, float Y) At, int Age)> Glints = [];
        public readonly List<Mote> Motes = [];
        public int Ticks;
        public double Shake;
        /// <summary>Fed by a home's heart (0096): its arcs and light the relic's violet-white.</summary>
        public bool Fed { get; init; }
        /// <summary>A relic's chip sparking in its niche (0096).</summary>
        public RelicSparks? Sparks { get; set; }
    }

    /// <summary>A home holding a heart (0096): its niche's cell, its box in cells, what it feeds, its floor's row of pixels, its cable.</summary>
    private sealed class Heart(int niche, Rect2I home, List<int> feeds, float floor)
    {
        public int Niche { get; } = niche;
        public Rect2I Home { get; } = home;
        public List<int> Feeds { get; } = feeds;
        public float Floor { get; } = floor;
        public List<Vector2> Cable { get; } = [];
        public bool Taken { get; set; }
    }

    private struct State
    {
        public double Value, Target;
        public int Burst;
        public bool On;
    }

    private sealed class Mote
    {
        public double X, Y, Vx, Vy, Phase;
        public Emitter? Box;
    }

    /// <summary>One canvas item drawing the pieces in view: their pictures lit, or their glows and the live layer added.</summary>
    private sealed partial class Painter(HomesView view, bool glow) : Node2D
    {
        public override void _Draw() => view.Paint(this, glow);
    }

    /// <summary>
    /// Every homes picture packed into one texture, a row of the tallest first, a pixel apart, and each one's glow into a second at the
    /// same place: the pieces are one batch for their pictures and one for their glows.
    /// </summary>
    private static class Atlas
    {
        private const int Across = 512;
        public static readonly Texture2D Pictures, Shine;
        public static readonly Dictionary<string, Rect2> At = new();
        public static readonly Dictionary<string, Rect2> Glows = new();

        static Atlas()
        {
            var names = Names().Select(n => (Name: n, Image: Rgba(PixelArt.Picture(n).GetImage()))).OrderByDescending(p => p.Image.GetHeight()).ThenBy(p => p.Name, StringComparer.Ordinal).ToList();
            int x = 0, y = 0, row = 0;
            foreach (var (name, image) in names)
            {
                if (x + image.GetWidth() > Across) (x, y, row) = (0, y + row + 1, 0);
                At[name] = new Rect2(x, y, image.GetWidth(), image.GetHeight());
                (x, row) = (x + image.GetWidth() + 1, Math.Max(row, image.GetHeight()));
            }
            var pictures = Image.CreateEmpty(Across, y + row + 1, false, Image.Format.Rgba8);
            var glows = Image.CreateEmpty(Across, y + row + 1, false, Image.Format.Rgba8);
            foreach (var (name, image) in names)
            {
                var at = At[name];
                var whole = new Rect2I(0, 0, (int)at.Size.X, (int)at.Size.Y);
                pictures.BlitRect(image, whole, (Vector2I)at.Position);
                if (!ResourceLoader.Exists($"res://art/{name}_glow.png")) continue;
                glows.BlitRect(Rgba(PixelArt.Picture($"{name}_glow").GetImage()), whole, (Vector2I)at.Position);
                Glows[name] = at;
            }
            (Pictures, Shine) = (ImageTexture.CreateFromImage(pictures), ImageTexture.CreateFromImage(glows));
        }

        private static IEnumerable<string> Names()
        {
            foreach (var (kind, art) in HomesArt.Of)
                for (int v = 0; v < art.Variants; v++)
                {
                    yield return $"homes/{kind}_{v}";
                    if (art.Dark) yield return $"homes/{kind}_{v}_dark";
                    if (HomesLooks.FedLooks.Contains(kind)) yield return $"homes/{kind}_{v}_fed";
                }
        }

        private static Image Rgba(Image image)
        {
            if (image.IsCompressed()) image.Decompress();
            if (image.GetFormat() != Image.Format.Rgba8) image.Convert(Image.Format.Rgba8);
            return image;
        }
    }
}
