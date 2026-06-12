# Confirmation Modal Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Add a reusable confirmation modal to `OfferDetailPage` that intercepts the "Buy now" action, asks the user to confirm, and navigates to checkout on confirmation.

**Architecture:** A single file `ConfirmModal.tsx` exports both `useConfirmModal` (state hook) and `ConfirmModal` (presentational component). Modal CSS is added to the design-system stylesheet. `OfferDetailPage` replaces its `<Link>` wrapper with a button that opens the modal and uses `useNavigate` on confirm.

**Tech Stack:** React 19, react-router-dom v7, TypeScript 6, Vite (no test framework — verification via `tsc -b` + `eslint` + browser).

---

### Task 1: Add modal CSS to the design-system stylesheet

**Files:**
- Modify: `packages/design-system/src/styles.css`

- [ ] **Step 1: Open the stylesheet and append the modal rules at the end**

Add the following block at the very end of `packages/design-system/src/styles.css`:

```css
/* ---------- modal ---------- */
.modal-backdrop {
  position: fixed;
  inset: 0;
  z-index: 100;
  background: rgba(0, 0, 0, 0.55);
  backdrop-filter: blur(4px);
  display: flex;
  align-items: center;
  justify-content: center;
}

.modal {
  z-index: 101;
  background: var(--surface);
  border: 1px solid var(--line);
  border-radius: var(--radius);
  padding: var(--space-5);
  max-width: 420px;
  width: calc(100% - var(--space-5));
  animation: modalIn 140ms ease;
}

.modal__message {
  color: var(--ink);
  font-size: 0.95rem;
  line-height: 1.55;
  margin: 0 0 var(--space-4);
}

.modal__actions {
  display: flex;
  gap: var(--space-2);
  justify-content: flex-end;
}

@keyframes modalIn {
  from { opacity: 0; transform: translateY(8px); }
  to   { opacity: 1; transform: translateY(0); }
}
```

- [ ] **Step 2: Verify TypeScript build still passes (no TS errors from CSS changes)**

Run from `frontend/`:
```bash
cd apps/web && npx tsc -b --noEmit
```
Expected: no errors printed, exit code 0.

- [ ] **Step 3: Commit**

```bash
git add packages/design-system/src/styles.css
git commit -m "style: add modal overlay CSS to design-system"
```

---

### Task 2: Create the `ConfirmModal` component and `useConfirmModal` hook

**Files:**
- Create: `apps/web/src/components/molecules/ConfirmModal.tsx`

- [ ] **Step 1: Create the file with the hook and component**

Create `apps/web/src/components/molecules/ConfirmModal.tsx` with this exact content:

```tsx
import { useState } from 'react'
import { Button } from '@flashmkt/design-system'

export function useConfirmModal() {
  const [isOpen, setIsOpen] = useState(false)
  return {
    isOpen,
    open: () => setIsOpen(true),
    close: () => setIsOpen(false),
  }
}

interface ConfirmModalProps {
  isOpen: boolean
  message: string
  onConfirm: () => void
  onCancel: () => void
}

export function ConfirmModal({ isOpen, message, onConfirm, onCancel }: ConfirmModalProps) {
  if (!isOpen) return null

  return (
    <div className="modal-backdrop" onClick={onCancel}>
      <div className="modal" onClick={(e) => e.stopPropagation()}>
        <p className="modal__message">{message}</p>
        <div className="modal__actions">
          <Button variant="ghost" onClick={onCancel}>Cancel</Button>
          <Button variant="primary" onClick={onConfirm}>Confirm</Button>
        </div>
      </div>
    </div>
  )
}
```

- [ ] **Step 2: Run TypeScript to verify the new file is valid**

```bash
cd apps/web && npx tsc -b --noEmit
```
Expected: no errors, exit code 0.

- [ ] **Step 3: Run lint**

```bash
cd apps/web && npx eslint src/components/molecules/ConfirmModal.tsx
```
Expected: no errors or warnings.

- [ ] **Step 4: Commit**

```bash
git add apps/web/src/components/molecules/ConfirmModal.tsx
git commit -m "feat: add ConfirmModal component and useConfirmModal hook"
```

---

### Task 3: Wire the confirmation modal into `OfferDetailPage`

**Files:**
- Modify: `apps/web/src/pages/OfferDetailPage.tsx`

- [ ] **Step 1: Update the imports at the top of `OfferDetailPage.tsx`**

Replace:
```tsx
import { useEffect, useState } from 'react'
import { Link, useParams } from 'react-router-dom'
```

With:
```tsx
import { useEffect, useState } from 'react'
import { useNavigate, useParams } from 'react-router-dom'
import { ConfirmModal, useConfirmModal } from '../components/molecules/ConfirmModal'
```

Remove the `Link` import — it is no longer needed.

- [ ] **Step 2: Add hook call inside the component, after existing state declarations**

After the line `const [error, setError] = useState<string | null>(null)`, add:
```tsx
const { isOpen, open, close } = useConfirmModal()
const navigate = useNavigate()
```

- [ ] **Step 3: Replace the `<Link>` + `<Button>` with a plain `<Button>` and add `<ConfirmModal>`**

Replace this block in the JSX:
```tsx
<Link to={soldOut ? '#' : `/checkout/${offer.id}`}>
  <Button disabled={soldOut}>{soldOut ? 'Sold out' : 'Buy now'}</Button>
</Link>
```

With:
```tsx
<Button disabled={soldOut} onClick={soldOut ? undefined : open}>
  {soldOut ? 'Sold out' : 'Buy now'}
</Button>
<ConfirmModal
  isOpen={isOpen}
  message="Are you sure you want to buy this item?"
  onConfirm={() => navigate(`/checkout/${offer.id}`)}
  onCancel={close}
/>
```

- [ ] **Step 4: Run TypeScript to verify the updated page is valid**

```bash
cd apps/web && npx tsc -b --noEmit
```
Expected: no errors, exit code 0.

- [ ] **Step 5: Run lint**

```bash
cd apps/web && npx eslint src/pages/OfferDetailPage.tsx
```
Expected: no errors or warnings.

- [ ] **Step 6: Commit**

```bash
git add apps/web/src/pages/OfferDetailPage.tsx
git commit -m "feat: intercept Buy now with confirmation modal on OfferDetailPage"
```

---

### Task 4: Manual browser verification

**Files:** none — verification only.

- [ ] **Step 1: Start the dev server**

```bash
cd apps/web && npm run dev
```
Open the URL printed in the terminal (usually `http://localhost:5173`).

- [ ] **Step 2: Verify the happy path**

1. Navigate to any offer detail page (e.g. click an offer from the catalog).
2. Click "Buy now".
3. Confirm the modal appears with the message "Are you sure you want to buy this item?" and two buttons: "Cancel" (ghost) and "Confirm" (volt/yellow-green).
4. Click "Confirm".
5. Confirm you are redirected to `/checkout/:offerId`.

- [ ] **Step 3: Verify the cancel path**

1. Click "Buy now" again to open the modal.
2. Click "Cancel" — modal should close, you should remain on the offer detail page.
3. Click "Buy now" again — modal should reopen (state resets correctly).

- [ ] **Step 4: Verify backdrop click dismisses the modal**

1. Open the modal.
2. Click outside the modal panel (on the dark backdrop).
3. Confirm the modal closes without navigating.

- [ ] **Step 5: Verify sold-out state is unaffected**

Find or simulate a sold-out offer. The "Sold out" button should remain disabled with no modal trigger.
