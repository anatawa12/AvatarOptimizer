# Avatar Optimizer Development Instructions

Avatar Optimizer is a Unity Package Manager (UPM) package for VRChat avatar optimization using the Non-Destructive Modular Framework (NDMF). It is NOT a standalone Unity project - it's a package that gets installed into Unity projects via VPM (VRChat Package Manager).

Always reference these instructions first and fallback to search or bash commands only when you encounter unexpected information that does not match the info here.

## Working Effectively

### Prerequisites and Setup
NEVER try to open this repository directly in Unity. This is a Unity package, not a Unity project.

#### Required Tools Installation:
- **Unity 2022.3 or later** - Download from https://unity.com/releases/editor/archive
- **VRC-Get** - VRChat package manager CLI:
  ```bash
  # Download latest release from https://github.com/vrc-get/vrc-get/releases
  # Extract and add to PATH
  vrc-get --version
  ```
- **Node.js 20+** - For TypeScript code generation (optional, Deno can also be used):
  ```bash
  node --version  # Should be 20.19.4 or later
  # Only needed when modifying .ts files that generate C# code
  # Documentation building does not require Node.js
  ```
- **Hugo Extended** - For documentation building (version specified in workflow files):
  ```bash
  # Version is defined in .github/workflows/update-website.yml and .github/workflows/release.yml
  # Currently 0.148.1 but check workflow files for latest version
  # Download from https://github.com/gohugoio/hugo/releases
  # Extract hugo binary to /usr/local/bin/
  hugo version  # Should show extended version
  ```

#### Setting Up Development Environment:
1. **Create a Unity Project:**
   ```bash
   # Create a new Unity 2022.3 project for development
   # Do NOT try to open this repository directly in Unity
   ```

2. **Install Dependencies via VRC-Get:**
   ```bash
   vrc-get install com.vrchat.avatars
   vrc-get repo add https://vpm.anatawa12.com/vpm.json
   vrc-get repo add https://vpm.nadena.dev/vpm-prerelease.json
   ```

3. **Install the Package for Development:**
   ```bash
   # In your Unity project, add this as a local package
   # Window > Package Manager > + > Add package from disk
   # Select the package.json in this repository
   ```

4. **Initialize Submodules (for documentation only):**
   ```bash
   cd /path/to/AvatarOptimizer
   git submodule update --init --recursive
   # Only necessary when building documentation
   ```

### Build and Test Process

#### Running Tests - NEVER CANCEL, ALLOW 30+ MINUTES
Tests MUST be run through Unity's Test Runner. Build times are significant due to Unity compilation.

```bash
# Tests run via Unity Test Runner in EditMode
# In Unity Editor: Window > General > Test Runner
# Or run via GameCI (see .github/workflows/gameci.yml)
```

**CRITICAL TIMEOUT SETTINGS:**
- **Unity Test Runner: NEVER CANCEL - Allow 30+ minutes**
- **GameCI builds: NEVER CANCEL - Allow 45+ minutes**
- Always set timeouts of 60+ minutes for any Unity-related commands

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

### Development Workflow

#### Making Code Changes:
1. **ALWAYS test in a Unity project with VRChat SDK installed**
2. **Run Unity Test Runner after changes** - NEVER CANCEL, allow 30+ minutes
3. **Regenerate code if modifying .ts files**
4. **Build documentation if updating docs**

#### Testing and Validation:
- **Unit Tests:** 145 test methods across 30 test files in Test~ directory, run via Unity Test Runner
- **Manual Testing:** Always test with actual VRChat avatars in Unity
- **CI Validation:** GameCI runs EditMode tests with 45+ minute timeouts

#### Key Validation Steps:
Always run these before committing changes:
1. **Unity Test Runner** (EditMode) - NEVER CANCEL, 30+ minute timeout
2. **Code generation** if TypeScript files changed
3. **Documentation build** if docs changed
4. **Manual avatar testing** in Unity Editor

## Repository Structure

### Key Directories:
- `Runtime/` - Main optimization logic and runtime C# code
- `Editor/` - Configuration components and Unity Editor UI scripts  
- `API-Editor/` - Public API for other developers
- `Test~/` - Unity test assemblies (145 test methods across 30 test files)
- `.docs/` - Hugo documentation site (requires submodule init)
- `Localization/` - Translation files (.po format)
- `Internal/` - Internally used utility libraries

### Important Generated Files:
- `Editor/MergePhysBoneEditorModificationUtils.generated.cs` - Generated from `.ts` file
- `.docs/public/` - Hugo output directory (after build)

