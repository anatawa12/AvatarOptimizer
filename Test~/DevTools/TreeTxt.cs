using System.Collections.Generic;

namespace AvatarOptimizer.DevTools
{
    public class TreeTxtNode
    {
        public int Level { get; }
        public string Text { get; }
        public List<TreeTxtNode> Children { get; } = new();

        // Text accessor
        // "Key: Value" line
        public string Key => Text.Split(':', 2)[0];
        public string Value => Text.Split(':', 2)[1].Trim();
        // Key in format of "Path(Type)"
        public string KeyPath => Key.Split('(', 2)[0];
        public string KeyType => Key.Split('(', 2)[1].TrimEnd(')');

        private TreeTxtNode(int level, string text)
        {
            Level = level;
            Text = text;
        }

        public static TreeTxtNode Parse(string treeTxt)
        {
            var stack = new List<TreeTxtNode>();
            var root = new TreeTxtNode(-1, "");
            stack.Add(root);

            foreach (var lineIn in treeTxt.Split('\n'))
            {
                var line = lineIn;
                if (string.IsNullOrWhiteSpace(line)) continue;
                if (line.EndsWith('\r')) line = line[..^1];
                var indent = ComputeLevel(line);
                var node = new TreeTxtNode(indent, line[indent..]);

                while (stack[^1].Level >= indent) stack.RemoveAt(stack.Count - 1);
                stack[^1].Children.Add(node);
                stack.Add(node);
            }

            return root;
        }

        private static int ComputeLevel(string line)
        {
            var level = 0;
            while (level < line.Length && line[level] == ' ') level++;
            return level;
        }
    }
}