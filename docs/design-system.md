# Design system

Le design system de Kairos vit dans `web/src/design-system`. Il part de l'identité fournie dans `brand/` et n'invente rien qui la contredise : les couleurs de marque sont reprises à l'identique, le symbole n'est jamais redessiné, et un test compare sa géométrie aux fichiers d'origine.

Trois principes guident tout le reste :

1. **Les chiffres d'abord.** Un trader lit des colonnes de prix toute la journée : chaque nombre est en chasse fixe et aligné à droite, et l'interface s'efface derrière lui.
2. **La couleur a un sens.** Le vert et le rouge ne servent qu'aux variations et au P&L. Tout le reste est en neutres, avec un seul accent bleu.
3. **Le mouvement informe.** Une animation dit qu'une valeur a changé ou d'où vient un élément. Elle ne décore pas et ne fait jamais attendre.

## Couleurs

Les couleurs sont des variables CSS définies dans `tokens.css`, puis exposées à Tailwind sous des noms sémantiques. La palette par défaut de Tailwind est supprimée : une couleur qui n'est pas un token ne peut pas être utilisée.

| Token | Sombre | Clair | Usage |
|-------|--------|-------|-------|
| `canvas` | `#0B0E14` | `#F5F6F9` | Fond de page (couleur de marque en sombre) |
| `surface-1` | `#10141C` | `#FAFBFC` | Barre latérale, barre haute |
| `surface-2` | `#151A24` | `#FFFFFF` | Panneaux (couleur de marque en sombre) |
| `surface-3` | `#1C2230` | `#EFF1F6` | Survol, en-têtes collants |
| `surface-4` | `#252C3C` | `#E4E8EF` | Éléments en relief, squelettes de chargement |
| `line-subtle`, `line`, `line-strong` | `#1C222E`, `#2A3242`, `#3B4558` | `#E9ECF2`, `#D9DEE7`, `#B7BFCC` | Séparateurs, bordures, axes |
| `ink` | `#E8ECF4` | `#0B0E14` | Texte principal (couleur de marque en sombre) |
| `ink-2` | `#8A93A6` | `#566074` | Texte secondaire (couleur de marque en sombre) |
| `ink-3` | `#7D869A` | `#667085` | Libellés, légendes |
| `accent` | `#4F7BFF` | `#3461F0` | Action principale, sélection, courbe en direct (couleurs de marque) |
| `gain` | `#34D399` | `#067A4E` | Hausse, P&L positif |
| `loss` | `#FF7A7A` | `#C22F3A` | Baisse, P&L négatif |
| `caution` | `#E9B25A` | `#8F5A00` | Signal d'attention qui n'est pas un P&L (trade annulé) |
| `idle` | `#626B7E` | `#8A93A6` | Flux déconnecté, élément inactif |

Les variantes `-soft` (accent, gain, loss, caution) sont les mêmes teintes en transparence, pour les fonds : sélection, flash d'un prix, badge.

Règles :

- Le vert et le rouge sont réservés aux variations et au P&L. Un trade annulé n'est pas rouge, un trade réglé n'est pas vert : un trade nouveau est en accent, validé ou réglé en neutres, annulé en `caution`.
- Le thème sombre est le thème par défaut, comme sur un vrai poste de trading. Le thème clair a ses propres valeurs, choisies pour garder un contraste AA, et n'est pas une inversion automatique.
- Les deux thèmes changent sans rechargement : les graphiques relisent les tokens au changement de thème.

## Typographie

| Police | Usage |
|--------|-------|
| Geist (variable) | Interface, titres, wordmark |
| Geist Mono (variable) | Tous les chiffres, libellés de section, signature |

Les deux polices sont servies avec l'application (`@fontsource-variable`), sans appel à un service externe.

Échelle, avec une taille de base de 14 px, celle des interfaces denses :

| Classe | Taille / interligne | Usage |
|--------|---------------------|-------|
| `text-2xs` | 11 / 16 px | Mentions, légendes |
| `text-xs` | 12 / 16 px | Cellules de tableau, contrôles |
| `text-sm` | 13 / 20 px | Texte courant de l'application |
| `text-base` | 14 / 20 px | Texte de la landing et des fenêtres |
| `text-lg` | 16 / 24 px | Chiffres mis en avant |
| `text-xl` | 20 / 28 px | Titres du ticket et du détail d'un trade |
| `text-2xl`, `text-3xl` | 28 / 36, 40 / 44 px | Grands chiffres : P&L du jour, résultat d'un stress |
| `text-4xl` | 64 / 68 px | Titre principal de la landing |

Deux utilitaires portent l'identité dans toute l'interface :

- `num` : Geist Mono en chiffres tabulaires. Chaque chiffre a la même largeur, donc une colonne de prix ne saute pas quand les valeurs changent.
- `label` : Geist Mono en capitales, 11 px, espacement de 0,12 em, en `ink-3`. Il fait écho à la signature de la marque sur chaque titre de panneau.

Les nombres suivent la typographie française : espace insécable pour les milliers, virgule décimale, signe moins typographique, symbole après le montant (`−1 093 279 €`). Tout passe par `web/src/lib/format.ts`, y compris l'axe des graphiques.

## Logo

