using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RepelTestScript : MonoBehaviour 
{
    public int ObjectsCount;

    [Range(0, 1000)]
    public float RepelDist;
    [Range(0, 1)]
    public float RepelPower;
    [Range(0, 1)]
    public float Attract;

    private Vector3[] _next;
    private Transform[] _balls;

    private void Start()
    {
        _balls = new Transform[ObjectsCount];
        _next = new Vector3[ObjectsCount];
        for (int i = 0; i < ObjectsCount; i++)
        {
            GameObject newBall = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            newBall.transform.position = new Vector3(UnityEngine.Random.value, 0, UnityEngine.Random.value);
            _balls[i] = newBall.transform;
            _next[i] = Vector3.zero;
        }

    }

    private void Update()
    {
        GetNextData();
        Apply();
    }

    private void Apply()
    {
        for (int i = 0; i < ObjectsCount; i++)
        {
            Apply(_balls[i], _next[i]);
            _next[i] = Vector3.zero;
        }
    }

    private void Apply(Transform obj, Vector3 vector)
    {
        obj.Translate(vector);
        obj.position = Vector3.Lerp(obj.position, Vector3.zero, Attract);
    }

    private void GetNextData()
    {
        for (int i = 0; i < ObjectsCount; i++)
        {
            for (int j = 0; j < ObjectsCount; j++)
            {
                if (i != j)
                {
                    _next[i] += GetRepelData(_balls[i], _balls[j]);
                }
            }
        }
    }

    private Vector3 GetRepelData(Transform transform1, Transform transform2)
    {
        Vector3 diff = transform1.position - transform2.position;
        float dist = diff.magnitude;
        Vector3 normalized = diff.normalized;
        float power = Mathf.Max(0, RepelDist - dist) / RepelDist;
        Vector3 repelVector = normalized * power * RepelPower;
        return repelVector;
    }
}
