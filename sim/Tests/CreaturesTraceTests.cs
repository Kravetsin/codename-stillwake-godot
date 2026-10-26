using System.Reflection;
using System.Text.Json;
using Stillwake.Sim.Creatures;
using Xunit;
using Tune = Stillwake.Content.Creatures;

namespace Stillwake.Sim.Tests;

/// <summary>
/// The creatures' behaviour pinned while their module is rebuilt (C-1 of docs/plans/creatures.md): runs over drawn ground, heroes
/// moving, striking and reporting rams, each folded tick by tick into one number — every creature's body and damage, the world's
/// random state, the suits and what lies about. Recorded before the rebuild; deleted once it is done.
/// </summary>
public class CreaturesTraceTests
{
    private const int Width = 96, Height = 32, Ticks = 600;

    private static Layout Ground(uint seed)
    {
        var rng = new Rng(new RngState { S = seed });
        var rows = Enumerable.Range(0, Height).Select(_ => new char[Width]).ToArray();
        int top = 20;
        for (int x = 0; x < Width; x++)
        {
            top = Math.Clamp(top + (int)rng.Below(13) - 6, 6, Height - 4);
            for (int y = 0; y < Height; y++)
                rows[y][x] = x == 0 || x == Width - 1 || y == Height - 1 || y >= top ? '#' : ' ';
            if (rng.Below(6) == 0 && top > 10) rows[top - 6 - (int)rng.Below(4)][x] = '#';
        }
        return Layout.Parse([.. rows.Select(r => new string(r))]);
    }

    private static readonly FieldInfo WorldRng = typeof(World).GetField("_rng", BindingFlags.NonPublic | BindingFlags.Instance)!;

    private static ulong Fold(ulong h, long v)
    {
        unchecked
        {
            for (int i = 0; i < 8; i++)
            {
                h ^= (byte)(v >> (8 * i));
                h *= 1099511628211;
            }
        }
        return h;
    }

    private static ulong Fold(ulong h, string s)
    {
        foreach (char ch in s) h = Fold(h, ch);
        return h;
    }

    /// <summary>One run's trace: <paramref name="seed"/> draws its ground, its creatures and every command.</summary>
    private static ulong Trace(uint seed)
    {
        var w = CreaturesTesting.Planet(Ground(seed), cells: true);
        var rng = new Rng(new RngState { S = seed * 7919 + 1 });
        const int heroes = 3;
        for (int h = 0; h < heroes; h++) w.HeroAt(1 + (int)rng.Below(Width - 2), 4, peer: h);
        w.Run();
        for (int i = 0; i < 24; i++) w.Do(new Release(Tune.All[(int)rng.Below((uint)Tune.All.Count)].Id, (int)rng.Below(Width * Height)));
        w.Run();
        ulong trace = 14695981039346656037;
        for (int t = 0; t < Ticks; t++)
        {
            var all = w.M.Creatures.Creatures;
            if (all.Count > 0 && rng.Below(5) == 0)
            {
                var weapons = Content.Weapons.All;
                w.Do(new Strike(all[(int)rng.Below((uint)all.Count)].Id, weapons[(int)rng.Below((uint)weapons.Count)].Item) { From = (int)rng.Below(heroes) });
            }
            // Two strikes on one creature in a tick now and then: the second finds it struck, or gone.
            if (all.Count > 0 && rng.Below(40) == 0)
            {
                int id = all[(int)rng.Below((uint)all.Count)].Id;
                w.Do(new Strike(id, Content.Weapons.Axe.Item) { From = 0 }).Do(new Strike(id, Content.Weapons.Axe.Item) { From = 1 });
            }
            foreach (var c in all)
                for (int h = 0; h < heroes; h++)
                    if (rng.Below(4) == 0) w.Do(new HitBy(c.Id) { From = h });
            if (rng.Below(15) == 0) w.HeroAt(1 + (int)rng.Below(Width - 2), (int)rng.Below(Height), peer: (int)rng.Below(heroes));
            if (rng.Below(60) == 0) w.Do(new Release(Tune.All[(int)rng.Below((uint)Tune.All.Count)].Id, (int)rng.Below(Width * Height)));
            w.Run();
            foreach (var c in w.M.Creatures.Creatures)
            {
                trace = Fold(trace, c.Id);
                trace = Fold(trace, c.Species);
                trace = Fold(trace, c.X);
                trace = Fold(trace, c.Y);
                trace = Fold(trace, c.VX);
                trace = Fold(trace, c.VY);
                trace = Fold(trace, c.Damage);
            }
            trace = Fold(trace, ((RngState)WorldRng.GetValue(w.World)!).S);
            trace = Fold(trace, JsonSerializer.Serialize(w.M.Crew.Slice, w.World.Json));
            trace = Fold(trace, JsonSerializer.Serialize(w.M.Items.Slice, w.World.Json));
        }
        return trace;
    }

    public static TheoryData<uint, ulong> Recorded => new()
    {
        { 1, 11012579585236370609 },
        { 2, 1254466463607602423 },
        { 3, 11265186195370852448 },
        { 4, 4286042091551047162 },
        { 5, 15739453438943536085 },
        { 6, 7535570482686293559 },
        { 7, 9497095347141773793 },
        { 8, 829044217340986804 },
        { 9, 5076772097305903915 },
        { 10, 1515339669540333063 },
        { 11, 1041101347940458636 },
        { 12, 13823321389894600842 },
        { 13, 5047208470772594844 },
        { 14, 13951664425486908670 },
        { 15, 3803954380246800331 },
        { 16, 12825960632093435621 },
    };

    [Theory]
    [MemberData(nameof(Recorded))]
    public void CreaturesBehaveAsRecorded(uint seed, ulong expected) => Assert.Equal(expected, Trace(seed));
}
