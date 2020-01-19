using System;
using SharpDX;

namespace NT
{
    public class DebugPass : RenderPass {
        VertexBufferRef vertexBuffer;
        IndexBufferRef indexBuffer;
        VertexBufferRef triVertexBuffer;
        IndexBufferRef triIndexBuffer;
        VertexBufferRef screenVertexBuffer;
        int numVertices;
        int numIndices;
        int numTriVertices;
        int numTriIndices;
        int numScreenPicVertices;
        int numScreenPicIndices;
        Veldrid.Framebuffer[] framebuffers;

        public DebugPass(FrameGraph myFrameGraph, string myName) : base(myFrameGraph, myName) {
            framebuffers = new Veldrid.Framebuffer[2];
            for(int i = 0; i < framebuffers.Length; i++) {
                framebuffers[i] = GraphicsDevice.ResourceFactory.CreateFramebuffer(new Veldrid.FramebufferDescription(
                    frameGraph.viewDepthMap.Target,
                    frameGraph.viewColorMaps[i].Target
                ));
            }
            framebuffer = framebuffers[0];
        }

        void WriteVertex(Vector3 p, Color color) {
            vertexStream.Write(p);
            vertexStream.Write(color);
        }

        void WriteTriVertex(Vector3 p, Color color) {
            triVertexStream.Write(p);
            triVertexStream.Write(color);
        }

        void WriteLineIndex(int v0, int v1) {
            indexStream.Write(v0);
            indexStream.Write(v1);
        }

        void DrawLine(Vector3 p1, Vector3 p2, Color color) {
            indexStream.Write(numVertices);
            indexStream.Write(numVertices + 1);
            vertexStream.Write(p1);
            vertexStream.Write(color);
            vertexStream.Write(p2);
            vertexStream.Write(color);
            numVertices += 2;
            numIndices += 2;
        }     

        void DrawCircle(int segments, float radius, Vector3 origin, Quaternion rotation, Color color) {
			float deg = 360f / (float)segments;
			for(int i = 0; i < segments; i++) {
				float rad = MathUtil.DegreesToRadians(((float)i * deg));
                Vector3 v = new Vector3(MathF.Cos(rad) * radius, MathF.Sin(rad) * radius, 0f);
                v = origin + Vector3.Transform(v, rotation);
                vertexStream.Write(v);
                vertexStream.Write(color);
                indexStream.Write(numVertices + i);
                indexStream.Write((i + 1) % segments + numVertices);
			} 
            numVertices += segments;
            numIndices += segments * 2;        
        }

        void DrawArc(int maxVerts, int segments, float radius, float startAngle, float endAngle, Vector3 origin, Quaternion rotation, Color color) {
            float totalAngle = endAngle - startAngle;
            float deg = totalAngle / (float)segments;
            for(int i = 0; i < maxVerts; i++) {
				float rad = MathUtil.DegreesToRadians(((float)i * deg + startAngle));
                Vector3 v = new Vector3(MathF.Cos(rad) * radius, MathF.Sin(rad) * radius, 0f);
                v = origin + Vector3.Transform(v, rotation);
                vertexStream.Write(v);
                vertexStream.Write(color);
                indexStream.Write(numVertices + i);
                indexStream.Write((i + 1) % maxVerts + numVertices);                
            }
            indexStream.Position -= sizeof(UInt32) * 2;
            numVertices += maxVerts;
            numIndices += maxVerts * 2 - 2;      
        }

        void DrawFan(int maxVerts, int segments, float radius, float startAngle, float endAngle, Vector3 origin, Quaternion rotation, Color color) {
            float totalAngle = endAngle - startAngle;
            float deg = totalAngle / (float)segments;
            for(int i = 0; i < maxVerts; i++) {
				float rad = MathUtil.DegreesToRadians(((float)i * deg + startAngle));
                Vector3 v = new Vector3(MathF.Cos(rad) * radius, MathF.Sin(rad) * radius, 0f);
                v = origin + Vector3.Transform(v, rotation);
                WriteTriVertex(v, color);           
            }
            WriteTriVertex(origin, color);

            for(int i = 0; i < segments; i++) {
                triIndexStream.Write(numTriVertices + i);
                triIndexStream.Write(numTriVertices + maxVerts);
                triIndexStream.Write(numTriVertices + i + 1);
            }

            numTriVertices += maxVerts + 1;
            numTriIndices += segments * 3;      
        }

