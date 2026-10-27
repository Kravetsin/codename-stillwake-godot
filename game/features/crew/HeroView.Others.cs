// HeroView's company: the other heroes drawn as sent, plumes and canisters breathed, harpoon rods and lines, and tethers.
using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Features.Sound;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim;
using Stillwake.Sim.Debris;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Crew;

public partial class HeroView
{
    /// <summary>
    /// Every other hero heard from lately, in its suit, posed by how it moves and whether it is out, its
    /// hands wielding and working as sent, the axe swung once for each swing sent (<see cref="AxeSwing.Follow"/>);
    /// those gone, hidden.
    /// </summary>
    private void ShowOthers(Ship ship, Registry m, double elapsedMs)
    {
        var crew = m.Crew;
        // Only the heroes on this hero's surface: the others stand on another grid (0076).
        int here = _client!.World.SurfaceOf(_client.Id);
        var others = _remote.At(_clockMs).Where(o => _client.World.SurfaceOf(o.Id) == here).ToList();
        _chargingNow.Clear();
        foreach (var c in m.Creatures.Creatures)
            if (c.Posture == Stillwake.Sim.Creatures.Posture.Charge) _chargingNow.Add(c.Id);
        foreach (var (id, shown) in _shownOthers)
            if (!others.Any(o => o.Id == id))
            {
                shown.Figure.Visible = false;
                (shown.Tether, shown.Busy) = (null, null);
                shown.Swing.Stop();
                ShowRod(id, shown, null, false, elapsedMs);
            }
        foreach (var (id, seen) in others)
        {
            if (!_shownOthers.TryGetValue(id, out var shown))
            {
                var drum = Cues.Player2D(S.Drum, S.World);
                AddChild(drum);
                shown = _shownOthers[id] = new Other(new Figure(Suits.Of(m.Players, id), own: false), NewRod(), drum);
                _others.AddChild(shown.Figure);
            }
            var hero = seen.Hero;
            bool out_ = (crew.HeroOf(id)?.Out ?? 0) > 0;
            shown.Figure.Visible = m.Landing.SeatOf(id) == null && m.Landing.FlightOf(id) == null;
            shown.Figure.Wear(Suits.Of(m.Players, id));
            var (axe, chop) = _heardChops.GetValueOrDefault(id);
            if (!axe) shown.Swing.Stop();
            // Its blade heard and seen where it strikes the ground as it is played here, the fan its own peer sends then (0093, 0094),
            // in the front layer: which button its peer swings with is not sent.
            if (shown.Swing.Follow(elapsedMs, axe ? chop : null).Hits)
                StruckBy(m, id, hero, shown.Swing.At, Content.Weapons.Axe.Item);
            // Its drill's hits likewise, at its pace where the sim says it runs and it sends its bit: the fan its peer sends there.
            string? running = m.Items.Running(id);
            bool drills = HeldLook.IsDrill(running) && seen.Busy is { Work: Handwork.Drill };
            if (shown.Drill.Due(_client.World.Tick, drills, Content.Weapons.Of(running)?.Pace ?? 1))
                StruckBy(m, id, hero, seen.Busy!.Value.At, running!);
            ShowHands(shown.Figure, seen.Wields, FittedOf(m, id, seen.Wields), seen.Busy, shown.Swing.Phase is double phase ? (shown.Swing.At, phase) : null);
            // A friend's drill running points where it drills, its harpoon where its rod flies or sticks, else
            // either hangs; the drill spins while the sim says it runs.
            var rodAim = shown.Rod.AimAt(seen.Rod, PieceDrawnAt);
            shown.Figure.Aim = seen.Busy is { Work: Handwork.Drill, At: var drilled } ? Pixels(drilled) : rodAim is { } aimed ? Pixels(aimed) : null;
            shown.Figure.Spinning = HeldLook.IsDrill(m.Items.Running(id));
            if (Busy.BeamsAnew(shown.Busy, seen.Busy)) shown.Figure.Beam([Pixels(seen.Busy!.Value.At)]);
            shown.Figure.Loaded = shown.Rod.Loaded;
            shown.Figure.Jetpack = m.Items.Equipped(id, Content.Jetpack.Item);
            shown.Figure.HeadLamp = m.Items.Equipped(id, Content.HelmetLamp.Item);
            shown.Figure.FloorRoom = FloorRoom(m.Grid, hero);
            shown.Figure.Footing = FootingOf(m, hero, shown.Figure.Footing);
            shown.Figure.Ledge = LedgeOf(ship, hero);
            bool afloat = ship.Space(hero.BodyCell(ship.Width));
            // A charge ended by it: it was rammed, its own peer says so; here it flinches, its air knocked out, heard where it is.
            if (!out_ && RamEndedBy(m, hero) is (int rammedFrom, string rammer))
            {
                shown.Figure.Flinch(rammedFrom);
                var (cx, cy) = Chest(hero);
                _air.Leak(new Vector2(cx, cy), rammedFrom);
                _othersThud.Play(S.RamHits.GetValueOrDefault(rammer, S.RamThud), new Vector2(cx, cy));
            }
            shown.Figure.Show(hero, Poses.PoseOf(hero, afloat || ship.Glides(hero.X), out_), out_ ? 0 : seen.Look ?? 0,
                seen.HoldsPlan, elapsedMs);
            for (int burnt = shown.Burns.Due(elapsedMs, Burns(m, crew.HeroOf(id)), 1 / S.LavaBurnEveryS); burnt > 0; burnt--)
                _othersBurn.PlayAt(shown.Figure.GlobalPosition);
            // A friend's canister: its air rises round it, heard where it is (0081).
            if (crew.HeroOf(id) is { } breathing && _canisters.Breathed(id, breathing.Air, breathing.Out))
            {
                var figure = shown.Figure;
                _air.Vortex(() => figure.Stance);
                _othersBreathe.PlayAt(figure.Visor);
            }
            // Its thrust is not sent: the plume blows against the way it moves, down while it is still.
            Plume(id, shown.Figure, crew.HeroOf(id)?.Thrusting == true ? (hero.Vx, hero.Vy) : null, elapsedMs);
            ShowRod(id, shown, seen.Rod, afloat, elapsedMs);
            (shown.Tether, shown.Busy) = (Tie(shown.Tether, id, hero.Anchor, Tether.ClipOf(hero.X, hero.Y), elapsedMs), seen.Busy);
        }
        _canisters.Sweep();
        Others = others.Select(o => (o.Id, o.Seen.Hero)).ToList();
        (_charging, _chargingNow) = (_chargingNow, _charging);
    }

