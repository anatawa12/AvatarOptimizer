# Avatar Optimizer Development Instructions

Avatar Optimizer is a Unity Package Manager (UPM) package for VRChat avatar optimization using the Non-Destructive Modular Framework (NDMF).
It is NOT a standalone Unity project - it's a package that gets installed into Unity projects via VPM (VRChat Package Manager).

## Working Effectively

### Prerequisites and Setup
NEVER try to open this repository directly in Unity. This is a Unity package, not a Unity project.

#### Code Generation
Some C# files are generated from TypeScript. Run this when modifying .ts files:

```bash
# Generate C# code from TypeScript
npx tsx Editor/.MergePhysBoneEditorModificationUtils.ts > Editor/MergePhysBoneEditorModificationUtils.generated.cs
```

#### Documentation Building
Documentation is built with Hugo and takes approximately 6 minutes. **Requires submodule initialization first:**

```bash
cd .docs
git submodule update --init --recursive  # Initialize hugo-book theme (required for documentation)
# Full build (will fail on fetch but Hugo succeeds):
./build.sh 'https://test.example.com' 'test-version'

# Or Hugo only (faster, ~300ms):
hugo --minify --baseURL 'https://test.example.com'
```

**Note:** The build script tries to fetch from real URLs for version management. In sandboxed environments, expect network errors but Hugo build will still complete successfully.

### CHANGELOG Macros and Rules

Any text that looks like `#1234` is automatically converted to ``[`#1234`](https://github.com/anatawa12/AvatarOptimizer/pull/1234)`` on release.
Therefore, it's preferred to use this macro instead of generating link fully.

All CHANGELOG entries should be appended to each section, not prepended in general.
This is NOT strict rule so we might choose to prepend to show at the top of changelog.

Changelog entry should be linked to pull request, not issues, to know what was changed easily.
Many small bugs doesn't have corresponding pull request so there is this rule.

### Commit Message Requirements
This project uses [Conventional Commits](https://www.conventionalcommits.org/en/v1.0.0/) format. All commit messages are validated via GitHub Actions (see `.github/workflows/commitlint.yml`).

**Required format:**
```
type(scope): description

[optional body]

[optional footer(s)]
```

**Examples:**
- `feat(merge-physbone): add new optimization algorithm`
- `fix(editor): resolve component selection issue` 
- `docs: update installation instructions`
- `chore: update dependencies`

**Common types:** `feat`, `fix`, `docs`, `style`, `refactor`, `test`, `chore`
