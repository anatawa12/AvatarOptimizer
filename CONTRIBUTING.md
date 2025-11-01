# Contributing Guide

## Issues

Both feature requests and bug reports are welcome!

### Archive & Compression Formats

Because anatawa12 loves command line on macOS, some windows-specific archive formats are very hard to extract.
Please choose one archive / compression format from the Recommended / Accepted list below.

#### Recommended

Those formats are easy to use, good for compatibility, and good for file size.

- `.zip` with utf8
- `.gz` (for single file like log file)
- `.tar.gz` = `.tgz` (for multiple files. Better compression ratio than `.zip` in most case)
- `.tar.xz` (if you want more compression ratio than `.tar.gz`)
- `.tar.zstd` (if you want more compression ratio than `.tar.gz`)

#### Accepted

Those formats are not good enough to recommend.

- `.tar` (no compression)
- `.cpio` (no compression and not familiar to me)

#### Rejected

Those formats are not common for commandline use or posix environment.

- `.7z`
- `.cab`

## Pull Requests

Pull Requests are welcome!

Before contribution, please read [LICENSE](./LICENSE) and
agree to your contribution will be published under the license.

For small changes like fixing typo or documentation changes,
you can create Pull Requests without making issues.

Please follow [Conventional Commits] commit format.
There's check for this so it's (almost) required to follow this format.

[Conventional Commits]: https://www.conventionalcommits.org/en/v1.0.0/

Please add your changes to both `CHANGELOG-SNAPSHOS.md` and `CHANGELOG.md`
unless your change is fixing problem of feature which is not published.
If your change is published in snapshots but not in release, please update `CHANGELOG-SNAPSHOS.md`.

Because I want to include link to pull request in the CHANGELOG files, I recommend the following steps for creating Pull Requests.

For documentation or localization changes, updating CHANGELOG is not required.
The CI will fail due to the lack of CHANGELOG update, but you can ignore it.
I'll add `documentation` or `localization` label to the PR and the CI will ignore the CHANGELOG update.

1. Fork this repository
2. Create branch for changes
3. Make changes on the branch
4. Open draft Pull Request
5. Make Changes in CHANGELOG file
6. Mark the Pull Request ready for review

## Notes for writing codes

- Do not use `Object.DestroyImmediate`, use `DestroyTracker.DestroyImmediate` instead.
- When you add some components in process of optimization that persists after build (or multiple phases of optimization),
  register new component to GCComponentInfoContext.
- Before modifying code, review [ASSUMPTIONS.md](./ASSUMPTIONS.md) for system assumptions and constraints.
  Document any new assumptions you introduce.

## Adding Localization Locales

If you can be the maintainer of the new locale, I'm glad to add it for localization.
As a maintainer, you should keep the locale up-to-date as possible.

To add new locale, please follow the steps below.
1. Create `<langid>.po` file in `Localization` directory.
2. Add the `<langid>.po` to `Internal/Localization/Editor.AAOL10N.cs`
3. Fill the translations in `<langid>.po`.
4. Create draft pull request.
5. Make changes in CHANGELOG file as a notice of adding new locale
6. Mark the Pull Request ready for review

## Adding Documentation Locales

If you can be the maintainer of the new locale, I'm glad to add it for document localization.
As a maintainer, you should keep the locale up-to-date as possible.

To add new locale, please follow the steps below.
1. Add your locale to `.docs/config.toml`
2. Create `index.<langid>.md` in all files in `.docs/content` directory.
3. Create pull request. The CI will fail due to the lack of CHANGELOG update, but you can ignore it.
