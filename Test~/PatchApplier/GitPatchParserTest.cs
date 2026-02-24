using System.Linq;
using Anatawa12.AvatarOptimizer.PatchApplier;
using NUnit.Framework;

namespace Anatawa12.AvatarOptimizer.Test.PatchApplier
{
    public class GitPatchParserTest
    {
        [Test]
        public void ParsePatch_SimpleFile()
        {
            var patchContent = @"From abc1234567890def1234567890abc1234567890 Mon Sep 17 00:00:00 2001
From: Test Author <test@example.com>
Date: Mon, 1 Jan 2024 12:00:00 +0000
Subject: [PATCH] Test patch

Base-Version: 1.9.0-rc.10
Base-Commit: def1234567890abc1234567890def1234567890

---
 test.cs | 2 +-
 1 file changed, 1 insertion(+), 1 deletion(-)

diff --git a/test.cs b/test.cs
index abc1234..def5678 100644
--- a/test.cs
+++ b/test.cs
@@ -1,3 +1,3 @@
 line1
-line2
+line2 modified
 line3
";
            
            var (metadata, patches) = GitPatchParser.Parse(patchContent);
            
            Assert.AreEqual("abc1234567890def1234567890abc1234567890", metadata.CommitHash);
            Assert.AreEqual("1.9.0-rc.10", metadata.BaseVersion);
            Assert.AreEqual("def1234567890abc1234567890def1234567890", metadata.BaseCommit);
            
            Assert.AreEqual(1, patches.Count);
            Assert.AreEqual("test.cs", patches[0].GetPath());
            Assert.AreEqual("abc1234", patches[0].OldSha);
            Assert.AreEqual("def5678", patches[0].NewSha);
            Assert.IsFalse(patches[0].IsNewFile);
            Assert.IsFalse(patches[0].IsDeletedFile);
            
            Assert.AreEqual(1, patches[0].Hunks.Count);
            Assert.AreEqual(1, patches[0].Hunks[0].OldStart);
            Assert.AreEqual(3, patches[0].Hunks[0].OldCount);
            Assert.AreEqual(1, patches[0].Hunks[0].NewStart);
            Assert.AreEqual(3, patches[0].Hunks[0].NewCount);
        }

        [Test]
        public void ParsePatch_NewFile()
        {
            var patchContent = @"From abc123 Mon Sep 17 00:00:00 2001
From: Test <test@example.com>
Date: Mon, 1 Jan 2024 12:00:00 +0000
Subject: [PATCH] Add new file

Base-Version: 1.9.0
Base-Commit: base123
---
 newfile.cs | 1 +
 1 file changed, 1 insertion(+)

diff --git a/newfile.cs b/newfile.cs
new file mode 100644
index 0000000..abc1234
--- /dev/null
+++ b/newfile.cs
@@ -0,0 +1 @@
+new content
";

            var (metadata, patches) = GitPatchParser.Parse(patchContent);
            
            Assert.AreEqual(1, patches.Count);
            Assert.AreEqual("newfile.cs", patches[0].GetPath());
            Assert.IsTrue(patches[0].IsNewFile);
            Assert.IsFalse(patches[0].IsDeletedFile);
        }

        [Test]
        public void ParsePatch_DeletedFile()
        {
            var patchContent = @"From abc123 Mon Sep 17 00:00:00 2001
From: Test <test@example.com>
Date: Mon, 1 Jan 2024 12:00:00 +0000
Subject: [PATCH] Delete file

Base-Version: 1.9.0
Base-Commit: base123
---
 oldfile.cs | 1 -
 1 file changed, 1 deletion(-)

diff --git a/oldfile.cs b/oldfile.cs
deleted file mode 100644
index abc1234..0000000
--- a/oldfile.cs
+++ /dev/null
@@ -1 +0,0 @@
-old content
";

            var (metadata, patches) = GitPatchParser.Parse(patchContent);
            
            Assert.AreEqual(1, patches.Count);
            Assert.AreEqual("oldfile.cs", patches[0].GetPath());
            Assert.IsFalse(patches[0].IsNewFile);
            Assert.IsTrue(patches[0].IsDeletedFile);
        }

        [Test]
        public void ParsePatch_WithPullRequest()
        {
            var patchContent = @"From abc123 Mon Sep 17 00:00:00 2001
From: Test <test@example.com>
Date: Mon, 1 Jan 2024 12:00:00 +0000
Subject: [PATCH] Test

Base-Version: 1.9.0
Base-Commit: base123
Pull-Request: https://github.com/test/pr/1
Commit-Range: master...feature
---
 test.cs | 0
 1 file changed, 0 insertions(+), 0 deletions(-)

diff --git a/test.cs b/test.cs
index abc1234..abc1234 100644
";

            var (metadata, patches) = GitPatchParser.Parse(patchContent);
            
            Assert.AreEqual("https://github.com/test/pr/1", metadata.PullRequest);
            Assert.AreEqual("master...feature", metadata.CommitRange);
        }

        [Test]
        public void ParsePatch_MultipleFiles()
        {
            var patchContent = @"From abc123 Mon Sep 17 00:00:00 2001
From: Test <test@example.com>
Date: Mon, 1 Jan 2024 12:00:00 +0000
Subject: [PATCH] Multi file

Base-Version: 1.9.0
Base-Commit: base123
---
 file1.cs | 1 +
 file2.cs | 1 +
 2 files changed, 2 insertions(+)

diff --git a/file1.cs b/file1.cs
index abc1234..def5678 100644
--- a/file1.cs
+++ b/file1.cs
@@ -1 +1,2 @@
 line1
+line2
diff --git a/file2.cs b/file2.cs
index 111222..333444 100644
--- a/file2.cs
+++ b/file2.cs
@@ -1 +1,2 @@
 content1
+content2
";

            var (metadata, patches) = GitPatchParser.Parse(patchContent);
            
            Assert.AreEqual(2, patches.Count);
            Assert.AreEqual("file1.cs", patches[0].GetPath());
            Assert.AreEqual("file2.cs", patches[1].GetPath());
        }

        [Test]
        public void ParsePatch_IncompleteHunk_ReturnsNull()
        {
            // Test patch with incomplete hunk (header says 3 lines, but only 2 provided)
            var patchContent = @"From abc123 Mon Sep 17 00:00:00 2001
From: Test <test@example.com>
Date: Mon, 1 Jan 2024 12:00:00 +0000
Subject: [PATCH] Incomplete hunk

Base-Version: 1.9.0
Base-Commit: base123
---
 test.cs | 1 +
 1 file changed, 1 insertion(+)

diff --git a/test.cs b/test.cs
index abc1234..def5678 100644
--- a/test.cs
+++ b/test.cs
@@ -1,3 +1,3 @@
 line1
-line2
";

            var (metadata, patches) = GitPatchParser.Parse(patchContent);
            
            // The patch should be parsed but the hunk should be rejected due to incomplete lines
            Assert.AreEqual(1, patches.Count);
            Assert.AreEqual(0, patches[0].Hunks.Count); // Hunk should be rejected
        }
    }
}
