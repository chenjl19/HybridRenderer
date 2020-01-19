using System;
using System.Collections.Generic;
using SharpDX;

namespace NT
{
    public struct ScreenPic {
        public DrawVertex pt0;
        public DrawVertex pt1;
        public DrawVertex pt2;
        public DrawVertex pt3;
        public MaterialRenderProxy material;
    }

    public struct DebugVertex {
        public Vector3 position;
        public Color color;
    }

    public enum DebugDrawType : UInt32 {
        Line,
        Box,
        Plane,
        Circle,
        Arc,
        Sphere,
        UVSphere,
        Cone,
        Grid,
        Frustum,
        Handle,
        Axis,
        Arrow,
        SolidCircle,
        Fan,
        SolidBox,
        SolidPlane,
        SolidCylinder,
        ArcRing
    }

    public class DebugToolRenderData {
        public int numTriVertices;
        public int numTriIndices;
        public int numVertices;
        public int numIndices;
        public DataStream[] buffers;  
        public ScreenPic[] screenPics;
        public MaterialRenderProxy wireframeMaterial;
        public MaterialRenderProxy filledMaterial;      
    }

    public class RenderDebug {
        int totalTriVertexCount;
        int totalTriIndexCount;
        int totalVertexCount;
        int totalIndexCount;
        List<DataStream> buffers;
        List<ScreenPic> screenPics;
        Material material;
        MaterialRenderProxy wireframeMaterial;
        MaterialRenderProxy filledMaterial;

        static readonly int VertexSize = Utilities.SizeOf<DebugVertex>();

        public RenderDebug() {
            buffers = new List<DataStream>();
            screenPics = new List<ScreenPic>();
            material = new Material(Common.declManager.FindShader("tools/debugDraw"));
            wireframeMaterial = material.MakeLightingProxy();
            material.SetRenderState(1);
            filledMaterial = material.MakeLightingProxy();
            material.SetRenderState(2);
            material.SetSampler("_LinearSampler", GraphicsDevice.gd.LinearSampler);
        }

        public void AddBox(BoundingBox box, Quaternion rotation, Color color) {
            AddBox(box.Minimum, box.Maximum, box.Center, rotation, color);
        }

        public void AddBox(OrientedBoundingBox box, Color color) {
            Vector3[] corners = box.GetCorners();
            AddLine(corners[0], corners[1], color);
            AddLine(corners[1], corners[2], color);
            AddLine(corners[2], corners[3], color);
            AddLine(corners[3], corners[0], color);
            AddLine(corners[4], corners[5], color);
            AddLine(corners[5], corners[6], color);
            AddLine(corners[6], corners[7], color);
            AddLine(corners[7], corners[4], color);
            AddLine(corners[4], corners[0], color);
            AddLine(corners[5], corners[1], color);
            AddLine(corners[6], corners[2], color);
            AddLine(corners[7], corners[3], color);
        }

        public void AddBox(Vector3 min, Vector3 max, Vector3 origin, Quaternion rotation, Color color) {
            DataStream buffer = new DataStream(FrameAllocator.AllocMemory(128), 128, true, true);
            buffer.Write(DebugDrawType.Box);
            buffer.Write(min);
            buffer.Write(max);
            buffer.Write(origin);
            buffer.Write(rotation);
            buffer.Write(color);
            buffers.Add(buffer);
            totalVertexCount += 8;
            totalIndexCount += 24;
        }

        public void AddSolidBox(Vector3 min, Vector3 max, Vector3 origin, Quaternion rotation, Color color) {
            DataStream buffer = new DataStream(FrameAllocator.AllocMemory(128), 128, true, true);
            buffer.Write(DebugDrawType.SolidBox);
            buffer.Write(min);
            buffer.Write(max);
            buffer.Write(origin);
            buffer.Write(rotation);
            buffer.Write(color);
            buffers.Add(buffer);
            totalTriVertexCount += 8;
            totalTriIndexCount += 36;
        }

        public void AddPlane(Plane plane, float size, Color color) {
            DataStream buffer = new DataStream(FrameAllocator.AllocMemory(128), 128, true, true);
            buffer.Write(DebugDrawType.Plane);
            buffer.Write(plane);
            buffer.Write(size);
            buffer.Write(color);
            buffers.Add(buffer);
            totalVertexCount += 4;
            totalIndexCount += 8;
        }

