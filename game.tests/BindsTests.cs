using System;
using System.Linq;
using System.Text.RegularExpressions;
using FsCheck;
using FsCheck.Fluent;
using FsCheck.Xunit;
using Stillwake.Game.I18n;
using Stillwake.Game.Shared;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Which key does each action (2026-11-04-key-binds): one key an action, none shared, none reserved, swapped on a clash.</summary>
[Collection(BindsCollection.Name)]
public class BindsTests
{
    private static Binds Rebound(Binds binds, Bind bind, string key) => binds.Rebind(bind, key, out _, out _)!;

    /// <summary>Every action has a key, no key does two, and none of them is one that is never bound.</summary>
    private static void AssertSound(Binds binds)
    {
        var keys = Enum.GetValues<Bind>().Select(binds.KeyOf).ToList();
        Assert.All(keys, key => Assert.False(string.IsNullOrEmpty(key)));
        Assert.Equal(keys.Count, keys.Distinct().Count());
        Assert.DoesNotContain(keys, Binds.Reserved.Contains);
    }

    [Fact]
    public void TheDefaultsGiveEveryActionAKeyOfItsOwn() => AssertSound(Binds.Default);

    [Fact]
    public void AFreeKeyIsTakenWithNothingSwapped()
    {
        var binds = Binds.Default.Rebind(Bind.Use, "F", out var swapped, out var refused);
        Assert.Equal("F", binds!.KeyOf(Bind.Use));
        Assert.Null(swapped);
        Assert.Null(refused);
        Assert.Equal("E", Binds.Default.KeyOf(Bind.Use));
    }

    [Fact]
    public void AKeyAnotherActionHasIsSwapped()
    {
        var binds = Binds.Default.Rebind(Bind.Use, "Q", out var swapped, out _);
        Assert.Equal("Q", binds!.KeyOf(Bind.Use));
        Assert.Equal(Bind.Drop, swapped);
        Assert.Equal("E", binds.KeyOf(Bind.Drop));
    }

    [Fact]
    public void ItsOwnKeyAgainChangesNothing()
    {
        var binds = Binds.Default.Rebind(Bind.Use, "E", out var swapped, out _);
        Assert.Equal("E", binds!.KeyOf(Bind.Use));
        Assert.Null(swapped);
    }

    [Theory]
    [InlineData("Key1", "bind.hotbar")]
    [InlineData("Key0", "bind.hotbar")]
    [InlineData("Escape", "bind.reserved")]
    [InlineData("Quoteleft", "bind.reserved")]
    [InlineData("Ctrl", "bind.reserved")]
    [InlineData("Left", "bind.reserved")]
    [InlineData("KpAdd", "bind.reserved")]
    public void AKeyNeverBoundIsRefusedSayingWhy(string key, string why)
    {
        Assert.Null(Binds.Default.Rebind(Bind.Use, key, out var swapped, out var refused));
        Assert.Equal(why, refused);
        Assert.Null(swapped);
    }

    [Fact]
    public void OnlyTheChangedKeysAreKeptAndReadBackAlike()
    {
        var binds = Rebound(Rebound(Binds.Default, Bind.Use, "F"), Bind.Jump, "Q");
        var kept = binds.Changed();
        Assert.Equal([("jump", "Q"), ("use", "F"), ("drop", "Space")], kept.OrderBy(k => k.Key == "Space").ThenBy(k => k.Id).ToList());
        var read = Binds.Read(kept);
        Assert.All(Enum.GetValues<Bind>(), b => Assert.Equal(binds.KeyOf(b), read.KeyOf(b)));
    }

    [Fact]
    public void WhatTheFileDoesNotMeanIsLeftAtItsDefault()
    {
        var read = Binds.Read([("noSuchAction", "F"), ("use", "Escape"), ("jump", ""), ("build", "Key5")]);
        Assert.All(Enum.GetValues<Bind>(), b => Assert.Equal(Binds.Default.KeyOf(b), read.KeyOf(b)));
    }

