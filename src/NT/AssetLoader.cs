using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Mathematics;
using ImGuiNET;

namespace NT
{
    struct MD5VertexWeight {
        public int jointIndex;
        public float weight;
        public Vector3 offsets;
    }

    public struct MD5Joint {
        public string name;
        public int parent;
    }

    public struct MD5JointTransform {
        public Quaternion q;
        public Vector3 t;
        public float w;
    }

    public class MD5Mesh {
        public string name;
        public string materialName;
        public int numVertices;
        public int numIndices;
        public int numMeshJoints;
        public float maxJointVertDist;
        public BoundingBox bounds;
        public DrawVertex[] vertices;
        public UInt16[] indices;
    }

    public struct MD5Model {
        public MD5Mesh[] meshes;
        public MD5Joint[] joints;
        public MD5JointTransform[] defaultPose;
        public Matrix[] bindposes;
    }

    public static class AssetLoader {
        public static BinaryImage[] LoadBinaryImgae(string filename) {
            BinaryImage[] imageDatas = null;
            using(System.IO.BinaryReader reader = new System.IO.BinaryReader(System.IO.File.OpenRead(filename))) {
                BinaryImageFile file = new BinaryImageFile();
                file.ReadHeader(reader);
                imageDatas = new BinaryImage[file.numLevels];
                for(int i = 0; i < imageDatas.Length; i++) {
                    imageDatas[i].ReadHeader(reader);
                    imageDatas[i].bytes = reader.ReadBytes((int)imageDatas[i].dataSizeInBytes);
                }
            }
            return imageDatas;
        }

        public static BinaryImageFile LoadBinaryImgaeFile(string filename) {
            BinaryImageFile file;
            using(System.IO.BinaryReader reader = new System.IO.BinaryReader(System.IO.File.OpenRead(filename))) {
                file = new BinaryImageFile();
                file.ReadHeader(reader);
                file.images = new BinaryImage[file.numLevels];
                for(int i = 0; i < file.images.Length; i++) {
                    file.images[i].ReadHeader(reader);
                    file.images[i].bytes = reader.ReadBytes((int)file.images[i].dataSizeInBytes);
                }
            }
            return file;
        }

        public static void CreateShaders(string filename, Action<byte[]> callback) {
            if(File.Exists(filename)) {
                Byte[] bytes = File.ReadAllBytes(filename);
                callback?.Invoke(bytes);
            }
        }

        static void ReadMeshData(Assimp.Mesh aiMesh, float assimpImportScale, DataStream vertexBuffer0, DataStream vertexBuffer1, DataStream indexBuffer, out BoundingBox boundingBox) {
            DrawVertexPacked0 packed0 = new DrawVertexPacked0();
            DrawVertexPacked1 packed1 = new DrawVertexPacked1();
            bool hasVertexColor = aiMesh.HasVertexColors(0);
            boundingBox = MathHelper.NullBoundingBox;
            for(int v = 0; v < aiMesh.VertexCount; v++) {
                packed0.position = new Vector3(aiMesh.Vertices[v].X, -aiMesh.Vertices[v].Z, aiMesh.Vertices[v].Y) * assimpImportScale;
                packed0.st = new Half2(aiMesh.TextureCoordinateChannels[0][v].X, aiMesh.TextureCoordinateChannels[0][v].Y);
                Vector3 normal = new Vector3(aiMesh.Normals[v].X, -aiMesh.Normals[v].Z, aiMesh.Normals[v].Y);
                Vector3 tangent = new Vector3(aiMesh.Tangents[v].X, -aiMesh.Tangents[v].Z, aiMesh.Tangents[v].Y);
                Vector3 bitangent = new Vector3(aiMesh.BiTangents[v].X, -aiMesh.BiTangents[v].Z, aiMesh.BiTangents[v].Y);
                packed1.SetNormal(normal);
                packed1.SetTangent(tangent);
                packed1.SetBitangentSign((Vector3.Dot(Vector3.Cross(normal, tangent), bitangent) < 0.0f) ? -1.0f : 1.0f);
                if(hasVertexColor) {
                    packed1.color = new Byte4(aiMesh.VertexColorChannels[0][v].R, aiMesh.VertexColorChannels[0][v].G, aiMesh.VertexColorChannels[0][v].B, aiMesh.VertexColorChannels[0][v].A);
                } else {
                    packed1.color = new Byte4(255, 255, 255, 255);
                }
                vertexBuffer0.Write(packed0);
                vertexBuffer1.Write(packed1);

                MathHelper.BoundingBoxAddPoint(ref boundingBox, packed0.position);
            }
            for(int i = 0; i < aiMesh.FaceCount; i++) {
                indexBuffer.Write((UInt16)aiMesh.Faces[i].Indices[0]);
                indexBuffer.Write((UInt16)aiMesh.Faces[i].Indices[1]);
                indexBuffer.Write((UInt16)aiMesh.Faces[i].Indices[2]);
            }
        }

