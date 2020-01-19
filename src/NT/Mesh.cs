using System;
using System.IO;
using SharpDX;

namespace NT
{
    public struct SkeletonJoint {
        public string name;
        public int parent;
        public Vector3 localPosition;
        public Quaternion localRotation;
        public Vector3 localScale;
    }

    public class Skeleton {
        public string name;
        public SkeletonJoint[] joints;
    }

    public struct SubMesh {
        public int materialIndex;
        public int indexOffset;
        public int numIndices;
        public int vertexOffset;
        public int numVertices;
        public BoundingBox boundingBox;
    }

    public sealed class Mesh {
        public string name;
        public Vector3[] positions;
        public Vector3[] normals;
        public Vector4[] tangents;
        public Vector2[] uvSet0;
        public UInt32[] jointIndices;
        public Vector4[] jointWeights;
        public Color[] vertexColors;
        public UInt16[] indices;
        public UInt32[] indices32;
        public SubMesh[] subMeshes;
        public Matrix[] bindposes;
        public BoundingBox boundingBox;
        public int materialIndex;
        public Veldrid.IndexFormat indexFormat;

        // native datas
        public int numVertices;
        public int numIndices;
        public int nativeVertexBufferSize;
        public int nativeIndexBufferSize;
        public IntPtr nativeVerticesPacked0;
        public IntPtr nativeVerticesPacked1;
        public IntPtr nativeIndices;

        public void RecalculateBounds() {
            if(positions != null) {
                boundingBox = BoundingBox.FromPoints(positions);
            }
        }
    }

    public struct MeshSurface {
        public Material material;
        public SubMesh drawInfo;

        public MeshSurface(Material[] _materials, SubMesh _drawInfo) {
            material = _materials[_drawInfo.materialIndex];
            drawInfo = _drawInfo;
        }
    }
}