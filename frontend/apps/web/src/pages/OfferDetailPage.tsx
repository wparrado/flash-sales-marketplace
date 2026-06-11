import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
import { catalogApi } from '@flashmkt/app-kernel'
import type { OfferSummary } from '@flashmkt/app-kernel'
import { Button } from '@flashmkt/design-system'
import { Price } from '@flashmkt/design-system'
import { Spinner } from '@flashmkt/design-system'
import { StockBadge } from '@flashmkt/design-system'

const STOCK_POLL_MS = 4000

export default function OfferDetailPage() {
  const { offerId } = useParams<{ offerId: string }>()
  const [offer, setOffer] = useState<OfferSummary | null>(null)
  const [liveStock, setLiveStock] = useState<number | null>(null)
  const [error, setError] = useState<string | null>(null)

  useEffect(() => {
    if (!offerId) return
    catalogApi.getOffer(offerId)
      .then((o) => { setOffer(o); setLiveStock(o.stock) })
      .catch((e: Error) => setError(e.message))
  }, [offerId])

  // Live stock: polls the backend in-memory cache (microsecond reads), so a
  // thousand shoppers staring at this page never touch the database.
  useEffect(() => {
    if (!offerId) return
    const handle = setInterval(() => {
      catalogApi.getLiveStock(offerId)
        .then((s) => setLiveStock(s.stock))
        .catch(() => undefined) // stale badge beats a broken page
    }, STOCK_POLL_MS)
    return () => clearInterval(handle)
  }, [offerId])

  if (error) return <div className="alert alert--error">{error}</div>
  if (!offer) return <Spinner />

  const soldOut = (liveStock ?? 0) <= 0

  return (
    <div className="detail">
      <div className="detail__visual" aria-hidden>⚡</div>
      <div>
        <StockBadge stock={liveStock} />
        <h1 style={{ margin: '0.6rem 0' }}>{offer.name}</h1>
        <p style={{ color: 'var(--muted)' }}>{offer.description}</p>
        <p>
          <Price amount={offer.price} currency={offer.currency} className="offer-card__price" />
        </p>
        <Link to={soldOut ? '#' : `/checkout/${offer.id}`}>
          <Button disabled={soldOut}>{soldOut ? 'Sold out' : 'Buy now'}</Button>
        </Link>
      </div>
    </div>
  )
}
