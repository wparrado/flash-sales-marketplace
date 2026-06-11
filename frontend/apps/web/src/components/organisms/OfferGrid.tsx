import type { OfferSummary } from '@flashmkt/app-kernel'
import { OfferCard } from '../molecules/OfferCard'

export function OfferGrid({ offers }: { offers: OfferSummary[] }) {
  if (offers.length === 0) {
    return <p className="hint">No deals match — the flash window may have closed.</p>
  }

  return (
    <div className="offer-grid">
      {offers.map((offer) => (
        <OfferCard key={offer.id} offer={offer} />
      ))}
    </div>
  )
}
