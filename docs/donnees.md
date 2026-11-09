# Données de marché

Trois jeux de données Kaggle alimentent la simulation. Ils sont téléchargés dans `data/raw/` et ne sont jamais versionnés.

## Sources et formats

| Dossier         | Dataset Kaggle                                         | Fichiers utiles                                 | Couverture réelle         |
| --------------- | ------------------------------------------------------ | ----------------------------------------------- | ------------------------- |
| `raw/sp500/`    | `andrewmvd/sp-500-stocks`                              | `sp500_stocks.csv`, `sp500_companies.csv`       | 2010-01-04 → 2024-12-20   |
| `raw/eurusd/`   | `imetomi/eur-usd-forex-pair-historical-data-2002-2019` | `eurusd_minute.csv`, `eurusd_hour.csv`          | 2005-01-02 → 2020-04-29   |
| `raw/treasury/` | `guillemservera/us-treasury-yields-daily`              | `us_treasury_yields_daily.csv`                  | 1962-01-02 → 2024-06-21   |

### `sp500_stocks.csv`

Colonnes : `Date` (ISO), `Symbol`, `Adj Close`, `Close`, `High`, `Low`, `Open`, `Volume`.

- Seules 172 valeurs sur 502 ont des cours : les autres (dont AAPL, JPM, XOM) ont des lignes entièrement vides.
- `Open`, `High`, `Low` et `Close` sont ajustés des splits. `Adj Close` intègre en plus les dividendes : il n'est pas utilisé, pour rester cohérent avec les quatre autres prix.
- Contrôle OHLC sur la période : aucune ligne où `Low` dépasse l'ouverture ou la clôture, ni où `High` leur est inférieur.
- Biais du survivant : le fichier décrit la composition de l'indice fin 2024.

### `eurusd_minute.csv`

Colonnes : `Date` (ISO), `Time`, `BO`, `BH`, `BL`, `BC` (bid open, high, low, close), `AO`, `AH`, `AL`, `AC` (ask), `BCh` et `ACh` (variation sur la barre).

- `Time` n'a pas de zéro initial sur l'heure (`0:46`, `18:29`) : à analyser avec un format souple.
- Les prix contiennent du bruit de virgule flottante (`1.0874700000000002`) : ils sont arrondis à 5 décimales à l'import.
- **Fuseau : UTC**. Vérifié année par année : la plus forte variation des premiers vendredis du mois (publication de l'emploi américain à 8 h 30 à New York) tombe à 12 h 30 en été et 13 h 30 en hiver.
- Spread (ask moins bid), mesuré sur un échantillon d'un jour sur cinq : minimum 0,4 pip, médiane 1,3 pip, centile 99 à 5,4 pips, maximum 30 pips. Aucun spread négatif sur l'ensemble de la période.
- Environ 1 170 barres par jour. Les minutes sans cotation sont absentes du fichier.

### `us_treasury_yields_daily.csv`

Colonnes : `date` (ISO), `US1M`, `US3M`, `US6M`, `US1Y`, `US2Y`, `US3Y`, `US5Y`, `US7Y`, `US10Y`, `US20Y`, `US30Y`, en pourcentage.

Les jours fériés obligataires sont des lignes vides. Sur la période, une ligne est soit complète, soit entièrement vide.

## Période commune

**Du 2010-01-04 au 2020-04-29, soit 2 567 journées de marché.**

Une journée de marché exige les trois sources : des cours d'actions, une courbe complète et au moins 1 000 barres EUR/USD. Sur 2 598 séances boursières, 31 sont écartées :

- les jours où le marché obligataire ferme alors que la Bourse reste ouverte (Columbus Day, Veterans Day) ;
- les séances où l'EUR/USD est lacunaire dans le fichier (par exemple du 12 au 15 décembre 2016, ou le 31 mars 2017).

Le rejeu commence au plus tôt le 2010-01-05, car il interpole la courbe depuis la veille.

## Journées remarquables

Variations mesurées dans les données : médiane des actions du fichier complètes sur la période, EUR/USD de clôture à clôture, taux 10 ans en points de base.

| Date       | Événement                                 | Actions  | EUR/USD | 10 ans  |
| ---------- | ----------------------------------------- | -------- | ------- | ------- |
| 2010-05-06 | Flash crash                               | -3,22 %  | -1,26 % | -17 pb  |
| 2011-08-08 | Dégradation de la note des États-Unis     | -7,33 %  | -0,73 % | -18 pb  |
| 2015-01-15 | Fin du taux plancher du franc suisse      | -0,55 %  | -1,25 % | -9 pb   |
| 2015-08-24 | Lundi noir chinois                        | -3,98 %  | +1,70 % | -4 pb   |
| 2016-06-24 | Résultat du référendum sur le Brexit      | -3,88 %  | -1,98 % | -17 pb  |
| 2016-11-09 | Élection présidentielle américaine        | +0,34 %  | -0,89 % | +19 pb  |
| 2018-02-05 | Choc de volatilité                        | -3,57 %  | -0,71 % | -7 pb   |
| 2020-03-09 | Guerre des prix du pétrole                | -7,27 %  | +1,03 % | -20 pb  |
| 2020-03-12 | Pandémie : suspension des vols vers l'Europe | -9,63 % | -0,70 % | +6 pb   |
| 2020-03-16 | Krach de mars 2020                        | -12,99 % | +0,52 % | -21 pb  |
| 2020-03-24 | Rebond après les mesures de soutien       | +10,23 % | +0,32 % | +8 pb   |

## Univers d'actions

40 valeurs réparties sur les 11 secteurs. Choix expliqué dans l'ADR-006.

| Secteur                | Valeurs                        |
| ---------------------- | ------------------------------ |
| Technology             | MSFT, ORCL, CRM, CSCO, MSI     |
| Communication Services | DIS, T, EA, CMCSA              |
| Consumer Cyclical      | MCD, SBUX, TJX, DHI, CCL       |
| Consumer Defensive     | PEP, PM, STZ, GIS              |
| Energy                 | CVX, VLO, WMB                  |
| Financial Services     | V, PGR, ICE, CME, AJG          |
| Healthcare             | MRK, ABT, BSX, VRTX            |
| Industrials            | CAT, DE, UPS, FDX              |
| Real Estate            | EQIX, O                        |
| Utilities              | SO, ED                         |
| Basic Materials        | FCX, PPG                       |

Toutes ont un cours sur chaque séance de la période. Les plus fortes variations quotidiennes supérieures à 30 % ont été contrôlées et correspondent à des événements réels propres à la société (VRTX, EQIX, STZ, WMB) ou à mars 2020 (CCL).

## Licences

Voir la section « Données et licences » du `README.md` et l'ADR-005.
