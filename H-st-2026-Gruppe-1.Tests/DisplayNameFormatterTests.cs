using H_st_2026_Gruppe_1.Models;

namespace H_st_2026_Gruppe_1.Tests;

// Tests for DisplayNameFormatter: makes names look nice ("ola nordmann" -> "Ola Nordmann").
public class DisplayNameFormatterTests
{
    [Theory]
    [InlineData("ola nordmann", "Ola Nordmann")]
    [InlineData("OLA NORDMANN", "Ola Nordmann")]
    [InlineData("oLa NoRdMaNn", "Ola Nordmann")]
    public void Format_UsesCapitalLetterOnEachWord(string input, string expected)
    {
        Assert.Equal(expected, DisplayNameFormatter.Format(input));
    }

    [Theory]
    [InlineData("  kari  ", "Kari")]
    public void Format_RemovesSpacesAtTheStartAndEnd(string input, string expected)
    {
        Assert.Equal(expected, DisplayNameFormatter.Format(input));
    }

    [Fact]
    public void Format_Null_ThrowsBecauseNameIsRequired()
    {
        Assert.Throws<NullReferenceException>(() => DisplayNameFormatter.Format(null!));
    }
}
