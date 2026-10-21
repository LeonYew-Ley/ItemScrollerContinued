# Changelog

## 1.0.0 — 2026-10-21

- Published on [Thunderstore](https://thunderstore.io/c/peak/p/LeonYew/ItemScroller_Continued/) on 2026-10-22 (Asia/Shanghai), using the same ZIP as the GitHub release.
- Continued fork of [MasterAli2/ItemScroller](https://github.com/MasterAli2/ItemScroller), maintained by LeonYew-Ley under GPLv3.
- Fix the null-component-array exception that interrupts switching, dropping and using items in the same update. This addresses a verified failure path behind the symptoms described in [upstream issue #1](https://github.com/MasterAli2/ItemScroller/issues/1).
- Avoid inspecting held items on frames with no wheel input; tolerate partially initialized inventory slots and an absent selection.
- Preserve item scroll delegates and rope spool behavior; use each character's actual equip time for the scrollable-item hold window.
- Respect the game's casting, consciousness, climbing, sticky-item and equipment-cooldown restrictions. Explicit slot keys retain priority over wheel switching.
- Give the fork its own plugin ID and configuration file, and declare incompatibility with the original plugin to prevent duplicate patches.
- Add portable game-reference configuration, source-linked managed regression checks and a reproducible Thunderstore package build.

Validation: managed regression checks and compilation against PEAK 2.6 assemblies. Native Unity/Photon, airport passport interaction and multiplayer testing remain manual checks; this release does not claim all issue #1 symptoms are confirmed fixed in game.
