<p align="center">
  <img src="assets/nexus-cover.png" alt="Level Up" width="800">
</p>

# Level Up

A [Stardew Valley](https://www.stardewvalley.net/) [SMAPI](https://smapi.io/) mod
that adds a meta **player-level** system on top of the vanilla skills.

- Earn XP from in-world actions: monster kills, days survived, quests, and
  optionally festivals and discovering new areas.
- Hit **milestone levels** to unlock permanent stat bonuses.
- Everything — XP rates, the level curve, and 20 milestone slots — is configurable
  in-game via [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098).

> **Status:** fully functional, single-player and multiplayer.
> See [CHANGELOG.md](CHANGELOG.md) for version history.

## Features

- **Player levels** with a configurable XP curve and level cap.
- **Eight XP sources**, each individually toggleable with its own rate:
  monster kills (scaled by max HP, with a boss multiplier), days survived,
  story quests, "Help Wanted" billboard quests, festival attendance,
  new-area discovery, vanilla skill level-ups, and a share of all vanilla
  skill XP (so any productive task — farming, fishing, mining, foraging,
  combat — feeds it).
- **20 milestone slots.** Each enabled milestone you reach grants cumulative
  bonuses: max HP, max energy, HP/energy regen, attack, defense, crit chance,
  weapon speed, movement speed, magnetic radius, luck, skill-XP gain, and sell
  price.
- **Passive HP/energy regen** as a milestone bonus, ticking while time passes
  (paused in menus, events, and sleep).
- **Horizontal XP bar HUD** centered above the toolbar: a `LVL <n>` plate plus a
  progress bar, with a floating `+N XP` popup on every gain and a hover tooltip.
- **Configurable hotkey** to open the mod's settings menu directly (unset by
  default).
- **Level-up notification:** an on-screen toast and sound, using the milestone
  name when you cross one.
- **Save-safe:** stat bonuses never get baked into your save file (they're
  stripped before saving and reapplied after), so disabling the mod cleanly
  reverts your character.
- **Console commands** for inspecting and adjusting progress.
- **Soft GMCM dependency:** works without it, you just lose the in-game config UI.
- **No Content Patcher / asset edits** — pure C# logic.

## Requirements

