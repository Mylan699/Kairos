namespace Kairos.Application.Replay;

/// <summary>Days worth replaying. The figures behind each choice are in docs/donnees.md.</summary>
public static class RemarkableDays
{
    private const int BeforeUsOpen = 13 * 60;

    public static IReadOnlyList<RemarkableDayDto> All { get; } =
    [
        new(new(2020, 3, 16), "Krach de mars 2020", "La pire séance de la période : l'action médiane perd près de 13 %.", BeforeUsOpen),
        new(new(2020, 3, 12), "Suspension des vols", "Les restrictions de voyage déclenchent une nouvelle chute de près de 10 %.", BeforeUsOpen),
        new(new(2020, 3, 9), "Guerre des prix du pétrole", "L'effondrement du pétrole s'ajoute à la pandémie.", BeforeUsOpen),
        new(new(2020, 3, 24), "Rebond historique", "Les mesures de soutien provoquent une hausse de plus de 10 %.", BeforeUsOpen),
        new(new(2018, 2, 5), "Choc de volatilité", "La volatilité revient brutalement après une longue période de calme.", BeforeUsOpen),
        new(new(2016, 11, 9), "Élection américaine", "Les taux longs bondissent de 19 pb, le dollar se renforce.", 0),
        new(new(2016, 6, 24), "Brexit", "L'euro cède près de 2 % dès la nuit, à mesure que les résultats tombent.", 0),
        new(new(2015, 8, 24), "Lundi noir chinois", "Les marchés plongent à l'ouverture et l'euro s'envole.", BeforeUsOpen),
        new(new(2015, 1, 15), "Fin du plancher du franc suisse", "La décision surprise de la banque centrale suisse secoue le change.", 8 * 60),
        new(new(2011, 8, 8), "Dégradation américaine", "Première séance après la perte du triple A.", BeforeUsOpen),
        new(new(2010, 5, 6), "Flash crash", "Les marchés américains décrochent en quelques minutes dans l'après-midi.", BeforeUsOpen),
    ];
}
