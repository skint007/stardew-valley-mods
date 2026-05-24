# Better Fertilizer

A configurable fertilizer overhaul for Stardew Valley 1.6 (SMAPI 4.x).

This is a clean reimplementation of the feature set from
[Stardew Ultimate Fertilizer](https://github.com/foxwhite25/Stardew-Ultimate-Fertilizer)
by fox_white25, **plus** one extra feature: Tree Fertilizer also works on fruit
trees (vanilla deliberately ignores them).

## Features

Everything is configurable in-game via [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) (optional dependency).

### Crop fertilizers (from Ultimate Fertilizer)
- **Fertilizer modes**: `multi-fertilizer-stack`, `multi-fertilizer-single-level`,
  `single-fertilizer-replace`, `single-fertilizer-stack`, or `Vanilla`. Mix
  speed / quality / water-retaining fertilizers on the same tile.
- **Fertilize anytime** – apply fertilizer to tiles that already have a growing crop.
- **Keep fertilizer across seasons** – fertilizer no longer disappears each season.
- **Tunable potency** for every tier of Speed-Gro, Quality and Retaining Soil.
- **Tunable craft amounts** per fertilizer crafting recipe.
- **Speed-Gro after harvest** for multi-harvest crops, and an optional fix for the
  vanilla multi-drop quality bug.
- **Fertilizer sprite transparency** setting.

### Tree Fertilizer on fruit trees (new)
In vanilla, Tree Fertilizer (`(O)805`) only affects wild trees and does nothing on
fruit trees. With this mod you can apply it to a fruit tree, and:

- **Grow Anywhere** – the fertilized fruit tree ignores the surrounding-clearance
  rule, so it keeps growing even when crowded by objects, paths, crops or other trees
  (this mirrors how Tree Fertilizer lets wild trees grow regardless of crowding).
- **Maturity cut on apply** – the moment you fertilize a tree, its remaining
  days-to-mature are cut by a configurable percentage. Default **50%** (a tree with
  14 days left jumps to 7); 100% matures it instantly; 0% disables it.
- **Optional ongoing rate** – a fertilized tree can also gain extra maturity each
  day going forward (default off / vanilla speed; up to 7×).

The fertilized state is stored on the fruit tree's `modData`, so it persists across
saves and is per-tree. Each of the three behaviours (enable / grow-anywhere /
maturation rate) has its own GMCM toggle.

> Note: fruit trees are not visually tinted when fertilized (wild trees turn pink in
> vanilla because that draw code is specific to the `Tree` class). The effect is
> functional only.

## Building

The build auto-detects a standard Steam/GOG install. For a non-standard path, create
`BetterFertilizer.csproj.user` (git-ignored) next to the `.csproj`:

```xml
<Project>
  <PropertyGroup>
    <GamePath>/path/to/Stardew Valley</GamePath>
  </PropertyGroup>
</Project>
```

Then `dotnet build -c Release`. The
[ModBuildConfig](https://github.com/Pathoschild/SMAPI/blob/develop/docs/technical/mod-package.md)
package copies the built mod into your `Mods` folder automatically.

## Credits

Original mechanics and design by **fox_white25**
([Stardew Ultimate Fertilizer](https://github.com/foxwhite25/Stardew-Ultimate-Fertilizer)).
Fruit-tree support and repackaging by skint007.
