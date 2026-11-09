import { useReferenceData } from '@/api/queries';
import {
  cx,
  IconButton,
  KairosMark,
  KeyboardIcon,
  MoonIcon,
  RollingNumber,
  Select,
  setTheme,
  Skeleton,
  SunIcon,
  useTheme,
} from '@/design-system';
import { formatLongDate, formatMinute, formatSignedEur, trendClass } from '@/lib/format';
import { useLive } from '@/realtime/live';
import type { ConnectionStatus } from '@/realtime/LiveStore';
import { useBookFilter } from './BookFilter';

const WHOLE_DESK = 'desk';

const STATUS_LABELS: Record<ConnectionStatus, string> = {
  connecting: 'Connexion',
  connected: 'En direct',
  reconnecting: 'Reconnexion',
  disconnected: 'Hors ligne',
};

function MarketClock() {
  const { tick } = useLive();

  if (!tick) {
    return <Skeleton className="h-4 w-56" />;
  }

  return (
    <p className="num min-w-0 truncate text-xs text-ink-2">
      <span className="hidden capitalize text-ink sm:inline">{formatLongDate(tick.status.date)}</span>
      <span className="hidden sm:inline"> · </span>
      <span className="text-ink sm:text-ink-2">{formatMinute(tick.status.minute)} UTC</span>
    </p>
  );
}

function DayPnl() {
  const { tick } = useLive();
  const { bookId } = useBookFilter();

  if (!tick) {
    return <Skeleton className="h-5 w-32" />;
  }

  const figures = bookId ? tick.summary.books.find((book) => book.bookId === bookId)?.pnl : tick.summary.total;
  const day = figures?.day ?? 0;

  return (
    <div className="flex items-baseline gap-2">
      <span className="label hidden lg:inline">P&amp;L du jour</span>
      <RollingNumber text={formatSignedEur(day, 0)} className={cx('text-sm font-medium', trendClass(day))} />
    </div>
  );
}

function ConnectionPill() {
  const { status } = useLive();

  return (
    <span className="hidden items-center gap-1.5 text-xs text-ink-2 lg:inline-flex" role="status">
      <span className={cx('size-1.5 rounded-full', status === 'connected' ? 'bg-accent' : 'bg-idle')} />
      {STATUS_LABELS[status]}
    </span>
  );
}

function BookSelect() {
  const { data } = useReferenceData();
  const { bookId, setBookId } = useBookFilter();

  const options = [
    { value: WHOLE_DESK, label: 'Tous les books' },
    ...(data?.books.map((book) => ({ value: book.id, label: book.name })) ?? []),
  ];

  return (
    <div className="hidden w-48 md:block">
      <Select
        label="Book"
        hideLabel
        value={bookId ?? WHOLE_DESK}
        options={options}
        onChange={(value) => setBookId(value === WHOLE_DESK ? null : value)}
      />
    </div>
  );
}

export function TopBar({ onShowShortcuts }: { onShowShortcuts: () => void }) {
  const theme = useTheme();
  const { status } = useLive();

  return (
    <header className="sticky top-0 z-30 flex h-12 shrink-0 items-center gap-3 border-b border-line-subtle bg-surface-1 px-3">
      <span className="md:hidden">
        <KairosMark size={24} animation="live" connected={status === 'connected'} />
      </span>
      <MarketClock />
      <div className="flex-1" />
      <DayPnl />
      <ConnectionPill />
      <BookSelect />
      <IconButton
        label={theme === 'dark' ? 'Passer au thème clair' : 'Passer au thème sombre'}
        onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')}
      >
        {theme === 'dark' ? <SunIcon /> : <MoonIcon />}
      </IconButton>
      <IconButton label="Raccourcis clavier" className="hidden md:inline-grid" onClick={onShowShortcuts}>
        <KeyboardIcon />
      </IconButton>
    </header>
  );
}
