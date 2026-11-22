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
/// The nests asleep (companion-fights): three of its sleepers folded on each nest's piece in view, rocking with its picture, their eyes
/// nearly out. A stand-in till the breaker's pick page. Drawn from a pool that never frees.
/// </summary>
public partial class SleepingNests : Node2D
{
    private const float Cell = ShipCamera.Cell;
    private const int Unit = CreaturesModule.Unit;

    private static readonly float Middle = (float)-CreatureArt.InkTick.Centre.Y;

    // Where the three lie in shares of their piece's half box, how each is turned and which way it faces.
    private static readonly (float X, float Y, float Turn, int Face)[] Lie = [(-0.45f, -0.35f, -0.5f, -1), (0.4f, -0.1f, 0.35f, 1), (-0.05f, 0.4f, 0.15f, 1)];

    private sealed partial class Folded : PosedSprite
    {
        public Folded() : base(Middle)
        {
            var rig = new TickRig();
            rig.Place(new TickArt.Pose(1, 0, 0, 1, TickMotion.Watching, 0.25, 0));
            var ink = new InkRig(CreatureArt.InkTick.Id, CreatureArt.InkTick.Parts) { Position = new Vector2(0, Middle) };
            AddChild(ink);
            ink.Show(rig.Slots, 0, 0);
        }

        public void Lies(Vector2 middle, float turn, int face, float size) => Place(middle + new Vector2(0, Middle * size), 0, turn, face, size);
    }

    private readonly List<Folded> _folded = [];

    /// <summary>Shows the nests of <paramref name="creatures"/> on their pieces within <paramref name="view"/> (cells), drawn <paramref name="drift"/> ticks on and turned by <paramref name="turns"/>.</summary>
    public void Frame(CreaturesModule creatures, IDebrisQueries debris, IReadOnlyDictionary<int, double> turns, Rect2 view, double drift)
    {
        int shown = 0;
        var nests = creatures.Nests;
        var near = view.Grow(8);
        for (int i = 0; i < nests.Count; i++)
        {
            if (debris.PieceById(nests[i].Piece) is not { } piece) continue;
            var (x, y) = PieceDrawn.At(piece, drift);
            var at = new Vector2((float)(x / Unit), (float)(y / Unit));
            if (!near.HasPoint(at) || Spec.Wreckage.SizeOf(piece.Item) is not var (w, h)) continue;
            float rock = (float)turns.GetValueOrDefault(piece.Id);
            if (creatures.SleeperOn(piece) is not { } species) continue;
            var look = CreatureLooks.Ticks[Spec.Creatures.Get(species).Look];
            foreach (var lie in Lie)
            {
                if (shown == _folded.Count) Made();
                var one = _folded[shown++];
                var off = new Vector2(lie.X * w, lie.Y * h) * 0.5f;
                one.Lies((at + off.Rotated(rock)) * Cell, rock + lie.Turn, lie.Face, look.Size);
                one.Visible = true;
            }
        }
        for (int i = shown; i < _folded.Count; i++) _folded[i].Visible = false;
    }

    private void Made()
    {
        var one = new Folded();
        AddChild(one);
        _folded.Add(one);
    }
}
