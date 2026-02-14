using Anatawa12.AvatarOptimizer.PatchApplier;
using NUnit.Framework;

namespace Anatawa12.AvatarOptimizer.Test.PatchApplier
{
    public class GitBlobHashTest
    {
        [Test]
        public void ComputeSha1_SimpleText()
        {
            var content = "test content\n";
            var sha = GitBlobHash.ComputeSha1(content);
            
            // Expected Git blob hash for "test content\n"
            Assert.AreEqual("d670460b4b4aece5915caf5c68d12f560a9fe3e4", sha);
        }

        [Test]
        public void ComputeSha1_EmptyString()
        {
            var content = "";
            var sha = GitBlobHash.ComputeSha1(content);
            
            // Expected Git blob hash for empty string
            Assert.AreEqual("e69de29bb2d1d6434b8b29ae775ad8c2e48c5391", sha);
        }

        [Test]
        public void NormalizeLineEndings_CRLF()
        {
            var content = "line1\r\nline2\r\nline3";
            var normalized = GitBlobHash.NormalizeLineEndings(content);
            
            Assert.AreEqual("line1\nline2\nline3", normalized);
        }

        [Test]
        public void NormalizeLineEndings_CR()
        {
            var content = "line1\rline2\rline3";
            var normalized = GitBlobHash.NormalizeLineEndings(content);
            
            Assert.AreEqual("line1\nline2\nline3", normalized);
        }

        [Test]
        public void NormalizeLineEndings_LF()
        {
            var content = "line1\nline2\nline3";
            var normalized = GitBlobHash.NormalizeLineEndings(content);
            
            Assert.AreEqual("line1\nline2\nline3", normalized);
        }

        [Test]
        public void NormalizeLineEndings_Mixed()
        {
            var content = "line1\r\nline2\rline3\nline4";
            var normalized = GitBlobHash.NormalizeLineEndings(content);
            
            Assert.AreEqual("line1\nline2\nline3\nline4", normalized);
        }
    }
}
