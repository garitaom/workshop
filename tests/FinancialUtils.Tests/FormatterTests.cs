using FinancialUtils;

namespace FinancialUtils.Tests;

public class FormatterTests
{
    // --- FormatCurrency ---

    [Fact]
    public void FormatCurrency_USD_ContainsCodeAndAmount()
    {
        var result = Formatter.FormatCurrency(1234.50m, "USD");
        Assert.Contains("USD", result);
        Assert.Contains("1", result);
    }

    [Fact]
    public void FormatCurrency_MXN_ContainsCode()
    {
        var result = Formatter.FormatCurrency(5000m, "MXN");
        Assert.Contains("MXN", result);
    }

    [Fact]
    public void FormatCurrency_EmptyCurrencyCode_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Formatter.FormatCurrency(100m, ""));
    }

    [Fact]
    public void FormatCurrency_WhitespaceCurrencyCode_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Formatter.FormatCurrency(100m, "   "));
    }

    // --- FormatPercentage ---

    [Fact]
    public void FormatPercentage_ConvertsDecimalToPercent()
    {
        Assert.Equal("5.00%", Formatter.FormatPercentage(0.05m));
    }

    [Fact]
    public void FormatPercentage_RespectsDecimalPlaces()
    {
        Assert.Equal("12.3%", Formatter.FormatPercentage(0.1234m, 1));
        Assert.Equal("12.340%", Formatter.FormatPercentage(0.1234m, 3));
    }

    [Fact]
    public void FormatPercentage_Zero_ReturnsZeroPercent()
    {
        Assert.Equal("0.00%", Formatter.FormatPercentage(0m));
    }

    [Fact]
    public void FormatPercentage_NegativeDecimals_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Formatter.FormatPercentage(0.05m, -1));
    }

    // --- FormatNumber ---

    [Fact]
    public void FormatNumber_LargeNumber_ContainsThousandSeparator()
    {
        var result = Formatter.FormatNumber(1000000m);
        // En es-MX el separador de miles es coma
        Assert.Contains(",", result);
    }

    [Fact]
    public void FormatNumber_WithDecimals_IncludesDecimalPart()
    {
        var result = Formatter.FormatNumber(1234.56m, 2);
        Assert.Contains("34", result);
    }

    [Fact]
    public void FormatNumber_NegativeDecimals_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Formatter.FormatNumber(100m, -1));
    }

    // --- TruncateDecimals ---

    [Fact]
    public void TruncateDecimals_DoesNotRound()
    {
        Assert.Equal(1.99m, Formatter.TruncateDecimals(1.999m, 2));
    }

    [Fact]
    public void TruncateDecimals_ZeroDecimals_ReturnsIntegerPart()
    {
        Assert.Equal(7m, Formatter.TruncateDecimals(7.9m, 0));
    }

    [Fact]
    public void TruncateDecimals_WholeNumber_RemainsUnchanged()
    {
        Assert.Equal(5m, Formatter.TruncateDecimals(5m, 2));
    }

    [Fact]
    public void TruncateDecimals_NegativeDecimals_ThrowsArgumentException()
    {
        Assert.Throws<ArgumentException>(() => Formatter.TruncateDecimals(1.5m, -1));
    }

    [Theory]
    [InlineData(-1.999, 2, -1.99)]
    [InlineData(0.001, 2, 0.00)]
    [InlineData(100.1234, 3, 100.123)]
    public void TruncateDecimals_VariousInputs_TruncatesCorrectly(
        decimal input, int places, decimal expected)
    {
        Assert.Equal(expected, Formatter.TruncateDecimals(input, places));
    }
}
