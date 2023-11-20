using Anatawa12.AvatarOptimizer.AnimatorParsers;

namespace Anatawa12.AvatarOptimizer.Test.AnimatorParserTest
{
    class TestSourceImpl : IModificationSource
    {
        public static readonly TestSourceImpl Instance = new TestSourceImpl();

        private TestSourceImpl()
        {
        }
    }
}