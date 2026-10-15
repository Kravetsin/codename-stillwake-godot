using Stillwake.Game.Menu;
using Xunit;

namespace Stillwake.Game.Tests;

/// <summary>The codes a host reads out and a friend types in.</summary>
public class CodesTests
{
    [Fact]
    public void MakesFourLettersNoneMistakenForAnother()
    {
        var random = new System.Random(7);
        for (int i = 0; i < 200; i++)
        {
            string code = Codes.New(random);
            Assert.Equal(Codes.Length, code.Length);
            Assert.All(code, c => Assert.Contains(c, Codes.Letters));
        }
        Assert.DoesNotContain('O', Codes.Letters);
        Assert.DoesNotContain('0', Codes.Letters);
        Assert.DoesNotContain('I', Codes.Letters);
        Assert.DoesNotContain('1', Codes.Letters);
    }

    [Fact]
    public void TakesACodeAsTypedInAnyCaseDroppingWhatIsNoneOfItsLetters()
    {
        Assert.Equal("KQ7M", Codes.Typed("kq7m"));
        Assert.Equal("KQ7M", Codes.Typed(" k-q 7m!"));
        Assert.Equal("ABCD", Codes.Typed("abcdefg"));
        Assert.Equal("", Codes.Typed("0OI1"));
    }
}
