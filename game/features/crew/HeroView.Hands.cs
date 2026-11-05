// HeroView's hands: what this hero works at the pointer (E, plans, the axe, the drill), how it sounds and the aim's outline.
using System.Collections.Generic;
using System.Linq;
using Godot;
using Stillwake.Game.Features.Effects;
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
    /// What the hands do at the pointer: the aim said and outlined while the hero is awake, whatever the
    /// hand holds, doors and valves as <paramref name="shown"/>; E on what it works whatever the hand holds,
    /// so a door opens with a card or a cable out; the left button held works plans with the screwdriver in
    /// hand (0107); the left button grabs only with a free hand, as it is the card's otherwise, and held
    /// or clicked swings the fire axe toward the pointer while the hand wields it (<see cref="AxeSwing"/>),
    /// unless the click grabbed a piece, hitting what its fan of rays from the shoulder meets (tool-reach); the right button does nothing with a tool.
    /// Passed out, the hands do nothing: a press is spent, a hold works no plan and swings no axe.
    /// </summary>
    private void UseHands(Client client, Registry m, ShownGrid shown, (double X, double Y)? pointer, bool passedOut, Pose pose,
        double elapsedMs, bool handFree)
    {
        // Flying in a drop pod, the hands do nothing, as passed out: E is no one's until it lands.
        passedOut |= m.Landing.FlightOf(client.Id) != null;
        // A tool struck with by hand, the axe, the sledgehammer or the spear: its strokes (Strokes).
        string? struck = handFree && Strokes.Of(_ui.Wielded.Get()) != null ? _ui.Wielded.Get() : null;
        var stroke = Strokes.Of(struck);
        bool gun = handFree && _ui.Wielded.Get() == Content.Harpoon.Item;
        var shooter = GunInHand(handFree);
        string? drill = handFree && HeldLook.IsDrill(_ui.Wielded.Get()) ? _ui.Wielded.Get() : null;
        var driver = handFree ? Content.Screwdrivers.Of(_ui.Wielded.Get()) : null;
        // Seated in a drop pod, the hands work nothing: E leaves it, wherever the pointer is.
        bool seated = m.Landing.SeatOf(client.Id) != null;
        // A running tool's hit: its fan of rays from the shoulder, the cutter's beam as drawn (tool-reach).
        var edged = drill != null && pointer != null && !seated ? EdgeOf(m, _current, drill, _figure) : null;
        // Whatever hits or shoots aims at nothing: it hits what its rays meet, no outline, glow or prompt (melee-edge).
        bool armed = struck != null || gun || shooter != null || drill != null;
        var aim = pointer is { } p && !seated ? Aiming.AimAt(m, client.Id, _current, p, Predicted.Grabbing(client.InFlight), armed, shown, ShipTiles.RiseOf) : null;
        // The power driver charged in its mode works the square round the pointer while it holds plans or marks, every layer of
        // each cell, shown before the press; over nothing the pointer aims at one cell (power-driver).
        bool charged = driver != null && Content.Items.Get(driver.Item).Draw is int pays && m.Items.CanDrain(client.Id, pays);
        int side = Aiming.SideOf(driver, _ui.DriverSquare.Get(), charged);
        var squared = aim != null && !passedOut ? Aiming.WorkedSquare(m, client.Id, _current, pointer!.Value, side, aim.Work) : null;
        var square = squared?.Square;
        var targets = squared?.Targets;
        _ui.WorkedSquare = square?.Cells;
        var prompt = passedOut ? null : seated ? Prompts.Seated : Prompts.Of(m, targets != null ? Aiming.OverSquare(aim!, targets) : aim, driver?.Item);
        // The power driver's mode by the arrow over the world: its square's dots, lit, one, or waiting for charge (mode-glyph).
        bool marked = !passedOut && !seated && pointer != null && GetViewport().GuiGetHoveredControl() == null;
        _glyph.Show(marked ? DriverMode.DotsOf(driver, _ui.DriverSquare.Get(), charged) : null,
            pointer is { } tip ? GetCanvasTransform() * (new Vector2((float)tip.X, (float)tip.Y) * ShipCamera.Cell) : default);
        if (square is { } sq) TraceSquare(m, (sq.X, sq.Y, side), targets!);
        else TraceAim(m, shown, prompt != null && aim is { Piece: null } ? aim : null, prompt?.Problem != null);
        _prompt.Show(prompt);
        _prompt.Place(_ui.MenuTop);
        _ui.AimedPiece.Set(prompt != null && aim?.Piece is int piece ? (piece, prompt.Problem != null) : null);

        // The press is taken every frame, so one made out of reach is spent rather than kept. The left
        // button grabs debris too: out in space, with the keys held and the mouse aiming, E is a stretch.
        bool pressed = (_autopilot?.TakePress() ?? false) | TakeUse();
        bool clicked = _mouse.Buttons.TakeClick();
        // With the harpoon's rod out of the gun, a click is the gun's: hooked, it reels, even on a piece in reach.
        bool grab = clicked && aim?.Piece != null && !(gun && _harpoon.Rod != Rod.Seated);
        bool acts = !passedOut && handFree;
        bool builds = acts && driver != null && _mouse.Buttons.HoldsLeft;
        if (seated && pressed && !passedOut)
        {
            client.Send(new Sim.Landing.Alight());
            _ui.OpenCrate.Set(null);
        }
        // Running, four ticks of work a tick over the square, two at most on one plan; flat, the screwdriver's one (power-driver).
        var (pace, budget) = Aiming.PaceOf(driver, m.Items.Running(client.Id));
        var uses = Aiming.UsesOf(aim, !passedOut && pressed || acts && grab, builds, client.World.Tick, _workedOnTick, pace, targets, budget);
        _workedOnTick = uses.WorkedOnTick;
        foreach (var command in uses.Commands) client.Send(command);
        if (uses.Open != null || uses.Commands.Any(c => c is Sim.Debris.Grab or Sim.Atmosphere.LoadCanister or Sim.Water.PourWater)) _act.Play(Floats);
        // Held on a plan it can work, the hero turns its screwdriver at it; on a square, while any of its plans can be worked.
        bool working = builds && (targets != null ? targets.Any(t => t.Problem == null) : aim is { InReach: true, Work.Problem: null });
        // A right click with a canister in hand breathes it (0047); with the power driver, switches its square and one cell.
        bool rightClicked = _mouse.Buttons.TakeRightClick();
        if (rightClicked && acts && _ui.Wielded.Get() == Content.Crew.Canister) Breathe(client, m);
        if (rightClicked && acts && driver is { Area: > 1 }) _ui.DriverSquare.Set(!_ui.DriverSquare.Get());
        // The axe: a swing begun toward the pointer plays through and, as it strikes, its fan hits what it meets, let go or not;
        // put away, it stops unstruck. A wreck's chop is heard where it takes the hit (DebrisView).
        bool grabbed = grab && uses.Commands.Any(c => c is Sim.Debris.Grab);
        _grabHeld = (_grabHeld || grabbed) && _mouse.Buttons.HoldsLeft;
        bool swings = acts && stroke != null && !working && Poses.HoldsIn(pose);
        if (!swings) _swing.Stop();
        bool wanted = swings && pointer != null && !grabbed && !_grabHeld && (_mouse.Buttons.HoldsLeft || clicked);
        var swung = _swing.Advance(elapsedMs, wanted, pointer is { } aimed ? Aiming.PointOf(aimed) : default, stroke);
        if (swung.Begun)
        {
            _swingFace = Poses.FaceOf(pointer, _current) ?? _current.Facing;
            _current = _current with { Facing = _swingFace };
        }
        if (swung.Whooshes && S.SwingOf(struck) is var whoosh) (whoosh == S.HammerSwing ? _hammerSwing : whoosh == S.SpearThrust ? _spearThrust : _swingSound).Play();
        if (swung.Hits)
        {
            // What the stroke's fan of rays from the shoulder meets as it lands (tool-reach): the cells, wrecks and
            // creatures the sim would take its hits on; a creature's flinch and a wreck's chop are theirs to show (0102, DebrisView).
            var hit = StrokeOf(m, _current, _swing.At, struck!);
            var sent = ToolEdges.CommandsOf(m, client.Id, struck!, hit);
            if (sent.Cells is { } cells) client.Send(cells);
            if (sent.Pieces is { } pieces) client.Send(pieces);
            foreach (var strike in sent.Strikes) client.Send(strike);
            // The ground it bites first heard and seen there, the ship's hull too, which takes no harm: a clang and sparks.
            if (hit.Ground?.Cell is int bitten) Struck(_groundHits, m, bitten, struck!, _current, client.Id);
            // A blade that bites is drawn stopped on contact a moment (0075).
            if (hit.Contact != null || sent.Strikes.Count > 0) _figure.Bit();
        }
        // The drill: aimed at the pointer; held on the left button it runs, aboard or at nothing too, the sim drawing
        // its power every tick it runs (0055), and what its rays meet is hit each Pace ticks (tool-reach). Flat, it winds down. The power
        // driver runs only while it works a plan, so a press at nothing draws no charge (power-driver); it is no drill.
        bool wieldsDrill = acts && drill != null && !working && Poses.HoldsIn(pose);
        _aimsDrill = wieldsDrill && pointer != null;
        var running = Motor.Wanted(m.Items, client.Id, _aimsDrill ? drill : working ? driver!.Item : null, _mouse.Buttons.HoldsLeft, _grabHeld);
        // Sent before this frame's hits, so the host applies the run first: a hit of the drill needs it.
        if (Motor.RunOf(m.Items, client.Id, running) is { } run) client.Send(run);
        bool runs = running != null && running == drill;
        Drill(client, m, runs ? edged : null, drill);
        // The power driver running: its chuck turns at its work, and its motor is heard there; flat, the wrist twists it (power-driver).
        bool drives = HeldLook.Drives(running);
        _figure.Spinning = runs || drives;
        // Loaded while its edge touches something it hits, a wreck, the ground or a creature. Its hits sound as what they hit
        // and crack it (0093); it is seen at work where it touches, every frame, whether a hit lands then or not (tool-contact).
        bool works = runs && edged != null && ToolEdges.CommandsOf(m, client.Id, running!, edged).Any;
        _ownWork = runs ? WorkOf(m, client.Id, _current, edged, running!, _figure) : null;
        HearDrill(drill, runs, works, elapsedMs);
        // The harpoon: aimed at the pointer out in space; a fresh click fires, reels or does nothing, never a hold.
        bool wieldsGun = acts && gun && Poses.HoldsIn(pose);
        // Aimed aboard too, though it fires only out in space.
        // A gun of the sim's shots, aimed at the pointer anywhere; its shot fires from the muzzle as last drawn.
        bool wieldsShooter = acts && shooter != null && Poses.HoldsIn(pose);
        _aimsGun = (wieldsGun || wieldsShooter) && pointer != null;
        // An aimed item comes up while used and a moment after, else it hangs (aim-on-use): held, clicked, running, its rod out or
        // thrust. A click waits till the arm is up, so a shot leaves along the aim, never the hanging barrel.
        bool held = _mouse.Buttons.HoldsLeft && !_grabHeld, thrusts = swings && stroke?.Kind == StrokeKind.Thrust;
        bool fresh = clicked && !grabbed || _clickWaits;
        bool used = (_aimsGun || _aimsDrill) && (held || fresh || runs || gun && _harpoon.Rod != Rod.Seated) || thrusts && (wanted || _swing.Phase != null);
        _armUp = _arm.Step(used, elapsedMs) && (_aimsGun || _aimsDrill || thrusts);
        bool ready = _figure.Aimed;
        _clickWaits = fresh && !ready && _aimsGun;
        Shoot(client, m, shooter, wieldsShooter, fresh && ready, held && ready, _hosts, elapsedMs);
        var middle = _current.Middle;
        var hold = new GunHold(wieldsGun, _figure.Muzzle is { } muzzle ? Thousandths(muzzle) : null, (_figure.Barrel.X, _figure.Barrel.Y),
            (middle.X, middle.Y), _letsGo);
        _letsGo = false;
        var shot = _harpoon.Step(elapsedMs, fresh && ready, hold, _harpoonWorld);
        if (shot.Fired)
        {
            _shot.Play();
            Puff(_figure);
        }
        if (shot.Hooked is int hooked) Bite(hooked);
        // The rod biting into the ship's structure rings as metal does.
        if (shot.Gripped)
        {
            _bite.PlayAt(Pixels(_harpoon.Tip));
            RodLight.Bit(Pixels(_harpoon.Tip), Unit(_harpoon.Heading), metal: true);
        }
        if (shot.Reel is { } reel) client.Send(reel);
        if (shot.Ready) _ready.Play();
        // The drum turns from the press that reels till the piece is in, or while it pulls the hero, till the line comes free.
        if (HarpoonShot.Drums(_harpoon.Rod) != _drum.Playing)
        {
            if (_drum.Playing) _drum.Stop();
            else _drum.Play();
        }
        // Pulled, the gun points along the line at the hook; hanging, at the pointer again, as a wall it hangs flush on would swallow it.
        _figure.Aim = _harpoon.Rod == Rod.Pulling && wieldsGun ? Pixels(_harpoon.Tip)
            : _armUp && pointer is { } at ? new Vector2((float)(at.X * Rig.Cell), (float)(at.Y * Rig.Cell)) : null;
        _figure.Loaded = shooter != null ? _guns.Ready : _harpoon.Ready;
        _wields = passedOut ? null : _ui.Wielded.Get();
        _busy = working ? new Busy(Handwork.Work, square is { } box ? Middle(box.X, box.Y, side) : Middle(aim!.Cell))
            : _swing.Phase != null ? new Busy(Handwork.Chop, _swing.At, _swing.Count)
            : runs ? new Busy(Handwork.Drill, Aiming.PointOf(pointer!.Value))
            : _beamLeftMs > 0 && !handFree && !passedOut ? new Busy(Handwork.Beam, _beamAt)
            : null;
        _driverHeard.Step(drives && working, _busy is { Work: Handwork.Work, At: var worked } ? Pixels(worked) : null, elapsedMs / 1000);
        ShowHands(_figure, _wields, FittedOf(m, client.Id, _wields), _busy, _swing.Phase is double phase ? (_swing.At, phase) : null);
        // The computer opens its own window; every other machine its slots under the inventory.
        if (uses.Open is int open && m.Grid.MachineById(open) is { } machine)
        {
            if (Content.Machines.Is(machine.Kind, Content.MachineTrait.Computer)) _ui.OpenComputer.Set(open);
            else _ui.OpenCrate.Set(open);
        }
        // A crate or a computer stays open while the hero can reach it, awake.
        if (_ui.OpenCrate.Get() is int opened && (passedOut || !Aiming.CrateInReach(m.Grid, _current, opened))) _ui.OpenCrate.Set(null);
        if (_ui.OpenComputer.Get() is int screen && (passedOut || !Aiming.CrateInReach(m.Grid, _current, screen))) _ui.OpenComputer.Set(null);
    }

    /// <summary>
    /// The drill running, touching <paramref name="hit"/> with its edge, or not at all with none: when its pace says, what the edge
    /// touches is hit (tool-edge), the ground heard and seen once at the first cell it strikes, a wreck heard as it sounds hit, the
    /// chop, once a hit (0093); a creature struck at the same pace, how it sounds and flinches the creatures' (0102).
    /// </summary>
    private void Drill(Client client, Registry m, EdgeHit? hit, string? drill)
    {
        if (!_drillPace.Due(client.World.Tick, hit != null && drill != null, Content.Weapons.Of(drill)?.Pace ?? 1)) return;
        var sent = ToolEdges.CommandsOf(m, client.Id, drill!, hit!);
        if (sent.Cells is { } cells)
        {
            if (ToolEdges.LeadOf(m, hit!) is int lead) Struck(_groundHits, m, lead, drill!, _current, client.Id);
            client.Send(cells);
        }
        if (sent.Pieces is { } pieces)
        {
            client.Send(pieces);
            if (S.ChopsWrecks(drill)) _drillChop.PlayAt(_figure.Muzzle ?? _figure.GlobalPosition, 1, GroundSounds.HitDbOf(drill!));
        }
        foreach (var strike in sent.Strikes) client.Send(strike);
    }

    /// <summary>
    /// The drill's motor heard at its bit as last drawn whenever it <paramref name="runs"/>, spun up and down with its picture
    /// and loaded while it <paramref name="works"/> on a piece (0059), held on <c>Sound.DrillTouchHoldS</c> past the bit leaving
    /// it. Its hits sound as what they hit, a wreck's chop or the ground's (0093), each one as it is sent. The grinder's motor is a
    /// loop on the same spin, its wheel biting as it comes on what it grinds; the cutter's jet struck, looped and cut off with its run.
    /// </summary>
    private void HearDrill(string? tool, bool runs, bool works, double elapsedMs)
    {
        double elapsedS = elapsedMs / 1000;
        bool onPiece = runs && _onPiece.Step(works, elapsedS, S.DrillTouchHoldS);
        var at = _figure.Muzzle ?? _figure.GlobalPosition;
        _drillMotor.Step(runs, onPiece, elapsedS, S.DrillMotor);
        double speed = _drillMotor.Speed(S.DrillMotor);
        float gain = _drillMotor.GainDb(S.DrillMotor);
        bool grinder = tool == Content.Weapons.Grinder.Item, cutter = tool == Content.Weapons.PlasmaCutter.Item;
        int grains = _grains.Due(elapsedMs, _drillMotor.Spin > 0 && !grinder && !cutter, S.DrillWhirGrainsPerSecond * speed);
        for (int i = 0; i < grains; i++)
        {
            var (spread, _) = StrikeTrain.Vary(_strikeSpread, S.DrillWhirPitch, 0);
            _whir.PlayAt(at, (float)speed * spread, gain);
        }
        var cue = _runSounds.Step(runs && (grinder || cutter), onPiece, elapsedS, S.GrinderBiteEveryS);
        if (cue.Starts && cutter)
        {
            _cutterStrike.PlayAt(at);
            _cutterRun.PlayAt(at);
        }
        if (cue.Stops && _cutterRun.Playing)
        {
            _cutterRun.Stop();
            _cutterOff.PlayAt(at);
        }
        if (cue.Bites && grinder) _grinderBite.PlayAt(at);
        bool spins = grinder && _drillMotor.Spin > 0;
        if (spins && !_grinderRun.Playing) _grinderRun.PlayAt(at, (float)speed, gain);
        else if (!spins && _grinderRun.Playing) _grinderRun.Stop();
        (_grinderRun.GlobalPosition, _grinderRun.PitchScale, _grinderRun.OffsetDb, _cutterRun.GlobalPosition) = (at, (float)speed, gain, at);
    }

    /// <summary>
    /// A hero's power driver heard where it works (power-driver), on the World bus as the ratchet it stands in for: its bite, its
    /// loop and its run-down, played as its <see cref="BiteLoop"/> says, following the work; letting go, the bite and loop fade out.
    /// </summary>
    private sealed class DriverHeard
    {
        public readonly Heard2D Bite = Cues.Player2D(S.PowerDriverBite, S.World), Loop = Cues.Player2D(S.PowerDriverLoop, S.World),
            Off = Cues.Player2D(S.PowerDriverOff, S.World);
        public BiteLoop Hum;
        /// <summary>Where it last worked, in the world's pixels.</summary>
        public Vector2 At;
        private double _fadeS;

        public Node[] Players => [Bite, Loop, Off];

        /// <summary>One frame: whether it works (<paramref name="works"/>), and where, in the world's pixels.</summary>
        public void Step(bool works, Vector2? at, double elapsedS)
        {
            if (at is { } now) At = now;
            switch (Hum.Step(works, elapsedS))
            {
                case BiteCue.Bite:
                    (_fadeS, Loop.OffsetDb) = (0, 0);
                    Loop.Stop();
                    Bite.PlayAt(At);
                    break;
                case BiteCue.Loop:
                    Loop.PlayAt(At);
                    break;
                case BiteCue.Off:
                    _fadeS = S.PowerDriverFadeS;
                    Off.PlayAt(At);
                    break;
            }
            Bite.GlobalPosition = Loop.GlobalPosition = At;
            if (_fadeS <= 0) return;
            _fadeS = System.Math.Max(0, _fadeS - elapsedS);
            Bite.OffsetDb = Loop.OffsetDb = Mathf.LinearToDb((float)System.Math.Max(_fadeS / S.PowerDriverFadeS, 1e-4));
            if (_fadeS > 0) return;
            Bite.Stop();
            Loop.Stop();
        }
    }

    /// <summary>Where every running tool on this surface touches what it works, as last drawn, and the tool: where a wreck it bites is struck (0094).</summary>
    private List<(float X, float Y, string Tool)> Bits()
    {
        var bits = new List<(float X, float Y, string Tool)>();
        foreach (var work in Works())
        {
            bits.Add((work.At.X, work.At.Y, work.Tool));
            foreach (var (_, at) in work.Wrecks) bits.Add((at.X, at.Y, work.Tool));
        }
        return bits;
    }

    /// <summary>Every running tool on this surface as last seen at work: this hero's and its friends' (tool-contact).</summary>
    private IEnumerable<ToolWork.Seen> Works()
    {
        if (_ownWork is { } own) yield return own;
        foreach (var shown in _shownOthers.Values)
            if (shown.Work is { } work) yield return work;
    }

    /// <summary>
    /// What running <paramref name="tool"/> held by <paramref name="hero"/> touches as <paramref name="figure"/> drew it last: the
    /// cutter's beam from its nozzle (tool-edge), any other's fan of rays from the shoulder along its barrel (tool-reach); none
    /// while it is not up.
    /// </summary>
    private EdgeHit? EdgeOf(Registry m, Hero hero, string tool, Figure figure)
    {
        if (!figure.Visible || !figure.Aimed || figure.EdgeStart is not { } start || figure.Muzzle is not { } end) return null;
        var solid = _wreckPixels.Solid(_progress, _ui.DebrisTurns);
        if (tool == Content.Weapons.PlasmaCutter.Item)
            return ToolEdges.Hits(m, ToolEdges.Of(tool, hero, Thousandths(start), Thousandths(end)), Outside(m, hero), _progress, solid);
        var hand = Aiming.HandOf(hero);
        var barrel = figure.Barrel;
        return ToolRays.Fan(m, hero, tool, ToolRays.HeadingOf(hero, (hand.X + barrel.X * Hero.Unit, hand.Y + barrel.Y * Hero.Unit)),
            Outside(m, hero), _progress, solid);
    }

    /// <summary>
    /// What a stroke of <paramref name="tool"/> by <paramref name="hero"/> toward <paramref name="at"/> touches: its fan of rays from
    /// the shoulder (tool-reach), the wrecks as drawn this frame.
    /// </summary>
    private EdgeHit StrokeOf(Registry m, Hero hero, Point at, string tool) =>
        ToolRays.Fan(m, hero, tool, ToolRays.HeadingOf(hero, (at.X, at.Y)), Outside(m, hero), _progress, _wreckPixels.Solid(_progress, _ui.DebrisTurns));

    /// <summary>Whether <paramref name="hero"/> is out in space, where its tools touch wrecks.</summary>
    private static bool Outside(Registry m, Hero hero) => m.Rooms.RoomAt(hero.BodyCell(m.Grid.Width)) == Sim.Rooms.RoomsModule.Space;

    /// <summary>
    /// Peer <paramref name="id"/>'s running <paramref name="tool"/> by <paramref name="hero"/>, what its edge touches this frame
    /// (<paramref name="hit"/>), as seen at work (tool-contact, tool-edge): from its edge's start as <paramref name="figure"/> was drawn
    /// to where it first touches, else to its end, looking as what it touches looks hit, a wreck or the wall's material, and
    /// sparking at every wreck it touches; none unseen.
    /// </summary>
    private static ToolWork.Seen? WorkOf(Registry m, int id, Hero hero, EdgeHit? hit, string tool, Figure figure)
    {
        if (!HeldLook.IsDrill(tool) || hit == null || figure.EdgeStart is not { } start) return null;
        var wrecks = new List<(int Piece, Vector2 At)>();
        foreach (var touch in hit.Touches)
            if (touch.Wreck is { } wreck) wrecks.Add((wreck.Id, Pixels(touch.At)));
        if (hit.Contact is not { } contact) return new(id, tool, start, Pixels(hit.End), figure.Barrel, null, null, wrecks);
        var at = Pixels(contact.At);
        if (contact.Wreck != null) return new(id, tool, start, at, figure.Barrel, HitLooks.Of[Impact.Wreck], null, wrecks);
        if (contact.Cell is not int cell || Impacts.At(m.Grid, m.Building, cell) is not { } impact) return new(id, tool, start, at, figure.Barrel, null, null, wrecks);
        var middle = hero.Middle;
        var face = Impacts.On(m.Grid, cell, (double)middle.X / Hero.Unit, (double)middle.Y / Hero.Unit);
        return new(id, tool, start, at, figure.Barrel, HitLooks.Of[impact], Impacts.FloorUnder(m.Grid, face) * Rig.Cell, wrecks);
    }

    /// <summary>The wrecks' pictures, read once each, that a running tool's edge is tested against (tool-edge).</summary>
    private readonly WreckPixels _wreckPixels = new();

    /// <summary>
    /// A hit of <paramref name="tool"/> on <paramref name="cell"/> by <paramref name="hero"/>
    /// heard and seen where it strikes, as the wall or growth it lands
    /// on sounds and looks hit, whatever the tool (0093, 0094): on the face of it turned toward the hero (<see cref="Impacts.On"/>),
    /// heard on <paramref name="voices"/>. Every hit, a drill's as often as it hits, each throwing its share of the axe's look,
    /// the one that breaks or fells it the whole (<see cref="Impacts.ShareAt"/>), by the damage the sim gives peer
    /// <paramref name="peer"/>'s hit, its module's boost in it (0098).
    /// </summary>
    private static void Struck(Voices voices, Registry m, int cell, string tool, Hero hero, int peer)
    {
        var middle = hero.Middle;
        var at = Impacts.On(m.Grid, cell, (double)middle.X / Hero.Unit, (double)middle.Y / Hero.Unit);
        var point = new Vector2((float)(at.X * Rig.Cell), (float)(at.Y * Rig.Cell));
        int damage = m.Items.BlockDamage(peer, tool);
        if (GroundSounds.HitAt(m.Grid, m.Building, cell, damage, tool) is { } hit)
            voices.Play(hit, point, offsetDb: GroundSounds.HitDbOf(tool));
        if (Impacts.At(m.Grid, m.Building, cell) is not { } impact) return;
        double? floor = Impacts.FloorUnder(m.Grid, at) * Rig.Cell;
        double share = Impacts.ShareAt(m.Grid, m.Building, cell, tool, damage);
        // A running tool is seen at work all along (ToolWork): only the hit that breaks the wall or fells the growth throws.
        if (HeldLook.IsDrill(tool) && share < 1) return;
        Stages.Of(Layers.Effects).Hits.Throw(HitLooks.Of[impact], point.X, point.Y, at.Nx, at.Ny, share, floor);
    }

    /// <summary>Whether <paramref name="hero"/>, awake, touches lava where the sim has it, its suit burning (0087).</summary>
    private static bool Burns(Registry m, Sim.Crew.Hero? hero) => hero is { Out: 0 } && m.Crew.Burns(hero.Cell);

    /// <summary>What <paramref name="hero"/>'s boots stand on, or <paramref name="was"/> while nothing holds it up (0093).</summary>
    private static S.Footing FootingOf(Registry m, Hero hero, S.Footing was) =>
        GroundSounds.FootingOf(m.Grid, m.Building.Plants, hero.X, hero.Y) ?? was;

    /// <summary>The jetpack's hiss while this hero thrusts, risen and fallen by its envelope; stopped once fallen.</summary>
    private void HearThrust(double elapsedMs)
    {
        _thrustSound.Step(_thrust != null, elapsedMs / 1000, S.ThrustAttackS, S.ThrustReleaseS);
        if (_thrustSound.Playing && !_thrustLoop.Playing) _thrustLoop.Play();
        else if (!_thrustSound.Playing && _thrustLoop.Playing) _thrustLoop.Stop();
        _thrustLoop.VolumeDb = S.Thrust.VolumeDb + _thrustSound.GainDb;
    }

    /// <summary>
    /// A figure's hands as <paramref name="wields"/> and <paramref name="busy"/> say, the axe swung toward
    /// <paramref name="swing"/>'s point as far as it is through: this machine's hero's and the others' alike.
    /// </summary>
    private static void ShowHands(Figure figure, string? wields, IReadOnlyList<Content.ModuleSpec> fitted, Busy? busy, (Point At, double Phase)? swing)
    {
        figure.Wields = wields;
        figure.Fitted = fitted;
        figure.Chop(swing is { At: var point, Phase: var phase } ? (Pixels(point), phase) : null);
        figure.Work(busy is { Work: Handwork.Work, At: var plan } ? Pixels(plan) : null);
    }

    /// <summary>
    /// The modules fitted into the stack of <paramref name="wields"/> peer <paramref name="peer"/> works with, as the sim holds
    /// its slots (0098, <see cref="Sim.Items.ToolStats.ModulesOf"/>): every peer's alike, a friend's too; none for an empty hand or a plain tool.
    /// </summary>
    private static IReadOnlyList<Content.ModuleSpec> FittedOf(Registry m, int peer, string? wields) =>
        wields != null && m.Items.ToolStack(peer, wields) is { } stack ? Sim.Items.ToolStats.ModulesOf(stack) : [];

    /// <summary>
    /// How far the floor runs open from <paramref name="hero"/>'s feet to either side, in pixels of the world, looking eight
    /// cells along its lowest row: a landing's dust spreads no further, so none passes the walls of a shaft (0077).
    /// </summary>
    private static (double Left, double Right) FloorRoom(Sim.Grid.IGridQueries grid, Hero hero)
    {
        int w = grid.Width, cx = hero.X / Hero.Unit, row = (hero.Y - 1) / Hero.Unit;
        double Room(int dir)
        {
            for (int i = 1; i <= 8; i++)
            {
                int x = cx + dir * i;
                if (x < 0 || x >= w || row < 0 || row >= grid.Height) break;
                if (grid.Blocks(row * w + x))
                    return (dir > 0 ? x * Hero.Unit - hero.X : hero.X - (x + 1) * Hero.Unit) * Rig.Cell / Hero.Unit;
            }
            return double.MaxValue;
        }
        return (Room(-1), Room(1));
    }

    /// <summary>
    /// How far the top of the ladder <paramref name="hero"/> climbs, or of the ledge its jump steps up onto, is over its feet, in
    /// pixels: what its figure pulls itself up onto (0092), read alike for every hero from where it is.
    /// </summary>
    private static double? LedgeOf(Ship ship, Hero hero) =>
        (Hero.LedgeAbove(ship, hero) ?? Hero.MantleLedge(ship, hero)) is int up ? up * Rig.Cell / Hero.Unit : null;

    /// <summary>The middle of <paramref name="cell"/>, in thousandths of a cell.</summary>
    private Point Middle(int cell) => new(cell % _width * Hero.Unit + Hero.Unit / 2, cell / _width * Hero.Unit + Hero.Unit / 2);

    /// <summary>The middle of the square <paramref name="side"/> cells across from column <paramref name="x"/>, row <paramref name="y"/>, in thousandths of a cell.</summary>
    private static Point Middle(int x, int y, int side) => new(x * Hero.Unit + side * Hero.Unit / 2, y * Hero.Unit + side * Hero.Unit / 2);

    /// <summary>A point in thousandths of a cell, in the world's pixels.</summary>
    private static Vector2 Pixels(Point at) => new Vector2(at.X, at.Y) * (float)(Rig.Cell / Hero.Unit);

    /// <summary>
    /// Outlines <paramref name="aim"/>, green, or red when the sim would <paramref name="refuse"/>; nothing with none. Aimed at
    /// the <paramref name="back"/> layer (0107), the line is dashed and drawn a pixel inside.
    /// </summary>
    private void TraceAim(Registry m, ShownGrid shown, Aim? aim, bool refuse, bool back = false)
    {
        var grid = m.Grid;
        var traced = aim is { } a
            ? (a.Cell, a.Work?.Plan, refuse, shown.StructureAt(grid, a.Cell), shown.ValveOpenAt(m.Pipes, a.Cell) switch { null => 0, true => 1, false => 2 }, grid.MachineAt(a.Cell)?.Id, back)
            : ((int, int?, bool, int, int, int?, bool)?)null;
        if (traced == _traced && _tracedSquare == null) return;
        _traced = traced;
        _tracedSquare = null;
        // A plan or a mark aimed at glows as the driver's square does, teal over the plans' pale ghosts (driver-square).
        bool plan = aim?.Work is { Repair: null };
        _outline.Dashed = back;
        _outline.ZIndex = plan ? Layers.SquareAim : Layers.Aim;
        _outline.Begin();
        if (aim != null) AimOutline.Trace(_outline, m, aim, shown, refuse ? AimRefused : plan ? Outline.SquareReady : AimReady);
        _outline.End();
    }

    /// <summary>The power driver's <paramref name="square"/> outlined, its side showing the mode, and each of its <paramref name="targets"/> (power-driver).</summary>
    private void TraceSquare(Registry m, (int X, int Y, int Side) square, IReadOnlyList<WorkAim> targets)
    {
        if (_tracedSquare is { } was && was.Square == square && was.Targets.SequenceEqual(targets)) return;
        _tracedSquare = (square, [.. targets]);
        _traced = null;
        _outline.Dashed = false;
        _outline.ZIndex = Layers.SquareAim;
        _outline.Begin();
        AimOutline.Square(_outline, m, square, targets, Outline.SquareReady, AimRefused);
        _outline.End();
    }
}