        public void AddSolidPlane(Plane plane, float size, Color color) {
            DataStream buffer = new DataStream(FrameAllocator.AllocMemory(128), 128, true, true);
            buffer.Write(DebugDrawType.SolidPlane);
            buffer.Write(plane);
            buffer.Write(size);
            buffer.Write(color);
            buffers.Add(buffer);
            totalTriVertexCount += 4;
            totalTriIndexCount += 6;
        }

        public void AddCircle(int segments, float radius, Vector3 origin, Quaternion rotation, Color color) {
            segments = MathUtil.Clamp(segments, 4, 32);
            DataStream buffer = new DataStream(FrameAllocator.AllocMemory(128), 128, true, true);
            buffer.Write(DebugDrawType.Circle);
            buffer.Write(segments);
            buffer.Write(radius);
            buffer.Write(origin);
            buffer.Write(rotation);
            buffer.Write(color);
            buffers.Add(buffer);
            totalVertexCount += segments;
            totalIndexCount += segments * 2;
        }

        public void AddSolidCircle(int segments, float radius, Vector3 origin, Quaternion rotation, Color color) {
            segments = MathUtil.Clamp(segments, 4, 32);
            DataStream buffer = new DataStream(FrameAllocator.AllocMemory(128), 128, true, true);
            buffer.Write(DebugDrawType.SolidCircle);
            buffer.Write(segments);
            buffer.Write(radius);
            buffer.Write(origin);
            buffer.Write(rotation);
            buffer.Write(color);
            buffers.Add(buffer);
            totalTriVertexCount += segments + 1;
            totalTriIndexCount += segments * 3;
        }

        public void AddSphere(int segments, float radius, Vector3 origin, Color color) {
            segments = MathUtil.Clamp(segments, 4, 32);
            DataStream buffer = new DataStream(FrameAllocator.AllocMemory(128), 128, true, true);
            buffer.Write(DebugDrawType.Sphere);
            buffer.Write(segments);
            buffer.Write(radius);
            buffer.Write(origin);
            buffer.Write(color);
            buffers.Add(buffer);
            totalVertexCount += segments * 3;
            totalIndexCount += segments * 6;
        }   

        public void AddSolidSphere(int segments, float radius, Vector3 origin, Color color) {
        } 

        public void AddSpotCone(int segments, float range, float spotAngle, Vector3 origin, Quaternion rotation, Color color) { 
            AddCone(segments, range, spotAngle, origin, rotation * Quaternion.RotationAxis(MathHelper.Vec3Right, MathUtil.DegreesToRadians(-90f)), color);
        }

        public void AddCone(int segments, float range, float coneAngle, Vector3 origin, Quaternion rotation, Color color) { 
            segments = MathUtil.Clamp(segments, 4, 32);
            DataStream buffer = new DataStream(FrameAllocator.AllocMemory(128), 128, true, true);
            buffer.Write(DebugDrawType.Cone);
            buffer.Write(segments);
            buffer.Write(range);
            buffer.Write(coneAngle);
            buffer.Write(origin);
            buffer.Write(rotation);
            buffer.Write(color);
            buffers.Add(buffer);        
            totalVertexCount += segments + 1;    
            totalIndexCount += segments * 4;
        }

        public void AddSolidCone(float range, float spotAngle, Vector3 origin, Quaternion rotation, Color color) { 
        }

        public void AddArrow(int segments, float radius, float height, Vector3 origin, Quaternion rotation, Color color) {
            segments = MathUtil.Clamp(segments, 4, 32);
            DataStream buffer = new DataStream(FrameAllocator.AllocMemory(128), 128, true, true);
            buffer.Write(DebugDrawType.Arrow);
            buffer.Write(segments);
            buffer.Write(radius);
            buffer.Write(height);
            buffer.Write(origin);
            buffer.Write(rotation);
            buffer.Write(color);
            buffers.Add(buffer);        
            totalTriVertexCount += segments + 2;
            totalTriIndexCount += segments * 3;
        }

