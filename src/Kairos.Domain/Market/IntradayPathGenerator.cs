namespace Kairos.Domain.Market;

/// <summary>
/// Builds a plausible minute-by-minute equity path from the only facts known about the
/// day: open, high, low and close. The path goes through all four exactly and never
/// leaves the high-low range. The same symbol and date always give the same path.
/// </summary>
public static class IntradayPathGenerator
{
    public const int SessionMinutes = 390;

    private const int FirstExtremeEarliest = 30;
    private const int FirstExtremeLatest = 170;
    private const int SecondExtremeEarliest = 210;
    private const int SecondExtremeLatest = 360;
    private const double NoisePerMinuteAsShareOfRange = 0.012;
    private const int DisplayDecimals = 2;

    /// <returns>One price per minute of the session, from the open to the close included.</returns>
    public static decimal[] Generate(decimal open, decimal high, decimal low, decimal close, int seed)
    {
        var random = new Random(seed);
        var noise = (double)(high - low) * NoisePerMinuteAsShareOfRange;

        // A day that ends up tends to find its low first, and the other way round.
        var (firstExtreme, secondExtreme) = close >= open ? (low, high) : (high, low);
        var firstTime = random.Next(FirstExtremeEarliest, FirstExtremeLatest);
        var secondTime = random.Next(SecondExtremeEarliest, SecondExtremeLatest);

        var prices = new decimal[SessionMinutes + 1];
        FillBridge(prices, 0, open, firstTime, firstExtreme, noise, low, high, random);
        FillBridge(prices, firstTime, firstExtreme, secondTime, secondExtreme, noise, low, high, random);
        FillBridge(prices, secondTime, secondExtreme, SessionMinutes, close, noise, low, high, random);

        return prices;
    }

    /// <summary>A stable seed: string.GetHashCode changes from one run of the process to the next.</summary>
    public static int SeedFor(string symbol, DateOnly date)
    {
        unchecked
        {
            var hash = date.DayNumber;
            foreach (var character in symbol)
            {
                hash = hash * 31 + character;
            }

            return hash;
        }
    }

    /// <summary>A random walk, corrected linearly so that it lands exactly on the end price.</summary>
    private static void FillBridge(
        decimal[] prices,
        int startTime,
        decimal startPrice,
        int endTime,
        decimal endPrice,
        double noise,
        decimal low,
        decimal high,
        Random random)
    {
        var steps = endTime - startTime;
        var walk = new double[steps + 1];

        for (var step = 1; step <= steps; step++)
        {
            walk[step] = walk[step - 1] + noise * NextGaussian(random);
        }

        prices[startTime] = startPrice;
        prices[endTime] = endPrice;

        for (var step = 1; step < steps; step++)
        {
            var progress = (double)step / steps;
            var trend = (double)startPrice + (double)(endPrice - startPrice) * progress;
            var price = (decimal)(trend + walk[step] - progress * walk[steps]);

            prices[startTime + step] = Math.Clamp(Math.Round(price, DisplayDecimals, MidpointRounding.AwayFromZero), low, high);
        }
    }

    // Box-Muller transform: turns two uniform draws into a normally distributed one.
    private static double NextGaussian(Random random) =>
        Math.Sqrt(-2d * Math.Log(1d - random.NextDouble())) * Math.Cos(2d * Math.PI * random.NextDouble());
}
