# Questions d'entretien

Vingt-cinq questions qu'un recruteur technique pourrait poser sur Kairos, avec des réponses courtes appuyées sur le code. Les chemins indiqués partent de `src/` pour le backend et de `web/src/` pour le front.

## Architecture et .NET

### 1. Pourquoi quatre projets plutôt qu'une seule API ?

Pour que les règles métier ne dépendent de rien. `Kairos.Domain` ne connaît ni la base, ni le réseau, ni l'horloge : le pricing obligataire ou le calcul du P&L se testent en quelques millisecondes, sans infrastructure. `Kairos.Application` décrit les cas d'usage et déclare ses besoins sous forme d'interfaces (`Abstractions/Ports.cs`), que `Kairos.Infrastructure` implémente avec EF Core. Les dépendances vont toujours vers le domaine. Pour un projet de cette taille, je n'ai pas ajouté de médiateur ni de CQRS : des services applicatifs suffisent et restent faciles à suivre.

### 2. Pourquoi `decimal` pour l'argent, mais `double` dans le pricing obligataire ?

`decimal` représente exactement les montants en base 10 : 0,1 + 0,2 vaut bien 0,3, ce qui compte quand on additionne des milliers de lignes de P&L. Mais `Math.Pow` avec un exposant fractionnaire n'existe pas en `decimal`, et l'actualisation d'une obligation en a besoin. Un prix de modèle n'a de toute façon pas de valeur exacte, donc `BondPricer` calcule en `double` et rend des `decimal` (`Kairos.Domain/Rates/BondPricer.cs`). Le commentaire en tête de la classe l'explique.

### 3. Comment l'API transforme-t-elle les erreurs en réponses HTTP ?

Par un seul `IExceptionHandler` (`Kairos.Api/Errors/ApiExceptionHandler.cs`) qui associe chaque type d'exception à un statut ProblemDetails. Une `ValidationException` de FluentValidation donne une 400 avec les erreurs par champ, `NotFoundException` une 404, `DomainException` (une règle métier refusée, comme amender un trade réglé) une 422, et un marché pas encore démarré une 503. Toute autre exception est un bug : le gestionnaire par défaut répond 500 sans révéler de détail. Le front lit le même format partout (`api/client.ts`).

### 4. Comment validez-vous les entrées ?

Sur deux niveaux. FluentValidation vérifie la forme de la requête : quantité positive, vitesse de rejeu parmi les valeurs autorisées, minute entre 0 et 1 439 (`Kairos.Application/Trades/TradeValidators.cs`, `Replay/ReplayContracts.cs`). Le domaine protège ses invariants lui-même : une cotation avec un ask sous le bid ou une transition de statut interdite lèvent une `DomainException`. Ainsi, même un appel qui contourne l'API ne peut pas créer un état incohérent.

### 5. Comment EF Core stocke-t-il un trade avec son historique ?

Un trade est un agrégat : ses versions et sa piste d'audit sont des entités possédées (`OwnsMany`), stockées dans `TradeVersions` et `TradeAuditEntries`, et toujours chargées et enregistrées avec lui (`Kairos.Infrastructure/Persistence/Configurations/TradeConfiguration.cs`). Les entités du domaine gardent des setters privés. EF Core passe par des constructeurs privés, sans paramètre ou liés aux colonnes comme `Trade(Guid id)`, si bien que le domaine n'expose rien de plus pour faire plaisir à l'ORM.

### 6. Comment importez-vous 3,5 millions de barres minute ?

Avec `SqlBulkCopy` par lots de 200 000 lignes, plutôt qu'avec EF Core, qui ferait une insertion par ligne (`Kairos.Infrastructure/Import/MarketDataImporter.cs`). L'import vide d'abord les tables, puis écrit les barres journalières EUR/USD en dernier : leur présence sert de marqueur d'import terminé. Un import interrompu est donc simplement recommencé au démarrage suivant. Dans le conteneur, l'ensemble prend environ 90 secondes.

## Métier

### 7. Comment calculez-vous le P&L d'une position ?

Au coût moyen pondéré (`Kairos.Domain/Positions/Position.cs`). Un achat qui renforce la position recalcule le prix moyen. Une vente qui la réduit dégage un P&L réalisé, égal à la quantité vendue multipliée par l'écart entre le prix de vente et le prix moyen, sans changer le prix moyen de ce qui reste. Si la vente retourne la position (de longue à courte), la partie qui dépasse ouvre une nouvelle position au prix de cette vente. La position est un `record` immuable : `Apply(fill)` renvoie un nouvel état, et une position n'est que le rejeu de ses exécutions.

### 8. Comment séparez-vous l'effet prix de l'effet change ?

Chaque position garde deux prix moyens : en dollars, et en euros au taux de chaque achat. L'effet prix est le mouvement du prix converti au taux du jour. L'effet change est la différence entre le coût en dollars converti au taux du jour et le coût en euros historique (`Position.EffectsOf`). La somme des deux est exactement le P&L en euros, sans résidu, et un test le vérifie. Une position EUR/USD n'a pas d'effet change sur capital, car elle est financée en dollars (`AssetClass.HasCurrencyEffect`).

