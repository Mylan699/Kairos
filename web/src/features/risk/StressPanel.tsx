import { useMutation } from '@tanstack/react-query';
import { useEffect, useRef, useState, type FormEvent } from 'react';
import { useSearchParams } from 'react-router';
import { apiPost, describeError } from '@/api/client';
import { useScenarios } from '@/api/queries';
import type { HistoricalScenario, StressResult } from '@/api/types';
import { BarList, Button, cx, EmptyState, Panel, RollingNumber, SegmentedControl, Skeleton, TextField, useToast } from '@/design-system';
import { formatCompactEur, formatDate, formatSignedEur, trendClass } from '@/lib/format';
import { assetClassKeyLabel, validateShocks, type ShockFields } from './riskModel';

const MODES = [
  { value: 'parametric', label: 'Paramétrable' },
  { value: 'historical', label: 'Historique' },
] as const;

type Mode = (typeof MODES)[number]['value'];

const PRESETS: ReadonlyArray<{ label: string; fields: ShockFields }> = [
  { label: 'Actions -10 %', fields: { equity: '-10', eurUsd: '', rates: '' } },
  { label: 'EUR/USD -5 %', fields: { equity: '', eurUsd: '-5', rates: '' } },
  { label: 'Taux +100 pb', fields: { equity: '', eurUsd: '', rates: '100' } },
  { label: 'Les trois combinés', fields: { equity: '-10', eurUsd: '-5', rates: '100' } },
];

type StressCall = { kind: 'parametric'; body: unknown } | { kind: 'historical'; body: unknown };

function ResultView({ result, isPending }: { result: StressResult | undefined; isPending: boolean }) {
  if (isPending && !result) {
    return (
      <div className="flex flex-col gap-3" aria-busy="true">
        <Skeleton className="h-11 w-56" />
        <Skeleton className="h-28 w-full" />
      </div>
    );
  }

  if (!result) {
    return <EmptyState title="Aucun scénario lancé" description="Choisissez un choc pour voir son impact sur le portefeuille, en euros." />;
  }

  return (
    <div className="flex flex-col gap-4">
      <div>
        <p className="text-xs text-ink-2">{result.scenario}</p>
        <RollingNumber text={formatSignedEur(result.totalEur, 0)} className={cx('text-2xl font-medium', trendClass(result.totalEur))} />
      </div>
      <div className="grid gap-4 md:grid-cols-2">
        <div>
          <h3 className="label mb-2">Par book</h3>
          <BarList
            tone="pnl"
            labelWidth="8.5rem"
            items={result.byBook.map((line) => ({ key: line.bookId, label: line.book, value: line.eur, valueText: formatCompactEur(line.eur) }))}
          />
        </div>
        <div>
          <h3 className="label mb-2">Par classe d'actifs</h3>
          <BarList
            tone="pnl"
            labelWidth="5.5rem"
            items={result.byAssetClass.map((line) => ({ key: line.key, label: assetClassKeyLabel(line.key), value: line.eur, valueText: formatCompactEur(line.eur) }))}
          />
        </div>
      </div>
    </div>
  );
}

function ScenarioList({ scenarios, selectedId, onRun }: { scenarios: HistoricalScenario[]; selectedId: string | null; onRun: (scenario: HistoricalScenario) => void }) {
  return (
    <ul className="flex flex-col gap-1">
      {scenarios.map((scenario) => (
        <li key={scenario.id}>
          <button
            type="button"
            aria-pressed={scenario.id === selectedId}
            onClick={() => onRun(scenario)}
            className={cx(
              'flex w-full flex-col gap-0.5 rounded-md border px-3 py-2 text-left transition-colors duration-[var(--motion-fast)]',
              scenario.id === selectedId ? 'border-accent bg-accent-soft' : 'border-line-subtle hover:border-line-strong',
            )}
          >
            <span className="flex items-baseline justify-between gap-2">
              <span className="text-sm font-medium text-ink">{scenario.name}</span>
              <span className="num whitespace-nowrap text-2xs text-ink-3">{formatDate(scenario.from)} → {formatDate(scenario.to)}</span>
            </span>
            <span className="text-xs text-ink-2">{scenario.description}</span>
          </button>
        </li>
      ))}
    </ul>
  );
}

