using System;
using SharpDX;

namespace NT
{
        public enum AxisHandleType {
        None,
        X,
        Y,
        Z,
        All
    }

    public sealed class Scaler {
        public Vector3 origin;
        public Vector3 initialOffset;
        public Vector3 initialPosition;
        public Vector3 initialScale;
        public AxisHandleType handleType = AxisHandleType.None;
        public Vector3 scale;
        Vector3 planeNormal;
        Vector3 normalToRemove;

        public Scaler() {
            scale = Vector3.One;
            initialScale = Vector3.One;
        }

        void GetHitPoint(Ray ray, out Vector3 pt) {
            pt = origin;
            if(MathF.Abs(Vector3.Dot(ray.Direction, planeNormal)) > float.Epsilon) {
                Plane plane = new Plane(origin, planeNormal);
                Collision.RayIntersectsPlane(ref ray, ref plane, out pt);
            }
        }

        public void DragBegin(Ray ray, Vector3 widgetOrigin, Quaternion rotation, Vector3 viewForward, float widgetScale) {
            origin = widgetOrigin;
            handleType = AxisHandleType.None;
            BoundingSphere sphere = new BoundingSphere(origin, RenderDebug.axisOriginBoxSize * widgetScale * 2f);
            if(Collision.RayIntersectsSphere(ref ray, ref sphere, out Vector3 _)) {
                planeNormal = viewForward;
                GetHitPoint(ray, out Vector3 pt);
                handleType = AxisHandleType.All;
                initialPosition = sphere.Center;
                initialOffset = pt - initialPosition;
            } else {
                Vector3 right = Vector3.Transform(MathHelper.Vec3Right, rotation);
                Vector3 forward = Vector3.Transform(MathHelper.Vec3Forward, rotation);
                Vector3 up = Vector3.Transform(MathHelper.Vec3Up, rotation);
                float xDot = MathF.Abs(Vector3.Dot(right, viewForward));
                float yDot = MathF.Abs(Vector3.Dot(forward, viewForward));
                float zDot = MathF.Abs(Vector3.Dot(up, viewForward));
                float radius = RenderDebug.axisOriginBoxSize * 2 * widgetScale;
                BoundingSphere sphere1 = new BoundingSphere(origin + right * (RenderDebug.axisOriginBoxSize * 2f + RenderDebug.axisCylinderHeight) * widgetScale, radius);
                BoundingSphere sphere2 = new BoundingSphere(origin + forward * (RenderDebug.axisOriginBoxSize * 2f + RenderDebug.axisCylinderHeight) * widgetScale, radius);
                BoundingSphere sphere3 = new BoundingSphere(origin + up * (RenderDebug.axisOriginBoxSize * 2f + RenderDebug.axisCylinderHeight) * widgetScale, radius);
                if(Collision.RayIntersectsSphere(ref ray, ref sphere1, out float _)) {
                    handleType = AxisHandleType.X;
                    if(yDot > zDot) {
                        planeNormal = forward;
                        normalToRemove = up;
                    } else {
                        planeNormal = up;
                        normalToRemove = forward;
                    }
                } else if(Collision.RayIntersectsSphere(ref ray, ref sphere2, out float _)) {
                    handleType = AxisHandleType.Y;
                    if(xDot > yDot) {
                        planeNormal = up;
                        normalToRemove = right;
                    } else {
                        planeNormal = right;
                        normalToRemove = up;
                    }
                } else if(Collision.RayIntersectsSphere(ref ray, ref sphere3, out float _)) {
                    handleType = AxisHandleType.Z;
                    if(xDot > zDot) {
                        planeNormal = forward;
                        normalToRemove = right;
                    } else {
                        planeNormal = right;
                        normalToRemove = forward;
                    }
                }

                if(handleType != AxisHandleType.None) {
                    GetHitPoint(ray, out initialPosition);
                    //initialPosition = origin;
                    //initialOffset = pt - initialPosition;
                }
            }
        }

        Vector3 move;

        public void DragUpdate(Ray ray, Vector3 viewForward) {
            if(handleType == AxisHandleType.None) {
                return;
            }
            if(handleType == AxisHandleType.All) {
                GetHitPoint(ray, out Vector3 pt);
                origin = pt - initialOffset;
            } else {
                GetHitPoint(ray, out Vector3 pt);
                move = pt - initialPosition;
                float removeScale = Vector3.Dot(move, normalToRemove);
                move -= normalToRemove * removeScale;
                if(handleType == AxisHandleType.X) {
                    scale.X = initialScale.X + move.X * MathF.Sign(initialScale.X);
                } else if(handleType == AxisHandleType.Y) {
                    scale.Y = initialScale.Y + move.Y * MathF.Sign(initialScale.Y);
                } else {
                    scale.Z = initialScale.Z + move.Z * MathF.Sign(initialScale.Z);
                }
            }
        }

