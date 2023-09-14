# Contributing Guide

## Issues

Both feature requests and bug reports are welcome!

### Archive & Compression Formats

Because anatawa12 loves command line on macOS, some windows-specific archive formats are very hard to extract.
Please choose one archive/compression format from the Recommended / Accepted list below.

#### Recommended

Those formats are easy to use, good for compatibility, and good for file size.

- `.zip` with utf8
- `.gz` (for single file like log files)
- `.tar.gz` = `.tgz` (for multiple files. better compression than `.zip` in most case)
- `.tar.xz` (if you want more compression ratio than `.tar.gz`)
- `.tar.zstd` (if you want more compression ratio than `.tar.gz`)

#### Accepted

Those formats are not good enough to recommend.

- `.tar` (no compression)
- `.cpio` (no compression and not familiar to me)

#### Rejected

Those formats are not common for commandline use nor posix environment.

- `.7z`
- `.cab`

## Pull Requests

Pull Requests are welcome!

Before contribution, please read [LICENSE](./LICENSE) and
please agree to your contribution will be published under the license.

For small changes like fixing typo or documentation changes,
You can create Pull Requests without making issues.

Please follow [Conventional Commits] commit format.
There's check for this so it's (almost) required to follow this format.

[Conventional Commits]: https://www.conventionalcommits.org/en/v1.0.0/

Please add your changes to both `CHANGELOG-SNAPSHOS.md` and `CHANGELOG.md`
unless your change is fixing problem of feature which is not published.
If your change is published in snapshots but not in release, please update `CHANGELOG-SNAPSHOS.md`.

Because I want to include link to pull request in the CHANGELOG files, I recommend the following steps for creating Pull Requests.

1. Fork this repository
2. Create branch for changes
3. Make changes on the branch
4. Open draft Pull Request
5. Make Changes in CHANGELOG file
6. Mark the Pull Request ready for review