    /// <summary>
    /// A jetpack's plume (0047, 0075): while a hero thrusts toward <paramref name="toward"/>, its <see cref="JetPlume"/> from
    /// its figure's nozzle, blown the other way, down with no way; stopped, it dies back.
    /// </summary>
    private void Plume(int peer, Figure figure, (double X, double Y)? toward, double elapsedMs)
    {
        if (!_plumes.TryGetValue(peer, out var plume))
        {
            if (toward == null) return;
            AddChild(_plumes[peer] = plume = new JetPlume());
        }
        plume.Step(figure.Nozzle, toward, elapsedMs);
    }

    /// <summary>
    /// Breathes a canister into the suit (0047): sent, the hiss heard, when the sim would take it now and
    /// none is in flight; a full suit or no canister does nothing.
    /// </summary>
    private void Breathe(Client client, Registry m)
    {
        if (Report.RefillOf(m.Crew.RefillProblem(client.Id), client.InFlight) is not { } refill) return;
        client.Send(refill);
        _breathes.Play();
        _ui.Click();
        _air.Vortex(() => _figure.Stance);
        _ui.Refill();
    }

    /// <summary>This player's hero breathes room air again: a few pale motes off its visor (0081).</summary>
    public void BreathesAgain() => _air.Puff(_figure.Visor);

