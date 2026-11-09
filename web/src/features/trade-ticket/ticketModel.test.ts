import { describe, expect, it } from 'vitest';
import type { Quote } from '@/api/types';
import { marketPrice, parseNumber, validateTicket, type TicketFields } from './ticketModel';

const fields = (change: Partial<TicketFields> = {}): TicketFields => ({
  instrumentId: 'i',
  side: 'Buy',
  quantity: '100',
  price: '50,5',
  counterpartyId: 'c',
  bookId: 'b',
  traderId: 't',
  tradeDate: '',
  valueDate: '',
  ...change,
});

describe('parseNumber', () => {
  it.each([
    ['1000.5', 1000.5],
    ['1000,5', 1000.5],
    ['1 000,5', 1000.5],
    ['1 000', 1000],
  ])('reads %s', (text, expected) => {
    expect(parseNumber(text)).toBe(expected);
  });

  it('refuses what is not a number', () => {
    expect(parseNumber('abc')).toBeNull();
    expect(parseNumber('')).toBeNull();
  });
});

describe('marketPrice', () => {
  const quote = { bid: 1.1, ask: 1.2 } as Quote;

  it('gives the ask to a buyer and the bid to a seller', () => {
    expect(marketPrice(quote, 'Buy')).toBe(1.2);
    expect(marketPrice(quote, 'Sell')).toBe(1.1);
  });

  it('gives nothing until a quote is known', () => {
    expect(marketPrice(undefined, 'Buy')).toBeUndefined();
  });
});

describe('validateTicket', () => {
  it('builds the request of a complete ticket', () => {
    expect(validateTicket(fields()).request).toMatchObject({ instrumentId: 'i', side: 'Buy', quantity: 100, price: 50.5 });
  });

  it('leaves dates out when they are empty, so that the server defaults apply', () => {
    const { request } = validateTicket(fields());

    expect(request?.tradeDate).toBeUndefined();
    expect(request?.valueDate).toBeUndefined();
  });

  it('does not complain about a field that is still empty', () => {
    const { request, errors } = validateTicket(fields({ quantity: '' }));

    expect(request).toBeNull();
    expect(errors).toEqual({});
  });

  it.each(['0', '-5', 'abc'])('rejects a quantity of %s', (quantity) => {
    expect(validateTicket(fields({ quantity })).errors.quantity).toBeDefined();
  });

  it('rejects a non-positive price', () => {
    expect(validateTicket(fields({ price: '0' })).errors.price).toBeDefined();
  });

  it('rejects a badly formatted date', () => {
    const { request, errors } = validateTicket(fields({ tradeDate: '24/06/2016' }));

    expect(request).toBeNull();
    expect(errors.tradeDate).toBeDefined();
  });

  it('needs a book, a trader, a counterparty and an instrument', () => {
    expect(validateTicket(fields({ bookId: null })).request).toBeNull();
    expect(validateTicket(fields({ traderId: null })).request).toBeNull();
    expect(validateTicket(fields({ counterpartyId: null })).request).toBeNull();
    expect(validateTicket(fields({ instrumentId: null })).request).toBeNull();
  });
});
