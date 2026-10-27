using System;
using System.Collections.Generic;
using Stillwake.Content;
using Stillwake.Sim;
using Stillwake.Sim.Creatures;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A shot this player's gun fired, flown here ahead of the sim's by the sim's own step (shots-are-the-sims): where it was a tick
/// ago and is now, in thousandths of a cell, to draw it between, the gun it left and what it has flown on through
/// (2026-10-27-shots-pierce); once ended, the cell it met or last flew in.
/// </summary>
public sealed class OwnShot(Projectile projectile, Gun gun, ShotMotion launched)
{
    public Projectile Projectile { get; } = projectile;
    public Gun Gun { get; } = gun;
    public ShotMotion Was { get; internal set; } = launched;
    public ShotMotion Now { get; internal set; } = launched;
    public IReadOnlyList<ShotPass> Passed { get; internal set; } = [];
    public int? EndCell { get; internal set; }
}

/// <summary>
/// What a frame of the gun in hand did: fired (the sim's <see cref="Sim.Creatures.Fire"/> to send), clicked dry, or came ready again
/// (its cooldown out); for a gun held to charge, how far its charge is (0..1, none while not charging) and whether it came full
/// this frame (2026-10-27-the-railgun).
/// </summary>
public readonly record struct GunStep(Sim.Creatures.Fire? Fire = null, bool Dry = false, bool Ready = false, double Charge = 0, bool Charged = false);

/// <summary>
/// The guns in this player's hand (shots-are-the-sims): a fresh click fires one when the hero's cooldown is out, its gun's
/// <c>Pace</c> as real time, which every gun it holds waits for, as the harpoon's (0044); with no ammo it clicks dry, the
/// cooldown untouched. Where it <see cref="Flies"/> them, a guest's, each shot fired flies here from the muzzle drawn, a tick at
/// a time by the step the sim flies it by, till it meets something or runs out: the shooter sees it at once, a round trip
/// before the sim's, which is never drawn there. A gun held to charge (its <c>Charge</c>, 2026-10-27-the-railgun) begins on a fresh press
/// with the cooldown out and fires only when let go full: let go sooner, put away or no longer held up, it fires nothing.
/// </summary>
public sealed class GunHand
{
    private double _cooldownMs;
    private bool _wasReady = true;
    private readonly List<OwnShot> _shots = [];
    private readonly List<ShotHit> _hits = [];
    /// <summary>The gun being charged and how long it has been held, in ms; none while none is.</summary>
    private Gun? _charging;
    private double _chargedMs;

    /// <summary>This player's shots in flight and those ended since the last frame took them (<see cref="TakeEnded"/>).</summary>
    public IReadOnlyList<OwnShot> Shots => _shots;

    /// <summary>Whether a shot fired is flown here (<see cref="Shared.ShotDraws.FliesOwn"/>): not on the host, which draws the sim's.</summary>
    public bool Flies { get; set; } = true;

    /// <summary>Whether the hero may fire: its cooldown out.</summary>
    public bool Ready => _cooldownMs <= 0;

    /// <summary>A gun's pace in milliseconds: its ticks of the world as real time.</summary>
    public static double PaceMs(Gun gun) => gun.Pace * 1000.0 / Sim.World.TicksPerSecond;

    /// <summary>How long a gun is held to charge, in milliseconds: its <c>Charge</c> ticks as real time; 0 for one fired on the click.</summary>
    public static double ChargeMs(Gun gun) => gun.Charge * 1000.0 / Sim.World.TicksPerSecond;

