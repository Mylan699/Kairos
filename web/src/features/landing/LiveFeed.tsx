import { AnimatePresence, motion, MotionConfig } from 'motion/react';
import { useEffect, useRef, useState } from 'react';
import type { MarketTick, Quote } from '@/api/types';
import { RollingNumber } from '@/design-system/components/LiveFigures';
import { cx } from '@/design-system/cx';
import { duration, ease, spring } from '@/design-system/motion';
import { formatLongDate, formatMinute, formatPercent, formatPrice, formatSignedEur, trendClass } from '@/lib/format';
import { ASSET_CLASS_LABELS } from '@/lib/labels';
import { LiveProvider, useLive, useLiveStore } from '@/realtime/live';
import { getLiveStore } from '@/realtime/sharedStore';

const VISIBLE_ROWS = 7;
const ROW_HEIGHT = 'h-8';

export interface FeedRow {
  key: string;
  minute: number;
  quote: Quote;
}

/** The instrument that moved most since the previous tick: real data, nothing is invented for the show. */
export function biggestMove(previous: readonly Quote[], current: readonly Quote[]): Quote | null {
  let best: Quote | null = null;
  let bestMove = 0;

  for (const quote of current) {
    const before = previous.find((item) => item.instrumentId === quote.instrumentId);
    const move = before && before.last !== 0 ? Math.abs(quote.last / before.last - 1) : 0;

    if (move > bestMove) {
      best = quote;
      bestMove = move;
    }
  }

  return best;
}

function useFeedRows(): FeedRow[] {
  const store = useLiveStore();
  const [rows, setRows] = useState<FeedRow[]>([]);
  const previousTick = useRef<MarketTick | null>(null);

  useEffect(
    () =>
      store.subscribe(() => {
        const tick = store.getState().tick;
        const previous = previousTick.current;

        if (!tick || tick === previous) {
          return;
        }

        previousTick.current = tick;

        const mover = previous ? biggestMove(previous.quotes, tick.quotes) : null;
        if (mover) {
          const row = { key: `${tick.status.minute}-${mover.instrumentId}`, minute: tick.status.minute, quote: mover };
          setRows((current) => [row, ...current.filter((item) => item.key !== row.key)].slice(0, VISIBLE_ROWS));
        }
      }),
    [store],
  );

  return rows;
}

function Placeholder({ message }: { message: string }) {
  return (
    <li className="grid h-56 place-items-center text-xs text-ink-3">{message}</li>
  );
}

function Feed() {
  const { tick, status } = useLive();
  const rows = useFeedRows();

  return (
    <div className="flex h-full flex-col">
      <header className="flex items-start justify-between gap-4 border-b border-line-subtle p-4">
        <div>
          <p className="label flex items-center gap-2">
            <span className={cx('size-1.5 rounded-full', status === 'connected' ? 'bg-accent' : 'bg-idle')} />
            {status === 'connected' ? 'Rejeu en direct' : 'Connexion au flux'}
          </p>
          <p className="num mt-1 min-h-5 text-sm capitalize text-ink">
            {tick ? `${formatLongDate(tick.status.date)} · ${formatMinute(tick.status.minute)} UTC` : ' '}
          </p>
        </div>
        <div className="text-right">
          <p className="label">P&amp;L du jour</p>
          {tick ? (
            <RollingNumber text={formatSignedEur(tick.summary.total.day, 0)} className={cx('mt-1 text-lg font-medium', trendClass(tick.summary.total.day))} />
          ) : (
            <p className="mt-1 h-7" />
          )}
        </div>
      </header>

      <div className="grid grid-cols-[3.5rem_1fr_auto_4.5rem] gap-3 border-b border-line-subtle px-4 py-2">
        <span className="label">Heure</span>
        <span className="label">Instrument</span>
        <span className="label text-right">Dernier</span>
        <span className="label text-right">Var.</span>
      </div>

      <ul className="min-h-56 flex-1 overflow-hidden">
        {rows.length === 0 && <Placeholder message={status === 'disconnected' ? 'Flux momentanément indisponible.' : 'En attente des premiers ticks…'} />}
        <AnimatePresence initial={false}>
          {rows.map((row) => (
            <motion.li
              key={row.key}
              layout
              initial={{ opacity: 0, x: -12 }}
              animate={{ opacity: 1, x: 0 }}
              exit={{ opacity: 0, transition: { duration: duration.fast, ease: ease.exit } }}
              transition={{ layout: spring.layout, duration: duration.slow, ease: ease.standard }}
              className={cx('kairos-row-new grid grid-cols-[3.5rem_1fr_auto_4.5rem] items-center gap-3 border-b border-line-subtle px-4 text-xs', ROW_HEIGHT)}
            >
              <span className="num text-ink-2">{formatMinute(row.minute)}</span>
              <span className="flex min-w-0 items-baseline gap-2">
                <span className="num truncate text-ink">{row.quote.symbol}</span>
                <span className="hidden text-2xs text-ink-2 sm:inline">{ASSET_CLASS_LABELS[row.quote.assetClass]}</span>
              </span>
              <span className="num text-right text-ink">{formatPrice(row.quote.last, row.quote.assetClass)}</span>
              <span className={cx('num text-right', trendClass(row.quote.changePercent))}>{formatPercent(row.quote.changePercent)}</span>
            </motion.li>
          ))}
        </AnimatePresence>
      </ul>
    </div>
  );
}

export default function LiveFeed() {
  return (
    <LiveProvider store={getLiveStore()}>
      <MotionConfig reducedMotion="user">
        <Feed />
      </MotionConfig>
    </LiveProvider>
  );
}
