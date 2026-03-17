# Propentro API — TODO

Future work items tracked centrally. Claude Code reads this via CLAUDE.md reference.

---

## Phase 2 — Post-Launch

Items that become valuable when real users generate traffic.

- [ ] **OpenTelemetry** — Distributed tracing + metrics export (Grafana/Jaeger/Datadog). Answers "which endpoint is slow and why?" with request-level traces through middleware → handler → DB.
- [ ] **Response Compression** — gzip/brotli middleware. `AddResponseCompression()` + `UseResponseCompression()`. Measurable impact only with real traffic.

## Scheduled Jobs

- [ ] **Expired invite cleanup job** — Quartz job that runs periodically, finds invites past expiry date, marks them as expired or deletes. Also clean up any other time-expired records.

## Documentation

- [x] **Endpoint flow documentation** — For each endpoint: validation rules → business logic → events raised → response. Short, scannable format for quick reference. See `docs/endpoints/`.

## Cross-Cutting Concerns Checklist

When implementing any business feature, consider each of these:

- [ ] **Activity logging** — Does this action need an activity record?
- [ ] **Notifications** — Should users be notified?
- [ ] **Tags** — Can this entity be tagged?