    /// <summary>
    /// Another hero's harpoon as it sends it (<see cref="RemoteRod"/>, 0065), from its gun's muzzle as just drawn:
    /// its rod and line drawn as this hero's; heard where it is, on World, as its steps are: the shot and its puff
    /// as the rod leaves, the bite where it sticks (the chop, or the glove's thump on a crate or pod), the drum
    /// at the gun while it reels or is pulled.
    /// </summary>
    private void ShowRod(int peer, Other shown, RodSeen? sent, bool afloat, double elapsedMs)
    {
        var figure = shown.Figure;
        var rod = shown.Rod;
        var cues = rod.Step(elapsedMs, sent, figure.Muzzle is { } muzzle ? Thousandths(muzzle) : null, PieceDrawnAt);
        if (cues.Fired && figure.Muzzle is { } gun)
        {
            _othersShot.PlayAt(gun);
            Puff(figure);
        }
        if (cues.Bit)
        {
            bool thumps = rod.Piece is int id && PieceOf(id) is { } piece && Thumps(piece);
            if (thumps) _othersThump.PlayAt(Pixels(rod.Tip));
            else _othersBite.PlayAt(Pixels(rod.Tip));
            RodLight.Bit(Pixels(rod.Tip), Unit(rod.Heading), !thumps);
        }
        bool drums = rod.Drums && figure.Muzzle != null;
        if (drums) shown.Drum.GlobalPosition = figure.Muzzle!.Value;
        if (drums != shown.Drum.Playing)
        {
            if (drums) shown.Drum.Play();
            else shown.Drum.Stop();
        }
        DrawRod(rod.Rod, rod.Tip, rod.Heading, figure.Muzzle, ref shown.Line, shown.RodPicture, shown.Light, peer, elapsedMs);
    }

    /// <summary>A point in thousandths of a cell, in the world's pixels.</summary>
    private static Vector2 Pixels((double X, double Y) at) => new((float)(at.X / Hero.Unit * Rig.Cell), (float)(at.Y / Hero.Unit * Rig.Cell));

    /// <summary>A point in the world's pixels, in thousandths of a cell.</summary>
    private static (double X, double Y) Thousandths(Vector2 at) => (at.X / Rig.Cell * Hero.Unit, at.Y / Rig.Cell * Hero.Unit);

    /// <summary>The rod's eye this far behind its point, in thousandths of a cell, as its picture is drawn.</summary>
    private static readonly double RodLength = (RodLook.Tip.X - RodLook.Eye.X) / Rig.Cell * Hero.Unit;

    /// <summary>This hero's rod and line as the harpoon has them this frame, from the muzzle the figure was just drawn with.</summary>
    private void ShowHarpoon(double elapsedMs) =>
        DrawRod(_harpoon.Rod, _harpoon.Tip, _harpoon.Heading, _figure.Muzzle, ref _line, _rod, _rodLight, 0, elapsedMs);

    /// <summary>
    /// A rod and its line where <paramref name="rod"/> says, its point at <paramref name="tip"/>, from a gun's
    /// <paramref name="muzzle"/> as just drawn: the rod pointing the way it flew, <paramref name="heading"/>, or, reeled
    /// or winding back, away from the gun along the line; the line slack in flight and hooked, taut reeling, pulling,
    /// hanging and winding back. Nothing while the rod is in the gun. This hero's and the others' alike.
    /// </summary>
    private static void DrawRod(Rod rod, (double X, double Y) tip, (double X, double Y) heading, Vector2? muzzle,
        ref HarpoonLine? line, Sprite2D picture, RodLight light, int seed, double elapsedMs)
    {
        if (rod == Rod.Seated || muzzle is not { } at)
        {
            (line, picture.Visible) = (null, false);
            light.Step(rod, default, default, default, null, elapsedMs);
            return;
        }
        var gun = Thousandths(at);
        var pointing = heading;
        double far = Math.Sqrt((tip.X - gun.X) * (tip.X - gun.X) + (tip.Y - gun.Y) * (tip.Y - gun.Y));
        // Stuck in the structure it keeps the way it flew in; riding a piece or winding back, it points along the line.
        if (rod is Rod.Reeling or Rod.Returning && far > 1) pointing = ((tip.X - gun.X) / far, (tip.Y - gun.Y) / far);
        var eye = (tip.X - pointing.X * RodLength, tip.Y - pointing.Y * RodLength);
        line ??= new HarpoonLine(gun, eye, seed);
        line.Update(gun, eye, HarpoonShot.Taut(rod), elapsedMs);
        picture.Visible = true;
        picture.Position = Pixels(eye);
        picture.Rotation = (float)Math.Atan2(pointing.Y, pointing.X);
        light.Step(rod, Pixels(tip), Unit(pointing), Pixels(eye), at, elapsedMs);
    }

