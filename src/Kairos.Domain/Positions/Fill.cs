using Kairos.Domain.Common;
using Kairos.Domain.ValueObjects;

namespace Kairos.Domain.Positions;

/// <summary>An executed quantity at a price, as seen by the position it changes.</summary>
public sealed record Fill
{
    /// <param name="side">Whether the position is bought or sold.</param>
    /// <param name="quantity">Shares, euros of EUR/USD, or bond nominal. Always positive.</param>
    /// <param name="price">The price in dollars (per 100 of nominal for a bond).</param>
    /// <param name="usdPerEur">The EUR/USD rate at the time of the trade.</param>
    public Fill(Side side, decimal quantity, decimal price, decimal usdPerEur)
    {
        if (quantity <= 0m)
        {
            throw new DomainException("A fill quantity must be strictly positive.");
        }

        if (price <= 0m)
        {
            throw new DomainException("A fill price must be strictly positive.");
        }

        if (usdPerEur <= 0m)
        {
            throw new DomainException("The EUR/USD rate of a fill must be strictly positive.");
        }

        Side = side;
        Quantity = quantity;
        Price = price;
        UsdPerEur = usdPerEur;
    }

    public Side Side { get; }

    public decimal Quantity { get; }

    public decimal Price { get; }

    public decimal UsdPerEur { get; }

    public decimal SignedQuantity => Quantity * Side.Sign();

    public decimal PriceInEur => Price / UsdPerEur;
}
