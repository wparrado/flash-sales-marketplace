namespace FlashSales.Domain.Shared;

/// <summary>Structured domain error: machine-readable code + human message.</summary>
public sealed record Error(string Code, string Message)
{
    public override string ToString() => $"{Code}: {Message}";
}
