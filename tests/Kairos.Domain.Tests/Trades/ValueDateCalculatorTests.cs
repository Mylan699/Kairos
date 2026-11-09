using Kairos.Domain.Trades;
using Kairos.Domain.ValueObjects;

namespace Kairos.Domain.Tests.Trades;

public class ValueDateCalculatorTests
{
    [Theory]
    [InlineData(AssetClass.Equity, "2016-06-20", "2016-06-22")] // Monday to Wednesday
    [InlineData(AssetClass.Fx, "2016-06-22", "2016-06-24")] // Wednesday to Friday
    [InlineData(AssetClass.Equity, "2016-06-23", "2016-06-27")] // Thursday to Monday
    [InlineData(AssetClass.Fx, "2016-06-24", "2016-06-28")] // Friday to Tuesday
    [InlineData(AssetClass.Bond, "2016-06-20", "2016-06-21")] // Monday to Tuesday
    [InlineData(AssetClass.Bond, "2016-06-24", "2016-06-27")] // Friday to Monday
    [InlineData(AssetClass.Equity, "2016-06-25", "2016-06-28")] // Saturday counts from Monday
    [InlineData(AssetClass.Bond, "2016-06-26", "2016-06-27")] // Sunday counts from Monday
    public void Value_date_skips_weekends(AssetClass assetClass, string tradeDate, string expected)
    {
        var valueDate = ValueDateCalculator.ValueDateFor(assetClass, DateOnly.Parse(tradeDate));

        Assert.Equal(DateOnly.Parse(expected), valueDate);
    }
}
