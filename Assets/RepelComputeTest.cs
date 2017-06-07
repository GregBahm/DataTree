using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepelComputeTest : MonoBehaviour 
{
    public int ObjectCount;
    public Mesh ObjectMesh;

    public Material RepelTestMaterial;

    public ComputeShader RepelComputeShader;

    public ComputeBuffer _repelSiblingsBuffer;
    public ComputeBuffer _repelPositionsBuffer;

    private int _repelSiblingsStride;

    struct RepelSiblingsStruct
    {
        public int SelfIndex;
        public int SiblingIndex;
    }

    private void Start()
    {
        
    }

    private void OnRenderObject()
    {
        
    }
}
