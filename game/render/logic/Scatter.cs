using System;
using System.Collections.Generic;

namespace Stillwake.Game.Render;

/// <summary>
/// A thing scattered over a plane of the backdrop: its tile, its place in the tile in the plane's
/// pixels, and a hash of its own that draws the rest of it (<see cref="Scatter.Next"/>).
/// </summary>
public readonly record struct Scattered(int TileX, int TileY, float X, float Y, uint Hash);

/// <summary>
/// Where the backdrop's things lie (render/README.md): each tile of a plane holds what a hash of its seed
/// and the tile's coordinates says, and nothing else decides it, so panning away and back shows the same
/// things and every peer sees the same. Plain C#, so it is tested without Godot.
/// </summary>
public static class Scatter
{
    /// <summary>The hash of a seed, a tile and an index within it.</summary>
    public static uint Hash(uint seed, int x, int y, int i)
    {
        unchecked
        {
            uint h = Mix(seed * 0x9E3779B1u ^ (uint)x * 0x85EBCA77u);
            h = Mix(h ^ (uint)y * 0xC2B2AE3Du);
            return Mix(h ^ (uint)i * 0x27D4EB2Fu);
        }
    }

    /// <summary>A number in [0, 1) from <paramref name="hash"/>, which moves on to the next.</summary>
    public static float Next(ref uint hash)
    {
        float unit = Unit(hash);
        hash = Mix(unchecked(hash + 0x9E3779B9u));
        return unit;
    }

    private static float Unit(uint hash) => (hash >> 8) * (1f / (1 << 24));

    private static uint Mix(uint h)
    {
        unchecked
        {
            h ^= h >> 16;
            h *= 0x7FEB352Du;
            h ^= h >> 15;
            h *= 0x846CA68Bu;
            h ^= h >> 16;
            return h;
        }
    }

    /// <summary>How many things tile (<paramref name="x"/>, <paramref name="y"/>) holds: <paramref name="mean"/> rounded down or up, up as often as its fraction says.</summary>
    public static int CountIn(uint seed, int x, int y, float mean)
    {
        uint h = Hash(seed, x, y, -1);
        return (int)(mean + Next(ref h));
    }

    /// <summary>
    /// What the tiles <paramref name="tiles"/> hold, row by row, into <paramref name="into"/>: each
    /// tile <paramref name="tile"/> pixels wide, <paramref name="mean"/> things in it on average.
    /// </summary>
    public static void Over(uint seed, int tile, float mean, (int X0, int Y0, int X1, int Y1) tiles, List<Scattered> into)
    {
        for (int y = tiles.Y0; y <= tiles.Y1; y++)
            for (int x = tiles.X0; x <= tiles.X1; x++)
            {
                int count = CountIn(seed, x, y, mean);
                for (int i = 0; i < count; i++)
                {
                    uint h = Hash(seed, x, y, i);
                    float px = Next(ref h) * tile, py = Next(ref h) * tile;
                    into.Add(new Scattered(x, y, px, py, h));
                }
            }
    }

    /// <summary>The tiles, first and last each way, that a rectangle of the plane touches.</summary>
    public static (int X0, int Y0, int X1, int Y1) Tiles(double left, double top, double right, double bottom, int tile) =>
        ((int)Math.Floor(left / tile), (int)Math.Floor(top / tile), (int)Math.Floor(right / tile), (int)Math.Floor(bottom / tile));

    /// <summary>
    /// The point of a plane at the middle of the screen: the camera's centre, in pixels of the world,
    /// times the share of its movement the plane follows, carried right by its drift so what lies on it
    /// moves left.
    /// </summary>
    public static (double X, double Y) Centre(double cameraX, double cameraY, float parallax, float drift, double seconds) =>
        (cameraX * parallax + drift * seconds, cameraY * parallax);

    /// <summary>Where a point of a plane falls on the screen, a pixel of the plane <paramref name="zoom"/> of the screen's.</summary>
    public static (double X, double Y) OnScreen(double x, double y, (double X, double Y) centre, float zoom, float screenW, float screenH) =>
        (screenW / 2 + (x - centre.X) * zoom, screenH / 2 + (y - centre.Y) * zoom);
}
