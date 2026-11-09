import type { AssetClass, BookSensitivities, RiskReport } from '@/api/types';
import { ASSET_CLASS_LABELS } from '@/lib/labels';
import { parseNumber } from '@/features/trade-ticket/ticketModel';

export interface ShockFields {
  equity: string;
  eurUsd: string;
  rates: string;
}

export interface ShockRequest {
  equityShockPercent: number;
  eurUsdShockPercent: number;
  rateShockBasisPoints: number;
}

export interface ShockValidation {
  request: ShockRequest | null;
  errors: Partial<Record<keyof ShockFields, string>>;
}

const LIMITS = {
  equity: { min: -90, max: 100, message: 'Entre -90 % et +100 %.' },
  eurUsd: { min: -50, max: 50, message: 'Entre -50 % et +50 %.' },
  rates: { min: -500, max: 500, message: 'Entre -500 et +500 pb.' },
} as const;

/** An empty field means no shock on that market. Limits mirror those of the API. */
export function validateShocks(fields: ShockFields): ShockValidation {
  const errors: ShockValidation['errors'] = {};

  const read = (name: keyof ShockFields): number => {
    if (fields[name].trim() === '') {
      return 0;
    }

    const value = parseNumber(fields[name]);
    if (value === null || value < LIMITS[name].min || value > LIMITS[name].max) {
      errors[name] = LIMITS[name].message;
      return 0;
    }

    return value;
  };

  const request = {
    equityShockPercent: read('equity'),
    eurUsdShockPercent: read('eurUsd'),
    rateShockBasisPoints: read('rates'),
  };

  return { request: Object.keys(errors).length === 0 ? request : null, errors };
}

export interface SensitivityFigures {
  equityDelta: number;
  fxDelta: number;
  dv01: number;
}

export function sensitivitiesFor(report: RiskReport, bookId: string | null): SensitivityFigures {
  const book: BookSensitivities | undefined = bookId ? report.books.find((item) => item.bookId === bookId) : undefined;

  if (bookId) {
    return { equityDelta: book?.equityDelta ?? 0, fxDelta: book?.fxDelta ?? 0, dv01: book?.dv01 ?? 0 };
  }

  return { equityDelta: report.equityDelta, fxDelta: report.fxDelta, dv01: report.dv01 };
}

export const assetClassKeyLabel = (key: string): string => ASSET_CLASS_LABELS[key as AssetClass] ?? key;