        void DrawArcRing(int maxVerts, int segments, float innerRadius, float outerRadius, float startAngle, float endAngle, Vector3 origin, Quaternion rotation, Color color) {
            float totalAngle = endAngle - startAngle;
            float deg = totalAngle / (float)segments;
            for(int i = 0; i < (segments + 1); i++) {
				float rad = MathUtil.DegreesToRadians(((float)i * deg + startAngle));
                Vector3 v = new Vector3(MathF.Cos(rad) * innerRadius, MathF.Sin(rad) * innerRadius, 0f);
                Vector3 v2 = new Vector3(MathF.Cos(rad) * outerRadius, MathF.Sin(rad) * outerRadius, 0f);
                v = origin + Vector3.Transform(v, rotation);
                v2 = origin + Vector3.Transform(v2, rotation);
                WriteTriVertex(v, color);  
                WriteTriVertex(v2, color);         
            }

            for(int i = 0; i < segments; i++) {
                triIndexStream.Write(numTriVertices + i * 2);
                triIndexStream.Write(numTriVertices + i * 2 + 2);
                triIndexStream.Write(numTriVertices + i * 2 + 3);

                triIndexStream.Write(numTriVertices + i * 2);
                triIndexStream.Write(numTriVertices + i * 2 + 3);
                triIndexStream.Write(numTriVertices + i * 2 + 1);
            }

            numTriVertices += maxVerts;
            numTriIndices += segments * 2 * 3;      
        }

        void DrawSolidCircle(int segments, float radius, Vector3 origin, Quaternion rotation, Color color) {
			float deg = 360f / (float)segments;
			for(int i = 0; i < segments; i++) {
				float rad = MathUtil.DegreesToRadians(((float)i * deg));
                Vector3 v = new Vector3(MathF.Cos(rad) * radius, MathF.Sin(rad) * radius, 0f);
                v = origin + Vector3.Transform(v, rotation);
                triVertexStream.Write(v);
                triVertexStream.Write(color);
			} 
            triVertexStream.Write(origin);
            triVertexStream.Write(color);
            for(int i = 0; i < segments; i++) {
                triIndexStream.Write(numTriVertices + i);
                triIndexStream.Write(numTriVertices + segments);
                triIndexStream.Write((i + 1) % segments + numTriVertices);
            }
            numTriVertices += segments + 1;
            numTriIndices += segments * 3;        
        }

        void DrawSphere(int segments, float radius, Vector3 origin, Color color) {
            DrawCircle(segments, radius, origin, Quaternion.Identity, color);
            DrawCircle(segments, radius, origin, Quaternion.RotationAxis(MathHelper.Vec3Right, MathF.PI * 0.5f), color);
            DrawCircle(segments, radius, origin, Quaternion.RotationAxis(MathHelper.Vec3Forward, MathF.PI * 0.5f), color);
        }

