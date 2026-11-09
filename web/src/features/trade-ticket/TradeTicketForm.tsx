import { keepPreviousData, useMutation, useQuery } from '@tanstack/react-query';
import { useMemo, useState, type FormEvent } from 'react';
import { useBookFilter } from '@/app/BookFilter';
import { apiPost, apiPut, ApiError, describeError } from '@/api/client';
import { useReferenceData } from '@/api/queries';
import type { Quote, ReferenceData, Side, Trade, TradeImpact, TradeRequest } from '@/api/types';
import { Button, cx, FlashValue, SegmentedControl, Select, Skeleton, TextField, useToast } from '@/design-system';
import { formatEur, formatPrice, formatQuantity, formatSignedEur, trendClass } from '@/lib/format';
import { useThrottledValue } from '@/lib/useThrottledValue';
import { useLive } from '@/realtime/live';
import type { TicketIntent } from './TicketContext';
import { marketPrice, PRICE_DECIMALS, QUANTITY_LABELS, validateTicket } from './ticketModel';

const ESTIMATE_INTERVAL_MILLISECONDS = 1_000;

const SIDES = [
  { value: 'Buy', label: 'Achat' },
  { value: 'Sell', label: 'Vente' },
] as const;

export function TradeTicketForm({ intent, onDone }: { intent: TicketIntent; onDone: () => void }) {
  const { data } = useReferenceData();

  if (!data) {
    return (
      <div className="flex flex-col gap-4 p-4" aria-busy="true">
        <Skeleton className="h-16 w-full" />
        {Array.from({ length: 6 }, (_, index) => <Skeleton key={index} className="h-12 w-full" />)}
      </div>
    );
  }

  return <LoadedForm intent={intent} reference={data} onDone={onDone} />;
}

function QuoteTile({ label, value, quote, active, onClick }: { label: string; value: number; quote: Quote; active: boolean; onClick: () => void }) {
  return (
    <button
      type="button"
      onClick={onClick}
      className={cx(
        'flex flex-1 flex-col items-start gap-0.5 rounded-md border p-2.5 text-left transition-colors duration-[var(--motion-fast)]',
        active ? 'border-accent bg-accent-soft' : 'border-line hover:border-line-strong',
      )}
    >
      <span className="label">{label}</span>
      <FlashValue value={value} className="-mx-1 text-xl">{formatPrice(value, quote.assetClass)}</FlashValue>
    </button>
  );
}

function ImpactLine({ label, children }: { label: string; children: React.ReactNode }) {
  return (
    <div className="flex items-baseline justify-between gap-3">
      <dt className="text-xs text-ink-2">{label}</dt>
      <dd className="num text-xs text-ink">{children}</dd>
    </div>
  );
}

function ImpactPreview({ request }: { request: TradeRequest | null }) {
  const key = useThrottledValue(request ? JSON.stringify(request) : null, ESTIMATE_INTERVAL_MILLISECONDS);

  const { data, isError } = useQuery({
    queryKey: ['estimate', key],
    queryFn: () => apiPost<TradeImpact>('/api/trades/estimate', JSON.parse(key!) as TradeRequest),
    enabled: key !== null && request !== null,
    placeholderData: keepPreviousData,
    staleTime: 0,
  });

  return (
    <section aria-label="Impact estimé" className="border-t border-line-subtle p-4">
      <h3 className="label mb-2">Impact estimé</h3>
      {request === null || isError ? (
        <p className="text-xs text-ink-3">
          {isError ? 'Estimation indisponible pour ce ticket.' : "Complétez le ticket pour voir l'impact."}
        </p>
      ) : !data ? (
        <div className="flex flex-col gap-1.5" aria-busy="true">
          {Array.from({ length: 4 }, (_, index) => <Skeleton key={index} className="h-4 w-full" />)}
        </div>
      ) : (
        <dl className="flex flex-col gap-1">
          <ImpactLine label="Montant du trade">{formatEur(data.notionalEur, 0)}</ImpactLine>
          <ImpactLine label="P&L immédiat">
            <span className={trendClass(data.pnlImpactEur)}>{formatSignedEur(data.pnlImpactEur)}</span>
          </ImpactLine>
          <ImpactLine label="Position">
            {formatQuantity(data.positionBefore)} → {formatQuantity(data.positionAfter)}
          </ImpactLine>
          <ImpactLine label="Variation du DV01">{formatSignedEur(data.dv01ChangeEur, 0)}</ImpactLine>
          <ImpactLine label="DV01 du desk après trade">{formatEur(data.totalDv01AfterEur, 0)}</ImpactLine>
        </dl>
      )}
    </section>
  );
}

