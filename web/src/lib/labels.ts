import type { AssetClass } from './format';
import type { AuditAction, Side, TradeStatus } from '@/api/types';

export const ASSET_CLASS_LABELS: Record<AssetClass, string> = {
  Equity: 'Actions',
  Fx: 'Change',
  Bond: 'Obligations',
};

export const SIDE_LABELS: Record<Side, string> = { Buy: 'Achat', Sell: 'Vente' };

export const STATUS_LABELS: Record<TradeStatus, string> = {
  New: 'Nouveau',
  Validated: 'Validé',
  Settled: 'Réglé',
  Cancelled: 'Annulé',
};

export const AUDIT_LABELS: Record<AuditAction, string> = {
  Booked: 'Saisie',
  Validated: 'Validation',
  Settled: 'Règlement',
  Cancelled: 'Annulation',
  Amended: 'Amendement',
};

const SECTOR_LABELS: Record<string, string> = {
  Technology: 'Technologie',
  'Communication Services': 'Communication',
  'Consumer Cyclical': 'Consommation cyclique',
  'Consumer Defensive': 'Consommation de base',
  Energy: 'Énergie',
  'Financial Services': 'Services financiers',
  Healthcare: 'Santé',
  Industrials: 'Industrie',
  'Real Estate': 'Immobilier',
  Utilities: 'Services publics',
  'Basic Materials': 'Matériaux de base',
};

export const sectorLabel = (sector: string): string => SECTOR_LABELS[sector] ?? sector;
