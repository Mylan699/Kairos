using Kairos.Domain.Market;

namespace Kairos.Domain.Tests.Market;

public class IntradayPathGeneratorTests
{
    private const decimal Open = 100m;
    private const decimal High = 108m;
    private const decimal Low = 95m;
    private const decimal Close = 104m;

    private static decimal[] Path(int seed = 42) => IntradayPathGenerator.Generate(Open, High, Low, Close, seed);

    [Fact]
    public void There_is_one_price_per_minute_of_the_session()
    {
        Assert.Equal(391, Path().Length);
    }

    [Fact]
    public void The_path_starts_at_the_open_and_ends_at_the_close()
    {
        var path = Path();

        Assert.Equal(Open, path[0]);
        Assert.Equal(Close, path[^1]);
    }

    [Theory]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(3)]
    [InlineData(99)]
    public void The_path_reaches_the_high_and_low_without_going_beyond(int seed)
    {
        var path = Path(seed);

        Assert.Equal(High, path.Max());
        Assert.Equal(Low, path.Min());
    }

    [Fact]
    public void The_same_seed_gives_the_same_path()
    {
        Assert.Equal(Path(7), Path(7));
    }

    [Fact]
    public void Another_seed_gives_another_path()
    {
        Assert.NotEqual(Path(7), Path(8));
    }

    [Fact]
    public void A_rising_day_finds_its_low_before_its_high()
    {
        var path = Path();

        Assert.True(Array.IndexOf(path, Low) < Array.IndexOf(path, High));
    }

    [Fact]
    public void A_falling_day_finds_its_high_before_its_low()
    {
        var path = IntradayPathGenerator.Generate(100m, 103m, 90m, 92m, 42);

        Assert.True(Array.IndexOf(path, 103m) < Array.IndexOf(path, 90m));
    }

    [Fact]
    public void The_path_is_noisy_rather_than_straight()
    {
        var path = Path();

        var moves = path.Zip(path.Skip(1), (previous, next) => Math.Sign(next - previous));

        Assert.True(moves.Zip(moves.Skip(1), (first, second) => first != second).Count(changed => changed) > 50);
    }

    [Fact]
    public void A_day_with_no_range_stays_flat()
    {
        Assert.All(IntradayPathGenerator.Generate(50m, 50m, 50m, 50m, 1), price => Assert.Equal(50m, price));
    }

    [Fact]
    public void A_seed_depends_on_symbol_and_date_and_is_stable()
    {
        var date = new DateOnly(2016, 6, 24);

        Assert.Equal(IntradayPathGenerator.SeedFor("MSFT", date), IntradayPathGenerator.SeedFor("MSFT", date));
        Assert.NotEqual(IntradayPathGenerator.SeedFor("MSFT", date), IntradayPathGenerator.SeedFor("ORCL", date));
        Assert.NotEqual(IntradayPathGenerator.SeedFor("MSFT", date), IntradayPathGenerator.SeedFor("MSFT", date.AddDays(1)));
    }
}
