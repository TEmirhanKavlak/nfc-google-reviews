using System.Text;
using NfcDegerlendirme.Services;
using Xunit;
namespace NfcDegerlendirme.Tests;

public class HelpersTests {
    [Theory]
    [InlineData("https://evil.com")] [InlineData("https://google.com.evil.com")] [InlineData("https://evilgoogle.com")]
    [InlineData("http://google.com")] [InlineData("javascript:alert(1)")] [InlineData("https://u:p@google.com")] [InlineData("")] [InlineData(null)]
    public void SafeGoogleUrl_RejectsUnsafe(string? url) => Assert.Null(Helpers.SafeGoogleUrl(url));

    [Fact] public void SafeGoogleUrl_AcceptsGoogle() {
        Assert.NotNull(Helpers.SafeGoogleUrl("https://search.google.com/local/writereview?placeid=X"));
        Assert.NotNull(Helpers.SafeGoogleUrl("https://g.page/r/abc/review"));
    }
    [Fact] public void GoogleRedirect_OnlyForFourAndFive() {
        const string u = "https://g.page/r/abc/review";
        Assert.All(new[] { 1, 2, 3 }, r => Assert.Null(Helpers.GoogleRedirect(r, u)));
        Assert.NotNull(Helpers.GoogleRedirect(4, u)); Assert.NotNull(Helpers.GoogleRedirect(5, u));
        Assert.Null(Helpers.GoogleRedirect(5, "https://evil.com")); Assert.Null(Helpers.GoogleRedirect(5, null));
    }
    [Fact] public void CleanText_StripsTagsAndControls() {
        Assert.Equal("xMerhaba", Helpers.CleanText("<script>x</script>Merhaba\u0000"));
        Assert.Null(Helpers.CleanText("   "));
    }
    [Fact] public void CsvCell_GuardsFormulas() {
        Assert.Equal("\"'=1+1\"", Helpers.CsvCell("=1+1")); Assert.Equal("\"a\"\"b\"", Helpers.CsvCell("a\"b"));
    }
    [Fact] public void Slugify_Turkish() {
        Assert.Equal("cicek-kafe-restoran", Helpers.Slugify("Çiçek Kafe & Restoran!")); Assert.Equal("firma", Helpers.Slugify("???"));
    }
    [Fact] public void CardCode_UniqueAndLong() {
        var a = Helpers.NewCardCode(); Assert.NotEqual(a, Helpers.NewCardCode()); Assert.True(a.Length >= 16);
    }
    [Fact] public void DetectImage_ByMagicBytes() {
        var pad = new byte[16];
        Assert.Equal("png", Helpers.DetectImage(new byte[] { 0x89, 0x50, 0x4E, 0x47 }.Concat(pad).ToArray()));
        Assert.Equal("jpg", Helpers.DetectImage(new byte[] { 0xFF, 0xD8, 0xFF }.Concat(pad).ToArray()));
        Assert.Equal("webp", Helpers.DetectImage(Encoding.ASCII.GetBytes("RIFF").Concat(new byte[4]).Concat(Encoding.ASCII.GetBytes("WEBP")).Concat(pad).ToArray()));
        Assert.Null(Helpers.DetectImage(Encoding.UTF8.GetBytes("<svg xmlns='http://www.w3.org/2000/svg'></svg>")));
    }
    [Fact] public void SafeColor_FallsBack() { Assert.Equal("#112233", Helpers.SafeColor("#112233")); Assert.Equal("#1a73e8", Helpers.SafeColor("red;x")); }
}