function LoadedForm({ intent, reference, onDone }: { intent: TicketIntent; reference: ReferenceData; onDone: () => void }) {
  const { tick } = useLive();
  const { bookId: filteredBook } = useBookFilter();
  const { notify } = useToast();
  const amended: Trade | undefined = intent.amend;

  const tradable = useMemo(() => reference.instruments.filter((item) => item.isTradable), [reference]);

  const [instrumentId, setInstrumentId] = useState(amended?.instrumentId ?? intent.instrumentId ?? tradable[0]?.id ?? null);
  const [side, setSide] = useState<Side>(amended?.side ?? intent.side);
  const [quantity, setQuantity] = useState(amended ? String(amended.quantity) : '');
  // While the price is null, the ticket follows the market. Typing a price detaches it.
  const [typedPrice, setTypedPrice] = useState<string | null>(amended ? String(amended.price) : null);
  const [counterpartyId, setCounterpartyId] = useState(amended?.counterpartyId ?? reference.counterparties[0]?.id ?? null);
  const [bookId, setBookId] = useState(amended?.bookId ?? filteredBook ?? reference.books[0]?.id ?? null);
  const [traderId, setTraderId] = useState(amended?.traderId ?? reference.traders[0]?.id ?? null);
  const [tradeDate, setTradeDate] = useState(amended?.tradeDate ?? '');
  const [valueDate, setValueDate] = useState(amended?.valueDate ?? '');
  const [serverErrors, setServerErrors] = useState<Record<string, string[]>>({});

  const instrument = reference.instruments.find((item) => item.id === instrumentId);
  const quote = tick?.quotes.find((item) => item.instrumentId === instrumentId);
  const assetClass = instrument?.assetClass ?? 'Equity';

  const followedPrice = marketPrice(quote, side)?.toFixed(PRICE_DECIMALS[assetClass]).replace('.', ',') ?? '';
  const priceText = typedPrice ?? followedPrice;

  const { request, errors } = validateTicket({
    instrumentId, side, quantity, price: priceText, counterpartyId, bookId, traderId, tradeDate, valueDate,
  });

  const submit = useMutation({
    mutationFn: (body: TradeRequest) =>
      amended ? apiPut<Trade>(`/api/trades/${amended.id}`, body) : apiPost<Trade>('/api/trades', body),
    onSuccess: (trade) => {
      notify({
        tone: 'success',
        title: amended ? 'Trade amendé' : 'Trade enregistré',
        description: `${trade.side === 'Buy' ? 'Achat' : 'Vente'} de ${formatQuantity(trade.quantity)} ${trade.symbol}`,
      });
      onDone();
    },
    onError: (error) => {
      setServerErrors(error instanceof ApiError ? error.fieldErrors : {});
      notify({ tone: 'error', title: 'Trade refusé', description: describeError(error) });
    },
  });

  const onSubmit = (event: FormEvent) => {
    event.preventDefault();
    if (request) {
      submit.mutate(request);
    }
  };

  const toOptions = (items: ReferenceData['books']) => items.map((item) => ({ value: item.id, label: item.name }));
  const verb = side === 'Buy' ? 'Acheter' : 'Vendre';

  return (
    <form onSubmit={onSubmit} className="flex min-h-full flex-col">
      <div className="flex flex-col gap-3 p-4">
        <Select
          label="Instrument"
          value={instrumentId}
          options={tradable.map((item) => ({ value: item.id, label: item.symbol }))}
          onChange={setInstrumentId}
        />

        <SegmentedControl label="Sens" fullWidth value={side} segments={SIDES} onChange={setSide} />

        {quote ? (
          <div className="flex gap-2">
            <QuoteTile label="Bid" value={quote.bid} quote={quote} active={side === 'Sell'} onClick={() => setSide('Sell')} />
            <QuoteTile label="Ask" value={quote.ask} quote={quote} active={side === 'Buy'} onClick={() => setSide('Buy')} />
          </div>
        ) : (
          <Skeleton className="h-[74px] w-full" />
        )}

        <div className="grid grid-cols-2 gap-3">
          <TextField
            label={QUANTITY_LABELS[assetClass]}
            numeric
            value={quantity}
            onChange={(event) => setQuantity(event.target.value)}
            error={errors.quantity ?? serverErrors.Quantity?.[0]}
            autoComplete="off"
          />
          <TextField
            label={typedPrice === null ? 'Prix (marché)' : 'Prix'}
            numeric
            value={priceText}
            onChange={(event) => setTypedPrice(event.target.value)}
            error={errors.price ?? serverErrors.Price?.[0]}
            autoComplete="off"
          />
        </div>

        <Select label="Book" value={bookId} options={toOptions(reference.books)} onChange={setBookId} />
        <Select label="Contrepartie" value={counterpartyId} options={toOptions(reference.counterparties)} onChange={setCounterpartyId} />
        <Select label="Trader" value={traderId} options={toOptions(reference.traders)} onChange={setTraderId} />

        <div className="grid grid-cols-2 gap-3">
          <TextField
            label="Date de trade"
            className="num"
            placeholder={tick?.status.date ?? 'AAAA-MM-JJ'}
            value={tradeDate}
            onChange={(event) => setTradeDate(event.target.value)}
            error={errors.tradeDate}
            autoComplete="off"
          />
          <TextField
            label="Date de valeur"
            className="num"
            placeholder="Automatique"
            value={valueDate}
            onChange={(event) => setValueDate(event.target.value)}
            error={errors.valueDate}
            autoComplete="off"
          />
        </div>
      </div>

      <div className="mt-auto">
        <ImpactPreview request={amended ? null : request} />
        <div className="border-t border-line-subtle p-4">
          <Button type="submit" variant="primary" className="w-full" disabled={!request || submit.isPending}>
            {amended ? "Enregistrer l'amendement" : `${verb}${instrument ? ` ${instrument.symbol}` : ''}`}
          </Button>
        </div>
      </div>
    </form>
  );
}
