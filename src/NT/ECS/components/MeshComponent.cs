using System;
using System.Collections.Generic;
using SharpDX;

namespace NT
{
    public sealed class SkeletonComponent {
        [Flags]
        public enum Flags {
            Empty,
            Dirty = 1 << 0
        }

        public Flags flags {get; private set;}
        public Entity[] joints {get; private set;}
        public Entity rootJoint {get; private set;}
        public Matrix[] bindPoses {get; private set;}

        internal void SetDirty(bool value = true) {
            if(value) {
                flags |= Flags.Dirty;
            } else {
                flags &= ~Flags.Dirty;
            }
        }

        public bool IsDirty() {return flags.HasFlag(Flags.Dirty);}

        public void SetJoints(Entity[] jointEntities, SkeletonJoint[] skeletonJoints, Matrix[] skeletonBindposes) {
            if(jointEntities != null && skeletonJoints != null /*&& skeletonJoints.Length == jointEntities.Length*/) {
                joints = jointEntities;
                bindPoses = skeletonBindposes;// new Matrix[joints.Length];
                matrices = new Matrix[bindPoses.Length];
                rootJoint = jointEntities[0];
                //for(int i = 0; i < joints.Length; i++) {
                //    Matrix poseMatrix = Matrix.Scaling(skeletonJoints[i].localScale) * Matrix.RotationQuaternion(skeletonJoints[i].localRotation) * Matrix.Translation(skeletonJoints[i].localPosition);
                //    bindPoses[i] = poseMatrix;
                //}
                SetDirty();
            }
        }

        public void SetJoints(Entity[] jointEntities, Matrix[] skeletonBindposes) {
            if(jointEntities != null && skeletonBindposes != null /*&& skeletonJoints.Length == jointEntities.Length*/) {
                joints = jointEntities;
                bindPoses = skeletonBindposes;
                matrices = new Matrix[bindPoses.Length];       
                rootJoint = joints[0];         
                SetDirty();
            }
        }

        // frame data
        internal Matrix[] matrices;
    }

    public sealed class MeshComponent {
        public enum Flags {
            Empty,
            Renderable = 1 << 0,
            Skinned = 1 << 1,
            Dynamic = 1 << 2,
            LightmapStatic = 1 << 3,
            CastShadow = 1 << 4,
            Enabled = 1 << 5,
        }

        public Flags flags {get; private set;}
        public BoundingBox localBoundingBox;
        public Entity skeletonID {get; internal set;}
        public Entity rootJoint {get; internal set;}
        public int lightmapIndex {get; private set;}
        public Vector4 lightmapScaleOffset {get; private set;}
        internal MeshSurface[] surfaces {get; private set;}
        internal Tuple<VertexFactory, IndexBuffer, Matrix[]> runtimeMesh {get; private set;}

        public bool IsEmpty() => flags == Flags.Empty;
        public bool IsStatic() => !flags.HasFlag(Flags.Dynamic);
        public bool IsEnabled() => flags.HasFlag(Flags.Enabled);
        public bool IsSkinned() => flags.HasFlag(Flags.Skinned);
        public bool IsCastShadow() => flags.HasFlag(Flags.CastShadow);
        public bool IsLightmapStatic() => flags.HasFlag(Flags.LightmapStatic);

        internal int numPositionStreams;
        // frame data
        internal uint frameCount;
        internal int instanceOffset;
        internal BoundingBox boundingBox {get; private set;}

        public MeshComponent() {
            flags = Flags.Empty;
            lightmapIndex = -1;
            lightmapScaleOffset = Vector4.Zero;
            numPositionStreams = 1;
        }

        public void SetDynamic() {
            flags |= Flags.Dynamic;
            lightmapIndex = -1;
            lightmapScaleOffset = Vector4.Zero;
        }

        public void SetCastShadow(bool value) {
            if(value) {
                flags |= Flags.CastShadow;
            } else {
                flags &= ~Flags.CastShadow;
            }            
        }

        public void SetEnabled(bool value) {
            if(value) {
                flags |= Flags.Enabled;
            } else {
                flags &= ~Flags.Enabled;
            }
        }

        public void SetLocalBounds(BoundingBox boundingBox) {
            localBoundingBox = boundingBox;
        }

