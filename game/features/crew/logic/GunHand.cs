using System;
using System.Collections.Generic;
using Stillwake.Content;
using Stillwake.Sim;

namespace Stillwake.Game.Features.Crew;

/// <summary>
/// A shot this player's gun fired, flown here ahead of the sim's by the sim's own step (shots-are-the-sims): where it was a tick
/// ago and is now, in thousandths of a cell, to draw it between, and the gun it left; once ended, the cell it met or last flew in,
/// and whether it met something there.
/// </summary>
public sealed class OwnShot(Projectile projectile, Gun gun, ShotMotion launched)
{
    public Projectile Projectile { get; } = projectile;
    public Gun Gun { get; } = gun;
    public ShotMotion Was { get; internal set; } = launched;
    public ShotMotion Now { get; internal set; } = launched;
    public int? EndCell { get; internal set; }
    public bool Met { get; internal set; }
}

/// <summary>What a frame of the gun in hand did: fired (the sim's <see cref="Sim.Creatures.Fire"/> to send), clicked dry, or came ready again.</summary>
public readonly record struct GunStep(Sim.Creatures.Fire? Fire = null, bool Dry = false, bool Ready = false);

/// <summary>
/// The guns in this player's hand (shots-are-the-sims): a fresh click fires one when the hero's cooldown is out, its gun's
/// <c>Pace</c> as real time, which every gun it holds waits for, as the harpoon's (0044); with no ammo it clicks dry, the
/// cooldown untouched. Each shot fired flies here from the muzzle drawn, a tick at a time by the step the sim flies it by, till
/// it meets something or runs out: the shooter sees it at once, a round trip before the sim's, which is never drawn here.
/// </summary>
public sealed class GunHand
{
    private double _cooldownMs;
    private bool _wasReady = true;
    private readonly List<OwnShot> _shots = [];

    /// <summary>This player's shots in flight and those ended since the last frame took them (<see cref="TakeEnded"/>).</summary>
    public IReadOnlyList<OwnShot> Shots => _shots;

    /// <summary>Whether the hero may fire: its cooldown out.</summary>
    public bool Ready => _cooldownMs <= 0;

    /// <summary>A gun's pace in milliseconds: its ticks of the world as real time.</summary>
    public static double PaceMs(Gun gun) => gun.Pace * 1000.0 / Sim.World.TicksPerSecond;

    /// <summary>
    /// A frame: the cooldown runs down by <paramref name="elapsedMs"/>; a <paramref name="pressed"/> click with <paramref name="gun"/>
    /// wielded and its muzzle drawn at <paramref name="muzzle"/> (thousandths of a cell) fires <paramref name="ammo"/> along
    /// <paramref name="heading"/> if the hero is ready and has some (the sim's <c>AmmoOf</c>), else clicks dry. Ready again it says so,
    /// while the gun is held.
    /// </summary>
    public GunStep Step(double elapsedMs, bool pressed, Gun? gun, (int X, int Y)? muzzle, int heading, Projectile? ammo)
    {
        _cooldownMs = Math.Max(0, _cooldownMs - elapsedMs);
        var step = new GunStep();
        if (pressed && gun != null && muzzle is { } at && Ready)
        {
            if (ammo == null) step = step with { Dry = true };
            else
            {
                _cooldownMs = PaceMs(gun);
                _shots.Add(new OwnShot(ammo, gun, ShotFlight.Launch(gun, at.X, at.Y, heading)));
                step = step with { Fire = new Sim.Creatures.Fire(gun.Item, at.X, at.Y, heading) };
            }
        }
        step = step with { Ready = Ready && !_wasReady && gun != null };
        _wasReady = Ready;
        return step;
    }

    /// <summary>
    /// A tick of the world: every shot still flying takes its step by <paramref name="step"/> (the sim's,
    /// <see cref="Sim.Creatures.CreaturesModule.StepOf"/>) on a grid <paramref name="width"/> by <paramref name="height"/>; one that
    /// met something, ran out or left the grid ends there.
    /// </summary>
    public void Tick(Func<ShotMotion, Projectile, (ShotMotion Next, int Met, int Last)> step, int width, int height)
    {
        foreach (var shot in _shots)
        {
            if (shot.EndCell != null) continue;
            var (next, met, last) = step(shot.Now, shot.Projectile);
            shot.Was = shot.Now;
            if (met >= 0) (shot.EndCell, shot.Met) = (met, true);
            else if (next.Left <= 0 || !ShotFlight.OnGrid(next.X, next.Y, width, height)) shot.EndCell = last;
            else shot.Now = next;
        }
    }

    /// <summary>The shots that ended, taken out: what the frame shows an end for.</summary>
    public List<OwnShot> TakeEnded()
    {
        var ended = _shots.FindAll(s => s.EndCell != null);
        _shots.RemoveAll(s => s.EndCell != null);
        return ended;
    }

    /// <summary>Every shot dropped, the cooldown kept: moved to another surface, its grid is another's.</summary>
    public void Drop() => _shots.Clear();
}
