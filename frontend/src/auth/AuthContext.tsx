import { useCallback, useMemo, useState } from 'react'
import type { ReactNode } from 'react'
import { tokenStore } from '../api/client'
import { authApi } from '../api/services'
import { AuthContext } from './auth-context'
import type { AuthState } from './auth-context'

const USERNAME_KEY = 'flashmkt.username'

export function AuthProvider({ children }: { children: ReactNode }) {
  const [username, setUsername] = useState<string | null>(
    () => (tokenStore.get() ? sessionStorage.getItem(USERNAME_KEY) : null),
  )

  const login = useCallback(async (user: string, password: string) => {
    const session = await authApi.login({ username: user, password })
    tokenStore.set(session.token)
    sessionStorage.setItem(USERNAME_KEY, session.username)
    setUsername(session.username)
  }, [])

  const logout = useCallback(() => {
    tokenStore.clear()
    sessionStorage.removeItem(USERNAME_KEY)
    setUsername(null)
  }, [])

  const value = useMemo<AuthState>(
    () => ({ username, isAuthenticated: username !== null, login, logout }),
    [username, login, logout],
  )

  return <AuthContext value={value}>{children}</AuthContext>
}
