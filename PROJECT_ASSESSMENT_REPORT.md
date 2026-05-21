# SpaceTraders Unity Project Reassessment Report

Date: 2026-05-20  
Scope: Reassessment of current clients/unity implementation after remediation updates

## Executive Summary

The codebase has improved materially since the prior assessment. Key risk reducers are now in place: encrypted token persistence, response sanitization, centralized unauthorized/rate-limit exception typing, presenter extraction from the dashboard monolith, safer SQLite sync mode, and baseline EditMode tests.

Current status:
- Architecture: Fair to Good
- Maintainability: Fair to Good
- Usability: Good
- Security: Fair
- Reliability/Operations: Fair

## What Improved

- Token persistence now uses encrypted storage via SecureTokenStorage before PlayerPrefs writes.
- API client now sanitizes token-like values in logs and distinguishes 401/429 failure classes.
- Unauthorized handling path exists and is wired to UI redirection flow.
- DashboardController has been decomposed into MapPresenter, FleetPresenter, and ContractPresenter.
- Database durability improved by moving SQLite synchronous mode from OFF to NORMAL.
- EditMode tests were added for AuthManager/SecureTokenStorage and DatabaseManager basics.

## Remaining Findings and Recommendations

### 1) Security

1. Issue: Encryption key derivation is device-ID plus static salt, not OS-backed secret storage.  
Severity: Medium  
Impact: Better than plaintext, but still weaker than platform vault-backed secrets and vulnerable to reverse engineering in compromised clients.  
Recommendation: Move key material to platform secure stores (DPAPI/Credential Locker, Keychain, Keystore) and use envelope encryption.

2. Issue: Error logging still prints full sanitized response bodies in non-debug paths for failures.  
Severity: Medium  
Impact: Non-token sensitive data can still leak to logs in production builds.  
Recommendation: Gate full-body error logs behind debug/development mode; keep production logs to status code and request identifier.

3. Issue: Token unauthorized state is overwritten during cleanup flow.  
Severity: Medium  
Impact: AuthManager.HandleTokenUnauthorized sets Invalid, then ClearTokens resets state to Unknown, reducing diagnostic clarity and state semantics.  
Recommendation: Preserve Invalid state through unauthorized handling (e.g., clear token without resetting state, or add a dedicated clear path).

### 2) Architecture and Maintainability

1. Issue: Presenter split is partial; MapPresenter remains large and multi-responsibility.  
Severity: Medium  
Impact: Better than before, but still high cognitive load and change coupling in map rendering + selection + API action flows.  
Recommendation: Further split MapPresenter into map rendering, selection/state, and waypoint action services.

2. Issue: Singleton-based implicit service creation remains core wiring strategy.  
Severity: Medium  
Impact: Limits testability and makes lifecycle/order-of-initialization behavior harder to reason about.  
Recommendation: Introduce explicit bootstrap/installer with constructor-injected service interfaces.

3. Issue: Some UI event wiring in OnEnable uses lambda subscriptions without corresponding unsubscription.  
Severity: Medium  
Impact: Potential duplicate handlers after disable/enable cycles.  
Recommendation: Register callbacks once in Awake/Start, or explicitly unregister in OnDisable.

### 3) Reliability and Correctness

1. Issue: Retry/backoff is still not implemented in APIService or SpaceTradersClient despite typed exceptions.  
Severity: Medium  
Impact: Transient network or 429 events still bubble up as immediate user-facing failures.  
Recommendation: Add centralized bounded retries with jitter for transient codes and timeout handling.

2. Issue: Rate-limit headers are extracted but not used to adapt request pacing.  
Severity: Low to Medium  
Impact: Leaves throughput and API friendliness on the table under pressure.  
Recommendation: Add adaptive pacing in universe sync and high-volume fetch flows.

3. Issue: One waypoint enrichment condition in map loading is brittle and likely incorrect.  
Severity: Medium  
Impact: Potentially skips detail hydration depending on first waypoint traits, causing inconsistent waypoint data quality.  
Recommendation: Replace first-element traits check with explicit completeness criteria across loaded waypoint payload.

### 4) Testing and Delivery

1. Issue: Automated testing improved, but remains narrow (EditMode only, no key gameplay/UI integration tests).  
Severity: Medium  
Impact: Regressions in presenter/UI interaction and async flow handling can still escape.  
Recommendation: Add PlayMode tests for token expiry redirect, map interaction, fleet actions, and contract flows.

2. Issue: CI quality gates are still not visible in repository workflow artifacts.  
Severity: Medium  
Impact: Build/test enforcement still likely manual.  
Recommendation: Add CI pipeline for build + EditMode + PlayMode (or smoke subset) + lint/static checks.

## Updated Priority Plan

### Immediate (next 1-2 weeks)
- Fix unauthorized state handling so Invalid state is preserved through auth-expiry flow.
- Add production-safe logging policy for failure payloads.
- Add explicit callback unsubscription strategy for UI event handlers.

### Near-term (2-6 weeks)
- Implement retry/backoff and rate-limit-aware pacing.
- Further decompose MapPresenter.
- Add PlayMode coverage for critical user journeys.

### Longer-term (6+ weeks)
- Move secret management to platform vaults.
- Introduce explicit dependency injection/bootstrap architecture.
- Add CI quality gates with required checks.

## Reassessment Notes

- This reassessment reflects current code present in clients/unity at review time.
- Static review only; no runtime penetration testing or performance profiling executed in this pass.