        void DrawBox(Vector3 min, Vector3 max, Vector3 origin, Quaternion rotation, Color color) {
            Vector3 right = Vector3.Transform(MathHelper.Vec3Right, rotation);
            Vector3 forward = Vector3.Transform(MathHelper.Vec3Forward, rotation);
            Vector3 up = Vector3.Transform(MathHelper.Vec3Up, rotation);
            Vector3 size = (max - min) * 0.5f;
            //origin += (max - min);
            right *= size.X;
            forward *= size.Y;
            up *= size.Z;

            WriteVertex(origin - right - forward - up, color);
            WriteVertex(origin - right + forward - up, color);
            WriteVertex(origin + right + forward - up, color);
            WriteVertex(origin + right - forward - up, color);

            WriteVertex(origin - right - forward + up, color);
            WriteVertex(origin - right + forward + up, color);
            WriteVertex(origin + right + forward + up, color);
            WriteVertex(origin + right - forward + up, color);

            int baseVert = numVertices;
            WriteLineIndex(baseVert, baseVert + 1);
            WriteLineIndex(baseVert + 1, baseVert + 2);
            WriteLineIndex(baseVert + 2, baseVert + 3);
            WriteLineIndex(baseVert + 3, baseVert);
            baseVert += 4;
            WriteLineIndex(baseVert, baseVert + 1);
            WriteLineIndex(baseVert + 1, baseVert + 2);
            WriteLineIndex(baseVert + 2, baseVert + 3);
            WriteLineIndex(baseVert + 3, baseVert);
            baseVert = numVertices;
            WriteLineIndex(baseVert, baseVert + 4);
            WriteLineIndex(baseVert + 1, baseVert + 5);
            WriteLineIndex(baseVert + 2, baseVert + 6);
            WriteLineIndex(baseVert + 3, baseVert + 7);

            numVertices += 8;
            numIndices += 24;        
        }

        readonly int[] boxTriIndices = {
            0,1,2,
            0,2,3,
            4,0,3,
            4,3,7,
            7,3,2,
            7,2,6,
            6,2,1,
            6,1,5,
            5,4,7,
            6,5,7,
            5,1,0,
            5,0,4            
        };

        void DrawSolidBox(Vector3 min, Vector3 max, Vector3 origin, Quaternion rotation, Color color) {
            Vector3 right = Vector3.Transform(MathHelper.Vec3Right, rotation);
            Vector3 forward = Vector3.Transform(MathHelper.Vec3Forward, rotation);
            Vector3 up = Vector3.Transform(MathHelper.Vec3Up, rotation);
            Vector3 size = (max - min) * 0.5f;
            right *= size.X;
            forward *= size.Y;
            up *= size.Z;

            WriteTriVertex(origin - right - forward - up, color);
            WriteTriVertex(origin - right + forward - up, color);
            WriteTriVertex(origin + right + forward - up, color);
            WriteTriVertex(origin + right - forward - up, color);

            WriteTriVertex(origin - right - forward + up, color);
            WriteTriVertex(origin - right + forward + up, color);
            WriteTriVertex(origin + right + forward + up, color);
            WriteTriVertex(origin + right - forward + up, color);

            for(int i = 0; i < boxTriIndices.Length; i++) {
                triIndexStream.Write(boxTriIndices[i] + numTriVertices);
            }

            numTriVertices += 8;
            numTriIndices += boxTriIndices.Length;        
        }

        void DrawPlane(Plane plane, float size, Color color) {
            Vector3 origin = plane.Normal * plane.D;
            plane.Normal.NormalVectors(out Vector3 right, out Vector3 up);
            right *= size;
            up *= size;
            Vector3 pt1 = origin - right + up;
            Vector3 pt2 = origin + right + up;
            Vector3 pt3 = origin + right - up;
            Vector3 pt4 = origin - right - up;

            vertexStream.Write(pt1);
            vertexStream.Write(color);
            vertexStream.Write(pt2);
            vertexStream.Write(color);
            vertexStream.Write(pt3);
            vertexStream.Write(color);
            vertexStream.Write(pt4);
            vertexStream.Write(color);
            indexStream.Write(numVertices);
            indexStream.Write(numVertices + 1);
            indexStream.Write(numVertices + 1);
            indexStream.Write(numVertices + 2);
            indexStream.Write(numVertices + 2);
            indexStream.Write(numVertices + 3);
            indexStream.Write(numVertices + 3);
            indexStream.Write(numVertices);

            numVertices += 4;
            numIndices += 8;             
        }

