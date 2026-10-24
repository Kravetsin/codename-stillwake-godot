namespace Stillwake.Game;

public static partial class Sound
{
    public static readonly Cue Hiss = Looped("atmosphere/hiss", -8, new(6, 24));
    public static readonly Cue VentHum = Looped("atmosphere/vent-hum", -12, new(3, 10));
}
