﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Serialization;

public class Intersect : MonoBehaviour {
    public MeshFilter targetMeshFilter;
    public MeshFilter subtractMeshFilter;

    private MeshFilter meshFilter;
    private Light light;

    public Vector3 i1, i2;

    private event Action gizmos;

    void Start() {
        meshFilter = GetComponent<MeshFilter>();
        light = GetComponent<Light>();
    }

    void Update() {
        //Update_CutSingleTriangles();
        Update_CutMesh();
    }

    void Update_CutMesh() {
        meshFilter.mesh = CutMesh(targetMeshFilter, subtractMeshFilter);
    }

    void Update_CutSingleTriangles() {
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

        Vector3[] upperV = null;
        gizmos = null;

        bool intersects = GetIntersect(V1, V2, ref upperV);

        light.enabled = intersects;

        if (intersects) {
            Debug.DrawLine(i1, i2, new Color(0f, 1f, 0f, 0.5f), 0f, false);

            if (upperV != null) {
                DrawPolygon(upperV, new Color(1f, 0f, 0f, 0.5f));
            }
        }
    }

    void OnDrawGizmos() {
        gizmos?.Invoke();
    }

    void DrawPolygon(Vector3[] vertices, Color color) {
        Color[] vertexColors = {
            new Color(1f, 0f, 0f, 0.5f), new Color(0f, 1f, 0f, 0.5f), new Color(0f, 0f, 1f, 0.5f), new Color(0f, 1f, 1f, 0.5f)
        };

        Vector3 prevVertex = vertices.Last();

        for (int i = 0; i < vertices.Length; i++) {
            Vector3 currentVertex = vertices[i];
            Debug.DrawLine(prevVertex, currentVertex, color, 0f, false);
            int index = i;
            gizmos += () => {
                Gizmos.color = vertexColors[index];
                Gizmos.DrawSphere(currentVertex, 0.03f);
            };
            prevVertex = currentVertex;
        }
    }

    Mesh CutMesh(MeshFilter targetMeshFilter, MeshFilter cuttingMeshFilter) {
        Mesh targetMesh = targetMeshFilter.mesh;
        Mesh cuttingMesh = cuttingMeshFilter.mesh;
        
        Vector3[] targetVertices = targetMesh.vertices;
        int[] targetTriangles = targetMesh.triangles;

        Vector3[] cuttingVertices = cuttingMesh.vertices;
        int[] cuttingTriangles = cuttingMesh.triangles;
        
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        
        for (int i = 0; i < targetTriangles.Length; i += 3) {
            for (int j = 0; j < cuttingTriangles.Length; j += 3) {
                Vector3[] V1 = {
                    targetMeshFilter.transform.TransformPoint(targetVertices[targetTriangles[i + 0]]),
                    targetMeshFilter.transform.TransformPoint(targetVertices[targetTriangles[i + 1]]),
                    targetMeshFilter.transform.TransformPoint(targetVertices[targetTriangles[i + 2]])
                };

                Vector3[] V2 = {
                    subtractMeshFilter.transform.TransformPoint(cuttingVertices[cuttingTriangles[j + 0]]),
                    subtractMeshFilter.transform.TransformPoint(cuttingVertices[cuttingTriangles[j + 1]]),
                    subtractMeshFilter.transform.TransformPoint(cuttingVertices[cuttingTriangles[j + 2]])
                };
                
                Vector3[] upperV = null;
                
                bool intersects = GetIntersect(V1, V2, ref upperV);

                if (intersects) {
                    vertices.AddRange(upperV);
                    int lastIndex = vertices.Count;
                    
                    if (upperV.Length == 3) {
                        triangles.Add(lastIndex - 3);
                        triangles.Add(lastIndex - 2);
                        triangles.Add(lastIndex - 1);
                    } else if (upperV.Length == 4) {
                        triangles.Add(lastIndex - 4);
                        triangles.Add(lastIndex - 3);
                        triangles.Add(lastIndex - 2);
                        
                        triangles.Add(lastIndex - 4);
                        triangles.Add(lastIndex - 2);
                        triangles.Add(lastIndex - 1);
                    }
                }
            }
        }

        return new Mesh {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray()
        };
    }
    
