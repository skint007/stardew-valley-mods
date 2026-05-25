# Changelog

All notable changes to **Level Up** are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html):

- **MAJOR** — incompatible changes (e.g. save-data format breaks, config keys removed)
- **MINOR** — new functionality, backward compatible
- **PATCH** — backward-compatible bug fixes

The mod's version lives in [`manifest.json`](manifest.json); bump it there to match
the entry you add below.

## [Unreleased]

## [1.1.0] - 2026-05-25

### Added

- **Multiplayer support** with independent per-player levels. Each farmer earns
  and tracks their own XP, level, and milestone bonuses.
- **Milestone presets** (Balanced, Combat, Survivalist, Explorer, Minimalist,
  Empty), selectable from a GMCM dropdown on the Milestones page. Applying one
  overwrites the 20 slots; every slot stays individually editable afterward.
- **Two new XP sources** (both on by default, toggleable in GMCM):
  - *Skill level-up* — XP when a vanilla skill levels up (150 × levels gained).
  - *Scale with skill XP* — meta XP as a fraction (default 10%) of all vanilla
    skill XP earned, so any productive task feeds player levels.
- **Localization.** All player-facing text is now pulled from `i18n/` instead of
  being hardcoded: the entire GMCM config menu (labels, tooltips, section titles,
  dropdowns), the level-up toast, the milestone buff name, and the XP-bar HUD
  (level label, `+N XP` popup, hover tooltip). Translators can add a language by
  dropping a `<code>.json` next to `default.json`; SMAPI picks it up automatically
  and the GMCM menu reflects in-game language changes without a restart. Preset
  dropdown values keep stable English keys, so existing `config.json` files are
  unaffected.

### Changed

- Per-player progress is now stored on `Farmer.modData` (network-synced,
  per-character) instead of SMAPI's host-only save-data store.
- Stat bonuses are now stripped/reapplied on `DayEnding`/`DayStarted` instead of
  the host-only `Saving`/`Saved`, so farmhand stats stay clean in the save and
  bonuses apply for everyone.
- Removed the `Context.IsMainPlayer` gates so farmhands earn XP too; monster-kill
  XP is now credited only on the killer's machine to prevent double-counting.

## [1.0.0] - 2026-05-15

First complete release.

### Added

- Meta player-level system with a configurable XP curve (Casual / Standard /
  Hardcore / Custom presets) and level cap.
- XP sources: monster kills (XP scaled by max HP, boss multiplier), quest
  completion (story vs. Help Wanted billboard quests), day survived, festival
  attendance, and new-area discovery — each individually toggleable.
- 20 configurable milestone slots; reaching a milestone level grants cumulative
  bonuses: max HP, max energy, attack, defense, crit chance, weapon speed,
  movement speed, magnetic radius, luck, skill-XP gain, and sell price.
- Bonus application: HP/energy via direct stat mutation against cached vanilla
  baselines; combat/utility stats via a single persistent buff. Bonuses are
  stripped before save and reapplied after, so they never bake into the save
  file.
- Framed XP bar HUD matching the vanilla HP/Energy bars, with the level shown
  in the game's native number font and a hover tooltip.
- Cursor tooltips for the vanilla HP/Energy bars (the original numbers were
  hidden behind the new XP bar).
- Floating "+N XP" popup above the bar on every XP gain; rapid gains accumulate.
- Level-up notification (HUD toast + sound, uses the milestone name when one is
  crossed).
- Generic Mod Config Menu integration for all settings (soft dependency).
- Console commands for inspecting/adjusting progress.

[Unreleased]: https://example.com/compare/v1.1.0...HEAD
[1.1.0]: https://example.com/compare/v1.0.0...v1.1.0
[1.0.0]: https://example.com/releases/v1.0.0
