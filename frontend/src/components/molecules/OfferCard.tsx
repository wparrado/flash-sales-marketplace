import { Link } from 'react-router-dom'
import type { OfferSummary } from '../../api/types'
import { Price } from '../atoms/Price'
import { StockBadge } from '../atoms/StockBadge'

function endsIn(endsAt: string): string {
  const ms = new Date(endsAt).getTime() - Date.now()
  if (ms <= 0) return 'ended'
  const hours = Math.floor(ms / 3_600_000)
  const minutes = Math.floor((ms % 3_600_000) / 60_000)
  return hours > 0 ? `ends in ${hours}h ${minutes}m` : `ends in ${minutes}m`
}

export function OfferCard({ offer }: { offer: OfferSummary }) {
  return (
    <Link to={`/offers/${offer.id}`} className="offer-card">
      <StockBadge stock={offer.stock} />
      <h3 className="offer-card__name">{offer.name}</h3>
      <p className="offer-card__desc">{offer.description}</p>
      <div className="offer-card__meta">
        <Price amount={offer.price} currency={offer.currency} className="offer-card__price" />
        <span className="offer-card__ends">{endsIn(offer.endsAt)}</span>
      </div>
    </Link>
  )
}
