using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class MainScript : MonoBehaviour
{
    public Material BranchMaterial;
    public Material SphereMaterial;
    public Mesh BranchMesh;
    public Node RootNode;
    public float BranchThickness = 1;
    public float BranchThicknessRamp = 1;
    public float SphereScaler = 1;
    public Color SmallBranchColor;
    public Color LargeBranchColor;

    public float AvatarSize = 1;

    public float RepelPower;
    public float AttractPower;
    private PositionManager _positionManager;

    public float Height = 1;
    
    void Start()
    {
        string rawDataFolder = @"D:\DataTree\SamplePostData\Raw\";
        DataProcessor processor = new DataProcessor(rawDataFolder);
        RootNode = processor.ProcessData();
        _positionManager = new PositionManager(RootNode);
        DrawNode(RootNode, null, 0, _positionManager.GetPositionersDictionary());
    }

    private void Update()
    {
        Shader.SetGlobalColor("_BranchSmallColor", SmallBranchColor);
        Shader.SetGlobalColor("_BranchLargeColor", LargeBranchColor);
        Shader.SetGlobalFloat("_AvatarSize", AvatarSize);
        _positionManager.UpdateNodes(RepelPower, AttractPower, Height);
    }

    private GameObject DrawNode(Node node, Transform parent, float angle, Dictionary<Node, NodePositioner> positioners)
    {
        GameObject newObject = new GameObject();
        newObject.name = node.SubUrl;
        newObject.transform.parent = parent;

        if (node.Parent != null)
        {
            bool hasSibblings = node.Parent != null && node.Parent.ImmediateChildCount > 1;
            Vector3 localPos = new Vector3(hasSibblings ? 1 : 0, 1, 0);
            newObject.transform.position = localPos + parent.position;
            parent.Rotate(Vector3.up, angle);
            CreateMesh(newObject.transform, parent, node, positioners[node]);
        }

        DrawChildrenNodes(node, newObject.transform, positioners);
        return newObject;
    }

    private void CreateMesh(Transform selfTransform, Transform parentTransform, Node node, NodePositioner positioner)
    {
        GameObject newObj = new GameObject();

        SkinnedMeshRenderer renderer = newObj.AddComponent<SkinnedMeshRenderer>();
        renderer.sharedMesh = BranchMesh;
        Material mat = new Material(BranchMaterial);
        renderer.material = mat;

        PrototypingNodeScript nodeScript = selfTransform.gameObject.AddComponent<PrototypingNodeScript>();
        nodeScript.Renderer = renderer;
        nodeScript.Node = node;
        nodeScript.Mothership = this;
        nodeScript.Positioner = positioner;
    }

    private void DrawChildrenNodes(Node node, Transform parent, Dictionary<Node, NodePositioner> positioners)
    {
        float angleIncrement = node.ImmediateChildCount > 1 ? (360f / node.ImmediateChildCount) : 0;
        int index = 0;
        foreach (Node child in node.Children)
        {
            DrawNode(child, parent, angleIncrement, positioners);
            index++;
        }
    }

    private class PositionManager
    {
        private IEnumerable<NodePositioner> _nodes;

        public PositionManager(Node rootNode)
        {
            List<PositionNodeBuilder> builders = new List<PositionNodeBuilder>();
            GetBuilders(rootNode, null, builders);
            List<PositionNodeBuilder>[] byLayer = GetByLayer(builders);
            foreach (List<PositionNodeBuilder> layer in byLayer)
            {
                foreach (PositionNodeBuilder item in layer)
                {
                    item.SetSiblings(layer);
                }
            }
            Dictionary<PositionNodeBuilder, NodePositioner> dictionary = builders.ToDictionary(item => item, item => item.ToNode());
            foreach (KeyValuePair<PositionNodeBuilder, NodePositioner> item in dictionary)
            {
                NodePositioner parent = item.Key.Parent == null ? null : dictionary[item.Key.Parent];
                IEnumerable<NodePositioner> siblings = item.Key.Siblings.Select(sibling => dictionary[sibling]);
                IEnumerable<NodePositioner> children = item.Key.Children.Select(child => dictionary[child]);
                item.Value.FinishInitialization(parent, siblings, children);
            }
            _nodes = dictionary.Values;
        }

        public Dictionary<Node, NodePositioner> GetPositionersDictionary()
        {
            return _nodes.ToDictionary(item => item.Node, item => item);
        }

        public void UpdateNodes(float repelPower, float attractPower, float height)
        {
            foreach (NodePositioner item in _nodes)
            {
                item.UpdatePosition(repelPower, attractPower, height);
            }
        }

        private List<PositionNodeBuilder>[] GetByLayer(IEnumerable<PositionNodeBuilder> builders)
        {

            int highestBranch = builders.Max(item => item.Node.ParentCount);
            List<PositionNodeBuilder>[] ret = new List<PositionNodeBuilder>[highestBranch + 1];
            for (int i = 0; i < highestBranch + 1; i++)
            {
                ret[i] = new List<PositionNodeBuilder>();
            }
            foreach (PositionNodeBuilder builder in builders)
            {
                ret[builder.Node.ParentCount].Add(builder);
            }
            return ret;
        }

        private PositionNodeBuilder GetBuilders(Node node, PositionNodeBuilder parent, List<PositionNodeBuilder> ret)
        {
            PositionNodeBuilder newBuilder = new PositionNodeBuilder(parent, node);
            ret.Add(newBuilder);

            List<PositionNodeBuilder> children = new List<PositionNodeBuilder>();
            foreach (Node child in node.Children)
            {
                PositionNodeBuilder childBuilder = GetBuilders(child, newBuilder, ret);
                children.Add(childBuilder);
            }
            newBuilder.Children = children;
            return newBuilder;
        }

        private class PositionNodeBuilder
        {
            private  PositionNodeBuilder _parent;
            public PositionNodeBuilder Parent{ get{ return _parent; } }

            private readonly Node _node;
            public Node Node{ get{ return _node; } }

            private IEnumerable<PositionNodeBuilder> _siblings;
            public IEnumerable<PositionNodeBuilder> Siblings { get{ return _siblings; } }
            
            public IEnumerable<PositionNodeBuilder> Children { get; set; }

            public PositionNodeBuilder(PositionNodeBuilder parent, Node node)
            {
                _parent = parent;
                _node = node;
            }

            internal void SetSiblings(List<PositionNodeBuilder> layer)
            {
                _siblings = layer.Where(item => item != this).ToArray();
            }

            public NodePositioner ToNode()
            {
                return new NodePositioner(_node, Vector2.zero, 0, _node.ParentCount);
            }
        }
    }
}


