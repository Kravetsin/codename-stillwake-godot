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
              {"name":"Stillwake-windows.zip","browser_download_url":"https://x/Stillwake-windows.zip"}]}
            """;
        Assert.Equal(new Updates.Release("v0.4", "https://x/Stillwake-windows.zip"), Updates.Read(answer));
        Assert.Null(Updates.Read("""{"tag_name":"v0.4","assets":[]}"""));
        Assert.Null(Updates.Read("""{"message":"Not Found"}"""));
        Assert.Null(Updates.Read("not json"));
    }

    [Fact]
    public void WritesAScriptThatWaitsForTheGameThenCopiesTheBuildOverAndStartsIt()
    {
        string script = Updates.Script(4242, @"C:\u\files", @"C:\games\Stillwake", @"C:\games\Stillwake\Stillwake.exe");
        Assert.Contains("PID eq 4242", script);
        Assert.Contains(@"robocopy ""C:\u\files"" ""C:\games\Stillwake"" /E", script);
        Assert.Contains(@"start """" ""C:\games\Stillwake\Stillwake.exe""", script);
        Assert.True(script.IndexOf("goto wait") < script.IndexOf("robocopy"));
    }
}
