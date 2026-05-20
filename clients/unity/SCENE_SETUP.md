# Scene Setup & VContainer Integration

This guide explains how to properly set up your Unity scenes to work with the new Dependency Injection (VContainer) and Structured Logging systems.

## 1. The Global Lifetime Scope
The project uses **VContainer** for dependency injection. All core managers are registered in a `GameLifetimeScope`.

### Setup:
1.  Create a new scene called `Initialization` (or use your first scene, e.g., `Registration`).
2.  Create an empty GameObject named **`GameLifetimeScope`**.
3.  Attach the **`GameLifetimeScope`** script to it.
4.  In the Inspector, set the **`Is Root`** checkbox to **True**. This makes these dependencies available across all scenes.
5.  Create empty GameObjects for the following managers and attach their respective scripts:
    *   `DatabaseManager`
    *   `AuthManager`
    *   `SpaceTradersClient`
    *   `APIService`
    *   `UniverseSyncManager`
    *   `GameManager`
6.  (Optional) Nest these manager GameObjects under the `GameLifetimeScope` GameObject for organization.

## 2. Wiring UI Components
VContainer needs to be told which components in your scene require injection.

### Option A: Register in Hierarchy (Recommended for singletons)
For components that exist once in a scene:
1.  In `GameLifetimeScope.cs`, add `builder.RegisterComponentInHierarchy<YourComponent>();`.

### Option B: Auto-Inject GameObjects
If you have many UI components or don't want to register every script:
1.  Select your **`GameLifetimeScope`** GameObject.
2.  Find the **`Auto Inject Game Objects`** list in the Inspector.
3.  Add the GameObjects containing your UI scripts (e.g., `MenuManager`, `SettingsUI`) to this list.

## 3. UI Toolkit Setup
For all UI scenes (`MainMenu`, `Settings`, `Registration`, `Dashboard`):
1.  Ensure there is a GameObject with a **`UIDocument`** component.
2.  Assign the correct `.uxml` file to the `Visual Tree Asset` field.
3.  Attach the corresponding script (e.g., `MenuManager`) to the **same** GameObject as the `UIDocument`.
4.  Ensure the GameObject is included in the `Auto Inject Game Objects` list of the `LifetimeScope` (if not using `RegisterComponentInHierarchy`).

## 4. Troubleshooting

### NullReferenceException in OnEnable
If you see a crash in `OnEnable`, it usually means:
*   The `UIDocument` is missing from the GameObject.
*   The `Visual Tree Asset` (.uxml) doesn't contain the expected element names (e.g., "PlayButton").
*   **VContainer hasn't injected the dependencies yet.** Ensure the GameObject is registered for injection (see Step 2).

### AES Key Size Error
If you see "Specified key is not a valid size", ensure `SecureTokenStorage.cs` uses a 32-byte string for the AES-256 key. (This has been fixed in the latest code updates).
