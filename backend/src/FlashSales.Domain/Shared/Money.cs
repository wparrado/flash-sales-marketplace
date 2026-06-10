namespace FlashSales.Domain.Shared;

/// <summary>
/// Immutable value object for monetary amounts. All arithmetic returns new
/// instances and rounds to 2 decimals (away from zero), keeping order math pure.
/// </summary>
public readonly record struct Money
{
    public decimal Amount { get; }
    public string Currency { get; }

    public Money(decimal amount, string currency)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(amount, 0m);
        ArgumentException.ThrowIfNullOrWhiteSpace(currency);
        Amount = decimal.Round(amount, 2, MidpointRounding.AwayFromZero);
        Currency = currency;
    }

    public static Money Usd(decimal amount) => new(amount, "USD");
    public static Money Zero(string currency) => new(0m, currency);

    public static Money operator +(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount + right.Amount, left.Currency);
    }

    public static Money operator -(Money left, Money right)
    {
        EnsureSameCurrency(left, right);
        return new Money(left.Amount - right.Amount, left.Currency);
    }

    public static Money operator *(Money money, int quantity) =>
        new(money.Amount * quantity, money.Currency);

    /// <summary>Applies a fractional rate (e.g. 0.10m for 10%) and rounds the result.</summary>
    public Money ApplyRate(decimal rate) => new(Amount * rate, Currency);

    private static void EnsureSameCurrency(Money left, Money right)
    {
        if (left.Currency != right.Currency)
            throw new InvalidOperationException(
                $"Cannot operate on different currencies: {left.Currency} vs {right.Currency}");
    }

    public override string ToString() => $"{Amount:0.00} {Currency}";
}
