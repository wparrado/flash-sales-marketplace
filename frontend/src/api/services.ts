import { apiFetch } from './client'
import type {
  CheckoutRequest,
  LoginRequest,
  LoginResponse,
  OfferSummary,
  OrderResponse,
  StockResponse,
} from './types'

export const authApi = {
  login: (request: LoginRequest) =>
    apiFetch<LoginResponse>('/api/auth/login', {
      method: 'POST',
      body: JSON.stringify(request),
    }),
}

export const catalogApi = {
  listOffers: (limit = 50) =>
    apiFetch<OfferSummary[]>(`/api/catalog/offers?limit=${limit}`),

  getOffer: (offerId: string) =>
    apiFetch<OfferSummary>(`/api/catalog/offers/${offerId}`),

  /** Served from the backend in-memory cache — safe to poll aggressively. */
  getLiveStock: (offerId: string) =>
    apiFetch<StockResponse>(`/api/catalog/offers/${offerId}/stock`),

  search: (query: string, limit = 20) =>
    apiFetch<OfferSummary[]>(`/api/catalog/search?q=${encodeURIComponent(query)}&limit=${limit}`),
}

export const checkoutApi = {
  placeOrder: (request: CheckoutRequest) =>
    apiFetch<OrderResponse>('/api/checkout/orders', {
      method: 'POST',
      body: JSON.stringify(request),
    }),
}