    /// <summary>A way, any length, as a unit vector of the world; none for none.</summary>
    private static Vector2 Unit((double X, double Y) way) => new Vector2((float)way.X, (float)way.Y).Normalized();

    /// <summary>
    /// The rod biting into piece <paramref name="id"/> as it sticks (0042): metal, a wreck or salvage, heard as an
    /// axe's chop where it struck; a crate or a pod as the glove's thump.
    /// </summary>
    private void Bite(int id)
    {
        if (PieceOf(id) is not { } piece) return;
        if (Thumps(piece)) _act.Play(Floats);
        else _bite.PlayAt(Pixels(_harpoon.Tip));
        RodLight.Bit(Pixels(_harpoon.Tip), Unit(_harpoon.Heading), !Thumps(piece));
    }

    /// <summary>Whether a rod biting into <paramref name="piece"/> thumps as a glove does, a crate or a pod, rather than ringing as metal.</summary>
    /// <summary>
    /// Peer <paramref name="id"/>'s hit of <paramref name="tool"/> by <paramref name="hero"/> toward <paramref name="at"/>, heard and
    /// seen at the ground its fan strikes nearest the point, as that peer's own hit is (the fan of rays).
    /// </summary>
    private void StruckBy(Registry m, int id, Hero hero, Point at, string tool)
    {
        var fan = Aiming.FanOf(m, hero, at, tool);
        if (!fan.Hewn.IsEmpty) Struck(_othersGroundHits, m, Aiming.LeadOf(fan, at, m.Grid.Width), tool, hero, id);
    }

    private static bool Thumps(Piece piece) => Content.Loot.Of(piece.Item) != null && Content.Wreckage.HpOf(piece.Item) == null;

    /// <summary>The spring's breath leaving <paramref name="figure"/>'s muzzle with the rod, along its barrel, and its glint.</summary>
    private static void Puff(Figure figure)
    {
        if (figure.Muzzle is { } at) RodLight.Fired(figure.Held, at, figure.Barrel);
    }

    private void PaintLines(Sketch g)
    {
        foreach (var shown in _shownOthers.Values)
            if (shown.Line != null) PaintLine(g, shown.Line);
        if (_line != null) PaintLine(g, _line);
    }

    private void PaintLine(Sketch g, HarpoonLine line)
    {
        _linePoints.Clear();
        line.DrawnInto(_linePoints);
        for (int i = 0; i < _linePoints.Count; i++) _linePoints[i] = (_linePoints[i].X / Hero.Unit, _linePoints[i].Y / Hero.Unit);
        g.Rope(_linePoints, HarpoonRope, 1);
    }

    /// <summary>A hero's tether tied at <paramref name="anchor"/>, anew wherever it is tied; none aboard.</summary>
    private static Tether? Tie(Tether? line, int seed, Point? anchor, (double X, double Y) clip, double elapsedMs)
    {
        if (anchor is not Point a) return null;
        if (line?.Anchor != a) line = new Tether(a, clip, seed);
        line.Update((a.X, a.Y), clip, elapsedMs);
        return line;
    }

    private void PaintTethers(Sketch g)
    {
        foreach (var shown in _shownOthers.Values) PaintTether(g, shown.Tether);
        PaintTether(g, _tether);
    }

    private void PaintTether(Sketch g, Tether? line)
    {
        if (line == null) return;
        _tetherPoints.Clear();
        line.DrawnInto(_tetherPoints);
        // Thousandths of a cell to cells, which the sketch traces ropes in.
        for (int i = 0; i < _tetherPoints.Count; i++) _tetherPoints[i] = (_tetherPoints[i].X / Hero.Unit, _tetherPoints[i].Y / Hero.Unit);
        g.Rope(_tetherPoints, Line, 1);
        g.DrawCircle(new Vector2(line.Anchor.X, line.Anchor.Y) * (float)(Rig.Cell / Hero.Unit), 1.5f, Line);
    }
}
