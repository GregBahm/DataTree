using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

public class Node
{
    private readonly Node _parent;
    public Node Parent { get { return _parent; } }

    private readonly string _name;
    public string Name { get { return _name; } }

    private readonly string _accountUrl;
    public string AccountUrl { get { return _accountUrl; } }

    private readonly string _avatarUrl;
    public string AvatarUrl { get { return _avatarUrl; } }

    private readonly string _subUrl;
    public string SubUrl { get { return _subUrl; } }

    private readonly IEnumerable<Node> _children;
    public IEnumerable<Node> Children { get { return _children; } }

    private readonly int _immediateChildCount;
    public int ImmediateChildCount { get { return _immediateChildCount; } }

    private readonly int _totalChildCount;
    public int TotalChildCount { get { return _totalChildCount; } }

    private readonly int _levelsOfChildren;
    public int LevelsOfChildren { get { return _levelsOfChildren; } }

    private readonly int _parentCount;
    public int ParentCount { get { return _parentCount; } }

    internal Node(Node parent, string name, string accountUrl, string avatarUrl, IEnumerable<NodeBuilder> children)
    {
        _parent = parent;
        _name = name;
        _accountUrl = accountUrl;
        _avatarUrl = avatarUrl;
        _subUrl = accountUrl.Replace("http://", "").Replace("https://", "").Split('.')[0];
        _children = children.Select(item => item.ToNode(this)).ToArray();
        _immediateChildCount = _children.Count();
        _totalChildCount = _children.Sum(child => child.TotalChildCount + 1);
        _parentCount = GetParentCount(parent, 0);
        _levelsOfChildren = GetMaxParentCount(this) - _parentCount;
    }

    private int GetMaxParentCount(Node node)
    {
        if(node.ImmediateChildCount > 0)
        {
            return node.Children.Max(child => GetMaxParentCount(child));
        }
        return node.ParentCount;
    }

    private int GetParentCount(Node parent, int count)
    {
        if (parent == null)
        {
            return count;
        }
        return GetParentCount(parent.Parent, count + 1);
    }

    public override string ToString()
    {
        string ret = SubUrl;
        if (Parent != null)
        {
            ret += " child of " + Parent.SubUrl;
        }
        ret += ", with " + ImmediateChildCount + " kids and " + TotalChildCount + " descendents";
        return ret;
    }
}