        void DrawSolidPlane(Plane plane, float size, Color color) {
            Vector3 origin = plane.Normal * plane.D;
            plane.Normal.NormalVectors(out Vector3 right, out Vector3 up);
            right *= size;
            up *= size;
            Vector3 pt1 = origin - right + up;
            Vector3 pt2 = origin + right + up;
            Vector3 pt3 = origin + right - up;
            Vector3 pt4 = origin - right - up;

            WriteTriVertex(pt1, color);
            WriteTriVertex(pt2, color);
            WriteTriVertex(pt3, color);
            WriteTriVertex(pt4, color);
            triIndexStream.Write(numTriVertices);
            triIndexStream.Write(numTriVertices + 1);
            triIndexStream.Write(numTriVertices + 3);
            triIndexStream.Write(numTriVertices + 1);
            triIndexStream.Write(numTriVertices + 2);
            triIndexStream.Write(numTriVertices + 3);

            numTriVertices += 4;
            numTriIndices += 6;             
        }

        void DrawCone(int segments, float range, float spotAngle, Vector3 origin, Quaternion rotation, Color color) {
            float radius = MathF.Tan(MathUtil.DegreesToRadians(spotAngle) * 0.5f) * range;
			float deg = 360f / (float)segments;
			for(int i = 0; i < segments; i++) {
				float rad = MathUtil.DegreesToRadians(((float)i * deg));
                Vector3 v = new Vector3(MathF.Cos(rad) * radius, MathF.Sin(rad) * radius, range);
                v = origin + Vector3.Transform(v, rotation);
                vertexStream.Write(v);
                vertexStream.Write(color);
                indexStream.Write(numVertices + i);
                indexStream.Write((i + 1) % segments + numVertices);
			} 
            vertexStream.Write(origin);
            vertexStream.Write(color);
            for(int i = 0; i < segments; i++) {
                indexStream.Write(numVertices + segments);
                indexStream.Write(numVertices + i);                
            }

            numVertices += segments + 1;
            numIndices += segments * 4;                    
        }

        void DrawArrow(int segments, float radius, float height, Vector3 origin, Quaternion rotation, Color color) {
            int baseVert = numTriVertices;
            DrawSolidCircle(segments, radius, origin, rotation, color);
            WriteTriVertex(origin + Vector3.Transform(MathHelper.Vec3Up, rotation) * height, color);
            for(int i = 0; i < segments; i++) {
                triIndexStream.Write(baseVert + i);
                triIndexStream.Write(numTriVertices);
                triIndexStream.Write((i + 1) % segments + baseVert);
            }
            numTriVertices += 1;
            numTriIndices += segments * 3;
        }        

        void DrawFrustum(float near, float far, float fov, float aspect, Vector3 origin, Vector3 right, Vector3 forward, Vector3 up) {
            Color color = Color.Blue;

            float halfHeight = near * MathF.Tan(fov * 0.5f);
            Vector3 toUp = up * halfHeight;
            Vector3 toRight = right * halfHeight * aspect;
            Vector3 toNear = forward * near;
            Vector3 topL = toNear + toUp - toRight;
            Vector3 topR = toNear + toUp + toRight;
            Vector3 bottomL = toNear - toUp - toRight;
            Vector3 bottomR = toNear - toUp + toRight;

            WriteVertex(origin + topL, color);
            WriteVertex(origin + topR, color);
            WriteVertex(origin + bottomR, color);
            WriteVertex(origin + bottomL, color);
            WriteLineIndex(numVertices, numVertices + 1);
            WriteLineIndex(numVertices + 1, numVertices + 2);
            WriteLineIndex(numVertices + 2, numVertices + 3);
            WriteLineIndex(numVertices + 3, numVertices);

            halfHeight = far * MathF.Tan(fov * 0.5f);
            toUp = up * halfHeight;
            toRight = right * halfHeight * aspect;
            Vector3 toFar = forward * far;
            topL = toFar + toUp - toRight;
            topR = toFar + toUp + toRight;
            bottomL = toFar - toUp - toRight;
            bottomR = toFar - toUp + toRight;

            numVertices += 4;
            WriteVertex(origin + topL, color);
            WriteVertex(origin + topR, color);
            WriteVertex(origin + bottomR, color);
            WriteVertex(origin + bottomL, color);
            WriteLineIndex(numVertices, numVertices + 1);
            WriteLineIndex(numVertices + 1, numVertices + 2);
            WriteLineIndex(numVertices + 2, numVertices + 3);
            WriteLineIndex(numVertices + 3, numVertices);
            numVertices += 4;
            WriteVertex(origin, color);
            WriteLineIndex(numVertices, numVertices - 4);
            WriteLineIndex(numVertices, numVertices - 3);
            WriteLineIndex(numVertices, numVertices - 2);
            WriteLineIndex(numVertices, numVertices - 1);
            numVertices += 1;

            numIndices += 24;
        }  