        public void UpdateLightmapData(int lightmapIndex, Vector4 lightmapScaleOffset) {
            if(!IsEmpty() && lightmapIndex >= 0 && IsStatic() && surfaces != null) {
                this.lightmapIndex = lightmapIndex;
                this.lightmapScaleOffset = lightmapScaleOffset;
                this.flags |= Flags.LightmapStatic;
            }
        }
/*
        public void UpdateLightmapData(Image2D lightmapDir, Image2D lightmapColor) {
            if(!IsEmpty() && lightmapIndex >= 0 && IsLightmapStatic() && surfaces != null) {
                for(int i = 0; i < surfaces.Length; i++) {
                    surfaces[i].material.SetFloat4("_Lightmap_ST", lightmapScaleOffset);
                    surfaces[i].material.SetImage("_LightmapDirTex", lightmapDir);
                    surfaces[i].material.SetImage("_LightmapColorTex", lightmapColor);
                }
            }
        }

        public void UpdateLightmapData(int lightmapIndex, Vector4 lightmapScaleOffset, Image2D lightmapDir, Image2D lightmapColor) {
            if(!IsEmpty() && lightmapIndex >= 0 && IsStatic() && surfaces != null) {
                this.lightmapIndex = lightmapIndex;
                this.lightmapScaleOffset = lightmapScaleOffset;
                this.flags |= Flags.LightmapStatic;
                for(int i = 0; i < surfaces.Length; i++) {
                    surfaces[i].material.SetFloat4("_Lightmap_ST", lightmapScaleOffset);
                    surfaces[i].material.SetImage("_LightmapDirTex", lightmapDir);
                    surfaces[i].material.SetImage("_LightmapColorTex", lightmapColor);
                }
            }
        }
*/
        public void SetRenderData(RuntimeMesh registered, Material[] sharedMaterials) {
            if(IsEmpty()) {
                this.runtimeMesh = registered.data;
                surfaces = registered.subMeshes != null ? new MeshSurface[registered.subMeshes.Length] : new MeshSurface[1];
                if(registered.subMeshes != null) {
                    for(int i = 0; i < surfaces.Length; i++) {
                        surfaces[i] = new MeshSurface(sharedMaterials, registered.subMeshes[i]);
                    }
                } else {
                    surfaces[0].material = sharedMaterials[0];
                    surfaces[0].drawInfo.vertexOffset = 0;
                    surfaces[0].drawInfo.indexOffset = 0;
                    surfaces[0].drawInfo.numVertices = registered.numVertices;
                    surfaces[0].drawInfo.numIndices = registered.numIndices;
                    surfaces[0].drawInfo.boundingBox = registered.boundingBox;
                }
                localBoundingBox = registered.boundingBox;
                boundingBox = localBoundingBox;
                flags |= Flags.Renderable;
            }
        }

        public void SetRenderData(Mesh mesh, Material[] sharedMaterials, Type vertexFactory, string registerName = null) {
            if(IsEmpty()) {
                IndexBuffer indexBuffer = null;
                VertexFactory vertexFactoryInstance = (VertexFactory)Activator.CreateInstance(vertexFactory, mesh, Scene.GlobalScene.meshRenderSystem);
                if(mesh.indices != null) {
                    if(mesh.indexFormat == Veldrid.IndexFormat.UInt16) {
                        indexBuffer = new IndexBuffer16();
                        int indexDataSize = sizeof(UInt16) * mesh.indices.Length;
                        DataStream indexData = new DataStream(Utilities.AllocateMemory(indexDataSize), indexDataSize, true, true);
                        indexData.WriteRange(mesh.indices);
                        indexBuffer.InitData(indexData.DataPointer, indexDataSize);
                    } else {
                        indexBuffer = new IndexBuffer32();
                        int indexDataSize = sizeof(UInt16) * mesh.indices.Length;
                        DataStream indexData = new DataStream(Utilities.AllocateMemory(indexDataSize), indexDataSize, true, true);
                        indexData.WriteRange(mesh.indices32);
                        indexBuffer.InitData(indexData.DataPointer, (int)indexData.Length);
                    }
                } else if(mesh.nativeIndices != IntPtr.Zero) {
                    indexBuffer = new IndexBuffer(mesh.indexFormat);
                    indexBuffer.InitData(mesh.nativeIndices, mesh.nativeIndexBufferSize);
                }
                surfaces = mesh.subMeshes != null ? new MeshSurface[mesh.subMeshes.Length] : new MeshSurface[1];
                if(mesh.subMeshes != null) {
                    for(int i = 0; i < surfaces.Length; i++) {
                        surfaces[i] = new MeshSurface(sharedMaterials, mesh.subMeshes[i]);
                    }
                } else {
                    surfaces[0].material = sharedMaterials[mesh.materialIndex];
                    surfaces[0].drawInfo.vertexOffset = 0;
                    surfaces[0].drawInfo.indexOffset = 0;
                    surfaces[0].drawInfo.numVertices = mesh.positions != null ? mesh.positions.Length : mesh.numVertices;
                    surfaces[0].drawInfo.numIndices = mesh.indices != null ? mesh.indices.Length : mesh.numIndices;
                    surfaces[0].drawInfo.boundingBox = mesh.boundingBox;
                }
                runtimeMesh = Scene.GlobalScene.meshRenderSystem.RegisterRuntimeMesh(registerName, vertexFactoryInstance, indexBuffer, mesh);
                localBoundingBox = mesh.boundingBox;
                boundingBox = localBoundingBox;
                flags |= Flags.Renderable;
            }
        }

        public void SetSkeleton(Entity skeleton, Entity root) {
            if(!IsEmpty() && skeleton.IsValid()) {
                flags |= Flags.Skinned | Flags.Dynamic;
                skeletonID = skeleton;
                rootJoint = root;
                lightmapIndex = -1;
                lightmapScaleOffset = Vector4.Zero;
            }
        }

        internal void UpdateWorldBounds(Matrix inv, TransformComponent transform) {
            boundingBox = MathHelper.TransformBoundingBox(localBoundingBox, inv);
            //UpdateWorldBounds(transform);          
        }

        internal void UpdateWorldBounds(TransformComponent transform) {
            boundingBox = MathHelper.TransformBoundingBox(localBoundingBox, transform.localToWorldMatrix);
            if(surfaces != null) {
                for(int i = 0; i < surfaces.Length; i++) {
                    surfaces[i].drawInfo.boundingBox = boundingBox;
                }                    
            }            
        }
    }
}