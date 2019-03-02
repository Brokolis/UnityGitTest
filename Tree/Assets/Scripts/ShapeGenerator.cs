using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
public class ShapeGenerator : MonoBehaviour {
    public enum MeshShape {
        Triangle,
        TriangleWithZ,
        Quad,
        Tetrahedron
    }

    public MeshShape shape = MeshShape.Triangle;

    private MeshFilter meshFilter = null; // Change the world!
    private MeshShape? currentShape = null;

    // Start is called before the first frame update
    void Start() {
        meshFilter = GetComponent<MeshFilter>();
    }

    void Update() {
        if (currentShape.HasValue && currentShape.Value == shape) {
            return;
        }
        
        meshFilter = GetComponent<MeshFilter>();
        
        switch (shape) {
            case MeshShape.Triangle:
                meshFilter.mesh = GenerateTriangle();
                break;
            case MeshShape.TriangleWithZ:
                meshFilter.mesh = GenerateTriangleWithZ();
                break;
            case MeshShape.Quad:
                meshFilter.mesh = GenerateQuad();
                break;
            case MeshShape.Tetrahedron:
                meshFilter.mesh = GenerateTetrahedron();
                break;
        }
    }

    Mesh GenerateTriangle() {
        return new Mesh {
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
    }
    
    Mesh GenerateTriangleWithZ() {
        return new Mesh {
            vertices = new[] {
                new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f),
                new Vector3(0f, 1f, 1f),
            },

            uv = new[] {
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 1f),
            },

            triangles = new[] {
                0, 1, 2,
            }
        };
    }

    Mesh GenerateQuad() {
        return new Mesh {
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
    }

    Mesh GenerateTetrahedron() {
        return new Mesh {
            vertices = new[] {
                new Vector3(0f, 0f, 0f), new Vector3(1f, 0f, 0f),
                new Vector3(0f, 1f, 0f), new Vector3(0f, 0f, -1f),
            },

            uv = new[] {
                new Vector2(0f, 0f), new Vector2(1f, 0f),
                new Vector2(0f, 1f), new Vector2(1f, 1f),
            },

            triangles = new[] {
                0, 1, 2,
                0, 3, 1,
                0, 2, 3,
                2, 1, 3,
            }
        };
    }
}