export function StressPanel() {
  const { notify } = useToast();
  const { data: scenarios } = useScenarios();
  const [searchParams] = useSearchParams();
  const requestedScenario = searchParams.get('scenario');

  const [mode, setMode] = useState<Mode>(requestedScenario ? 'historical' : 'parametric');
  const [fields, setFields] = useState<ShockFields>({ equity: '-10', eurUsd: '-5', rates: '100' });
  const [selectedId, setSelectedId] = useState<string | null>(null);

  const stress = useMutation({
    mutationFn: ({ kind, body }: StressCall) => apiPost<StressResult>(`/api/risk/stress/${kind}`, body),
    onError: (error) => notify({ tone: 'error', title: 'Scénario impossible', description: describeError(error) }),
  });

  const { request, errors } = validateShocks(fields);

  const runScenario = (scenario: HistoricalScenario) => {
    setSelectedId(scenario.id);
    stress.mutate({ kind: 'historical', body: { from: scenario.from, to: scenario.to, name: scenario.name } });
  };

  const runRef = useRef(runScenario);

  useEffect(() => {
    runRef.current = runScenario;
  });

  // A scenario chosen from the command palette runs as soon as its data is available.
  useEffect(() => {
    const scenario = scenarios?.find((item) => item.id === requestedScenario);
    if (scenario) {
      runRef.current(scenario);
    }
  }, [scenarios, requestedScenario]);

  const onSubmit = (event: FormEvent) => {
    event.preventDefault();
    if (request) {
      setSelectedId(null);
      stress.mutate({ kind: 'parametric', body: request });
    }
  };

  const setField = (name: keyof ShockFields, value: string) => setFields((current) => ({ ...current, [name]: value }));

  return (
    <Panel title="Stress tests" aside={<SegmentedControl label="Type de scénario" value={mode} segments={MODES} onChange={setMode} />}>
      <div className="grid gap-4 lg:grid-cols-[minmax(0,5fr)_minmax(0,7fr)]">
        <div className="lg:max-h-80 lg:overflow-y-auto lg:pr-1">
          {mode === 'parametric' ? (
            <form onSubmit={onSubmit} className="flex flex-col gap-3">
              <div className="grid grid-cols-3 gap-2">
                <TextField label="Actions (%)" numeric value={fields.equity} error={errors.equity} onChange={(event) => setField('equity', event.target.value)} autoComplete="off" />
                <TextField label="EUR/USD (%)" numeric value={fields.eurUsd} error={errors.eurUsd} onChange={(event) => setField('eurUsd', event.target.value)} autoComplete="off" />
                <TextField label="Taux (pb)" numeric value={fields.rates} error={errors.rates} onChange={(event) => setField('rates', event.target.value)} autoComplete="off" />
              </div>
              <div className="flex flex-wrap gap-1.5">
                {PRESETS.map((preset) => (
                  <Button key={preset.label} size="sm" variant="ghost" className="border border-line-subtle" onClick={() => setFields(preset.fields)}>
                    {preset.label}
                  </Button>
                ))}
              </div>
              <Button type="submit" variant="primary" className="self-start" disabled={!request || stress.isPending}>Lancer le scénario</Button>
            </form>
          ) : scenarios ? (
            <ScenarioList scenarios={scenarios} selectedId={selectedId} onRun={runScenario} />
          ) : (
            <div className="flex flex-col gap-1" aria-busy="true">
              {Array.from({ length: 4 }, (_, index) => <Skeleton key={index} className="h-14 w-full" />)}
            </div>
          )}
        </div>

        <ResultView result={stress.data} isPending={stress.isPending} />
      </div>
    </Panel>
  );
}
