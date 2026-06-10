using FlashSales.Domain.Catalog;
using FlashSales.Domain.Shared;

namespace FlashSales.UnitTests.TestDoubles;

/// <summary>
/// Builder pattern for test data: keeps tests declarative and resilient to
/// constructor changes in the Offer record.
/// </summary>
public class OfferBuilder
{
    private Guid _id = Guid.NewGuid();
    private Guid _sellerId = Guid.NewGuid();
    private string _name = "Nintendo Switch 2";
    private string _description = "Flash deal on the latest console";
    private Money _price = Money.Usd(299.99m);
    private int _stock = 10;
    private DateTimeOffset _startsAt = DateTimeOffset.UtcNow.AddHours(-1);
    private DateTimeOffset _endsAt = DateTimeOffset.UtcNow.AddHours(1);

    public OfferBuilder WithId(Guid id) { _id = id; return this; }
    public OfferBuilder WithSeller(Guid sellerId) { _sellerId = sellerId; return this; }
    public OfferBuilder WithName(string name) { _name = name; return this; }
    public OfferBuilder WithDescription(string description) { _description = description; return this; }
    public OfferBuilder WithPrice(Money price) { _price = price; return this; }
    public OfferBuilder WithStock(int stock) { _stock = stock; return this; }

    public OfferBuilder WithWindow(DateTimeOffset startsAt, DateTimeOffset endsAt)
    {
        _startsAt = startsAt;
        _endsAt = endsAt;
        return this;
    }

    public Offer Build() => new(_id, _sellerId, _name, _description, _price, _stock, _startsAt, _endsAt);
}
