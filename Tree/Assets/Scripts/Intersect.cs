using System;
using System.Collections.Generic;
using UnityEngine;

public class Intersect : MonoBehaviour {
    public MeshFilter targetMeshFilter;
    public MeshFilter subtractMeshFilter;


    /** jhjhjhjh */
    private MeshFilter meshFilter;

    void Start() {
        meshFilter = GetComponent<MeshFilter>();
    }

    void Update() {
        Update_CutMesh();
    }

    void Update_CutMesh() {
        /***/
        meshFilter.mesh = CutMesh(targetMeshFilter, subtractMeshFilter);
    }

    Mesh CutMesh(MeshFilter targetMeshFilter, MeshFilter cuttingMeshFilter) {
        const float epsilon = 0.000001f;

        bool floatEquals(float n1, float n2) => Math.Abs(n2 - n1) < epsilon;

        bool vectorEquals(Vector3 vector1, Vector3 vector2)
            => floatEquals(vector1.x, vector2.x) && floatEquals(vector1.y, vector2.y) && floatEquals(vector1.z, vector2.z);

        int findIndex<T>(IEnumerable<T> list, T item) {
            int i = 0;

            foreach (T listItem in list) {
                if (listItem.Equals(item)) {
                    return i;
                }

                i++;
            }

            return -1;
        }

        Mesh targetMesh = targetMeshFilter.mesh;
        Mesh cuttingMesh = cuttingMeshFilter.mesh;

        Vector3[] targetVertices = targetMesh.vertices;
        int[] targetTriangles = targetMesh.triangles;

        Vector3[] cuttingVertices = cuttingMesh.vertices;
        int[] cuttingTriangles = cuttingMesh.triangles;

        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        for (int i = 0; i < targetTriangles.Length; i += 3) {
            int lastTriangleCount = triangles.Count;
            bool[] triangleIntersectedEdges = new bool[3];

            Vector3[] targetTriangleVertices = {
                targetMeshFilter.transform.TransformPoint(targetVertices[targetTriangles[i + 0]]),
                targetMeshFilter.transform.TransformPoint(targetVertices[targetTriangles[i + 1]]),
                targetMeshFilter.transform.TransformPoint(targetVertices[targetTriangles[i + 2]])
            };

            for (int j = 0; j < cuttingTriangles.Length; j += 3) {
                Vector3[] cutterTriangleVertices = {
                    cuttingMeshFilter.transform.TransformPoint(cuttingVertices[cuttingTriangles[j + 0]]),
                    cuttingMeshFilter.transform.TransformPoint(cuttingVertices[cuttingTriangles[j + 1]]),
                    cuttingMeshFilter.transform.TransformPoint(cuttingVertices[cuttingTriangles[j + 2]])
                };

                if (GetIntersect(targetTriangleVertices, cutterTriangleVertices, out Vector3[] upperV, out int intersectedEdge)) {
                    int lastIndex = vertices.Count;

                    vertices.AddRange(upperV);

                    triangles.Add(lastIndex + 0);
                    triangles.Add(lastIndex + 1);
                    triangles.Add(lastIndex + 2);

                    if (intersectedEdge != -1) {
                        triangleIntersectedEdges[intersectedEdge] = true;
                    }
                }
            }

            Vector3?[] possibleConnections = new Vector3?[6];

            for (int j = lastTriangleCount; j < vertices.Count; j += 3) {
                int vertexIndex = findIndex(targetTriangleVertices, vertices[j]);
                int nextVertexIndex;

                if (vertexIndex == 0) {
                    nextVertexIndex = 5;
                } else {
                    nextVertexIndex = vertexIndex + 2;
                }

                if (!possibleConnections[vertexIndex].HasValue || vectorEquals(possibleConnections[vertexIndex].Value, vertices[j + 2])) {
                    possibleConnections[vertexIndex] = vertices[j + 1];
                }

                if (!possibleConnections[nextVertexIndex].HasValue || vectorEquals(possibleConnections[nextVertexIndex].Value, vertices[j + 1])) {
                    possibleConnections[nextVertexIndex] = vertices[j + 2];
                }
            }

            for (int vertex1Index = 0; vertex1Index < 3; vertex1Index++) {
                if (triangleIntersectedEdges[vertex1Index]) continue;

                bool possible1 = possibleConnections[vertex1Index].HasValue;
                bool possible2 = possibleConnections[vertex1Index + 3].HasValue;

                int chosenPossible = -1;

                if (possible1 && !possible2 ||
                    possible1 && vectorEquals(possibleConnections[vertex1Index].Value, possibleConnections[vertex1Index + 3].Value)) {
                    chosenPossible = vertex1Index;
                } else if (!possible1 && possible2) {
                    chosenPossible = vertex1Index + 3;
                }

                if (chosenPossible != -1) {
                    int vertex2Index;

                    if (vertex1Index == 2) {
                        vertex2Index = 0;
                    } else {
                        vertex2Index = vertex1Index + 1;
                    }

                    int lastIndex = vertices.Count;

                    vertices.Add(targetTriangleVertices[vertex1Index]);
                    vertices.Add(targetTriangleVertices[vertex2Index]);
                    vertices.Add(possibleConnections[chosenPossible].Value);

                    triangles.Add(lastIndex + 0);
                    triangles.Add(lastIndex + 1);
                    triangles.Add(lastIndex + 2);
                }
            }
        }

        return new Mesh {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray()
        };
    }