        public Vector3 GetMove() {
            return Vector3.One + move;
        }

        public void DragEnd() {
            handleType = AxisHandleType.None;
            initialScale = scale;
        }

        public static void Draw(Scaler scaler, Quaternion rotation, float widgetScale) {
            Vector3 origin = scaler.origin;
            Vector3 scale = scaler.GetMove();
            AxisHandleType handleType = scaler.handleType;
            Vector3 right = Vector3.Transform(MathHelper.Vec3Right, rotation);
            Vector3 forward = Vector3.Transform(MathHelper.Vec3Forward, rotation);
            Vector3 up = Vector3.Transform(MathHelper.Vec3Up, rotation);
            Vector3 min = RenderDebug.axisOriginBoxMin * widgetScale;
            Vector3 max = RenderDebug.axisOriginBoxMax * widgetScale;
            Vector3 xOrigin = origin + right * (RenderDebug.axisOriginBoxSize * 2f + RenderDebug.axisCylinderHeight) * scale.X * widgetScale;
            Vector3 yOrigin = origin + forward * (RenderDebug.axisOriginBoxSize * 2f + RenderDebug.axisCylinderHeight) * scale.Y * widgetScale;
            Vector3 zOrigin = origin + up * (RenderDebug.axisOriginBoxSize * 2f + RenderDebug.axisCylinderHeight) * scale.Z * widgetScale;

            switch(handleType) {
                case AxisHandleType.None:
                    Scene.GlobalScene.renderDebug.AddScaleHandle(origin, rotation, widgetScale);
                    break;
                case AxisHandleType.All: {
                    Scene.GlobalScene.renderDebug.AddLine(origin - right * 99999f, origin + right * 99999f, Color.Red);
                    Scene.GlobalScene.renderDebug.AddLine(origin - forward * 9999f, origin + forward * 99999f, Color.Green);
                    Scene.GlobalScene.renderDebug.AddLine(origin - up * 99999f, origin + up * 99999f, Color.Blue);
                    Scene.GlobalScene.renderDebug.AddScaleHandle(origin, rotation, widgetScale);
                    break;
                }
                case AxisHandleType.X: {
                    Scene.GlobalScene.renderDebug.AddSolidBox(min, max, xOrigin, rotation, Color.Red);
                    Scene.GlobalScene.renderDebug.AddLine(origin - right * 99999f, origin + right * 99999f, Color.Red);
                    break;
                }
                case AxisHandleType.Y: {
                    Scene.GlobalScene.renderDebug.AddSolidBox(min, max, yOrigin, rotation, Color.Green);
                    Scene.GlobalScene.renderDebug.AddLine(origin - forward * 99999f, origin + forward * 99999f, Color.Green);
                    break;
                }
                case AxisHandleType.Z: {
                    Scene.GlobalScene.renderDebug.AddSolidBox(min, max, zOrigin, rotation, Color.Blue);
                    Scene.GlobalScene.renderDebug.AddLine(origin - up * 99999f, origin + up * 99999f, Color.Blue);
                    break;
                }
            }            
        }
    }

    public sealed class Rotator {
        public Vector3 origin;
        public Quaternion rotation = Quaternion.Identity;
        public AxisHandleType handleType = AxisHandleType.None;
        Vector3 axis0;
        Vector3 axis1;
        Vector2 axisScreenDir;

        public Rotator() {
        }

        Vector2 WorldToUVPoint(Vector3 pt, ref Matrix viewProjectionMatrix) {
            var pos = Vector4.Transform(new Vector4(pt, 1), viewProjectionMatrix);
            pos /= pos.W;
            return new Vector2(pos.X, pos.Y) * new Vector2(0.5f, -0.5f) + 0.5f;
        }

