using System.Collections.Generic;

namespace Stillwake.Game;

/// <summary>
/// How a planet's land behind its ground is composed (composed-backdrops), drawn by <c>features/backdrop</c>: three layers far
/// to near, each laid from the planet type's own plants and ground pictures by the surface's seed (<c>BackdropPlan</c>). What is
/// the planet's own — its plants, its ground's waves, its soil and stone, its fog — is read where it already is; this is the
/// recipe, one for every planet type until one names its own. Lengths are pixels of a layer at its own scale unless named cells.
/// </summary>
public static class BackdropLooks
{
    /// <summary>
    /// One layer of land. <c>Scale</c>: a world pixel's size there (plants, cells and pictures shrink with distance). <c>Tall</c>:
    /// its drawn height, its land filled on below it by <see cref="Recipe.Below"/>; <c>GroundRow</c>: its row on the ground line.
    /// <c>Follows</c>: its share of the camera's movement. <c>Dark</c> and <c>Haze</c>: how far its colours are mixed toward
    /// <see cref="Recipe.Dark"/>, then toward the planet's fog. <c>Green</c>: how strongly its grassy land is washed toward the
    /// grass's own green in shade; <c>Rows</c>: the rows of grass tops along a grassy crest. <c>Base</c>, <c>Region</c>,
    /// <c>Hill</c>, <c>Rise</c>: its land's height in cells, and shares of the planet's region lift, hill rise and range rise.
    /// <c>Range</c>, <c>Forest</c>: how much of the panorama its stone ranges and forest edges may hold. <c>Lower</c>: at most this many shelves of
    /// land below its crest, fewer once one lies wholly under its height. <c>Wall</c>: its land filled with the front walls' pictures rather than the back walls' darker ones.
    /// </summary>
    public sealed record Depth(
        string Name, double Scale, double Tall, double GroundRow, float Follows, float Dark, float Haze, double Green, int Rows,
        double Base, double Region, double Hill, double Rise, double Range, double Forest, int Lower, bool Wall);

    /// <summary>A range drawn uniformly, from <c>A</c> to <c>B</c>.</summary>
    public readonly record struct Span(double A, double B);

    /// <summary>
    /// Groves: <c>Size</c> trees together, <c>Within</c> a tree's width apart, one of them <c>Big</c>, the rest <c>Other</c>, a
    /// shrub at the first's feet and <c>Feet</c> at the last's; <c>Gap</c> widths to the next, wider where a range rises.
    /// </summary>
    public sealed record Groves(Span Size, Span Within, Span Gap, Span Feet, Span Big, Span Other);

    /// <summary>
    /// Open plain: a lone tree <c>Lone</c> of the time (fewer by a range), <c>Tree</c> big, a shrub at its feet <c>FeetOdds</c> of
    /// the time; else up to <c>ShrubsMost</c> shrubs <c>ShrubOdds</c> of the time; <c>Step</c> widths on.
    /// </summary>
    public sealed record Open(double Lone, Span Step, Span Tree, double FeetOdds, double ShrubOdds, int ShrubsMost);

    /// <summary>
    /// A forest edge: a back row <c>Back</c> big, shifted <c>Shift</c> of a width, and a front row <c>Front</c> big, a shrub at
    /// the feet <c>Feet</c> of the time, <c>Step</c> widths apart over the forest's share, at least <c>Least</c>.
    /// </summary>
    public sealed record Forest(Span Step, Span Back, Span Front, double Feet, double Shift, double Least);

    /// <summary>
    /// Outcrops of small solids where a range meets its foothills: tried every <c>Every</c> widths, kept by <c>Odds</c> × the
    /// range's share × what is left of it, plus <c>Least</c>; <c>Size</c> of them <c>Apart</c> widths apart, <c>Scale</c> big,
    /// each <c>Shrink</c> smaller than the last.
    /// </summary>
    public sealed record Outcrop(Span Size, Span Scale, double Every, double Odds, double Least, double Apart, double Shrink);

