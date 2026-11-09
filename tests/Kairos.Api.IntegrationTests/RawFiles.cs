using Kairos.Infrastructure.Seeding;

namespace Kairos.Api.IntegrationTests;

/// <summary>Writes small files in the layout of the Kaggle datasets.</summary>
internal static class RawFiles
{
    private const int MinutesPerDay = 1_100;

    public static void Write(string root, params DateOnly[] days)
    {
        var curves = new List<string> { "date,US1M,US3M,US6M,US1Y,US2Y,US3Y,US5Y,US7Y,US10Y,US20Y,US30Y" };
        var stocks = new List<string> { "Date,Symbol,Adj Close,Close,High,Low,Open,Volume" };
        var fx = new List<string> { "Date,Time,BO,BH,BL,BC,BCh,AO,AH,AL,AC,ACh" };

        for (var index = 0; index < days.Length; index++)
        {
            var text = days[index].ToString("yyyy-MM-dd");
            var close = 100 + index * 5;

            curves.Add($"{text},1,1,1,1,1,1,2,2,{2 + index},3,3");
            stocks.AddRange(ReferenceSeed.EquitySectors.Keys.Select(ticker => $"{text},{ticker},{close},{close},{close + 2},{close - 2},{close},1000"));

            for (var minute = 0; minute < MinutesPerDay; minute++)
            {
                fx.Add($"{text},{minute / 60}:{minute % 60:00},1.1,1.1,1.1,1.1000,0,1.1,1.1,1.1,1.1002,0");
            }
        }

        WriteFile(Path.Combine(root, "treasury", "us_treasury_yields_daily.csv"), curves);
        WriteFile(Path.Combine(root, "sp500", "sp500_stocks.csv"), stocks);
        WriteFile(Path.Combine(root, "eurusd", "eurusd_minute.csv"), fx);
    }

    private static void WriteFile(string path, IEnumerable<string> lines)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllLines(path, lines);
    }
}
