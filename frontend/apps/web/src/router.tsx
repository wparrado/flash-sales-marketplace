/* eslint-disable react-refresh/only-export-components --
   Route table, not a hot-reloadable component module: React.lazy chunk
   boundaries live here on purpose. */
import { lazy } from 'react'
import { createBrowserRouter } from 'react-router-dom'
import { ProtectedRoute } from '@flashmkt/app-kernel'
import { Layout } from './components/templates/Layout'

// Route-level code splitting: each page is its own chunk, fetched on demand.
// Keeps the initial bundle lean → better LCP on the catalog (the landing page).
const CatalogPage = lazy(() => import('./pages/CatalogPage'))
const OfferDetailPage = lazy(() => import('./pages/OfferDetailPage'))
const CheckoutPage = lazy(() => import('./pages/CheckoutPage'))
const LoginPage = lazy(() => import('./pages/LoginPage'))

export const router = createBrowserRouter([
  {
    element: <Layout />,
    children: [
      { path: '/', element: <CatalogPage /> },
      { path: '/offers/:offerId', element: <OfferDetailPage /> },
      { path: '/login', element: <LoginPage /> },
      {
        path: '/checkout/:offerId',
        element: (
          <ProtectedRoute>
            <CheckoutPage />
          </ProtectedRoute>
        ),
      },
    ],
  },
])
