using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

internal class NodeBuilder : DatumBase
{
    public int RawIndex { get; set; }

    private readonly List<NodeBuilder> _children;
    public IEnumerable<NodeBuilder> Children { get { return _children; } }

    private NodeBuilder _parent;
    public NodeBuilder Parent
    {
        get { return _parent; }
        set
        {
            if (_parent == value)
            {
                return;
            }
            if (_parent != null)
            {
                _parent._children.Remove(this);
            }
            _parent = value;
            if (_parent != null)
            {
                _parent._children.Add(this);
            }
        }
    }

    public NodeBuilder(string name, string accountUrl, string avatarUrl)
        : base(name, accountUrl, avatarUrl)
    {
        _children = new List<NodeBuilder>();
    }

    public override string ToString()
    {
        return AccountUrl + " + " + _children.Count + " children";
    }

    public Node ToNode(Node parent)
    {
        return new Node(parent, Name, AccountUrl, AvatarUrl, Children);
    }
}
