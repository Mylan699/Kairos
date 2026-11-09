namespace Kairos.Domain.ValueObjects;

public enum Side
{
    Buy,
    Sell,
}

public static class SideExtensions
{
    /// <summary>+1 for a buy, -1 for a sell: turns a quantity into a signed position change.</summary>
    public static int Sign(this Side side) => side == Side.Buy ? 1 : -1;

    public static Side Opposite(this Side side) => side == Side.Buy ? Side.Sell : Side.Buy;
}
