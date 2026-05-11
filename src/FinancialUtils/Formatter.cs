using System.Globalization;

namespace FinancialUtils;

/// <summary>
/// Formateo de valores numéricos para presentación.
/// </summary>
public static class Formatter
{
    /// <summary>
    /// Formatea un decimal como moneda.
    /// </summary>
    /// <param name="amount">Monto a formatear.</param>
    /// <param name="currencyCode">Código ISO 4217 (USD, MXN, CRC, etc.).</param>
    /// <param name="cultureName">Nombre del culture (default: es-MX).</param>
    /// <returns>Cadena formateada como moneda.</returns>
    public static string FormatCurrency(decimal amount, string currencyCode = "USD", string cultureName = "es-MX")
    {
        if (string.IsNullOrWhiteSpace(currencyCode))
            throw new ArgumentException("El código de moneda no puede estar vacío.", nameof(currencyCode));

        var culture = new CultureInfo(cultureName);
        var regionInfo = new RegionInfo(cultureName);

        // Usamos el símbolo del currencyCode solicitado, no el del culture
        var formatted = amount.ToString("N2", culture);
        return $"{currencyCode} {formatted}";
    }

    /// <summary>
    /// Formatea un valor decimal como porcentaje.
    /// </summary>
    /// <param name="value">Valor decimal (0.05 → "5.00%").</param>
    /// <param name="decimalPlaces">Cantidad de decimales a mostrar.</param>
    /// <returns>Cadena formateada como porcentaje.</returns>
    public static string FormatPercentage(decimal value, int decimalPlaces = 2)
    {
        if (decimalPlaces < 0)
            throw new ArgumentException("Los decimales no pueden ser negativos.", nameof(decimalPlaces));

        return $"{(value * 100).ToString($"F{decimalPlaces}")}%";
    }

    /// <summary>
    /// Formatea un número con separadores de miles.
    /// </summary>
    /// <param name="value">Número a formatear.</param>
    /// <param name="decimalPlaces">Decimales a mostrar.</param>
    /// <param name="cultureName">Nombre del culture (default: es-MX).</param>
    public static string FormatNumber(decimal value, int decimalPlaces = 0, string cultureName = "es-MX")
    {
        if (decimalPlaces < 0)
            throw new ArgumentException("Los decimales no pueden ser negativos.", nameof(decimalPlaces));

        var culture = new CultureInfo(cultureName);
        return value.ToString($"N{decimalPlaces}", culture);
    }

    /// <summary>
    /// Trunca un decimal a n lugares sin redondear.
    /// </summary>
    /// <param name="value">Valor a truncar.</param>
    /// <param name="decimalPlaces">Número de decimales a conservar.</param>
    public static decimal TruncateDecimals(decimal value, int decimalPlaces)
    {
        if (decimalPlaces < 0)
            throw new ArgumentException("Los decimales no pueden ser negativos.", nameof(decimalPlaces));

        var factor = (decimal)Math.Pow(10, decimalPlaces);
        return Math.Truncate(value * factor) / factor;
    }
}
