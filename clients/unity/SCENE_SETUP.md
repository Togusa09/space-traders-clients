# Scene Setup & VContainer Integration

This guide explains how to properly set up your Unity scenes to work with the new Dependency Injection (VContainer) and Structured Logging systems.

## 1. The Project Root Lifetime Scope
The project uses **VContainer** for application-wide dependencies (Managers and Services).

### Setup:
1.  **Create the Prefab:**
    *   Create an empty GameObject in a new scene.
    *   Attach the **`GameLifetimeScope`** script to it.
    *   Create child GameObjects for the following and attach their scripts:
        *   `DatabaseManager`
        *   `AuthManager`
        *   `SpaceTradersClient`
        *   `APIService`
        *   `UniverseSyncManager`
        *   `GameManager`
    *   Drag this GameObject into your project folder to create a **Prefab**.
2.  **Configure VContainer Settings:**
    *   Go to `Assets -> Create -> VContainer -> VContainer Settings`.
    *   In the **`VContainer Settings`** asset, find the **`Root Lifetime Scope`** field.
    *   Assign your **`GameLifetimeScope`** prefab to this field.
    *   *Note:* VContainer will now automatically instantiate this prefab at startup and make it a parent to all other scopes.

## 2. Scene-Specific Lifetime Scopes
Each scene that contains UI or scene-specific logic needs its own **`LifetimeScope`** to handle injection for those objects.

### Setup for MainMenu, Settings, etc.:
1.  In your scene (e.g., `MainMenu`), create an empty GameObject named **`MainMenuLifetimeScope`**.
2.  Attach a new script that inherits from `LifetimeScope` (or use the generic `LifetimeScope` component if only using Auto-Inject).
3.  **To Inject into UI MonoBehaviours:**
    *   **Option A (Recommended):** Add your UI GameObjects (like the one with `MenuManager`) to the **`Auto Inject Game Objects`** list in the `LifetimeScope` Inspector.
    *   **Option B (Code-based):** Create a custom script for the scope and register the component:
        ```csharp
        protected override void Configure(IContainerBuilder builder) {
            builder.RegisterComponentInHierarchy<MenuManager>();
        }
        ```

## 3. UI Toolkit Setup
For all UI scenes (`MainMenu`, `Settings`, `Registration`, `Dashboard`):
1.  Ensure there is a GameObject with a **`UIDocument`** component.
2.  Assign the correct `.uxml` file to the `Visual Tree Asset` field.
3.  Attach the corresponding script (e.g., `MenuManager`) to a GameObject.
4.  **Crucial:** Ensure that GameObject is registered for injection in the scene's `LifetimeScope` (see Step 2).

## 4. Initialization Scene
It is recommended to have a dedicated **Initialization** scene that is loaded first. This scene can be empty, as the **Project Root Lifetime Scope** will be created automatically before any scene loads if configured in `VContainerSettings`.

## 5. Troubleshooting

### NullReferenceException / "AuthManager not injected"
If a dependency is null:
*   Verify the `GameLifetimeScope` prefab is assigned in `VContainerSettings`.
*   Verify the manager (e.g., `AuthManager`) is a child of the `GameLifetimeScope` prefab.
*   Verify the scene has its own `LifetimeScope` and the script (e.g., `MenuManager`) is in the `Auto Inject Game Objects` list.

### AES Key Size Error
Fixed. Ensure `SecureTokenStorage.cs` uses a 32-byte string for the AES-256 key.
