# Implementation Plan - SpaceTraders Unity Project Remediation

This plan addresses the findings and recommendations detailed in the [PROJECT_ASSESSMENT_REPORT.md](file:///c:/Source/Unity/SpaceTraders-Unity/PROJECT_ASSESSMENT_REPORT.md). It organizes remediation into a phased roadmap, beginning with high-priority security, error handling, and reliability fixes, followed by structural architectural refactoring and automated test coverage.

---

## User Review Required

> [!IMPORTANT]
> **Token Storage Strategy**: We propose using standard AES-256 encryption using a per-device key derived from `SystemInfo.deviceUniqueIdentifier` as a cross-platform solution. This avoids external native DLLs or complicated iOS/macOS Keychain plugins while ensuring that tokens stored in the Windows Registry/PlayerPrefs are unreadable in plaintext. If platform-native vaults (such as Windows Credential Locker or macOS Keychain) are strictly required, please let us know.
>
> **Monolith Refactoring Scope**: Split the monolithic `DashboardController` into clear, manageable Presenters (`MapPresenter`, `FleetPresenter`, `ContractPresenter`) while keeping the `DashboardController` as the composition root. This preserves the existing scene layout while making the codebase highly maintainable.

---

## Open Questions

> [!NOTE]
> 1. Do we have any CI/CD environment currently set up (e.g., GitHub Actions, GitLab CI) where we should introduce quality gates, or is the build/test pipeline purely local for now?
> 2. Are there any particular testing frameworks or mock systems you prefer, or should we use the standard Unity Test Framework (UTF) with NUnit?

---

## Proposed Changes

We have partitioned the remediation into three distinct phases following the assessment report's action plan.

### Phase 1: Security & Initial Reliability (Weeks 0-2)

This phase eliminates immediate security and operational risks, ensuring secure token storage, redacted logging, robust 401/429 handling, and self-validating scenes.

#### [NEW] [SecureTokenStorage.cs](file:///c:/Source/Unity/SpaceTraders-Unity/clients/unity/Assets/Scripts/Core/SecureTokenStorage.cs)
- Introduce a utility helper that handles symmetric encryption (AES-256) of agent/account tokens.
- Generate encryption keys dynamically by hashing a salt combined with the device-specific identifier `SystemInfo.deviceUniqueIdentifier`.
- Provide helper methods `string Encrypt(string plaintext)` and `string Decrypt(string encryptedText)`.

#### [MODIFY] [AuthManager.cs](file:///c:/Source/Unity/SpaceTraders-Unity/clients/unity/Assets/Scripts/Core/AuthManager.cs)
- Integrate `SecureTokenStorage` to encrypt tokens before saving them to `PlayerPrefs` and decrypt them upon loading.
- Introduce a `TokenState` enum (`Unknown`, `Valid`, `Invalid`) to track local token health and validity.
- Expose an event `public static event Action OnTokenUnauthorized;` to notify UI layers when a session expires.

#### [MODIFY] [SpaceTradersClient.cs](file:///c:/Source/Unity/SpaceTraders-Unity/clients/unity/Assets/Scripts/API/SpaceTradersClient.cs)
- **Response Redaction**: Implement `SanitizeResponse(string text)` to redact token payloads and Authorization headers from log dumps, preventing local token leaks.
- **Log Level Gating**: Restrict verbose full-body printouts to Development builds (`Debug.isDebugBuild`) or debug-level logs.
- **Centralized Error Handling**: Intercept failed HTTP calls and:
  - If `responseCode == 401`: Trigger `AuthManager.OnTokenUnauthorized` so that the active session is cleared and the UI redirects the player back to the main menu.
  - If `responseCode == 429` or other transient statuses: Propagate structured exceptions that can be caught by retries.
- **Rate Limit Tracking**: Extract response headers `x-rate-limit-remaining` and `x-rate-limit-reset` to expose active quota information globally.

#### [MODIFY] [DashboardController.cs](file:///c:/Source/Unity/SpaceTraders-Unity/clients/unity/Assets/Scripts/UI/DashboardController.cs)
- **Runtime Validation**: Add safety checks in `Awake()` to verify that all serialized fields (`uiDocument`, `contractTemplate`, `shipTemplate`, etc.) are assigned in the Inspector. Log explicit fail-fast diagnostics if any are missing.
- **Session Expiry Listener**: Subscribe to `AuthManager.OnTokenUnauthorized` to trigger an auth-failure popup and gracefully transition the user to the `MainMenu` scene.

---

### Phase 2: Architecture & Reliability (Weeks 2-6)

This phase addresses the monolithic `DashboardController`, database reliability, and introduces baseline test coverage.

#### [NEW] [MapPresenter.cs](file:///c:/Source/Unity/SpaceTraders-Unity/clients/unity/Assets/Scripts/UI/MapPresenter.cs)
- Extract all galaxy and system mapping logic from `DashboardController`.
- House map state variables (`_mapOffset`, `_mapZoom`, `_mapMode`, `_currentSystem`, `_selectedSymbol`).
- Handle 2D custom vector drawing (`OnGenerateVisualContent`, `DrawGalaxyBulk`, `DrawSystemBulk`, `DrawLines`) and map interaction (drag, zoom, click-to-select).

#### [NEW] [FleetPresenter.cs](file:///c:/Source/Unity/SpaceTraders-Unity/clients/unity/Assets/Scripts/UI/FleetPresenter.cs)
- Extract fleet rendering, ship visual bindings (`BindShip`), cargo capacity rendering, and cooldown/transit timer handling (`_activeTimers` loop).

#### [NEW] [ContractPresenter.cs](file:///c:/Source/Unity/SpaceTraders-Unity/clients/unity/Assets/Scripts/UI/ContractPresenter.cs)
- Extract contract layout generation, binding of contract cards (`BindContract`), and acceptance/delivery UI loops.

#### [MODIFY] [DashboardController.cs](file:///c:/Source/Unity/SpaceTraders-Unity/clients/unity/Assets/Scripts/UI/DashboardController.cs)
- Act as the central composition root.
- Manage tab selection, shared state orchestration, and presenter lifecycles.
- Wire dependencies between presenters explicitly rather than relying on global lookups.

#### [MODIFY] [DatabaseManager.cs](file:///c:/Source/Unity/SpaceTraders-Unity/clients/unity/Assets/Scripts/Core/DatabaseManager.cs)
- Change SQLite synchronous pragma from `OFF` to `NORMAL`. This preserves WAL-mode performance advantages while protecting the persistent database from corruption upon sudden game exit or application crash.

#### [NEW] [Assets/Tests/EditMode/AuthManagerTests.cs](file:///c:/Source/Unity/SpaceTraders-Unity/clients/unity/Assets/Tests/EditMode/AuthManagerTests.cs)
- Write EditMode unit tests using the Unity Test Framework.
- Verify that `SecureTokenStorage` correctly encrypts and decrypts test payloads and handles corrupt/modified stored strings gracefully.

#### [NEW] [Assets/Tests/EditMode/DatabaseManagerTests.cs](file:///c:/Source/Unity/SpaceTraders-Unity/clients/unity/Assets/Tests/EditMode/DatabaseManagerTests.cs)
- Verify cache retention, cache expiration policies, and system indexing performance.

---

### Phase 3: Long-term Operations (Weeks 6+)

#### API Client Code Generation
- Transition from hand-maintained C# classes in `Models.cs` to auto-generated OpenAPI models, ensuring robustness as the SpaceTraders v2 API contract evolves.

#### Dependency Injection / Bootstrapping
- Replace static `.Instance` singletons with an explicit installer/bootstrap sequence (such as VContainer or a lightweight pure-C# Service Locator) to improve testing capabilities, decouple initialization, and simplify unit-testing.

---

## Verification Plan

### Automated Verification
1. **Unity Test Runner**: Run newly created EditMode tests to validate encryption, decryption, caching, and database queries.
2. **Mock Request Integration**: Validate low-level exception catching, retry triggers, and response sanitization within a test sandbox.

### Manual Verification
1. **Security Audit**: Save a token, open the Windows Registry (`Computer\HKEY_CURRENT_USER\Software\Unity\Unity-Technologies\UnityEditor` or the specific application subkey), and confirm the `SpaceTraders_AgentToken` value is strongly encrypted and unreadable in plaintext.
2. **Log Redaction Test**: Trigger a deliberate API failure (e.g. invalid query) and inspect the Unity console to verify that the printed log output has redacted all Bearer tokens and sensitive payloads.
3. **Session Expiry Test**: Manually invoke a 401 response from the server (e.g., using a corrupted token) and confirm that the client pops up a friendly error message and returns to the Main Menu.
4. **Scene Fail-Fast**: Temporarily clear one of the inspector templates in `DashboardController` and verify that the startup routine outputs a clear diagnostic log pointing to the missing reference.