        void DrawSolidCylinder(int segments, float radius, float height, Vector3 origin, Quaternion rotation, Color color) {
            Vector3 offset = Vector3.Transform(MathHelper.Vec3Up, rotation) * height * 0.5f;

            int baseVert = numTriVertices;
            DrawSolidCircle(segments, radius, origin - offset, rotation, color);
            DrawSolidCircle(segments, radius, origin + offset, rotation, color);
            for(int i = 0; i < segments; i++) {
                int i0 = i;
                int i1 = (i + 1) % segments;
                int i2 = i1 + segments + 1;
                triIndexStream.Write(baseVert + i2);
                triIndexStream.Write(baseVert + i1);
                triIndexStream.Write(baseVert + i0);

                i0 = i;
                i1 = (i + 1) % segments + segments + 1;
                i2 = i0 + segments + 1;
                triIndexStream.Write(baseVert + i2);
                triIndexStream.Write(baseVert + i1);
                triIndexStream.Write(baseVert + i0);             
            }
            numTriIndices += segments * 6;
        }   

        void DrawAxis(Vector3 origin, Quaternion rotation, float size) {
            DrawLine(origin, origin + Vector3.Transform(MathHelper.Vec3Right, rotation) * size, Color.Red);
            DrawLine(origin, origin + Vector3.Transform(MathHelper.Vec3Forward, rotation) * size, Color.Green);
            DrawLine(origin, origin + Vector3.Transform(MathHelper.Vec3Up, rotation) * size, Color.Blue);
        }   

        DataStream vertexStream;
        DataStream indexStream;
        DataStream triVertexStream;
        DataStream triIndexStream;

        public override void Setup() {

        }

        public override void PostSetup() {
            vertexBuffer = new VertexBufferRef();
            indexBuffer = new IndexBufferRef();
            triVertexBuffer = new VertexBufferRef();
            triIndexBuffer = new IndexBufferRef();
            screenVertexBuffer = new VertexBufferRef();
        }