### Assembly Definitions:
- `com.anatawa12.avatar-optimizer.runtime.asmdef` - Main runtime assembly
- `com.anatawa12.avatar-optimizer.editor.asmdef` - Editor assembly
- `com.anatawa12.avatar-optimizer.api.editor.asmdef` - Public API
- Multiple test assemblies in Test~ directory

### Important Files:
- `package.json` - Unity package manifest with dependencies (version 1.9.0-beta.2)
- `CONTRIBUTING.md` - Contribution guidelines and localization info
- `.github/workflows/gameci.yml` - CI/CD with Unity testing

## Common Tasks

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

### Git Rebase Workflow
This project maintains a clean commit history through rebasing. GitHub Copilot can use the automated rebase workflow when force-push capabilities are needed.

#### Automated Rebase for GitHub Copilot:
When GitHub Copilot needs to rebase commits (including removing/dropping commits), use the automated rebase workflow by creating a `.github/rebase-config.yml` file.

**When to Use Automated Rebase:**
- Squash multiple commits into one with a cleaner commit message
- Combine feature commits before merging to main branch
- Clean up commit history by combining related changes
- Prepare commits for a pull request review
- Drop initial plan commits or work-in-progress commits that are no longer needed

**Important Notes:**
- The `rebase-config.yml` commit should be the last commit without any other change since any commits after it will be removed during the rebase process
- **GitHub Copilot Limitation**: When GitHub Copilot creates rebase configurations, the automated workflow will require manual approval before execution. This is because GitHub Copilot operates as a GitHub App, and GitHub's security model requires approval for workflows with write permissions triggered by GitHub Apps. Repository owners will need to manually approve the workflow run in the Actions tab.

#### Rebase Configuration File Format

The `.github/rebase-config.yml` file uses YAML format for better reliability and multi-line message support:

```yaml
# Base commit - the commit before the ones you want to modify
base_commit: "a1b2c3d4e5f6789012345678901234567890abcd"

# List of operations to perform
operations:
  - action: "pick"
    commit: "b2c3d4e5f6789012345678901234567890abcde"
    
  - action: "squash" 
    commit: "c3d4e5f6789012345678901234567890abcdef"
    message: |
      Implement user authentication feature
      
      - Add OAuth2 support
      - Fix session handling
      - Add comprehensive tests
      
      Co-authored-by: Jane Smith <jane@example.com>
```

**Structure Details:**
1. **`base_commit`**: The SHA hash of the commit before the ones to modify
2. **`operations`**: Array of operations to perform on specific commits

**Supported Operations:**
- **`pick`**: Keep the commit as-is
  ```yaml
  - action: "pick"
    commit: "abc1234567890abcdef1234567890abcdef12"
  ```

- **`squash`**: Combine with the previous commit, with optional new message
  ```yaml
  - action: "squash"
    commit: "abc1234567890abcdef1234567890abcdef12"
    message: "New combined commit message"
  ```

**Multi-line Message Support:**
The YAML format supports multi-line commit messages using the `|` operator:

```yaml
message: |
  Implement comprehensive authentication system
  
  This feature includes:
  - OAuth2 integration with multiple providers
  - Session management with Redis
  - Password strength validation
  - Two-factor authentication support
  
  Fixes #123, #124
  
  Co-authored-by: Alice Developer <alice@company.com>
  Co-authored-by: Bob Tester <bob@company.com>
```

#### Example Scenarios

**Scenario 1: Squash Last 3 Commits**
When you have made 3 related commits and want to combine them:

```yaml
# Base commit from 4 commits ago
base_commit: "a1b2c3d4e5f6789012345678901234567890abcd"

operations:
  # Keep the first commit
  - action: "pick"
    commit: "b2c3d4e5f6789012345678901234567890abcde"
  
  # Squash the next two commits with a new message
  - action: "squash"
    commit: "c3d4e5f6789012345678901234567890abcdef"
    message: |
      Implement user authentication with tests and documentation
      
      Combined changes:
      - Initial authentication implementation
      - Bug fixes and improvements  
      - Comprehensive test suite
      - API documentation updates
      
      Co-authored-by: John Doe <john@example.com>
  
  - action: "squash"
    commit: "d4e5f6789012345678901234567890abcdef01"
```

**Scenario 2: Multiple Squash Groups**
When you want to create separate logical commits:

