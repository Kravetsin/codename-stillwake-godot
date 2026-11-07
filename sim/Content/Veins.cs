namespace Stillwake.Content;

/// <summary>
/// A vein of ore in a planet's rock: cells of its <see cref="Material"/> in place of stone, along a thin winding line where its
/// noise, swelling every <see cref="Wave"/> columns and <see cref="Tall"/> rows, crosses its middle, and only in its planet
/// type's lodes (<see cref="PlanetType.LodeWave"/>), so veins come in rich stretches of rock with bare rock between; and in
/// <see cref="Pockets"/> scattered through all of the rock, lodes or not.
/// </summary>
/// <param name="Material">A natural material run through stone (<see cref="Content.Material.In"/>), dug for its ore.</param>
/// <param name="Widths">
/// How near its noise's middle a cell is in the vein, in each depth band (<see cref="PlanetType.Bands"/>), the coldest first, at
/// a lode's heart; tapering to nothing at its edge. 0 where it runs in no band.
/// </param>
/// <param name="Pockets">Its pockets of ore outside the veins, or null for none.</param>
public sealed record Vein(string Material, int Wave, int Tall, IReadOnlyList<int> Widths, Pockets? Pockets = null);

/// <summary>
/// An ore's pockets: blobs of it in plain stone where its own noise, swelling every <see cref="Wave"/> columns and
/// <see cref="Tall"/> rows, reaches its band's mark, so a mark near the top of the noise gives rare specks and a lower one more and
/// bigger blobs.
/// </summary>
/// <param name="Marks">
/// The noise a cell's must reach to be in a pocket, in each depth band (<see cref="PlanetType.Bands"/>), the coldest first: 0 to
/// 65536, 0 for no pockets in that band.
/// </param>
public sealed record Pockets(int Wave, int Tall, IReadOnlyList<int> Marks);
