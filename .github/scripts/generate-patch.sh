#!/bin/sh
# POSIX-compliant script for generating Avatar Optimizer patches locally
# Works on macOS, Linux, and Git Bash on Windows

set -e

# Colors for output (if supported)
if [ -t 1 ]; then
    RED='\033[0;31m'
    GREEN='\033[0;32m'
    YELLOW='\033[1;33m'
    NC='\033[0m'
else
    RED=''
    GREEN=''
    YELLOW=''
    NC=''
fi

print_usage() {
    cat << EOF
Usage: $0 [OPTIONS]

Generate a patch file for Avatar Optimizer testing.

OPTIONS:
    -t, --target TARGET       Target commit, commit range (base...head), or 'working' for current changes
    -v, --base-version VER    Base version (e.g., 1.9.0-rc.10)
    -p, --base-patch HASH     Base patch commit hash (optional, for continuous patches)
    -m, --message MSG         Custom message to include in commit
    -o, --output FILE         Output patch file (default: generated-<timestamp>.patch)
    -h, --help                Show this help message

EXAMPLES:
    # Generate patch for current working directory changes
    $0 -t working -v 1.9.0-rc.10 -m "Testing fix for issue #123"
    
    # Generate patch for a specific commit
    $0 -t abc1234 -v 1.9.0-rc.10
    
    # Generate patch for a commit range
    $0 -t master...feature-branch -v 1.9.0-rc.10
    
    # Generate continuous patch
    $0 -t def5678 -v 1.9.0-rc.10 -p abc1234

EOF
}

# Parse command line arguments
TARGET=""
BASE_VERSION=""
BASE_PATCH=""
CUSTOM_MESSAGE=""
OUTPUT_FILE=""

while [ $# -gt 0 ]; do
    case "$1" in
        -t|--target)
            TARGET="$2"
            shift 2
            ;;
        -v|--base-version)
            BASE_VERSION="$2"
            shift 2
            ;;
        -p|--base-patch)
            BASE_PATCH="$2"
            shift 2
            ;;
        -m|--message)
            CUSTOM_MESSAGE="$2"
            shift 2
            ;;
        -o|--output)
            OUTPUT_FILE="$2"
            shift 2
            ;;
        -h|--help)
            print_usage
            exit 0
            ;;
        *)
            echo "${RED}Error: Unknown option $1${NC}" >&2
            print_usage
            exit 1
            ;;
    esac
done

# Validate required arguments
if [ -z "$TARGET" ]; then
    echo "${RED}Error: Target is required${NC}" >&2
    print_usage
    exit 1
fi

if [ -z "$BASE_VERSION" ]; then
    echo "${RED}Error: Base version is required${NC}" >&2
    print_usage
    exit 1
fi

# Set default output file
if [ -z "$OUTPUT_FILE" ]; then
    OUTPUT_FILE="generated-$(date +%s).patch"
fi

echo "${GREEN}Avatar Optimizer Patch Generator${NC}"
echo "================================"
echo "Target: $TARGET"
echo "Base Version: $BASE_VERSION"
[ -n "$BASE_PATCH" ] && echo "Base Patch: $BASE_PATCH"
echo "Output: $OUTPUT_FILE"
echo ""

# Check if we're in a git repository
if ! git rev-parse --git-dir > /dev/null 2>&1; then
    echo "${RED}Error: Not in a git repository${NC}" >&2
    exit 1
fi

# Determine base commit
if [ -n "$BASE_PATCH" ]; then
    BASE_COMMIT="$BASE_PATCH"
    echo "Using base patch commit: $BASE_COMMIT"
else
    # Try to find version tag
    if git rev-parse "v${BASE_VERSION}" > /dev/null 2>&1; then
        BASE_COMMIT=$(git rev-list -n 1 "v${BASE_VERSION}")
        echo "Found version tag, using commit: $BASE_COMMIT"
    else
        echo "${YELLOW}Warning: Version tag v${BASE_VERSION} not found, using current HEAD as base${NC}" >&2
        BASE_COMMIT="HEAD"
    fi
fi

# Create temporary branch for patch generation
TEMP_BRANCH="temp-patch-gen-$(date +%s)"
echo "Creating temporary branch: $TEMP_BRANCH"

# Save current branch
ORIGINAL_BRANCH=$(git rev-parse --abbrev-ref HEAD)
ORIGINAL_COMMIT=$(git rev-parse HEAD)

# Cleanup function
cleanup() {
    echo ""
    echo "Cleaning up..."
    git checkout "$ORIGINAL_BRANCH" 2>/dev/null || git checkout "$ORIGINAL_COMMIT" 2>/dev/null || true
    git branch -D "$TEMP_BRANCH" 2>/dev/null || true
}

trap cleanup EXIT INT TERM

