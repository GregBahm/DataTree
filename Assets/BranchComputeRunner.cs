using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class BranchComputeRunner
{
    private readonly ComputeShader _computeShader;
    private readonly Mesh _branchMesh;

    private readonly int _meshBufferStride;
    private readonly ComputeBuffer _meshBuffer;

    private readonly int _branchStride;
    private readonly ComputeBuffer _branchBuffer;

    private readonly int _branchPointStride;
    private readonly ComputeBuffer _branchPointBuffer;

    private readonly int _finalPositionsKernel;
    private readonly int _siblingPressureKernel;

    public BranchComputeRunner(ComputeShader computeShader,
        Mesh branchMesh)
    {
        _computeShader = computeShader;
        _branchMesh = branchMesh;
        _meshBuffer = GetMeshBuffer();
        _branchBuffer = GetBranchBuffer();
        _branchPointBuffer = GetBranchPointBuffer();
    }

    private ComputeBuffer GetBranchPointBuffer()
    {
        throw new NotImplementedException();
    }

    private ComputeBuffer GetBranchBuffer()
    {
        throw new NotImplementedException();
    }

    private ComputeBuffer GetMeshBuffer()
    {
        throw new NotImplementedException();
    }

    public void DispatchCompute()
    {

    }
}
