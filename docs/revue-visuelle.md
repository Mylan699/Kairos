# Revue visuelle

Contrôle de chaque écran à 1440, 1280 et 390 px, en thème sombre et clair (42 captures), avec `npm run visual:review` sur le build de production. Le script fige le rejeu au 16 mars 2020 à 15:00 UTC pour que deux revues restent comparables et mesure ce qui peut l'être : débordement horizontal, tableaux et grilles qui défilent horizontalement sur un écran de bureau, couleur de fond par thème, chargement de Geist et Geist Mono, erreurs de console. Les captures sont ensuite examinées à l'œil et comparées à `brand/`.

## Défauts ouverts

Aucun.

## Défauts corrigés

| # | Écran | Largeur | Défaut | Correction |
|---|-------|---------|--------|------------|
| 1 | Landing | toutes | Le flux du héros restait vide tant que le rejeu ne bougeait pas. | Le flux s'ouvre sur les plus fortes variations depuis la clôture de la veille. |
| 2 | Barre haute, landing, rejeu | toutes | Date en « Lundi 16 Mars 2020 ». | Seul le jour prend une majuscule, dans `formatLongDate`. |
| 3 | Landing | 390 | Tableau « Fonctionnement » coupé à droite. | Lignes empilées sur téléphone. |
| 4 | Landing | 390 | Date du flux coupée au milieu de l'heure. | La date et l'heure occupent chacune une ligne. |
| 5 | Tableau de bord | 390 | Montants de la table des books sur deux lignes. | Colonne Valeur masquée sur téléphone, montants insécables. |
| 6 | Barre haute | 1440 | Icône de recherche écrasée. | Les icônes ne rétrécissent plus, bouton élargi. |
| 7 | Positions | 1440 | Colonnes Book et Secteur tronquées, en-tête coupé. | Secteur réservé aux grands écrans, colonnes Book et Coût moyen élargies. |
| 8 | Marchés | toutes | Étiquettes extrêmes de l'axe du temps coupées. | Bords de l'échelle de temps fixés. |
| 9 | Marchés | 1440 | Panneau Instruments décalé de 7 px. | Position collante exprimée en unités de la grille. |
| 10 | Marchés | 1440 | Page plus haute que l'écran. | Graphique ramené à 364 px. |
| 11 | Marchés | 390 | En-tête de la courbe des taux sur deux lignes. | Titre raccourci, valeurs insécables. |
| 12 | Marchés | toutes | Axe des prix avec un point décimal, le reste de l'écran avec une virgule. | L'axe utilise `formatPrice`. |
| 13 | Risques | 390 | Table des sensibilités plus large que son panneau. | Cellules resserrées, symbole Δ masqué sur téléphone. |
| 14 | Rejeu | toutes | Le curseur traversait « Séance de New York », qui sortait de sa zone à 390 px. | Étiquette dessinée au-dessus du curseur, raccourcie sur téléphone. |
| 15 | Rejeu | 390 | Titre « Journées remarquables » sur deux lignes. | Saisie d'une date libre réservée aux écrans plus larges. |
| 16 | Risques | 1280 | Colonne DV01 des sensibilités par book coupée. | Sous 1 344 px, les secteurs prennent deux colonnes et la table toute la largeur. |
| 17 | Positions | 1280 | Colonne Effet change hors de l'écran. | Coût moyen masqué sous 1 200 px de grille, colonne Book resserrée. |
| 18 | Blotter | 1280 | Colonne Statut coupée. | Classe (déjà proposée en filtre) et version masquées sous 1 200 px de grille. |

## Ce qui n'est pas un défaut

- La barre de navigation au milieu des captures de téléphone : elle est fixée en bas de l'écran et la capture pleine page la fige à cette hauteur.
- Le fond teinté de certains prix : c'est le flash d'un tick qui vient d'arriver, la couleur du texte ne change pas.

## Cohérence avec `brand/`

Couleurs de fond, accent, logo, Geist et Geist Mono conformes dans les deux thèmes. Le vert et le rouge restent réservés aux variations et au P&L.

## Lighthouse sur la landing

Performance 99, accessibilité 100, bonnes pratiques 100, SEO 100 (build de production, profil mobile).
