using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepelTestScript : MonoBehaviour 
{
    public Transform Repeller;
    public Transform Repelled;
    public float RepelDist;
    [Range(0,1)]
    public float Drag;

    private void Update()
    {
        Vector3 diff = Repelled.position - Repeller.position;
        float dist = diff.magnitude;
        Vector3 normalized = diff.normalized;
        float power = Mathf.Max(0, RepelDist - dist) / RepelDist;
        Vector3 repelVector = normalized * power * Drag;
        Repelled.Translate(repelVector);
    }
}