| Élément | Règle |
|---------|-------|
| Symbole | Un K dont la branche haute est une courbe de prix qui monte, le point final étant le dernier tick. Fichiers `brand/kairos-mark-dark.svg` (fond sombre), `-light.svg` (fond clair), `-mono.svg` (couleur du texte). |
| Wordmark | « Kairos » en Geist SemiBold, espacement −0,04 em, rendu en texte et non en image. |
| Signature | « CROSS-ASSET TRADING DESK » en Geist Mono, capitales, espacement 0,32 em. |
| Favicon | `brand/favicon.svg`, version simplifiée lisible en 16 px. Les icônes PNG, l'icône Apple et l'image Open Graph sont générées à partir de lui et du symbole sombre (`npm run generate:brand`). |

Le composant `KairosMark` reprend la géométrie exacte des fichiers SVG et propose trois états :

- `still` : le symbole seul, sur les écrans statiques ;
- `draw` : la courbe se trace puis le dernier tick apparaît avec une légère pulsation. Il est utilisé une seule fois, sur le titre de la landing ;
- `live` : le dernier tick respire tant que le flux temps réel est connecté et devient gris s'il se coupe. Il est utilisé dans la barre latérale et la barre haute : le logo devient un voyant de connexion.

À ne pas faire : déformer ou recolorer le symbole, l'utiliser sous 16 px (passer au favicon), poser la version sombre sur un fond clair, écrire le wordmark dans une autre police.

## Composants

| Composant | Rôle |
|-----------|------|
| `Panel` | Le conteneur de base : titre en `label`, zone d'actions à droite, contenu |
| `Button`, `IconButton` | Actions : primaire (accent), secondaire, discrète |
| `TextField`, `Select`, `SegmentedControl` | Saisie, avec un libellé associé à chaque champ et un message d'erreur sous le champ |
| `Badge` | Statut d'un trade |
| `Drawer`, `Modal` | Ticket de trade et détail d'un trade, fermés par Échap, focus piégé |
| `ToastProvider` | Confirmation d'une action, erreur d'API |
| `EmptyState`, `ErrorState`, `Skeleton` | Ce que l'on montre quand il n'y a rien, quand ça échoue, pendant le chargement |
| `FlashValue`, `RollingNumber` | Chiffres en direct (voir plus bas) |
| `Sparkline`, `BarList` | Petits graphiques de tendance et barres de contribution |
| `StaggerGroup` | Apparition décalée des panneaux d'un écran |
| `KairosGrid` | AG Grid habillé avec les tokens : blotter, positions |

Les graphiques de prix utilisent TradingView Lightweight Charts, la courbe des taux est dessinée en SVG. Tous lisent leurs couleurs dans les tokens.

## Mouvement

Toutes les durées et courbes sont définies dans `motion.ts`, lues à la fois par les composants Motion et par le CSS sous forme de variables.

| Token | Valeur | Usage |
|-------|--------|-------|
| `fast` | 150 ms | Survol, sortie d'un élément |
| `base` | 200 ms | Ouverture d'un menu, changement d'onglet |
| `slow` | 250 ms | Entrée d'un panneau, d'une ligne |
| `flash` | 700 ms | Fond coloré d'un prix qui vient de bouger |
| `highlight` | 1 s | Surbrillance d'un trade qui vient d'arriver dans le blotter |
| `draw` | 900 ms | Tracé d'un graphique ou du logo |

| Courbe | Valeur | Usage |
|--------|--------|-------|
| `standard` | `cubic-bezier(0.2, 0, 0, 1)` | Par défaut |
| `enter` | `cubic-bezier(0, 0, 0.2, 1)` | Un élément arrive et ralentit |
| `exit` | `cubic-bezier(0.4, 0, 1, 1)` | Un élément part et accélère |
| `spring.layout` | ressort (raideur 480, amortissement 38) | Réorganisation d'une liste : un mouvement interrompu reste continu |

Principes :

- **Les micro-interactions restent entre 150 et 250 ms.** Au-delà, l'interface paraît lente.
- **Un prix qui bouge flashe, il ne clignote pas.** `FlashValue` colore brièvement le fond en vert ou en rouge, puis revient au neutre. La couleur du texte ne change pas, et l'animation passe par l'API Web Animations : toute une liste de prix ne se redessine pas pour autant.
- **Les chiffres roulent.** `RollingNumber` fait défiler chaque chiffre vers sa nouvelle valeur, en comptant les positions depuis la droite pour que les unités restent sous les unités.
- **Un nouvel élément vient de quelque part.** Une ligne du blotter ou du flux de la landing glisse de 12 px depuis la gauche et reste surlignée un instant.
- **On n'attend qu'une fois.** L'apparition décalée des panneaux ne se joue qu'à la première visite d'un écran.
- **Le chargement a la forme du contenu.** Les squelettes reprennent la taille de ce qu'ils remplacent et une lumière les traverse, sans roue qui tourne.
- **Mouvement réduit respecté.** Avec `prefers-reduced-motion`, les flashs, la pulsation du logo et les déplacements disparaissent. L'information reste là, seule l'animation s'en va.

## Accessibilité

- Contraste AA visé dans les deux thèmes. Sur la landing, Lighthouse donne 100 en accessibilité.
- Focus clavier visible partout : contour de 2 px en couleur d'accent.
- Toute l'application se pilote au clavier : `Ctrl+K` ouvre la palette de commandes, `B` et `S` ouvrent le ticket en achat ou en vente, `Échap` ferme, `?` affiche les raccourcis.
- Les chiffres animés gardent un libellé lisible par les lecteurs d'écran, et le symbole décoratif est masqué quand le texte à côté dit déjà « Kairos ».