    /// <summary>
    /// A frame: the cooldown runs down by <paramref name="elapsedMs"/>; a <paramref name="pressed"/> click with <paramref name="gun"/>
    /// wielded and its muzzle drawn at <paramref name="muzzle"/> (thousandths of a cell) fires <paramref name="ammo"/> along
    /// <paramref name="heading"/> if the hero is ready and has some (the sim's <c>AmmoOf</c>, null too for a belt that cannot pay its
    /// charge), else clicks dry. A gun held to charge begins so instead, charges while <paramref name="held"/>, and fires let go full.
    /// Ready again it says so, while the gun is held.
    /// </summary>
    public GunStep Step(double elapsedMs, bool pressed, Gun? gun, (int X, int Y)? muzzle, int heading, Projectile? ammo, bool held = false)
    {
        _cooldownMs = Math.Max(0, _cooldownMs - elapsedMs);
        var step = new GunStep();
        if (_charging != null && (_charging != gun || muzzle == null)) Uncharge();
        if (pressed && gun != null && muzzle != null && Ready && _charging == null)
        {
            if (ammo == null) step = step with { Dry = true };
            else if (gun.Charge > 0) (_charging, _chargedMs) = (gun, 0);
            else step = step with { Fire = Fired(gun, muzzle.Value, heading, ammo) };
        }
        if (_charging is { } charging)
        {
            double full = ChargeMs(charging);
            bool was = _chargedMs >= full;
            if (held) _chargedMs = Math.Min(full, _chargedMs + elapsedMs);
            if (!held)
            {
                if (was && ammo != null) step = step with { Fire = Fired(charging, muzzle!.Value, heading, ammo) };
                Uncharge();
            }
            else step = step with { Charge = full > 0 ? _chargedMs / full : 1, Charged = !was && _chargedMs >= full };
        }
        step = step with { Ready = Ready && !_wasReady && gun != null };
        _wasReady = Ready;
        return step;
    }

    /// <summary>A shot of <paramref name="gun"/> fired from <paramref name="at"/> along <paramref name="heading"/>: the cooldown begun, flown here if this hand flies its shots.</summary>
    private Sim.Creatures.Fire Fired(Gun gun, (int X, int Y) at, int heading, Projectile ammo)
    {
        _cooldownMs = PaceMs(gun);
        if (Flies) _shots.Add(new OwnShot(ammo, gun, ShotFlight.Launch(gun, at.X, at.Y, heading)));
        return new Sim.Creatures.Fire(gun.Item, at.X, at.Y, heading);
    }

    /// <summary>The charge let go of, nothing fired.</summary>
    private void Uncharge() => (_charging, _chargedMs) = (null, 0);

    /// <summary>
    /// A tick of the world: every shot still flying takes its step by <paramref name="step"/> (the sim's,
    /// <see cref="Sim.Creatures.CreaturesModule.StepOf"/>) on a grid <paramref name="width"/> by <paramref name="height"/>, the hits it
    /// would take kept to show (<see cref="TakeHits"/>), through what they would break; one that met something it would not, ran out
    /// or left the grid ends there.
    /// </summary>
    public void Tick(Func<ShotMotion, Projectile, string?, IReadOnlyList<ShotPass>, ShotTick> step, int width, int height)
    {
        foreach (var shot in _shots)
        {
            if (shot.EndCell != null) continue;
            var next = step(shot.Now, shot.Projectile, shot.Gun.Item, shot.Passed);
            (shot.Was, shot.Passed) = (shot.Now, next.Passed);
            _hits.AddRange(next.Hits);
            if (next.Met >= 0) shot.EndCell = next.Met;
            else if (next.Next.Left <= 0 || !ShotFlight.OnGrid(next.Next.X, next.Next.Y, width, height)) shot.EndCell = next.Last;
            else shot.Now = next.Next;
        }
    }

    /// <summary>The shots that ended, taken out.</summary>
    public List<OwnShot> TakeEnded()
    {
        var ended = _shots.FindAll(s => s.EndCell != null);
        _shots.RemoveAll(s => s.EndCell != null);
        return ended;
    }

    /// <summary>The hits this player's shots would have taken since last taken, in order: what the frame shows a hit for.</summary>
    public List<ShotHit> TakeHits()
    {
        var hits = new List<ShotHit>(_hits);
        _hits.Clear();
        return hits;
    }

    /// <summary>Every shot dropped, the cooldown kept: moved to another surface, its grid is another's.</summary>
    public void Drop()
    {
        _shots.Clear();
        _hits.Clear();
        Uncharge();
    }
}