        public void DragBegin(Ray ray, Vector3 origin, float widgetScale, Vector3 viewOrigin, Vector3 viewForward, Matrix viewProjectionMatrix) {
            this.origin = origin;
            handleType = AxisHandleType.None;
            BoundingSphere sphere = new BoundingSphere(origin, RenderDebug.axisOuterRadius);
            if(!Collision.RayIntersectsSphere(ref ray, ref sphere, out Vector3 _)) {
                return;
            }

            Vector3 right = Vector3.Transform(MathHelper.Vec3Right, rotation);
            Vector3 forward = Vector3.Transform(MathHelper.Vec3Forward, rotation);
            Vector3 up = Vector3.Transform(MathHelper.Vec3Up, rotation);
            Vector3[] planeNormals = new Vector3[] {right, forward, up};

            Vector3 viewToOrigin = origin - viewOrigin;
            bool mirrorYZ = Vector3.Dot(right, viewToOrigin) >= float.Epsilon;
            bool mirrorXY = Vector3.Dot(up, viewToOrigin) >= float.Epsilon;
            bool mirrorXZ = Vector3.Dot(forward, viewToOrigin) >= float.Epsilon;
            Vector3 renderRight = mirrorYZ ? -right : right;
            Vector3 renderForward = mirrorXZ ? -forward : forward;
            Vector3 renderUp = mirrorXY ? -up : up;
            Vector3[] quadrantAxis = new Vector3[] {renderForward, renderUp, renderRight, renderUp, renderRight, renderForward};
            int[] index = new int[] { 1, 2, 0, 0, 1, 2, 0, 2, 1 };

            for(int i = 0; i < 3; i++) {
                Vector3 planeNormal = planeNormals[i];
                Vector3 initialHitPoint;
                Plane plane = new Plane(origin, planeNormal);
                Collision.RayIntersectsPlane(ref ray, ref plane, out initialHitPoint);
                Vector3 dir = initialHitPoint - origin;
                var dot1 = Vector3.Dot(dir, quadrantAxis[i * 2 + 0]);
                var dot2 = Vector3.Dot(dir, quadrantAxis[i * 2 + 1]);
                if(dot1 > float.Epsilon && dot2 > float.Epsilon) {
                    int l = 3;
                    Vector3 worldOrigin = new Vector3(origin[index[i * l + 0]], origin[index[i * l + 1]], origin[index[i * l + 2]]);
                    Vector3 tmpPt = Vector3.Transform(initialHitPoint, Quaternion.Invert(rotation));
                    Vector3 localPt = new Vector3(tmpPt[index[i * l + 0]], tmpPt[index[i * l + 1]], tmpPt[index[i * l + 2]]);
                    Vector3 localOrigin = Vector3.Transform(origin, Quaternion.Invert(rotation));
                    Vector3 localDir = localPt - new Vector3(localOrigin[index[i * l + 0]], localOrigin[index[i * l + 1]], localOrigin[index[i * l + 2]]);
                    float sqrLength = localDir.LengthSquared();
                    bool ret = (sqrLength >= ((RenderDebug.axisInnerRadius * widgetScale) * (RenderDebug.axisInnerRadius * widgetScale))) &&
                        (sqrLength <= ((RenderDebug.axisOuterRadius * widgetScale) * (RenderDebug.axisOuterRadius * widgetScale)));
                    if(ret) {
                        handleType = (AxisHandleType)(i + 1);
                        axis0 = quadrantAxis[i * 2 + 0];
                        axis1 = quadrantAxis[i * 2 + 1];
                        Vector2 pt0 = WorldToUVPoint(origin + quadrantAxis[i * 2 + 0] * 64f, ref viewProjectionMatrix);
                        Vector2 pt1 = WorldToUVPoint(origin + quadrantAxis[i * 2 + 1] * 64f, ref viewProjectionMatrix);
                        float d = 1f;
                        if(handleType == AxisHandleType.Z) {
                            d = (mirrorYZ ^ mirrorXZ) ? -1f : 1f;
                        }
                        axisScreenDir = Vector2.Normalize(pt1 - pt0) * d;
                        break;
                    }
                }
            }
        }

        float deltaAngle = 0f;
        float totalDeltaAngle = 0f;
        float everyDeleteAngle = 0f;
        float everyTotalAngle = 10f;
        float totalAngle = 0f;

        public float GetTotalAngle() {
            return totalAngle;
        }

