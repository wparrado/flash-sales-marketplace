import { useCallback, useEffect, useState } from 'react'
import { catalogApi } from '../api/services'
import type { OfferSummary } from '../api/types'
import { Spinner } from '../components/atoms/Spinner'
import { SearchBox } from '../components/molecules/SearchBox'
import { OfferGrid } from '../components/organisms/OfferGrid'

export default function CatalogPage() {
  const [offers, setOffers] = useState<OfferSummary[] | null>(null)
  const [error, setError] = useState<string | null>(null)

  const runQuery = useCallback((query: string) => {
    const request = query.length > 1 ? catalogApi.search(query) : catalogApi.listOffers()
    request
      .then(setOffers)
      .catch((e: Error) => setError(e.message))
  }, [])

  useEffect(() => runQuery(''), [runQuery])

  return (
    <>
      <section className="hero">
        <h1>
          Flash deals.<br />
          <span>Gone in minutes.</span>
        </h1>
        <p>
          Limited stock from verified sellers. Stock counters are live — when
          it says sold out, it is sold out.
        </p>
      </section>

      <SearchBox onSearch={runQuery} />

      {error && <div className="alert alert--error">{error}</div>}
      {offers === null ? <Spinner /> : <OfferGrid offers={offers} />}
    </>
  )
}