- Stardew Valley 1.6+
- [SMAPI](https://smapi.io/) 4.0.0 or later
- [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098)
  *(optional, recommended — for the in-game settings UI)*

## Installation

1. Install [SMAPI](https://smapi.io/).
2. Download the latest release and unzip it into your `Stardew Valley/Mods`
   folder (you should end up with `Mods/LevelUp/manifest.json`).
3. Run the game through SMAPI. The console will print `Level Up loaded.`

## Configuration

All settings are editable in-game via GMCM (Settings → Mod Options → Level Up),
or by hand in `Mods/LevelUp/config.json` after the first run.

### Default milestone progression

The first 9 of the 20 slots are pre-populated (all editable):

| Level | Name        | Bonuses |
| ----- | ----------- | ------- |
| 5     | Initiate    | +10 max HP, +10 max energy |
| 10    | Apprentice  | +1 attack, +1 defense, +5% XP gain |
| 15    | Journeyman  | +10 max HP, +10 max energy, +1 luck |
| 20    | Adept       | +5% crit chance, +5% weapon speed |
| 25    | Veteran     | +2 attack, +2 defense, +64 magnetic radius |
| 35    | Expert      | +20 max HP, +20 max energy, +5% sell price, +5 HP/min & +10 energy/min regen |
| 50    | Master      | +1 movement speed, +10% XP gain |
| 75    | Champion    | +3 attack, +3 defense, +5% crit chance, +10 HP/min & +20 energy/min regen |
| 100   | Legend      | +50 max HP, +50 max energy, +10% sell price, +1 luck, +25 HP/min & +40 energy/min regen |

Regen bonuses stack across every unlocked milestone; with the defaults a
level-100 character refills roughly a full HP/energy bar in about five minutes
of active play.

### Milestone presets

The Milestones page in GMCM has an **Apply preset** dropdown for different play
styles. Pick one and hit Save to overwrite all 20 slots; every slot stays
editable afterward and the selector reverts to *(keep current)*.

| Preset | Theme |
| ------ | ----- |
| Balanced    | The default progression above — a bit of everything |
| Combat      | Attack / defense / crit / weapon speed, light HP |
| Survivalist | Big max HP/energy + defense + a little luck |
| Explorer    | Movement, magnetic radius, luck, XP gain, sell price |
| Minimalist  | Small bonuses, fewer slots — levels matter but don't trivialize |
| Empty       | All slots disabled — a clean slate to build your own |

### XP curve

`xpToNextLevel(N) = floor(base × growth^(N-1))`. The **Standard** preset is
`base = 100, growth = 1.15`:

| Level → next | XP to next | Cumulative |
| ------------ | ---------- | ---------- |
| 1 → 2        | 100        | 100        |
| 5 → 6        | 175        | ~675       |
| 10 → 11      | 351        | ~1,933     |
| 25 → 26      | 2,851      | ~25,000    |
| 50 → 51      | 96,890     | ~750,000   |
| 100 → 101    | 1.17M      | ~10M       |

Other presets: **Casual** (75, 1.12 — faster), **Hardcore** (150, 1.20 — much
slower), **Custom** (your own base + growth).

### XP sources (default rates)

| Source            | Default XP                  | Default |
| ----------------- | --------------------------- | ------- |
| Monster kill      | 1 × max HP (× 5 for bosses) | on      |
| Day survived      | 50                          | on      |
| Story quest       | 100                         | on      |
| Help Wanted quest | 25                          | on      |
| Festival          | 75                          | off     |
| New area          | 25                          | off     |
| Skill level-up    | 150 per level               | on      |
| Skill XP (tasks)  | 10% of vanilla skill XP     | on      |

## Building from source

Requires the .NET 6 SDK. The project uses
[`Pathoschild.Stardew.ModBuildConfig`](https://github.com/Pathoschild/SMAPI/blob/develop/docs/technical/mod-package.md),
which resolves the game references and copies the built mod into your
`Stardew Valley/Mods/LevelUp/` folder.

```bash
dotnet build -c Debug
```

The build auto-detects a standard Steam/GOG install. If your game is somewhere
else, **don't** edit `LevelUp.csproj` (keeps machine paths out of version
control) — instead create a git-ignored `LevelUp.csproj.user` next to it:

```xml
<Project>
  <PropertyGroup>
    <GamePath>/path/to/Stardew Valley</GamePath>
  </PropertyGroup>
</Project>
```

A `Debug` build deploys `LevelUp.dll`, `manifest.json`, and `i18n/` to
`<GamePath>/Mods/LevelUp/` and also produces a release zip in `bin/Debug/`.

## How it works

Most XP awarding goes through SMAPI events; a few sources require Harmony to hook
the right game method:

| Concern            | Hook |
| ------------------ | ---- |
| Monster kill XP    | Harmony postfix on `GameLocation.onMonsterKilled` (the single choke point for *all* player kills — many monsters override `Monster.takeDamage` without calling base, so patching that misses them) |
| Quest XP           | Harmony prefix + postfix on `Quest.questComplete` (awards once, only on the not-completed → completed transition) |
| New-area / festival | SMAPI `Player.Warped` |
| Day survived       | SMAPI `DayEnding` (skips on pass-out if configured) |
| Skill level-up XP  | SMAPI `Player.LevelChanged` |
| Skill-XP "tasks" + XP-gain multiplier | Harmony prefix on `Farmer.gainExperience` — inflates vanilla skill XP by the milestone multiplier and awards meta XP as a fraction of the *original* skill XP |
| Sell-price bonus   | Harmony postfix on `Object.sellToStorePrice` |
| HP/Energy tooltips | Harmony prefix on `Game1.drawWithBorder` to suppress the vanilla hover numbers (which would render behind the XP bar), replaced with cursor tooltips |

Stat bonuses are applied two ways: max HP / max energy by direct field mutation
against cached vanilla baselines, and combat/utility stats via a single
persistent `Buff`. Both are stripped on `DayEnding` and reapplied on `DayStarted`
so the inflated values never enter the save file — `DayEnding` fires for the host
*and* every farmhand (the host-only `Saving`/`Saved` events would miss farmhands).

### Multiplayer

Levels are **per-player**: each farmer earns and tracks their own XP, level, and
milestone bonuses independently. Progress is stored on each character's
`Farmer.modData` (network-synced and persisted per-character), so no host-only
save store and no cross-player messaging are involved — every machine manages its
own local player. Kill XP is credited on the killer's machine only
(`who == Game1.player`), so each kill counts exactly once for the right player.

## Project layout

```
levelup/
├── manifest.json              SMAPI manifest (mod version lives here)
├── LevelUp.csproj             .NET 6 project + ModBuildConfig
├── CHANGELOG.md               Version history (Keep a Changelog + SemVer)
├── ModEntry.cs                Entry point, event + Harmony wiring
├── Config/
│   ├── ModConfig.cs           Root config (serialized to config.json)
│   ├── XpSourcesConfig.cs     Per-source XP toggles + rates
│   ├── CurveConfig.cs         Curve preset + custom (base, growth)
│   ├── MilestoneConfig.cs     One of 20 milestone slots
│   └── MilestonePresets.cs    Play-style milestone presets
├── State/
│   ├── PlayerLevelData.cs     Per-player: XP, level, baselines, visited areas
│   └── SaveDataManager.cs     Wraps Helper.Data.{Read,Write}SaveData
├── Systems/
│   ├── LevelCalculator.cs     XP↔level math (pure, testable)
│   ├── XpTracker.cs           Awards XP, advances level, raises XpAwarded
│   ├── BonusApplier.cs        Applies milestone bonuses (HP/energy + buff)
│   ├── LevelUpNotifier.cs     HUD toast + sound on level-up
│   └── ConsoleCommands.cs     Debug/inspection console commands
├── Patches/
│   ├── MonsterPatches.cs      GameLocation.onMonsterKilled — kill XP
│   ├── QuestPatches.cs        Quest.questComplete — quest XP
│   ├── FarmerPatches.cs       Farmer.gainExperience — XP multiplier
│   ├── ObjectPatches.cs       Object.sellToStorePrice — sell bonus
│   └── HudTextPatch.cs        Game1.drawWithBorder — hide buried bar text
├── Integration/
│   ├── IGenericModConfigMenuApi.cs   Soft-typed GMCM interface
│   └── GmcmIntegration.cs            All GMCM registration
├── Ui/
│   └── XpBarHud.cs            Framed XP bar, level, +N XP popup, tooltips
└── i18n/
    └── default.json           Translation strings
```

## Notes

- **Multiplayer:** supported, with independent per-player levels (see above).
  Install the mod on every player's machine.
- One known gap: `SecretLostItemQuest` (a rare secret quest) fully overrides
  `questComplete` without calling base, so it doesn't grant quest XP. Every
  normal quest and all Help Wanted billboard quests work.
