import type { ErrorResponse } from './types'

const TOKEN_KEY = 'flashmkt.token'

export const tokenStore = {
  get: (): string | null => sessionStorage.getItem(TOKEN_KEY),
  set: (token: string) => sessionStorage.setItem(TOKEN_KEY, token),
  clear: () => sessionStorage.removeItem(TOKEN_KEY),
}

/** Typed API error carrying the backend error envelope + correlation id. */
export class ApiError extends Error {
  readonly status: number
  readonly code: string
  readonly correlationId: string

  constructor(status: number, code: string, message: string, correlationId: string) {
    super(message)
    this.name = 'ApiError'
    this.status = status
    this.code = code
    this.correlationId = correlationId
  }
}

/**
 * Single fetch wrapper for the whole app. Every request:
 *  - carries a fresh X-Correlation-ID (traceable end-to-end in backend logs
 *    and OpenTelemetry traces),
 *  - attaches the JWT when a session exists,
 *  - normalizes failures into typed ApiError instances.
 */
export async function apiFetch<T>(path: string, init: RequestInit = {}): Promise<T> {
  const correlationId = crypto.randomUUID()
  const headers = new Headers(init.headers)
  headers.set('X-Correlation-ID', correlationId)
  headers.set('Content-Type', 'application/json')

  const token = tokenStore.get()
  if (token) headers.set('Authorization', `Bearer ${token}`)

  const response = await fetch(path, { ...init, headers })

  if (!response.ok) {
    const fallback: ErrorResponse = {
      code: `http.${response.status}`,
      message: response.statusText || 'Request failed',
      correlationId,
    }
    const body = await response.json().catch(() => fallback) as ErrorResponse
    throw new ApiError(
      response.status,
      body.code ?? fallback.code,
      body.message ?? fallback.message,
      body.correlationId ?? correlationId,
    )
  }

  return response.json() as Promise<T>
}
