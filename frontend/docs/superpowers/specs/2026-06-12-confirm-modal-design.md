# Confirmation Modal — Design Spec

**Date:** 2026-06-12
**Scope:** `apps/web` — app-local reusable component

## Goal

Intercept the "Buy now" action on `OfferDetailPage` with a confirmation modal before navigating to checkout. The modal component must be reusable across other pages in the same app.

## Component location

```
apps/web/src/components/molecules/ConfirmModal.tsx
```

Single file exports two things: the `useConfirmModal` hook and the `ConfirmModal` component.

## API

### `useConfirmModal()`

```tsx
const { isOpen, open, close } = useConfirmModal()
```

- `isOpen: boolean` — controls modal visibility
- `open: () => void` — sets `isOpen` to `true`
- `close: () => void` — sets `isOpen` to `false`

### `<ConfirmModal>`

```tsx
<ConfirmModal
  isOpen={isOpen}
  message="Are you sure you want to proceed?"
  onConfirm={() => navigate(`/checkout/${offer.id}`)}
  onCancel={close}
/>
```

Props:

| Prop | Type | Description |
|---|---|---|
| `isOpen` | `boolean` | Whether the modal is visible |
| `message` | `string` | The confirmation question shown to the user |
| `onConfirm` | `() => void` | Called when user clicks "Confirm" |
| `onCancel` | `() => void` | Called when user clicks "Cancel" or the backdrop |

Returns `null` when `isOpen` is `false`.

## Styling

Two new CSS classes added to `packages/design-system/src/styles.css`:

- **`.modal-backdrop`** — `position: fixed; inset: 0; z-index: 100; background: rgba(0,0,0,0.55); backdrop-filter: blur(4px)`. Clicking it calls `onCancel`.
- **`.modal`** — `z-index: 101; position: fixed; centered via transform`. Reuses `--surface`, `--line`, `--radius` tokens. Short `fadeIn` animation on open.

Buttons inside the modal:
- "Confirm" → `<Button variant="primary">` (volt / yellow-green)
- "Cancel" → `<Button variant="ghost">`

## Changes to `OfferDetailPage`

- Remove `<Link to={...}>` wrapper around the Buy now button.
- Replace with `<Button onClick={open} disabled={soldOut}>`.
- Add `useNavigate` from `react-router-dom`.
- Render `<ConfirmModal>` at the bottom of the JSX with `onConfirm={() => navigate(\`/checkout/\${offer.id}\`)}`.

## What does NOT change

- Router configuration
- `CheckoutPage`
- `packages/design-system` exports (`index.ts`)
- Any other page

## Reuse pattern

Any page that needs a confirmation step imports both exports from `ConfirmModal.tsx`, wires `useConfirmModal()` for state, and provides its own `onConfirm` callback. The message text is caller-supplied, so the component is content-agnostic.
