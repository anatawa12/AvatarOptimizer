namespace Anatawa12.AvatarOptimizer;

public static class UnionFind
{
    public static TNode FindRoot<TNode>(TNode node) where TNode : UnionFindNode<TNode>
    {
        if (node.Info.Parent == null)
            return node;
        return node.Info.Parent = FindRoot(node.Info.Parent);
    }

    public static void Merge<TNode>(TNode node1, TNode node2) where TNode : UnionFindNode<TNode>
    {
        var root1 = FindRoot(node1);
        var root2 = FindRoot(node2);

        if (root1 == root2) return;

        if (root1.Info.Rank < root2.Info.Rank)
        {
            root1.Info.Parent = root2;
        }
        else if (root1.Info.Rank > root2.Info.Rank)
        {
            root2.Info.Parent = root1;
        }
        else
        {
            root2.Info.Parent = root1;
            root1.Info.Rank++;
        }
    }

    public static bool Same<TNode>(TNode node1, TNode node2) where TNode : UnionFindNode<TNode> =>
        FindRoot(node1) == FindRoot(node2);
}

internal struct UnionFindNodeInfo<TNode> where TNode : UnionFindNode<TNode>
{
    public TNode? Parent;
    public int Rank;

    public UnionFindNodeInfo(TNode parent, int rank)
    {
        Parent = parent;
        Rank = rank;
    }
}

public abstract class UnionFindNode<TNode> where TNode : UnionFindNode<TNode>
{
    private UnionFindNodeInfo<TNode> _info;
    internal ref UnionFindNodeInfo<TNode> Info => ref _info;

    public TNode FindRoot() => UnionFind.FindRoot((TNode)this);
}
