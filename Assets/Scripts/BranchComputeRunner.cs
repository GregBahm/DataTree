using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class BranchComputeRunner : MonoBehaviour
{
    public Color BranchSmallColor;
    public Color BranchLargeColor;
    public Color BranchTipColor;
    public Color AvatarFrame;
    public float AvatarSize;
    public float BranchHeight;
    public float BranchThickness;
    public float BranchThicknessRamp;
    public float BranchColorRamp;
    public float BranchColorOffset;
    [Range(0, 1)]
    public float DrawPower;
    public float RepelDistance;
    [Range(0, 1)]
    public float RepelPower;

    public ComputeShader BranchCompute;
    public Material BranchMat;
    
    public Mesh BranchMesh;
    public Mesh AvatarDisplayMesh;

    public Material BlitMaterial;
    private AvatarLoader _avatarLoader;

    private int _meshBufferStride = sizeof(float) * 3 + sizeof(float) * 2 + sizeof(float) * 3 + sizeof(float) * 3; // Pos, Uvs, Normals, Color
    private ComputeBuffer _tubeMeshBuffer;
    private int _tubeVertCount;
    
    private ComputeBuffer _avatarMeshBuffer;
    private int _avatarVertCount;

    private int _fixedDataStride = sizeof(int) + sizeof(int) + sizeof(int)
        + sizeof(float) + sizeof(float) + sizeof(int) + sizeof(float) * 2; // ParentIndex, ImmediateChildrenCount, BranchLevel, LevelOffset, BranchParameter, Scale, AvatarUvOffset
    private ComputeBuffer _fixedDataBuffer;

    private int _variableDataStride = sizeof(float) * 2 + sizeof(int) * 2; // Pos, CurrentSiblingPressure
    private ComputeBuffer _variableDataBuffer;

    private int _siblingPairsStride = sizeof(int) + sizeof(int); // SelfIndex, SiblingIndex
    private ComputeBuffer[] _siblingPairsBuffers;

    private int _computeFinalPositionsKernel;
    private int _computeSiblingPressureKernel;

    private int _nodeCount;
    private int[] _siblingPairCounts;
    private int[] _siblingBatchSizes;
    private int _nodeBatchSize;
    private const int BatchSize = 128;
    struct MeshData
    {
        public Vector3 Pos;
        public Vector2 Uvs;
        public Vector3 Normal;
        public Vector3 Color;
    }
    struct FixedBranchData
    {
        public int ParentIndex;
        public int ImmediateChildenCount;
        public int BranchLevel;
        public float LevelOffset;
        public float BranchParameter;
        public int Scale;
        public Vector2 AvatarUvOffset;
    }
    struct VariableBranchData
    {
        public Vector2 Pos;
        public Vector2 CurrentSiblingPressure;
    }
    struct SiblingPair
    {
        public int SelfIndex;
        public int SiblingIndex;
    }

    struct SiblingsSetupData
    {
        public int[] PairCounts;
        public ComputeBuffer[] Buffers;
    }

    struct SiblingSetupDatum
    {
        public int PairsCount;
        public ComputeBuffer Buffer;
    }

    void Start()
    {

        DataProcessor processor = DataProcessor.GetTestProcessor();
        Node rootNode = processor.ProcessData();

        _avatarLoader = new AvatarLoader(processor, BlitMaterial);

        _computeFinalPositionsKernel = BranchCompute.FindKernel("ComputeFinalPositions");
        _computeSiblingPressureKernel = BranchCompute.FindKernel("ComputeSiblingPressure");

        _tubeVertCount = BranchMesh.triangles.Length;
        _tubeMeshBuffer = GetMeshBuffer(BranchMesh);
        _avatarVertCount = AvatarDisplayMesh.triangles.Length;
        _avatarMeshBuffer = GetMeshBuffer(AvatarDisplayMesh);
        
        _nodeCount = rootNode.TotalChildCount + 1;
        Node[] nodeList = GetNodeList(rootNode);
        Dictionary<Node, int> lookupTable = GetLookupTable(nodeList);
        _variableDataBuffer = GetVariableDataBuffer(rootNode, lookupTable);
        _fixedDataBuffer = GetFixedDataBuffer(nodeList, lookupTable, _avatarLoader);

        SiblingsSetupData siblingSetup = GetSibblingSetup(nodeList, lookupTable);
        _siblingPairCounts = siblingSetup.PairCounts;
        _siblingPairsBuffers = siblingSetup.Buffers;

        _nodeBatchSize = Mathf.CeilToInt((float)_nodeCount / BatchSize);
        _siblingBatchSizes = _siblingPairCounts.Select(item => Mathf.CeilToInt((float)item / BatchSize)).ToArray();
    }

    private Node[] GetNodeList(Node rootNode)
    {
        List<Node> ret = new List<Node>();
        PopulateNodeList(ret, rootNode);
        return ret.ToArray();
    }

    private void PopulateNodeList(List<Node> ret, Node node)
    {
        ret.Add(node);
        foreach (Node child in node.Children)
        {
            PopulateNodeList(ret, child);
        }
    }

    private Dictionary<Node, int> GetLookupTable(Node[] nodeList)
    {
        Dictionary<Node, int> ret = new Dictionary<Node, int>();
        for (int i = 0; i < nodeList.Length; i++)
        {
            ret.Add(nodeList[i], i);
        }
        return ret;
    }

    private void DispatchSiblingPressure()
    {
        BranchCompute.SetFloat("_RepelPower", RepelPower);
        BranchCompute.SetFloat("_RepelDist", RepelDistance);
        BranchCompute.SetBuffer(_computeSiblingPressureKernel, "_VariableDataBuffer", _variableDataBuffer);

        for (int i = 0; i < _siblingPairsBuffers.Length; i++)
        {
            BranchCompute.SetBuffer(_computeSiblingPressureKernel, "_SiblingPairsBuffer", _siblingPairsBuffers[i]);
            BranchCompute.Dispatch(_computeSiblingPressureKernel, _siblingBatchSizes[i], 1, 1);
        }
    }

    private void DispatchFinalPositioner()
    {
        BranchCompute.SetFloat("_DrawPower", DrawPower);
        BranchCompute.SetBuffer(_computeFinalPositionsKernel, "_VariableDataBuffer", _variableDataBuffer);
        BranchCompute.SetBuffer(_computeFinalPositionsKernel, "_FixedDataBuffer", _fixedDataBuffer);
        BranchCompute.Dispatch(_computeFinalPositionsKernel, _nodeBatchSize, 1, 1);
    }

    private void Update()
    {
        DispatchSiblingPressure();
        DispatchFinalPositioner();
    } 

    private SiblingsSetupData GetSibblingSetup(Node[] nodes, Dictionary<Node, int> lookupTable)
    {
        int potentialLayers = nodes.Max(item => item.ParentCount);
        List<int> pairCounts = new List<int>();
        List<ComputeBuffer> buffers = new List<ComputeBuffer>();
        for (int i = 0; i < potentialLayers; i++)
        {
            SiblingSetupDatum layerData = GetSibblingPairsBuffers(nodes, i, lookupTable);
            if(layerData.PairsCount > 0)
            {
                buffers.Add(layerData.Buffer);
                pairCounts.Add(layerData.PairsCount);
            }
        }
        return new SiblingsSetupData() { Buffers = buffers.ToArray(), PairCounts = pairCounts.ToArray() };
    }

    private SiblingSetupDatum GetSibblingPairsBuffers(Node[] nodes, int layer, Dictionary<Node, int> lookupTable)
    {
        Node[] siblingsOfLayer = nodes.Where(item => item.ParentCount == layer).ToArray();
        int siblingPairs = siblingsOfLayer.Length * siblingsOfLayer.Length - siblingsOfLayer.Length;
        if(siblingPairs == 0)
        {
            return new SiblingSetupDatum() { Buffer = null, PairsCount = 0 };
        }
        List<SiblingPair> data = new List<SiblingPair>();
        ComputeBuffer buffer = new ComputeBuffer(siblingPairs, _siblingPairsStride);
        
        for (int i = 0; i < siblingsOfLayer.Length; i++)
        {
            for (int j = 0; j < i; j++)
            {
                int selfIndex = lookupTable[siblingsOfLayer[i]];
                int siblingIndex = lookupTable[siblingsOfLayer[j]];
                data.Add(new SiblingPair() { SelfIndex = selfIndex, SiblingIndex = siblingIndex });
            }
        }

        buffer.SetData(data.ToArray());
        return new SiblingSetupDatum() { Buffer = buffer, PairsCount = siblingPairs };
    }

    private FixedBranchData GetFixedBranchDatum(Node node, Dictionary<Node, int> lookupTable, AvatarLoader avatarLoader)
    {
        FixedBranchData ret = new FixedBranchData()
        {
            BranchLevel = node.ParentCount,
            ImmediateChildenCount = node.ImmediateChildCount,
            LevelOffset = UnityEngine.Random.value + 1f,
            Scale = node.TotalChildCount + 1,
            ParentIndex = node.Parent == null ? 0 : lookupTable[node.Parent],
            BranchParameter = (float)node.ParentCount / (node.ParentCount + node.LevelsOfChildren),
            AvatarUvOffset = avatarLoader.GetCoordsFor(node)
        };
        return ret;
    }
    private ComputeBuffer GetFixedDataBuffer(Node[] nodes, Dictionary<Node, int> lookupTable, AvatarLoader avatarLoader)
    {
        FixedBranchData[] data = new FixedBranchData[_nodeCount];
        ComputeBuffer buffer = new ComputeBuffer(_nodeCount, _fixedDataStride);
        for (int i = 0; i < _nodeCount; i++)
        {
            data[i] = GetFixedBranchDatum(nodes[i], lookupTable, avatarLoader);
        }
        buffer.SetData(data);
        return buffer;
    }

    private ComputeBuffer GetVariableDataBuffer(Node rootNode, Dictionary<Node, int> lookupTable)
    {
        VariableBranchData[] data = new VariableBranchData[_nodeCount];
        ComputeBuffer buffer = new ComputeBuffer(_nodeCount, _variableDataStride);
        SetVariableBranchData(rootNode, Vector2.zero, data, lookupTable);
        buffer.SetData(data);
        return buffer;
    }

    private void SetVariableBranchData(Node currentNode, Vector2 parentPos, VariableBranchData[] dataToSet, Dictionary<Node, int> lookupTable)
    {
        float newX = parentPos.x + (UnityEngine.Random.value * 2 - 1);
        float newY = parentPos.y + (UnityEngine.Random.value * 2 - 1);
        Vector2 nodePos = new Vector2(newX, newY);

        int dataIndex = lookupTable[currentNode];
        if (dataIndex == 0)
        {
            nodePos = Vector2.zero;
        }
        VariableBranchData ret = new VariableBranchData()
        {
            Pos = nodePos,
            CurrentSiblingPressure = Vector2.zero,
        };
        dataToSet[dataIndex] = ret;

        foreach (Node child in currentNode.Children)
        {
            SetVariableBranchData(child, nodePos, dataToSet, lookupTable);
        }
    }

    private ComputeBuffer GetMeshBuffer(Mesh mesh)
    {
        int vertCount = mesh.triangles.Length;
        MeshData[] meshVerts = new MeshData[vertCount];
        ComputeBuffer ret = new ComputeBuffer(vertCount, _meshBufferStride);
        for (int i = 0; i < vertCount; i++)
        {
            Color color = mesh.colors.Length == 0 ? Color.red : mesh.colors[mesh.triangles[vertCount - i - 1]];
            meshVerts[i].Pos = mesh.vertices[mesh.triangles[vertCount - i - 1]];
            meshVerts[i].Uvs = mesh.uv[mesh.triangles[vertCount - i - 1]];
            meshVerts[i].Normal = mesh.normals[mesh.triangles[vertCount - i - 1]];
            meshVerts[i].Color = new Vector3(color.r, color.g, color.b);
        }
        ret.SetData(meshVerts);
        return ret;
    }

    private void OnRenderObject()
    { 
        BranchMat.SetFloat("_AvatarSize", AvatarSize);
        BranchMat.SetFloat("_BranchHeight", BranchHeight);
        BranchMat.SetFloat("_BranchThickness", BranchThickness);
        BranchMat.SetFloat("_BranchThicknessRamp", BranchThicknessRamp);
        BranchMat.SetColor("_BranchSmallColor", BranchSmallColor);
        BranchMat.SetColor("_BranchLargeColor", BranchLargeColor);
        BranchMat.SetColor("_BranchTipColor", BranchTipColor);
        BranchMat.SetFloat("_BranchColorRamp", BranchColorRamp);
        BranchMat.SetFloat("_BranchColorOffset", BranchColorOffset);

        BranchMat.SetBuffer("_MeshBuffer", _tubeMeshBuffer);
        BranchMat.SetBuffer("_CardMeshBuffer", _avatarMeshBuffer);
        BranchMat.SetBuffer("_FixedDataBuffer", _fixedDataBuffer);
        BranchMat.SetBuffer("_VariableDataBuffer", _variableDataBuffer);
        BranchMat.SetTexture("_AvatarAtlas", _avatarLoader.AtlasTexture);
        BranchMat.SetColor("_AvatarColor", AvatarFrame);
        BranchMat.SetPass(0);
        Graphics.DrawProcedural(MeshTopology.Quads, _tubeVertCount, _nodeCount);
        BranchMat.SetPass(1);
        Graphics.DrawProcedural(MeshTopology.Quads, _avatarVertCount, _nodeCount);
    }

    private void OnDestroy()
    {
        _tubeMeshBuffer.Dispose();
        _avatarMeshBuffer.Dispose();
        _fixedDataBuffer.Dispose();
        _variableDataBuffer.Dispose();
        for (int i = 0; i < _siblingPairsBuffers.Length; i++)
        {
            _siblingPairsBuffers[i].Dispose();
        }
    }
}
