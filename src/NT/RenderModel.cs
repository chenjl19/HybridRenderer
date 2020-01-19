using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Mathematics;
using ImGuiNET;

namespace NT
{
    public struct ModelSurface {
        public int materialIndex;
        public int vertexOffset;
        public int numVertices;
        public int indexOffset;
        public int numIndices;

        public ModelSurface(int _materialIndex, int _vertexOffset, int _numVertices, int _indexOffset, int _numIndices) {
            materialIndex = _materialIndex;
            vertexOffset = _vertexOffset;
            numVertices = _numVertices;
            indexOffset = _indexOffset;
            numIndices = _numIndices;
        }
    }

    public class DynamicModelProxy {
        public ModelSurface[] updatedSurfaces;
        public Matrix[] updatedJoints;
        public BoundingBox updatedWorldBounds;
    }

    public abstract class RenderModel {
        [Flags]
        public enum ModelFlags {
            None = 0,
            Updated = 1 << 0,
            Destroyed = 1 << 0,
            StaticModel = 1 << 1,
        }

        public string name {get; private set;}
        public ModelFlags flags {get; protected set;}
        public BoundingBox localBounds {get; protected set;}
        public IndexBuffer indexBuffer {get; protected set;}
        public VertexFactory vertexFactory {get; protected set;}
        public List<ModelSurface> modelSurfaces {get; protected set;}
        public int numJoints {get; protected set;}

        public RenderModel(string myName) {
            name = myName;
            modelSurfaces = new List<ModelSurface>();
        }

        public void AddSurface(ModelSurface surface) {
            AddSurface(ref surface);
        }

        public List<ModelSurface> GetCachedSurfaces() {
            return modelSurfaces;
        }

        public void AddSurface(ref ModelSurface surface) {
            if(flags.HasFlag(ModelFlags.Destroyed) || (flags.HasFlag(ModelFlags.StaticModel) && flags.HasFlag(ModelFlags.Updated))) {
                return;
            }

            if(modelSurfaces == null) {
                modelSurfaces = new List<ModelSurface>();
            }

            modelSurfaces.Add(surface);
        }

        public void SetLocalBounds(ref BoundingBox box) {
            if(flags.HasFlag(ModelFlags.Destroyed) || (flags.HasFlag(ModelFlags.StaticModel) && flags.HasFlag(ModelFlags.Updated))) {
                return;
            }

            localBounds = box;
        }

        public void Update() {
            if(flags.HasFlag(ModelFlags.Destroyed) || (flags.HasFlag(ModelFlags.StaticModel) && flags.HasFlag(ModelFlags.Updated))) {
                return;
            }

            flags |= ModelFlags.Updated;
            //Common.renderModelManager.UpdateModel(this);
        }

        public void Destroy() {
            flags |= ModelFlags.Destroyed;
        }

        public bool IsStaticModel() {return flags.HasFlag(ModelFlags.StaticModel);}
        //public abstract DynamicModelProxy InstantiateDynamicModel(RenderModelComponent component, ViewDef view, ref DynamicModelProxy cachedModel);
    }

    public struct RuntimeMesh {
        public Tuple<VertexFactory, IndexBuffer, Matrix[]> data {get; private set;}
        public BoundingBox boundingBox {get; private set;}
        public int numVertices {get; private set;}
        public int numIndices {get; private set;}
        public SubMesh[] subMeshes {get; private set;}

        public RuntimeMesh(Tuple<VertexFactory, IndexBuffer, Matrix[]> inData, Mesh meshData) {
            this.data = inData;
            this.boundingBox = meshData.boundingBox;
            this.subMeshes = meshData.subMeshes;
            this.numVertices = meshData.numVertices;
            this.numIndices = meshData.numIndices;
        }
    }

    public sealed class MeshRenderSystem {
        Queue<Tuple<VertexFactory, IndexBuffer, Matrix[]>> updateModelsPerFrame;
        Dictionary<string, RuntimeMesh> runtimeMeshes;
        int usedVertexMemory;
        int usedIndexMemory;
        const int VertexBufferMemoryPerFrame = 31 * 1024 * 1024;
        const int IndexBufferMemoryPerFrame = 31 * 1024 * 1024;

        RenderBuffer vertexBuffer;
        RenderBuffer indexBuffer;

        public MeshRenderSystem() {
            updateModelsPerFrame = new Queue<Tuple<VertexFactory, IndexBuffer, Matrix[]>>();
            runtimeMeshes = new Dictionary<string, RuntimeMesh>();
        }

        public VertexBuffer AllocVertexBuffer() {
            return null;
        }

        public void AllocIndexBuffer() {}

        public bool FindRuntimeMesh(string name, out RuntimeMesh registered) {
            return runtimeMeshes.TryGetValue(name, out registered);
        }

        public Tuple<VertexFactory, IndexBuffer, Matrix[]> RegisterRuntimeMesh(string name, VertexFactory vertexFactory, IndexBuffer indexBuffer, Mesh meshData) {
            var mesh = new Tuple<VertexFactory, IndexBuffer, Matrix[]>(vertexFactory, indexBuffer, meshData.bindposes);
            updateModelsPerFrame.Enqueue(mesh);
            runtimeMeshes.Add(name, new RuntimeMesh(mesh, meshData));
            return mesh;
        }

        public void UpdateMeshes_RenderThread(Veldrid.CommandList commandList) {
            if(updateModelsPerFrame.Count == 0) {
                return;
            }
            if(vertexBuffer == null) {
                vertexBuffer = new RenderBuffer(VertexBufferMemoryPerFrame, 1, Veldrid.BufferUsage.VertexBuffer);
                vertexBuffer.InitDeviceResource();
            }
            if(indexBuffer == null) {
                indexBuffer = new RenderBuffer(IndexBufferMemoryPerFrame, 1, Veldrid.BufferUsage.IndexBuffer);
                indexBuffer.InitDeviceResource();
            }
            int num = updateModelsPerFrame.Count;
            for(int i = 0; i < num; i++) {
                var mesh = updateModelsPerFrame.Dequeue();
                VertexFactory vertexFactory = mesh.Item1;
                for(int v = 0; v < vertexFactory.NumBuffers(); v++) {
                    VertexBuffer vb = vertexFactory.GetBuffer(v);
                    vb.Reference(vertexBuffer, usedVertexMemory);
                    vb.InitDeviceResource();
                    vb.ReleaseLocalResource();
                    usedVertexMemory += vb.sizeInBytes;
                }
                IndexBuffer ib = mesh.Item2;
                if(ib != null) {
                    ib.Reference(indexBuffer, usedIndexMemory);
                    ib.InitDeviceResource();
                    ib.ReleaseLocalResource();
                    usedIndexMemory += ib.sizeInBytes;
                }
            }
            usedVertexMemory = 0;
            usedIndexMemory = 0;
        }
    }
}