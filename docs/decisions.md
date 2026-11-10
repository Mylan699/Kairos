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

## ADR-009 : calculer les stress tests par revalorisation complète

- **Contexte** : un stress peut s'estimer à partir des sensibilités (delta multiplié par le choc), ou en revalorisant chaque position dans le marché choqué.
- **Décision** : revaloriser chaque position. Un scénario paramétrable applique le même choc partout, un scénario historique applique les variations réellement observées entre deux dates, valeur par valeur et maturité par maturité. Les deltas action et change sont des stress de +1 %, le DV01 est analytique.
- **Conséquences** : la convexité obligataire et le cumul des chocs actions et change sont pris en compte. Le coût de calcul reste faible avec quelques centaines de positions.

## ADR-010 : émettre plusieurs générations d'obligations par maturité

- **Contexte** : le brief prévoit quatre obligations (2, 5, 10 et 30 ans), mais une obligation à 2 ans ne peut pas vivre sur une période de dix ans.
- **Décision** : chaque maturité est réémise à l'échéance de la précédente, à partir du 4 janvier 2010, avec un coupon égal au taux de la courbe le jour d'émission. On obtient 6 émissions à 2 ans, 3 à 5 ans, 2 à 10 ans et une à 30 ans.
- **Conséquences** : quatre lignes restent négociables à toute date. Les émissions échues restent dans le référentiel et sont remboursées au pair.

## ADR-011 : partager un seul rejeu entre tous les visiteurs

- **Contexte** : la démo publique n'a pas d'authentification. Un rejeu par visiteur demanderait de garder une simulation complète en mémoire pour chacun d'eux, sur une petite instance.
- **Décision** : un seul rejeu tourne côté serveur, comme un marché commun. Les commandes sont ouvertes et limitées en débit. Après 20 minutes sans commande, le rejeu revient seul à la journée par défaut. Les trades sont partagés de la même façon et remis à zéro chaque nuit.
- **Conséquences** : le coût serveur ne dépend pas du nombre de visiteurs. Deux visiteurs simultanés peuvent se gêner, ce qui est acceptable pour une démonstration.

## ADR-012 : une seule image Docker, données importées une fois

- **Contexte** : il faut lancer toute l'application avec `docker compose up --build` et la déployer sur Azure App Service. Les fichiers de marché (844 Mo) ne sont pas versionnés et leurs licences interdisent de les redistribuer dans une image publique. Le limiteur de débit identifie chaque visiteur par la dernière adresse de `X-Forwarded-For`, ce qui suppose un seul proxy devant l'API.
- **Décision** : une image unique en trois étapes (build du front, publication de l'API, runtime ASP.NET) dans laquelle l'API sert aussi le front depuis `wwwroot`. Les données ne sont jamais dans l'image : en local, `./data/raw` est monté en lecture seule et importé au premier démarrage, puis conservé dans un volume SQL. Sur Azure, la même image est lancée une fois depuis le poste avec `--import-only` vers Azure SQL, et l'App Service démarre ensuite sur une base déjà remplie. La remise à zéro nocturne ne touche que les trades.
- **Conséquences** : une seule origine, donc ni CORS ni second proxy qui masquerait l'adresse des visiteurs, et l'image qui tourne en local est exactement celle qui part en ligne. Le front et l'API se déploient ensemble, ce qui convient à une démo mais empêcherait de les faire évoluer séparément. Le premier `docker compose up` dure quelques minutes, le temps de l'import.
