import { Suspense } from 'react'
import { Outlet } from 'react-router-dom'
import { Spinner } from '@flashmkt/design-system'
import { Footer } from '../organisms/Footer'
import { Header } from '../organisms/Header'

/**
 * App shell. Pages render lazily inside the single Suspense boundary, so each
 * route ships as its own chunk (route-level code splitting → smaller LCP).
 */
export function Layout() {
  return (
    <>
      <Header />
      <main className="layout__main">
        <Suspense fallback={<Spinner />}>
          <Outlet />
        </Suspense>
      </main>
      <Footer />
    </>
  )
}