        public void AddCapsule(int segments, float radius, float height, Vector3 origin, Quaternion rotation, Color color) {
            Vector3 right = Vector3.Transform(MathHelper.Vec3Right, rotation);
            Vector3 forward = Vector3.Transform(MathHelper.Vec3Forward, rotation);
            Vector3 up = Vector3.Transform(MathHelper.Vec3Up, rotation);
            Quaternion xAxis = Quaternion.RotationAxis(forward, MathUtil.DegreesToRadians(90f));
            Quaternion yAxis = Quaternion.RotationAxis(right, MathUtil.DegreesToRadians(-90f));
            Quaternion zAxis = rotation;

            AddLine(origin + -right * radius - up * height * 0.5f, origin + -right * radius + up * height * 0.5f, color);
            AddLine(origin + right * radius - up * height * 0.5f, origin + right * radius + up * height * 0.5f, color);

            AddLine(origin + forward * radius - up * height * 0.5f, origin + forward * radius + up * height * 0.5f, color);
            AddLine(origin + -forward * radius - up * height * 0.5f, origin + -forward * radius + up * height * 0.5f, color);

            AddCircle(segments, radius, origin + up * (height * 0.5f), rotation, color);
            AddCircle(segments, radius, origin - up * (height * 0.5f), rotation, color);

            AddArc(segments, radius, 0f, 180f, origin + up * (height * 0.5f), Quaternion.RotationAxis(right, MathUtil.DegreesToRadians(90f)), color);
            AddArc(segments, radius, 0f, 180f, origin + up * (height * 0.5f), Quaternion.RotationAxis(up, MathUtil.DegreesToRadians(-90f)) * Quaternion.RotationAxis(right, MathUtil.DegreesToRadians(90f)), color);

            AddArc(segments, radius, 0f, 180f, origin - up * (height * 0.5f), yAxis, color);
            AddArc(segments, radius, 0f, 180f, origin - up * (height * 0.5f), Quaternion.RotationAxis(up, MathUtil.DegreesToRadians(90f)) * yAxis, color);
        }

        public void AddSolidCylinder(int segments, float radius, float height, Vector3 origin, Quaternion rotation, Color color) {
            segments = MathUtil.Clamp(segments, 4, 32);
            DataStream buffer = new DataStream(FrameAllocator.AllocMemory(128), 128, true, true);
            buffer.Write(DebugDrawType.SolidCylinder);
            buffer.Write(segments);
            buffer.Write(radius);
            buffer.Write(height);
            buffer.Write(origin);
            buffer.Write(rotation);
            buffer.Write(color);
            buffers.Add(buffer);        
            totalTriVertexCount += (segments + 1) * 2;
            totalTriIndexCount += segments * 2 + segments * 6;       
        }

        public void AddLine(Vector3 p1, Vector3 p2, Color color) {
            DataStream buffer = new DataStream(FrameAllocator.AllocMemory(128), 128, true, true);
            buffer.Write(DebugDrawType.Line);
            buffer.Write(p1);
            buffer.Write(p2);
            buffer.Write(color);
            buffers.Add(buffer);
            totalVertexCount += 2;
            totalIndexCount += 2;
        }

        public void AddAxis(Vector3 origin, Quaternion rotation, float size) {
            DataStream buffer = new DataStream(FrameAllocator.AllocMemory(128), 128, true, true);
            buffer.Write(DebugDrawType.Axis);
            buffer.Write(origin);
            buffer.Write(rotation);
            buffer.Write(size);
            buffers.Add(buffer);
            totalVertexCount += 6;     
            totalIndexCount += 6;       
        }

        public void AddAxis(Vector3 origin, Vector3 right, Vector3 forward, Vector3 up, float size) {
            AddLine(origin, origin + right * size, Color.Red);
            AddLine(origin, origin + forward * size, Color.Green);
            AddLine(origin, origin + up * size, Color.Blue);            
        }

        public void AddFrustum(float near, float far, float fov, float aspect, Vector3 origin, Vector3 right, Vector3 forward, Vector3 up) {
            DataStream buffer = new DataStream(FrameAllocator.AllocMemory(128), 128, true, true);
            buffer.Write(DebugDrawType.Frustum);
            buffer.Write(near);
            buffer.Write(far);
            buffer.Write(fov);
            buffer.Write(aspect);
            buffer.Write(origin);
            buffer.Write(right);
            buffer.Write(forward);
            buffer.Write(up);
            buffers.Add(buffer);
            totalVertexCount += 9;
            totalIndexCount += 24;
        }

