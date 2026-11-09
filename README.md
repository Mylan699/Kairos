# Kairos

Poste de travail de trading cross-asset (actions, change, taux) : blotter, positions, P&L consolidé en euros, risques et rejeu de journées de marché historiques.

Projet de démonstration sur données historiques, aucune donnée réelle de client, pas de conseil en investissement.

## Structure du dépôt

| Dossier  | Contenu                                                  |
| -------- | -------------------------------------------------------- |
| `brand/` | Identité visuelle : symbole, favicon, règles d'usage     |
| `src/`   | Backend C# .NET en Clean Architecture                    |
| `web/`   | Frontend React et TypeScript                             |
| `docs/`  | Architecture, domaine métier, design system, décisions   |
| `data/`  | Jeux de données de marché téléchargés, jamais versionnés |

## Données et licences

La simulation couvre du 4 janvier 2010 au 29 avril 2020. Les fichiers sont téléchargés séparément et ne sont pas redistribués dans ce dépôt. Détails dans [`docs/donnees.md`](docs/donnees.md).

| Données                  | Source Kaggle                                                                                                                                 | Licence déclarée          |
| ------------------------ | --------------------------------------------------------------------------------------------------------------------------------------------- | ------------------------- |
| Actions du S&P 500       | [andrewmvd/sp-500-stocks](https://www.kaggle.com/datasets/andrewmvd/sp-500-stocks)                                                            | CC0 1.0 (domaine public)  |
| EUR/USD minute et heure  | [imetomi/eur-usd-forex-pair-historical-data-2002-2019](https://www.kaggle.com/datasets/imetomi/eur-usd-forex-pair-historical-data-2002-2019)  | GNU AGPL-3.0              |
| Taux du Trésor américain | [guillemservera/us-treasury-yields-daily](https://www.kaggle.com/datasets/guillemservera/us-treasury-yields-daily)                            | CC BY-NC 4.0              |

Les données appartiennent à leurs auteurs respectifs. Ce projet est non commercial.

Ce document sera complété au fil du projet (présentation métier, schéma, lancement).