        void UpdateRenderResources(DebugToolRenderData renderData, Veldrid.CommandList commandList) {
          if(renderData == null) {
                return;
            }

            FrameRenderResources frameRenderResources = frameGraph.frameRenderResources;         
            int vertexBufferSize = renderData.numVertices * Utilities.SizeOf<DebugVertex>();
            int indexBufferSize = renderData.numIndices * sizeof(UInt32);
            int triVertexBufferSize = renderData.numTriVertices * Utilities.SizeOf<DebugVertex>();
            int triIndexBufferSize = renderData.numTriIndices * sizeof(UInt32);
            if(vertexBufferSize > 0) {
                vertexStream = new DataStream(frameRenderResources.AllocVertices(vertexBufferSize, Utilities.SizeOf<DebugVertex>(), ref vertexBuffer), vertexBufferSize, true, true);
            }
            if(indexBufferSize > 0) {
                indexStream = new DataStream(frameRenderResources.AllocIndices(indexBufferSize, Veldrid.IndexFormat.UInt32, ref indexBuffer), indexBufferSize, true, true);
            }
            if(triVertexBufferSize > 0) {
                triVertexStream = new DataStream(frameRenderResources.AllocVertices(triVertexBufferSize, Utilities.SizeOf<DebugVertex>(), ref triVertexBuffer), triVertexBufferSize, true, true);
            }
            if(triIndexBufferSize > 0) { 
                triIndexStream = new DataStream(frameRenderResources.AllocIndices(triIndexBufferSize, Veldrid.IndexFormat.UInt32, ref triIndexBuffer), triIndexBufferSize, true, true);
            }
            if(renderData.screenPics != null) {
                int bufferSize = renderData.screenPics.Length * (Utilities.SizeOf<DrawVertex>() * 4);
                DataStream stream = new DataStream(frameRenderResources.AllocVertices(renderData.screenPics.Length * 4, Utilities.SizeOf<DrawVertex>(), ref screenVertexBuffer), bufferSize, true, true);
                for(int i = 0; i < renderData.screenPics.Length; i++) {
                    stream.Write(renderData.screenPics[i].pt0);
                    stream.Write(renderData.screenPics[i].pt1);
                    stream.Write(renderData.screenPics[i].pt2);
                    stream.Write(renderData.screenPics[i].pt3);
                }
            }
            var cmds = renderData.buffers;
            if(cmds != null) {
                for(int i = 0; i < cmds.Length; i++) {
                    var cmd = cmds[i];
                    cmd.Position = 0;
                    var type = cmd.Read<DebugDrawType>();
                    switch(type) {
                        case DebugDrawType.Line: {
                            DrawLine(cmd.Read<Vector3>(), cmd.Read<Vector3>(), cmd.Read<Color>());
                            break;
                        }
                        case DebugDrawType.Circle: {
                            DrawCircle(cmd.Read<int>(), cmd.Read<float>(), cmd.Read<Vector3>(), cmd.Read<Quaternion>(), cmd.Read<Color>());
                            break;
                        }
                        case DebugDrawType.Arc: {
                            DrawArc(cmd.Read<int>(), cmd.Read<int>(), cmd.Read<float>(), cmd.Read<float>(), cmd.Read<float>(), cmd.Read<Vector3>(), cmd.Read<Quaternion>(), cmd.Read<Color>());
                            break;
                        }
                        case DebugDrawType.Fan: {
                            DrawFan(cmd.Read<int>(), cmd.Read<int>(), cmd.Read<float>(), cmd.Read<float>(), cmd.Read<float>(), cmd.Read<Vector3>(), cmd.Read<Quaternion>(), cmd.Read<Color>());
                            break;
                        }
                        case DebugDrawType.ArcRing: {
                            DrawArcRing(cmd.Read<int>(), cmd.Read<int>(), cmd.Read<float>(), cmd.Read<float>(), cmd.Read<float>(), cmd.Read<float>(), cmd.Read<Vector3>(), cmd.Read<Quaternion>(), cmd.Read<Color>());
                            break;
                        }
                        case DebugDrawType.SolidCircle: {
                            DrawSolidCircle(cmd.Read<int>(), cmd.Read<float>(), cmd.Read<Vector3>(), cmd.Read<Quaternion>(), cmd.Read<Color>());
                            break;
                        }
                        case DebugDrawType.Sphere: {
                            DrawSphere(cmd.Read<int>(), cmd.Read<float>(), cmd.Read<Vector3>(), cmd.Read<Color>());
                            break;
                        }
                        case DebugDrawType.Plane: {
                            DrawPlane(cmd.Read<Plane>(), cmd.Read<float>(), cmd.Read<Color>());
                            break;
                        }
                        case DebugDrawType.SolidPlane: {
                            DrawSolidPlane(cmd.Read<Plane>(), cmd.Read<float>(), cmd.Read<Color>());
                            break;                        
                        }
                        case DebugDrawType.Cone: {
                            DrawCone(cmd.Read<int>(), cmd.Read<float>(), cmd.Read<float>(), cmd.Read<Vector3>(), cmd.Read<Quaternion>(), cmd.Read<Color>());
                            break;
                        }
                        case DebugDrawType.Frustum: {
                            DrawFrustum(cmd.Read<float>(), cmd.Read<float>(), cmd.Read<float>(), cmd.Read<float>(), cmd.Read<Vector3>(), cmd.Read<Vector3>(), cmd.Read<Vector3>(), cmd.Read<Vector3>());
                            break;
                        }
                        case DebugDrawType.Axis: {
                            DrawAxis(cmd.Read<Vector3>(), cmd.Read<Quaternion>(), cmd.Read<float>());
                            break;
                        }
                        case DebugDrawType.Box: {
                            DrawBox(cmd.Read<Vector3>(), cmd.Read<Vector3>(), cmd.Read<Vector3>(), cmd.Read<Quaternion>(), cmd.Read<Color>());
                            break;
                        }
                        case DebugDrawType.SolidBox: {
                            DrawSolidBox(cmd.Read<Vector3>(), cmd.Read<Vector3>(), cmd.Read<Vector3>(), cmd.Read<Quaternion>(), cmd.Read<Color>());
                            break;
                        }
                        case DebugDrawType.SolidCylinder: {
                            DrawSolidCylinder(cmd.Read<int>(), cmd.Read<float>(), cmd.Read<float>(), cmd.Read<Vector3>(), cmd.Read<Quaternion>(), cmd.Read<Color>());
                            break;
                        }
                        case DebugDrawType.Arrow: {
                            DrawArrow(cmd.Read<int>(), cmd.Read<float>(), cmd.Read<float>(), cmd.Read<Vector3>(), cmd.Read<Quaternion>(), cmd.Read<Color>());
                            break;
                        }
                    }
                }
            }
        }