        public void AddArc(int segments, float radius, float startAngle, float endAngle, Vector3 origin, Quaternion rotation, Color color) {
            segments = MathUtil.Clamp(segments, 2, 32);
			float totalAngle = endAngle - startAngle;
            int maxVerts = segments + 1;
            if(totalAngle > 360f) {
                totalAngle = 360f;
                maxVerts = segments;
            } else if(totalAngle > 90f) {
                int s = (int)MathF.Ceiling(totalAngle / 90f);
                if(segments < s) {
                    segments = s;
                }
            }
            DataStream buffer = new DataStream(FrameAllocator.AllocMemory(128), 128, true, true);
            buffer.Write(DebugDrawType.Arc);
            buffer.Write(maxVerts);
            buffer.Write(segments);
            buffer.Write(radius);
            buffer.Write(startAngle);
            buffer.Write(endAngle);
            buffer.Write(origin);
            buffer.Write(rotation);
            buffer.Write(color);
            buffers.Add(buffer);
            totalVertexCount += maxVerts;
            totalIndexCount += maxVerts * 2 - 2;            
        }

        public void AddFan(int segments, float radius, float startAngle, float endAngle, Vector3 origin, Quaternion rotation, Color color) {
            segments = MathUtil.Clamp(segments, 2, 32);
			float totalAngle = endAngle - startAngle;
            int maxVerts = segments + 1;
            if(totalAngle > 360f) {
                totalAngle = 360f;
                maxVerts = segments;
            } else if(totalAngle > 90f) {
                int s = (int)MathF.Ceiling(totalAngle / 90f);
                if(segments < s) {
                    segments = s;
                }
            }
            DataStream buffer = new DataStream(FrameAllocator.AllocMemory(128), 128, true, true);
            buffer.Write(DebugDrawType.Fan);
            buffer.Write(maxVerts);
            buffer.Write(segments);
            buffer.Write(radius);
            buffer.Write(startAngle);
            buffer.Write(endAngle);
            buffer.Write(origin);
            buffer.Write(rotation);
            buffer.Write(color);
            buffers.Add(buffer);
            totalTriVertexCount += maxVerts + 1;
            totalTriIndexCount += segments * 3;            
        }

        public void AddGrid(Vector2 segments, Vector2 size,Vector3 origin, Quaternion rotation, Color color) {

        }

        public void AddPic(Vector3 origin, Vector2 size, Quaternion rotation, Image2D image, Color color) {
            Vector3 right = Vector3.Transform(MathHelper.Vec3Right, rotation);
            Vector3 forward = Vector3.Transform(MathHelper.Vec3Forward, rotation);
            Vector3 up = Vector3.Transform(MathHelper.Vec3Up, rotation);
            Vector3 pt1 = size.X * -right + size.Y * up;
            Vector3 pt2 = size.X * right + size.Y * up;
            Vector3 pt3 = size.X * -right - size.Y * up;
            Vector3 pt4 = size.X * right - size.Y * up;
            ScreenPic pic = new ScreenPic();
            pic.pt0 = DrawVertex.DrawVertexColored(pt1, Vector2.Zero, color);
            pic.pt1 = DrawVertex.DrawVertexColored(pt2, new Vector2(1f, 0f), color);
            pic.pt2 = DrawVertex.DrawVertexColored(pt3, new Vector2(0f, 1f), color);
            pic.pt3 = DrawVertex.DrawVertexColored(pt4, new Vector2(1f, 1f), color);
            material.SetMainImage(image);
            pic.material = material.MakeLightingProxy();
            screenPics.Add(pic);
        }

        public float GetWidgetScale(Vector3 widgetOrigin, ref Matrix viewProjectionMatrix, float pixelWidth, ref Matrix projectionMatrix) {
            float w = Vector4.Transform(new Vector4(widgetOrigin, 1f), viewProjectionMatrix).W;
            return w * (4f / pixelWidth / projectionMatrix[0, 0]);
        }

        public float GetWidgetScale(Vector3 widgetOrigin, Vector3 viewOrigin) {
            return Vector3.Distance(widgetOrigin, viewOrigin) * 0.003f;
        }

        public void AddDirectionArrow(Vector3 origin, Quaternion rotation, Color color, float scale) {
            Vector3 up = Vector3.Transform(MathHelper.Vec3Up, rotation);
            int segments = 8;
            float radius = 1f;
            float height = 34;
            float halfHeight = height * 0.5f;
            AddSolidCylinder(segments, radius * scale, height * scale, origin + up * halfHeight * scale, rotation, color);
            AddArrow(segments, radius * scale * 3f, radius * scale * 10f, origin + up * height * scale, rotation, color);            
        }      

