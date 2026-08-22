import toast from 'react-hot-toast';

interface ApiErrorBody {
  error?: string;
  message?: string;
  title?: string;
  errors?: Record<string, string[]>;
}

/**
 * Extracts a human-readable error message from any value thrown by the axios
 * API client. Tries the server's `ApiResponse.error` field first, then
 * falls back to the HTTP status code, and finally to a generic message.
 */
export function parseApiError(error: unknown, fallback = 'An unexpected error occurred.'): string {
  if (!error) return fallback;

  if (typeof error === 'object' && error !== null) {
    // Axios error — has a `.response` property
    const e = error as {
      response?: { data?: ApiErrorBody; status?: number };
      message?: string;
      code?: string;
    };

    if (e.response) {
      const d = e.response.data;

      if (d?.error) return d.error;
      if (d?.message) return d.message;
      if (d?.title) return d.title;
      if (d?.errors) {
        const first = Object.values(d.errors)[0];
        if (first?.[0]) return first[0];
      }

      switch (e.response.status) {
        case 400: return 'Invalid request — please check your input.';
        case 401: return 'Your session has expired. Please log in again.';
        case 402: return 'Your subscription plan does not allow this action. Visit the Licensing page to upgrade.';
        case 403: return 'You do not have permission to perform this action.';
        case 404: return 'The requested resource was not found.';
        case 409: return 'A conflict occurred. Please refresh and try again.';
        case 422: return 'Validation failed — please check your input.';
        case 429: return 'Too many requests. Please wait a moment and try again.';
        case 500: return 'A server error occurred. Please try again later.';
        case 503: return 'The service is temporarily unavailable.';
        default: return fallback;
      }
    }

    // Network error — no response received
    if (e.code === 'ERR_NETWORK' || e.code === 'ECONNREFUSED') {
      return 'Cannot reach the server. Check your network connection.';
    }
    if (e.message) return e.message;
  }

  if (error instanceof Error) return error.message;

  return fallback;
}

/**
 * Returns true when the error is an HTTP 402 (Payment Required) response —
 * indicating the studio has hit a subscription plan limit.
 */
export function isSubscriptionLimitError(error: unknown): boolean {
  if (!error || typeof error !== 'object') return false;
  const e = error as { response?: { status?: number } };
  return e.response?.status === 402;
}

/**
 * Shows a styled upgrade toast for subscription-limit (402) errors.
 * Falls back to a regular error toast for other errors.
 */
export function apiErrorWithUpgrade(error: unknown, fallback?: string): void {
  const message = parseApiError(error, fallback);

  if (isSubscriptionLimitError(error)) {
    toast(`🔒 ${message}`, {
      id: `sub-limit-${message.slice(0, 60)}`,
      duration: 8000,
      style: {
        background: '#1e1b4b',
        color: '#c7d2fe',
        border: '1px solid #4338ca',
        maxWidth: '440px',
        fontSize: '13px',
      },
    });
  } else {
    toast.error(message, { id: message, duration: 5000 });
  }
}

/**
 * Parses the error and shows a toast. Uses the message as the toast ID so the same error never stacks.
 */
export function apiError(error: unknown, fallback?: string): void {
  const message = parseApiError(error, fallback);
  toast.error(message, { id: message, duration: 5000 });
}

/**
 * Shows an amber warning toast. Deduplicates by message.
 */
export function apiWarn(message: string): void {
  toast(message, { id: message, duration: 5000, icon: '⚠️' });
}
