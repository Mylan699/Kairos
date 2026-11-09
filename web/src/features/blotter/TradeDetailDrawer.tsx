import { useMutation } from '@tanstack/react-query';
import { useState, type FormEvent, type ReactNode } from 'react';
import { apiPost, describeError } from '@/api/client';
import { useTradeDetail } from '@/api/queries';
import type { Trade, TradeDetail } from '@/api/types';
import { Button, Drawer, ErrorState, Modal, Skeleton, TextField, useToast } from '@/design-system';
import { useTradeTicket } from '@/features/trade-ticket/TicketContext';
import { formatDate, formatPrice, formatQuantity } from '@/lib/format';
import { ASSET_CLASS_LABELS, AUDIT_LABELS, SIDE_LABELS } from '@/lib/labels';
import { availableActions } from './blotterModel';
import { StatusBadge } from './StatusBadge';

const timestamp = new Intl.DateTimeFormat('fr-FR', {
  day: '2-digit',
  month: '2-digit',
  year: 'numeric',
  hour: '2-digit',
  minute: '2-digit',
  timeZone: 'UTC',
});

const Term = ({ label, children }: { label: string; children: ReactNode }) => (
  <div className="flex flex-col gap-0.5">
    <dt className="label">{label}</dt>
    <dd className="text-sm text-ink">{children}</dd>
  </div>
);

const SectionTitle = ({ children }: { children: ReactNode }) => <h3 className="label mb-2">{children}</h3>;

function DetailBody({ detail, onClose }: { detail: TradeDetail; onClose: () => void }) {
  const { notify } = useToast();
  const { openTicket } = useTradeTicket();
  const [cancelling, setCancelling] = useState(false);
  const [reason, setReason] = useState('');
  const { trade, versions, auditTrail } = detail;

  const transition = useMutation({
    mutationFn: ({ action, body }: { action: 'validate' | 'settle' | 'cancel'; body?: unknown }) =>
      apiPost<Trade>(`/api/trades/${trade.id}/${action}`, body),
    onSuccess: (_, { action }) => {
      const titles = { validate: 'Trade validé', settle: 'Trade réglé', cancel: 'Trade annulé' };
      notify({ tone: 'success', title: titles[action] });
      setCancelling(false);
    },
    onError: (error) => notify({ tone: 'error', title: 'Action refusée', description: describeError(error) }),
  });

  const actions = availableActions(trade.status);

  const submitCancellation = (event: FormEvent) => {
    event.preventDefault();
    transition.mutate({ action: 'cancel', body: { reason } });
  };

  return (
    <div className="flex flex-col">
      <section className="flex flex-col gap-4 p-4">
        <div className="flex items-center justify-between gap-3">
          <p className="num text-xl text-ink">{trade.symbol}</p>
          <StatusBadge status={trade.status} />
        </div>
        <dl className="grid grid-cols-2 gap-x-4 gap-y-3">
          <Term label="Sens">{SIDE_LABELS[trade.side]}</Term>
          <Term label="Classe d'actifs">{ASSET_CLASS_LABELS[trade.assetClass]}</Term>
          <Term label="Quantité"><span className="num">{formatQuantity(trade.quantity)}</span></Term>
          <Term label="Prix"><span className="num">{formatPrice(trade.price, trade.assetClass)}</span></Term>
          <Term label="Book">{trade.book}</Term>
          <Term label="Contrepartie">{trade.counterparty}</Term>
          <Term label="Trader">{trade.trader}</Term>
          <Term label="Version"><span className="num">{trade.version}</span></Term>
          <Term label="Date de trade"><span className="num">{formatDate(trade.tradeDate)}</span></Term>
          <Term label="Date de valeur"><span className="num">{formatDate(trade.valueDate)}</span></Term>
        </dl>

        {actions.length > 0 && (
          <div className="flex flex-wrap gap-2">
            {actions.includes('validate') && (
              <Button variant="primary" disabled={transition.isPending} onClick={() => transition.mutate({ action: 'validate' })}>Valider</Button>
            )}
            {actions.includes('settle') && (
              <Button variant="primary" disabled={transition.isPending} onClick={() => transition.mutate({ action: 'settle' })}>Régler</Button>
            )}
            {actions.includes('amend') && (
              <Button onClick={() => { onClose(); openTicket({ side: trade.side, amend: trade }); }}>Amender</Button>
            )}
            {actions.includes('cancel') && (
              <Button variant="ghost" onClick={() => setCancelling(true)}>Annuler le trade</Button>
            )}
          </div>
        )}
      </section>

      {versions.length > 1 && (
        <section className="border-t border-line-subtle p-4">
          <SectionTitle>Versions</SectionTitle>
          <ol className="flex flex-col gap-1.5">
            {[...versions].reverse().map((version) => (
              <li key={version.number} className="grid grid-cols-[2rem_1fr_auto] items-baseline gap-2 text-xs">
                <span className="num text-ink-3">v{version.number}</span>
                <span className="num text-ink">
                  {SIDE_LABELS[version.side]} {formatQuantity(version.quantity)} à {formatPrice(version.price, trade.assetClass)}
                </span>
                <span className="text-ink-3">{version.createdBy}</span>
              </li>
            ))}
          </ol>
        </section>
      )}

      <section className="border-t border-line-subtle p-4">
        <SectionTitle>Piste d'audit</SectionTitle>
        <ol className="flex flex-col">
          {[...auditTrail].reverse().map((entry, index) => (
            <li key={index} className="relative border-l border-line pb-3 pl-3 last:border-transparent last:pb-0">
              <span className="absolute -left-[3px] top-1.5 size-[5px] rounded-full bg-accent" />
              <div className="flex items-baseline justify-between gap-2">
                <p className="text-xs font-medium text-ink">{AUDIT_LABELS[entry.action]}</p>
                <time className="num text-2xs text-ink-3">{timestamp.format(new Date(entry.at))}</time>
              </div>
              <p className="text-xs text-ink-2">{entry.actor}</p>
              {entry.details && <p className="num mt-0.5 break-words text-2xs text-ink-3">{entry.details}</p>}
            </li>
          ))}
        </ol>
      </section>

      <Modal open={cancelling} onClose={() => setCancelling(false)} title="Annuler ce trade">
        <form onSubmit={submitCancellation} className="flex flex-col gap-3">
          <TextField label="Motif" value={reason} maxLength={200} onChange={(event) => setReason(event.target.value)} />
          <div className="flex justify-end gap-2">
            <Button variant="ghost" onClick={() => setCancelling(false)}>Garder le trade</Button>
            <Button type="submit" variant="primary" disabled={reason.trim() === '' || transition.isPending}>Confirmer l'annulation</Button>
          </div>
        </form>
      </Modal>
    </div>
  );
}

export function TradeDetailDrawer({ tradeId, onClose }: { tradeId: string | null; onClose: () => void }) {
  const { data, isError, refetch } = useTradeDetail(tradeId);

  return (
    <Drawer open={tradeId !== null} onClose={onClose} title="Détail du trade">
      {isError ? (
        <ErrorState title="Trade introuvable" onRetry={() => void refetch()} />
      ) : data ? (
        <DetailBody detail={data} onClose={onClose} />
      ) : (
        <div className="flex flex-col gap-4 p-4" aria-busy="true">
          <Skeleton className="h-8 w-40" />
          <Skeleton className="h-48 w-full" />
          <Skeleton className="h-32 w-full" />
        </div>
      )}
    </Drawer>
  );
}