    /// <summary>
    /// The shelves below a layer's crest, so its face is never a wide bare wash: each <c>Drop</c> pixels lower than the last,
    /// its height <c>Soil</c> of the crest's and a <c>Wobble</c> in cells over hill waves <c>Wave</c> times as long; each nearer,
    /// so <c>Darken</c> darker and <c>Greener</c> more washed green than the one above. A tree on it <c>TreeOdds</c> of the
    /// time, <c>Tree</c> big, else a shrub <c>Shrub</c> big, <c>Step</c> widths apart from <c>Start</c> widths in.
    /// </summary>
    public sealed record Shelves(
        double Drop, double Soil, double Wobble, double Wave, double Darken, double Greener, double TreeOdds, Span Tree, Span Shrub, Span Step, double Start);

    /// <summary>
    /// The belts along a grassy crest: shrubs every <c>ShrubStep</c> × <c>ShrubWide</c>, shifted up to <c>ShrubJitter</c> of it,
    /// in runs of <c>Run</c> where a run is kept but <c>RunSkip</c> percent and a shrub but <c>ShrubSkip</c>, at
    /// <c>ShrubScales</c>; tufts every <c>TuftStep</c>, kept but <c>TuftSkip</c> percent, <c>TuftScale</c> big; all set
    /// <c>Lift</c> under the crest and down the slope over the grass's rows.
    /// </summary>
    public sealed record Belts(
        double ShrubWide, double ShrubStep, double ShrubJitter, int Run, int RunSkip, int ShrubSkip, IReadOnlyList<double> ShrubScales,
        double TuftStep, int TuftSkip, double TuftScale, double Lift);

    /// <summary>
    /// The recipe. <c>Tile</c>: the panorama's width, repeated across, in world pixels at every layer. <c>Below</c>: how far each
    /// layer's land goes on under its drawn height, so no screen shows past it. <c>Reach</c>: the columns either side of the
    /// camera whose surface rows are averaged into the ground line. <c>TreeTall</c>, <c>ShrubRoom</c>: a plant's role from its
    /// numbers, a tree at <c>Tall</c> this or more, a shrub (not a tree) at <c>Room</c> this or more, a small solid with less
    /// room, cover with none; cover drawn as often as its chance in <c>CoverChance</c>ths. <c>BigWide</c>: the widest plant's width
    /// with no plant to measure. <c>TopVariants</c>, <c>TopRise</c>, <c>TopTall</c>, <c>RowStep</c>: the grass tops along a crest,
    /// their pictures, how far over the crest they start, how tall each is, how far apart their rows. <c>Sink</c>: how deep a part
    /// stands by default. <c>Dark</c>: what near land is darkened toward. <c>Shade</c>: the grass's green in shade, as a share of
    /// it; <c>Grass</c> that green where none can be read. <c>PatternWave</c>, <c>RangeWave</c>: across how many pixels the
    /// patterns and the ranges change. <c>ForestFrom</c>, <c>OpenTo</c>, <c>RangeFrom</c>: where the noise hands over to a forest,
    /// from the open plain, and to a range, eased over the span; <c>RangeBias</c> moves the range's span by how stony the
    /// planet is. <c>RangeSoil</c>, <c>BumpShare</c>: a range's land over its share of the soil's, and the bumps' share on soil;
    /// <c>RangeLeast</c>: a range's height at the lowest of its long wave and of its short one. <c>RangeSparse</c>,
    /// <c>RangeGap</c>: where a range rises, plants that much fewer and groves that many times further apart.
    /// </summary>
    public sealed record Recipe(
        double Tile, double Below, int Reach, IReadOnlyList<Depth> Depths, Groves Groves, Open Open, Forest Forest, Outcrop Outcrop, Shelves Shelves,
        Belts Belts, Span AtFeet, double FeetSpread, int TreeTall, int ShrubRoom, int CoverChance, double BigWide, int TopVariants, double TopRise,
        double TopTall, double RowStep, double Sink, (float R, float G, float B) Dark, double Shade, (int R, int G, int B) Grass,
        double PatternWave, double RangeWave, Span ForestFrom, Span OpenTo, Span RangeFrom, double RangeBias, double RangeSoil, double BumpShare,
        Span RangeLeast, double RangeSparse, double RangeGap);

