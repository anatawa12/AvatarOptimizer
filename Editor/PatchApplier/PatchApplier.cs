using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Anatawa12.AvatarOptimizer.PatchApplier
{
    /// <summary>
    /// Result of a patch application attempt
    /// </summary>
    internal class PatchApplicationResult
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public List<string> Warnings { get; } = new List<string>();
        public PatchInfo? AppliedPatch { get; set; }
    }

    /// <summary>
    /// Applies Git patches to the package
    /// </summary>
    internal class PatchApplier
    {
        private const string PackageRoot = "Packages/com.anatawa12.avatar-optimizer";

        /// <summary>
        /// Applies a patch from a .patch file
        /// </summary>
        public static PatchApplicationResult ApplyPatch(string patchContent)
        {
            var result = new PatchApplicationResult();

            try
            {
                // Parse the patch
                var (metadata, filePatches) = GitPatchParser.Parse(patchContent);

                // Validate metadata
                if (string.IsNullOrEmpty(metadata.CommitHash))
                {
                    result.ErrorMessage = "Patch does not contain commit hash";
                    return result;
                }

                if (string.IsNullOrEmpty(metadata.BaseVersion))
                {
                    result.ErrorMessage = "Patch does not contain base version";
                    return result;
                }

                if (string.IsNullOrEmpty(metadata.BaseCommit))
                {
                    result.ErrorMessage = "Patch does not contain base commit";
                    return result;
                }

                // Load current patch registry
                var registry = PatchRegistry.Load();

                // Create patch info
                var patchInfo = new PatchInfo(metadata.CommitHash, metadata.BaseVersion, metadata.BaseCommit)
                {
                    BasePatch = metadata.BasePatch,
                    PullRequest = metadata.PullRequest
                };

                // Check if we can apply this patch
                if (!registry.CanApplyPatch(patchInfo))
                {
                    result.ErrorMessage = "Cannot apply patch: it must be based on the currently applied patch or the current version";
                    return result;
                }

                // Verify current file states before applying
                var verificationsNeeded = new List<(string path, string expectedSha)>();
                foreach (var filePatch in filePatches)
                {
                    if (!filePatch.IsNewFile)
                    {
                        if (string.IsNullOrEmpty(filePatch.OldSha))
                        {
                            result.ErrorMessage = $"Missing SHA for file {filePatch.GetPath()}";
                            return result;
                        }
                        verificationsNeeded.Add((filePatch.GetPath(), filePatch.OldSha));
                    }
                }

                // Verify all files
                foreach (var (path, expectedSha) in verificationsNeeded)
                {
                    var fullPath = Path.Combine(PackageRoot, path);
                    if (!File.Exists(fullPath))
                    {
                        result.ErrorMessage = $"File not found: {path}";
                        return result;
                    }

                    var content = File.ReadAllText(fullPath, Encoding.UTF8);
                    var normalizedContent = GitBlobHash.NormalizeLineEndings(content);
                    var actualSha = GitBlobHash.ComputeSha1(normalizedContent);

                    if (!actualSha.StartsWith(expectedSha) && !expectedSha.StartsWith(actualSha))
                    {
                        result.ErrorMessage = $"File SHA mismatch for {path}: expected {expectedSha}, got {actualSha}";
                        return result;
                    }
                }

                // Apply all patches
                var appliedFiles = new Dictionary<string, string>();
                
                foreach (var filePatch in filePatches)
                {
                    var applyResult = ApplyFilePatch(filePatch, result);
                    if (!applyResult.success)
                    {
                        result.ErrorMessage = applyResult.error;
                        return result;
                    }
                    
                    appliedFiles[filePatch.GetPath()] = applyResult.newContent;
                }

                // Verify new file states
                foreach (var filePatch in filePatches)
                {
                    if (!filePatch.IsDeletedFile && !string.IsNullOrEmpty(filePatch.NewSha))
                    {
                        var path = filePatch.GetPath();
                        if (!appliedFiles.ContainsKey(path))
                            continue;

                        var newContent = appliedFiles[path];
                        var actualSha = GitBlobHash.ComputeSha1(newContent);

                        if (!actualSha.StartsWith(filePatch.NewSha) && !filePatch.NewSha.StartsWith(actualSha))
                        {
                            result.ErrorMessage = $"Applied file SHA mismatch for {path}: expected {filePatch.NewSha}, got {actualSha}";
                            return result;
                        }
                    }
                }

                // Write all files at once (atomic operation as much as possible)
                foreach (var (path, content) in appliedFiles)
                {
                    var fullPath = Path.Combine(PackageRoot, path);
                    var directory = Path.GetDirectoryName(fullPath);
                    if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                        Directory.CreateDirectory(directory);
                    
                    File.WriteAllText(fullPath, content, Encoding.UTF8);
                }

                // Delete files marked for deletion
                foreach (var filePatch in filePatches)
                {
                    if (filePatch.IsDeletedFile)
                    {
                        var fullPath = Path.Combine(PackageRoot, filePatch.GetPath());
                        if (File.Exists(fullPath))
                            File.Delete(fullPath);
                    }
                }

                // Update registry
                registry.AddPatch(patchInfo);
                registry.Save();

                result.Success = true;
                result.AppliedPatch = patchInfo;
                return result;
            }
            catch (Exception ex)
            {
                result.ErrorMessage = $"Exception during patch application: {ex.Message}";
                return result;
            }
        }

        private static (bool success, string error, string newContent) ApplyFilePatch(GitFilePatch filePatch, PatchApplicationResult result)
        {
            var path = filePatch.GetPath();
            var fullPath = Path.Combine(PackageRoot, path);

            // Handle new file
            if (filePatch.IsNewFile)
            {
                if (File.Exists(fullPath))
                    return (false, $"Cannot create new file {path}: file already exists", "");

                var newContent = ApplyHunks(new string[0], filePatch.Hunks);
                if (newContent == null)
                    return (false, $"Failed to apply hunks for new file {path}", "");

                return (true, "", newContent);
            }

            // Handle deleted file
            if (filePatch.IsDeletedFile)
            {
                if (!File.Exists(fullPath))
                    return (false, $"Cannot delete file {path}: file does not exist", "");

                return (true, "", "");
            }

            // Handle modified file
            if (!File.Exists(fullPath))
                return (false, $"File not found: {path}", "");

            var content = File.ReadAllText(fullPath, Encoding.UTF8);
            var normalizedContent = GitBlobHash.NormalizeLineEndings(content);
            var lines = normalizedContent.Split('\n');

            var modifiedContent = ApplyHunks(lines, filePatch.Hunks);
            if (modifiedContent == null)
                return (false, $"Failed to apply hunks for file {path}", "");

            return (true, "", modifiedContent);
        }

        private static string? ApplyHunks(string[] originalLines, List<Hunk> hunks)
        {
            var result = new List<string>();
            int originalIndex = 0;
            bool resultMissingNewlineAtEof = false;

            foreach (var hunk in hunks)
            {
                // Copy lines before the hunk
                while (originalIndex < hunk.OldStart - 1 && originalIndex < originalLines.Length)
                {
                    result.Add(originalLines[originalIndex]);
                    originalIndex++;
                }

                // Apply hunk
                int hunkOriginalIndex = 0;
                foreach (var line in hunk.Lines)
                {
                    if (line.Length == 0)
                        continue;

                    var marker = line[0];
                    var content = line.Length > 1 ? line.Substring(1) : "";

                    if (marker == ' ')
                    {
                        // Context line - verify it matches
                        if (originalIndex >= originalLines.Length)
                            return null; // Patch doesn't match

                        if (originalLines[originalIndex] != content)
                        {
                            // Allow minor whitespace differences
                            if (originalLines[originalIndex].Trim() != content.Trim())
                                return null;
                        }

                        result.Add(originalLines[originalIndex]);
                        originalIndex++;
                        hunkOriginalIndex++;
                    }
                    else if (marker == '-')
                    {
                        // Removed line - verify it matches and skip
                        if (originalIndex >= originalLines.Length)
                            return null;

                        if (originalLines[originalIndex] != content)
                        {
                            if (originalLines[originalIndex].Trim() != content.Trim())
                                return null;
                        }

                        originalIndex++;
                        hunkOriginalIndex++;
                    }
                    else if (marker == '+')
                    {
                        // Added line
                        result.Add(content);
                    }
                }
                
                // Track if this hunk indicates missing newline at EOF
                resultMissingNewlineAtEof = hunk.NewMissingNewlineAtEof;
            }

            // Copy remaining lines
            while (originalIndex < originalLines.Length)
            {
                result.Add(originalLines[originalIndex]);
                originalIndex++;
            }

            // Join with newlines, but respect missing newline at EOF
            if (resultMissingNewlineAtEof && result.Count > 0)
            {
                // Last line should not have a trailing newline
                var joined = string.Join("\n", result);
                return joined;
            }
            else
            {
                return string.Join("\n", result);
            }
        }
    }
}
