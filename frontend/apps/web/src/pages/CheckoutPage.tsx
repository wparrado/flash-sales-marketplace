import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import type { FormEvent } from 'react'
import { ApiError } from '@flashmkt/app-kernel'
import { catalogApi, checkoutApi } from '@flashmkt/app-kernel'
import type { OfferSummary, OrderResponse } from '@flashmkt/app-kernel'
import { Button } from '@flashmkt/design-system'
import { Input } from '@flashmkt/design-system'
import { Price } from '@flashmkt/design-system'
import { Spinner } from '@flashmkt/design-system'

// Special methods exercise the backend resilience pipeline from the UI:
// DeclinedCard → hard decline + stock compensation; FlakyCard → provider
// outage → Polly retry + circuit breaker → graceful 402.
const PAYMENT_METHODS = ['CreditCard', 'DebitCard', 'DeclinedCard', 'FlakyCard'] as const

export default function CheckoutPage() {
  const { offerId } = useParams<{ offerId: string }>()
  const [offer, setOffer] = useState<OfferSummary | null>(null)
  const [quantity, setQuantity] = useState(1)
  const [paymentMethod, setPaymentMethod] = useState<string>(PAYMENT_METHODS[0])
  const [coupon, setCoupon] = useState('')
  const [submitting, setSubmitting] = useState(false)
  const [confirmation, setConfirmation] = useState<OrderResponse | null>(null)
  const [error, setError] = useState<ApiError | null>(null)

  useEffect(() => {
    if (offerId) catalogApi.getOffer(offerId).then(setOffer).catch(() => setOffer(null))
  }, [offerId])

  if (!offerId) return <div className="alert alert--error">Missing offer reference.</div>
  if (!offer) return <Spinner />

  const subtotal = offer.price * quantity

  async function submit(event: FormEvent) {
    event.preventDefault()
    setSubmitting(true)
    setError(null)
    try {
      setConfirmation(await checkoutApi.placeOrder({
        offerId: offerId!,
        quantity,
        paymentMethod,
        couponCode: coupon.trim() === '' ? null : coupon.trim(),
      }))
    } catch (e) {
      setError(e instanceof ApiError
        ? e
        : new ApiError(0, 'network.error', String(e), 'n/a'))
    } finally {
      setSubmitting(false)
    }
  }

  if (confirmation) {
    return (
      <div className="panel">
        <h2>Order confirmed ⚡</h2>
        <div className="alert alert--success">
          Payment accepted — reference {confirmation.paymentReference}
        </div>
        <div className="summary-row"><span>Order</span><span>{confirmation.orderId}</span></div>
        <div className="summary-row"><span>Status</span><span>{confirmation.status}</span></div>
        <div className="summary-row summary-row--total">
          <span>Charged</span>
          <Price amount={confirmation.total} currency={confirmation.currency} />
        </div>
        <p style={{ marginTop: 'var(--space-4)' }}>
          <Link to="/"><Button variant="ghost">Back to deals</Button></Link>
        </p>
      </div>
    )
  }

  return (
    <div className="panel">
      <h2>Checkout — {offer.name}</h2>

      {error && (
        <div className="alert alert--error">
          {error.message}
          <code>code: {error.code} · correlation: {error.correlationId}</code>
        </div>
      )}

      <form onSubmit={submit}>
        <div className="field">
          <label htmlFor="qty">Quantity</label>
          <Input
            id="qty"
            type="number"
            min={1}
            max={10}
            value={quantity}
            onChange={(e) => setQuantity(Number(e.target.value))}
          />
        </div>

        <div className="field">
          <label htmlFor="method">Payment method</label>
          <select
            id="method"
            className="input"
            value={paymentMethod}
            onChange={(e) => setPaymentMethod(e.target.value)}
          >
            {PAYMENT_METHODS.map((method) => (
              <option key={method} value={method}>{method}</option>
            ))}
          </select>
          <p className="hint">
            <code>DeclinedCard</code> and <code>FlakyCard</code> demo the
            payment resilience pipeline (compensation, retry, circuit breaker).
          </p>
        </div>

        <div className="field">
          <label htmlFor="coupon">Coupon (optional)</label>
          <Input
            id="coupon"
            placeholder="FLASH10 / VIP20"
            value={coupon}
            onChange={(e) => setCoupon(e.target.value)}
          />
        </div>

        <div className="summary-row">
          <span>Subtotal ({quantity}×)</span>
          <Price amount={subtotal} currency={offer.currency} />
        </div>
        <div className="summary-row">
          <span>Tax (19%) &amp; discounts</span>
          <span className="hint">computed server-side</span>
        </div>

        <p style={{ marginTop: 'var(--space-4)' }}>
          <Button type="submit" disabled={submitting}>
            {submitting ? 'Charging…' : 'Pay now'}
          </Button>
        </p>
      </form>
    </div>
  )
}
