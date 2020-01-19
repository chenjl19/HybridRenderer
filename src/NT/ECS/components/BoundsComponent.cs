using System;
using System.Collections.Generic;
using SharpDX;

namespace NT
{
    internal sealed class BoundsComponent {
        public BoundingBox boundingBox;
        public BoundingSphere boundingSphere;
        public Vector2 screenSize;
        public Plane[] planes;
        public Vector3[] corners;

        public void InitFromHalWidth(Vector3 center, Vector3 halfWidth) {
            boundingBox = new BoundingBox(center - halfWidth, center + halfWidth);
            boundingSphere = BoundingSphere.FromBox(boundingBox);
        }

        public void UpdateClusteredDecalBounds(Matrix worldToViewMatrix, Vector3 size, TransformComponent transform) {
            if(planes == null || corners == null) {
                planes = new Plane[6];
                corners = new Vector3[8];
            }

            Vector3 origin = Vector3.TransformCoordinate(transform.GetPosition(), worldToViewMatrix);
            Vector3 right = Vector3.TransformNormal(transform.GetRight(), worldToViewMatrix);
            Vector3 forward = Vector3.TransformNormal(transform.GetForward(), worldToViewMatrix);
            Vector3 up = Vector3.TransformNormal(transform.GetUp(), worldToViewMatrix);
            Vector3 left = right * -1f;
            Vector3 backward = forward * -1f;
            Vector3 down = up * -1f;
            Vector3 extents = size * 0.5f;
            planes[0] = new Plane(right, extents.X + Vector3.Dot(-origin, right));
            planes[1] = new Plane(forward, extents.Y + Vector3.Dot(-origin, forward));
            planes[2] = new Plane(up, extents.Z + Vector3.Dot(-origin, up));
            planes[3] = new Plane(left, extents.X + Vector3.Dot(-origin, left));
            planes[4] = new Plane(backward, extents.Y + Vector3.Dot(-origin, backward));
            planes[5] = new Plane(down, extents.Z + Vector3.Dot(-origin, down));    
            var xv = right * extents.X;
            var yv = forward * extents.Y;
            var zv = up * extents.Z;
            corners[0] = origin + xv + yv + zv;
            corners[1] = origin + xv + yv - zv;
            corners[2] = origin - xv + yv - zv;
            corners[3] = origin - xv + yv + zv;
            corners[4] = origin + xv - yv + zv;
            corners[5] = origin + xv - yv - zv;
            corners[6] = origin - xv - yv - zv;
            corners[7] = origin - xv - yv + zv;
        }

        public void UpdateClusteredProbeBounds(Matrix worldToViewMatrix) {
            if(planes == null || corners == null) {
                planes = new Plane[6];
                corners = new Vector3[8];
            }
            Vector3 origin = Vector3.TransformCoordinate(boundingBox.Center, worldToViewMatrix);
            Vector3 right = Vector3.TransformNormal(MathHelper.Vec3Right, worldToViewMatrix);
            Vector3 forward = Vector3.TransformNormal(MathHelper.Vec3Forward, worldToViewMatrix);
            Vector3 up = Vector3.TransformNormal(MathHelper.Vec3Up, worldToViewMatrix);
            Vector3 left = right * -1f;
            Vector3 backward = forward * -1f;
            Vector3 down = up * -1f;
            Vector3 extents = boundingBox.Size * 0.5f;
            planes[0] = new Plane(right, extents.X + Vector3.Dot(-origin, right));
            planes[1] = new Plane(forward, extents.Y + Vector3.Dot(-origin, forward));
            planes[2] = new Plane(up, extents.Z + Vector3.Dot(-origin, up));
            planes[3] = new Plane(left, extents.X + Vector3.Dot(-origin, left));
            planes[4] = new Plane(backward, extents.Y + Vector3.Dot(-origin, backward));
            planes[5] = new Plane(down, extents.Z + Vector3.Dot(-origin, down));    
            corners = boundingBox.GetCorners();
            for(int i = 0; i < corners.Length; i++) {
                corners[i] = Vector3.TransformCoordinate(corners[i], worldToViewMatrix);
            }
        }

