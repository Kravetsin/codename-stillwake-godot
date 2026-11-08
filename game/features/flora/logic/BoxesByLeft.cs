using System;
using System.Collections.Generic;

namespace Stillwake.Game.Features.Flora;

/// <summary>
/// Boxes on a planet, each with what it carries, found by a box they meet without walking them all (planet-frame-rate): kept sorted
/// by their left edge, a search starts the widest box's width left of the one it is given. Plain C#.
/// </summary>
public sealed class BoxesByLeft<T>
{
    private readonly List<(float Left, float Top, float Right, float Bottom, int Key, T Item)> _boxes = [];
    private readonly Dictionary<int, float> _lefts = new();
    private float _widest;

    /// <summary>How many it holds.</summary>
    public int Count => _boxes.Count;

    /// <summary>Adds box <paramref name="key"/>, from (<paramref name="left"/>, <paramref name="top"/>) to (<paramref name="right"/>, <paramref name="bottom"/>).</summary>
    public void Add(int key, float left, float top, float right, float bottom, T item)
    {
        if (!_lefts.TryAdd(key, left)) throw new ArgumentException($"box {key} is held already", nameof(key));
        _boxes.Insert(From(left), (left, top, right, bottom, key, item));
        _widest = Math.Max(_widest, right - left);
    }

    /// <summary>Takes box <paramref name="key"/> away; false if none is held.</summary>
    public bool Remove(int key)
    {
        if (!_lefts.Remove(key, out float left)) return false;
        for (int i = From(left); ; i++)
            if (_boxes[i].Key == key)
            {
                _boxes.RemoveAt(i);
                return true;
            }
    }

    /// <summary>Adds to <paramref name="into"/> what every box overlapping the one given carries, borders that only touch not counted.</summary>
    public void Meeting(float left, float top, float right, float bottom, List<T> into)
    {
        for (int i = From(left - _widest); i < _boxes.Count && _boxes[i].Left < right; i++)
        {
            var b = _boxes[i];
            if (b.Right > left && b.Bottom > top && b.Top < bottom) into.Add(b.Item);
        }
    }

    /// <summary>The first box whose left edge is at or right of <paramref name="left"/>.</summary>
    private int From(float left)
    {
        int lo = 0, hi = _boxes.Count;
        while (lo < hi)
        {
            int mid = (lo + hi) / 2;
            if (_boxes[mid].Left < left) lo = mid + 1;
            else hi = mid;
        }
        return lo;
    }
}