        static void ReadMeshData(Assimp.Mesh aiMesh, float assimpImportScale, ref SubMesh subMesh, DataStream vertexBuffer0, DataStream vertexBuffer1, DataStream indexBuffer) {
            DrawVertexPacked0 packed0 = new DrawVertexPacked0();
            DrawVertexPacked1 packed1 = new DrawVertexPacked1();
            bool hasVertexColor = aiMesh.HasVertexColors(0);
            for(int v = 0; v < aiMesh.VertexCount; v++) {
                packed0.position = new Vector3(aiMesh.Vertices[v].X, -aiMesh.Vertices[v].Z, aiMesh.Vertices[v].Y) * assimpImportScale;
                packed0.st = new Half2(aiMesh.TextureCoordinateChannels[0][v].X, aiMesh.TextureCoordinateChannels[0][v].Y);
                Vector3 normal = new Vector3(aiMesh.Normals[v].X, -aiMesh.Normals[v].Z, aiMesh.Normals[v].Y);
                Vector3 tangent = new Vector3(aiMesh.Tangents[v].X, -aiMesh.Tangents[v].Z, aiMesh.Tangents[v].Y);
                Vector3 bitangent = new Vector3(aiMesh.BiTangents[v].X, -aiMesh.BiTangents[v].Z, aiMesh.BiTangents[v].Y);
                packed1.SetNormal(normal);
                packed1.SetTangent(tangent);
                packed1.SetBitangentSign((Vector3.Dot(Vector3.Cross(normal, tangent), bitangent) < 0.0f) ? -1.0f : 1.0f);
                if(hasVertexColor) {
                    packed1.color = new Byte4(aiMesh.VertexColorChannels[0][v].R, aiMesh.VertexColorChannels[0][v].G, aiMesh.VertexColorChannels[0][v].B, aiMesh.VertexColorChannels[0][v].A);
                } else {
                    packed1.color = new Byte4(255, 255, 255, 255);
                }
                vertexBuffer0.Write(packed0);
                vertexBuffer1.Write(packed1);

                MathHelper.BoundingBoxAddPoint(ref subMesh.boundingBox, packed0.position);
            }
            for(int i = 0; i < aiMesh.FaceCount; i++) {
                indexBuffer.Write((UInt16)aiMesh.Faces[i].Indices[0]);
                indexBuffer.Write((UInt16)aiMesh.Faces[i].Indices[1]);
                indexBuffer.Write((UInt16)aiMesh.Faces[i].Indices[2]);
            }
        }

        static void ParseIndex(Lexer lexer, UInt16[] indices, int offset) {
            indices[offset] = (UInt16)lexer.ParseInteger();
        }

        static void ParseIndex(Lexer lexer, UInt32[] indices, int offset) {
            indices[offset] = (UInt32)lexer.ParseInteger();
        }

        public static Skeleton LoadTextSkeleton(string filename) {
            if(!File.Exists(filename)) {
                return null;
            }

            Token token = new Token();
            Lexer lex = new Lexer(File.ReadAllText(filename));

            lex.ReadToken(ref token);
            lex.ExpectTokenString("{");
            lex.ExpectTokenString("NumJoints");
            Skeleton skeleton = new Skeleton();
            skeleton.name = token.lexme;
            skeleton.joints = new SkeletonJoint[lex.ParseInteger()];
            for(int i = 0; i < skeleton.joints.Length; i++) {
                lex.ReadToken(ref token);
                skeleton.joints[i].name = token.lexme;
                skeleton.joints[i].parent = lex.ParseInteger();
                lex.ParseCSharpVector(ref skeleton.joints[i].localPosition);
                lex.ParseCSharpVector(ref skeleton.joints[i].localRotation);
                lex.ParseCSharpVector(ref skeleton.joints[i].localScale);
            }     

            return skeleton;
        }