        public void UpdateClusteredLightBounds(Matrix worldToViewMatrix, LightComponent light, TransformComponent transform) {
            if(planes == null || corners == null) {
                planes = new Plane[6];
                corners = new Vector3[8];
            }
            Vector3 origin = Vector3.TransformCoordinate(light.worldPosition, worldToViewMatrix);
            Vector3 right = Vector3.TransformNormal(light.right, worldToViewMatrix);
            Vector3 forward = Vector3.TransformNormal(light.forward, worldToViewMatrix);
            Vector3 up = Vector3.TransformNormal(light.up, worldToViewMatrix);
            switch(light.type) {
                case LightType.Sphere: {
                    float range = light.range * 1000f;
                    BoundingBox aabb = new BoundingBox(origin - new Vector3(range, range, range), origin + new Vector3(range, range, range));
                    corners = aabb.GetCorners();
                    planes[0] = new Plane(MathHelper.Vec3Right, range + Vector3.Dot(-origin, MathHelper.Vec3Right));
                    planes[1] = new Plane(MathHelper.Vec3Forward, range + Vector3.Dot(-origin, MathHelper.Vec3Forward));
                    planes[2] = new Plane(MathHelper.Vec3Up, range + Vector3.Dot(-origin, MathHelper.Vec3Up));
                    planes[3] = new Plane(MathHelper.Vec3Left, range + Vector3.Dot(-origin, MathHelper.Vec3Left));
                    planes[4] = new Plane(MathHelper.Vec3Backward, range + Vector3.Dot(-origin, MathHelper.Vec3Backward));
                    planes[5] = new Plane(MathHelper.Vec3Down, range + Vector3.Dot(-origin, MathHelper.Vec3Down));    
                    break;
                }
                case LightType.Point: {
                    BoundingBox aabb = new BoundingBox(origin - new Vector3(light.range, light.range, light.range), origin + new Vector3(light.range, light.range, light.range));
                    corners = aabb.GetCorners();
                    planes[0] = new Plane(MathHelper.Vec3Right, light.range + Vector3.Dot(-origin, MathHelper.Vec3Right));
                    planes[1] = new Plane(MathHelper.Vec3Forward, light.range + Vector3.Dot(-origin, MathHelper.Vec3Forward));
                    planes[2] = new Plane(MathHelper.Vec3Up, light.range + Vector3.Dot(-origin, MathHelper.Vec3Up));
                    planes[3] = new Plane(MathHelper.Vec3Left, light.range + Vector3.Dot(-origin, MathHelper.Vec3Left));
                    planes[4] = new Plane(MathHelper.Vec3Backward, light.range + Vector3.Dot(-origin, MathHelper.Vec3Backward));
                    planes[5] = new Plane(MathHelper.Vec3Down, light.range + Vector3.Dot(-origin, MathHelper.Vec3Down));         
                    break;
                }
                case LightType.Spot: {
                    float tan = MathF.Tan(MathUtil.DegreesToRadians(light.spotAngle * 0.5f));
                    float near = 0f;// light.nearClipPlane;
                    float far = light.range;
                    float radius0 = tan * near;
                    float radius = tan * far;
                    corners[0] = origin + forward * near - right * radius0 + up * radius0;
                    corners[1] = origin + forward * near + right * radius0 + up * radius0;
                    corners[2] = origin + forward * near - right * radius0 - up * radius0;
                    corners[3] = origin + forward * near + right * radius0 - up * radius0;
                    corners[4] = origin + forward * far - right * radius + up * radius;
                    corners[5] = origin + forward * far + right * radius + up * radius;
                    corners[6] = origin + forward * far - right * radius - up * radius;
                    corners[7] = origin + forward * far + right * radius - up * radius;
                    planes[0] = new Plane(corners[6], corners[4], corners[0]);
                    planes[1] = new Plane(corners[3], corners[5], corners[7]);
                    planes[2] = new Plane(corners[0], corners[4], corners[5]);
                    planes[3] = new Plane(corners[7], corners[6], corners[2]);
                    planes[4] = new Plane(corners[0], corners[1], corners[2]);
                    planes[5] = new Plane(corners[6], corners[5], corners[4]);
                    break;
                }
            }
        }
    }
}