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
    /// unless the click grabbed a piece; the right button swings it, or runs the drill, at the back walls alone.
    /// Passed out, the hands do nothing: a press is spent, a hold works no plan and swings no axe.
    /// </summary>
    private void UseHands(Client client, Registry m, ShownGrid shown, (double X, double Y)? pointer, bool passedOut, Pose pose,
        double elapsedMs, bool handFree)
    {
        bool axe = handFree && _ui.Wielded.Get() == Content.Weapons.Axe.Item;
        bool gun = handFree && _ui.Wielded.Get() == Content.Harpoon.Item;
        string? drill = handFree && HeldLook.IsDrill(_ui.Wielded.Get()) ? _ui.Wielded.Get() : null;
        var driver = handFree ? Content.Screwdrivers.Of(_ui.Wielded.Get()) : null;
        // Ctrl held strikes one cell, the precise mode; else the brush.
        _brush = Brush.SideOf(Input.IsPhysicalKeyPressed(Key.Ctrl));
        // The right button is the back layer's (0107): a swing begun with it, or the drill run on it, digs the back walls.
        bool back = (axe || drill != null) && (_swing.Phase != null ? _swingBack : Layering.Back(_mouse.Buttons.HoldsLeft, _mouse.Buttons.HoldsRight));
        var aim = pointer is { } p ? Aiming.AimAt(m, client.Id, _current, p, Predicted.Grabbing(client.InFlight), axe, gun, drill, shown, brush: _brush, back: back) : null;
        var prompt = passedOut ? null : Prompts.Of(m, aim);
        TraceAim(m, shown, prompt != null && aim is { Piece: null } ? aim : null, prompt?.Problem != null, back);
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
        var uses = Aiming.UsesOf(aim, !passedOut && pressed || acts && grab, builds, client.World.Tick, _workedOnTick, driver?.Work ?? 1);
        _workedOnTick = uses.WorkedOnTick;
        foreach (var command in uses.Commands) client.Send(command);
        if (uses.Open != null || uses.Commands.Any(c => c is Sim.Debris.Grab or Sim.Atmosphere.LoadCanister)) _act.Play(Floats);
        // Held on a plan it can work, the hero turns its screwdriver at it.
        bool working = builds && aim is { InReach: true, Work.Problem: null };
        // A right click with a canister in hand breathes it (0047).
        bool rightClicked = _mouse.Buttons.TakeRightClick();
        if (rightClicked && acts && _ui.Wielded.Get() == Content.Crew.Canister) Breathe(client, m);
        // The axe: a swing begun toward the pointer plays through and, as it strikes, hits every wreck its
        // blade meets, let go or not; put away, it stops unstruck. A wreck's chop is heard where it takes the hit (DebrisView).
        bool grabbed = grab && uses.Commands.Any(c => c is Sim.Debris.Grab);
        _grabHeld = (_grabHeld || grabbed) && _mouse.Buttons.HoldsLeft;
        bool swings = acts && axe && !working && Poses.HoldsIn(pose);
        if (!swings) _swing.Stop();
        bool wanted = swings && pointer != null && !grabbed && !_grabHeld && (_mouse.Buttons.HoldsLeft || clicked || _mouse.Buttons.HoldsRight || rightClicked);
        var swung = _swing.Advance(elapsedMs, wanted, pointer is { } aimed ? Aiming.PointOf(aimed) : default);
        if (swung.Begun)
        {
            _swingFace = Poses.FaceOf(pointer, _current) ?? _current.Facing;
            _current = _current with { Facing = _swingFace };
            _swingBack = Layering.Back(_mouse.Buttons.HoldsLeft || clicked, _mouse.Buttons.HoldsRight || rightClicked);
        }
        if (swung.Whooshes) _swingSound.Play();
        if (swung.Hits)
        {
            // A back swing digs the back walls only: no wreck nor creature is in their layer.
            var hits = _swingBack ? [] : Aiming.HitsOf(m, client.Id, _current, _swing.At);
            foreach (var hit in hits) client.Send(hit);
            // The ground under the brush round the swing's point is dug as it strikes, in reach (0077), heard once.
            var digs = Aiming.DigsAt(m, client.Id, _current, _swing.At, Content.Weapons.Axe.Item, _brush, _swingBack);
            if (Aiming.LeadOf(digs, _swing.At, m.Grid.Width) is { } lead) Struck(_groundHits, m, lead, _current, client.Id);
            foreach (var dig in digs) client.Send(dig);
            // Every creature the blade meets, wherever the hero stands (0102): its flinch is the fauna's to show.
            var strikes = _swingBack ? [] : Aiming.StrikesOf(m, client.Id, Aiming.StruckCreatures(m, _current, _swing.At), Content.Weapons.Axe.Item);
            foreach (var strike in strikes) client.Send(strike);
            // A blade that bites is drawn stopped on contact a moment (0075).
            if (hits.Count > 0 || digs.Count > 0 || strikes.Count > 0) _figure.Bit();
        }
        // The drill: aimed at the pointer; held on the left button it runs, aboard or at nothing too, the sim drawing
        // its power every tick it runs (0055), and hits every wreck its bit touches each Pace ticks. Flat, it winds down.
        bool wieldsDrill = acts && drill != null && !working && Poses.HoldsIn(pose);
        _aimsDrill = wieldsDrill && pointer != null;
        var running = Motor.Wanted(m.Items, client.Id, _aimsDrill ? drill : null, _mouse.Buttons.HoldsLeft || _mouse.Buttons.HoldsRight, _grabHeld);
        // Sent before this frame's hits, so the host applies the run first: a hit of the drill needs it.
        if (Motor.RunOf(m.Items, client.Id, running) is { } run) client.Send(run);
        bool runs = running != null;
        Drill(client, m, runs ? pointer : null, drill, back);
        _figure.Spinning = runs;
        // Loaded while its bit is on a piece the sim would let it hit, a wreck or the ground. Its hits sound and look as what
        // they hit, where they hit it (0093, 0094); the bit throws nothing of its own.
        bool onWreck = runs && (Aiming.HitsOf(m, client.Id, Aiming.Touched(m, _current, Aiming.PointOf(pointer!.Value)), running!).Count > 0
            || Aiming.StrikesOf(m, client.Id, Aiming.TouchedCreatures(m, _current, Aiming.PointOf(pointer!.Value)), running!).Count > 0);
        bool works = onWreck || runs && Aiming.DigsAt(m, client.Id, _current, Aiming.PointOf(pointer!.Value), running!, _brush, back).Count > 0;
        HearDrill(runs, works, elapsedMs);
        // The harpoon: aimed at the pointer out in space; a fresh click fires, reels or does nothing, never a hold.
        bool wieldsGun = acts && gun && Poses.HoldsIn(pose);
        // Aimed aboard too, though it fires only out in space.
        _aimsGun = wieldsGun && pointer != null;
        var middle = _current.Middle;
        var hold = new GunHold(wieldsGun, _figure.Muzzle is { } muzzle ? Thousandths(muzzle) : null, (_figure.Barrel.X, _figure.Barrel.Y),
            (middle.X, middle.Y), _letsGo);
        _letsGo = false;
        var shot = _harpoon.Step(elapsedMs, clicked && !grabbed, hold, _harpoonWorld);
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
            : (_aimsGun || _aimsDrill) && pointer is { } at ? new Vector2((float)(at.X * Rig.Cell), (float)(at.Y * Rig.Cell)) : null;
        _figure.Loaded = _harpoon.Ready;
        _wields = passedOut ? null : _ui.Wielded.Get();
        _busy = working ? new Busy(Handwork.Work, Middle(aim!.Cell))
            : _swing.Phase != null ? new Busy(Handwork.Chop, _swing.At, _swing.Count)
            : runs ? new Busy(Handwork.Drill, Aiming.PointOf(pointer!.Value))
            : _beamLeftMs > 0 && !handFree && !passedOut ? new Busy(Handwork.Beam, _beamAt)
            : null;
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
    /// The drill running at <paramref name="pointer"/>, in cells, or not at all with none: when its pace says (<see cref="DrillPace"/>),
    /// a hit for each wreck its bit touches that the sim would take, heard as the wreck sounds hit, the chop, once a hit (0093).
    /// </summary>
    private void Drill(Client client, Registry m, (double X, double Y)? pointer, string? drill, bool back)
    {
        if (!_drillPace.Due(client.World.Tick, pointer != null && drill != null, Content.Weapons.Of(drill)?.Pace ?? 1)) return;
        var at = Aiming.PointOf(pointer!.Value);
        if (back)
        {
            // On the right button the bit digs the back walls alone (0107).
            var behind = Aiming.DigsAt(m, client.Id, _current, at, drill!, _brush, back: true);
            if (Aiming.LeadOf(behind, at, m.Grid.Width) is { } first) Struck(_groundHits, m, first, _current, client.Id);
            foreach (var dig in behind) client.Send(dig);
            return;
        }
        bool wreck = false;
        foreach (var hit in Aiming.HitsOf(m, client.Id, Aiming.Touched(m, _current, at), drill!))
        {
            client.Send(hit);
            wreck = true;
        }
        if (wreck) _drillChop.PlayAt(_figure.Muzzle ?? _figure.GlobalPosition, 1, GroundSounds.HitDbOf(drill!));
        // A creature under the bit is struck at the same pace; how it sounds and flinches is the fauna's (0102).
        foreach (var strike in Aiming.StrikesOf(m, client.Id, Aiming.TouchedCreatures(m, _current, at), drill!)) client.Send(strike);
        var digs = Aiming.DigsAt(m, client.Id, _current, at, drill!, _brush);
        if (Aiming.LeadOf(digs, at, m.Grid.Width) is { } lead) Struck(_groundHits, m, lead, _current, client.Id);
        foreach (var dig in digs) client.Send(dig);
    }

    /// <summary>
    /// The drill's motor heard at its bit as last drawn whenever it <paramref name="runs"/>, spun up and down with its picture
    /// and loaded while it <paramref name="works"/> on a piece (0059), held on <c>Sound.DrillTouchHoldS</c> past the bit leaving
    /// it. Its hits sound as what they hit, a wreck's chop or the ground's (0093), each one as it is sent.
    /// </summary>
    private void HearDrill(bool runs, bool works, double elapsedMs)
    {
        double elapsedS = elapsedMs / 1000;
        bool onPiece = runs && _onPiece.Step(works, elapsedS, S.DrillTouchHoldS);
        var at = _figure.Muzzle ?? _figure.GlobalPosition;
        _drillMotor.Step(runs, onPiece, elapsedS, S.DrillMotor);
        double speed = _drillMotor.Speed(S.DrillMotor);
        int grains = _grains.Due(elapsedMs, _drillMotor.Spin > 0, S.DrillWhirGrainsPerSecond * speed);
        for (int i = 0; i < grains; i++)
        {
            var (spread, _) = StrikeTrain.Vary(_strikeSpread, S.DrillWhirPitch, 0);
            _whir.PlayAt(at, (float)speed * spread, _drillMotor.GainDb(S.DrillMotor));
        }
    }

    /// <summary>The point of every running drill on this surface as last drawn, and the drill: where a wreck it bites is struck (0094).</summary>
    private List<(float X, float Y, string Tool)> Bits(Registry m, int own)
    {
        var bits = new List<(float X, float Y, string Tool)>();
        void Add(int id, Figure figure)
        {
            if (figure.Visible && figure.Muzzle is { } at && m.Items.Running(id) is { } tool && HeldLook.IsDrill(tool)) bits.Add((at.X, at.Y, tool));
        }
        Add(own, _figure);
        foreach (var (id, shown) in _shownOthers) Add(id, shown.Figure);
        return bits;
    }

    /// <summary>
    /// A tool's <paramref name="dig"/> by <paramref name="hero"/> heard and seen where it strikes, as the wall or growth it lands
    /// on sounds and looks hit, whatever the tool (0093, 0094): on the face of it turned toward the hero (<see cref="Impacts.On"/>),
    /// heard on <paramref name="voices"/>. Every hit, a drill's as often as it hits, each throwing its share of the axe's look,
    /// the one that breaks or fells it the whole (<see cref="Impacts.ShareAt"/>), by the damage the sim gives peer
    /// <paramref name="peer"/>'s hit, its module's boost in it (0098).
    /// </summary>
    private static void Struck(Voices voices, Registry m, Sim.Building.Dig dig, Hero hero, int peer)
    {
        var middle = hero.Middle;
        var at = Impacts.On(m.Grid, dig.Cell, (double)middle.X / Hero.Unit, (double)middle.Y / Hero.Unit);
        var point = new Vector2((float)(at.X * Rig.Cell), (float)(at.Y * Rig.Cell));
        int damage = m.Items.Damage(peer, dig.Tool);
        // A back wall sounds and chips as its material, a hull's as a wreck's metal (0107).
        if (dig.Back)
        {
            if (m.Grid.BackAt(dig.Cell) is not { } behind) return;
            voices.Play(S.Ground.TryGetValue(behind.Id, out var sounds) ? sounds.Hit : S.Chop, point, offsetDb: GroundSounds.HitDbOf(dig.Tool));
            var look = HitLooks.Ground.TryGetValue(behind.Id, out var ground) ? ground : Impact.Alloy;
            HitBurst.Shared.Throw(HitLooks.Of[look], point.X, point.Y, at.Nx, at.Ny, Impacts.ShareOf(dig.Tool), Impacts.FloorUnder(m.Grid, at) * Rig.Cell);
            return;
        }
        if (GroundSounds.HitAt(m.Grid, m.Building, dig.Cell, damage) is { } hit)
            voices.Play(hit, point, offsetDb: GroundSounds.HitDbOf(dig.Tool));
        if (Impacts.At(m.Grid, m.Building, dig.Cell) is not { } impact) return;
        double? floor = Impacts.FloorUnder(m.Grid, at) * Rig.Cell;
        HitBurst.Shared.Throw(HitLooks.Of[impact], point.X, point.Y, at.Nx, at.Ny, Impacts.ShareAt(m.Grid, m.Building, dig.Cell, dig.Tool, damage), floor);
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
            ? (a.Cell, a.Work?.Plan, refuse, shown.StructureAt(grid, a.Cell), shown.ValveOpenAt(m.Pipes, a.Cell) switch { null => 0, true => 1, false => 2 }, grid.MachineAt(a.Cell)?.Id, a.Square, back)
            : ((int, int?, bool, int, int, int?, (int, int, int)?, bool)?)null;
        if (traced == _traced) return;
        _traced = traced;
        _outline.Dashed = back;
        // Ground dug at is outlined over the hero and its tool, which may stand in front of it (0077).
        _outline.ZIndex = aim?.Press?.Command is Sim.Building.Dig ? Layers.Dug : Layers.Aim;
        _outline.Begin();
        if (aim != null) AimOutline.Trace(_outline, m, aim, shown, refuse ? AimRefused : AimReady);
        _outline.End();
    }
}
