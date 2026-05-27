# Contributing

Thanks for your interest in these mods! This repo holds multiple Stardew Valley
mods (see the [README](README.md)); issues and PRs for all of them live here.

## Reporting bugs and requesting features

**Please [open an issue](https://github.com/skint007/stardew-valley-mods/issues/new/choose).**
There are two forms — *Bug report* and *Feature request* — each with a dropdown to
pick which mod it's about. A SMAPI log link (upload at <https://smapi.io/log>) is by
far the most useful thing you can attach to a bug.

Prefer the Nexus Mods forums? That's fine — post there and I'll mirror anything
actionable into a GitHub issue so it's actually tracked (Nexus comments scroll away;
issues don't). If you *do* have a GitHub account, filing the issue yourself is the
fastest path, since it skips the transcription step.

Before opening something new, a quick scan of the
[existing issues](https://github.com/skint007/stardew-valley-mods/issues) helps avoid
duplicates.

## Pull requests

PRs are welcome. A few notes:

- Each mod lives in its own folder with its own solution and builds independently
  (`dotnet build` from the mod folder). See the per-mod README for specifics.
- Keep a PR scoped to a single mod where you can — CI builds only the mod folders
  that changed.
- Mention the issue it addresses (`Closes #123`) so it links up and closes on merge.
- Note user-facing changes in that mod's `CHANGELOG.md` under `[Unreleased]`; the
  version in `manifest.json` is what drives releases.

## Licensing

Licensing is per-mod — see each mod's `LICENSE` file (summarized in the
[README](README.md#licensing)). By contributing, you agree your changes are licensed
under the same terms as the mod you're contributing to.
