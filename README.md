# StonemachiaModLoader
A lightweight Windows Desktop application designed to simplify mod managment for the game **Stonemachia**.
Stonemachia Mod Loader aims to provide a clean, convenient way to configure your game installation, manage mod related settings, and launch the game with the required components.
> **WARNING:**
> This project is still under development. Features may be incomplete, behavior may change between releases, and some functionality may not work as expected.

## Disclaimer
Stonemachia Mod Loader is an unofficial community project. It is not affiliated with or endorsed by the developers or publishers of Stonemachia, UE4SS, or Nefarius Injector.

Use the application and third-party components at your own risk. Always keep backups of files you modify.

## Non-Invasive Installation
Stonemachia Mod Loader is designed to keep your game installation clean and untouched.
Instead of bundling or permanently integrating third-party components into the game, the application uses separate installations of [UE4SS](https://github.com/UE4SS-RE/RE-UE4SS) and [Nefarius Injector](https://github.com/nefarius/Injector).

This approach offers several advantages:
- **No permanent game modifications** — The loader is designed to avoid modifying or replacing the game's original files.

- **Easy installation** — Configure the paths to your existing game, UE4SS, and injector installations.

- **Easy removal** — Remove the mod loader and its own configuration files without needing to restore the game's original files.

- **Clean game installation** — Since the loader does not permanently integrate its components into the game directory, removing the application leaves the original game files untouched.

[UE4SS](https://github.com/UE4SS-RE/RE-UE4SS) and [Nefarius Injector](https://github.com/nefarius/Injector) remain separate components and must be installed and configured as required. Any files or settings created outside the loader's own directory may need to be removed separately.

## Getting Started
1.  Download or build Stonemachia Mod Loader.
2.  Launch the application.
3.  Click the **Settings** icon on the left and configure the path to the game's executable.
4.  Select the appropriate injector executable and UE4SS DLL.
5.  Run **Installation Check** and review any warnings or errors.
6.  Save your configuration.
7.  Launch the game and check the logs if something goes wrong.
    

> **Note:** Passing the installation checks does not guarantee that UE4SS will initialize successfully. Actual compatibility must be verified at runtime.

## Issues and Feedback

If you encounter a problem, please open a [GitHub issue](https://github.com/DivanoSIMP/StonemachiaModLoader/issues) and include:
-   Your Windows version
-   The game version
-   Your UE4SS version
-   The injector version
-   The relevant application logs
-   The steps required to reproduce the issue

**Do not** include access tokens, personal information, or other sensitive data in logs or issue reports.