        static void ParseMD5Joint(Lexer lex, int index, MD5Joint[] md5Joints, MD5JointTransform[] defaultPose, float scale) {
            MD5Joint joint = new MD5Joint();

            Token token = new Token();
            lex.ExpectTokenType(TokenType.String, TokenSubType.None, ref token);
            joint.name = token.lexme;
            int parent = lex.ParseInteger();
            if(parent < 0) {
                joint.parent = 0;
            } else {
                if(parent >= md5Joints.Length - 1) {
                    throw new InvalidDataException($"Invalid parent for joint {joint.name}");
                }
                joint.parent = parent;
            }

            md5Joints[index] = joint;

            lex.ParseVector(ref defaultPose[index].t);
            Vector3 cq = Vector3.Zero;
            lex.ParseVector(ref cq);
            Quaternion q = cq.ToQuaternion();
            defaultPose[index].t *= scale;
            defaultPose[index].w = q.W;
            defaultPose[index].q = q;
        }

        static void SwapValue(ref int a, ref int b) {
            int tmp = a;
            a = b;
            b = tmp;
        }

        static MD5Mesh ParseMD5Mesh(Lexer lex, int numJoints, Matrix[] poseMatrices, float scale) {
            const int MaxWeightsPerVertex = 4;
            
            Token token = new Token();
            MD5Mesh mesh = new MD5Mesh();

            lex.ExpectTokenString("{");
            if(lex.CheckTokenString("name", ref token)) {
                mesh.name = token.lexme;
            }

            lex.ExpectTokenString("shader");
            lex.ReadToken(ref token);
            mesh.materialName = token.lexme;

            lex.ExpectTokenString("numverts");
            mesh.numVertices = lex.ParseInteger();

            Vector2[] texcoords = new Vector2[mesh.numVertices];
            int[] firstWeightForVertex = new int[mesh.numVertices];
            int[] numWeightForVertex = new int[mesh.numVertices];

            for(int i = 0; i < mesh.numVertices; i++) {
                lex.ExpectTokenString("vert");
                lex.ParseInteger();

                lex.ParseVector(ref texcoords[i]);
                firstWeightForVertex[i] = lex.ParseInteger();
                numWeightForVertex[i] = lex.ParseInteger();
            }

            lex.ExpectTokenString("numtris");
            mesh.numIndices = lex.ParseInteger() * 3;
            UInt16[] indices = new ushort[mesh.numIndices];
            for(int i = 0; i < mesh.numIndices; i += 3) {
                lex.ExpectTokenString("tri");
                lex.ParseInteger();
                indices[i + 0] = (ushort)lex.ParseInteger();
                indices[i + 1] = (ushort)lex.ParseInteger();
                indices[i + 2] = (ushort)lex.ParseInteger();
            }
            mesh.indices = indices;

            lex.ExpectTokenString("numweights");
            int numWeights = lex.ParseInteger();
            MD5VertexWeight[] tempWeights = new MD5VertexWeight[numWeights];
            for(int i = 0; i < numWeights; i++) {
                lex.ExpectTokenString("weight");
                lex.ParseInteger();

                int id = lex.ParseInteger();
                tempWeights[i].jointIndex = id;
                tempWeights[i].weight = lex.ParseFloat();
                lex.ParseVector(ref tempWeights[i].offsets);
                tempWeights[i].offsets *= scale;
            }

            lex.ExpectTokenString("}");

            DrawVertex[] baseVerts = new DrawVertex[texcoords.Length];
            for(int i = 0; i < texcoords.Length; i++) {
                Vector3 v = Vector3.Zero;
                for(int j = 0; j < numWeightForVertex[i]; j++) {
                    int weightIndex = firstWeightForVertex[i] + j;
                    int jointIndex = tempWeights[weightIndex].jointIndex;
                    //Vector3.Transform(ref tempWeights[weightIndex].offsets, ref defaultPose[jointIndex].q, out Vector3 vert);
                    //v += (vert + defaultPose[jointIndex].t) * tempWeights[weightIndex].weight;
                    Vector3.TransformCoordinate(ref tempWeights[weightIndex].offsets, ref poseMatrices[jointIndex], out Vector3 vert);
                    v += vert * tempWeights[weightIndex].weight;
                }
                baseVerts[i].position = v;
                baseVerts[i].st = new Half2(texcoords[i].X, texcoords[i].Y);
            }

            for(int i = 0; i < texcoords.Length; i++) {
                int[] weights = new int[16];
                int weightsForVertex = numWeightForVertex[i];
                for(int j = 0; j < weightsForVertex; j++) {
                    weights[j] = firstWeightForVertex[i] + j;
                }
                // sort
                for(int j = 0; j < weightsForVertex; j++) {
                    for(int k = 0; k < weightsForVertex - 1 - j; k++) {
                        if(tempWeights[weights[k]].weight < tempWeights[weights[k + 1]].weight) {
                            SwapValue(ref weights[k], ref weights[k + 1]);
                        }
                    }
                }
                float totalWeight = 0f;
                for(int j = 0; j < weightsForVertex; j++) {
                    totalWeight += tempWeights[weights[j]].weight;
                }
                if(!MathUtil.IsOne(totalWeight)) {
                    throw new InvalidDataException("totalWeight > 1");
                }

                int numUsedWeights = Math.Min(MaxWeightsPerVertex, weightsForVertex);
                float usedWeight = 0f;
                for(int j = 0; j < numUsedWeights; j++) {
                    usedWeight += tempWeights[weights[j]].weight;
                }  
                float residualWeight = totalWeight - usedWeight;

                int[] finalWeights = new int[MaxWeightsPerVertex];
                int[] finalJointIndices = new int[MaxWeightsPerVertex];
                for(int j = 0; j < numUsedWeights; j++) {
                    int jointIndex = tempWeights[weights[j]].jointIndex;
                    float weight = tempWeights[weights[j]].weight;
                    float normalizedWeight = weight / usedWeight;
                    finalWeights[j] = DrawVertex.FloatToByte(normalizedWeight * 255f);
                    finalJointIndices[j] = jointIndex;
                }
                // Sort the weights and indices for hardware skinning
                for ( int k = 0; k < 3; ++k ) {
                    for ( int l = k + 1; l < 4; ++l ) {
                        if ( finalWeights[l] > finalWeights[k] ) {
                            SwapValue(ref finalWeights[k], ref finalWeights[l] );
                            SwapValue(ref finalJointIndices[k], ref finalJointIndices[l] );
                        }
                    }
                }  
                finalWeights[0] += Math.Max(255 - finalWeights[0] - finalWeights[1] - finalWeights[2] - finalWeights[3], 0); 

                baseVerts[i].color = new Color((byte)finalJointIndices[0], (byte)finalJointIndices[1], (byte)finalJointIndices[2], (byte)finalJointIndices[3]); 
                baseVerts[i].color1 = new Color((byte)finalWeights[0], (byte)finalWeights[1], (byte)finalWeights[2], (byte)finalWeights[3]); 

                for(int j = 0; j < numUsedWeights; j++) {
                    float dist = Vector3.Distance(baseVerts[i].position, poseMatrices[finalJointIndices[j]].TranslationVector);
                    if(dist > mesh.maxJointVertDist) {
                        mesh.maxJointVertDist = dist;
                    }
                    MathHelper.BoundingBoxAddPoint(ref mesh.bounds, poseMatrices[finalJointIndices[j]].TranslationVector);
                }
            }
            mesh.vertices = baseVerts;
            mesh.bounds.Minimum -= new Vector3(mesh.maxJointVertDist, mesh.maxJointVertDist, mesh.maxJointVertDist);
            mesh.bounds.Maximum += new Vector3(mesh.maxJointVertDist, mesh.maxJointVertDist, mesh.maxJointVertDist);  

            // build normals
            Vector3[] vertexNormals = new Vector3[mesh.vertices.Length];
            for(int i = 0; i < indices.Length; i += 3) {
                int i0 = indices[i];
                int i1 = indices[i + 1];
                int i2 = indices[i + 2];

                DrawVertex v0 = baseVerts[i0];
                DrawVertex v1 = baseVerts[i1];
                DrawVertex v2 = baseVerts[i2];

                Vector3 e0 = v1.position - v0.position;
                Vector3 e1 = v2.position - v0.position;

                Vector3 fNormal = Vector3.Cross(e1, e0);

                vertexNormals[i0] += fNormal;
                vertexNormals[i1] += fNormal;
                vertexNormals[i2] += fNormal;
            }
            for(int i = 0; i < vertexNormals.Length; i++) {
                float mag = MathF.Sqrt(vertexNormals[i].LengthSquared());
                if(!MathUtil.IsZero(mag)) {
                    vertexNormals[i] /= mag;
                }
                mesh.vertices[i].SetNormal(vertexNormals[i]);
            }
            return mesh;
        }


