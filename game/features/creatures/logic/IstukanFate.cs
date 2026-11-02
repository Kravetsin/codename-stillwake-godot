namespace Stillwake.Game.Features.Creatures;

/// <summary>Why an Istukan is no longer in the world, read from what it leaves behind: a kill fells its post and leaves its wall.</summary>
public static class IstukanFate
{
    public enum Fate
    {
        /// <summary>Both stand: with no hero in its arena it went, to come back whole. Nothing to see.</summary>
        Calmed,

        /// <summary>Its post felled with the wall standing.</summary>
        Killed,

        /// <summary>Its relic taken, or the wall of its post dug out.</summary>
        ShutDown,
    }

    /// <summary>The fate of one gone with its relic <paramref name="taken"/> or not, its post standing or not, its post's wall standing or not.</summary>
    public static Fate Of(bool taken, bool postStands, bool wallStands) =>
        taken || !wallStands ? Fate.ShutDown : postStands ? Fate.Calmed : Fate.Killed;
}
