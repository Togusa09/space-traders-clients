# Library Adoption Plan - SpaceTraders Unity

This document outlines the evaluation and adoption strategy for three key architectural improvements: JSON serialization, Dependency Injection, and Structured Logging.

## 1. JSON Serialization & Model Handling
**Recommendation:** Replace `JsonUtility` with `com.unity.nuget.newtonsoft-json`.

### Evaluation
- **Current State:** Uses `JsonUtility`, which requires `[Serializable]` attributes and public fields. It fails on complex nested collections and doesn't support properties or specialized converters easily.
- **Benefits:**
    - **Robustness:** Newtonsoft handles complex JSON structures (nested arrays, dictionaries) much better.
    - **Flexibility:** Supports `[JsonProperty]` attributes, allowing for better C# naming conventions (PascalCase) while mapping to API snake_case/camelCase.
    - **Performance:** While `JsonUtility` is faster for simple objects, the overhead of Newtonsoft is negligible for API responses compared to network latency.
- **Risk:** Minimal. The change is mostly internal to `SpaceTradersClient` and `APIService`.

### Adoption Steps
1. **Install Package:** Add `"com.unity.nuget.newtonsoft-json": "3.0.2"` (or latest) to `Packages/manifest.json`.
2. **Refactor Client:** Update `SpaceTradersClient.cs` to use `JsonConvert.DeserializeObject<T>` instead of `JsonUtility.FromJson<T>`.
3. **Enhance Models:** (Optional) Transition models in `Models.cs` to use properties and `[JsonProperty]` for better idiomatic C#.
4. **Validation:** Ensure all API responses still parse correctly and the cache (SQLite) remains compatible (Newtonsoft can be used for the DB cache too).

---

## 2. Dependency Injection (DI)
**Recommendation:** Replace Singletons with `VContainer`.

### Evaluation
- **Current State:** Heavily reliant on lazy-loaded `_instance` patterns in `AuthManager`, `DatabaseManager`, `APIService`, etc. This leads to hidden dependencies and makes unit testing difficult.
- **Benefits:**
    - **Decoupling:** Components explicitly declare their dependencies.
    - **Testability:** Easily swap real services for mocks/stubs in tests.
    - **Lifecycle Management:** Centralized control over when services are created and destroyed (e.g., scoping to a specific scene or the whole game).
- **Risk:** Moderate. Requires refactoring the core initialization flow and how UI presenters access services.

### Adoption Steps
1. **Install VContainer:** Add via git URL: `https://github.com/hadashiA/VContainer.git?path=src/VContainer/Assets/VContainer`.
2. **Setup Lifetime Scope:** Create a `GameLifetimeScope` script to register all core services.
3. **Register Services:**
    - `AuthManager`
    - `DatabaseManager`
    - `SpaceTradersClient`
    - `APIService`
    - `UniverseSyncManager`
4. **Refactor Services:** Remove the `Instance` property and singleton logic from the classes. Use `MonoBehaviour` injection or plain C# classes where possible.
5. **Update Consumers:** Refactor UI Controllers and Presenters (e.g., `DashboardController`) to use `[Inject]` or constructor injection.

---

## 3. Structured Logging
**Recommendation:** Replace `Debug.Log` with `com.unity.logging`.

### Evaluation
- **Current State:** Uses `Debug.Log` with manual string interpolation and custom sanitization in `SpaceTradersClient`.
- **Benefits:**
    - **Structured Data:** Logs can include metadata (e.g., response codes, endpoint names) that can be filtered or exported.
    - **Filtering:** Easily silence verbose API logs in production while keeping critical errors.
    - **Performance:** More efficient string handling for complex logs.
- **Risk:** Low. Can be introduced incrementally.

### Adoption Steps
1. **Install Package:** Add `com.unity.logging` via Package Manager.
2. **Configure Logger:** Setup a default log configuration in the entry point of the game.
3. **Replace Calls:** Systematically replace `Debug.Log` with `Log.Info`, `Log.Error`, etc.
4. **Centralize Sanitization:** Implement a custom `LogHandler` or sink that automatically redacts sensitive data (tokens) across the entire application, rather than just in the API client.

---

## 4. Automated API Client Generation
**Recommendation:** Use `OpenAPI Generator` with the `csharp-unity` template.

### Evaluation
- **Current State:** Manually authored `Models.cs`, `APIService.cs`, and `SpaceTradersClient.cs`. This is prone to error and hard to maintain as the API evolves.
- **Benefits:**
    - **Consistency:** Models and endpoints always match the official spec.
    - **Maintainability:** Regenerate in seconds when the API updates.
    - **Efficiency:** Reduces boilerplate code for every new endpoint.
- **Risk:** High. Replaces a large portion of the `API/` folder. Requires careful integration of custom logic (rate limiting, caching).

### Adoption Steps
1. **Acquire Spec:** Download the official SpaceTraders OpenAPI definition.
2. **Setup Generator:** Install OpenAPI Generator CLI.
3. **Generate Client:** Run generation using the `csharp-unity` template, configured for Newtonsoft.Json.
4. **Integration:** Merge custom logic (rate limiting, SQLite caching) into the generated client using partial classes or wrapper services.

## Implementation Priority
1. **Newtonsoft.Json:** Easiest to implement and provides immediate stability for API handling.
2. **VContainer:** High architectural impact; should be done before adding more features to prevent further singleton debt.
3. **Unity Logging:** Can be done in parallel or as a polish step.
4. **OpenAPI Generation:** Long-term maintainability goal; should be done once the core architecture (JSON/DI) is settled.
