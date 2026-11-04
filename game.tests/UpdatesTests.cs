using Stillwake.Game.Platform;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>Which build this is, and whether GitHub's latest release is newer.</summary>
public class UpdatesTests
{
    [Fact]
    public void ReadsAReleaseFromItsVersionAndADeskBuildAsNone()
    {
        Assert.Equal(new Updates.Version("0.0.3", "67019ff"), Updates.Of("0.0.3+67019ffabcdef0123"));
        Assert.Equal(new Updates.Version(null, "67019ff"), Updates.Of("0.0.0+67019ffabcdef"));
        Assert.Equal(new Updates.Version("1.0.0", "unknown"), Updates.Of("1.0.0"));
        Assert.Equal("v0.0.3", Updates.Of("0.0.3+67019ffabc").ToString());
        Assert.Equal("dev 67019ff", Updates.Of("0.0.0+67019ffabc").ToString());
    }

    [Fact]
    public void OffersOnlyANewerReleaseNumberByNumberAndNothingToADeskBuild()
    {
        Assert.True(Updates.Newer("v0.0.4", "0.0.3"));
        Assert.True(Updates.Newer("v0.0.10", "0.0.9"));
        Assert.True(Updates.Newer("v0.1.0", "0.0.12"));
        Assert.True(Updates.Newer("v1.0.0", "0.12.3"));
        Assert.False(Updates.Newer("v0.0.3", "0.0.3"));
        Assert.False(Updates.Newer("v0.0.2", "0.0.3"));
        Assert.False(Updates.Newer("v9.9", null));
    }

    [Fact]
    public void ReadsTheLatestReleaseAndItsZipFromGitHubsAnswer()
    {
        const string answer = """
            {"tag_name":"v0.4","assets":[
              {"name":"notes.txt","browser_download_url":"https://x/notes.txt"},
              {"name":"Stillwake-windows.zip","browser_download_url":"https://x/Stillwake-windows.zip"},
              {"name":"Stillwake-macos.zip","browser_download_url":"https://x/Stillwake-macos.zip"}]}
            """;
        Assert.Equal(new Updates.Release("v0.4", "https://x/Stillwake-windows.zip"), Updates.Read(answer, "windows"));
        Assert.Equal(new Updates.Release("v0.4", "https://x/Stillwake-macos.zip"), Updates.Read(answer, "macos"));
        Assert.Null(Updates.Read("""{"tag_name":"v0.3","assets":[{"browser_download_url":"https://x/Stillwake-windows.zip"}]}""", "macos"));
        Assert.Null(Updates.Read("""{"tag_name":"v0.4","assets":[]}""", "windows"));
        Assert.Null(Updates.Read("""{"message":"Not Found"}""", "windows"));
        Assert.Null(Updates.Read("not json", "windows"));
    }

    [Fact]
    public void WritesAWindowsScriptThatWaitsForTheGameThenCopiesTheBuildOverAndStartsIt()
    {
        string script = Updates.WindowsScript(4242, @"C:\u\files", @"C:\games\Stillwake", @"C:\games\Stillwake\Stillwake.exe");
        Assert.Contains("PID eq 4242", script);
        Assert.Contains(@"robocopy ""C:\u\files"" ""C:\games\Stillwake"" /E", script);
        Assert.Contains(@"start """" ""C:\games\Stillwake\Stillwake.exe""", script);
        Assert.True(script.IndexOf("goto wait") < script.IndexOf("robocopy"));
    }

    [Fact]
    public void WritesAMacScriptThatWaitsForTheGameThenUnpacksSwapsTheAppAndOpensIt()
    {
        const string support = "/Users/a/Library/Application Support/Godot/app_userdata/Stillwake/update";
        string script = Updates.MacScript(4242, support + "/Stillwake.zip", support + "/files", "/Applications/Bob's Stillwake.app");
        Assert.StartsWith("#!/bin/sh\n", script);
        Assert.Contains("while kill -0 4242", script);
        Assert.Contains($"ditto -x -k '{support}/Stillwake.zip' '{support}/files' || exit 1", script);
        Assert.Contains($"ditto '{support}/files/Stillwake.app' '/Applications/Bob'\\''s Stillwake.app.new' || exit 1", script);
        Assert.Contains("open '/Applications/Bob'\\''s Stillwake.app'", script);
        Assert.DoesNotContain("\r", script);
        Assert.True(script.IndexOf("kill -0") < script.IndexOf("ditto") && script.IndexOf("ditto") < script.IndexOf("rm -rf '/Applications"));
    }
}