# Create and checkout temporary branch
git checkout -b "$TEMP_BRANCH" "$BASE_COMMIT"

# Handle different target types
if [ "$TARGET" = "working" ]; then
    echo "Applying working directory changes..."
    
    # Checkout the original commit to get working changes
    git checkout "$ORIGINAL_COMMIT" -- . 2>/dev/null || true
    
    # Add all changes
    git add -A
    
    if ! git diff --cached --quiet; then
        COMMIT_MSG="patch: Working directory changes"
        [ -n "$CUSTOM_MESSAGE" ] && COMMIT_MSG="$COMMIT_MSG\n\n$CUSTOM_MESSAGE"
    else
        echo "${YELLOW}Warning: No changes in working directory${NC}"
        exit 1
    fi
    
elif echo "$TARGET" | grep -q '\.\.\.'; then
    # It's a range
    echo "Applying commit range..."
    
    BASE_REF=$(echo "$TARGET" | cut -d'.' -f1)
    HEAD_REF=$(echo "$TARGET" | cut -d'.' -f4-)
    
    # Get commits in range
    git cherry-pick "${BASE_REF}..${HEAD_REF}" || {
        echo "${RED}Error: Failed to apply commit range${NC}" >&2
        echo "Please resolve conflicts manually"
        exit 1
    }
    
    COMMIT_MSG="patch: Applied commit range"
    [ -n "$CUSTOM_MESSAGE" ] && COMMIT_MSG="$COMMIT_MSG\n\n$CUSTOM_MESSAGE"
    
else
    # It's a single commit
    echo "Applying single commit..."
    
    git cherry-pick "$TARGET" || {
        echo "${RED}Error: Failed to cherry-pick commit${NC}" >&2
        echo "Please resolve conflicts manually"
        exit 1
    }
    
    # Get original commit message
    ORIGINAL_MSG=$(git log -1 --format=%B "$TARGET")
    COMMIT_MSG="patch: $ORIGINAL_MSG"
    [ -n "$CUSTOM_MESSAGE" ] && COMMIT_MSG="$COMMIT_MSG\n\n$CUSTOM_MESSAGE"
fi

# Remove documentation and Unity excluded files
echo "Removing documentation and excluded files..."
git rm -rf .docs CHANGELOG*.md 2>/dev/null || true
find . -path ./.git -prune -o -name ".*" -not -name ".gitignore" -type f -exec git rm -f {} \; 2>/dev/null || true
find . -name "*~" -type d -exec git rm -rf {} + 2>/dev/null || true  
find . -name "*~" -type f -exec git rm -f {} \; 2>/dev/null || true

# Commit or amend with proper metadata
if git diff --cached --quiet; then
    # Nothing to commit after removals, amend previous commit
    AMEND=true
else
    # Stage the removals
    git add -A
    AMEND=true
fi

# Build full commit message with metadata
FULL_MSG="${COMMIT_MSG}

Base-Version: ${BASE_VERSION}
Base-Commit: ${BASE_COMMIT}"

[ -n "$BASE_PATCH" ] && FULL_MSG="$FULL_MSG
Base-Patch: ${BASE_PATCH}"

if [ "$TARGET" != "working" ]; then
    if echo "$TARGET" | grep -q '\.\.\.'; then
        FULL_MSG="$FULL_MSG
Commit-Range: ${TARGET}"
        
        # Add co-authors
        BASE_REF=$(echo "$TARGET" | cut -d'.' -f1)
        HEAD_REF=$(echo "$TARGET" | cut -d'.' -f4-)
        git log --format="%an <%ae>" "${BASE_REF}..${HEAD_REF}" | sort -u | while read -r author; do
            FULL_MSG="$FULL_MSG
Co-authored-by: ${author}"
        done
    else
        AUTHOR=$(git log --format="%an <%ae>" -1 "$TARGET")
        FULL_MSG="$FULL_MSG
Co-authored-by: ${AUTHOR}"
    fi
fi

# Create or amend commit
if [ "$AMEND" = true ]; then
    printf "%s" "$FULL_MSG" | git commit --amend -F -
else
    printf "%s" "$FULL_MSG" | git commit -F -
fi

# Generate patch file
echo "Generating patch file..."
git format-patch -1 HEAD --stdout > "$OUTPUT_FILE"

# Get patch commit hash
PATCH_COMMIT=$(git rev-parse HEAD)

echo ""
echo "${GREEN}Patch generated successfully!${NC}"
echo "Patch commit: $PATCH_COMMIT"
echo "Output file: $OUTPUT_FILE"
echo ""
echo "To apply this patch in Unity:"
echo "  1. Copy the patch file to your project"
echo "  2. Open Tools > Avatar Optimizer > Apply Patch"
echo "  3. Select the patch file and click Apply"
