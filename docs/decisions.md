# Journal des décisions

Format court : contexte, décision, conséquences.

## ADR-001 : extraire l'identité visuelle dans `brand/`

- **Contexte** : les fichiers de marque ont été fournis dans l'archive `kairos-brand.zip`, à la racine du dépôt. Le reste du projet s'appuie sur le dossier `brand/`.
- **Décision** : extraire l'archive telle quelle dans `brand/`, sans modifier aucun fichier.
- **Conséquences** : `brand/` devient la source de vérité. Les SVG et leurs proportions restent ceux de l'archive.

## ADR-002 : ancrer l'exclusion des données à la racine du dépôt

- **Contexte** : un motif `data/` exclurait tout dossier portant ce nom à n'importe quelle profondeur. Git sous Windows ignore la casse, donc un dossier source `Data` (couramment utilisé avec EF Core) serait silencieusement exclu.
- **Décision** : utiliser le motif `/data/` dans le `.gitignore`.
- **Conséquences** : seuls les jeux de données téléchargés à la racine sont exclus. Les dossiers source nommés `Data` restent versionnés.

## ADR-003 : normaliser les fins de ligne avec `.gitattributes`

- **Contexte** : le développement se fait sous Windows alors que les images Docker s'exécutent sous Linux. Un script shell en CRLF échoue dans un conteneur.
- **Décision** : `text=auto` par défaut, LF forcé pour les scripts shell et les Dockerfiles.
- **Conséquences** : le comportement ne dépend plus de la configuration git de chaque poste.

## ADR-004 : définir une journée de marché comme l'intersection des trois sources

- **Contexte** : les trois datasets ont des calendriers différents. Le marché obligataire ferme certains jours où la Bourse reste ouverte, et le fichier EUR/USD présente des lacunes.
- **Décision** : une journée de marché exige des cours d'actions, une courbe complète et au moins 1 000 barres EUR/USD. La période commune va du 2010-01-04 au 2020-04-29, soit 2 567 journées. Dates de trade, valorisations et rejeu n'utilisent que ces journées.
- **Conséquences** : aucune valeur manquante à combler ni report de prix à justifier. 31 séances boursières sont écartées. Le détail est dans `docs/donnees.md`.

## ADR-005 : ne pas redistribuer les fichiers de données et respecter leurs licences

- **Contexte** : les licences déclarées sur Kaggle diffèrent : CC0 pour les actions, AGPL-3.0 pour l'EUR/USD, CC BY-NC 4.0 pour les taux. Le déposant du dataset EUR/USD indique lui-même que les données proviennent d'un tiers. Le site sera public.
- **Décision** : ne jamais versionner les fichiers bruts, citer chaque source et sa licence dans le README, garder la démonstration strictement non commerciale.
- **Conséquences** : un usage commercial imposerait de remplacer au moins les sources de change et de taux. Un clone du dépôt doit télécharger les données pour fonctionner.

## ADR-006 : composer un univers de 40 actions sans banque ni split postérieur

- **Contexte** : le fichier d'actions n'a de cours que pour 172 valeurs, et il est ajusté des splits jusqu'à fin 2024. Une valeur divisée après la période afficherait des prix très éloignés de ceux réellement cotés à l'époque (NVDA à moins d'un dollar en 2010). Le projet ne doit afficher aucun nom de banque réelle.
- **Décision** : retenir 40 valeurs cotées sur toute la période, réparties sur les 11 secteurs, en écartant les secteurs bancaires et de marchés de capitaux, ainsi que les valeurs connues pour un split après avril 2020. L'interface n'affiche que le ticker et le secteur, jamais le nom ou le logo de la société.
- **Conséquences** : les cours affichés correspondent aux cours historiques pour la plupart des dates. Les valeurs divisées pendant la période restent ajustées avant leur split. Des valeurs très connues (NVDA, AMZN, GOOG) sont absentes.

## ADR-007 : traiter toutes les heures de marché en UTC

- **Contexte** : les ticks EUR/USD sont en UTC, ce qui a été vérifié sur la période, alors que les actions se traitent de 9 h 30 à 16 h à New York, avec un décalage horaire qui varie selon l'heure d'été.
- **Décision** : stocker et rejouer en UTC, et convertir la séance actions depuis le fuseau `America/New_York` pour chaque date.
- **Conséquences** : les chocs de change restent alignés sur la séance actions toute l'année.

## ADR-008 : conventions de calcul du P&L

- **Contexte** : le P&L doit être consolidé en euros alors que tous les instruments sont cotés en dollars, avec une décomposition entre effet prix et effet change.
- **Décision** : coût moyen pondéré, suivi à la fois en dollars et en euros au taux de chaque achat. L'effet prix est le mouvement de prix converti au taux du jour, l'effet change est le mouvement du taux sur le capital investi : leur somme est exactement le P&L en euros. Une position EUR/USD est financée en dollars et n'a donc pas d'effet change sur capital. Les obligations sont suivies au prix clean.
- **Conséquences** : la décomposition n'a pas de résidu inexpliqué. Le portefeuille est supposé non couvert. Les coupons encaissés et le portage ne sont pas intégrés au P&L, ce qui est une simplification assumée.
