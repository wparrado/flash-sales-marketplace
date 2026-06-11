import { useEffect, useState } from 'react'
import { Input } from '@flashmkt/design-system'

const DEBOUNCE_MS = 300

/**
 * Debounced search box: keystrokes settle for 300ms before the query reaches
 * the backend fuzzy engine, keeping the hot search path cheap.
 */
export function SearchBox({ onSearch }: { onSearch: (query: string) => void }) {
  const [value, setValue] = useState('')

  useEffect(() => {
    const handle = setTimeout(() => onSearch(value.trim()), DEBOUNCE_MS)
    return () => clearTimeout(handle)
  }, [value, onSearch])

  return (
    <div className="searchbox">
      <span className="searchbox__icon" aria-hidden>⌕</span>
      <Input
        type="search"
        placeholder="Search deals — typos welcome (try “nintnedo”)"
        value={value}
        onChange={(e) => setValue(e.target.value)}
        aria-label="Search offers"
      />
    </div>
  )
}
