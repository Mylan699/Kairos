import { afterEach, describe, expect, it, vi } from 'vitest';
import { ApiError, apiGet, apiPost, describeError } from './client';

const respondWith = (status: number, body: unknown) =>
  vi.stubGlobal('fetch', vi.fn(async () => new Response(JSON.stringify(body), { status })));

afterEach(() => vi.unstubAllGlobals());

describe('api client', () => {
  it('returns the parsed body of a success', async () => {
    respondWith(200, { value: 1 });

    await expect(apiGet('/api/x')).resolves.toEqual({ value: 1 });
  });

  it('sends a body as json', async () => {
    respondWith(200, {});

    await apiPost('/api/x', { quantity: 5 });

    expect(fetch).toHaveBeenCalledWith('/api/x', expect.objectContaining({
      method: 'POST',
      body: '{"quantity":5}',
    }));
  });

  it('turns a problem response into an ApiError', async () => {
    respondWith(400, { title: 'Valeurs invalides', errors: { Quantity: ['Requis'] } });

    const error = await apiGet('/api/x').catch((reason: unknown) => reason);

    expect(error).toBeInstanceOf(ApiError);
    expect((error as ApiError).status).toBe(400);
    expect((error as ApiError).fieldErrors.Quantity).toEqual(['Requis']);
  });

  it('survives an error response that is not json', async () => {
    vi.stubGlobal('fetch', vi.fn(async () => new Response('Bad gateway', { status: 502 })));

    await expect(apiGet('/api/x')).rejects.toMatchObject({ status: 502 });
  });
});

describe('describeError', () => {
  it('translates a known business rule', () => {
    const error = new ApiError(422, 'Refusé', 'A trade cannot move from New to Settled.');

    expect(describeError(error)).toBe("Ce changement de statut n'est pas autorisé.");
  });

  it('falls back on the title sent by the server', () => {
    expect(describeError(new ApiError(429, 'Trop de requêtes'))).toBe('Trop de requêtes');
  });

  it('explains a network failure', () => {
    expect(describeError(new TypeError('Failed to fetch'))).toContain('ne répond pas');
  });
});
