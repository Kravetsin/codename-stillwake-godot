namespace Stillwake.Content;

/// <summary>
/// What pipes carry (2026-10-29-typed-ports): a line of spans carries one fluid, the one its ports name. A new fluid is
/// an id here and ports on the machines that take or give it.
/// </summary>
public static class Fluids
{
    public const string Air = "air";
    public const string Water = "water";

    public static readonly IReadOnlyList<string> All = [Air, Water];
}

/// <summary>Which way a port works: it takes its fluid in, or gives it out.</summary>
public enum Way
{
    In,
    Out,
}

/// <summary>A machine's pipe connection: an offset inside its footprint, the way it works and the fluid it carries.</summary>
public sealed record Port(int X, int Y, Way Way, string Fluid);
