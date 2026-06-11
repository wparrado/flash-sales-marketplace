# Architecture Decision Notes

Companion to the [README](../README.md) showcase. Each section is a condensed ADR.

## ADR-1 · Hexagonal over layered-by-convention

**Decision:** four assemblies with one-way references (`Api → Infrastructure → Application → Domain`), ports defined in Application, adapters in Infrastructure, enforced by NetArchTest in CI.

**Why:** in a flash-sale system the volatile pieces are exactly the edges — search engine, cache technology, payment provider, persistence. Hexagonal makes each of them a replaceable adapter behind a stable port, and the architecture tests make the rule survive team growth.

## ADR-2 · Overselling prevention: conditional UPDATE, not optimistic retry

**Options considered:**
- (a) Load entity + optimistic concurrency token (`xmin`) + retry loop
- (b) Pessimistic `SELECT … FOR UPDATE`
- (c) **Atomic conditional `UPDATE … WHERE stock >= qty RETURNING stock`** ✅

**Why (c):** flash sales concentrate thousands of writers on ONE hot row. (a) collapses into a retry storm precisely at peak; (b) holds locks across more round-trips than needed. (c) is a single statement: the row lock lives microseconds, the WHERE clause is the invariant, and "0 rows affected" *is* the sold-out signal. The cache check in the validation chain only exists to shed hopeless traffic before it reaches the database.

## ADR-3 · Cache is advisory, database is authoritative

The inventory cache (read-through, 5s TTL, write-through after commit) serves browsing traffic at memory latency. It is deliberately **not** part of the consistency story: no reservations in cache, no distributed locks. Consequences accepted: a buyer can pass the cache check and still receive 409; browsers may see stock numbers up to 5s stale. Scale path: same `IInventoryCache` port backed by Redis for multi-instance deployments.

## ADR-4 · Saga-style compensation instead of distributed transaction

Payment runs *outside* the database transaction (an external call inside a DB transaction is how you turn a provider outage into connection-pool exhaustion). Flow: reserve stock + persist `PendingPayment` order (tx1) → charge → confirm (tx2) or compensate (restore stock, invalidate cache, persist `Failed` order). The failed order is kept for audit, mirroring real marketplaces.

## ADR-5 · Transactional outbox for read-model propagation

Every committed stock change enqueues an explicit `OfferStockChanged` record into `ordering.outbox_messages` **inside the reservation transaction** — the event exists if and only if the business change committed. A polling dispatcher (`OutboxDispatcher`, 300ms) projects pending events onto the inventory cache and search index, then marks them processed.

Why now and not "later": the previous in-process call had a crash window (commit succeeds, process dies, read models stale until TTL) and the event was implicit in a method signature. With the outbox, the events are durable, replayable, versionable records — the exact message contracts a broker relay publishes when the system goes multi-instance. Single-instance assumption is documented in code; `FOR UPDATE SKIP LOCKED` is the known upgrade for competing dispatchers.

## ADR-6 · Resilience policy placement

- **Retry + jitter + circuit breaker + timeout**: payment adapter only (the one true external dependency). Polly v8 `ResiliencePipeline<PaymentResult>` composed once, injected as singleton.
- **Bulkhead**: ASP.NET concurrency limiter on the checkout endpoint group. Two budgets that fail independently: search can melt while checkout keeps billing, and vice versa.
- Failures surface as `Result` errors (`payment_provider_unavailable`), never exceptions across layer boundaries.

## ADR-7 · Functional core, imperative shell

Money math, pricing, commission strategies and state transitions are pure (`OrderPricing.Calculate`, `Offer.DecrementStock`, records + `with`). IO orchestration lives in the handler as a flat `Result` pipeline. This is what keeps cyclomatic complexity ~1 in the money paths and makes the TDD suite fast and deterministic (the entire unit suite runs in ~1s).

## ADR-8 · Test strategy

- **TDD discipline:** every behavior in Domain/Application/Search/Cache/Polly was written test-first (the git history shows the red-green increments).
- **Architecture tests** are guardrails, not documentation — they fail the PR that breaks the dependency rule or entangles the Catalog/Ordering contexts.
- **Integration tests** run the real composition root against a real PostgreSQL (Testcontainers, singleton container) — including the 20-buyers-5-units race that proves the oversell invariant under true parallelism, the idempotent replay, and the outbox projection under eventual consistency.
- **Contract-shape tests** mirror `frontend/packages/app-kernel/src/api/types.ts` from the consumer side; payload-breaking changes fail the backend CI first.

## ADR-9 · Idempotent checkout

Clients may send an `Idempotency-Key` header (the frontend can mint one per purchase intent). The handler returns the recorded outcome for a replayed key without reserving stock or charging again; a filtered unique index on `(buyer_id, idempotency_key)` backstops concurrent duplicates. This protects real users today (double click, network retry) and is the prerequisite for at-least-once message delivery between future services.

## ADR-10 · Extractability seams, paid for up front

Cheap decisions made while everything is still one process, so a future split is additive:

- **SharedKernel project** (`Money`, `Result`, `Error`, `SellerTier`): services reference a small package instead of dragging the whole Domain; enforced dependency-free by an architecture test.
- **Bounded-context isolation**: `Domain.Catalog` ↮ `Domain.Ordering` enforced by NetArchTest in both directions; the shared vocabulary lives in the kernel.
- **`IStockAuthority`** split from `IOfferRepository`: the interface IS the future inventory-service API; extraction = implement it over HTTP/gRPC.
- **Per-context schemas** (`inventory.*`, `ordering.*`), no cross-schema foreign keys: database-per-service becomes a dump-per-schema, not surgery.
- **Frontend workspaces**: `@flashmkt/design-system` (tokens + atoms) and `@flashmkt/app-kernel` (contract types, correlation-id client, auth) are the packages a microfrontend shell shares as federated singletons; route-level lazy chunks are the remote boundaries.
