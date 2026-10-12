# Implementation state: KITOS MCP server

Design: [kitos-mcp-server.md](kitos-mcp-server.md)
Authorized scope: implementation requested; developer selected catalog and query validation as the first cohesive slice. This slice is a foundation, not a complete remote MCP server or a narrowing of the full release requirements.
Environment: local .NET 10 SDK available; runtime inspection for this slice uses the compiled JSON validation entry point, with no KITOS host, credentials, or database required.

## Chunk checklist

Checked entries are verified. Future chunks are provisional until dependencies and acceptance scope are resolved; they do not reduce the full design requirements.

- [x] C01 — Domain catalog and fail-closed query validation; REQ-2.02, REQ-2.04, REQ-4.01, REQ-4.05; verified; [evidence](#validation).
- [x] C02 — Versioned JSON vocabulary, shared indexes, project naming, and UI terminology; REQ-2.03, REQ-2.04, REQ-4.05; verified; depends on C01; [evidence](#validation).
- [ ] C03 — Resolve read-only credential isolation and caller-delegation contract; REQ-2.05, REQ-4.02 through REQ-4.04; pending decision; prerequisite for downstream data access.
- [ ] C04 — One allowlisted GET path through the independent read boundary with caller-specific credentials; REQ-2.05, REQ-4.02 through REQ-4.04; blocked by C03 and authorization/bootstrap choices; verify denied writes and credential replay as well as permitted reads.
- [ ] C05 — Bounded REST execution for an agreed question family, including pagination, joins, coverage, and consistency reporting; REQ-2.02, REQ-2.04, REQ-2.06, REQ-4.05, REQ-4.06; blocked by C04 and first-release coverage decisions; split further before implementation if needed.
- [ ] C06 — Authenticated remote MCP tools and Streamable HTTP; REQ-2.01, REQ-4.01, REQ-4.04, REQ-4.07; pending hosting/SDK/authorization decisions and executable tool acceptance; scope into small chunks before starting.
- [ ] C07 — Integrated release acceptance with representative questions and security/permission/concurrency checks; all design requirement IDs; blocked by preceding release capabilities and agreed first-release coverage.

Local/stdio support and exact backend snapshots remain outside the agreed implementation scope. Full-release model coverage and question families remain unresolved.

## Proposed next step

- Recommend C03: settle the security contract before adding downstream data calls.
- Proposed option: ordinary KITOS JWTs remain exclusively in an independent read boundary; MCP receives caller-bound, read-only delegation. Compare this with KITOS-enforced read-only credentials before recording the developer's decision.
- Outcome: a reviewable design contract specifying trust boundaries, caller binding, credential ownership/replay protection, allowlisted routes/methods, and testable security acceptance. This is a decision/design chunk; it does not yet implement or deploy the boundary.
- Verification: trace permitted and denied requests, including direct replay of credentials visible to MCP, against REQ-2.05 and REQ-4.02 through REQ-4.04; identify remaining bootstrap/hosting decisions before C04.
- Handoff: awaiting the developer's reply to proceed with C03. No subsequent application implementation has started.

## MVP acceptance

Current-slice MVP acceptance is listed below. Full-feature requirements retain their original scope and are not silently deferred or marked complete.

| Requirement IDs | Current-slice observable acceptance | Status | Evidence or remaining work |
|---|---|---|---|
| REQ-2.04, REQ-4.01 | Versioned bilingual catalog uses verified KITOS concepts/fields/relationships; discovery identifies supported validation capabilities | verified for slice | Six initial model areas; full-release domain coverage remains unresolved |
| REQ-2.02, REQ-4.05 | Supported typed and serialized plans produce a validated plan with explicit traversal semantics | verified for slice | Automated tests and compiled inspection; no joins/data retrieval implemented |
| REQ-4.05 | Unknown entities, fields, relationships, operators, malformed input, and excessive plans are rejected before any I/O | verified for slice | Strict serializer/parser and semantic validator; core has no data-access dependency |
| REQ-4.05 | Validated output cannot be mutated to introduce unvalidated operations | verified for slice | Immutable collections and cloned JSON values; lifetime tests and independent inspection |
| Current-slice quality | Focused automated tests, independent compiled runtime inspection, final required gate pass | verified | 129 focused tests, 110 runtime checks; final gate passed with all 4,046 unit tests |

## Current slice

- Outcome: catalog discovery and fail-closed plan validation.
- Dependencies: established KITOS terms/API shapes; no authentication-provider choice is needed for this pure core.
- Owned components: standalone `Mcp.Core`, `Tests.Unit.Mcp`, solution/gate integration, design and this ledger.
- Delegated implementation/tests: `catalog_validation`; parent integrates project/gate and documentation. An independent agent will inspect the integrated slice.
- Implementation, vocabulary/naming follow-up, independent inspection, and final repository-gate verification completed. Stop at the selected slice; later work requires selecting the next scope and resolving its relevant decisions.

## Findings and decisions

| Finding | Type | Affected IDs | Decision or next action | Status |
|---|---|---|---|---|
| Full design spans multiple capabilities and unresolved security/coverage choices | design gap | REQ-2.01 through REQ-2.06; REQ-4.01 through REQ-4.07 | Developer chose catalog/query validation first | current slice agreed |
| Ordinary KITOS JWTs carry user permissions without a read-only scope; HMAC secret can mint tokens | existing security constraint | REQ-4.02, REQ-4.03, REQ-4.04 | Choose independent credential-holding boundary or KITOS-enforced read-only credentials before implementing downstream access | open for later slice |
| Remote authorization provider/caller bootstrap and first-release question families remain unspecified | design gap | REQ-2.01, REQ-2.03, REQ-2.04, REQ-4.04, REQ-4.07 | Resolve before affected remote/data-execution slices | open |
| Discovery initially omitted operator/value/prefix/limit semantics | design gap | REQ-4.05 | Added discoverable immutable policy and authoritative per-field operators; clarified design contract | resolved |
| Typed validation threw for disposed or undefined JSON values | defect | REQ-4.05 | Regression failures reproduced; clone valid values and return structured rejection for invalid lifetimes/undefined values | resolved |
| Existing public-website endpoint tests failed with communication errors in restricted execution | environment | Repository quality gate | All nine endpoint-class tests passed outside the sandbox; reran the complete gate there without skipping tests | resolved |
| Vocabulary was inline C# and reconstructed for each catalog instance; project name repeated the solution name | developer feedback | REQ-2.04, REQ-4.01, REQ-4.05 | Developer selected a versioned JSON resource; renamed projects to `Mcp.Core`/`Tests.Unit.Mcp`, loaded once, indexed immutable metadata, validated packaged vocabulary | resolved |
| Danish labels should use UI concepts; systems and system usages are distinct | developer feedback | REQ-2.03, REQ-2.04 | Verified sibling frontend labels; discovery uses `IT-snitflader`, `IT-Systemer i anvendelse`, and `Databehandling`. Design records UI vocabulary as primary and explains the shared-catalog/local-usage distinction | resolved |

## Validation

| Check/scenario | Command or inspected interaction | Actual result | Evidence |
|---|---|---|---|
| SDK prerequisite | `dotnet --version` | 10.0.401 | Local tool output |
| Scope/code review | Design, current token issuer and JWT authentication configuration reviewed | No existing MCP implementation found; security constraints confirmed | Referenced design/code |
| Focused automated acceptance | `dotnet test Tests.Unit.Mcp/Tests.Unit.Mcp.csproj --no-restore -p:KitosQualityGate=true` | 129 passed, zero warnings; formatting passed for both new projects | Subagent test execution; tests include real JSON parsing/catalog/validator composition |
| TDD regressions | Typed input with a disposed JSON document; subsequent undefined JSON value | Meaningful failures reproduced before repair, then passed | Regression tests retained in `QueryPlanValidatorTest` |
| Vocabulary follow-up TDD | Shared metadata identity and oversized metadata identifier | Separate allocations failed before extraction; oversized identifier failed before path-limit repair; both now pass | Regression tests and strict resource-loader tests retained |
| Independent compiled runtime | `dotnet run --project TestResults/McpInspection/McpInspection.csproj -c Release --source C:/Users/MortenHoffmann/.nuget/packages -p:NuGetAudit=false` | 110 checks passed against actual core; no mocked executor | Ignored inspection harness; independent subagent output |
| Restore environment | Existing local NuGet package cache used after ordinary source SSL failures | Restore succeeded without new dependencies; network vulnerability audit disabled only for these offline process invocations | No package/analyzer settings changed in project files |
| Final repository gate | `pwsh ./scripts/quality-check.ps1` outside restricted execution, process-local Git safe directory and local NuGet restore settings | Passed: formatting, zero-warning builds of both solutions, conventions, all 4,046 unit tests (2,197 core; 1,691 presentation; 129 MCP; 29 PubSub) | `TestResults/mcp-ui-vocabulary-quality-gate.log` and TRX reports |
| Independent integration review | Solution, project dependencies, gate and documentation diff reviewed | No blocking defects; existing checks retained, new suite and logic path added | Definition-change warning remains visible for normal review; no analyzer relaxations |

## Completion

- Current-slice MVP and authorized vocabulary/naming follow-up verified: yes; stop implementation here.
- Final automated/unit/quality checks: passed. Serialized plan parsing/catalog/validation composition is covered by automated tests and compiled inspection; remote/API/database integration belongs to later slices.
- Repository review note: the gate definition-change warning remains; its only changes expand MCP test/logic coverage, independently reviewed without weakening checks.
- Runtime inspection: passed 110 compiled checks; no HTTP/MCP server or database execution was exercised.
- Later slices: independent secure read boundary/caller bootstrap; data execution with permission/coverage/consistency handling; authenticated remote MCP tools/transport; integrated release acceptance.
- Full remote MCP MVP: not complete. Mandatory security requirements remain unchanged.
