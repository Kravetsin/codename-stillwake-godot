using System;
using System.Collections.Generic;
using Godot;
using Stillwake.Game.Render;
using Stillwake.Game.Shared;
using Stillwake.Sim.Creatures;
using Stillwake.Sim.Debris;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// The nests (companion-fights, breakers-look): a pair of each at work on its piece in view, rocking with its picture, the rest unseen
/// under it (<see cref="BreakerPack"/>). One gone is kept a moment for the bodies of its pack to fly out of. Drawn from a pool that never frees.
/// </summary>
public partial class SleepingNests : Node2D
{
    private const float Cell = ShipCamera.Cell;
    private const int Unit = CreaturesModule.Unit;

    /// <summary>How long a pack gone waits for its bodies to be seen.</summary>
    private const double LeftS = 0.5;

    /// <summary>One at work as last drawn.</summary>
    public readonly record struct AtWork(Vector2 At, float Angle, double Clock);

    /// <summary>A nest as last drawn: what its pack flies out of, when it went.</summary>
    public sealed class Pack
    {
        public int Piece;
        public string Species = "";
        public Vector2 Middle, Half;
        public readonly AtWork[] Workers = new AtWork[BreakerPack.AtWork];
        public int Count;
        public double Seen, WentAt;
    }

    private readonly List<BreakerSprite> _working = [];
    private readonly Dictionary<int, Pack> _packs = new();
    private readonly List<Pack> _left = [];
    private readonly List<int> _gone = [];
    private double _seconds;

    /// <summary>The packs that went less than <see cref="LeftS"/> ago, for <see cref="BreakerBodies"/> to take.</summary>
    public List<Pack> Left => _left;

    public double Seconds => _seconds;

    /// <summary>Shows the nests of <paramref name="creatures"/> on their pieces within <paramref name="view"/> (cells), drawn <paramref name="drift"/> ticks on and turned by <paramref name="turns"/>.</summary>
    public void Frame(CreaturesModule creatures, IDebrisQueries debris, IReadOnlyDictionary<int, double> turns, Rect2 view, double drift, double elapsedMs)
    {
        _seconds += Math.Max(0, elapsedMs / 1000);
        int shown = 0;
        var nests = creatures.Nests;
        var near = view.Grow(8);
        for (int i = 0; i < nests.Count; i++)
        {
            if (debris.PieceById(nests[i].Piece) is not { } piece) continue;
            var (x, y) = PieceDrawn.At(piece, drift);
            var at = new Vector2((float)(x / Unit), (float)(y / Unit));
            if (!near.HasPoint(at) || Spec.Wreckage.SizeOf(piece.Item) is not var (w, h)) continue;
            if (creatures.SleeperOn(piece) is not { } species || !CreatureLooks.Breakers.TryGetValue(Spec.Creatures.Get(species).Look, out var kind)) continue;
            if (!_packs.TryGetValue(piece.Id, out var pack)) _packs[piece.Id] = pack = new Pack { Piece = piece.Id };
            (pack.Species, pack.Middle, pack.Half, pack.Seen) = (species, at * Cell, new Vector2(w, h) * Cell / 2, _seconds);
            pack.Count = Math.Min(BreakerPack.AtWork, nests[i].Size);
            float rock = (float)turns.GetValueOrDefault(piece.Id);
            for (int k = 0; k < pack.Count; k++)
            {
                var worker = BreakerPack.WorkerOf(piece.Id, k, pack.Half.X, pack.Half.Y);
                var seen = new AtWork(pack.Middle + new Vector2((float)worker.X, (float)worker.Y).Rotated(rock), rock + (float)worker.Angle, worker.At(_seconds));
                pack.Workers[k] = seen;
                var one = Working(shown++, kind);
                one.Show(new BreakerPose(seen.Clock, Work: 1, Lamp: 0.8 + 0.2 * Math.Sin(seen.Clock * 3), Tint: BreakerMotion.Calm), 0);
                one.Lies(seen.At, 0, seen.Angle);
                one.Visible = true;
            }
        }
        for (int i = shown; i < _working.Count; i++) _working[i].Visible = false;
        Went(creatures);
    }

    /// <summary>The packs no longer drawn: those whose nest is gone left for their bodies, the rest out of view and forgotten.</summary>
    private void Went(CreaturesModule creatures)
    {
        _gone.Clear();
        foreach (var (piece, pack) in _packs)
        {
            if (pack.Seen >= _seconds) continue;
            _gone.Add(piece);
            if (Nested(creatures, piece)) continue;
            pack.WentAt = _seconds;
            _left.Add(pack);
        }
        foreach (int piece in _gone) _packs.Remove(piece);
        for (int i = _left.Count - 1; i >= 0; i--)
            if (_seconds - _left[i].WentAt > LeftS) _left.RemoveAt(i);
    }

    private static bool Nested(CreaturesModule creatures, int piece)
    {
        var nests = creatures.Nests;
        for (int i = 0; i < nests.Count; i++)
            if (nests[i].Piece == piece) return true;
        return false;
    }

    /// <summary>Another surface, or a load: nobody flies out of another world's nest.</summary>
    public void Reset()
    {
        _packs.Clear();
        _left.Clear();
    }

    private BreakerSprite Working(int at, BreakerKind kind)
    {
        if (at < _working.Count && _working[at].Kind == kind) return _working[at];
        var one = new BreakerSprite(kind);
        AddChild(one);
        if (at == _working.Count) _working.Add(one);
        else
        {
            _working[at].Free();
            _working[at] = one;
        }
        return one;
    }
}