        public static MD5Model LoadMD5Model(string filename, float scale) {
            Lexer lex = new Lexer(File.ReadAllText(filename));
            Token token = new Token();

            lex.ExpectTokenString("MD5Version");
            lex.ParseInteger();
            lex.ExpectTokenString("commandline");
            lex.ReadToken(ref token);
            
            lex.ExpectTokenString("numJoints");
            int numJoints = lex.ParseInteger();
            lex.ExpectTokenString("numMeshes");
            int numMeshes = lex.ParseInteger();

            lex.ExpectTokenString("joints");
            lex.ExpectTokenString("{");
            var md5Joints = new MD5Joint[numJoints];
            var defaultPose = new MD5JointTransform[numJoints];
            var poseMatrices = new Matrix[numJoints]; // WorldSpace joint matrices
            for(int i = 0; i < md5Joints.Length; i++) {
                ParseMD5Joint(lex, i, md5Joints, defaultPose, scale);
                Matrix.RotationQuaternion(ref defaultPose[i].q, out poseMatrices[i]);
                poseMatrices[i].TranslationVector = defaultPose[i].t;
                if(md5Joints[i].parent > 0) {
                    int parentIndex = md5Joints[i].parent;
                    Matrix3x3 localToWorld = (Matrix3x3)poseMatrices[i];
                    Matrix3x3 worldToParent = (Matrix3x3)poseMatrices[parentIndex];
                    worldToParent.Transpose();
                    Matrix3x3 m = localToWorld * worldToParent;
                    Quaternion.RotationMatrix(ref m, out defaultPose[i].q);
                    Vector3 offset = poseMatrices[i].TranslationVector - poseMatrices[parentIndex].TranslationVector;
                    Vector3.Transform(ref offset, ref worldToParent, out defaultPose[i].t);
                }
            }
            lex.ExpectTokenString("}");

            var model = new MD5Model();
            model.joints = md5Joints;
            model.defaultPose = defaultPose;
            model.bindposes = new Matrix[numJoints];
            for(int i = 0; i < numJoints; i++) {
                Matrix.Invert(ref poseMatrices[i], out model.bindposes[i]);
            }

            model.meshes = new MD5Mesh[numMeshes];
            BoundingBox localBounds = MathHelper.NullBoundingBox;
            for(int i = 0; i < numMeshes; i++) {
                lex.ExpectTokenString("mesh");
                model.meshes[i] = ParseMD5Mesh(lex, numJoints, poseMatrices, scale);
            }

            return model;                    
        }

