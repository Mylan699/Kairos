import type { AssetClass, Quote, Side, TradeRequest } from '@/api/types';

const ISO_DATE = /^\d{4}-\d{2}-\d{2}$/;

export const PRICE_DECIMALS: Record<AssetClass, number> = { Equity: 2, Fx: 5, Bond: 3 };

export const QUANTITY_LABELS: Record<AssetClass, string> = {
  Equity: 'Quantité (actions)',
  Fx: 'Nominal (EUR)',
  Bond: 'Nominal (USD)',
};

/** Accepts what people type in France: 1 000,50 as well as 1000.50. */
export function parseNumber(text: string): number | null {
  const normalized = text.replace(/[\s  ]/g, '').replace(',', '.');

  if (normalized === '') {
    return null;
  }

  const value = Number(normalized);
  return Number.isFinite(value) ? value : null;
}

/** A buyer takes the offer, a seller hits the bid. */
export const marketPrice = (quote: Quote | undefined, side: Side): number | undefined =>
  quote && (side === 'Buy' ? quote.ask : quote.bid);

export interface TicketFields {
  instrumentId: string | null;
  side: Side;
  quantity: string;
  price: string;
  counterpartyId: string | null;
  bookId: string | null;
  traderId: string | null;
  tradeDate: string;
  valueDate: string;
}

export interface TicketValidation {
  request: TradeRequest | null;
  errors: Partial<Record<'quantity' | 'price' | 'tradeDate' | 'valueDate', string>>;
}

/** Turns what is in the form into a request, or says what is missing. Empty fields are not errors yet. */
export function validateTicket(fields: TicketFields): TicketValidation {
  const errors: TicketValidation['errors'] = {};
  const quantity = parseNumber(fields.quantity);
  const price = parseNumber(fields.price);

  if (fields.quantity !== '' && (quantity === null || quantity <= 0)) {
    errors.quantity = 'Saisissez une quantité positive.';
  }

  if (fields.price !== '' && (price === null || price <= 0)) {
    errors.price = 'Saisissez un prix positif.';
  }

  if (fields.tradeDate !== '' && !ISO_DATE.test(fields.tradeDate)) {
    errors.tradeDate = 'Format attendu : AAAA-MM-JJ.';
  }

  if (fields.valueDate !== '' && !ISO_DATE.test(fields.valueDate)) {
    errors.valueDate = 'Format attendu : AAAA-MM-JJ.';
  }

  const { instrumentId, counterpartyId, bookId, traderId } = fields;

  if (
    !instrumentId || !counterpartyId || !bookId || !traderId ||
    quantity === null || price === null || Object.keys(errors).length > 0
  ) {
    return { request: null, errors };
  }

  return {
    errors,
    request: {
      instrumentId,
      side: fields.side,
      quantity,
      price,
      counterpartyId,
      bookId,
      traderId,
      tradeDate: fields.tradeDate || undefined,
      valueDate: fields.valueDate || undefined,
    },
  };
}
