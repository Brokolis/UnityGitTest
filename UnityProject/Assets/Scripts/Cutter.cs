using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Cutter : MonoBehaviour {
    public Mesh Target;

    public Vector3 PlaneNormal;
    public float PlaneDistanceFromOrigin;
    public bool ShowFront;

    private MeshFilter meshFilter;

    // Start is called before the first frame update
    void Start() {
        meshFilter = GetComponent<MeshFilter>();

        GenerateQuad();
        //GenerateTriangle();
        CutInHalfByPlane(PlaneNormal, PlaneDistanceFromOrigin);
    }

    // Adapted from https://gamedevelopment.tutsplus.com/tutorials/how-to-dynamically-slice-a-convex-shape--gamedev-14479
    void CutInHalfByPlane(Vector3 planeNormal, float distanceFromOrigin) {
        const float epsilon = 0.000001f;

        bool InFront(float pointDistance) {
            return pointDistance > epsilon;
        }

        bool Behind(float pointDistance) {
            return pointDistance < -epsilon;
        }

        bool OnPlane(float pointDistance) {
            return pointDistance <= epsilon && pointDistance >= -epsilon;
        }

        Vector3 Intersect(Vector3 point1, Vector3 point2, float distance1, float distance2) {
            return point1 + (distance1 / (distance1 - distance2)) * (point2 - point1);
        }

        Vector2 UV(Vector2 uv1, Vector2 uv2, float distance1, float distance2) {
            return uv1 + (distance1 / (distance1 - distance2)) * (uv2 - uv1);
        }

        List<Vector3> frontVertices = new List<Vector3>();
        List<int> frontTriangles = new List<int>();
        List<Vector2> frontUVs = new List<Vector2>();

        List<Vector3> backVertices = new List<Vector3>();
        List<int> backTriangles = new List<int>();
        List<Vector2> backUVs = new List<Vector2>();

        Vector3[] vertices = Target.vertices;
        int[] triangles = Target.triangles;
        int triangleLength = triangles.Length;
        Vector2[] uvs = Target.uv;

        for (int i = 0; i < triangleLength; i += 3) {
            Vector3[] triangleVertices = {
                vertices[triangles[i + 0]],
                vertices[triangles[i + 1]],
                vertices[triangles[i + 2]],
            };
            Vector2[] triangleUVs = {
                uvs[triangles[i + 0]],
                uvs[triangles[i + 1]],
                uvs[triangles[i + 2]],
            };
            const int verticesCount = 3;

            Vector3 pointA = triangleVertices[verticesCount - 1];
            float distanceA = Vector3.Dot(planeNormal, pointA) - distanceFromOrigin;
            Vector2 uvA = triangleUVs[verticesCount - 1];

            int addedToFront = 0;
            int addedToBack = 0;

            void AddToFront(Vector3 vertex, Vector2 uv) {
                frontTriangles.Add(frontVertices.Count);
                frontVertices.Add(vertex);
                frontUVs.Add(uv);
                addedToFront++;
            }

            void AddToBack(Vector3 vertex, Vector2 uv) {
                backTriangles.Add(backVertices.Count);
                backVertices.Add(vertex);
                backUVs.Add(uv);
                addedToBack++;
            }

            for (int j = 0; j < verticesCount; j++) {
                Vector3 pointB = triangleVertices[j];
                float distanceB = Vector3.Dot(planeNormal, pointB) - distanceFromOrigin;
                Vector2 uvB = triangleUVs[j];

                if (InFront(distanceB)) {
                    if (Behind(distanceA)) {
                        Vector3 intersect = Intersect(pointB, pointA, distanceB, distanceA);
                        AddToFront(intersect, UV(uvB, uvA, distanceB, distanceA));
                        AddToBack(intersect, UV(uvB, uvA, distanceB, distanceA));
                    } else if (OnPlane(distanceA)) {
                        AddToFront(pointA, uvA);
                    }

                    AddToFront(pointB, uvB);
                } else if (Behind(distanceB)) {
                    if (InFront(distanceA)) {
                        Vector3 intersect = Intersect(pointA, pointB, distanceA, distanceB);
                        AddToFront(intersect, UV(uvA, uvB, distanceB, distanceA));
                        AddToBack(intersect, UV(uvA, uvB, distanceB, distanceA));
                    } else if (OnPlane(distanceA)) {
                        AddToBack(pointA, uvA);
                    }

                    AddToBack(pointB, uvB);
                } else if (InFront(distanceA)) {
                    AddToFront(pointB, uvB);
                } else if (Behind(distanceA)) {
                    AddToBack(pointB, uvB);
                }

                if (addedToFront > 3) {
                    frontTriangles.Add(frontVertices.Count - 4);
                    frontTriangles.Add(frontVertices.Count - 2);
                } else if (addedToBack > 3) {
                    backTriangles.Add(backVertices.Count - 4);
                    backTriangles.Add(backVertices.Count - 2);
                }

                pointA = pointB;
                distanceA = distanceB;
                uvA = uvB;
            }
        }

        if (ShowFront && frontVertices.Count > 0) {
            Debug.Log("Vertices:");
            foreach (Vector3 vertex in frontVertices) {
                Debug.Log(vertex);
            }

            Debug.Log("Triangles:");
            foreach (int index in frontTriangles) {
                Debug.Log(index);
            }

            Target.Clear();

            Target.vertices = frontVertices.ToArray();
            Target.triangles = frontTriangles.ToArray();
            Target.uv = frontUVs.ToArray();
        } else if (!ShowFront && backVertices.Count > 0) {
            Debug.Log("Vertices:");
            foreach (Vector3 vertex in backVertices) {
                Debug.Log(vertex);
            }

            Debug.Log("Triangles:");
            foreach (int index in backTriangles) {
                Debug.Log(index);
            }

            Target.Clear();

            Target.vertices = backVertices.ToArray();
            Target.triangles = backTriangles.ToArray();
            Target.uv = backUVs.ToArray();
        }
    }

    void GenerateTriangle() {
        Target = new Mesh {
            vertices = new[] {
                new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f),
                new Vector3(0f, 1f, 0f),
            },

            uv = new[] {
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 1f),
            },

            triangles = new[] {
                0, 1, 2,
            }
        };

        meshFilter.mesh = Target;
    }

    void GenerateQuad() {
        Target = new Mesh {
            vertices = new[] {
                new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f),
                new Vector3(0f, 1f, 0f), new Vector3(1f, 1f, 0f),
            },

            uv = new[] {
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 1f), new Vector2(1f, 1f),
            },

            triangles = new[] {
                0, 1, 2,
                1, 3, 2,
            }
        };

        meshFilter.mesh = Target;
    }
}