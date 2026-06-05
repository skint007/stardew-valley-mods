# Level Up — Nexus description blocks

Source of truth for the BBCode blocks shown on the Level Up Nexus mod page (sections that aren't the changelog). Paste the relevant block into the Nexus description editor when the page needs to be updated. Single source so the values can't drift from the in-code defaults in `Config/ModConfig.cs` and `Config/MilestonePresets.cs`.

Sibling files:

- `CHANGELOG-nexus.md` — release notes per version (paste new section after a release).
- This file — feature documentation blocks that are always-visible on the page (paste when the underlying values change in code).

Notes for editors:

- Nexus does not reliably honor `[spoiler=Title]`; titled-spoiler markup tends to render as plain "Spoiler [Show]" buttons. Use a bold header *outside* the spoiler tag and a plain `[spoiler]` inside instead.
- One bullet per line — Nexus's editor turns soft-wrapped lines into separate bullets.
- House style: no em dashes, "and" not "&", `[b]Lv N: Name[/b] - bonuses` shape for each milestone bullet.

---

## Milestones section

```bbcode
[size=4][b]Default Milestones[/b][/size]

Nine milestones come pre-configured (the "Balanced" preset). Each is fully editable in the in-game settings menu (Generic Mod Config Menu), and all milestones you've reached stack cumulatively.

[list]
[*][b]Lv 5: Initiate[/b] - +10 max HP, +10 max energy
[*][b]Lv 10: Apprentice[/b] - +1 attack, +1 defense, +5% skill XP gain
[*][b]Lv 15: Journeyman[/b] - +10 max HP, +10 max energy, +1 luck
[*][b]Lv 20: Adept[/b] - +5% crit chance, +5% weapon speed
[*][b]Lv 25: Veteran[/b] - +2 attack, +2 defense, +64 magnetic radius
[*][b]Lv 35: Expert[/b] - +20 max HP, +20 max energy, +5% sell price, +5 HP/min and +10 energy/min regen
[*][b]Lv 50: Master[/b] - +1 movement speed, +10% skill XP gain
[*][b]Lv 75: Champion[/b] - +3 attack, +3 defense, +5% crit chance, +10 HP/min and +20 energy/min regen
[*][b]Lv 100: Legend[/b] - +50 max HP, +50 max energy, +10% sell price, +1 luck, +25 HP/min and +40 energy/min regen
[/list]

You can edit any of these, add up to 20 milestones total, or apply a one-click preset (themes below) from the Milestones page in the settings menu. Available bonus types include max HP / energy, HP and energy regen, attack, defense, crit chance, weapon speed, movement speed, magnetic radius, luck, skill XP gain, sell price, and gameplay rewards like extra crop chance, extra ore chance, and faster machine processing.

[size=4][b]Alternate Presets[/b][/size]

Apply any of these from the Milestones page in the settings menu by picking the name from the "Apply preset" dropdown and hitting Save. Click "Show" to view the full progression.

[b]Combat[/b] - attack, defense, crit, weapon speed; light HP
[spoiler]
[list]
[*][b]Lv 5: Recruit[/b] - +1 attack, +1 defense
[*][b]Lv 10: Fighter[/b] - +3% crit chance, +5% weapon speed
[*][b]Lv 15: Soldier[/b] - +2 attack, +2 defense
[*][b]Lv 20: Warrior[/b] - +15 max HP, +3% crit chance
[*][b]Lv 25: Vanguard[/b] - +3 attack, +3 defense, +5% weapon speed
[*][b]Lv 35: Slayer[/b] - +5% crit chance, +20 max HP
[*][b]Lv 50: Warlord[/b] - +5 attack, +5 defense, +5 HP/min regen
[*][b]Lv 75: Berserker[/b] - +7% crit chance, +10% weapon speed, +0.5 movement speed, +10 HP/min regen
[*][b]Lv 100: Godslayer[/b] - +10 attack, +10 defense, +10% crit chance, +30 max HP, +20 HP/min regen
[/list]
[/spoiler]

[b]Survivalist[/b] - big max HP/energy, defense, a little luck
[spoiler]
[list]
[*][b]Lv 5: Hardy[/b] - +15 max HP, +15 max energy
[*][b]Lv 10: Tough[/b] - +2 defense, +15 max energy
[*][b]Lv 15: Resilient[/b] - +20 max HP, +20 max energy, +5 HP/min and +10 energy/min regen
[*][b]Lv 20: Stalwart[/b] - +3 defense, +1 luck
[*][b]Lv 25: Enduring[/b] - +30 max HP, +30 max energy, +10 HP/min and +15 energy/min regen
[*][b]Lv 35: Ironhide[/b] - +5 defense, +25 max HP
[*][b]Lv 50: Unbreakable[/b] - +50 max HP, +50 max energy, +20 HP/min and +30 energy/min regen
[*][b]Lv 75: Juggernaut[/b] - +8 defense, +50 max energy, +1 luck
[*][b]Lv 100: Immortal[/b] - +100 max HP, +100 max energy, +10 defense, +35 HP/min and +55 energy/min regen
[/list]
[/spoiler]

[b]Explorer[/b] - movement, magnetism, luck, XP gain, sell price
[spoiler]
[list]
[*][b]Lv 5: Wanderer[/b] - +32 magnetic radius, +0.25 movement speed
[*][b]Lv 10: Scout[/b] - +5% skill XP gain, +32 magnetic radius
[*][b]Lv 15: Pathfinder[/b] - +0.5 movement speed, +1 luck
[*][b]Lv 20: Trader[/b] - +5% sell price
[*][b]Lv 25: Pioneer[/b] - +64 magnetic radius, +0.5 movement speed, +10 energy/min regen
[*][b]Lv 35: Prospector[/b] - +1 luck, +10% skill XP gain
[*][b]Lv 50: Trailblazer[/b] - +1 movement speed, +5% sell price, +15 energy/min regen
[*][b]Lv 75: Voyager[/b] - +128 magnetic radius, +2 luck
[*][b]Lv 100: Pathlord[/b] - +1 movement speed, +15% sell price, +15% skill XP gain, +2 luck, +30 energy/min regen
[/list]
[/spoiler]

[b]Minimalist[/b] - small bonuses, fewer slots; levels feel rewarding without trivializing
[spoiler]
[list]
[*][b]Lv 10: Apprentice[/b] - +5 max HP, +5 max energy
[*][b]Lv 25: Adept[/b] - +1 attack, +1 defense
[*][b]Lv 50: Expert[/b] - +10 max HP, +10 max energy, +5% skill XP gain
[*][b]Lv 75: Master[/b] - +2 attack, +2 defense
[*][b]Lv 100: Grandmaster[/b] - +15 max HP, +15 max energy, +1 luck, +25 HP/min and +60 energy/min regen
[/list]
[/spoiler]

[b]Empty[/b] - clean slate to design your own
[spoiler]
All 20 slots disabled. Use this when you want to build a custom milestone progression from scratch.
[/spoiler]
```
