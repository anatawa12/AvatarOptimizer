using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace Anatawa12.AvatarOptimizer.PatchApplier
{
    /// <summary>
    /// Represents a single file change in a Git patch
    /// </summary>
    internal class GitFilePatch
    {
        public string? OldPath { get; set; }
        public string? NewPath { get; set; }
        public string? OldSha { get; set; }
        public string? NewSha { get; set; }
        public bool IsNewFile { get; set; }
        public bool IsDeletedFile { get; set; }
        public List<Hunk> Hunks { get; } = new List<Hunk>();

        public string GetPath()
        {
            return NewPath ?? OldPath ?? throw new InvalidOperationException("Both paths are null");
        }
    }

    /// <summary>
    /// Represents a hunk (a section of changes) in a patch
    /// </summary>
    internal class Hunk
    {
        public int OldStart { get; set; }
        public int OldCount { get; set; }
        public int NewStart { get; set; }
        public int NewCount { get; set; }
        public List<string> Lines { get; } = new List<string>();
    }

    /// <summary>
    /// Represents metadata about a patch
    /// </summary>
    internal class PatchMetadata
    {
        public string? CommitHash { get; set; }
        public string? BaseVersion { get; set; }
        public string? BaseCommit { get; set; }
        public string? BasePatch { get; set; }
        public string? PullRequest { get; set; }
        public string? CommitRange { get; set; }
    }

    /// <summary>
    /// Parses Git .patch files
    /// </summary>
    internal class GitPatchParser
    {
        /// <summary>
        /// Parses a Git patch file
        /// </summary>
        public static (PatchMetadata metadata, List<GitFilePatch> patches) Parse(string patchContent)
        {
            var lines = patchContent.Replace("\r\n", "\n").Split('\n');
            var metadata = new PatchMetadata();
            var patches = new List<GitFilePatch>();

            int i = 0;

            // Parse commit message and metadata
            while (i < lines.Length)
            {
                var line = lines[i];

                // Look for commit hash in "From <hash>" line
                if (line.StartsWith("From ") && line.Length > 45)
                {
                    var parts = line.Split(new[] { ' ' }, 2);
                    if (parts.Length == 2)
                        metadata.CommitHash = parts[1].Split(' ')[0];
                }
                // Look for metadata in commit message footer
                else if (line.StartsWith("Base-Version: "))
                    metadata.BaseVersion = line.Substring("Base-Version: ".Length).Trim();
                else if (line.StartsWith("Base-Commit: "))
                    metadata.BaseCommit = line.Substring("Base-Commit: ".Length).Trim();
                else if (line.StartsWith("Base-Patch: "))
                    metadata.BasePatch = line.Substring("Base-Patch: ".Length).Trim();
                else if (line.StartsWith("Pull-Request: "))
                    metadata.PullRequest = line.Substring("Pull-Request: ".Length).Trim();
                else if (line.StartsWith("Commit-Range: "))
                    metadata.CommitRange = line.Substring("Commit-Range: ".Length).Trim();
                // Start of file diff
                else if (line.StartsWith("diff --git "))
                    break;

                i++;
            }

            // Parse file patches
            while (i < lines.Length)
            {
                if (lines[i].StartsWith("diff --git "))
                {
                    var filePatch = ParseFilePatch(lines, ref i);
                    if (filePatch != null)
                        patches.Add(filePatch);
                }
                else
                {
                    i++;
                }
            }

            return (metadata, patches);
        }

        private static GitFilePatch? ParseFilePatch(string[] lines, ref int i)
        {
            var patch = new GitFilePatch();

            // Parse diff --git a/... b/...
            if (i < lines.Length && lines[i].StartsWith("diff --git "))
            {
                var parts = lines[i].Substring("diff --git ".Length).Split(' ');
                if (parts.Length >= 2)
                {
                    patch.OldPath = parts[0].StartsWith("a/") ? parts[0].Substring(2) : parts[0];
                    patch.NewPath = parts[1].StartsWith("b/") ? parts[1].Substring(2) : parts[1];
                }
                i++;
            }

            // Parse file metadata
            while (i < lines.Length)
            {
                var line = lines[i];

                if (line.StartsWith("new file mode "))
                {
                    patch.IsNewFile = true;
                    i++;
                }
                else if (line.StartsWith("deleted file mode "))
                {
                    patch.IsDeletedFile = true;
                    i++;
                }
                else if (line.StartsWith("index "))
                {
                    // Parse index line: "index <oldsha>..<newsha>"
                    var indexPart = line.Substring("index ".Length);
                    var parts = indexPart.Split(new[] { "..", " " }, StringSplitOptions.RemoveEmptyEntries);
                    if (parts.Length >= 2)
                    {
                        patch.OldSha = parts[0];
                        patch.NewSha = parts[1];
                    }
                    i++;
                }
                else if (line.StartsWith("--- "))
                {
                    i++;
                }
                else if (line.StartsWith("+++ "))
                {
                    i++;
                }
                else if (line.StartsWith("@@ "))
                {
                    // Start of hunks
                    break;
                }
                else if (line.StartsWith("diff --git "))
                {
                    // Next file
                    return patch;
                }
                else
                {
                    i++;
                }
            }

            // Parse hunks
            while (i < lines.Length && lines[i].StartsWith("@@ "))
            {
                var hunk = ParseHunk(lines, ref i);
                if (hunk != null)
                    patch.Hunks.Add(hunk);
            }

            return patch;
        }

        private static Hunk? ParseHunk(string[] lines, ref int i)
        {
            var hunk = new Hunk();

            // Parse @@ -oldStart,oldCount +newStart,newCount @@
            if (i < lines.Length && lines[i].StartsWith("@@ "))
            {
                var hunkHeader = lines[i];
                var match = System.Text.RegularExpressions.Regex.Match(
                    hunkHeader, @"@@ -(\d+)(?:,(\d+))? \+(\d+)(?:,(\d+))? @@");
                
                if (match.Success)
                {
                    hunk.OldStart = int.Parse(match.Groups[1].Value);
                    hunk.OldCount = match.Groups[2].Success ? int.Parse(match.Groups[2].Value) : 1;
                    hunk.NewStart = int.Parse(match.Groups[3].Value);
                    hunk.NewCount = match.Groups[4].Success ? int.Parse(match.Groups[4].Value) : 1;
                }
                i++;
            }

            // Parse hunk lines
            while (i < lines.Length)
            {
                var line = lines[i];
                
                if (line.StartsWith("@@") || line.StartsWith("diff --git "))
                {
                    // Next hunk or file
                    break;
                }
                else if (line.StartsWith(" ") || line.StartsWith("+") || line.StartsWith("-"))
                {
                    hunk.Lines.Add(line);
                    i++;
                }
                else if (line.StartsWith("\\"))
                {
                    // Skip "\ No newline at end of file" markers
                    i++;
                }
                else
                {
                    i++;
                }
            }

            return hunk;
        }
    }

    /// <summary>
    /// Utilities for computing Git blob SHA1 hashes
    /// </summary>
    internal static class GitBlobHash
    {
        /// <summary>
        /// Computes the Git blob SHA1 hash for file content
        /// </summary>
        public static string ComputeSha1(string content)
        {
            // Git blob format: "blob <size>\0<content>"
            var contentBytes = Encoding.UTF8.GetBytes(content);
            var header = Encoding.UTF8.GetBytes($"blob {contentBytes.Length}\0");
            var blob = new byte[header.Length + contentBytes.Length];
            Buffer.BlockCopy(header, 0, blob, 0, header.Length);
            Buffer.BlockCopy(contentBytes, 0, blob, header.Length, contentBytes.Length);

            using (var sha1 = SHA1.Create())
            {
                var hash = sha1.ComputeHash(blob);
                return BitConverter.ToString(hash).Replace("-", "").ToLowerInvariant();
            }
        }

        /// <summary>
        /// Normalizes line endings to LF
        /// </summary>
        public static string NormalizeLineEndings(string content)
        {
            return content.Replace("\r\n", "\n").Replace("\r", "\n");
        }
    }
}
