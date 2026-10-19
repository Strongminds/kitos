# Implementation state: KITOS MCP server

Design: [kitos-mcp-server.md](kitos-mcp-server.md)
MVP: ChatGPT web/custom remote connection, existing KITOS identity, catalog discovery and a known system-name query. First exercise broker and worker locally over HTTPS. Broader model querying remains the goal; stdio, public publishing and a custom chat UI are outside this first integration.

## Vertical slice checklist

The developer confirmed local live-token/catalog/staging queries and the actual ChatGPT connection with a Danish name query returning the expected UUID and correctly explained coverage. Live denied-identity permissions remain to be verified.

- [ ] W01 — Walking skeleton: KITOS-authenticated ChatGPT → one named system query → independent boundary → real KITOS GET → UUID/name and coverage limits. Local and ChatGPT expected-query/coverage paths confirmed; awaiting live permission checks; REQ-2.07 plus security requirements.
- [ ] W02 — Extend the working search scenario with bounded complete paging and truthful incomplete outcomes; proposed after W01; REQ-2.06, REQ-4.05.
- [ ] W03 — Add one useful relationship question end to end; scope after W01/W02; REQ-2.02, REQ-2.04.

## Existing supporting work

Preserve completed work; stop treating these technical tasks as the delivery order.

- [x] C01 — Catalog/plan validation; [tests](../../Tests.Unit.Mcp/QueryPlanValidatorTest.cs).
- [x] C02 — JSON vocabulary/indexes/UI terminology; [tests](../../Tests.Unit.Mcp/CatalogVocabularyLoaderTest.cs).
- [x] C03 — Independent credential-holding boundary adopted; [contract](kitos-mcp-server.md#42-read-only-security-boundary).
- [x] C04b.1 — Safe GET request description; [tests](../../Tests.Unit.Mcp.ReadBoundary/ReadRequestPolicyTest.cs); no execution.
- [x] C04a — Local container runtime, bounded ephemeral authentication/state and Secure MCP Tunnel selected; actual ChatGPT connection confirmed.
- [ ] C04b — Secure GET path, assembled within W01.
- [x] C04b.2 — Caller-bound grants/admission; expiry/replacement/replay tests passed.
- [x] C04b.3 — Manual token page, OAuth/PKCE, ephemeral state and pinned workload identity; HTTPS tests passed.
- [ ] C04b.4 — Real GET and security checks for W01.
- [ ] C05 — Query composition/coverage added with later user scenarios.
- [x] C06 — Streamable HTTP discovery/query path verified locally through broker and worker.
- [ ] C07 — Intended-client and security acceptance starts in W01; full-release acceptance remains unfinished.

## Next step and blockers

The ChatGPT plugin is connected through the approved Secure MCP Tunnel, using static public client `kitos-chatgpt-pilot`, empty secret and token authentication `none`. Its exact management callback and tunnel OAuth resource are configured; discovery, authorization and token audience use that one resource identifier. Developer-only JWT entry and a Danish ChatGPT query for `KMD Nexus Video` returned expected UUID `8314c750-8499-472c-bdd7-cb2aa421d8b1`; a tool-only follow-up confirmed raw coverage and correct caller-visible/single-page/best-effort interpretation. Next, exercise a live denied-permission case with an authorized test identity and an existing active local catalog system owned by another organization; the known restricted UUID must be absent while a public positive-control query still succeeds. The [launcher](../../deployment/mcp-local/Start-ChatGptTunnel.ps1) prompts for the tunnel runtime key locally. No credentials belong in chat.

Target: configurable `https://staging.kitos.dk`. Broker: `https://localhost:7443`; inspector: `https://localhost:7445`; worker private to Podman. Approved bounded in-memory state requires reconnect/token entry after restart. The selected ChatGPT Compose profile uses its exact callback and tunnel resource; the original inspector profile is retained separately. Switching profiles restarts the broker and requires fresh authorization. The separate inspector process uses Connect to clear a cached client/results after broker restart or expiry.

Authentication: KITOS remains unchanged (REQ-4.09); paste a login-issued JWT only into the broker page. OpenIddict and the official MCP SDK provide OAuth/transport. No passwords/JWTs are requested in chat.

Readiness: **W01 partially verified—live permission acceptance pending**. Strict Chrome token-entry and synthetic complete broker/worker/inspector journeys passed; the developer confirmed local live-token queries and the actual ChatGPT connection/expected UUID/coverage answer. Live permission-denial remains unverified. Embedded VS Code browser compatibility was not retested. Later query/reporting semantics and scale targets are deferred.

## Requirement acceptance

| Scope | Requirements | Observable acceptance |
|---|---|---|
| MVP interface | REQ-2.01, REQ-2.03, REQ-2.07, REQ-4.01, REQ-4.07 | Authenticated discovery/name query through ChatGPT, including Danish wording; local HTTPS and actual ChatGPT expected UUID answer passed. |
| MVP visibility/security | REQ-2.05, REQ-4.02–REQ-4.06, REQ-4.08–REQ-4.09 | Denied/replayed/invalid calls fail; finite caller-bound GETs and isolated credentials. Local fixtures/isolation passed; live KITOS permissions pending. |
| MVP coverage | REQ-2.06 | Single-page caller-visible, active-system coverage and best-effort interval disclosed; verified locally and in the actual ChatGPT raw result/explanation. |
| Deferred breadth | REQ-2.02, REQ-2.04 | Relationships and broader model scenarios follow W01/W02. |

## Latest validation

- Developer-observed live connection/queries on 2026-10-13: Connected, catalog version 1.0.0 and a successful staging query with 100 distinct UUID/name pairs (`mayHaveMore: true`). A subsequent narrow query returned expected system `KMD Nexus Video`, UUID `8314c750-8499-472c-bdd7-cb2aa421d8b1`, with `mayHaveMore: false`. Both disclose `caller_visible`, `single_page`, `best_effort`, page 0/pageSize 100. Denied-identity acceptance remains pending; no JWT was shared with the agent.
- Browser check passed: [Playwright script](../../deployment/mcp-local/verify-browser.cjs), [sanitized result](../../TestResults/W01Browser/result.log). Actual inspector click reached the empty broker token-entry form and reload succeeded, with certificate validation enabled and no credential entry.
- Callback policy regression passed after fixing the broker's `form-action` to allow the registered callback origin: [browser check](../../deployment/mcp-local/verify-callback-policy.cjs), [result](../../TestResults/W01Browser/csp-after.log). Token POST is intercepted; only a synthetic error redirect reaches the inspector. This proves browser navigation policy, not successful token exchange. OAuth still validates the exact callback URI.
- Independent complete browser fixture passed: [report](../../TestResults/W01CspInspection/browser-result.log), [script](../../TestResults/W01CspInspection/verify-browser.cjs). Actual consent, OAuth callback/token exchange, SDK catalog and known system query through real broker/worker, with fixture KITOS and synthetic credentials. Broker now preserves required `Mcp-Method`/`Mcp-Name` protocol headers; credential and unrelated-header stripping remains covered by [OAuth/proxy regression tests](../../Tests.Unit.Mcp.Broker/OAuthProtocolTest.cs).
- Manual-entry timing regression passed: real Chrome authorization/catalog/query flow after [eight-second](../../TestResults/W01CspInspection/green-delayed8-browser.log) and [25-second](../../TestResults/W01CspInspection/green-delayed25-browser.log) pauses, synthetic credentials only. SDK discovery no longer cancels the interactive flow after five seconds; SDK initialization and the host session retain a five-minute bound. [Tracked regression](../../Tests.Unit.Mcp.LocalClient/LocalClientHttpTest.cs) verifies the delayed callback still reaches token exchange.
- Deployed Linux broker-to-worker seam passed SDK connection and catalog discovery using pinned mTLS and a synthetic grant inside the trusted broker, with no KITOS data reads: [report](../../TestResults/W01DeploymentProbe/result.log). Inspector failure-stage/status diagnostics exclude dependency bodies and exception text; privacy/retry regression is in [local-client tests](../../Tests.Unit.Mcp.LocalClient/LocalClientHttpTest.cs). Updated inspector rendering verified in Chrome with TLS validation enabled.
- Restart/reconnect browser regression passed: [report](../../TestResults/W01CspInspection/green-restart-browser.log). Synthetic login/query, broker restart with the inspector process retained, fresh Connect/empty token field/old-result clearing, fresh login and successful query; Chrome TLS validation stayed enabled. Stopped inspector on port 7445 restored and rendering verified separately.
- Full gate passed on 2026-10-13: all unit suites passed, zero build warnings; [latest report](../../TestResults/W01ChatGPT/resource-quality-gate.log). Includes exact ChatGPT registration/resource binding, the tunnel launcher and [browser-form/connection/privacy/timing/reconnect regression tests](../../Tests.Unit.Mcp.LocalClient/LocalClientHttpTest.cs). Existing gate-definition warning concerns the previously added MCP suites/convention coverage; no gate rules were changed in this diagnostic.
- Secure MCP Tunnel runtime verified healthy/ready on 2026-10-13, following developer-only API-key entry. Broker initialization correctly requires authentication; runtime readiness does not prove ChatGPT OAuth or query acceptance.
- Actual ChatGPT happy path developer-confirmed on 2026-10-13: plugin connected; Danish query returned expected `KMD Nexus Video` UUID and correctly limited plugin data to name/UUID. Tool-only follow-up returned raw coverage and correctly explained caller-visible/single-page/best-effort semantics. [Sanitized acceptance evidence](../../TestResults/W01ChatGPT/chatgpt-acceptance.md). External vendor facts and ChatGPT's confidence percentage are not server evidence; live denial remains pending.
- ChatGPT registration/resource regression: [broker suite](../../TestResults/W01ChatGPT/resource-broker-suite.log) passed, including exact registration, HTTPS PKCE/token/MCP proxy with the tunnel resource, legacy-resource rejection and malformed/widened metadata denial. [Strict Chrome smoke](../../TestResults/W01ChatGPT/resource-browser.log) passed on the deployed final build with ChatGPT's configured callback/resource; only an empty token form was inspected. Real ChatGPT authorization and expected-query answer were subsequently developer-confirmed.
- Independent integrated HTTPS/OAuth/MCP inspection rerun after listener separation: 29 checks passed with fixture KITOS, including private mTLS query execution, public read denial and private OAuth denial; [report](../../TestResults/W01BrowserInspection/result.log). Tracked [broker OAuth/listener tests](../../Tests.Unit.Mcp.Broker/OAuthProtocolTest.cs) and [worker protocol tests](../../Tests.Unit.Mcp.Worker/WorkerProtocolTest.cs).
- Podman broker/worker expose only public broker port 7443; private worker reads use pinned mTLS on port 7446. Real-token entry and expected staging query results through the inspector and ChatGPT were developer-confirmed; no agent credential entry, live staging denial or database acceptance is claimed. Inspector reconnect clears local state; broker access revocation uses `/disconnect`.
