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

## ADR-5 · In-process index update now, outbox later

`IOfferIndexUpdater` is invoked after commit to refresh the search read model. In-process is exactly right for a single node and keeps the demo honest. The moment this becomes multi-instance, the same port is implemented by an outbox table + relay (the event is already modelled — `OfferStockChanged`), which is also the Event Sourcing on-ramp.

## ADR-6 · Resilience policy placement

- **Retry + jitter + circuit breaker + timeout**: payment adapter only (the one true external dependency). Polly v8 `ResiliencePipeline<PaymentResult>` composed once, injected as singleton.
- **Bulkhead**: ASP.NET concurrency limiter on the checkout endpoint group. Two budgets that fail independently: search can melt while checkout keeps billing, and vice versa.
- Failures surface as `Result` errors (`payment_provider_unavailable`), never exceptions across layer boundaries.

## ADR-7 · Functional core, imperative shell

Money math, pricing, commission strategies and state transitions are pure (`OrderPricing.Calculate`, `Offer.DecrementStock`, records + `with`). IO orchestration lives in the handler as a flat `Result` pipeline. This is what keeps cyclomatic complexity ~1 in the money paths and makes the TDD suite fast and deterministic (the entire unit suite runs in ~1s).

## ADR-8 · Test strategy

- **TDD discipline:** every behavior in Domain/Application/Search/Cache/Polly was written test-first (the git history shows the red-green increments).
- **Architecture tests** are guardrails, not documentation — they fail the PR that breaks the dependency rule.
- **Integration tests** run the real composition root against a real PostgreSQL (Testcontainers, singleton container) — including the 20-buyers-5-units race that proves the oversell invariant under true parallelism.
- **Contract-shape tests** mirror `frontend/src/api/types.ts` from the consumer side; payload-breaking changes fail the backend CI first.