        public static Mesh LoadTextModel(string filename) {
            if(!File.Exists(filename)) {
                return null;
            }
        
            Mesh mesh = new Mesh();
            Token token = new Token();
            Lexer lex = new Lexer(File.ReadAllText(filename));

            lex.ReadToken(ref token);
            lex.ExpectTokenString("{");

            if(lex.CheckTokenString("version", ref token)) {
                lex.ParseInteger();
            }

            bool hasLightmapTexcoords = false;
            if(lex.CheckTokenString("HasLightmapTexcoords", ref token)) {
                hasLightmapTexcoords = true;
            }
            lex.ExpectTokenString("Bounds");
            lex.ExpectTokenString("{");
            lex.ExpectTokenString("Min");
            lex.ParseCSharpVector(ref mesh.boundingBox.Minimum);
            lex.ExpectTokenString("Max");
            lex.ParseCSharpVector(ref mesh.boundingBox.Maximum);
            lex.ExpectTokenString("}");

            int numVertices = 0;
            Vector3[] positions = null;
            Vector3[] normals = null;
            Vector4[] tangents = null;
            Vector2[] uvSet0 = null;
            Vector2[] uvSet1 = null;
            Color[] colors = null;
            ushort[] jointIndices = null;
            float[] jointWeights = null;
            lex.ExpectTokenString("NumIndices");
            int numIndices = lex.ParseInteger();
            lex.ExpectTokenString("NumVertices");
            {
                numVertices = lex.ParseInteger();
                lex.ExpectTokenString("Vertices");
                lex.ExpectTokenString("{");
                
                lex.ExpectTokenString("Positions");
                lex.ExpectTokenString("{");
                positions = new Vector3[numVertices];
                for(int i = 0; i < numVertices; i++) {
                    lex.ParseCSharpVector(ref positions[i]);
                }
                lex.ExpectTokenString("}");

                lex.ExpectTokenString("Normals");
                lex.ExpectTokenString("{");
                normals = new Vector3[numVertices];
                for(int i = 0; i < numVertices; i++) {
                    lex.ParseCSharpVector(ref normals[i]);
                }
                lex.ExpectTokenString("}");

                lex.ExpectTokenString("Tangents");
                lex.ExpectTokenString("{");
                tangents = new Vector4[numVertices];
                for(int i = 0; i < numVertices; i++) {
                    lex.ParseCSharpVector(ref tangents[i]);
                }
                lex.ExpectTokenString("}");

                lex.ExpectTokenString("UVSet0");
                lex.ExpectTokenString("{");
                uvSet0 = new Vector2[numVertices];
                for(int i = 0; i < numVertices; i++) {
                    lex.ParseCSharpVector(ref uvSet0[i]);
                }
                lex.ExpectTokenString("}");
                if(hasLightmapTexcoords) {
                    lex.ExpectTokenString("UVSet1");
                    lex.ExpectTokenString("{");
                    uvSet1 = new Vector2[numVertices];
                    for(int i = 0; i < numVertices; i++) {
                        lex.ParseCSharpVector(ref uvSet1[i]);
                    }
                    lex.ExpectTokenString("}");                    
                }
                if(lex.CheckTokenString("JointWeights", ref token)) {
                    lex.ExpectTokenString("{");
                    jointIndices = new ushort[numVertices * 4];
                    jointWeights = new float[numVertices * 4];
                    for(int i = 0; i < numVertices; i++) {
                        lex.ParseVector(ref jointIndices, i * 4, 4);
                        lex.ParseVector(ref jointWeights, i * 4, 4);
                    }
                    lex.ExpectTokenString("}");
                    if(lex.CheckTokenString("BindPoses", ref token)) {
                        lex.ExpectTokenString("{");
                        lex.ExpectTokenString("NumBindPoses");
                        mesh.bindposes = new Matrix[lex.ParseInteger()];
                        for(int i = 0; i < mesh.bindposes.Length; i++) {
                            Vector3 scale = Vector3.One;
                            Quaternion rotation = Quaternion.Identity;
                            Vector3 position = Vector3.Zero;
                            Vector3 right = MathHelper.Vec3Right;
                            Vector3 forward = MathHelper.Vec3Forward;
                            Vector3 up = MathHelper.Vec3Up;
                            lex.ParseCSharpVector(ref scale);
                            //lex.ParseCSharpVector(ref rotation);
                            lex.ParseCSharpVector(ref right); lex.ParseCSharpVector(ref forward); lex.ParseCSharpVector(ref up);
                            lex.ParseCSharpVector(ref position);
                            Matrix rotationMatrix = new Matrix(
                                right.X, right.Y, right.Z, 0, 
                                forward.X, forward.Y, forward.Z, 0, 
                                up.X, up.Y, up.Z, 0, 
                                0, 0, 0, 1);
                            mesh.bindposes[i] = Matrix.Scaling(scale) * rotationMatrix * Matrix.Translation(position);
                        }
                        lex.ExpectTokenString("}");                        
                    }
                }
                if(lex.CheckTokenString("Colors", ref token)) {
                    lex.ExpectTokenString("{");
                    colors = new Color[numVertices];
                    for(int i = 0; i < numVertices; i++) {
                        lex.ParseCSharpColor(ref colors[i]);
                    }
                    lex.ExpectTokenString("}");
                }
            }
            lex.ExpectTokenString("}");

            lex.ExpectTokenString("NumSubmeshes");
            int numSubmeshes = lex.ParseInteger();
            lex.ExpectTokenString("IndexFormat");
            int indexFormat = lex.ParseInteger();
            lex.ExpectTokenString("Meshes");
            lex.ExpectTokenString("{");

            UInt16[] indices = null;
            UInt32[] indices32 = null;
            if(indexFormat == 16) {
                indices = new ushort[numIndices];
                mesh.indexFormat = Veldrid.IndexFormat.UInt16;
            } else {
                indices32 = new UInt32[numIndices];
                mesh.indexFormat = Veldrid.IndexFormat.UInt32;
            }
            SubMesh[] subMeshes = new SubMesh[numSubmeshes];
            for(int i = 0; i < subMeshes.Length; i++) {
                lex.ExpectTokenString("{");
                lex.ExpectTokenString("VertexOffset");
                subMeshes[i].vertexOffset = lex.ParseInteger();
                lex.ExpectTokenString("IndexOffset");
                subMeshes[i].indexOffset = lex.ParseInteger();
                lex.ExpectTokenString("NumIndices");
                subMeshes[i].numIndices = lex.ParseInteger();
                if(mesh.indexFormat == Veldrid.IndexFormat.UInt16) {
                    for(int index = 0; index < subMeshes[i].numIndices; index++) {
                        ParseIndex(lex, indices, subMeshes[i].indexOffset + index);
                    }
                } else {
                    for(int index = 0; index < subMeshes[i].numIndices; index++) {
                        ParseIndex(lex, indices32, subMeshes[i].indexOffset + index);
                    }                    
                }
                subMeshes[i].materialIndex = i;
                subMeshes[i].boundingBox = mesh.boundingBox;
                lex.ExpectTokenString("}");
            }
            mesh.subMeshes = subMeshes;

            int vbSize = DrawVertexPacked0.SizeInBytes * numVertices;
            int ibSize = indexFormat == 16 ? (sizeof(UInt16) * numIndices) : (sizeof(UInt32) * numIndices);
            IntPtr vb = Utilities.AllocateMemory(vbSize);
            IntPtr vb1 = Utilities.AllocateMemory(vbSize);
            IntPtr ib = Utilities.AllocateMemory(ibSize);
            DataStream vertexBuffer0 = new DataStream(vb, vbSize, false, true);
            DataStream vertexBuffer1 = new DataStream(vb1, vbSize, false, true);
            DataStream indexBuffer = new DataStream(ib, ibSize, false, true);
            if(hasLightmapTexcoords) {
                for(int i = 0; i < numVertices; i++) {
                    DrawVertexPacked0 packed0 = new DrawVertexPacked0();
                    DrawVertexStaticPacked1 packed1 = new DrawVertexStaticPacked1();
                    packed0.position = positions[i];
                    packed0.st = uvSet0[i];
                    packed1.SetLightmapTexCoord(uvSet1[i]);
                    packed1.SetNormal(normals[i]);
                    packed1.SetTangent(tangents[i]);
                    vertexBuffer0.Write(packed0);
                    vertexBuffer1.Write(packed1);
                }
            } else {
                for(int i = 0; i < numVertices; i++) {
                    DrawVertexPacked0 packed0 = new DrawVertexPacked0();
                    DrawVertexPacked1 packed1 = new DrawVertexPacked1();
                    packed0.position = positions[i];
                    packed0.st = uvSet0[i];
                    packed1.SetNormal(normals[i]);
                    packed1.SetTangent(tangents[i]);
                    if(jointIndices != null && jointWeights != null) {
                        packed1.SetJointIndices((byte)jointIndices[i * 4], (byte)jointIndices[i * 4 + 1], (byte)jointIndices[i * 4 + 2], (byte)jointIndices[i * 4 + 3]);
                        packed1.SetJointWeights(jointWeights[i * 4], jointWeights[i * 4 + 1], jointWeights[i * 4 + 2], jointWeights[i * 4 + 3]);
                    }
                    vertexBuffer0.Write(packed0);
                    vertexBuffer1.Write(packed1);
                }
            }

            if(indexFormat == 16) {
                indexBuffer.WriteRange(indices);
            } else {
                indexBuffer.WriteRange(indices32);
            }
            vertexBuffer0.Position = 0;
            vertexBuffer1.Position = 0;
            indexBuffer.Position = 0;

            mesh.numVertices = numVertices;
            mesh.numIndices = numIndices;
            mesh.nativeVertexBufferSize = vbSize;
            mesh.nativeIndexBufferSize = ibSize;
            mesh.nativeVerticesPacked0 = vertexBuffer0.DataPointer;
            mesh.nativeVerticesPacked1 = vertexBuffer1.DataPointer;
            mesh.nativeIndices = indexBuffer.DataPointer;
            return mesh;
        }

