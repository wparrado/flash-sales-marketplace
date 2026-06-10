export function Price({ amount, currency, className = '' }: {
  amount: number
  currency: string
  className?: string
}) {
  const formatted = new Intl.NumberFormat('en-US', { style: 'currency', currency }).format(amount)
  return <span className={`price ${className}`.trim()}>{formatted}</span>
}