        public const float axisOriginBoxSize = 3f;
        public static readonly Vector3 axisOriginBoxMin = new Vector3(-axisOriginBoxSize, -axisOriginBoxSize, -axisOriginBoxSize);
        public static readonly Vector3 axisOriginBoxMax = new Vector3(axisOriginBoxSize, axisOriginBoxSize, axisOriginBoxSize);
        public const float axisArrowRadius = 3f;
        public const float axisArrowHeight = 10f;
        public const float axisCylinderRadius = 1f;
        public const float axisCylinderHeight = 40f;
        public const float axisCylinderHalHeight = 20f;

        public void AddPositionHandle(Vector3 origin, Quaternion rotation, float scale) {
            if(scale <= 0f) {
                return;
            }

            float boxSize = axisOriginBoxSize * scale;
            const int segments = 6;
            Vector3 right = Vector3.Transform(MathHelper.Vec3Right, rotation);
            Vector3 forward = Vector3.Transform(MathHelper.Vec3Forward, rotation);
            Vector3 up = Vector3.Transform(MathHelper.Vec3Up, rotation);
            Quaternion xAxis = rotation * Quaternion.RotationAxis(MathHelper.Vec3Forward, MathUtil.DegreesToRadians(90f));
            Quaternion yAxis = rotation * Quaternion.RotationAxis(MathHelper.Vec3Right, MathUtil.DegreesToRadians(-90f));
            Quaternion zAxis = rotation;
            AddSolidBox(axisOriginBoxMin * scale, axisOriginBoxMax * scale, origin, rotation, Color.Yellow);
            AddSolidCylinder(segments, axisCylinderRadius * scale, axisCylinderHeight * scale, origin + right * (axisCylinderHalHeight + axisOriginBoxSize) * scale, xAxis, Color.Red);
            AddSolidCylinder(segments, axisCylinderRadius * scale, axisCylinderHeight * scale, origin + forward * (axisCylinderHalHeight + axisOriginBoxSize) * scale, yAxis, Color.Green);
            AddSolidCylinder(segments, axisCylinderRadius * scale, axisCylinderHeight * scale, origin + up * (axisCylinderHalHeight + axisOriginBoxSize) * scale, zAxis, Color.Blue);
            AddArrow(segments, axisArrowRadius * scale, axisArrowHeight * scale, origin + right * (axisOriginBoxSize + axisCylinderHeight) * scale, xAxis, Color.Red);
            AddArrow(segments, axisArrowRadius * scale, axisArrowHeight * scale, origin + forward * (axisOriginBoxSize + axisCylinderHeight) * scale, yAxis, Color.Green);
            AddArrow(segments, axisArrowRadius * scale, axisArrowHeight * scale, origin + up * (axisOriginBoxSize + axisCylinderHeight) * scale, zAxis, Color.Blue);
        }

        public void AddArcRing(int segments, float innerRadius, float outerRadius, float startAngle, float endAngle, Vector3 origin, Quaternion rotation, Color color) {
            segments = MathUtil.Clamp(segments, 2, 32);
			float totalAngle = endAngle - startAngle;
            int maxVerts = segments + 1;
            if(totalAngle > 360f) {
                totalAngle = 360f;
                maxVerts = segments;
            } else if(totalAngle > 90f) {
                int s = (int)MathF.Ceiling(totalAngle / 90f);
                if(segments < s) {
                    segments = s;
                }
            }
            maxVerts *= 2;
            DataStream buffer = new DataStream(FrameAllocator.AllocMemory(128), 128, true, true);
            buffer.Write(DebugDrawType.ArcRing);
            buffer.Write(maxVerts);
            buffer.Write(segments);
            buffer.Write(innerRadius);
            buffer.Write(outerRadius);
            buffer.Write(startAngle);
            buffer.Write(endAngle);
            buffer.Write(origin);
            buffer.Write(rotation);
            buffer.Write(color);
            buffers.Add(buffer);
            totalTriVertexCount += maxVerts;
            totalTriIndexCount += segments * 2 * 3;                 
        }

        public const float axisInnerRadius = 40f;
        public const float axisOuterRadius = 50f;