        public void Render(Veldrid.CommandList commandList, DebugToolRenderData renderData, int currentFrame) {
            if(renderData == null) {
                return;
            }

            framebuffer = framebuffers[currentFrame];

            commandList.PushDebugGroup("Debug 3D pass");

            UpdateRenderResources(renderData, commandList);
            commandList.SetFramebuffer(framebuffer);
            if(numIndices > 0) {
                commandList.SetPipeline(renderData.wireframeMaterial.renderState.pipeline);
                if(renderData.wireframeMaterial.renderPassResourceSet != null) {
                    commandList.SetGraphicsResourceSet(0, renderData.wireframeMaterial.renderPassResourceSet);
                }
                commandList.SetVertexBuffer(0, vertexBuffer.bufferObject, (uint)vertexBuffer.offsetInOtherInBytes);
                commandList.SetIndexBuffer(indexBuffer.bufferObject, indexBuffer.indexFormat, (uint)indexBuffer.offsetInOtherInBytes);
                commandList.DrawIndexed((uint)numIndices);
            }
            if(numTriIndices > 0) {
                commandList.SetPipeline(renderData.filledMaterial.renderState.pipeline);
                if(renderData.filledMaterial.renderPassResourceSet != null) {
                    commandList.SetGraphicsResourceSet(0, renderData.filledMaterial.renderPassResourceSet);
                }
                commandList.SetVertexBuffer(0, triVertexBuffer.bufferObject, (uint)triVertexBuffer.offsetInOtherInBytes);
                commandList.SetIndexBuffer(triIndexBuffer.bufferObject, triIndexBuffer.indexFormat, (uint)triIndexBuffer.offsetInOtherInBytes);
                commandList.DrawIndexed((uint)numTriIndices);
            }
            if(renderData.screenPics != null) {
                for(int i = 0; i < renderData.screenPics.Length; i++) {
                    ScreenPic pic = renderData.screenPics[i];
                    commandList.SetPipeline(pic.material.renderState.pipeline);
                    if(pic.material.renderPassResourceSet != null) {
                        commandList.SetGraphicsResourceSet(0, pic.material.renderPassResourceSet);
                    }
                    if(pic.material.materialResourceSet != null) {
                        commandList.SetGraphicsResourceSet(1, pic.material.materialResourceSet);
                    }
                    commandList.SetVertexBuffer(0, screenVertexBuffer.bufferObject, (uint)screenVertexBuffer.offsetInOtherInBytes + (uint)i * 4);
                    commandList.Draw(4);                    
                }
            }

            commandList.PopDebugGroup();

            numVertices = 0;
            numIndices = 0;
            numTriVertices = 0;
            numTriIndices = 0;
        }

        public override void Dispose() {
            vertexBuffer?.ReleaseDeviceResource();
            indexBuffer?.ReleaseDeviceResource();
            triVertexBuffer?.ReleaseDeviceResource();
            triIndexBuffer?.ReleaseDeviceResource();
        }
    }
}