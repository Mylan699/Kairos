import { useReferenceData } from '@/api/queries';
import {
  Button,
  cx,
  IconButton,
  Kbd,
  KairosMark,
  KeyboardIcon,
  MoonIcon,
  RollingNumber,
  SearchIcon,
  Select,
  setTheme,
  Skeleton,
  SunIcon,
  useTheme,
} from '@/design-system';
import { formatLongDate, formatMinute, formatSignedEur, trendClass } from '@/lib/format';
import { useTradeTicket } from '@/features/trade-ticket/TicketContext';
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
    <p className="num min-w-0 shrink-0 whitespace-nowrap text-xs text-ink-2">
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
    <div className="flex items-baseline gap-2 whitespace-nowrap">
      <span className="label hidden xl:inline">P&amp;L du jour</span>
      <RollingNumber text={formatSignedEur(day, 0)} className={cx('text-sm font-medium', trendClass(day))} />
    </div>
  );
}

function ConnectionPill() {
  const { status } = useLive();

  return (
    <span className="hidden items-center gap-1.5 whitespace-nowrap text-xs text-ink-2 xl:inline-flex" role="status">
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
    <div className="hidden w-44 lg:block">
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

export function TopBar({ onShowShortcuts, onOpenPalette }: { onShowShortcuts: () => void; onOpenPalette: () => void }) {
  const theme = useTheme();
  const { status } = useLive();
  const { openTicket } = useTradeTicket();

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
      <button
        type="button"
        onClick={onOpenPalette}
        className="hidden h-8 w-44 items-center gap-2 rounded-md border border-line bg-surface-1 px-2.5 text-sm text-ink-3 transition-colors duration-[var(--motion-fast)] hover:border-line-strong xl:flex"
      >
        <SearchIcon />
        Rechercher
        <span className="ml-auto"><Kbd>Ctrl K</Kbd></span>
      </button>
      {/* Wrapped because the button sets its own display, which would override `hidden`. */}
      <span className="hidden md:inline-flex">
        <Button variant="primary" onClick={() => openTicket({ side: 'Buy' })}>Nouveau trade</Button>
      </span>
      <IconButton
        label={theme === 'dark' ? 'Passer au thème clair' : 'Passer au thème sombre'}
        onClick={() => setTheme(theme === 'dark' ? 'light' : 'dark')}
      >
        {theme === 'dark' ? <SunIcon /> : <MoonIcon />}
      </IconButton>
      <span className="hidden md:inline-flex">
        <IconButton label="Raccourcis clavier" onClick={onShowShortcuts}>
          <KeyboardIcon />
        </IconButton>
      </span>
    </header>
  );
}
