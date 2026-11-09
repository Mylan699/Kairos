import { useMutation } from '@tanstack/react-query';
import { useState, type FormEvent } from 'react';
import { apiPost, describeError } from '@/api/client';
import { useIntradayOverview, useRemarkableDays } from '@/api/queries';
import type { MarketTick, RemarkableDay, ReplayStatus } from '@/api/types';
import { Button, CONTROL_CLASSES, cx, IconButton, PauseIcon, Panel, PlayIcon, RollingNumber, SegmentedControl, Skeleton, useToast } from '@/design-system';
import { formatDate, formatLongDate, formatMinute, formatPercent, formatPrice, formatSignedEur, formatYield, trendClass } from '@/lib/format';
import { useLive } from '@/realtime/live';
import { isIsoDate, realMinutesPerDay, speedLabel } from './replayModel';
import { ReplayTimeline } from './ReplayTimeline';

const SPEEDS = [30, 60, 120, 240, 600, 1800];

const SPEED_SEGMENTS = SPEEDS.map((speed) => ({ value: String(speed), label: speedLabel(speed) }));

function useReplayCommand() {
  const { notify } = useToast();

  return useMutation({
    mutationFn: ({ command, body }: { command: string; body?: unknown }) => apiPost<ReplayStatus>(`/api/replay/${command}`, body),
    onError: (error) => notify({ tone: 'error', title: 'Commande refusée', description: describeError(error) }),
  });
}

const Figure = ({ label, children }: { label: string; children: React.ReactNode }) => (
  <div className="flex flex-col gap-0.5">
    <dt className="label">{label}</dt>
    <dd className="num text-lg text-ink">{children}</dd>
  </div>
);

function LiveFigures({ tick }: { tick: MarketTick }) {
  const eurUsd = tick.quotes.find((quote) => quote.assetClass === 'Fx');
  const tenYear = tick.curve.points.find((point) => point.months === 120);
  const equities = tick.quotes.filter((quote) => quote.assetClass === 'Equity');
  const basket = equities.reduce((total, quote) => total + quote.changePercent, 0) / (equities.length || 1);

  return (
    <dl className="grid grid-cols-2 gap-x-6 gap-y-3 lg:grid-cols-4">
      <Figure label="P&L du jour">
        <RollingNumber text={formatSignedEur(tick.summary.total.day, 0)} className={trendClass(tick.summary.total.day)} />
      </Figure>
      <Figure label="Panier actions"><span className={trendClass(basket)}>{formatPercent(basket)}</span></Figure>
      <Figure label="EUR/USD">{formatPrice(eurUsd?.last, 'Fx')}</Figure>
      <Figure label="Taux 10 ans">{formatYield(tenYear?.yieldPercent)}</Figure>
    </dl>
  );
}

function DayCard({ day, isCurrent, onReplay }: { day: RemarkableDay; isCurrent: boolean; onReplay: () => void }) {
  return (
    <li>
      <button
        type="button"
        aria-pressed={isCurrent}
        onClick={onReplay}
        className={cx(
          'flex h-full w-full flex-col gap-0.5 rounded-md border px-3 py-2.5 text-left transition-colors duration-[var(--motion-fast)]',
          isCurrent ? 'border-accent bg-accent-soft' : 'border-line-subtle hover:border-line-strong hover:bg-surface-3',
        )}
      >
        <span className="flex items-baseline justify-between gap-2">
          <span className="text-sm font-medium text-ink">{day.name}</span>
          <span className="num text-2xs text-ink-3">{formatDate(day.date)}</span>
        </span>
        <span className="text-xs text-ink-2">{day.description}</span>
      </button>
    </li>
  );
}

function ReplaySkeleton() {
  return (
    <div className="flex flex-col gap-2 md:gap-3" aria-busy="true" aria-label="Chargement du rejeu">
      <Skeleton className="h-64 w-full" />
      <Skeleton className="h-96 w-full" />
    </div>
  );
}

export default function ReplayPage() {
  const { tick } = useLive();
  const days = useRemarkableDays();
  const points = useIntradayOverview();
  const command = useReplayCommand();
  const [customDate, setCustomDate] = useState('');

  if (!tick) {
    return <ReplaySkeleton />;
  }

  const { status } = tick;

  const loadDay = (date: string, startMinute?: number) => command.mutate({ command: 'load', body: { date, startMinute } });

  const onSubmitDate = (event: FormEvent) => {
    event.preventDefault();
    if (isIsoDate(customDate)) {
      loadDay(customDate, 0);
    }
  };

  return (
    <div className="flex flex-col gap-2 md:gap-3">
      <Panel title="Machine à remonter le temps">
        <div className="flex flex-wrap items-end justify-between gap-x-6 gap-y-3">
          <div>
            <p className="text-sm capitalize text-ink-2">{formatLongDate(status.date)}</p>
            <p className="num text-3xl font-medium text-ink">
              {formatMinute(status.minute)}
              <span className="ml-2 text-base text-ink-3">UTC</span>
            </p>
          </div>

          <div className="flex items-center gap-3">
            <IconButton
              label={status.isPlaying ? 'Mettre en pause' : 'Reprendre la lecture'}
              className="size-10 border border-line"
              onClick={() => command.mutate({ command: status.isPlaying ? 'pause' : 'play' })}
            >
              {status.isPlaying ? <PauseIcon /> : <PlayIcon />}
            </IconButton>
            <div className="max-w-full overflow-x-auto">
              <SegmentedControl
                label="Vitesse"
                value={String(status.speed)}
                segments={SPEED_SEGMENTS}
                onChange={(value) => command.mutate({ command: 'speed', body: { speed: Number(value) } })}
              />
            </div>
            <p className="hidden text-xs text-ink-3 lg:block">Une journée en {realMinutesPerDay(status.speed)} min</p>
          </div>
        </div>

        <div className="mt-4">
          <ReplayTimeline
            minute={status.minute}
            sessionOpenMinute={status.sessionOpenMinute}
            sessionCloseMinute={status.sessionCloseMinute}
            points={points}
            onSeek={(minute) => command.mutate({ command: 'seek', body: { minute } })}
          />
        </div>

        <div className="mt-4 border-t border-line-subtle pt-3">
          <LiveFigures tick={tick} />
        </div>
      </Panel>

      <Panel
        title="Journées remarquables"
        aside={
          <form onSubmit={onSubmitDate} className="flex items-center gap-2">
            <input
              aria-label="Autre date à rejouer"
              className={cx(CONTROL_CLASSES, 'num h-7 w-28 text-xs')}
              placeholder="AAAA-MM-JJ"
              value={customDate}
              onChange={(event) => setCustomDate(event.target.value)}
              autoComplete="off"
            />
            <Button type="submit" size="sm" disabled={!isIsoDate(customDate) || command.isPending}>Rejouer</Button>
          </form>
        }
      >
        {days.data ? (
          <ul className="grid gap-2 md:grid-cols-2 xl:grid-cols-3">
            {days.data.map((day) => (
              <DayCard key={day.date} day={day} isCurrent={day.date === status.date} onReplay={() => loadDay(day.date, day.startMinute)} />
            ))}
          </ul>
        ) : (
          <div className="grid gap-2 md:grid-cols-2 xl:grid-cols-3" aria-busy="true">
            {Array.from({ length: 6 }, (_, index) => <Skeleton key={index} className="h-16" />)}
          </div>
        )}
      </Panel>
    </div>
  );
}