    // Adapted from: https://web.stanford.edu/class/cs277/resources/papers/Moller1997b.pdf
    // TODO: This might not work correctly when one or more points are directly on one of the planes.
    bool GetIntersect(Vector3[] V1, Vector3[] V2, ref Vector3[] upperV) {
        const float epsilon = 0.000001f;

        // Construct plane 2
        Vector3 N2 = Vector3.Cross(V2[1] - V2[0], V2[2] - V2[0]);
        float d2 = Vector3.Dot(-N2, V2[0]);

        // Get distances between points V1 and plane 2
        float[] dV1 = {
            Vector3.Dot(N2, V1[0]) + d2,
            Vector3.Dot(N2, V1[1]) + d2,
            Vector3.Dot(N2, V1[2]) + d2
        };

        // Get the sign of the distances by assigning -1, 0 or 1
        int[] sV1 = {
            dV1[0] < -epsilon ? -1 : dV1[0] > epsilon ? 1 : 0,
            dV1[1] < -epsilon ? -1 : dV1[1] > epsilon ? 1 : 0,
            dV1[2] < -epsilon ? -1 : dV1[2] > epsilon ? 1 : 0
        };

        // Early rejection
        if (sV1[0] >= 0 && sV1[1] >= 0 && sV1[2] >= 0 || sV1[0] <= 0 && sV1[1] <= 0 && sV1[2] <= 0) {
            return false;
        }

        // Construct plane 1
        Vector3 N1 = Vector3.Cross(V1[1] - V1[0], V1[2] - V1[0]);
        float d1 = Vector3.Dot(-N1, V1[0]);

        // Get distances between points V2 and plane 1
        float[] dV2 = {
            Vector3.Dot(N1, V2[0]) + d1,
            Vector3.Dot(N1, V2[1]) + d1,
            Vector3.Dot(N1, V2[2]) + d1
        };

        // Get the sign of the distances by assigning -1, 0 or 1
        int[] sV2 = {
            dV2[0] < -epsilon ? -1 : dV2[0] > epsilon ? 1 : 0,
            dV2[1] < -epsilon ? -1 : dV2[1] > epsilon ? 1 : 0,
            dV2[2] < -epsilon ? -1 : dV2[2] > epsilon ? 1 : 0
        };

        // Early rejection
        if (sV2[0] >= 0 && sV2[1] >= 0 && sV2[2] >= 0 || sV2[0] <= 0 && sV2[1] <= 0 && sV2[2] <= 0) {
            return false;
        }

        void rotate3<T>(T[] a, bool right) {
            if (right) {
                T temp = a[2];

                a[2] = a[1];
                a[1] = a[0];
                a[0] = temp;
            } else {
                T temp = a[0];

                a[0] = a[1];
                a[1] = a[2];
                a[2] = temp;
            }
        }

        // Order the vertices, so that the one, on the opposite side of the plane to
        // the two others is in the middle

        // Also, check if one of the points isn't just on the plane itself
        if (sV1[1] * sV1[2] > 0) {
            if (sV1[0] == 0) {
                return false;
            }
            
            rotate3(V1, true);
            rotate3(dV1, true);
            rotate3(sV1, true);
        } else if (sV1[0] * sV1[1] > 0) {
            if (sV1[2] == 0) {
                return false;
            }
            
            rotate3(V1, false);
            rotate3(dV1, false);
            rotate3(sV1, false);
        }

        if (sV2[1] * sV2[2] > 0) {
            if (sV2[0] == 0) {
                return false;
            }
            
            rotate3(V2, true);
            rotate3(dV2, true);
            rotate3(sV2, true);
        } else if (sV2[0] * sV2[1] > 0) {
            if (sV2[2] == 0) {
                return false;
            }
            
            rotate3(V2, false);
            rotate3(dV2, false);
            rotate3(sV2, false);
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

        float[] t2 = {
            t(pV2[0], pV2[1], dV2[0], dV2[1]),
            t(pV2[1], pV2[2], dV2[1], dV2[2])
        };

        bool betweenInclusive(float number, float[] bounds) =>
            bounds[0] > bounds[1] ? number >= bounds[1] && number <= bounds[0] : number >= bounds[0] && number <= bounds[1];

        bool betweenExclusive(float number, float[] bounds) =>
            bounds[0] > bounds[1] ? number > bounds[1] && number < bounds[0] : number > bounds[0] && number < bounds[1];
        
        Vector3 intersect(int index, Vector3[] V, float[] d) =>
            V[index] + (V[index + 1] - V[index]) * d[index] / (d[index] - d[index + 1]);

        bool intersects = false;
        Vector3 i1 = Vector3.zero, i2 = Vector3.zero;

        if (betweenInclusive(t2[0], t1)) {
            i1 = intersect(0, V2, dV2);

            if (betweenInclusive(t2[1], t1)) {
                if (t1[0] < t1[1] && t2[1] > t2[0] || t1[0] > t1[1] && t2[1] < t2[0]) {
                    i2 = i1;
                    i1 = intersect(1, V2, dV2);
                } else {
                    i2 = intersect(1, V2, dV2);
                }
            } else if (betweenExclusive(t1[0], t2)) {
                i2 = intersect(0, V1, dV1);
            } else if (betweenExclusive(t1[1], t2)) {
                i2 = i1;
                i1 = intersect(1, V1, dV1);
            }

            intersects = true;
        } else if (betweenInclusive(t2[1], t1)) {
            i1 = intersect(1, V2, dV2);

            if (betweenExclusive(t1[0], t2)) {
                i2 = intersect(0, V1, dV1);
            } else if (betweenExclusive(t1[1], t2)) {
                i2 = i1;
                i1 = intersect(1, V1, dV1);
            }

            intersects = true;
        }
        // or between(t1[1], t2) - they are both either inside or outside (on same side, thus not touching)
        else if (betweenInclusive(t1[0], t2)) {
            i1 = intersect(1, V1, dV1);
            i2 = intersect(0, V1, dV1);
            intersects = true;
        }

        if (!intersects) {
            return false;
        }

        if (sV1[1] > 0) {
            upperV = new Vector3[3];
            upperV[0] = V1[1];
            upperV[1] = i1;
            upperV[2] = i2;
        } else {
            upperV = new Vector3[4];
            upperV[0] = V1[2];
            upperV[1] = V1[0];
            upperV[2] = i2;
            upperV[3] = i1;
        }

        this.i1 = i1;
        this.i2 = i2;

        return true;
    }
}