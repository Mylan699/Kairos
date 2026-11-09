namespace Kairos.Application.Risk;

/// <summary>Well-known market episodes. Every date is a market day of the datasets.</summary>
public static class HistoricalScenarios
{
    public static IReadOnlyList<HistoricalScenarioDto> All { get; } =
    [
        new("covid-2020", "Krach de mars 2020", "Du sommet de février au point bas : la pandémie met les marchés à l'arrêt.", new(2020, 2, 19), new(2020, 3, 23)),
        new("us-downgrade-2011", "Dégradation américaine", "La note des États-Unis perd son triple A et la crise de la dette européenne s'aggrave.", new(2011, 7, 22), new(2011, 8, 8)),
        new("brexit-2016", "Référendum sur le Brexit", "Le choc du vote britannique, du jour du scrutin au point bas qui suit.", new(2016, 6, 23), new(2016, 6, 27)),
        new("china-2015", "Dévaluation chinoise", "Les craintes sur la Chine culminent avec le lundi noir d'août 2015.", new(2015, 8, 17), new(2015, 8, 25)),
        new("taper-2013", "Taper tantrum", "L'annonce d'une réduction des achats de la Fed fait bondir les taux longs.", new(2013, 5, 22), new(2013, 9, 5)),
        new("volatility-2018", "Choc de volatilité 2018", "Le retour brutal de la volatilité après une longue période de calme.", new(2018, 1, 26), new(2018, 2, 8)),
    ];
}
