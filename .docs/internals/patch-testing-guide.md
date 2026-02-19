# Manual Testing Guide for Patch System

This guide will help you manually test the patch application system.

## Prerequisites

1. Unity 2022.3+ with VRChat SDK installed
2. Avatar Optimizer package installed in a test project
3. Access to this repository for generating test patches

## Test Scenarios

### Scenario 1: Apply a Simple Patch

**Goal**: Verify basic patch application works

1. **Create a test patch**:
   ```bash
   cd /path/to/AvatarOptimizer
   
   # Make a simple change to a test file
   echo "// Test comment" >> Editor/PatchApplier/VersionInfo.cs
   
   # Generate patch
   ./.github/scripts/generate-patch.sh \
     -t working \
     -v 1.9.0-rc.10 \
     -m "Test patch for manual testing" \
     -o test-patch.patch
   
   # Revert the change
   git checkout Editor/PatchApplier/VersionInfo.cs
   ```

2. **Apply the patch in Unity**:
   - Open your test Unity project
   - Go to `Tools > Avatar Optimizer > Apply Patch`
   - Click "Browse" and select `test-patch.patch`
   - Click "Apply Patch"

3. **Verify**:
   - Check that version shows `1.9.0-rc.10+patch.XXXXXXXXX`
   - Open `Editor/PatchApplier/VersionInfo.cs` and verify the comment was added
   - Check that `.patches.txt` exists in the package directory
   - Verify patch info shows in the Patch Applier window

### Scenario 2: Version Display Integration

**Goal**: Verify version display in various places

1. After applying a patch from Scenario 1:
   - Open `Tools > Avatar Optimizer > Bug Report Helper`
   - Check that the header shows patched version
   - Generate a bug report and verify patch hash is included

2. Check NDMF Console (if available):
   - Verify the version display includes patch information

### Scenario 3: Reject Invalid Patches

**Goal**: Verify patch validation works

1. **Test SHA mismatch**:
   - Modify a file manually: `echo "// Manual change" >> Editor/PatchApplier/VersionInfo.cs`
   - Try to apply a patch that expects the original file
   - Should fail with "File SHA mismatch" error

2. **Test non-continuous patch**:
   - After applying one patch, try to apply another patch that's not based on it
   - Should fail with "it must be based on the currently applied patch" error

### Scenario 4: Continuous Patches

**Goal**: Verify continuous patch application works

1. **Apply first patch**:
   - Create and apply a patch (see Scenario 1)
   - Note the commit hash from the applied patch

2. **Create a second patch based on the first**:
   ```bash
   # Make another change
   echo "// Second test comment" >> Editor/PatchApplier/PatchInfo.cs
   
   # Generate continuous patch (use the first patch's commit hash)
   ./.github/scripts/generate-patch.sh \
     -t working \
     -v 1.9.0-rc.10 \
     -p <first-patch-commit-hash> \
     -m "Second test patch" \
     -o test-patch-2.patch
   
   git checkout Editor/PatchApplier/PatchInfo.cs
   ```

3. **Apply the second patch**:
   - Should succeed
   - Verify both patches are listed in the Patch Applier window
   - Version should still show the latest patch hash

### Scenario 5: GitHub Workflow Testing

**Goal**: Verify patch generation workflow works

1. **Manual workflow**:
   - Go to Actions > Generate Patch
   - Run workflow with:
     - Target: A recent commit hash
     - Base version: 1.9.0-rc.10
   - Wait for completion
   - Download the artifact
   - Try applying it in Unity

2. **PR workflow** (requires open PR):
   - Create a test PR with a simple change
   - Wait for the auto-generate workflow to run
   - Check that a comment appears with patch links
   - Download and apply the patch

### Scenario 6: Local Script Testing

**Goal**: Verify local patch generation script works

1. **Test with working directory**:
   ```bash
   echo "// Test" >> Editor/PatchApplier/VersionInfo.cs
   ./.github/scripts/generate-patch.sh -t working -v 1.9.0-rc.10 -o test.patch
   git checkout Editor/PatchApplier/VersionInfo.cs
   # Verify test.patch was created and contains the change
   ```

2. **Test with commit**:
   ```bash
   COMMIT=$(git rev-parse HEAD)
   ./.github/scripts/generate-patch.sh -t $COMMIT -v 1.9.0-rc.10 -o commit.patch
   # Verify commit.patch was created
   ```

3. **Test with range**:
   ```bash
   ./.github/scripts/generate-patch.sh -t HEAD~5...HEAD -v 1.9.0-rc.10 -o range.patch
   # Verify range.patch was created with all commits
   ```

### Scenario 7: Error Handling

**Goal**: Verify proper error messages

1. **Missing base version tag**:
   - Try to apply a patch with a non-existent base version
   - Should show appropriate error

2. **Corrupted patch file**:
   - Create a text file with random content, name it .patch
   - Try to apply it
   - Should fail gracefully with error message

3. **Network error** (for URL-based application):
   - Enter an invalid URL
   - Should show download error

## Expected Results

All scenarios should:
- ✅ Complete without crashes
- ✅ Show clear error messages when something fails
- ✅ Maintain package integrity (no broken state)
- ✅ Update version display correctly
- ✅ Record patches in `.patches.txt`
- ✅ Include patch info in bug reports

## Cleanup After Testing

To reset the package after testing:

1. Delete `.patches.txt` file
2. Reinstall the package or revert all patched files
3. Restart Unity Editor

## Reporting Issues

If you find any issues during testing:

1. Note the exact steps to reproduce
2. Collect error messages from Unity Console
3. Check `.patches.txt` contents
4. Generate a bug report (it will include patch info)
5. Report on GitHub with all collected information

## Advanced Testing

For thorough testing:

1. Test with different Unity versions (2022.3, 2023.x)
2. Test on different platforms (Windows, macOS, Linux)
3. Test with various avatar types and configurations
4. Test patch application during play mode (should be disabled)
5. Test with corrupted `.patches.txt` file
6. Test concurrent patch applications (should not be possible)

## Performance Testing

1. Apply multiple continuous patches (5-10)
2. Measure time for each application
3. Check Unity Editor responsiveness
4. Verify memory usage is reasonable

## Security Testing

1. Try to apply patches with malicious file paths (should be rejected)
2. Verify SHA validation cannot be bypassed
3. Test with patches containing very large files
4. Verify patch metadata cannot be injected with malicious content