### 9. Comment pricez-vous une obligation, et qu'est-ce que le DV01 ?

`BondPricer` actualise chaque coupon restant et le remboursement au taux de la courbe pour la maturité résiduelle, en capitalisation semestrielle. Il en déduit le prix dirty, le coupon couru, le prix clean, la duration modifiée et le DV01 : la variation de prix pour un point de base, égale à la duration modifiée × le prix dirty × 0,0001. Le DV01 du desk est la somme sur toutes les positions obligataires, convertie en euros (`Kairos.Domain/Risk/RiskCalculator.cs`).

### 10. Pourquoi des stress tests par revalorisation complète ?

Une estimation par sensibilités (delta × choc) est linéaire : elle ignore la convexité des obligations et le fait qu'une action américaine qui baisse pendant que le dollar baisse cumule deux pertes en euros. `StressEngine` applique le scénario au marché (`MarketSnapshot.Apply`), puis revalorise chaque position. Les scénarios historiques appliquent les mouvements réels entre deux dates, action par action et maturité par maturité (`Kairos.Domain/Risk/StressScenario.cs`). Avec quelques centaines de positions, le coût reste négligeable.

### 11. Comment fonctionne la machine à états d'un trade ?

Un dictionnaire décrit les transitions autorisées : `New` vers `Validated` ou `Cancelled`, `Validated` vers `Settled`, `Cancelled` ou `New`, et rien après `Settled` ou `Cancelled` (`Kairos.Domain/Trades/TradeStateMachine.cs`). C'est le seul endroit qui le dit, et chaque méthode de `Trade` passe par lui. Amender un trade validé crée une nouvelle version et le renvoie en `New`, parce qu'une modification doit être revalidée. Chaque action ajoute une entrée d'audit avec l'auteur et l'heure.

### 12. Comment gérez-vous les dates de valeur et les fuseaux horaires ?

T+2 jours ouvrés pour les actions et le change, T+1 pour les obligations du Trésor, en sautant les week-ends (`Kairos.Domain/Trades/ValueDateCalculator.cs`). Les jours fériés ne sont pas gérés, c'est une simplification assumée : il faudrait un calendrier par place. Pour le temps, tout est stocké et rejoué en UTC. Seule l'ouverture de la séance actions est convertie depuis `America/New_York` pour chaque date, ce qui suit l'heure d'été (`Market/UsEquitySession.cs`). L'image Docker contient bien la base `tzdata` nécessaire sous Linux.

### 13. Les données d'actions sont journalières. Comment rejouez-vous une journée minute par minute ?

`IntradayPathGenerator` construit un pont brownien : une marche aléatoire corrigée pour passer exactement par l'ouverture, le plus haut, le plus bas et la clôture réels, sans sortir de la fourchette. Un jour qui finit en hausse touche plutôt son plus bas en premier. La graine dépend du symbole et de la date, avec un hachage écrit à la main, car `string.GetHashCode` change d'un processus à l'autre : le même jour donne toujours le même chemin. EUR/USD, lui, utilise les vraies cotations minute.

## Temps réel et performance

### 14. Comment le serveur pousse-t-il les prix ?

Un `BackgroundService` (`Kairos.Infrastructure/Replay/ReplayEngine.cs`) avance l'horloge toutes les 500 ms, reconstruit le marché, valorise le portefeuille et publie un message `tick` à tous les clients SignalR. Les positions détaillées et le rapport de risque, plus lourds, ne partent qu'aux connexions abonnées aux groupes `positions` et `risk`, c'est-à-dire aux écrans qui les affichent (`Kairos.Api/Realtime/MarketHub.cs`). Une erreur sur un tick est journalisée sans arrêter le rejeu des autres visiteurs.

### 15. Le marché courant est lu par les requêtes HTTP et écrit par le moteur. Comment évitez-vous les problèmes de concurrence ?

Le `MarketSnapshot` est immuable. Le moteur en construit un nouveau à chaque tick, puis remplace la référence d'une seule écriture, dans un champ `volatile` (`Kairos.Infrastructure/Replay/LiveMarketState.cs`). Une requête lit donc toujours un marché complet et cohérent, sans verrou. C'est le même principe que l'immutabilité des positions.

### 16. Recalculez-vous tout le portefeuille à chaque tick ?

Non. Les positions, c'est-à-dire le rejeu de milliers de trades, ne sont recalculées que quand un trade change ou que la date de marché change (`Kairos.Application/Portfolio/PortfolioState.cs`). À chaque tick, seule la valorisation de ces positions dans le nouveau marché est refaite, ce qui reste léger. Les totaux sont additionnés à partir des lignes arrondies, pour que les lignes affichées tombent juste sur le total.

### 17. Côté React, comment évitez-vous de redessiner toute l'application deux fois par seconde ?

