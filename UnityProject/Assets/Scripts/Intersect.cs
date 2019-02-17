using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

public class Intersect : MonoBehaviour {
    public MeshFilter targetMeshFilter;
    public MeshFilter subtractMeshFilter;

    private MeshFilter meshFilter;
    private Light light;

    public Vector3 i1, i2;

    void Start() {
        meshFilter = GetComponent<MeshFilter>();
        light = GetComponent<Light>();
    }

    void Update() {
        bool intersects = GetIntersect(targetMeshFilter, subtractMeshFilter, ref i1, ref i2);

        light.enabled = intersects;
        
        if (intersects) {
            Debug.DrawLine(i1, i2, new Color(0f, 1f, 0f, 0.5f), 0f, false);
        }
    }

    // https://web.stanford.edu/class/cs277/resources/papers/Moller1997b.pdf
    bool GetIntersect(MeshFilter targetMeshFilter, MeshFilter subtractMeshFilter, ref Vector3 i1, ref Vector3 i2) {
        const float epsilon = 0.000001f;

        bool equals(float n1, float n2) => Mathf.Abs(n1 - n2) <= epsilon;

        void swap<T>(T[] a, int i, int j) {
            T temp = a[i];
            a[i] = a[j];
            a[j] = temp;
        }

        Mesh target = targetMeshFilter.mesh;
        Mesh subtract = subtractMeshFilter.mesh;

        Vector3[] V1 = {
            targetMeshFilter.transform.TransformPoint(target.vertices[target.triangles[0]]),
            targetMeshFilter.transform.TransformPoint(target.vertices[target.triangles[1]]),
            targetMeshFilter.transform.TransformPoint(target.vertices[target.triangles[2]])
        };

        Vector3[] V2 = {
            subtractMeshFilter.transform.TransformPoint(subtract.vertices[subtract.triangles[0]]),
            subtractMeshFilter.transform.TransformPoint(subtract.vertices[subtract.triangles[1]]),
            subtractMeshFilter.transform.TransformPoint(subtract.vertices[subtract.triangles[2]])
        };

        // Construct plane 2
        Vector3 N2 = Vector3.Cross(V2[1] - V2[0], V2[2] - V2[0]);
        float d2 = Vector3.Dot(-N2, V2[0]);

        // Get distances between points V1i and plane 2
        float[] dV1 = {
            Vector3.Dot(N2, V1[0]) + d2,
            Vector3.Dot(N2, V1[1]) + d2,
            Vector3.Dot(N2, V1[2]) + d2
        };

        // Early rejection
        if (!equals(dV1[0], 0) && !equals(dV1[1], 0) && !equals(dV1[2], 0) &&
            (dV1[0] < 0 && dV1[1] < 0 && dV1[2] < 0 || dV1[0] > 0 && dV1[1] > 0 && dV1[2] > 0)) {
            return false;
        }

        // Construct plane 1
        Vector3 N1 = Vector3.Cross(V1[1] - V1[0], V1[2] - V1[0]);
        float d1 = Vector3.Dot(-N1, V1[0]);

        // Get distances between points V2i and plane 1
        float[] dV2 = {
            Vector3.Dot(N1, V2[0]) + d1,
            Vector3.Dot(N1, V2[1]) + d1,
            Vector3.Dot(N1, V2[2]) + d1
        };

        // Early rejection
        if (!equals(dV2[0], 0) && !equals(dV2[1], 0) && !equals(dV2[2], 0) &&
            (dV2[0] < 0 && dV2[1] < 0 && dV2[2] < 0 || dV2[0] > 0 && dV2[1] > 0 && dV2[2] > 0)) {
            return false;
        }

        // Triangles are co-planar, I don't need this case
        if (equals(dV1[0], 0) && equals(dV1[1], 0) && equals(dV1[2], 0)) {
            return false;
        }

        // Order the vertices, so that the one, on the opposite side of the plane to
        // the two others is in the middle

        if (dV1[1] * dV1[2] >= 0f) {
            swap(V1, 0, 1);
            swap(dV1, 0, 1);
        } else if (dV1[0] * dV1[1] >= 0f) {
            swap(V1, 1, 2);
            swap(dV1, 1, 2);
        }

        if (dV2[1] * dV2[2] >= 0f) {
            swap(V2, 0, 1);
            swap(dV2, 0, 1);
        } else if (dV2[0] * dV2[1] >= 0f) {
            swap(V2, 1, 2);
            swap(dV2, 1, 2);
        }

        // Direction of line
        Vector3 D = Vector3.Cross(N1, N2);

        float[] pV1 = {
            Vector3.Dot(D, V1[0]),
            Vector3.Dot(D, V1[1]),
            Vector3.Dot(D, V1[2])
        };

        float[] pV2 = {
            Vector3.Dot(D, V2[0]),
            Vector3.Dot(D, V2[1]),
            Vector3.Dot(D, V2[2])
        };

        // Calculate the intersection point offsets on line
        float t(float p_0, float p_1, float d_0, float d_1) => p_0 + (p_1 - p_0) * d_0 / (d_0 - d_1);

        float[] t1 = {
            t(pV1[0], pV1[1], dV1[0], dV1[1]),
            t(pV1[1], pV1[2], dV1[1], dV1[2])
        };

        bool t1Swapped = false;
        if (t1[0] > t1[1]) {
            swap(t1, 0, 1);
            t1Swapped = true;
        }

        float[] t2 = {
            t(pV2[0], pV2[1], dV2[0], dV2[1]),
            t(pV2[1], pV2[2], dV2[1], dV2[2])
        };

        bool t2Swapped = false;
        if (t2[0] > t2[1]) {
            swap(t2, 0, 1);
            t2Swapped = true;
        }

        bool between(float number, float[] bounds) => number >= bounds[0] && number <= bounds[1];

        Vector3 intersect(Vector3 point1, Vector3 point2, float distance1, float distance2) =>
            point1 + distance1 / (distance1 - distance2) * (point2 - point1);

        Vector3 intersectSwapped(bool swapped, int index, Vector3[] V, float[] d) {
            if (swapped) {
                index = index == 0 ? 1 : 0;
            }

            return intersect(V[index], V[index + 1], d[index], d[index + 1]);
        }

        bool intersects = false;
        
        if (between(t2[0], t1)) {
            i1 = intersectSwapped(t2Swapped, 0, V2, dV2);
            
            if (between(t2[1], t1)) {
                i2 = intersectSwapped(t2Swapped, 1, V2, dV2);
            } else {
                i2 = intersectSwapped(t1Swapped, 1, V1, dV1);
            }

            intersects = true;
        } else if (between(t2[1], t1)) {
            i1 = intersectSwapped(t2Swapped, 1, V2, dV2);
            i2 = intersectSwapped(t1Swapped, 0, V1, dV1);
            intersects = true;
        } else if (between(t1[0], t2)) { // or between(t1[1], t2) - they are both either inside or outside
            i1 = intersectSwapped(t1Swapped, 0, V1, dV1);
            i2 = intersectSwapped(t1Swapped, 1, V1, dV1);
            intersects = true;
        }

        if (intersects) {
            return true;
        }

        // The triangles don't intersect
        return false;
    }
}