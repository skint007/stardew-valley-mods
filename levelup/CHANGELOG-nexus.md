# Level Up — What's New

A short, player-friendly version of the changelog for the Nexus page. The full, technical history lives in [CHANGELOG.md](CHANGELOG.md).

Note: each bullet is intentionally on a single line so it pastes into Nexus's editor without becoming several bullets (Nexus turns newlines into new list items).

## 1.3.7

Fixed
- Fixed a regression from 1.3.5 that permanently inflated max stamina (and in some cases max HP) on every level-up and every time you saved the mod settings. The Stardrop-detection logic added in 1.3.5 was reading the max stamina getter, which includes food, ring, and other-mod buffs, and mistaking those buff bonuses for a permanent vanilla bump. It baked them into the baseline and re-added them on every apply, so the value grew forever. Both sides of the check now use the base value only, so buffs no longer leak into the baseline. Big thanks to miraj6, AvatarRuizu0v0, and rafaelhiv for the reports.

Recovery
- If you played 1.3.5 or 1.3.6 and your max HP / stamina is now higher than it should be, there's a new console command to put it back. In the SMAPI console, type: levelup_setbaseline 100 270 (that's the vanilla starting max HP and max stamina). Add 34 to the second number for each Stardrop you've eaten, and add 25 to the first number if you've done the Combat Mastery cave.

## 1.3.6

Fixed
- Fixed a runaway XP spike that could happen when an explosion-on-kill ring (or similar) destroyed many rocks at once. The Quarry was the worst case (a single explosion could grant millions of meta XP through the "Scale with skill XP" source). There's now a per-call cap (default 5,000) on how much skill XP is absorbed at once. The skill itself still receives the full amount; only what feeds the mod's meta XP is capped. The cap is configurable from the settings menu, and setting it to 0 turns it off.

## 1.3.5

New
- The mod's version number now shows at the top of the in-game settings page, so it's easy to see what version you have installed without leaving the game.

Fixed
- Fixed Stardrops and the Combat Mastery cave reward (+25 max HP) being wiped if you ate them after installing the mod. Affected characters will see them restored on the next day load.

## 1.3.4

New
- New "Toggle XP bar hotkey" setting. Bind a key to hide or show the XP bar in-place, no need to open the menu.
- New "Vertical bar left offset" setting. Shift the vertical XP bar left or right by a chosen number of pixels to keep it clear of vertical bars added by other mods (Magic mana, Hunger, Thirst, etc.).

## 1.3.3

Fixed
- Bonus crops no longer keep dropping when you sweep a scythe (especially the Iridium scythe) back over a crop you've already harvested that's currently regrowing. Each plant only gives a bonus on the actual harvest now.

## 1.3.2

Fixed
- The "+XP gain", "+Sell price", "+Crit chance", "+Weapon speed", and curve "Growth rate" sliders could show ".09" when you'd snapped them to ".10" (and similar values). The stored value was already correct; the display is now too.
- Each milestone slot now has a "← Back to Milestones" link at the top, so editing a slot no longer strands you on the main page when you back out.

## 1.3.1

Fixed
- Fixed a bug where loading a save, returning to title, and then starting or loading another save in the same session could carry the first save's level over. Progress is properly per-character again; each save starts at its own level.

## 1.3.0

New
- The horizontal XP bar is now resizable, so mobile and small-screen players can shrink it (or make it bigger). There's a new "XP bar size" slider in the settings.
- The "LVL" number now sits on a dark plate so it's easier to read at any size.
- Three new milestone bonuses you can assign in the settings (off by default, so existing milestones stay the same): **+Extra crop chance** for a chance at a bonus crop on harvest; **+Extra ore chance** for a chance to double up on what you mine; **+Machine speed** to make machines (kegs, furnaces, preserves jars, and so on) finish faster.

## 1.2.1

Fixed
- Fixed a bug where saving mid-day (on mobile, or with mods like Save Anywhere on PC) could roll your level back to where it was when you woke up. Progress is now saved continuously so any save captures your current level.

## 1.2.0

New
- The XP bar can now sit vertically next to your health and energy bars, and it fades out when you haven't gained XP for a bit (like the vanilla bars do). Hover it or gain XP to bring it back. There's a toggle for it in the settings.

Improved
- Leveling pace feels much steadier. Before, you shot up early and then hit a wall where level 100 felt impossible. Now progress is spread out more evenly.
- Everyday tasks (farming, fishing, mining, foraging, combat) reliably count toward your level now. Small tasks used to round down to nothing.
- Monster XP was toned down so the mines don't level you quite so fast.
- Settings sliders are easier to set to exact values, and milestone bonus limits are more reasonable.

Fixed
- Fixed a bug where certain XP curve settings (a steep custom growth, or a high level cap) could break leveling and shoot you to a nonsense level like 314 or 800.
- Changing your settings now takes effect right away, instead of causing a surprise level jump a little later.

Heads up
- Because the leveling curve is gentler now, existing characters may jump up a few levels the first time you load after updating. That's expected, and you keep all the milestone bonuses you've unlocked.

## 1.1.1
- Brought the 1.1 features to Nexus. No gameplay changes.

## 1.1.0
- Multiplayer support, with each player keeping their own level and bonuses.
- Milestone presets (Balanced, Combat, Survivalist, Explorer, and more) you can apply in one click.
- More ways to earn XP, including a share of all the skill XP you earn from tasks.
- Passive health/energy regen as a milestone reward.
- An optional hotkey to jump straight to the settings menu.
- A redesigned XP bar above the toolbar.

## 1.0.0
- First release: a player level system that earns XP from monster kills, quests, and days survived, with milestone stat bonuses, all configurable in-game.