```yaml
base_commit: "a1b2c3d4e5f6789012345678901234567890abcd"

operations:
  - action: "pick"
    commit: "b2c3d4e5f6789012345678901234567890abcde"
  - action: "squash"
    commit: "c3d4e5f6789012345678901234567890abcdef"
    message: "Add authentication system"
  
  - action: "pick"
    commit: "d4e5f6789012345678901234567890abcdef01"
  - action: "squash"
    commit: "e5f6789012345678901234567890abcdef012"
    message: "Update documentation and tests"
```

**Scenario 3: Dropping Initial Plan Commit**
When you want to remove an initial planning commit:

```yaml
base_commit: "a1b2c3d4e5f6789012345678901234567890abcd"

operations:
  # Skip the initial plan commit entirely by not including it
  - action: "pick"
    commit: "c3d4e5f6789012345678901234567890abcdef"  # Skip commit b2c3... (initial plan)
  - action: "squash"
    commit: "d4e5f6789012345678901234567890abcdef01"
    message: "Implement feature with complete implementation"
```

#### Safety Notes
Always remember that:
- This workflow performs force pushes
- Only use on branches where force pushing is safe
- The rebase config file is automatically removed after successful completion
- You should backup your branch before performing complex rebases
- The YAML format prevents injection attacks and parsing errors

### Testing Changes:
```bash
# 1. Make code changes
# 2. Open Unity with your test project
# 3. Window > General > Test Runner
# 4. Run EditMode tests - NEVER CANCEL, allow 30+ minutes
# 5. Test manually with VRChat avatars
```

### Building Documentation:
```bash
cd .docs
./build.sh 'https://vpm.anatawa12.com/avatar-optimizer/beta' '1.9.0-beta.2'
# Takes 6 minutes, network errors are expected in sandboxed environments
```

### Code Generation (when modifying .ts files):
```bash
npx tsx Editor/.MergePhysBoneEditorModificationUtils.ts > Editor/MergePhysBoneEditorModificationUtils.generated.cs
```

### Repository Structure Check:
```bash
# View package info
cat package.json | grep -E '"name"|"version"|"unity"'

# Count test methods  
find Test~ -name "*.cs" -exec grep -c "\[Test\]" {} \; | awk '{sum += $1} END {print "Total test methods: " sum}'

# Check submodules
git submodule status
```

### Release Process (Reference Only):
The release process uses:
- `something-releaser` for version management
- GameCI for automated testing  
- Hugo for documentation building
- VPM for package distribution

## Validation Requirements

### CRITICAL: Manual Testing Protocol
After making changes, ALWAYS test complete user scenarios:

1. **Create test avatar in Unity with VRChat SDK**
2. **Add AAO Trace and Optimize component**
3. **Enter Play Mode** (this triggers optimization)
4. **Verify avatar behavior is unchanged**
5. **Test upload process** (if applicable)

### Build Time Expectations:
- **Unity Test Runner:** 15-30 minutes - NEVER CANCEL
- **GameCI builds:** 30-45 minutes - NEVER CANCEL  
- **Documentation build:** 6 minutes (full) or 300ms (Hugo only)
- **Code generation:** Under 30 seconds

### Known Limitations:
- This environment may not have Unity installed - tests run via GameCI in CI
- Network restrictions may cause Hugo build warnings (expected)
- VRC-Get may not be available - install manually if needed
- Some URLs may be blocked in sandboxed environments

## Troubleshooting

### Common Issues:
- **"Unity not found"** - This is expected, use GameCI for testing
- **Hugo network errors** - Expected in sandboxed environments, build still succeeds
- **VRC-Get not available** - Install manually from GitHub releases
- **Package import errors** - Ensure VRChat SDK is installed first

### Error Patterns:
- Network timeouts during documentation build are normal
- Unity compilation warnings about missing dependencies need VRChat SDK
- Test failures often indicate breaking changes to optimization logic

### Critical Don'ts:
- **NEVER open this repository directly in Unity** - It's a package, not a project
- **NEVER cancel Unity builds or tests** - They take 30+ minutes normally
- **NEVER modify generated .cs files** - Modify the .ts source instead
- **NEVER commit without testing** - Always run full validation workflow

### Debugging Workflow:
1. **Check package structure:** Verify this is installed as UPM package in Unity project
2. **Verify dependencies:** Ensure VRChat SDK 3.7.0+ and NDMF 1.8.0+ are installed
3. **Test with clean avatar:** Use simple test avatar without existing AAO components
4. **Check Console:** Unity Console shows detailed error messages
5. **Manual validation:** Always test actual avatar upload process

Always test thoroughly with real VRChat avatars to ensure optimizations work correctly and don't break avatar functionality.