    [Fact]
    public void AFileGivingTwoActionsOneKeyIsReadAsSwaps()
    {
        AssertSound(Binds.Read([("use", "Q"), ("drop", "Q"), ("jump", "Q")]));
    }

    private static readonly string[] Pool =
        ["A", "D", "E", "F", "G", "Q", "Space", "Shift", "Tab", "Slash", "Equal", "F5", "Key3", "Escape", "Ctrl", "Up"];

    /// <summary>Proven to bite: with the swap taken out of <see cref="Binds.Rebind"/>, a clash fails it at once.</summary>
    [Property]
    public Property AnyRebindsLeaveEveryActionAKeyOfItsOwn() =>
        Prop.ForAll(Gen.ListOf(Gen.Zip(Gen.Elements(Enum.GetValues<Bind>()), Gen.Elements(Pool))).ToArbitrary(), steps =>
        {
            var binds = Binds.Default;
            foreach (var (bind, key) in steps) binds = binds.Rebind(bind, key, out _, out _) ?? binds;
            AssertSound(binds);
        });

    [Fact]
    public void ANamedKeyIsShownByItsWordAndASignByItself()
    {
        Assert.Equal("E", Binds.QwertyName("E"));
        Assert.Equal("/", Binds.QwertyName("Slash"));
        Assert.Equal(En.In(Language.English, "key.space"), Binds.QwertyName("Space"));
        Assert.Equal("F5", Binds.QwertyName("F5"));
    }

    [Fact]
    public void TextNamesTheKeyBoundNow()
    {
        var before = Binds.Current;
        try
        {
            Assert.Equal("[E] Open door", En.In(Language.English, "use.openDoor"));
            Binds.Current = Rebound(Binds.Default, Bind.Use, "F");
            Assert.Equal("[F] Open door", En.In(Language.English, "use.openDoor"));
            Assert.Equal("[F] Открыть дверь", En.In(Language.Russian, "use.openDoor"));
        }
        finally
        {
            Binds.Current = before;
        }
    }

    [Fact]
    public void ALabelMadeOnceIsToldAgainAfterARebind()
    {
        var before = Binds.Current;
        try
        {
            string label = En.In(Language.Russian, "menu.handbook");
            var told = Languages.Told(Language.Russian);
            Binds.Current = Rebound(Binds.Default, Bind.Handbook, "H");
            Assert.Equal("Справочник (H)", Languages.Retold(label, told, Language.Russian));
            Assert.Equal("Handbook (H)", Languages.Retold(label, told, Language.English));
        }
        finally
        {
            Binds.Current = before;
        }
    }

    [Fact]
    public void EveryPlaceholderNamesAnActionAndRussianNamesTheSameOnes()
    {
        foreach (var (key, text) in En.Table)
        {
            Assert.All(Binds.IdsIn(text), id => Assert.True(Binds.Named(id) != null, $"{key}: no action {id}"));
            if (Ru.Table.TryGetValue(key, out var russian)) Assert.True(Binds.IdsIn(text).SetEquals(Binds.IdsIn(russian)), $"{key}: Russian names other keys");
        }
    }

    [Fact]
    public void NoTextWritesABoundKeyByHand()
    {
        var written = new Regex(@"\[[A-Z]\]|\([A-Z]\)");
        foreach (var table in new[] { En.Table, Ru.Table })
            Assert.All(table, pair => Assert.False(written.IsMatch(pair.Value), $"{pair.Key} names a key by hand: {pair.Value}"));
    }
}

/// <summary>Tests that set <see cref="Binds.Current"/>, which <c>T</c> reads: run alone, so no other test reads the keys they put.</summary>
[CollectionDefinition(Name, DisableParallelization = true)]
public sealed class BindsCollection
{
    public const string Name = "binds";
}
