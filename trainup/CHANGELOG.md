# Changelog

All notable changes to **Train Up** are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html):

- **MAJOR** — incompatible changes (e.g. save-data format breaks, config keys removed)
- **MINOR** — new functionality, backward compatible
- **PATCH** — backward-compatible bug fixes

The mod's version lives in [`manifest.json`](manifest.json); bump it there to match
the entry you add below.

## [Unreleased]

## [0.1.0] - 2026-06-02

### Added

- **Three custom skills on the vanilla skill page** (via SpaceCore), each with its own XP bar and
  a profession choice at level 5 and 10:
  - **Defense** — trained by taking hits; XP scales with damage actually taken (after armor).
  - **Vitality** — trained by losing HP from any source.
  - **Stamina** — trained by spending energy.
- **Profession perks** for all three skills: defense/HP/energy boosts (save-safe buffs that are
  stripped before saving and reapplied after), HP/energy regen, dodge + counter + retaliate,
  low-HP damage scaling, healing/energy-from-food bonuses, and tool-energy savings.
- **Training Dummy** big craftable (25 Wood + 10 Stone). Hitting it with a weapon grants vanilla
  Combat XP per hit, with a configurable daily cap. Hits are detected via the
  `GameLocation.damageMonster` choke point, with a hit sound and floating XP feedback.
- **GMCM config** for the master toggle, profession-perks toggle, per-skill XP rates, per-skill
  daily caps, and the dummy's XP-per-hit / daily cap.
- **Console commands**: `trainup_skills`, `trainup_addxp`, `trainup_dummy`.
