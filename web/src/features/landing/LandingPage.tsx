import { lazy, Suspense, useEffect, useState, type ReactNode } from 'react';
import { Link } from 'react-router';
import { IconButton } from '@/design-system/components/Button';
import { MoonIcon, SunIcon } from '@/design-system/components/Icons';
import { KairosLogo } from '@/design-system/brand/KairosLogo';
import { KairosMark } from '@/design-system/brand/KairosMark';
import { setTheme, useTheme } from '@/design-system/theme';

// The live feed brings SignalR with it: it is only fetched once the page is on screen.
const LiveFeed = lazy(() => import('./LiveFeed'));

const PRIMARY_LINK =
  'inline-flex h-10 items-center rounded-md bg-accent px-4 text-sm font-medium text-on-accent transition-colors duration-[var(--motion-fast)] ease-standard hover:bg-accent-hover';

const SECONDARY_LINK =
  'inline-flex h-10 items-center rounded-md border border-line px-4 text-sm font-medium text-ink transition-colors duration-[var(--motion-fast)] ease-standard hover:border-line-strong hover:bg-surface-3';

const FIGURES: ReadonlyArray<[string, string]> = [
  ['2 567', 'journées de marché, de 2010 à 2020'],
  ['3,5 M', 'minutes EUR/USD bid et ask réelles'],
  ['53', 'instruments : actions, change, obligations'],
  ['500+', 'tests automatisés, du pricing aux écrans'],
];

const FEATURES: ReadonlyArray<[string, string]> = [
  ['Blotter et cycle de vie', 'Saisie, validation, règlement, annulation. Une machine à états refuse les transitions interdites, chaque amendement crée une version et tout est tracé dans une piste d’audit.'],
  ['Positions en euros', 'Coût moyen pondéré, P&L réalisé et latent, et séparation exacte entre ce qui vient du prix de l’actif et ce qui vient du change.'],
  ['Risques et stress tests', 'Delta actions, delta change, DV01. Chocs paramétrables ou variations réellement observées pendant le krach de 2020 ou le Brexit, par revalorisation complète.'],
  ['Marchés', 'Graphiques en minute et en quotidien, courbe des taux du Trésor américain qui se déforme en direct, comparée à deux dates passées.'],
  ['Machine à remonter le temps', 'Toute l’application vit à une date simulée. Le serveur rejoue la journée en accéléré, sans jamais laisser voir le futur.'],
  ['Un vrai poste de travail', 'Palette de commandes Ctrl K, raccourcis clavier, ticket de saisie avec bid et ask vivants et estimation de l’impact avant validation.'],
];

const FLOWS: ReadonlyArray<[string, string, string]> = [
  ['Actions', 'Cours de marché en dollars', 'Converti au mid EUR/USD'],
  ['Change', 'EUR/USD au bid ou à l’ask selon le sens', 'Le gain en dollars est converti'],
  ['Obligations', 'Flux actualisés sur la courbe des taux', 'Prix clean, duration et DV01, puis conversion'],
];

const LEDGER: ReadonlyArray<[string, string, string?]> = [
  ['Achat de 100 actions à 45,00 $, 1 € = 1,125 $', '4 000,00 €'],
  ['Cours actuel 50,00 $, 1 € = 1,100 $', '4 545,45 €'],
  ['Effet prix : 100 × 5,00 $ ÷ 1,10', '+454,55 €', 'text-gain'],
  ['Effet change : 100 × (45 ÷ 1,10 − 40,00 €)', '+90,91 €', 'text-gain'],
];

const LAYERS: ReadonlyArray<[string, string]> = [
  ['Kairos.Domain', 'Pricing, P&L, machine à états, stress. Aucune dépendance externe.'],
  ['Kairos.Application', 'Cas d’usage, validation, ports. Ne connaît ni SQL ni SignalR.'],
  ['Kairos.Infrastructure', 'EF Core, import des données, moteur de rejeu en BackgroundService.'],
  ['Kairos.Api', 'Contrôleurs, hub SignalR, ProblemDetails, rate limiting, health checks.'],
];

const STACK = [
  'C# .NET 10', 'ASP.NET Core', 'EF Core', 'SQL Server', 'SignalR', 'FluentValidation', 'Serilog', 'xUnit', 'Testcontainers',
  'React 19', 'TypeScript', 'TanStack Query', 'React Router', 'Tailwind CSS 4', 'Motion', 'AG Grid', 'Lightweight Charts', 'cmdk', 'Vitest', 'Playwright', 'Docker',
];