        public static Mesh[] LoadAssimp(string filename, out int numMaterials, float scale = 0.01f, int flipUV = -1) {
            Mesh[] meshes = null;
            using(var importer = new Assimp.AssimpContext()) {
                Assimp.Scene scene = importer.ImportFile(
                    filename, 
                    Assimp.PostProcessSteps.Triangulate | 
                    Assimp.PostProcessSteps.GenerateSmoothNormals |
                    Assimp.PostProcessSteps.CalculateTangentSpace |
                    Assimp.PostProcessSteps.FlipWindingOrder |
                    (flipUV > 0 ? Assimp.PostProcessSteps.FlipUVs : Assimp.PostProcessSteps.None));

                DataStream vertexMemory = null;
                DataStream vertexMemory1 = null;
                DataStream indexMemory = null;

                numMaterials = scene.MaterialCount;
                int numMeshes = scene.MeshCount;
                meshes = new Mesh[numMeshes];
                for(int i = 0; i < numMeshes; i++) {
                    meshes[i] = new Mesh();
                    meshes[i].numVertices = scene.Meshes[i].VertexCount;
                    meshes[i].numIndices = scene.Meshes[i].FaceCount * 3;
                    meshes[i].materialIndex = scene.Meshes[i].MaterialIndex;

                    int vertexBufferSize = MathHelper.Align(DrawVertexPacked0.SizeInBytes * meshes[i].numVertices, 16);
                    int indexBufferSize = sizeof(UInt16) * meshes[i].numIndices;
                    IntPtr dataPtr = Utilities.AllocateMemory(vertexBufferSize);
                    vertexMemory = new DataStream(dataPtr, vertexBufferSize, true, true);
                    dataPtr = Utilities.AllocateMemory(vertexBufferSize);
                    vertexMemory1 = new DataStream(dataPtr, vertexBufferSize, true, true);
                    dataPtr = Utilities.AllocateMemory(indexBufferSize);
                    indexMemory = new DataStream(dataPtr, indexBufferSize, true, true);
                    ReadMeshData(scene.Meshes[i], scale, vertexMemory, vertexMemory1, indexMemory, out meshes[i].boundingBox);
                    meshes[i].nativeVertexBufferSize = vertexBufferSize;
                    meshes[i].nativeIndexBufferSize = indexBufferSize;
                    meshes[i].nativeVerticesPacked0 = vertexMemory.DataPointer;
                    meshes[i].nativeVerticesPacked1 = vertexMemory1.DataPointer;
                    meshes[i].nativeIndices = indexMemory.DataPointer;          
                }
            }

            return meshes;
        }

