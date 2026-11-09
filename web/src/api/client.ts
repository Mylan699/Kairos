const BASE_URL: string = import.meta.env.VITE_API_URL ?? '';

interface ProblemDetails {
  title?: string;
  detail?: string;
  errors?: Record<string, string[]>;
}

/** An error returned by the API, in its ProblemDetails form. */
export class ApiError extends Error {
  constructor(
    public readonly status: number,
    message: string,
    public readonly detail?: string,
    public readonly fieldErrors: Record<string, string[]> = {},
  ) {
    super(message);
    this.name = 'ApiError';
  }
}

// Business rules are worded in English in the domain. These are the ones a user can reach.
const BUSINESS_RULE_MESSAGES: Array<[RegExp, string]> = [
  [/cannot move from/, "Ce changement de statut n'est pas autorisé."],
  [/cannot be amended/, 'Ce trade ne peut plus être amendé.'],
  [/must change at least one term/, "L'amendement ne modifie aucune valeur."],
  [/after the current market date/, 'La date de trade ne peut pas dépasser la date de marché.'],
  [/is not tradable on/, "Cet instrument n'est pas négociable à cette date."],
  [/is not a market day/, "Cette date n'est pas une journée de marché."],
  [/cannot be replayed/, 'Cette journée ne peut pas être rejouée.'],
  [/value date/i, 'La date de valeur est invalide.'],
];

/** The sentence to show to the user for any failure. */
export function describeError(error: unknown): string {
  if (error instanceof ApiError) {
    const translated = BUSINESS_RULE_MESSAGES.find(([pattern]) => error.detail && pattern.test(error.detail));
    return translated?.[1] ?? error.message;
  }

  return 'Le serveur ne répond pas. Vérifiez la connexion et réessayez.';
}

async function toApiError(response: Response): Promise<ApiError> {
  try {
    const problem = (await response.json()) as ProblemDetails;
    return new ApiError(response.status, problem.title ?? 'La demande a échoué.', problem.detail, problem.errors);
  } catch {
    return new ApiError(response.status, 'La demande a échoué.');
  }
}

async function request<T>(path: string, init?: RequestInit): Promise<T> {
  const response = await fetch(`${BASE_URL}${path}`, init);

  if (!response.ok) {
    throw await toApiError(response);
  }

  return (await response.json()) as T;
}

export const apiGet = <T>(path: string, signal?: AbortSignal) => request<T>(path, { signal });

const withBody = (method: string, body: unknown): RequestInit => ({
  method,
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify(body ?? {}),
});

export const apiPost = <T>(path: string, body?: unknown) => request<T>(path, withBody('POST', body));

export const apiPut = <T>(path: string, body: unknown) => request<T>(path, withBody('PUT', body));

export const hubUrl = `${BASE_URL}/hubs/market`;
