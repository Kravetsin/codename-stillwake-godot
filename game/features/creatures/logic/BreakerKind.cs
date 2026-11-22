namespace Stillwake.Game.Features.Creatures;

/// <summary>
/// A kind of breaker (breakers-look): its painted design and its moves, a pack all of one. Keyed by look in
/// <see cref="CreatureLooks.Breakers"/>; another zone's kind is a line there, its table exported beside the Knot's.
/// </summary>
public sealed record BreakerKind(CreatureArt.BreakerDesign Design, IBreakerMoves Moves);