        public static Mesh LoadAssimp(string filename, float scale = 0.01f, int flipUV = -1) {
            Mesh mesh = null;

            using(var importer = new Assimp.AssimpContext()) {
                Assimp.Scene scene = importer.ImportFile(
                    filename, 
                    Assimp.PostProcessSteps.Triangulate | 
                    Assimp.PostProcessSteps.GenerateSmoothNormals |
                    Assimp.PostProcessSteps.CalculateTangentSpace |
                    Assimp.PostProcessSteps.FlipWindingOrder |
                    (flipUV > 0 ? Assimp.PostProcessSteps.FlipUVs : Assimp.PostProcessSteps.None));

                mesh = new Mesh();
                DataStream vertexMemory = null;
                DataStream vertexMemory1 = null;
                DataStream indexMemory = null;

                int numMeshes = scene.MeshCount;
                int numMaterials = scene.MaterialCount;
                int totalVertices = 0;
                int totalIndices = 0;
                int vertexOffset = 0;
                int indexOffset = 0;

                var subMeshes = new SubMesh[numMeshes];

                for(int i = 0; i < numMeshes; i++) {
                    SubMesh surface = new SubMesh();
                    surface.vertexOffset = vertexOffset;
                    surface.indexOffset = indexOffset;
                    surface.numVertices = scene.Meshes[i].VertexCount;
                    surface.numIndices = scene.Meshes[i].FaceCount * 3;
                    surface.materialIndex = scene.Meshes[i].MaterialIndex;
                    MathHelper.BoundingBoxClear(ref surface.boundingBox);
                    subMeshes[i] = surface;
                    vertexOffset += scene.Meshes[i].VertexCount;
                    indexOffset += scene.Meshes[i].FaceCount * 3;
                    totalVertices += scene.Meshes[i].VertexCount;
                    totalIndices += scene.Meshes[i].FaceCount * 3;
                }
 
                int vertexBufferSize = MathHelper.Align(DrawVertexPacked0.SizeInBytes * totalVertices, 16);
                int indexBufferSize = sizeof(UInt16) * totalIndices;
                IntPtr dataPtr = Utilities.AllocateMemory(vertexBufferSize);
                vertexMemory = new DataStream(dataPtr, vertexBufferSize, true, true);
                dataPtr = Utilities.AllocateMemory(vertexBufferSize);
                vertexMemory1 = new DataStream(dataPtr, vertexBufferSize, true, true);
                dataPtr = Utilities.AllocateMemory(indexBufferSize);
                indexMemory = new DataStream(dataPtr, indexBufferSize, true, true);
                for(int i = 0; i < numMeshes; i++) {
                    ReadMeshData(scene.Meshes[i], scale, ref subMeshes[i], vertexMemory, vertexMemory1, indexMemory);
                    MathHelper.BoundingBoxAddBounds(ref mesh.boundingBox, subMeshes[i].boundingBox);
                    //Console.WriteLine($"min:{localBoundsArray[i].Minimum} max:{localBoundsArray[i].Maximum}");
                }
                mesh.subMeshes = subMeshes;
                mesh.numVertices = totalVertices;
                mesh.numIndices = totalIndices;
                mesh.nativeVertexBufferSize = vertexBufferSize;
                mesh.nativeIndexBufferSize = indexBufferSize;
                mesh.nativeVerticesPacked0 = vertexMemory.DataPointer;
                mesh.nativeVerticesPacked1 = vertexMemory1.DataPointer;
                mesh.nativeIndices = indexMemory.DataPointer;                       
            }

            return mesh;
        }
    }
}