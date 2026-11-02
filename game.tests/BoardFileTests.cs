using System;
using System.Collections.Generic;
using System.IO;
using Stillwake.Game.Features.Research;
using Xunit;

namespace Stillwake.Game.Tests;

public sealed class BoardFileTests : IDisposable
{
    private readonly string _folder = Path.Combine(Path.GetTempPath(), Path.GetRandomFileName());

    public void Dispose()
    {
        if (Directory.Exists(_folder)) Directory.Delete(_folder, true);
    }

    [Fact]
    public void WhatWasMovedComesBack()
    {
        string path = BoardFile.PathOf(_folder, 7);
        Assert.True(BoardFile.Write(path, new BoardKept(new Dictionary<string, BoardSpot> { ["scrap"] = new(12.5, -40) }, ["scrap"])));
        var kept = BoardFile.Read(path);
        Assert.Equal(new BoardSpot(12.5, -40), Assert.Single(kept.Moved!).Value);
        Assert.Equal("scrap", Assert.Single(kept.Seen!));
    }

    [Fact]
    public void EachWorldHasItsOwnFile()
    {
        BoardFile.Write(BoardFile.PathOf(_folder, 7), new BoardKept(new Dictionary<string, BoardSpot> { ["scrap"] = new(1, 2) }));
        Assert.Null(BoardFile.Read(BoardFile.PathOf(_folder, 8)).Moved);
    }

    [Fact]
    public void ALostFileKeepsNothing() => Assert.Equal(new BoardKept(), BoardFile.Read(BoardFile.PathOf(_folder, 7)));

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("""{"Moved":{"scrap":null}}""")]
    [InlineData("""{"Moved":{"scrap":5}}""")]
    [InlineData("""{"Seen":5}""")]
    public void ABadFileKeepsNothing(string text)
    {
        Directory.CreateDirectory(_folder);
        string path = BoardFile.PathOf(_folder, 7);
        File.WriteAllText(path, text);
        Assert.Equal(new BoardKept(), BoardFile.Read(path));
    }

    [Fact]
    public void AWriteThatCannotBeDoneOnlySaysSo()
    {
        Directory.CreateDirectory(_folder);
        string blocked = Path.Combine(_folder, "file");
        File.WriteAllText(blocked, "");
        Assert.False(BoardFile.Write(Path.Combine(blocked, "7.json"), new BoardKept()));
    }
}
