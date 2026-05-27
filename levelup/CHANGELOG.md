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

### Added

- **Update notifications.** Added the Nexus update key (`Nexus:46651`) to the
  manifest so SMAPI tells players when a newer version is available. Takes effect
  for anyone running this version or later.
- **Idle fade for the vertical XP bar.** When the vertical bar layout is in use, it
  now fades to a faint ghost after a few seconds without XP gains (like the vanilla
  HP/Energy bars) and brightens back on the next gain or when you hover it. Toggle
  with the new "Fade vertical bar when idle" option (on by default); the horizontal
  bar is unaffected.

### Changed

- **Retuned XP pacing so leveling isn't front-loaded then a wall (first pass).**
  Leveling used to rocket early (a single boss kill could hand out ~4,000 XP) and
  then stall hard after ~level 50, making level 100 feel unreachable. The curve
  presets now use a higher base and a much gentler growth, which spreads
  progression evenly across 1–100 (Standard's total drops from an effectively
  unreachable ~681M to ~6.4M), and monster XP is toned down (per-HP 1 → 0.25, boss
  multiplier 5 → 2). Presets: Casual `(200, 1.06)`, Standard `(250, 1.08)`,
  Hardcore `(300, 1.10)`.
  - **Existing saves:** player level is derived from lifetime XP, so it is
    recalculated under the new curve on load. Most characters will jump *up* a few
    levels (the old curve was far harsher); milestone bonuses simply reapply. The
    monster-XP change only affects configs created after updating, since existing
    `config.json` values are preserved.
- **Tightened GMCM slider ranges so values are easier to set precisely.** Several
  options had huge maximums (Base XP up to 10,000; +Max HP up to 9,999), which made
  the drag sliders so coarse you couldn't land on a precise value. Lowered them to
  sensible ceilings (Base XP 1,000; growth rate 1.50; per-source XP 2,000; and more
  moderate milestone caps such as +Max HP 500, +Attack/+Defense 50, +Luck 20) — the
  milestone bonuses are cumulative across slots, so the old maximums were excessive
  anyway. A pre-existing config value above the new ceiling is clamped to it.

### Fixed

- **XP curve overflow that threw players to absurd levels.** A steep growth rate
  or a high level cap could make a per-level XP cost exceed what the threshold
  table can store, wrapping it negative and corrupting the level lookup (reports
  of jumping to level 314 / 800, often the instant a new save earned any XP). The
  curve now saturates safely instead of overflowing, so the table stays valid and
  those extreme upper levels simply become unreachable. Default settings are
  unaffected.

## [1.1.1] - 2026-05-25

Maintenance release: brings the 1.1 feature set to Nexus Mods (1.1.0 was published
on GitHub only). No gameplay changes since 1.1.0.

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
- **Choice of XP bar layout.** The XP bar is now a horizontal "LVL &lt;n&gt;" plate
  plus progress bar centered above the toolbar; a new **Use vertical XP bar** toggle
  brings back the original vertical bar beside the HP/Energy bars.
- **Passive HP/energy regen** as a milestone bonus, ticking while time passes (paused
  in menus, events, and sleep).
- **Configurable hotkey** to open the mod's settings menu directly.

### Changed

- Per-player progress is now stored on `Farmer.modData` (network-synced,
  per-character) instead of SMAPI's host-only save-data store.
- Stat bonuses are now stripped/reapplied on `DayEnding`/`DayStarted` instead of
  the host-only `Saving`/`Saved`, so farmhand stats stay clean in the save and
  bonuses apply for everyone.
- Removed the `Context.IsMainPlayer` gates so farmhands earn XP too; monster-kill
  XP is now credited only on the killer's machine to prevent double-counting.
- Redesigned the XP bar into the horizontal above-toolbar layout by default (use the
  new toggle to keep the original vertical bar). The cursor tooltips that replaced the
  vanilla HP/Energy hover numbers were removed, since the bar no longer overlaps them.

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

[Unreleased]: https://example.com/compare/v1.1.1...HEAD
[1.1.1]: https://example.com/compare/v1.1.0...v1.1.1
[1.1.0]: https://example.com/compare/v1.0.0...v1.1.0
[1.0.0]: https://example.com/releases/v1.0.0
