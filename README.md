# Stardew Valley Mods

A collection of [SMAPI](https://smapi.io/) mods for Stardew Valley by skint007. Each mod
lives in its own folder with its own solution and can be built independently.

## Mods

| Mod | Version | Description |
| --- | --- | --- |
| [Better Fertilizer](BetterFertilizer/README.md) | 1.2.0 | Configurable fertilizer overhaul: multi-fertilizer stacking, fertilize anytime, keep fertilizer across seasons, tunable potencies/craft amounts, plus a Tree Fertilizer that works on fruit trees. |
| [Level Up](levelup/README.md) | 1.3.2 | A meta player-level system earned from in-world actions (kills, days, quests), with milestone-based stat bonuses fully configurable via GMCM. |
| [Train Up](trainup/README.md) | 0.1.0 | Train custom Defense, Vitality, and Stamina skills (shown on the vanilla skill page with their own professions) by taking hits, losing HP, and spending energy, plus a craftable training dummy you whack for Combat XP. Built on [SpaceCore](https://www.nexusmods.com/stardewvalley/mods/1348). |

These mods optionally integrate with [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098)
for in-game configuration.

## Building

Each mod targets .NET and uses the [Mod Build Package](https://github.com/Pathoschild/SMAPI/blob/develop/docs/technical/mod-package.md),
which auto-deploys the build into the game's `Mods` folder. From a mod's folder:

```bash
dotnet build
```

Build output (`bin/`, `obj/`, `*.dll`, packaged `*.zip`, etc.) is git-ignored.

## Licensing

Licensing is per-mod, not repo-wide:

- **Level Up** — [MIT](levelup/LICENSE).
- **Better Fertilizer** — [AGPL-3.0](BetterFertilizer/LICENSE), matching its upstream
  [Stardew Ultimate Fertilizer](https://github.com/foxwhite25/Stardew-Ultimate-Fertilizer)
  by fox_white25, from which it is derived and published with permission.
