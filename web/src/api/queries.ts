import { useQuery } from '@tanstack/react-query';
import { useLive } from '@/realtime/live';
import { apiGet } from './client';
import type {
  Curve,
  DailyBar,
  Dashboard,
  HistoricalScenario,
  IntradayPoint,
  Position,
  ReferenceData,
  RemarkableDay,
  RiskReport,
  Trade,
  TradeDetail,
} from './types';

export const queryKeys = {
  reference: ['reference'],
  trades: ['trades'],
  trade: (id: string) => ['trades', id],
  positions: ['portfolio', 'positions'],
  dashboard: ['portfolio', 'dashboard'],
  risk: ['risk'],
  scenarios: ['scenarios'],
  replayDays: ['replay-days'],
  bars: (instrumentId: string, count: number) => ['bars', instrumentId, count],
  curve: (date: string | null) => ['curve', date],
  intraday: (date: string | null) => ['intraday', date],
} as const;

const NEVER_STALE = Number.POSITIVE_INFINITY;

export const useReferenceData = () =>
  useQuery({
    queryKey: queryKeys.reference,
    queryFn: ({ signal }) => apiGet<ReferenceData>('/api/market/reference', signal),
  });

export const useTrades = () =>
  useQuery({
    queryKey: queryKeys.trades,
    queryFn: ({ signal }) => apiGet<Trade[]>('/api/trades', signal),
  });

export const useTradeDetail = (id: string | null) =>
  useQuery({
    queryKey: queryKeys.trade(id ?? ''),
    queryFn: ({ signal }) => apiGet<TradeDetail>(`/api/trades/${id}`, signal),
    enabled: id !== null,
  });

export const usePositions = () =>
  useQuery({
    queryKey: queryKeys.positions,
    queryFn: ({ signal }) => apiGet<Position[]>('/api/portfolio/positions', signal),
  });

export const useDashboard = () =>
  useQuery({
    queryKey: queryKeys.dashboard,
    queryFn: ({ signal }) => apiGet<Dashboard>('/api/portfolio/dashboard', signal),
  });

export const useRiskReport = () =>
  useQuery({
    queryKey: queryKeys.risk,
    queryFn: ({ signal }) => apiGet<RiskReport>('/api/risk', signal),
  });

export const useScenarios = () =>
  useQuery({
    queryKey: queryKeys.scenarios,
    queryFn: ({ signal }) => apiGet<HistoricalScenario[]>('/api/risk/scenarios', signal),
    staleTime: NEVER_STALE,
  });

export const useRemarkableDays = () =>
  useQuery({
    queryKey: queryKeys.replayDays,
    queryFn: ({ signal }) => apiGet<RemarkableDay[]>('/api/replay/days', signal),
    staleTime: NEVER_STALE,
  });

export const useDailyBars = (instrumentId: string | null, count: number) =>
  useQuery({
    queryKey: queryKeys.bars(instrumentId ?? '', count),
    queryFn: ({ signal }) => apiGet<DailyBar[]>(`/api/market/instruments/${instrumentId}/bars?count=${count}`, signal),
    enabled: instrumentId !== null,
  });

export const useCurve = (date: string | null) =>
  useQuery({
    queryKey: queryKeys.curve(date),
    queryFn: ({ signal }) => apiGet<Curve>(`/api/market/curve${date ? `?date=${date}` : ''}`, signal),
    enabled: date !== null,
  });

/** The day so far: what the server rebuilds when the screen opens, then what the feed adds. */
export function useIntradayOverview(): IntradayPoint[] {
  const { tick, intradayTail } = useLive();
  const date = tick?.status.date ?? null;

  const { data = [] } = useQuery({
    queryKey: queryKeys.intraday(date),
    queryFn: ({ signal }) => apiGet<IntradayPoint[]>('/api/replay/intraday', signal),
    enabled: date !== null,
    staleTime: NEVER_STALE,
  });

  const lastFetchedMinute = data[data.length - 1]?.minute ?? -1;

  return [...data, ...intradayTail.filter((point) => point.minute > lastFetchedMinute)];
}
