# 🏔️ Everest

Everest is a mod for [PEAK](https://store.steampowered.com/app/3527290/PEAK/) that creates a shared experience of failure and discovery. When you die, your location is saved to a server. As you play, your world will be populated with the skeletons of other fallen players, marking the dangerous places where they met their end.

![skeleton](https://github.com/user-attachments/assets/f1c7b164-2810-4e42-946a-b7a7e4f9706b)


## ✨ Features

* **Shared Deaths:** Automatically uploads your death location to a central server.
* **Persistent Skeletons:** Downloads the death locations of other players and spawns skeletons in their place, showing you where others have perished on their arduous climb to the top.
* **UI Toasts:** Provides easy confirmation that the mod is working as expected and informs you when it isn't.
* **Configurable:** A detailed configuration file lets you adjust the experience to your liking.

---

## 📋 Requirements

Before installing Everest, please ensure you have the following installed:

* **[BepInEx](https://github.com/BepInEx/BepInEx)**: The modding framework required to load the mod.
* **[UniTask](https://github.com/Cysharp/UniTask)**: A library used to better handle async operations in Unity.
  * Simply drop the UniTask dll files into `BepInEx/core` to install.

---

## 🛠️ Installation

1.  Make sure you have successfully installed both **BepInEx** and **UniTask**.
2.  Download the latest `Everest.dll` from the releases page.
3.  Place the **`Everest.dll`** file into your **`BepInEx/plugins/`** folder.
4.  That's it! The mod will be active the next time you launch the game.

---

## ⚙️ Configuration

The first time you run the game with Everest installed, it will generate a configuration file located at **`BepInEx/config/Everest.cfg`**. You can open this file with any text editor to change the settings.

Here are the available options:

* **`Enabled`**
    * Toggles the entire mod on or off.
    * **Values**: `true` / `false`
    * **Default**: `true`

* **`MaxSkeletons`**
    * Sets the maximum number of skeletons that can be spawned in your world at one time.
    * **Values**: Any whole number (e.g., `10`, `25`, `50`)
    * **Default**: `100`

* **`AllowUpload`**
    * Determines if the mod will upload your own death location to the server. Set to `false` if you only want to see other players' skeletons without contributing your own.
    * **Values**: `true` / `false`
    * **Default**: `true`

* **`ShowToasts`**
    * Enables or disables the small UI popups that notify you of mod activity (e.g., "Your death has been recorded" or "Skeletons have been summoned").
    * **Values**: `true` / `false`
    * **Default**: `true`