        public void DragUpdate(Ray ray, Vector2 dragDir) {
            if(handleType == AxisHandleType.None) {
                return;
            }

            Scene.GlobalScene.renderDebug.AddLine(origin, origin + axis0 * 2f, Color.Red);
            Scene.GlobalScene.renderDebug.AddLine(origin, origin + axis1 * 2f, Color.Red);
            Scene.GlobalScene.renderDebug.AddLine(origin, origin + Vector3.Normalize(axis1 - axis0)* 2f, Color.Red);
            float delta = Vector2.Dot(axisScreenDir, dragDir);

            totalDeltaAngle += delta;
            everyDeleteAngle += delta;
            
            if(MathF.Abs(everyDeleteAngle) > everyTotalAngle) {
                float angle = everyDeleteAngle > 0f ? everyTotalAngle : -everyTotalAngle;
                if (handleType == AxisHandleType.X) {
                    rotation *= Quaternion.RotationAxis(MathHelper.Vec3Right, MathUtil.DegreesToRadians(angle));
                } else if (handleType == AxisHandleType.Y) {
                    rotation *= Quaternion.RotationAxis(MathHelper.Vec3Forward, MathUtil.DegreesToRadians(angle));
                } else if (handleType == AxisHandleType.Z) {
                    rotation *= Quaternion.RotationAxis(MathHelper.Vec3Up, MathUtil.DegreesToRadians(angle));
                }
                everyDeleteAngle -= angle;
                totalAngle += angle;
                if(totalAngle > 360f || totalAngle < -360f) {
                    totalAngle = 0f;
                }
            }
        }

        public void DragEnd() {
            handleType = AxisHandleType.None;
            totalAngle = 0;
        }

        public static void Draw(Rotator rotator, Vector3 viewOrigin, float widgetScale) {
            Vector3 origin = rotator.origin;
            Quaternion rotation = rotator.rotation;
            AxisHandleType handleType = rotator.handleType;
            float totalAngle = rotator.GetTotalAngle();
            switch(handleType) {
                case AxisHandleType.None:
                    Scene.GlobalScene.renderDebug.AddRotationHandle(origin, rotation, viewOrigin, widgetScale);
                    break;
                case AxisHandleType.X: {
                    rotation *= Quaternion.RotationAxis(MathHelper.Vec3Forward, MathUtil.DegreesToRadians(90f));
                    Scene.GlobalScene.renderDebug.AddArcRing(8, RenderDebug.axisInnerRadius * widgetScale, RenderDebug.axisOuterRadius * widgetScale, 0f, totalAngle, origin, rotation, Color.Yellow);
                    break;
                }
                case AxisHandleType.Y: {
                    rotation *= Quaternion.RotationAxis(MathHelper.Vec3Right, MathUtil.DegreesToRadians(90f));
                    Scene.GlobalScene.renderDebug.AddArcRing(8, RenderDebug.axisInnerRadius * widgetScale, RenderDebug.axisOuterRadius * widgetScale, 0f, totalAngle, origin, rotation, Color.Yellow);
                    break;
                }
                case AxisHandleType.Z: {
                    Scene.GlobalScene.renderDebug.AddArcRing(8, RenderDebug.axisInnerRadius * widgetScale, RenderDebug.axisOuterRadius * widgetScale, 0f, totalAngle, origin, rotation, Color.Yellow);
                    break;
                }
            }
        }
    }

    public sealed class Translator {
        public Vector3 origin;
        public Vector3 initialOffset;
        public Vector3 initialPosition;
        public AxisHandleType handleType = AxisHandleType.None;

        Vector3 planeNormal;
        Vector3 normalToRemove;

        void GetHitPoint(Ray ray, out Vector3 pt) {
            pt = origin;
            if(MathF.Abs(Vector3.Dot(ray.Direction, planeNormal)) > float.Epsilon) {
                Plane plane = new Plane(origin, planeNormal);
                Collision.RayIntersectsPlane(ref ray, ref plane, out pt);
            }
        }

