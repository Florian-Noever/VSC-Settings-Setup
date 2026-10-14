# <img src="./assets/icon.svg" alt="" height="48"> VSC-Settings-Setup

Installer for VS Code and common extensions and settings for AL development (Windows x64 and ARM64).

## What it does

The console tool runs four steps and asks before each one:

1. Installs VS Code (user setup) if it isn't installed yet, as the build for the architecture of Windows (x64 or ARM64).
2. Installs the recommended extensions from [`extensions-install.csv`](VSC-Settings-Setup/Resources/extensions-install.csv).
3. Uninstalls the extensions listed in [`extensions-uninstall.csv`](VSC-Settings-Setup/Resources/extensions-uninstall.csv).
4. Merges the recommended settings from [`settings-install.json`](VSC-Settings-Setup/Resources/settings-install.json) into the VS Code user settings.

Before anything is changed, you can deselect individual extensions or settings.

## Download

Download `VSC-Settings-Setup-win-x64.exe`, or `VSC-Settings-Setup-win-arm64.exe` for Windows on ARM, from the [latest release](https://github.com/Florian-Noever/VSC-Settings-Setup/releases/latest) and run it. It is self-contained, so it needs neither an installation nor .NET.

## Lists

The files in [`VSC-Settings-Setup/Resources`](VSC-Settings-Setup/Resources) are the live data: the tool downloads them from the `main` branch of this repository, so a change is picked up as soon as it is pushed (GitHub caches raw files for a few minutes).

`extensions-install.csv` has the columns `name`, `displayname`, `publisher`, `author`, `description` and `prerelease` (`true` installs the pre-release version). `extensions-uninstall.csv` uses the same columns without `prerelease`. Only `name` and `publisher` are required.

## Configuration

`config.ini` next to the executable is optional, as is every key in it:

| Key | Default | Description |
|-|-|-|
| `AllowManualOverride` | `true` | Use a list file placed next to the executable (e.g. `extensions-install.csv`) instead of downloading it. |
| `ExtToInstallUrl` | `extensions-install.csv` of this repository | Download URL of the extensions to install. |
| `ExtToUninstallUrl` | `extensions-uninstall.csv` of this repository | Download URL of the extensions to uninstall. |
| `SettingsToInstallUrl` | `settings-install.json` of this repository | Download URL of the recommended settings. |

## Build

Requires the .NET 10 SDK.

```
dotnet test VSC-Settings-Setup.slnx
dotnet publish VSC-Settings-Setup/VSC-Settings-Setup.csproj -p:PublishProfile=FolderProfile
```

The publish profile creates a self-contained `VSC-Settings-Setup.exe` (no .NET installation needed on the target machine) and `config.ini` in `VSC-Settings-Setup/bin/Publish`. Add `-r win-arm64` to build it for Windows on ARM.

## CI & Releases

CI runs on every push and pull request through the shared workflows of [Florian-Noever/Florian-Noever](https://github.com/Florian-Noever/Florian-Noever/blob/main/.github/CI.md). It runs the tests and publishes the exe for x64 and ARM64, which each run keeps as an artifact for 14 days.

To release, publish a GitHub release `vx.y.z` from a commit whose CI is green. The publish workflow tests and publishes the tag and attaches `VSC-Settings-Setup-win-x64.exe` and `VSC-Settings-Setup-win-arm64.exe` to the release.

## License

Licensed under the [MIT License](./LICENSE). The icon uses [Material Symbols](https://github.com/google/material-design-icons) by Google, licensed under the [Apache License 2.0](https://www.apache.org/licenses/LICENSE-2.0).