Les données SignalR passent par un magasin unique (`realtime/LiveStore.ts`) lu avec `useSyncExternalStore` : un composant ne se redessine que si la partie qu'il lit a changé. Le flash d'un prix n'utilise pas d'état React mais l'API Web Animations (`design-system/components/LiveFigures.tsx`). Quand un trade change, le blotter remplace une seule ligne dans le cache TanStack Query avec `setQueryData` au lieu de recharger les 3 000 trades. AG Grid virtualise les lignes affichées.

### 18. Comment le front reste-t-il cohérent après une coupure réseau ou un changement de date ?

La connexion SignalR se reconnecte avec un délai croissant, plafonné. Une fois reconnecté, le front invalide ses requêtes, car des trades ont pu changer pendant la coupure. Quand le rejeu change de date, tout ce qui a été lu pour l'ancienne date est périmé : le magasin invalide les requêtes TanStack Query. La remise à zéro nocturne envoie un message `dataReset` qui fait de même.

## Sécurité, tests et exploitation

### 19. La démo est publique et sans compte. Comment la protégez-vous ?

Limitation de débit par adresse IP : 600 lectures et 60 écritures par minute, avec une 429 en ProblemDetails et un en-tête `Retry-After`. Derrière un proxy, l'adresse vient de `X-Forwarded-For`, en ne gardant que la dernière entrée, celle de notre propre proxy, pour qu'un client ne puisse pas se faire passer pour un autre. S'y ajoutent CORS fermé par défaut, des en-têtes de sécurité, et une remise à zéro des trades chaque nuit (`Kairos.Api/Security/SecurityExtensions.cs`). Il n'y a ni secret ni donnée personnelle à protéger.

### 20. Que faudrait-il ajouter pour un vrai desk ?

Une authentification Microsoft Entra ID, avec des rôles : trader, middle office, lecture seule. Ensuite, le contrôle à quatre yeux (celui qui valide un trade ne peut pas être celui qui l'a saisi), ce qui serait une règle de plus dans le domaine. Il faudrait aussi un jeton de concurrence optimiste sur les trades : aujourd'hui, deux amendements simultanés du même trade se résolvent par « le dernier gagne ». Enfin, un calendrier de jours fériés par place, et les coupons dans le P&L.

### 21. Comment avez-vous testé ?

Par niveaux (voir `docs/architecture.md`). Le domaine a 181 tests xUnit sans dépendance : pricing, conversion, P&L, machine à états, dates de valeur, stress. L'application en a 89, avec des doublures en mémoire. L'infrastructure et l'API utilisent Testcontainers avec un vrai SQL Server, parce que le fournisseur EF Core en mémoire ne vérifie ni les contraintes, ni les types SQL, ni `SqlBulkCopy`. Le front a 200 tests Vitest sur le formatage, les modèles de pages et les composants. Enfin, un script Playwright capture chaque écran en deux largeurs et deux thèmes, et la CI GitHub Actions lance l'ensemble à chaque push.

### 22. Pourquoi une seule image Docker pour l'API et le front ?

Une seule origine supprime CORS et, surtout, évite un second proxy qui masquerait l'adresse des visiteurs au limiteur de débit (ADR-012). L'image est construite en trois étapes : build du front avec Node, publication de l'API avec le SDK, puis un runtime ASP.NET seul, exécuté par un utilisateur sans privilèges. L'image qui tourne en local avec `docker compose` est exactement celle qui part sur Azure. En échange, le front et l'API se déploient ensemble.

### 23. Comment l'application passerait-elle à plusieurs instances ?

Aujourd'hui elle ne le peut pas, et c'est voulu : le rejeu partagé vit en mémoire dans le processus (ADR-011), et App Service tourne sur une seule instance. Pour passer à l'échelle, il faudrait sortir le moteur de rejeu dans un service unique, publier les ticks par un backplane SignalR (Azure SignalR Service ou Redis), et appliquer les migrations dans une étape de déploiement plutôt qu'au démarrage, pour que deux instances ne migrent pas en même temps.

### 24. Comment surveilleriez-vous l'application en production ?

Elle expose déjà deux health checks : `/health` dit que le processus répond, `/health/ready` vérifie aussi la base, et c'est celui qu'utilise App Service pour redémarrer une instance malade. Les journaux Serilog sont structurés, avec une ligne par requête HTTP. L'étape suivante serait de les envoyer à Application Insights, avec des métriques sur la durée d'un tick et le nombre de connexions SignalR.

### 25. Si vous deviez refaire le projet, que changeriez-vous ?

J'introduirais dès le départ un jeton de concurrence sur les trades et un calendrier de jours fériés : deux points faciles au début, plus coûteux une fois le modèle en place. Je découperais aussi le gros chunk d'AG Grid, qui pèse 1,2 Mo et n'est chargé que dans l'application de travail, mais qui la ralentit au premier affichage. En revanche, je garderais l'ordre de travail : domaine et tests d'abord, écrans ensuite. Il a permis de construire les pages sur des calculs déjà justes.
