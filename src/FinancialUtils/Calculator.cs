namespace FinancialUtils;

/// <summary>
/// Operaciones financieras básicas y cálculos de préstamos.
/// </summary>
public static class Calculator
{
    /// <summary>
    /// Suma dos valores decimales.
    /// </summary>
    public static decimal Add(decimal a, decimal b) => a + b;

    /// <summary>
    /// Resta b de a.
    /// </summary>
    public static decimal Subtract(decimal a, decimal b) => a - b;

    /// <summary>
    /// Multiplica dos valores decimales.
    /// </summary>
    public static decimal Multiply(decimal a, decimal b) => a * b;

    /// <summary>
    /// Divide a entre b.
    /// </summary>
    /// <exception cref="DivideByZeroException">Si b es cero.</exception>
    public static decimal Divide(decimal a, decimal b)
    {
        if (b == 0)
            throw new DivideByZeroException("El divisor no puede ser cero.");

        return a / b;
    }

    /// <summary>
    /// Calcula el monto final con interés compuesto.
    /// Fórmula: P * (1 + r)^n
    /// </summary>
    /// <param name="principal">Capital inicial. Debe ser mayor o igual a cero.</param>
    /// <param name="rate">Tasa por periodo en decimal (ej: 0.05 para 5%). Debe ser >= 0.</param>
    /// <param name="periods">Número de periodos. Debe ser un entero positivo.</param>
    /// <returns>Monto final después de aplicar interés compuesto.</returns>
    /// <exception cref="ArgumentException">Si algún parámetro es inválido.</exception>
    public static decimal CompoundInterest(decimal principal, decimal rate, int periods)
    {
        if (principal < 0)
            throw new ArgumentException("El capital no puede ser negativo.", nameof(principal));

        if (rate < 0)
            throw new ArgumentException("La tasa no puede ser negativa.", nameof(rate));

        if (periods < 1)
            throw new ArgumentException("Los periodos deben ser un entero positivo.", nameof(periods));

        return principal * (decimal)Math.Pow((double)(1 + rate), periods);
    }

    /// <summary>
    /// Calcula la cuota mensual de un préstamo (amortización francesa).
    /// Fórmula: P * [r(1+r)^n] / [(1+r)^n - 1]
    /// </summary>
    /// <param name="principal">Monto del préstamo. Debe ser mayor a cero.</param>
    /// <param name="annualRate">Tasa anual en decimal (ej: 0.12 para 12%). Debe ser >= 0.</param>
    /// <param name="months">Plazo en meses. Debe ser un entero positivo.</param>
    /// <returns>Cuota mensual redondeada a 2 decimales.</returns>
    /// <exception cref="ArgumentException">Si algún parámetro es inválido.</exception>
    public static decimal LoanPayment(decimal principal, decimal annualRate, int months)
    {
        if (principal <= 0)
            throw new ArgumentException("El préstamo debe ser mayor a cero.", nameof(principal));

        if (annualRate < 0)
            throw new ArgumentException("La tasa no puede ser negativa.", nameof(annualRate));

        if (months < 1)
            throw new ArgumentException("El plazo debe ser un entero positivo.", nameof(months));

        if (annualRate == 0)
            return Math.Round(principal / months, 2, MidpointRounding.AwayFromZero);

        var monthlyRate = annualRate / 12;
        var factor = (decimal)Math.Pow((double)(1 + monthlyRate), months);
        var payment = principal * monthlyRate * factor / (factor - 1);

        return Math.Round(payment, 2, MidpointRounding.AwayFromZero);
    }

    /// <summary>
    /// Calcula el Valor Presente Neto (VPN) de una serie de flujos de caja.
    /// </summary>
    /// <param name="discountRate">Tasa de descuento por periodo en decimal.</param>
    /// <param name="cashFlows">
    /// Flujos de caja donde el índice 0 es la inversión inicial (normalmente negativa)
    /// y los siguientes son los flujos futuros.
    /// </param>
    /// <returns>Valor Presente Neto.</returns>
    /// <exception cref="ArgumentException">Si cashFlows está vacío.</exception>
    public static decimal NetPresentValue(decimal discountRate, IEnumerable<decimal> cashFlows)
    {
        var flows = cashFlows?.ToList()
            ?? throw new ArgumentNullException(nameof(cashFlows));

        if (flows.Count == 0)
            throw new ArgumentException("Se requiere al menos un flujo de caja.", nameof(cashFlows));

        decimal npv = 0;
        for (int t = 0; t < flows.Count; t++)
        {
            npv += flows[t] / (decimal)Math.Pow((double)(1 + discountRate), t);
        }

        return Math.Round(npv, 2, MidpointRounding.AwayFromZero);
    }
}
