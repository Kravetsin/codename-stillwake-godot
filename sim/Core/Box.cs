using System.Text.Json.Serialization;

namespace Stillwake.Sim;

/// <summary>A rectangle of cells, its edges included; <see cref="None"/> holds none.</summary>
public readonly record struct Box(int Left, int Top, int Right, int Bottom)
{
    public static readonly Box None = new(0, 0, -1, -1);
    [JsonIgnore] public bool IsEmpty => Right < Left || Bottom < Top;
    [JsonIgnore] public int Width => IsEmpty ? 0 : Right - Left + 1;
    [JsonIgnore] public int Height => IsEmpty ? 0 : Bottom - Top + 1;
    public bool Contains(int x, int y) => x >= Left && x <= Right && y >= Top && y <= Bottom;

    /// <summary>The smallest box holding both.</summary>
    public Box Union(Box other) =>
        IsEmpty ? other
        : other.IsEmpty ? this
        : new(Math.Min(Left, other.Left), Math.Min(Top, other.Top), Math.Max(Right, other.Right), Math.Max(Bottom, other.Bottom));
}
