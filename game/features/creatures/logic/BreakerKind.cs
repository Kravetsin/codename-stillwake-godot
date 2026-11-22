namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// A kind of breaker (breakers-look): its painted design, its moves, and the picture that is its body drawn <paramref name="Across"/>
/// pixels wide. Keyed by look in <see cref="CreatureLooks.Breakers"/>; another zone's kind is a line there, its table exported beside the Knot's.
/// </summary>
public sealed record BreakerKind(CreatureArt.BreakerDesign Design, IBreakerMoves Moves, string Body, double Across)
{
    /// <summary>How much its design is drawn for its body to be <see cref="Across"/> wide.</summary>
    public double Fit => Across / (Design.Parts[Body].W * Design.Sizes[Body]);
}
