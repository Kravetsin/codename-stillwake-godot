namespace Stillwake.Content;

/// <summary>
/// The size of every new world, in cells, and of the build zone centred in it (0048): the ship is built
/// only inside the zone; the space around it is there to fly out into.
/// </summary>
public static class World
{
    public const int Width = 2048;
    public const int Height = 1024;
    public const int ZoneWidth = 1024;
    public const int ZoneHeight = 512;
}
