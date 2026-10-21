# Item Scroller Continued

Switch between occupied PEAK inventory slots with the mouse wheel, including the backpack slot. Empty slots are skipped.

An independently maintained, GPLv3 fork of [MasterAli2/ItemScroller](https://github.com/MasterAli2/ItemScroller). Original work by MasterAli2; continuation maintained by LeonYew-Ley.

## Install

**Remove or disable the original ItemScroller first.** The two plugins patch the same game method and must not run together. Continued declares an incompatibility with `MasterAli2.ItemScroller` so BepInEx will not load both.

- Mod manager: install BepInExPack_PEAK and import the release ZIP as a local mod, or install this package from Thunderstore once it is listed.
- Manual: install [BepInExPack_PEAK](https://thunderstore.io/c/peak/p/BepInEx/BepInExPack_PEAK/), then extract the release ZIP's `BepInEx` directory into the PEAK game directory.
- [Download releases](https://github.com/LeonYew-Ley/ItemScrollerContinued/releases).

Only the local player needs the plugin. Native multiplayer behavior still needs in-game verification.

## What changed

The original mod could inspect a held item's uninitialized component array even when the wheel was idle. An exception there also interrupted the game's subsequent dropping and using updates, which explains a concrete failure path matching [upstream issue #1](https://github.com/MasterAli2/ItemScroller/issues/1): wheel, click and passport actions stopped responding.

Continued makes that detection null-safe, skips it on idle frames and tolerates missing inventory slots. Scrollable items retain their own wheel handlers. Normal slot keys and the game's switching restrictions keep priority.

**Validation:** built against PEAK 2.6 and covered by managed regression checks that link the production patch source. Those checks use stand-ins for Unity/game APIs. Airport passport interaction, native Unity/Photon behavior and multiplayer require manual testing; the full issue has not been confirmed resolved in game.

## Configuration

BepInEx creates `BepInEx/config/LeonYew.ItemScrollerContinued.cfg` after the first launch.

| Setting | Default | Behavior |
| --- | --- | --- |
| `Scrolling.ScrollThreshold` | `0.75` | Minimum wheel input to switch slots. |
| `Scrolling.MaxHoldTime` | `0.75` seconds | After equipping a scrollable item, briefly allow scrolling past it. Once this time expires, the wheel controls that item instead. Set to `0` to preserve its wheel controls immediately. |

Continued uses a separate configuration file. Existing `ScrollThreshold` and `MaxHoldTime` values can be copied from the original config if desired.

## Build and checks

Requires a .NET 8 or newer SDK, PowerShell 7 for the packaging script, and your own installed copy of PEAK. No game assemblies are included in this repository or release.

```powershell
dotnet run --project tests/ItemScrollerContinued.Tests.csproj --configuration Release
dotnet build ItemScroller/ItemScrollerContinued.csproj --configuration Release -p:GameManagedPath="<PEAK>/PEAK_Data/Managed"
./tools/Build-Release.ps1 -GameManagedPath "<PEAK>/PEAK_Data/Managed"
```

Alternatively, set `PEAK_GAME_PATH` to the PEAK installation directory for a direct `dotnet build`. Release ZIPs are written to `artifacts/releases` and contain only the plugin DLL, package metadata, documentation, attribution and icon. The managed tests do not require PEAK and run in GitHub Actions.

Manual smoke checks: disable the original plugin; launch into the airport; open, use and drop the passport; scroll in both directions across occupied/empty hotbar slots and the backpack; then check a rope spool before and after `MaxHoldTime`. Repeat as host and client before reporting multiplayer compatibility.

## Source and license

GPLv3. See [LICENSE](LICENSE) and [third-party notices](THIRD_PARTY_NOTICES.md). Changes are recorded in [CHANGELOG.md](CHANGELOG.md); this is not an official release by the original author.

[Corresponding source for version 1.0.0](https://github.com/LeonYew-Ley/ItemScrollerContinued/tree/v1.0.0) is available from the matching release tag and its source archive. The full original Git history is retained. Report continuation bugs in [this fork's issue tracker](https://github.com/LeonYew-Ley/ItemScrollerContinued/issues).