        public void AddRotationHandle(Vector3 origin, Quaternion rotation, Vector3 viewOrigin, float scale) {
            if(scale <= 0f) {
                return;
            }  

            const int segments = 8;

            Vector3 right = Vector3.Transform(MathHelper.Vec3Right, rotation);
            Vector3 forward = Vector3.Transform(MathHelper.Vec3Forward, rotation);
            Vector3 up = Vector3.Transform(MathHelper.Vec3Up, rotation);

            Plane yz = new Plane(origin, right);
            Plane xz = new Plane(origin, forward);
            Plane xy = new Plane(origin, up);
            Vector3 viewToOrigin = origin - viewOrigin;
            if(Plane.DotNormal(xz, viewToOrigin) > 0f) {
                AddLine(origin, origin + -forward * axisInnerRadius * scale, Color.Green);
            } else {
                AddLine(origin, origin + forward * axisInnerRadius * scale, Color.Red);
            }
            if(Plane.DotNormal(xy, viewToOrigin) > 0f) {
                AddLine(origin, origin + -up * axisInnerRadius * scale, Color.Blue);
            } else {
                AddLine(origin, origin + up * axisInnerRadius * scale, Color.Red);
            }
            if(Plane.DotNormal(yz, viewToOrigin) > 0f) {
                AddLine(origin, origin + -right * axisInnerRadius * scale, Color.Red);
            } else {
                AddLine(origin, origin + right * axisInnerRadius * scale, Color.Red);
            }
            right = MathHelper.Vec3Right;// Vector3.Transform(MathHelper.Vec3Right, rotation);
            forward = MathHelper.Vec3Forward;// Vector3.Transform(MathHelper.Vec3Forward, rotation);
            up = MathHelper.Vec3Up;// Vector3.Transform(MathHelper.Vec3Up, rotation);
            Quaternion xRot = Quaternion.Identity;
            Quaternion yRot = Quaternion.Identity;
            Quaternion zRot = Quaternion.Identity;
            if(Plane.DotNormal(xy, viewToOrigin) < 0f) {
                if(Plane.DotNormal(xz, viewToOrigin) < 0f) {
                    xRot = Quaternion.RotationAxis(up, MathUtil.DegreesToRadians(90f)) * Quaternion.RotationAxis(right, MathUtil.DegreesToRadians(90f));
                } else {
                    xRot = Quaternion.RotationAxis(up, MathUtil.DegreesToRadians(-90f)) * Quaternion.RotationAxis(right, MathUtil.DegreesToRadians(90f));
                }
                if(Plane.DotNormal(yz, viewToOrigin) < 0f) {
                    yRot = Quaternion.RotationAxis(right, MathUtil.DegreesToRadians(90f));
                } else {
                    yRot = Quaternion.RotationAxis(up, MathUtil.DegreesToRadians(180f)) * Quaternion.RotationAxis(right, MathUtil.DegreesToRadians(90f));
                }            
            } else {
                if(Plane.DotNormal(xz, viewToOrigin) < 0f) {
                    xRot = Quaternion.RotationAxis(up, MathUtil.DegreesToRadians(90f)) * Quaternion.RotationAxis(right, MathUtil.DegreesToRadians(-90f));
                } else {
                    xRot = Quaternion.RotationAxis(up, MathUtil.DegreesToRadians(-90f)) * Quaternion.RotationAxis(right, MathUtil.DegreesToRadians(-90f));
                }
                if(Plane.DotNormal(yz, viewToOrigin) < 0f) {
                    yRot = Quaternion.RotationAxis(right, MathUtil.DegreesToRadians(-90f));
                } else {
                    yRot = Quaternion.RotationAxis(up, MathUtil.DegreesToRadians(180f)) * Quaternion.RotationAxis(right, MathUtil.DegreesToRadians(-90f));
                }
            }
            if(Plane.DotNormal(yz, viewToOrigin) < 0f) {
                if(Plane.DotNormal(xz, viewToOrigin) > 0f) {
                    zRot = Quaternion.RotationAxis(right, MathUtil.DegreesToRadians(-180f));
                } 
            } else {
                if(Plane.DotNormal(xz, viewToOrigin) < 0f) {
                    zRot = Quaternion.RotationAxis(forward, MathUtil.DegreesToRadians(180f));
                } else {
                    zRot = Quaternion.RotationAxis(up, MathUtil.DegreesToRadians(180f));
                }
            }         
            AddArcRing(segments, axisInnerRadius * scale, axisOuterRadius * scale, 0f, 90f, origin, rotation * xRot, Color.Red);                  
            AddArcRing(segments, axisInnerRadius * scale, axisOuterRadius * scale, 0f, 90f, origin, rotation * yRot, Color.Green);     
            AddArcRing(segments, axisInnerRadius * scale, axisOuterRadius * scale, 0f, 90f, origin, rotation * zRot, Color.Blue);
        }