    /// <summary>
    /// The first planet's, picked on the backdrops' page (composed-backdrops): far grassy ridges with a stone range here and
    /// there, nearer hills with groves and forest edges, and near land with two shelves under its crest.
    /// </summary>
    public static readonly Recipe First = new(
        Tile: 2048, Below: 1024, Reach: 100,
        Depths:
        [
            new("far", Scale: 0.2, Tall: 620, GroundRow: 460, Follows: 0.05f, Dark: 0, Haze: 0.56f, Green: 0.55, Rows: 3,
                Base: 24, Region: 1.1, Hill: 1.4, Rise: 2.2, Range: 1, Forest: 0, Lower: 0, Wall: false),
            new("mid", Scale: 0.34, Tall: 620, GroundRow: 380, Follows: 0.13f, Dark: 0, Haze: 0.34f, Green: 0.6, Rows: 4,
                Base: 16, Region: 0.55, Hill: 1.1, Rise: 1.2, Range: 0.45, Forest: 0.7, Lower: 2, Wall: false),
            new("near", Scale: 0.56, Tall: 800, GroundRow: 380, Follows: 0.3f, Dark: 0.22f, Haze: 0.22f, Green: 0.62, Rows: 4,
                Base: 10, Region: 0.35, Hill: 0.8, Rise: 0, Range: 0, Forest: 1, Lower: 16, Wall: true),
        ],
        Groves: new(Size: new(3, 7), Within: new(0.35, 0.65), Gap: new(1.6, 4.2), Feet: new(1, 3), Big: new(1.05, 1.2), Other: new(0.7, 1)),
        Open: new(Lone: 0.3, Step: new(2.6, 6), Tree: new(0.8, 1.1), FeetOdds: 0.5, ShrubOdds: 0.5, ShrubsMost: 2),
        Forest: new(Step: new(0.42, 0.7), Back: new(0.7, 0.85), Front: new(0.95, 1.15), Feet: 0.7, Shift: 0.3, Least: 0.35),
        Outcrop: new(Size: new(2, 4), Scale: new(1.2, 2.2), Every: 1.5, Odds: 3, Least: 0.04, Apart: 0.22, Shrink: 0.15),
        Shelves: new(Drop: 52, Soil: 0.9, Wobble: 3, Wave: 1.5, Darken: 0.025, Greener: 0.02, TreeOdds: 0.3, Tree: new(0.7, 0.95), Shrub: new(0.8, 1), Step: new(1.4, 3.2), Start: 2),
        Belts: new(ShrubWide: 45, ShrubStep: 0.7, ShrubJitter: 0.4, Run: 5, RunSkip: 35, ShrubSkip: 30, ShrubScales: [0.65, 0.8, 0.95],
            TuftStep: 14, TuftSkip: 45, TuftScale: 0.9, Lift: 2),
        AtFeet: new(0.75, 1), FeetSpread: 0.9,
        TreeTall: 8, ShrubRoom: 3, CoverChance: 25, BigWide: 40,
        TopVariants: 4, TopRise: 16, TopTall: 32, RowStep: 9, Sink: 2,
        Dark: (22 / 255f, 28 / 255f, 24 / 255f), Shade: 0.72, Grass: (84, 112, 62),
        PatternWave: 760, RangeWave: 900, ForestFrom: new(0.56, 0.7), OpenTo: new(0.3, 0.44), RangeFrom: new(0.66, 0.86), RangeBias: 0.4,
        RangeSoil: 0.6, BumpShare: 0.5, RangeLeast: new(0.3, 0.6), RangeSparse: 0.7, RangeGap: 2);
}
