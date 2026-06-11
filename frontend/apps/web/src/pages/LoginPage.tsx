import { useState } from 'react'
import { useLocation, useNavigate } from 'react-router-dom'
import type { FormEvent } from 'react'
import { useAuth } from '@flashmkt/app-kernel'
import { Button } from '@flashmkt/design-system'
import { Input } from '@flashmkt/design-system'

export default function LoginPage() {
  const { login } = useAuth()
  const navigate = useNavigate()
  const location = useLocation()
  const [username, setUsername] = useState('demo')
  const [password, setPassword] = useState('')
  const [error, setError] = useState<string | null>(null)
  const [submitting, setSubmitting] = useState(false)

  const from = (location.state as { from?: { pathname: string } } | null)?.from?.pathname ?? '/'

  async function submit(event: FormEvent) {
    event.preventDefault()
    setSubmitting(true)
    setError(null)
    try {
      await login(username, password)
      navigate(from, { replace: true })
    } catch {
      setError('Invalid credentials. Try demo / demo123.')
    } finally {
      setSubmitting(false)
    }
  }

  return (
    <div className="panel">
      <h2>Log in to buy</h2>
      <p className="hint">
        Demo users: <code>demo / demo123</code> or <code>admin / admin123</code>
      </p>

      {error && <div className="alert alert--error">{error}</div>}

      <form onSubmit={submit}>
        <div className="field">
          <label htmlFor="username">Username</label>
          <Input
            id="username"
            value={username}
            onChange={(e) => setUsername(e.target.value)}
            autoComplete="username"
          />
        </div>
        <div className="field">
          <label htmlFor="password">Password</label>
          <Input
            id="password"
            type="password"
            value={password}
            onChange={(e) => setPassword(e.target.value)}
            autoComplete="current-password"
          />
        </div>
        <Button type="submit" disabled={submitting}>
          {submitting ? 'Signing in…' : 'Sign in'}
        </Button>
      </form>
    </div>
  )
}
