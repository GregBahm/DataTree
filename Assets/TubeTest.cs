using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TubeTest : MonoBehaviour
{
    public Material TubeMat;
    public float Offset;

    private void Update()
    {
        TubeMat.SetVector("_EndPoint", new Vector4(0, 1, Offset, 0));
        TubeMat.SetFloat("_StartScale", 1);
        TubeMat.SetFloat("_EndScale", 1);
    }
}
