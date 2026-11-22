using System;
using I = Stillwake.Game.Shared.DroneInk;

namespace Stillwake.Game.Shared;

/// <summary>A colour of light, each channel 0..1.</summary>
public readonly record struct Glint(float R, float G, float B)
{
    /// <summary>How bright it is to the eye, 0..1.</summary>
    public float Brightness => 0.2126f * R + 0.7152f * G + 0.0722f * B;

    public static Glint Of(int rgb) => new(((rgb >> 16) & 0xff) / 255f, ((rgb >> 8) & 0xff) / 255f, (rgb & 0xff) / 255f);

    public static Glint Of((int R, int G, int B) c) => new(c.R / 255f, c.G / 255f, c.B / 255f);

    /// <summary>This, <paramref name="share"/> of the way to <paramref name="to"/>.</summary>
    public Glint Toward(Glint to, float share) => new(R + (to.R - R) * share, G + (to.G - G) * share, B + (to.B - B) * share);

    /// <summary>This, <paramref name="share"/> of the way to white.</summary>
    public Glint Paled(float share) => Toward(new Glint(1, 1, 1), share);
}

/// <summary>
/// A player's colour in the drone's embers, the pick «С облачком» (drone-salts): the coal, its cracks and eye stay ember; what leaves
/// the coal burns through a pale flame into the player's colour. The one rule for the drone, its windows' corners, its icons and what
/// it draws for its player. An alarm turns all of it red, as bright and as pale as the red is drawn bare. Client only.
/// </summary>
public readonly record struct Salts
{
    private static readonly Glint Coals = Glint.Of(I.Ember), Alarm = Glint.Of(I.Red), Flame = Coals.Paled(I.Cream);

    private readonly Glint _colour;
    private readonly float _room, _gain, _red;

    private Salts(Glint colour)
    {
        float bright = colour.Brightness, ember = Coals.Brightness;
        _colour = colour;
        _room = bright <= ember ? 1 : Math.Clamp(1 - (bright - ember) * I.RoomFall, I.RoomLeast, 1);
        _gain = bright <= 0 ? I.GainMost : Math.Clamp(MathF.Sqrt(ember / bright), I.GainLeast, I.GainMost);
    }

    /// <summary>The salts of player colour <paramref name="rgb"/>, 0xRRGGBB.</summary>
    public static Salts Of(int rgb) => new(Glint.Of(rgb));

    /// <summary>The ember for a colour: what a drone burns until it is told whose it is.</summary>
    public static Salts Embers { get; } = new(Coals);

    /// <summary>These, <paramref name="red"/> of the way an alarm's.</summary>
    public Salts Reddened(float red) => red <= 0 ? this : new Salts(_colour, _room, _gain, Math.Min(1, red));

    private Salts(Glint colour, float room, float gain, float red) => (_colour, _room, _gain, _red) = (colour, room, gain, red);

    /// <summary>How much brighter its light is drawn, so a dark colour holds beside a light one.</summary>
    public float Gain => Lift(_gain);

    /// <summary>How much of a paleness it takes, so a light colour does not turn white.</summary>
    public float Room => Lift(_room);

    /// <summary>The colour cloud's alpha round the drone, gone as an alarm reddens it.</summary>
    public float Cloud => I.CloudAlpha * _gain * (1 - _red);

    /// <summary>A multiple of the bare ember's drawing as it applies now: none of it at a full alarm.</summary>
    public float Lift(float times) => times + (1 - times) * _red;

    /// <summary>The coal, its cracks and eye, <paramref name="pale"/> toward white.</summary>
    public Glint Coal(float pale = 0) => Coals.Toward(Alarm, _red).Paled(pale);

    /// <summary>A line of the player's colour, <paramref name="pale"/> toward white as far as the colour has room.</summary>
    public Glint Line(float pale = 0) => _colour.Toward(Alarm, _red).Paled(pale * Room);

    /// <summary>A spark or thread <paramref name="burn"/> of the way out of the coal (0 ember, 1 the player's).</summary>
    public Glint Spark(float burn, float pale = 0) => Burning(burn).Toward(Alarm, _red).Paled(pale * Room);

    /// <summary>A piece of an icon's run, paled the same for every colour.</summary>
    public Glint Run(float burn, float pale) => Burning(burn).Toward(Alarm, _red).Paled(pale * I.RunPale);

    private Glint Burning(float burn) =>
        burn < I.BurnEmber ? Coals
        : burn < I.BurnCream ? Coals.Toward(Flame, (burn - I.BurnEmber) / (I.BurnCream - I.BurnEmber))
        : Flame.Toward(_colour, Smooth((burn - I.BurnCream) / (I.BurnSalt - I.BurnCream)));

    /// <summary>How far out of the coal a point is, 0..1, by its place in pick px from the core.</summary>
    public static float OutOf(float x, float y) => Smooth((MathF.Sqrt(x * x + y * y) - I.OutFrom) / I.OutOver);

    /// <summary>
    /// How burnt body spark <paramref name="i"/> is at <paramref name="seconds"/>: by how far out it is (<paramref name="x"/>,
    /// <paramref name="y"/>), and on a shape by how long ago it was born, whichever is less.
    /// </summary>
    public static float SparkBurn(int i, double seconds, float x, float y, bool shaped)
    {
        float burnt = OutOf(x, y);
        return shaped ? MathF.Min(burnt, Born(seconds * I.SparkBorn + Hash(i + I.SparkBornLater), I.SparkBornIn)) : burnt;
    }

    /// <summary>How burnt gauge dot <paramref name="j"/> is, <paramref name="away"/> from its mark: by that, and by how long ago it was born.</summary>
    public static float DotBurn(int j, double seconds, float away) =>
        MathF.Min(away, Born(seconds * I.DotBorn + Hash(j + I.DotBornLater), I.DotBornIn));

    /// <summary>How burnt an icon run's piece is: <paramref name="along"/> the thread, <paramref name="toHead"/> of the way to its head.</summary>
    public static float RunBurn(float along, float toHead) => along * I.RunAlong + (1 - toHead) * I.RunBehind;

    private static float Born(double lives, float over) => Smooth((float)(lives - Math.Floor(lives)) / over);

    private static double Hash(double i)
    {
        double s = Math.Sin(i * 127.1 + 311.7) * 43758.5453;
        return s - Math.Floor(s);
    }

    private static float Smooth(float u)
    {
        u = Math.Clamp(u, 0, 1);
        return u * u * (3 - 2 * u);
    }
}
