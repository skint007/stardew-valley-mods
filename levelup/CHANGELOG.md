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

### Fixed

- **Progress leaking between saves in the same session.** Loading save A, returning
  to title, then starting / loading save B could carry over save A's level. The
  in-memory `SaveDataManager.Current` kept save A's data until `SaveLoaded` fired
  for save B, and any write path that happened to run in between (the mid-day-save
  flush added in 1.2.1, a pre-load warp with NewArea XP enabled, or an intro skill
  XP grant) flushed that stale data onto save B's farmer modData. Two defenses:
  `Save()` now refuses to write modData while `Context.IsWorldReady` is false, and
  a new `ReturnedToTitle` handler clears the in-memory state when the player exits
  to title.

## [1.3.0] - 2026-05-30

### Added

- **XP bar size option (horizontal layout).** New `XpBarScale` config (range 0.5–1.5,
  default 1.0) scales the horizontal bar's geometry, the "LVL N" label, and a new dark
  inset plate behind the label together, so mobile / small-screen players can shrink
  the bar without losing the layout or readability. The label switched from
  `SpriteText` (a fixed-size bitmap font) to `smallFont` (a TrueType font) so it
  scales smoothly instead of overshooting the bar at small sizes. No effect on the
  vertical layout.
- **Three new gameplay milestone bonuses.** All disabled by default (existing presets
  unchanged); enable per-milestone in GMCM under the new "Gameplay" section.
  - **+Extra crop chance** — chance to roll a bonus copy of the crop you harvest.
    Postfix on `Crop.harvest`; Junimo harvests are skipped. Values above 1.0 grant
    guaranteed extras plus a roll for one more.
  - **+Extra ore chance** — chance to duplicate ore / stone-node drops from mining.
    Pre+postfix on `GameLocation.OnStoneDestroyed`, snapshotting `location.debris` and
    duplicating any items added during the call. Works for every node type (stone,
    copper / iron / gold / iridium, coal, gems, geodes, bones) without a per-id map.
  - **+Machine speed** — scales a machine's processing time down by `1 / (1 + bonus)`
    at placement. Postfix on `Object.PlaceInMachine`. Only affects machines started
    after the milestone is unlocked; already-running machines keep their original
    timer.

## [1.2.1] - 2026-05-27

### Fixed

- **Mid-day saves losing XP/level progress.** The mod only persisted to
  `Farmer.modData` at end-of-day events, so a save written mid-day (mobile
  Stardew, Save Anywhere, etc.) serialized the previous night's snapshot, and on
  reload the level appeared to regress to the wake-up value. `XpTracker.AwardXp`
  now flushes modData on every XP gain so any save path captures current state.

## [1.2.0] - 2026-05-27

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
  Hardcore `(300, 1.10)`. The "Scale with skill XP" rate default is raised 0.1 → 1.0
  (100%): the award is floored, so at 0.1 any task granting under 10 skill XP rounded
  away to nothing, making most small tasks feel like they did nothing.
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

- **Changing curve/level-cap settings now takes effect immediately.** Saving config
  changes rebuilt the level calculator into a new object that the XP tracker and HUD
  never picked up, and the player's level wasn't recomputed, so a changed curve only
  "applied" on the next XP gain — surfacing as a sudden level jump mid-activity. The
  calculator is now reconfigured in place and the level is re-derived on save.
- **Debug logging now appears in the SMAPI console.** It was logged at `Trace`, which
  SMAPI writes only to the log file; it now logs at `Debug` so enabling the option
  actually shows the per-source XP breakdown in the console.
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

[Unreleased]: https://example.com/compare/v1.3.0...HEAD
[1.3.0]: https://example.com/compare/v1.2.1...v1.3.0
[1.2.1]: https://example.com/compare/v1.2.0...v1.2.1
[1.2.0]: https://example.com/compare/v1.1.1...v1.2.0
[1.1.1]: https://example.com/compare/v1.1.0...v1.1.1
[1.1.0]: https://example.com/compare/v1.0.0...v1.1.0
[1.0.0]: https://example.com/releases/v1.0.0