        public void DragBegin(Ray ray, Quaternion rotation, Vector3 viewForward, float widgetScale) {
            handleType = AxisHandleType.None;
            BoundingSphere sphere = new BoundingSphere(origin, RenderDebug.axisOriginBoxSize * widgetScale * 2f);
            if(Collision.RayIntersectsSphere(ref ray, ref sphere, out Vector3 _)) {
                planeNormal = viewForward;
                GetHitPoint(ray, out Vector3 pt);
                handleType = AxisHandleType.All;
                initialPosition = sphere.Center;
                initialOffset = pt - initialPosition;
            } else {
                Vector3 right = Vector3.Transform(MathHelper.Vec3Right, rotation);
                Vector3 forward = Vector3.Transform(MathHelper.Vec3Forward, rotation);
                Vector3 up = Vector3.Transform(MathHelper.Vec3Up, rotation);
                float xDot = MathF.Abs(Vector3.Dot(right, viewForward));
                float yDot = MathF.Abs(Vector3.Dot(forward, viewForward));
                float zDot = MathF.Abs(Vector3.Dot(up, viewForward));
                float radius = RenderDebug.axisArrowRadius * widgetScale;
                float halHeight = (RenderDebug.axisCylinderHeight + RenderDebug.axisArrowHeight) * widgetScale * 0.5f + radius;
                Vector3 xAxisOrigin = origin + right * halHeight;
                Vector3 yAxisOrigin = origin + forward * halHeight;
                Vector3 zAxisOrigin = origin + up * halHeight;
                if(CollisionEx.RayIntersectsCylinder(ray, xAxisOrigin - right * halHeight, xAxisOrigin + right * halHeight, radius)) {
                    handleType = AxisHandleType.X;
                    if(yDot > zDot) {
                        planeNormal = forward;
                        normalToRemove = up;
                    } else {
                        planeNormal = up;
                        normalToRemove = forward;
                    }
                } else if(CollisionEx.RayIntersectsCylinder(ray, yAxisOrigin - forward * halHeight, yAxisOrigin + forward * halHeight, radius)) {
                    handleType = AxisHandleType.Y;
                    if(xDot > yDot) {
                        planeNormal = up;
                        normalToRemove = right;
                    } else {
                        planeNormal = right;
                        normalToRemove = up;
                    }
                } else if(CollisionEx.RayIntersectsCylinder(ray, zAxisOrigin - up * halHeight, zAxisOrigin + up * halHeight, radius)) {
                    handleType = AxisHandleType.Z;
                    if(xDot > zDot) {
                        planeNormal = forward;
                        normalToRemove = right;
                    } else {
                        planeNormal = right;
                        normalToRemove = forward;
                    }
                }

                if(handleType != AxisHandleType.None) {
                    GetHitPoint(ray, out Vector3 pt);
                    initialPosition = origin;
                    initialOffset = pt - initialPosition;
                }
            }
        }

        public void DragUpdate(Ray ray, Vector3 viewForward) {
            if(handleType == AxisHandleType.None) {
                return;
            }
            if(handleType == AxisHandleType.All) {
                GetHitPoint(ray, out Vector3 pt);
                origin = pt - initialOffset;
            } else {
                GetHitPoint(ray, out Vector3 pt);
                Vector3 move = pt - origin - initialOffset;
                float removeScale = Vector3.Dot(move, normalToRemove);
                move -= normalToRemove * removeScale;
                Vector3 eyeNewDir = origin + move - ray.Position;
                if(Vector3.Dot(eyeNewDir, ray.Direction) <= 0f) {
                    move = Vector3.Zero;
                }
                origin += move;
            }
        }

        public void DragEnd() {
            handleType = AxisHandleType.None;
        }

        public static void Draw(Translator translator, Quaternion rotation, float widgetScale) {
            Vector3 origin = translator.origin;
            AxisHandleType handleType = translator.handleType;
            switch(handleType) {
                case AxisHandleType.None:
                case AxisHandleType.All:
                    Scene.GlobalScene.renderDebug.AddPositionHandle(origin, rotation, widgetScale);
                    break;
                case AxisHandleType.X: {
                    Vector3 right = Vector3.Transform(MathHelper.Vec3Right, rotation);
                    Scene.GlobalScene.renderDebug.AddPositionHandle(origin, rotation, widgetScale);
                    Scene.GlobalScene.renderDebug.AddLine(origin - right * 9999f, origin + right * 9999f, Color.Red);
                    break;
                }
                case AxisHandleType.Y: {
                    Vector3 forward = Vector3.Transform(MathHelper.Vec3Forward, rotation);
                    Scene.GlobalScene.renderDebug.AddPositionHandle(origin, rotation, widgetScale);
                    Scene.GlobalScene.renderDebug.AddLine(origin - forward * 9999f, origin + forward * 9999f, Color.Green);
                    break;
                }
                case AxisHandleType.Z: {
                    Vector3 up = Vector3.Transform(MathHelper.Vec3Up, rotation);
                    Scene.GlobalScene.renderDebug.AddPositionHandle(origin, rotation, widgetScale);
                    Scene.GlobalScene.renderDebug.AddLine(origin - up * 9999f, origin + up * 9999f, Color.Blue);
                    break;
                }
            }            
        }
    }
}