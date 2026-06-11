const LOW_STOCK_THRESHOLD = 10

/** Urgency-coded live stock indicator. */
export function StockBadge({ stock }: { stock: number | null }) {
  if (stock === null) return <span className="badge badge--out">unknown</span>
  if (stock <= 0) return <span className="badge badge--out">Sold out</span>
  if (stock <= LOW_STOCK_THRESHOLD) {
    return <span className="badge badge--low">⚡ Only {stock} left</span>
  }
  return <span className="badge badge--in-stock">In stock · {stock}</span>
}
