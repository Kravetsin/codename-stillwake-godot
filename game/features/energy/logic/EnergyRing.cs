using System;
using System.Collections.Generic;
using Spec = Stillwake.Content;

namespace Stillwake.Game.Features.Energy;

/// <summary>
/// What the charge row shows of each open belt slot's store (energy-ring, hud-vitals): what it holds of its own full, gliding
/// between reads. The one an electric tool drains is lit, a glint running back along it; a store put on fills in; when
/// draining passes to the next store a spark hops there. Plain C#: read four times a
/// second, stepped each frame.
/// </summary>
public sealed class EnergyRing
{
    /// <summary>How long a fall glides to its read: one read's span, so a steady drain runs down evenly.</summary>
    public const double GlideS = 0.25;
    /// <summary>How long a store put on takes to fill its piece in, and a spark to hop to the next store.</summary>
    public const double FillInS = 0.55, HopS = 0.35;
    /// <summary>A jump up of this share between two reads is a store put on, filled in rather than glided.</summary>
    public const float PutOn = 0.05f;
    /// <summary>How bright a piece is: the drained one while drawing, while not, and every other.</summary>
    public const float LitDrawing = 1, LitResting = 0.8f, LitOther = 0.4f;

    private float[] _from = [], _to = [], _t = [];
    private double[] _fillIn = [];
    private float[] _lit = [];
    private bool _read;

    /// <summary>How many pieces: the belt's open slots.</summary>
    public int Count => _to.Length;

    /// <summary>The piece an electric tool drains now, or would next; -1 with nothing on the belt to drain.</summary>
    public int Draining { get; private set; } = -1;

    /// <summary>A spark hopping from one piece to the next: its pieces and how far it has gone, 0 to 1; null with none.</summary>
    public (int From, int To, double Gone)? Hop { get; private set; }

    /// <summary>The glint's place along the drained piece, 0 at its end to 1 at its start, while drawing.</summary>
    public double Glint { get; private set; }

    /// <summary>
    /// A read: each open slot's store, its charge and what it holds full in stored units, null for a slot with none; and
    /// <paramref name="draw"/>, what a tool drawing takes a tick in power units, 0 while none runs.
    /// </summary>
    public void Read(IReadOnlyList<(int Charge, int Capacity)?> stores, int draw)
    {
        int n = stores.Count;
        if (!_read || n != Count)
        {
            (_from, _to, _t, _fillIn, _lit) = (new float[n], new float[n], new float[n], new double[n], new float[n]);
            for (int i = 0; i < n; i++) (_from[i], _to[i], _t[i], _fillIn[i]) = (ShareOf(stores[i]), ShareOf(stores[i]), 1, 1);
        }
        int was = Draining;
        Draining = DrainingOf(stores, draw);
        for (int i = 0; i < n; i++)
        {
            float share = ShareOf(stores[i]);
            if (share == _to[i]) continue;
            if (share - _to[i] >= PutOn) (_from[i], _fillIn[i]) = (share, 0);
            else _from[i] = Share(i);
            (_to[i], _t[i]) = (share, 0);
        }
        if (_read && was >= 0 && Draining >= 0 && Draining != was) Hop = (was, Draining, 0);
        if (!_read) for (int i = 0; i < n; i++) _lit[i] = LitOf(i, draw > 0);
        _read = true;
        _drawing = draw;
    }

    private int _drawing;

    /// <summary>Moves what is shown on by <paramref name="seconds"/>.</summary>
    public void Step(double seconds)
    {
        for (int i = 0; i < Count; i++)
        {
            _t[i] = (float)Math.Min(1, _t[i] + seconds / GlideS);
            if (_fillIn[i] < 1) _fillIn[i] = Math.Min(1, _fillIn[i] + seconds / FillInS);
            // The piece a spark hops to lights slowly, so the spark lands before it does.
            double rate = Hop is { } h && h.To == i ? 2.5 : 6;
            _lit[i] += (LitOf(i, _drawing > 0) - _lit[i]) * (float)Math.Min(1, seconds * rate);
        }
        if (Hop is { } hop) Hop = hop.Gone + seconds / HopS >= 1.4 ? null : hop with { Gone = hop.Gone + seconds / HopS };
        if (_drawing > 0) Glint = (Glint + seconds * 1.6 * _drawing) % 1;
    }

    /// <summary>What piece <paramref name="i"/> shows filled, 0 to 1: its store's share, glided, and filling in once put on.</summary>
    public float Share(int i)
    {
        float glided = _from[i] + (_to[i] - _from[i]) * _t[i];
        return glided * (float)(1 - Math.Pow(1 - _fillIn[i], 3));
    }

    /// <summary>Whether piece <paramref name="i"/> is filling in, a store just put on.</summary>
    public bool FillingIn(int i) => _fillIn[i] < 1;

    /// <summary>How far piece <paramref name="i"/> has filled in, 0 as its store is put on to 1.</summary>
    public double FillIn(int i) => _fillIn[i];

    /// <summary>How bright piece <paramref name="i"/> is, <see cref="LitOther"/> to <see cref="LitDrawing"/>.</summary>
    public float Lit(int i) => _lit[i];

    /// <summary>
    /// The store a tool drains: the first in slot order holding a tick of <paramref name="draw"/>, as the sim pays; with none
    /// drawing, the first holding any. -1 with neither.
    /// </summary>
    public static int DrainingOf(IReadOnlyList<(int Charge, int Capacity)?> stores, int draw)
    {
        int least = Math.Max(1, draw * Spec.Charge.Scale);
        for (int i = 0; i < stores.Count; i++)
            if (stores[i] is { } s && s.Charge >= least) return i;
        return -1;
    }

    private float LitOf(int i, bool drawing) => i != Draining ? LitOther : drawing ? LitDrawing : LitResting;

    private static float ShareOf((int Charge, int Capacity)? store) =>
        store is { Capacity: > 0 } s ? Math.Clamp((float)s.Charge / s.Capacity, 0, 1) : 0;
}
