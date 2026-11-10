# Déploiement

Deux façons de lancer Kairos : en local avec Docker Compose, ou en ligne sur Azure App Service avec Azure SQL. Dans les deux cas, c'est la même image : l'API ASP.NET sert aussi le front (ADR-012).

## En local avec Docker Compose

Prérequis : Docker Desktop, et les fichiers de marché dans `data/raw/` (voir [`donnees.md`](donnees.md)) :

```
data/raw/eurusd/eurusd_minute.csv
data/raw/sp500/sp500_stocks.csv
data/raw/sp500/sp500_companies.csv
data/raw/treasury/us_treasury_yields_daily.csv
```

```powershell
docker compose up --build
```

L'application répond sur <http://localhost:8000>, Swagger sur <http://localhost:8000/swagger>.

Ce qui se passe au premier démarrage :

1. SQL Server démarre. L'application attend que son contrôle de santé réponde.
2. L'API applique les migrations EF Core.
3. Elle importe les fichiers de `./data/raw`, montés en lecture seule. Comptez quelques minutes pour 3,5 millions de barres minute.
4. Elle crée le référentiel (instruments, books, émissions obligataires) et génère les trades de départ.
5. Le rejeu démarre sur la journée par défaut.

La base vit dans le volume `kairos_sql-data` : les démarrages suivants sautent l'import et prennent quelques secondes. Pour repartir de zéro :

```powershell
docker compose down --volumes
```

Le mot de passe SQL du compose est un mot de passe de développement local. Pour le changer, créez un fichier `.env` à la racine (non versionné) avec `KAIROS_SQL_PASSWORD=...`.

## En ligne sur Azure

Architecture cible :

| Ressource | Rôle | Taille conseillée |
|-----------|------|-------------------|
| Azure Container Registry | Stocke l'image | Basic |
| Azure SQL Database | Base Kairos (environ 150 Mo) | Basic (5 DTU), S2 le temps de l'import |
| App Service Linux | Exécute l'image, WebSockets activés | B1, une seule instance |

Une seule instance, car le rejeu partagé vit en mémoire dans le processus (ADR-011). `Always On` doit rester activé : sans lui, App Service endort l'application et le rejeu s'arrête avec elle.

