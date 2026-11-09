import type { TradeStatus } from '@/api/types';
import { Badge } from '@/design-system';
import { STATUS_LABELS } from '@/lib/labels';

const TONES = { New: 'accent', Validated: 'neutral', Settled: 'muted', Cancelled: 'caution' } as const;

export const StatusBadge = ({ status }: { status: TradeStatus }) => (
  <Badge tone={TONES[status]}>{STATUS_LABELS[status]}</Badge>
);
