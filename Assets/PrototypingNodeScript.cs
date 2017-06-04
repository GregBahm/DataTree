using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//Delete once you start instancing
public class PrototypingNodeScript : MonoBehaviour
{
    public Node Node;
    public SkinnedMeshRenderer Renderer;
    public MainScript Mothership;
    
    public NodePositioner Positioner;

    private float GetScale(Node node)
    {
        float scaleCount = node.TotalChildCount + 1;
        float relativeScale = scaleCount / Mothership.RootNode.TotalChildCount;
        float ramp = 1 - Mathf.Pow(1 - relativeScale, Mothership.BranchThicknessRamp);
        float ret = Mathf.Lerp(relativeScale, 1, ramp) * Mothership.BranchThickness;
        return Mathf.Pow(scaleCount * Mothership.BranchThickness, Mothership.BranchThicknessRamp);
    }
    
	void Update ()
    {
        UpdatePosition();

        Renderer.material.SetVector("_EndPoint", Positioner.Position);
        Renderer.material.SetVector("_StartPoint", Positioner.Parent.Position);

        float startScale = GetScale(Node.Parent);
        float endScale = GetScale(Node);

        Renderer.material.SetFloat("_StartScale", startScale);
        Renderer.material.SetFloat("_EndScale", endScale);

        Renderer.material.SetFloat("_BranchColorStart", startScale);
        Renderer.material.SetFloat("_BranchColorEnd", endScale);

        Renderer.localBounds = GetBranchBounds(Positioner.Position, Positioner.Parent.Position);
    }

    private void UpdatePosition()
    {
        
    }

    private Bounds GetBranchBounds(Vector3 from, Vector3 to)
    {
        Vector3 midPoint = (from + to) / 2;
        Vector3 halfDiagonal = (from - to);
        halfDiagonal = new Vector3(Mathf.Abs(halfDiagonal.x), Mathf.Abs(halfDiagonal.y), Mathf.Abs(halfDiagonal.z));
        halfDiagonal += Vector3.one;
        return new Bounds(midPoint, halfDiagonal);
    }
}