    // Adapted from: https://web.stanford.edu/class/cs277/resources/papers/Moller1997b.pdf
    // TODO: This might not work correctly when one or more points are directly on one of the planes
    bool GetIntersect(Vector3[] targetVertices, Vector3[] cutterVertices, out Vector3[] upperV, out int intersectedEdge) {
        const float epsilon = 0.000001f;

        upperV = new Vector3[0];
        intersectedEdge = -1;

        // Create copies of vertices, because the vertices array might be modified (rotated)

        Vector3[] V1 = {
            targetVertices[0],
            targetVertices[1],
            targetVertices[2]
        };

        Vector3[] V2 = {
            cutterVertices[0],
            cutterVertices[1],
            cutterVertices[2]
        };

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
        // the two others is in the middle.
        // Also, check if one of the points isn't just on the plane itself.

        int V1rotation = 0;
        if (sV1[1] * sV1[2] > 0) {
            if (sV1[0] == 0) {
                return false;
            }

            V1rotation = -1;

            rotate3(V1, true);
            rotate3(dV1, true);
            rotate3(sV1, true);
        } else if (sV1[0] * sV1[1] > 0) {
            if (sV1[2] == 0) {
                return false;
            }

            V1rotation = 1;

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

        // Check for intersection

        bool betweenInclusive(float number, float[] bounds) =>
            bounds[0] > bounds[1] ? number >= bounds[1] && number <= bounds[0] : number >= bounds[0] && number <= bounds[1];

        bool betweenExclusive(float number, float[] bounds) =>
            bounds[0] > bounds[1] ? number > bounds[1] && number < bounds[0] : number > bounds[0] && number < bounds[1];

        Vector3 intersect(int index, Vector3[] V, float[] d) =>
            V[index] + (V[index + 1] - V[index]) * d[index] / (d[index] - d[index + 1]);

        int getIntersectedV1Edge(int edgeIndex) {
            int edge = edgeIndex + V1rotation;
            return edge < 0 ? 2 : edge > 2 ? 0 : edge;
        }

        bool intersects = false;
        Vector3 i1 = Vector3.zero, i2 = Vector3.zero;

        if (betweenExclusive(t2[0], t1)) {
            i1 = intersect(0, V2, dV2);

            if (betweenExclusive(t2[1], t1)) {
                if (t1[0] < t1[1] && t2[1] > t2[0] || t1[0] > t1[1] && t2[1] < t2[0]) {
                    i2 = i1;
                    i1 = intersect(1, V2, dV2);
                } else {
                    i2 = intersect(1, V2, dV2);
                }
            } else if (betweenInclusive(t1[0], t2)) {
                i2 = intersect(0, V1, dV1);
                intersectedEdge = getIntersectedV1Edge(0);
            } else { // if (betweenInclusive(t1[1], t2))
                i2 = i1;
                i1 = intersect(1, V1, dV1);
                intersectedEdge = getIntersectedV1Edge(1);
            }

            intersects = true;
        } else if (betweenExclusive(t2[1], t1)) {
            i1 = intersect(1, V2, dV2);

            if (betweenInclusive(t1[0], t2)) {
                i2 = intersect(0, V1, dV1);
                intersectedEdge = getIntersectedV1Edge(0);
            } else { // if (betweenInclusive(t1[1], t2))
                i2 = i1;
                i1 = intersect(1, V1, dV1);
                intersectedEdge = getIntersectedV1Edge(1);
            }

            intersects = true;
        } else if (betweenInclusive(t1[0], t2)) { // or betweenInclusive(t1[1], t2) - they are both either inside or outside (on same side, thus not touching)
            i1 = intersect(1, V1, dV1);
            i2 = intersect(0, V1, dV1);
            intersectedEdge = getIntersectedV1Edge(0);
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
            upperV = new Vector3[3];

            if (sV1[0] > 0) {
                upperV[0] = V1[0];
            } else {
                upperV[0] = V1[2];
            }

            upperV[1] = i2;
            upperV[2] = i1;
        }

        return true;
    }
}