Les commandes ci-dessous sont écrites pour PowerShell avec [Azure CLI](https://learn.microsoft.com/cli/azure/install-azure-cli). Les noms de registre, de serveur SQL et d'application doivent être uniques dans tout Azure : remplacez `kairosdemo` par un préfixe à vous.

### 1. Variables et groupe de ressources

```powershell
az login

$Location  = "francecentral"
$Group     = "kairos-rg"
$Registry  = "kairosdemoacr"
$SqlServer = "kairosdemo-sql"
$SqlAdmin  = "kairosadmin"
$SqlPassword = Read-Host "Mot de passe administrateur SQL"
$Plan      = "kairos-plan"
$App       = "kairosdemo"
$SiteUrl   = "https://$App.azurewebsites.net"

az group create --name $Group --location $Location
```

Choisissez un mot de passe SQL d'au moins 12 caractères sans `;` ni `$`, qui casseraient la chaîne de connexion ou la commande.

### 2. Construire l'image dans le registre

```powershell
az acr create --resource-group $Group --name $Registry --sku Basic
az acr build --registry $Registry --image kairos:1.0 --build-arg SITE_URL=$SiteUrl .
```

`az acr build` envoie le dossier au registre et construit l'image dans Azure. Le fichier `.dockerignore` écarte `data/`, les tests et les dossiers de build : les fichiers de marché ne quittent pas le poste à cette étape. `SITE_URL` sert aux liens absolus de l'image Open Graph.

### 3. Créer la base

```powershell
az sql server create --resource-group $Group --name $SqlServer --location $Location --admin-user $SqlAdmin --admin-password $SqlPassword
az sql db create --resource-group $Group --server $SqlServer --name Kairos --service-objective S2

# Accès depuis les services Azure (l'App Service)
az sql server firewall-rule create --resource-group $Group --server $SqlServer --name AllowAzureServices --start-ip-address 0.0.0.0 --end-ip-address 0.0.0.0

# Accès temporaire depuis votre poste, pour l'import
$MyIp = (Invoke-RestMethod https://api.ipify.org)
az sql server firewall-rule create --resource-group $Group --server $SqlServer --name ImportFromMyMachine --start-ip-address $MyIp --end-ip-address $MyIp

$ConnectionString = "Server=tcp:$SqlServer.database.windows.net,1433;Database=Kairos;User ID=$SqlAdmin;Password=$SqlPassword;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30"
```

### 4. Importer les données depuis le poste

L'image locale applique les migrations, importe les fichiers, crée le référentiel et les trades de départ, puis s'arrête grâce à `--import-only`.

```powershell
docker compose build app
docker run --rm -v "${PWD}/data/raw:/data/raw:ro" -e "ConnectionStrings__Kairos=$ConnectionString" -e "MarketData__RawPath=/data/raw" kairos-app --import-only
```

Une fois l'import terminé, redescendez la base en Basic et fermez l'accès du poste :

```powershell
az sql db update --resource-group $Group --server $SqlServer --name Kairos --service-objective Basic
az sql server firewall-rule delete --resource-group $Group --server $SqlServer --name ImportFromMyMachine
```

### 5. Créer l'App Service

```powershell
az appservice plan create --resource-group $Group --name $Plan --is-linux --sku B1
az webapp create --resource-group $Group --plan $Plan --name $App --container-image-name "$Registry.azurecr.io/kairos:1.0"
```

L'application récupère l'image avec son identité managée plutôt qu'avec un mot de passe de registre :

```powershell
$Principal = az webapp identity assign --resource-group $Group --name $App --query principalId --output tsv
$RegistryId = az acr show --name $Registry --query id --output tsv
az role assignment create --assignee $Principal --scope $RegistryId --role AcrPull

$WebConfigId = az webapp config show --resource-group $Group --name $App --query id --output tsv
az resource update --ids $WebConfigId --set properties.acrUseManagedIdentityCreds=True
```

### 6. Configurer l'application

```powershell
az webapp config set --resource-group $Group --name $App --web-sockets-enabled true --always-on true
az webapp update --resource-group $Group --name $App --https-only true
az resource update --ids $WebConfigId --set properties.healthCheckPath=/health/ready

az webapp config appsettings set --resource-group $Group --name $App --settings WEBSITES_PORT=8080 "ConnectionStrings__Kairos=$ConnectionString"

az webapp restart --resource-group $Group --name $App
```

Toute la configuration passe par des variables d'environnement, avec `__` pour descendre dans les sections de `appsettings.json` :

| Variable | Rôle | Valeur par défaut |
|----------|------|-------------------|
| `ConnectionStrings__Kairos` | Base SQL | aucune, obligatoire |
| `MarketData__RawPath` | Fichiers à importer s'ils manquent en base | aucune (inutile sur Azure) |
| `WEBSITES_PORT` | Port écouté par le conteneur, pour App Service | 8080 |
| `RateLimiting__ReadPermitsPerMinute` | Lectures par minute et par adresse | 600 |
| `RateLimiting__WritePermitsPerMinute` | Écritures par minute et par adresse | 60 |
| `Replay__NightlyResetHourUtc` | Heure UTC de la remise à zéro des trades | 3 |
| `Replay__IdleResetMinutes` | Retour à la journée par défaut sans commande | 20 |
| `Cors__AllowedOrigins__0` | Origine autorisée si le front est hébergé ailleurs | aucune |

### 7. Vérifier

```powershell
Invoke-RestMethod "$SiteUrl/health/ready"
Start-Process $SiteUrl
```

- `/health/ready` répond `Healthy` : l'application voit la base.
- Sur l'écran de rejeu, l'heure avance : la connexion SignalR passe bien par WebSocket. Dans les outils de développement, l'onglet Réseau montre une requête `/hubs/market` en statut 101.
- Les journaux du conteneur :

```powershell
az webapp log config --resource-group $Group --name $App --docker-container-logging filesystem
az webapp log tail --resource-group $Group --name $App
```

### Mettre à jour

```powershell
az acr build --registry $Registry --image kairos:1.1 --build-arg SITE_URL=$SiteUrl .
az webapp config container set --resource-group $Group --name $App --container-image-name "$Registry.azurecr.io/kairos:1.1"
```

Les migrations EF Core s'appliquent au démarrage. Les données de marché restent en base : l'import n'est pas rejoué.

### Supprimer

```powershell
az group delete --name $Group
```

## Pistes pour aller plus loin

- Remplacer le mot de passe SQL par l'identité managée de l'App Service (authentification Microsoft Entra), ou au minimum par une référence Key Vault.
- Publier l'image depuis GitHub Actions à chaque tag plutôt qu'avec `az acr build` à la main.
- Ajouter un domaine personnalisé, puis reconstruire l'image avec ce `SITE_URL`.
