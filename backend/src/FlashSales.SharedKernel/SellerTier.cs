namespace FlashSales.SharedKernel;

/// <summary>
/// Cross-context vocabulary: Catalog stamps it on offers, Ordering uses it to
/// select the commission strategy. Living in the shared kernel keeps the
/// Catalog and Ordering contexts free of references to each other.
/// </summary>
public enum SellerTier
{
    Standard,
    Premium
}