function Section({ id, label, title, children }: { id: string; label: string; title: string; children: ReactNode }) {
  return (
    <section id={id} className="border-t border-line-subtle">
      <div className="mx-auto grid max-w-6xl gap-8 px-4 py-16 md:px-6 md:py-24 lg:grid-cols-[minmax(0,4fr)_minmax(0,8fr)]">
        <div>
          <p className="label">{label}</p>
          <h2 className="mt-3 text-2xl font-semibold tracking-tight text-ink">{title}</h2>
        </div>
        <div className="min-w-0">{children}</div>
      </div>
    </section>
  );
}

/** Waits until the browser has finished the initial work, so that the feed never delays the first paint. */
function useAfterFirstPaint(): boolean {
  const [ready, setReady] = useState(false);

  useEffect(() => {
    const timer = window.setTimeout(() => setReady(true), 300);
    return () => window.clearTimeout(timer);
  }, []);

  return ready;
}

export default function LandingPage() {
  const theme = useTheme();
  const showFeed = useAfterFirstPaint();

  return (
    <div className="min-h-dvh bg-canvas text-ink">
      <header className="sticky top-0 z-30 border-b border-line-subtle bg-canvas/90 backdrop-blur">
        <div className="mx-auto flex h-14 max-w-6xl items-center justify-between gap-4 px-4 md:px-6">
          <KairosLogo markSize={26} />
          <nav aria-label="Sections de la page" className="hidden items-center gap-6 text-sm text-ink-2 md:flex">
            <a className="hover:text-ink" href="#fonctionnalites">Fonctionnalités</a>
            <a className="hover:text-ink" href="#fonctionnement">Fonctionnement</a>
            <a className="hover:text-ink" href="#architecture">Architecture</a>
          </nav>
          <div className="flex items-center gap-2">
            <IconButton
              label={theme === 'dark' ? 'Passer au thème clair' : 'Passer au thème sombre'}
              onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')}
            >
              {theme === 'dark' ? <SunIcon /> : <MoonIcon />}
            </IconButton>
            <Link to="/app" className={PRIMARY_LINK.replace('h-10', 'h-8').replace('px-4', 'px-3')}>Lancer la démo</Link>
          </div>
        </div>
      </header>

      <main>
        <section className="mx-auto grid max-w-6xl items-center gap-10 px-4 pb-16 pt-12 md:px-6 md:pb-24 md:pt-20 lg:grid-cols-[minmax(0,6fr)_minmax(0,6fr)]">
          <div>
            <KairosMark size={72} animation="draw" title="Symbole Kairos" className="-ml-3" />
            <p className="mt-6 font-mono text-[11px] uppercase tracking-[0.32em] text-ink-2">Cross-asset trading desk</p>
            <h1 className="mt-4 text-3xl font-semibold tracking-tight sm:text-4xl sm:leading-[1.05]">Trois marchés.<br />Un seul P&amp;L.</h1>
            <p className="mt-5 max-w-lg text-lg text-ink-2">
              Kairos réunit actions, change et taux dans un poste de trading consolidé en euros, puis rejoue minute par minute les journées qui ont secoué les marchés.
            </p>
            <div className="mt-8 flex flex-wrap gap-3">
              <Link to="/app" className={PRIMARY_LINK}>Lancer la démo</Link>
              <a href="#fonctionnement" className={SECONDARY_LINK}>Comprendre le calcul</a>
            </div>
          </div>

          <div className="min-h-[372px] overflow-hidden rounded-lg border border-line bg-surface-2">
            {showFeed && (
              <Suspense>
                <LiveFeed />
              </Suspense>
            )}
          </div>
        </section>

        <section aria-label="Quelques chiffres" className="border-t border-line-subtle">
          <dl className="mx-auto grid max-w-6xl grid-cols-2 gap-x-6 gap-y-8 px-4 py-10 md:px-6 lg:grid-cols-4">
            {FIGURES.map(([value, label]) => (
              <div key={value}>
                <dd className="num text-2xl font-medium text-ink">{value}</dd>
                <dt className="mt-1 text-sm text-ink-2">{label}</dt>
              </div>
            ))}
          </dl>
        </section>

        <Section id="fonctionnalites" label="Fonctionnalités" title="Ce qu'un desk attend de son poste.">
          <ol className="border-b border-line-subtle">
            {FEATURES.map(([name, description], index) => (
              <li key={name} className="grid gap-1 border-t border-line-subtle py-5 sm:grid-cols-[3rem_minmax(0,14rem)_minmax(0,1fr)] sm:gap-4">
                <span className="num text-sm text-ink-3">{String(index + 1).padStart(2, '0')}</span>
                <h3 className="text-base font-medium text-ink">{name}</h3>
                <p className="text-sm leading-6 text-ink-2">{description}</p>
              </li>
            ))}
          </ol>
        </Section>

        <Section id="fonctionnement" label="Fonctionnement" title="Trois actifs, une seule devise de reporting.">
          <p className="max-w-2xl text-base leading-7 text-ink-2">
            Tout est coté en dollars, tout est reporté en euros. L'EUR/USD n'est donc pas un marché parmi d'autres : c'est lui qui relie les trois.
          </p>

          <div className="mt-8 overflow-x-auto rounded-md border border-line-subtle">
            <table className="w-full min-w-[540px] text-left text-sm">
              <thead>
                <tr className="border-b border-line-subtle">
                  <th scope="col" className="label px-4 py-2.5 font-normal">Actif</th>
                  <th scope="col" className="label px-4 py-2.5 font-normal">Valorisation</th>
                  <th scope="col" className="label px-4 py-2.5 font-normal">Vers l'euro</th>
                </tr>
              </thead>
              <tbody>
                {FLOWS.map(([asset, valuation, conversion]) => (
                  <tr key={asset} className="border-b border-line-subtle last:border-b-0">
                    <th scope="row" className="px-4 py-3 font-medium text-ink">{asset}</th>
                    <td className="px-4 py-3 text-ink-2">{valuation}</td>
                    <td className="px-4 py-3 text-ink-2">{conversion}</td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>

          <h3 className="label mt-10">Exemple : effet prix et effet change</h3>
          <dl className="mt-3 rounded-md border border-line-subtle">
            {LEDGER.map(([label, amount, tone]) => (
              <div key={label} className="flex items-baseline justify-between gap-4 border-b border-line-subtle px-4 py-2.5 last:border-b-0">
                <dt className="text-sm text-ink-2">{label}</dt>
                <dd className={`num whitespace-nowrap text-sm ${tone ?? 'text-ink'}`}>{amount}</dd>
              </div>
            ))}
            <div className="flex items-baseline justify-between gap-4 border-t border-line px-4 py-3">
              <dt className="text-sm font-medium text-ink">P&amp;L latent</dt>
              <dd className="num whitespace-nowrap text-base font-medium text-gain">+545,45 €</dd>
            </div>
          </dl>
        </Section>

        <Section id="architecture" label="Architecture" title="Une logique métier qui ne dépend de rien.">
          <ol className="flex flex-col gap-2">
            {LAYERS.map(([name, description]) => (
              <li key={name} className="grid gap-1 rounded-md border border-line-subtle bg-surface-1 px-4 py-3 sm:grid-cols-[13rem_minmax(0,1fr)] sm:gap-4">
                <span className="num text-sm text-ink">{name}</span>
                <span className="text-sm text-ink-2">{description}</span>
              </li>
            ))}
          </ol>
          <p className="mt-4 text-sm leading-6 text-ink-2">
            Les dépendances pointent vers le domaine. Le rejeu suit un chemin simple : fichiers historiques, SQL Server, moteur de rejeu, SignalR, écran.
          </p>

          <h3 className="label mt-10">Stack</h3>
          <ul className="mt-3 flex flex-wrap gap-1.5">
            {STACK.map((item) => (
              <li key={item} className="num rounded-sm border border-line-subtle px-2 py-1 text-xs text-ink-2">{item}</li>
            ))}
          </ul>
        </Section>

        <section className="border-t border-line-subtle">
          <div className="mx-auto flex max-w-6xl flex-col items-start gap-5 px-4 py-16 md:px-6 md:py-24">
            <h2 className="text-2xl font-semibold tracking-tight">Rejouez le 16 mars 2020.</h2>
            <p className="max-w-xl text-base leading-7 text-ink-2">
              La pire séance de la période, minute par minute, avec le P&amp;L qui se recalcule devant vous.
            </p>
            <Link to="/app" className={PRIMARY_LINK}>Lancer la démo</Link>
          </div>
        </section>
      </main>

      <footer className="border-t border-line-subtle">
        <div className="mx-auto flex max-w-6xl flex-col gap-3 px-4 py-8 text-xs leading-5 text-ink-3 md:px-6">
          <p className="text-ink-2">
            Projet de démonstration sur données historiques, aucune donnée réelle de client, pas de conseil en investissement.
          </p>
          <p>
            Données : S&amp;P 500 Stocks (CC0), EUR/USD Forex Pair Historical Data (AGPL-3.0), U.S. Treasury Yields (CC BY-NC 4.0), via Kaggle. Traders et contreparties fictifs.
            Graphiques réalisés avec{' '}
            <a className="underline hover:text-ink" href="https://www.tradingview.com/lightweight-charts/" target="_blank" rel="noreferrer">TradingView Lightweight Charts</a>.
          </p>
        </div>
      </footer>
    </div>
  );
}
