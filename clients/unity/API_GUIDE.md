# API Client Implementation Details

The SpaceTraders API client is implemented using `UnityWebRequest` and `JsonUtility`.

## Components

### 1. `SpaceTradersClient.cs`
- Handles the low-level HTTP requests (GET, POST).
- Manages the Bearer Token authentication header.
- Provides generic methods for JSON deserialization.

### 2. `Models.cs`
- Contains `[Serializable]` C# classes that map to the API's JSON responses.
- Includes models for `Agent`, `Faction`, `RegistrationData`, and responses.

### 3. `APIService.cs`
- Provides high-level methods for specific API endpoints (e.g., `Register`, `GetMyAgent`).
- Uses `SpaceTradersClient` to perform the actual requests.

### 4. `AuthManager.cs`
- Manages the storage and retrieval of the API token using `PlayerPrefs`.
- Provides an easy way to check if a token exists and to clear it.

### 5. `GameManager.cs`
- Orchestrates the initialization and flow of the game.
- Links the API service and authentication.

### 6. `RegistrationUI.cs`
- A sample UI script to handle user registration.

## How to Use

1. **Setup Scene**:
   - Create a GameObject named `SpaceTradersClient` and attach the `SpaceTradersClient` script.
   - Create a GameObject named `APIService` and attach the `APIService` script. Link the `SpaceTradersClient`.
   - Create a GameObject named `AuthManager` and attach the `AuthManager` script.
   - Create a GameObject named `GameManager` and attach the `GameManager` script. Link the other components.
2. **Registration**:
   - Create a simple UI with two `InputField`s (Symbol, Faction) and a `Button`.
   - Attach `RegistrationUI` to a UI GameObject and link the inputs and button.
3. **Extend**:
   - Add new models to `Models.cs` for ships, systems, etc.
   - Add new methods to `APIService.cs` to call the corresponding endpoints.

## UI & Navigation (UI Toolkit)

The project uses Unity's **UI Toolkit** for its interface.

### General Setup for all UI Scenes:
1.  Create a GameObject named `UIDocument`.
2.  Add a `UIDocument` component to it.
3.  Assign the corresponding `.uxml` file from `Assets/UI/Layouts/`.
4.  Attach the appropriate UI script (e.g., `MenuManager`) to a GameObject and link the `UIDocument` component in the Inspector.

### 1. Main Menu (`MainMenu` scene)
- **Layout**: `Assets/UI/Layouts/MainMenu.uxml`
- **Script**: `MenuManager.cs`
- **Logic**: The `PLAY` button is automatically disabled if the `Account Token` is missing.

### 2. Settings (`Settings` scene)
- **Layout**: `Assets/UI/Layouts/Settings.uxml`
- **Script**: `SettingsUI.cs`
- **Logic**: Allows manual entry and saving of both Account and Agent tokens.

### 3. Registration (Optional Integration)
- **Layout**: `Assets/UI/Layouts/Registration.uxml`
- **Script**: `RegistrationUI.cs`
- **Logic**: Facilitates new agent registration via the API.

### 4. Gameplay Placeholder (`GameplayPlaceholder` scene)
- A simple scene serving as the destination for the `PLAY` button.

## Styling
- All UI elements are styled using `Assets/UI/Styles/MainStyle.uss`. You can modify this file to change the look and feel of the entire application.

## Token Persistence
- Tokens are saved locally using `PlayerPrefs`.
- `Account Token`: Required to enable the `Play` button.
- `Agent Token`: Used for most API calls.
