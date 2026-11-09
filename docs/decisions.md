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
