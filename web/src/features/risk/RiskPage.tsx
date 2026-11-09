import { useBookFilter } from '@/app/BookFilter';
import { useRiskReport } from '@/api/queries';
import type { RiskReport } from '@/api/types';
import { BarList, cx, ErrorState, Panel, RollingNumber, Skeleton } from '@/design-system';
import { formatCompactEur, formatSignedEur, trendClass } from '@/lib/format';
import { sectorLabel } from '@/lib/labels';
import { useLive, useLiveTopic } from '@/realtime/live';
import { assetClassKeyLabel, sensitivitiesFor } from './riskModel';
import { StressPanel } from './StressPanel';

function Sensitivity({ title, value, hint, isResult = true }: { title: string; value: number; hint: string; isResult?: boolean }) {
  return (
    <Panel title={title} className="h-full">
      <RollingNumber text={formatSignedEur(value, 0)} className={cx('text-2xl font-medium', isResult ? trendClass(value) : 'text-ink')} />
      <p className="mt-1 text-xs text-ink-2">{hint}</p>
    </Panel>
  );
}

function BookTable({ report }: { report: RiskReport }) {
  const { bookId } = useBookFilter();

  return (
    <div className="overflow-x-auto">
      <table className="w-full text-xs">
        <thead>
          <tr className="text-left">
            <th scope="col" className="label px-3 py-2 font-normal">Book</th>
            <th scope="col" className="label whitespace-nowrap px-3 py-2 text-right font-normal">Δ actions</th>
            <th scope="col" className="label whitespace-nowrap px-3 py-2 text-right font-normal">Δ change</th>
            <th scope="col" className="label px-3 py-2 text-right font-normal">DV01</th>
          </tr>
        </thead>
        <tbody>
          {report.books.map((book) => (
            <tr key={book.bookId} className={cx('border-t border-line-subtle', book.bookId === bookId && 'bg-accent-soft')}>
              <td className="whitespace-nowrap px-3 py-2 text-ink">{book.book}</td>
              <td className={cx('num whitespace-nowrap px-3 py-2 text-right', trendClass(book.equityDelta))}>{formatCompactEur(book.equityDelta)}</td>
              <td className={cx('num whitespace-nowrap px-3 py-2 text-right', trendClass(book.fxDelta))}>{formatCompactEur(book.fxDelta)}</td>
              <td className="num whitespace-nowrap px-3 py-2 text-right text-ink">{formatCompactEur(book.dv01)}</td>
            </tr>
          ))}
        </tbody>
      </table>
    </div>
  );
}

function RiskSkeleton() {
  return (
    <div className="grid gap-2 md:grid-cols-3 md:gap-3" aria-busy="true" aria-label="Chargement des risques">
      <Skeleton className="h-28" />
      <Skeleton className="h-28" />
      <Skeleton className="h-28" />
      <Skeleton className="h-80 md:col-span-3" />
      <Skeleton className="h-64" />
      <Skeleton className="h-64" />
      <Skeleton className="h-64" />
    </div>
  );
}

export default function RiskPage() {
  useLiveTopic('risk');

  const { risk: liveRisk } = useLive();
  const query = useRiskReport();
  const { bookId } = useBookFilter();
  const report = liveRisk ?? query.data;

  if (query.isError && !report) {
    return <ErrorState onRetry={() => void query.refetch()} />;
  }

  if (!report) {
    return <RiskSkeleton />;
  }

  const figures = sensitivitiesFor(report, bookId);

  return (
    <div className="grid gap-2 md:grid-cols-3 md:gap-3">
      <Sensitivity title="Delta actions" value={figures.equityDelta} hint="Gain si toutes les actions montent de 1 %." />
      <Sensitivity title="Delta change" value={figures.fxDelta} hint="Gain si l'EUR/USD monte de 1 %." />
      <Sensitivity title="DV01" value={figures.dv01} hint="Perte si les taux montent de 1 pb." isResult={false} />

      <div className="md:col-span-3">
        <StressPanel />
      </div>

      <Panel title="Exposition par classe d'actifs">
        <BarList
          labelWidth="6rem"
          items={report.byAssetClass.map((line) => ({ key: line.key, label: assetClassKeyLabel(line.key), value: line.eur, valueText: formatCompactEur(line.eur, false) }))}
        />
        <h3 className="label mb-2 mt-4">Par devise</h3>
        <BarList
          labelWidth="6rem"
          items={report.byCurrency.map((line) => ({ key: line.key, label: line.key, value: line.eur, valueText: formatCompactEur(line.eur, false) }))}
        />
      </Panel>

      <Panel title="Exposition par secteur">
        <BarList
          labelWidth="9.5rem"
          items={report.bySector.map((line) => ({ key: line.key, label: sectorLabel(line.key), value: line.eur, valueText: formatCompactEur(line.eur, false) }))}
        />
      </Panel>

      <Panel title="Sensibilités par book" flush>
        <BookTable report={report} />
      </Panel>
    </div>
  );
}
