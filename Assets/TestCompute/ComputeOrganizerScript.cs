using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ComputeOrganizerScript : MonoBehaviour
{
    public int ObjectsCount;
    private int _siblingPairsCount;

    [Range(0, 1000)]
    public float RepelDist;
    [Range(0, 1)]
    public float RepelPower;
    [Range(0, 1)]
    public float Attract;

    public Material ObjectMaterial;

    public ComputeShader TestOrganizerCompute;
    private int _siblingPressureKernel;
    private int _applyPressureKernel;

    public Mesh TestOrganizerMesh;
    private const int _meshBufferStride = sizeof(float) * 3;
    private ComputeBuffer _meshBuffer;
    private int _meshVertCount;
    
    private const int _siblingPairsStride = sizeof(int) + sizeof(int); // SelfIndex, SiblingIndex
    private ComputeBuffer _siblingPairsBuffers;

    private const int _positionStride = sizeof(float) * 2;
    private ComputeBuffer _positionBuffer;

    private const int _siblingPressureStride = sizeof(int) * 2;
    private ComputeBuffer _siblingPressureBuffer;

    private const int _batchSize = 128;

    struct SiblingPair
    {
        public float SelfIndex;
        public float SiblingIndex;
    }

    private void Start()
    {
        _siblingPressureKernel = TestOrganizerCompute.FindKernel("ComputeSiblingPressure");
        _applyPressureKernel = TestOrganizerCompute.FindKernel("ApplySiblingPressure");
        _meshVertCount = TestOrganizerMesh.triangles.Length;
        _meshBuffer = GetMeshBuffer(TestOrganizerMesh);
        _positionBuffer = GetDataBuffer();
        _siblingPairsBuffers = GetSiblingPairsBuffer();
        _siblingPairsCount = (ObjectsCount * ObjectsCount - ObjectsCount) / 2;
        _siblingPressureBuffer = new ComputeBuffer(ObjectsCount, _siblingPressureStride);
    }

    private ComputeBuffer GetSiblingPairsBuffer()
    {
        List<SiblingPair> data = new List<SiblingPair>();
        for (int i = 0; i < ObjectsCount; i++)
        {
            for (int j = 0; j < i; j++)
            {
                data.Add(new SiblingPair() { SelfIndex = i, SiblingIndex = j });
            }
        }
        ComputeBuffer buffer = new ComputeBuffer(data.Count, _siblingPairsStride);
        buffer.SetData(data.ToArray());
        return buffer;
    }

    private ComputeBuffer GetDataBuffer()
    {
        Vector2[] data = new Vector2[ObjectsCount];
        ComputeBuffer buffer = new ComputeBuffer(ObjectsCount, _positionStride);
        for (int i = 0; i < ObjectsCount; i++)
        {
            data[i] = new Vector2(UnityEngine.Random.value, UnityEngine.Random.value);
        }
        buffer.SetData(data);
        return buffer;
    }

    private ComputeBuffer GetMeshBuffer(Mesh mesh)
    {
        Vector3[] meshVerts = new Vector3[_meshVertCount];
        ComputeBuffer ret = new ComputeBuffer(_meshVertCount, _meshBufferStride);
        for (int i = 0; i < _meshVertCount; i++)
        {
            meshVerts[i] = mesh.vertices[mesh.triangles[_meshVertCount - i - 1]];
        }
        ret.SetData(meshVerts);
        return ret;
    }

    private void Update()
    {
        TestOrganizerCompute.SetFloat("_RepelDist", RepelDist);
        TestOrganizerCompute.SetFloat("_RepelPower", RepelPower);
        TestOrganizerCompute.SetFloat("_DrawPower", Attract);
        TestOrganizerCompute.SetFloat("_ObjectsCount", ObjectsCount);

        TestOrganizerCompute.SetBuffer(_siblingPressureKernel, "_PositionsBuffer", _positionBuffer);
        TestOrganizerCompute.SetBuffer(_siblingPressureKernel, "_SiblingPairsBuffer", _siblingPairsBuffers);
        TestOrganizerCompute.SetBuffer(_siblingPressureKernel, "_SiblingPressureBuffer", _siblingPressureBuffer);

        int siblingBatchSize = Mathf.CeilToInt((float)_siblingPairsCount / _batchSize);
        int nodeBatchSize = Mathf.CeilToInt((float)ObjectsCount / _batchSize);

        TestOrganizerCompute.Dispatch(_siblingPressureKernel, siblingBatchSize, 1, 1);

        TestOrganizerCompute.SetBuffer(_applyPressureKernel, "_PositionsBuffer", _positionBuffer);
        TestOrganizerCompute.SetBuffer(_applyPressureKernel, "_SiblingPressureBuffer", _siblingPressureBuffer);
        TestOrganizerCompute.Dispatch(_applyPressureKernel, nodeBatchSize, 1, 1);
    }

    private void OnRenderObject()
    {
        ObjectMaterial.SetBuffer("_PositionsBuffer", _positionBuffer);
        ObjectMaterial.SetBuffer("_MeshBuffer", _meshBuffer);
        ObjectMaterial.SetPass(0);
        Graphics.DrawProcedural(MeshTopology.Triangles, _meshVertCount, ObjectsCount);
    }

    private void OnDestroy()
    {
        _meshBuffer.Release();
        _positionBuffer.Release();
        _siblingPairsBuffers.Release();
        _siblingPressureBuffer.Release();
    }
}
