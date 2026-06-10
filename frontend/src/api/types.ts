/**
 * API contract layer — mirrors the backend DTOs in FlashSales.Api/Contracts.
 * Backend contract-shape tests (ContractShapeTests.cs) pin the same shapes from
 * the consumer side, so a breaking payload change fails CI before reaching us.
 */

export interface OfferSummary {
  id: string
  name: string
  description: string
  price: number
  currency: string
  stock: number
  endsAt: string
}

export interface LoginRequest {
  username: string
  password: string
}

export interface LoginResponse {
  token: string
  username: string
  expiresAt: string
}

export interface CheckoutRequest {
  offerId: string
  quantity: number
  paymentMethod: string
  couponCode: string | null
}

export interface OrderResponse {
  orderId: string
  status: string
  total: number
  currency: string
  paymentReference: string | null
}

export interface StockResponse {
  offerId: string
  stock: number | null
}

export interface ErrorResponse {
  code: string
  message: string
  correlationId: string
}
