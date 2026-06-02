# Train Up

A [Stardew Valley](https://www.stardewvalley.net/) [SMAPI](https://smapi.io/) mod that lets you
**actively train** new skills through play. It adds three custom skills to the vanilla skill page
(with their own XP bars and professions, courtesy of [SpaceCore](https://www.nexusmods.com/stardewvalley/mods/1348)),
plus a craftable training dummy for practicing combat.

> **Status:** initial development (0.1.0). Single-player focused; built on SpaceCore's custom-skill
> framework. See [CHANGELOG.md](CHANGELOG.md) for version history.

## Skills

Each skill appears on the vanilla skill page with a matching XP bar and offers a profession choice
at level 5 and again at level 10.

- **Defense** — trained by **taking hits** (XP scales with the damage you actually take, after
  armor). Professions:
  - *Tough* (+1 Defense) → *Ironhide* (+2 more Defense) / *Retaliate* (reflect 25% of damage to the attacker)
  - *Evasive* (10% dodge) → *Acrobat* (20% dodge) / *Counter* (heal a little when you dodge)
- **Vitality** — trained by **losing HP** (from any source). Professions:
  - *Hardy* (+15 max HP) → *Juggernaut* (+25 more max HP) / *Bloodied* (more damage the lower your HP)
  - *Recovery* (HP regen) → *Second Wind* (faster HP regen) / *Medic* (+25% healing from items)
- **Stamina** — trained by **spending energy**. Professions:
  - *Energetic* (+25 max energy) → *Marathoner* (+50 more max energy) / *Tireless* (energy regen)
  - *Efficient* (tools cost 10% less) → *Conservationist* (20% chance of a free action) / *Caffeinated* (+25% energy from food)

> A monster hit trains **both** Defense (the hit) and Vitality (the HP lost). Both rates are
> independent and either can be set to 0.

## Training dummy

Craft a **Training Dummy** (25 Wood + 10 Stone) and place it anywhere. Whack it with a weapon to
earn **vanilla Combat XP** per hit, with a configurable daily cap so it can't be cheesed. It's a
normal big craftable, so it saves and picks up like any other.

## Configuration

With [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) installed, all of
this is tunable in-game (otherwise edit `config.json`):

- Master enable, profession-perks toggle, debug logging.
- Per-skill XP rates (set any to 0 to disable that training source).
- Per-skill daily XP caps.
- Dummy: enable, Combat XP per hit, daily cap.

## Console commands

- `trainup_skills` — show your current skill levels and XP.
- `trainup_addxp <defense|vitality|stamina> <amount>` — award XP to a skill.
- `trainup_dummy` — add a Training Dummy to your inventory.

## Requirements

- [SMAPI](https://smapi.io/) 4.0.0+
- [SpaceCore](https://www.nexusmods.com/stardewvalley/mods/1348) (**required** — provides the custom-skill framework)
- [Generic Mod Config Menu](https://www.nexusmods.com/stardewvalley/mods/5098) (optional — in-game config UI)

## Building

From this folder: `dotnet build`. The
[Mod Build Package](https://github.com/Pathoschild/SMAPI/blob/develop/docs/technical/mod-package.md)
auto-deploys into the game's `Mods` folder. To build against SpaceCore, it must be installed in
your game's `Mods` folder (it's referenced from there).

## License

[MIT](LICENSE).
