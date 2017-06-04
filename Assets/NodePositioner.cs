using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class NodePositioner
{
    private Vector3 _publicPosition;
    public Vector3 Position{ get{ return _publicPosition; } }

    private Vector2 _currentPosition;

    private readonly float _radius;
    private readonly int _branchHeight; // The Y Value
    private IEnumerable<NodePositioner> _siblingPositions;

    private readonly Node _node;
    public Node Node { get{ return _node; } }
    

    private NodePositioner _parent;
    public NodePositioner Parent { get{ return _parent; } }

    private IEnumerable<NodePositioner> _childrenPositions;

    private int _childCount;

    private float _heightRand;

    public NodePositioner(Node node,
        Vector2 startingPositon,
        float radius,
        int branchHeight)
    {
        _node = node;
        _currentPosition = startingPositon;
        _radius = radius;
        _branchHeight = branchHeight;
        _heightRand = UnityEngine.Random.value;
    }

    public void FinishInitialization(NodePositioner parent, IEnumerable<NodePositioner> siblings, IEnumerable<NodePositioner> children)
    {
        _parent = parent;
        _childrenPositions = children.ToArray();
        _childCount = children.Count();
        _siblingPositions = siblings.ToArray();
        _currentPosition = new Vector2(UnityEngine.Random.value / 100, UnityEngine.Random.value / 100);
    }

    public void UpdatePosition(float repelPower, float attractPower, float height)
    {
        Vector2 forceVector = GetForceVector(repelPower);
        Vector2 newPosition = _currentPosition + forceVector;
        newPosition = AttractToParent(newPosition, attractPower);
        newPosition = AttractToChildren(newPosition, attractPower);
        _currentPosition = newPosition;
        _publicPosition = new Vector3(_currentPosition.x, _heightRand + (_branchHeight * height), _currentPosition.y);
    }

    private Vector2 GetForceVector(float repelPower)
    {
        Vector2 forceVector = Vector3.zero;
        foreach (NodePositioner node in _siblingPositions)
        {
            forceVector += GetRepelForce(node, repelPower);
        }
        float clampedX = Mathf.Min(1f, Mathf.Abs(forceVector.x)) * Mathf.Sign(forceVector.x);
        float clampedY = Mathf.Min(1f, Mathf.Abs(forceVector.y)) * Mathf.Sign(forceVector.y);
        return new Vector2(clampedX, clampedY);
    }

    private Vector2 AttractToChildren(Vector2 afterRepel, float attractPower)
    {
        if(_childCount == 0)
        {
            return afterRepel;
        }
        Vector2 averagePos = Vector2.zero;
        foreach (NodePositioner child in _childrenPositions)
        {
            averagePos += child._currentPosition;
        }
        averagePos /= _childCount;
        Vector2 ret = Vector2.Lerp(afterRepel, averagePos, attractPower);
        return ret;
    }

    private Vector2 AttractToParent(Vector2 newPosition, float attractPower)
    {
        Vector2 parentPos = _parent == null ? Vector2.zero : _parent._currentPosition;
        float distToParent = (parentPos - _currentPosition).sqrMagnitude;
        return Vector2.Lerp(newPosition, parentPos, Mathf.Clamp01(attractPower));
    }

    private Vector2 GetRepelForce(NodePositioner from, float repelPower)
    {
        Vector2 repelVector = from._currentPosition - _currentPosition;
        if (repelVector.sqrMagnitude < float.Epsilon)
        {
            return new Vector2(UnityEngine.Random.value / 100, UnityEngine.Random.value / 100);
        }
        float squareDist = repelVector.sqrMagnitude;
        Vector2 ret = repelPower * repelVector.normalized * (0.00001f / squareDist);
        return -ret;
    }
}