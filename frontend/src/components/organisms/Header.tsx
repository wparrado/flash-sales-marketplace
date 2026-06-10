import { Link, useNavigate } from 'react-router-dom'
import { useAuth } from '../../auth/auth-context'
import { Button } from '../atoms/Button'

const TICKER_ITEMS = '⚡ LIVE FLASH DROPS — LIMITED STOCK — NO RESTOCKS — '

export function Header() {
  const { isAuthenticated, username, logout } = useAuth()
  const navigate = useNavigate()

  return (
    <header className="header">
      <div className="ticker" aria-hidden>
        <div className="ticker__track">{TICKER_ITEMS.repeat(6)}</div>
      </div>
      <div className="header__inner">
        <Link to="/" className="header__logo">
          FLASH<em>//</em>MKT
        </Link>
        <nav className="header__nav">
          {isAuthenticated ? (
            <>
              <span className="header__user">@{username}</span>
              <Button variant="ghost" onClick={() => { logout(); navigate('/') }}>
                Log out
              </Button>
            </>
          ) : (
            <Button variant="ghost" onClick={() => navigate('/login')}>
              Log in
            </Button>
          )}
        </nav>
      </div>
    </header>
  )
}
