# Unity Scene Setup Guide

Follow these steps to set up the scenes you created in the Unity Editor using the provided scripts and UI Toolkit assets.

## 1. Global Setup (Project Settings)
Ensure all scenes are added to your Build Settings so navigation works:
1. Go to **File > Build Settings**.
2. Drag and drop the following scenes into the **Scenes In Build** list:
   - `MainMenu`
   - `Settings`
   - `GameplayPlaceholder`
   - `Registration` (If you intend to use it)

---

## 2. Setup the `MainMenu` Scene

1. **Create the UI Document**:
   - Right-click in the Hierarchy: **UI Toolkit > UI Document**.
   - Select the `UIDocument` GameObject.
   - In the Inspector, assign **Source Asset**: `Assets/UI/Layouts/MainMenu.uxml`.

2. **Add Core Managers**:
   - Create an Empty GameObject named `Core`.
   - Attach the following scripts to it:
     - `AuthManager`
     - `SpaceTradersClient`
   - Create another Empty GameObject named `MenuManager`.
   - Attach the `MenuManager` script to it.
   - **Link the Inspector fields**:
     - Drag the `UIDocument` GameObject into the **UI Document** field.
     - Drag the `Core` GameObject into the **Auth Manager** field.

---

## 3. Setup the `Settings` Scene

1. **Create the UI Document**:
   - Right-click in the Hierarchy: **UI Toolkit > UI Document**.
   - Select the `UIDocument` GameObject.
   - In the Inspector, assign **Source Asset**: `Assets/UI/Layouts/Settings.uxml`.

2. **Add Core Managers**:
   - Create an Empty GameObject named `Core`.
   - Attach the following scripts:
     - `AuthManager`
     - `SpaceTradersClient`
     - `APIService` (Drag `SpaceTradersClient` into its **Client** field).
   - Create another Empty GameObject named `SettingsUI`.
   - Attach the `SettingsUI` script to it.
   - **Link the Inspector fields**:
     - Drag `UIDocument` GameObject into the **UI Document** field.
     - Drag `Core` (with AuthManager) into the **Auth Manager** field.
     - Drag `Core` (with SpaceTradersClient) into the **Api Client** field.
     - Drag `Core` (with APIService) into the **Api Service** field.

---

## 4. Setting up the Popup UI
The Settings page now uses a popup to display test results.
1. Open `Assets/UI/Layouts/Settings.uxml` in the **UI Builder**.
2. From the Library, drag and drop `Assets/UI/Layouts/Popup.uxml` into the **Hierarchy** of the `Settings.uxml` as a child of the root.
3. Save the UXML.

## 4. Setup the `Registration` Scene (Optional)

1. **Create the UI Document**:
   - Right-click in the Hierarchy: **UI Toolkit > UI Document**.
   - assign **Source Asset**: `Assets/UI/Layouts/Registration.uxml`.

2. **Add Core Managers**:
   - Create an Empty GameObject named `Core`.
   - Attach: `AuthManager`, `SpaceTradersClient`, `APIService`, and `GameManager`.
   - **Configure APIService**: Drag `SpaceTradersClient` into its "Client" field.
   - **Configure GameManager**: Link `SpaceTradersClient`, `APIService`, and `AuthManager` from the same object.
   
3. **Add UI Logic**:
   - Create an Empty GameObject named `RegistrationUI`.
   - Attach the `RegistrationUI` script.
   - **Link the Inspector fields**:
     - Drag `UIDocument`, `APIService`, and `GameManager` into their respective slots.

---

## 5. Setup the `GameplayPlaceholder` Scene (Gameplay Dashboard)
The placeholder has been replaced with a functional Gameplay Dashboard.
1. **Create the UI Document**:
   - Right-click in the Hierarchy: **UI Toolkit > UI Document**.
   - assign **Source Asset**: `Assets/UI/Layouts/Dashboard.uxml`.

2. **Add Core Managers**:
   - Create an Empty GameObject named `Core`.
   - Attach: `AuthManager`, `SpaceTradersClient`, and `APIService`.
   - **Configure APIService**: Drag `SpaceTradersClient` into its **Client** field.

3. **Add Dashboard Controller**:
   - Create an Empty GameObject named `DashboardController`.
   - Attach the `DashboardController` script.
   - **Link the Inspector fields**:
     - Drag `UIDocument` GameObject into the **UI Document** field.
     - Drag `Core` (with APIService) into the **Api Service** field.
     - Drag `Core` (with AuthManager) into the **Auth Manager** field.
     - Drag `Core` (with SpaceTradersClient) into the **Api Client** field.

---

## Pro-Tips for UI Toolkit:
- **Visual Preview**: You can double-click any `.uxml` file in the Project window to open the **UI Builder**. This allows you to see the layout and styles (`MainStyle.uss`) in real-time.
- **Troubleshooting**: If buttons don't click, ensure your scene has an **EventSystem** (Right-click Hierarchy > UI > Event System), though UI Toolkit usually handles its own input.
