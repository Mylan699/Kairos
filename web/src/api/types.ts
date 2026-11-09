import type { AssetClass } from '@/lib/format';

export type { AssetClass };

export type Side = 'Buy' | 'Sell';
export type TradeStatus = 'New' | 'Validated' | 'Settled' | 'Cancelled';
export type AuditAction = 'Booked' | 'Validated' | 'Settled' | 'Cancelled' | 'Amended';

export interface Trade {
  id: string;
  version: number;
  status: TradeStatus;
  instrumentId: string;
  symbol: string;
  assetClass: AssetClass;
  side: Side;
  quantity: number;
  price: number;
  counterpartyId: string;
  counterparty: string;
  bookId: string;
  book: string;
  traderId: string;
  trader: string;
  tradeDate: string;
  valueDate: string;
  updatedAt: string;
}

export interface TradeVersion {
  number: number;
  side: Side;
  quantity: number;
  price: number;
  counterparty: string;
  book: string;
  trader: string;
  tradeDate: string;
  valueDate: string;
  createdAt: string;
  createdBy: string;
}

export interface AuditEntry {
  at: string;
  actor: string;
  action: AuditAction;
  resultingStatus: TradeStatus;
  version: number;
  details: string;
}

export interface TradeDetail {
  trade: Trade;
  versions: TradeVersion[];
  auditTrail: AuditEntry[];
}

export interface TradeRequest {
  instrumentId: string;
  side: Side;
  quantity: number;
  price: number;
  counterpartyId: string;
  bookId: string;
  traderId: string;
  tradeDate?: string;
  valueDate?: string;
}

export interface TradeImpact {
  notionalEur: number;
  pnlImpactEur: number;
  positionBefore: number;
  positionAfter: number;
  dv01ChangeEur: number;
  totalDv01AfterEur: number;
}

export interface PnlFigures {
  realized: number;
  unrealized: number;
  priceEffect: number;
  fxEffect: number;
  total: number;
  day: number;
}

export interface Position {
  bookId: string;
  book: string;
  instrumentId: string;
  symbol: string;
  assetClass: AssetClass;
  sector: string | null;
  quantity: number;
  averagePrice: number;
  markPrice: number;
  marketValue: number;
  pnl: PnlFigures;
}

export interface BookSummary {
  bookId: string;
  book: string;
  marketValue: number;
  pnl: PnlFigures;
}

export interface PortfolioSummary {
  marketDate: string;
  books: BookSummary[];
  total: PnlFigures;
}

export interface Contributor {
  symbol: string;
  assetClass: AssetClass;
  dayPnl: number;
}

export interface AssetClassExposure {
  assetClass: AssetClass;
  marketValue: number;
  dayPnl: number;
}

export interface PnlHistoryPoint {
  date: string;
  total: number;
}

export interface Dashboard {
  summary: PortfolioSummary;
  assetClasses: AssetClassExposure[];
  topContributors: Contributor[];
  bottomContributors: Contributor[];
  history: PnlHistoryPoint[];
}

export interface ExposureLine {
  key: string;
  eur: number;
}

export interface BookSensitivities {
  bookId: string;
  book: string;
  equityDelta: number;
  fxDelta: number;
  dv01: number;
}

export interface RiskReport {
  marketDate: string;
  byAssetClass: ExposureLine[];
  byCurrency: ExposureLine[];
  bySector: ExposureLine[];
  equityDelta: number;
  fxDelta: number;
  dv01: number;
  books: BookSensitivities[];
}

export interface StressResult {
  scenario: string;
  totalEur: number;
  byBook: Array<{ bookId: string; book: string; eur: number }>;
  byAssetClass: ExposureLine[];
}

export interface HistoricalScenario {
  id: string;
  name: string;
  description: string;
  from: string;
  to: string;
}

export interface Instrument {
  id: string;
  symbol: string;
  assetClass: AssetClass;
  sector: string | null;
  isTradable: boolean;
  maturityDate: string | null;
  couponRate: number | null;
}

export interface ReferenceItem {
  id: string;
  name: string;
}

export interface ReferenceData {
  instruments: Instrument[];
  books: ReferenceItem[];
  traders: ReferenceItem[];
  counterparties: ReferenceItem[];
}

export interface DailyBar {
  date: string;
  open: number;
  high: number;
  low: number;
  close: number;
}

export interface CurvePoint {
  tenor: string;
  months: number;
  yieldPercent: number;
}

export interface Curve {
  date: string;
  points: CurvePoint[];
}

export interface ReplayStatus {
  date: string;
  minute: number;
  speed: number;
  isPlaying: boolean;
  sessionOpenMinute: number;
  sessionCloseMinute: number;
}

export interface RemarkableDay {
  date: string;
  name: string;
  description: string;
  startMinute: number;
}

export interface Quote {
  instrumentId: string;
  symbol: string;
  assetClass: AssetClass;
  bid: number;
  ask: number;
  last: number;
  changePercent: number;
  yieldPercent: number | null;
}

export interface IntradayPoint {
  minute: number;
  eurUsd: number;
  equityBasketPercent: number;
  tenYearPercent: number;
  dayPnl: number;
}

export interface IntradayPrice {
  minute: number;
  price: number;
}

export interface MarketTick {
  status: ReplayStatus;
  quotes: Quote[];
  curve: Curve;
  summary: PortfolioSummary;
}
