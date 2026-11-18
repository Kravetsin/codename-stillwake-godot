using System;
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
    public void WhatWasKeptComesBack()
    {
        string path = BoardFile.PathOf(_folder, 7);
        Assert.True(BoardFile.Write(path, new BoardKept(["scrap"], 2, null, [new BoardBatch(BoardSeen.CauseFound, ["scrap"], ["scrap", "nail"], 3)], "plate")));
        var kept = BoardFile.Read(path);
        Assert.Equal("scrap", Assert.Single(kept.Seen!));
        Assert.Equal(2, kept.Orders);
        var batch = Assert.Single(kept.Batches!);
        Assert.Equal((BoardSeen.CauseFound, "scrap", 2, 3), (batch.Cause, Assert.Single(batch.By), batch.Things.Count, batch.Almost));
        Assert.Equal("plate", kept.Pinned);
    }

    [Fact]
    public void EachWorldHasItsOwnFile()
    {
        BoardFile.Write(BoardFile.PathOf(_folder, 7), new BoardKept(["scrap"]));
        Assert.Null(BoardFile.Read(BoardFile.PathOf(_folder, 8)).Seen);
    }

    [Fact]
    public void ALostFileKeepsNothing() => Assert.Equal(new BoardKept(), BoardFile.Read(BoardFile.PathOf(_folder, 7)));

    [Theory]
    [InlineData("not json")]
    [InlineData("null")]
    [InlineData("""{"Seen":5}""")]
    [InlineData("""{"Batches":5}""")]
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
