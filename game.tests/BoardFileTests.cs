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
        Assert.True(BoardFile.Write(path, new Dictionary<string, BoardSpot> { ["scrap"] = new(12.5, -40) }));
        Assert.Equal(new BoardSpot(12.5, -40), Assert.Single(BoardFile.Read(path)).Value);
    }

    [Fact]
    public void EachWorldHasItsOwnFile()
    {
        BoardFile.Write(BoardFile.PathOf(_folder, 7), new Dictionary<string, BoardSpot> { ["scrap"] = new(1, 2) });
        Assert.Empty(BoardFile.Read(BoardFile.PathOf(_folder, 8)));
    }

    [Fact]
    public void ALostFileMovesNothing() => Assert.Empty(BoardFile.Read(BoardFile.PathOf(_folder, 7)));

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("""{"scrap":null}""")]
    [InlineData("""{"scrap":5}""")]
    public void ABadFileMovesNothing(string text)
    {
        Directory.CreateDirectory(_folder);
        string path = BoardFile.PathOf(_folder, 7);
        File.WriteAllText(path, text);
        Assert.Empty(BoardFile.Read(path));
    }

    [Fact]
    public void AWriteThatCannotBeDoneOnlySaysSo()
    {
        Directory.CreateDirectory(_folder);
        string blocked = Path.Combine(_folder, "file");
        File.WriteAllText(blocked, "");
        Assert.False(BoardFile.Write(Path.Combine(blocked, "7.json"), new Dictionary<string, BoardSpot>()));
    }
}
