// HeroView's guns (shots-are-the-sims): the nailer and every gun after it, fired at the pointer, its shots flown here ahead of the sim's.
using System.Linq;
using Godot;
using Stillwake.Game.Features.Sound;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Net;
using Stillwake.Sim;
using S = Stillwake.Game.Sound;

namespace Stillwake.Game.Features.Crew;

public partial class HeroView
{
    /// <summary>This hero's guns: the cooldown every gun it holds waits for, and its shots in flight as flown here.</summary>
    private readonly GunHand _guns = new();

    /// <summary>This player's own shots, over the heroes, drawn as flown here: the sim's of its hero are never drawn.</summary>
    private readonly ShotStreaks _ownShots = new() { ZIndex = Layers.Hero };

    private readonly AudioStreamPlayer _nailShot = Cues.Player(S.NailShot, S.Suit), _dryClick = Cues.Player(S.DryClick, S.Suit),
        _nailReady = Cues.Player(S.NailReady, S.Suit);
    private readonly Voices _ownShotEnds = new(S.Suit, voices: 3);

    /// <summary>The world's tick this hero's own shots were last flown to.</summary>
    private int _gunTick = -1;

    /// <summary>The gun this hand holds, a gun of <c>Content.Shots</c>; none for anything else or an empty hand.</summary>
    private Content.Gun? GunInHand(bool handFree) => handFree ? Content.Shots.GunOf(_ui.Wielded.Get()) : null;

    /// <summary>
    /// The gun in hand, <paramref name="wields"/>, aimed as the figure was last drawn: a fresh <paramref name="clicked"/> fires from its
    /// muzzle along its barrel when the hero is ready (<see cref="GunHand"/>), sent as the sim's <c>Fire</c> and flown here at once;
    /// with no ammo it clicks dry; with the muzzle off the grid it does nothing. Every tick of the world flies this player's shots a
    /// step by the sim's own (<c>CreaturesModule.StepOf</c>), and one that ends is shown ending as what it met (<see cref="ShotEnds"/>).
    /// </summary>
    private void Shoot(Client client, Registry m, Content.Gun? gun, bool wields, bool clicked, double elapsedMs)
    {
        (int X, int Y)? muzzle = null;
        int heading = 0;
        Content.Projectile? ammo = null;
        if (wields && gun != null && _figure.Muzzle is { } at)
        {
            var (x, y) = Thousandths(at);
            var from = new Point((int)x, (int)y);
            heading = Aiming.HeadingOf(from, new Point(from.X + (int)(_figure.Barrel.X * Hero.Unit), from.Y + (int)(_figure.Barrel.Y * Hero.Unit)));
            string? problem = m.Creatures.FireProblem(client.Id, gun.Item, from.X, from.Y);
            if (problem is null or "items.notEnough") (muzzle, ammo) = ((from.X, from.Y), m.Creatures.AmmoOf(client.Id, gun));
        }
        var step = _guns.Step(elapsedMs, clicked && wields, wields ? gun : null, muzzle, heading, ammo);
        if (step.Fire is { } fire)
        {
            client.Send(fire);
            _nailShot.Play();
            Puff(_figure);
        }
        if (step.Dry) _dryClick.Play();
        if (step.Ready) _nailReady.Play();
        int tick = client.World.Tick;
        if (_gunTick < 0 || _gunTick > tick) _gunTick = tick;
        for (; _gunTick < tick; _gunTick++) _guns.Tick(m.Creatures.StepOf, m.Grid.Width, m.Grid.Height);
        foreach (var ended in _guns.TakeEnded().Where(e => e.Met))
            ShotEnds.Show(m, _ownShotEnds, ended.Gun.Item, ended.Projectile.Id, ended.EndCell!.Value, (ended.Was.X / 1000.0, ended.Was.Y / 1000.0));
        _ownShots.Show(_guns.Shots.Select(s =>
        {
            var (point, way) = ShotStreaks.Between(s.Was, s.Now, _progress);
            return (point, way, ShotLooks.Of(s.Projectile.Id));
        }));
    }
}
