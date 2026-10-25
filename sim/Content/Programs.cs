namespace Stillwake.Content;

/// <summary>A program of the ship's computer: its id, and the machine kind whose module brings it, on the computer's network.</summary>
public sealed record ProgramSpec(string Id, string Module);

/// <summary>
/// The ship's computer's programs, in the order of its buttons: one a module of the ship. The computer shows a button for each
/// once any is brought.
/// </summary>
public static class Programs
{
    public const string Radar = "radar";

    public static readonly IReadOnlyList<ProgramSpec> All = [new(Radar, "radarAntenna")];
}