        public void AddScaleHandle(Vector3 origin, Quaternion rotation, float scale) {
            if(scale <= 0f) {
                return;
            }

            float boxSize = axisOriginBoxSize * scale;
            const int segments = 6;
            Vector3 right = Vector3.Transform(MathHelper.Vec3Right, rotation);
            Vector3 forward = Vector3.Transform(MathHelper.Vec3Forward, rotation);
            Vector3 up = Vector3.Transform(MathHelper.Vec3Up, rotation);
            Quaternion xAxis = Quaternion.RotationAxis(forward, MathUtil.DegreesToRadians(90f));
            Quaternion yAxis = Quaternion.RotationAxis(right, MathUtil.DegreesToRadians(-90f));
            Quaternion zAxis = rotation;
            AddSolidBox(axisOriginBoxMin * scale, axisOriginBoxMax * scale, origin, rotation, Color.Yellow);
            AddSolidCylinder(segments, axisCylinderRadius * scale, axisCylinderHeight * scale, origin + right * (axisCylinderHalHeight + axisOriginBoxSize) * scale, xAxis, Color.Red);
            AddSolidCylinder(segments, axisCylinderRadius * scale, axisCylinderHeight * scale, origin + forward * (axisCylinderHalHeight + axisOriginBoxSize) * scale, yAxis, Color.Green);
            AddSolidCylinder(segments, axisCylinderRadius * scale, axisCylinderHeight * scale, origin + up * (axisCylinderHalHeight + axisOriginBoxSize) * scale, zAxis, Color.Blue);
            
            Vector3 min = axisOriginBoxMin * scale;
            Vector3 max = axisOriginBoxMax * scale;
            AddSolidBox(min, max, origin + right * (axisOriginBoxSize * 2f + axisCylinderHeight) * scale, xAxis, Color.Red);
            AddSolidBox(min, max, origin + forward * (axisOriginBoxSize * 2f + axisCylinderHeight) * scale, yAxis, Color.Green);
            AddSolidBox(min, max, origin + up * (axisOriginBoxSize * 2f + axisCylinderHeight) * scale, zAxis, Color.Blue);
        }

        public void AddJoints(Vector3 origin, Quaternion rotation, float scale, Color color, Matrix[] joints) {
            for(int i = 0; i < joints.Length; i++) {
                Vector3 pos = origin + Vector3.Transform(joints[i].TranslationVector, rotation);
                AddBox(-Vector3.One * scale, Vector3.One * scale, pos, Quaternion.Identity, color);
            }
        }

        public void AddWinding(DrawVertex[] vertices, int n) {
            AddWinding(new ReadOnlySpan<DrawVertex>(vertices), n);
        }

        public void AddWinding(ReadOnlySpan<DrawVertex> vertices, int n) {
            for(int i = 0; i < vertices.Length; i += n) {
                //AddLine(vertices[i].position, vertices[i + 1].position, vertices[i].jointWeights);
            }
        }

        internal void GetRenderData(out DebugToolRenderData drawData) {
            if(buffers.Count > 0 || screenPics.Count > 0) {
                drawData = new DebugToolRenderData();
                drawData.wireframeMaterial = wireframeMaterial;
                drawData.filledMaterial = filledMaterial;
                drawData.numTriVertices = totalTriVertexCount;
                drawData.numTriIndices = totalTriIndexCount;
                drawData.numVertices = totalVertexCount;
                drawData.numIndices = totalIndexCount;
                drawData.buffers = buffers.Count > 0 ? buffers.ToArray() : null;
                drawData.screenPics = screenPics.Count > 0 ? screenPics.ToArray() : null;
                totalTriVertexCount = 0;
                totalTriIndexCount = 0;
                totalVertexCount = 0;
                totalIndexCount = 0;
                buffers.Clear();
                screenPics.Clear();
            } else {
                drawData = null;
            }
        }
    }
}