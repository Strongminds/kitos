# Runtime validation

Read this when selecting or executing an inspection. Inspect behavior through the boundary that users/clients actually exercise; choose tools from capabilities available in the session. Do not invent browser, database, or MCP tools.

## Select the right inspection

| Feature | Runtime evidence | Supporting automated checks |
|---|---|---|
| MCP server | Initialize/connect with the supported transport, discover tools, execute valid and rejected calls, inspect scoped results and downstream behavior | Transport/serialization/authentication integration tests and domain/query tests |
| REST/backend | Send real requests to the running host with representative identities; inspect statuses, data, and relevant side effects | HTTP integration tests and focused service tests |
| UI | Use an available local browser/computer-use skill to navigate the real flow, including loading/error/empty states | Existing UI/end-to-end suite and relevant backend checks |
| Persistence | Verify actual schema, stored outcomes, constraints, and transactions in a disposable database | Provider-backed integration tests; in-memory substitutes cannot prove provider behavior |
| Background processing | Trigger the supported entry point and observe completion/failed outcomes and idempotency | Integration tests with deterministic/bounded readiness checks |

Use purpose-built APIs/CLIs/test clients for headless behavior. Browser inspection is relevant to a UI or interactive login, not required for every backend. If browser automation is needed, read the available browser/computer-use skill and follow its supported APIs. A screenshot can support UI findings but cannot establish backend authorization or isolation.

## Environment and observations

- Identify the intended local/test host, database, branch/build, and fixture ownership before inspection. Confirm prerequisites and readiness. Reuse only an appropriate instance; do not run stateful integration fixtures against an unknown or production target.
- Start authorized local services using project instructions, keeping background helpers hidden where applicable. Coordinate shared resources through the parent agent. Clean up only fixtures/services this task owns, preserving existing data and developer processes. Repository rules for resets or applying migrations still apply.
- Use synthetic or approved test data. Keep credentials in supported credential handling, outside model-visible tool payloads where feasible; do not put authorization headers, connection strings, personal data, or secrets in screenshots/logs/evidence. Capture sanitized inputs/outputs, test names/commands, environment identity, and relevant timestamps.
- Define expected outcomes before exercising the feature. Compare against known fixture ground truth or another independently justified oracle; do not use the same query implementation to generate its own expected result.
- Include success and meaningful rejected/failed outcomes. For permissioned features, vary user/organization context and test direct boundary calls, not only UI restrictions. For concurrency, control interleaving with fixtures/barriers where possible rather than hoping an arbitrary delay produces a race.
- Establish that the running instance contains the inspected changes. Label stubbed dependencies and development-only authentication substitutes. A development configuration cannot prove production credential or network isolation; require an appropriate boundary/configuration test for that criterion.

## Findings and repair

Classify findings as implementation defects, design/requirement gaps, environment limitations, or deferred enhancements. Preserve the reproduction and expected outcome. Use meaningful automated regressions for repeatable defects; manual inspection complements these tests rather than replacing them. Reinspect the affected behavior after the fix and retain a compact evidence summary in the ledger.

Record relevant design gaps before implementing a changed interpretation. Keep unknown semantics as open questions until resolved; do not convert a failed check into a new 'expected' result without a justified requirement change.
