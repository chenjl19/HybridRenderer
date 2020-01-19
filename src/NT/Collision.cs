using System;
using SharpDX;

namespace NT 
{
    public static class CollisionEx {
        public static bool RayIntersectsCylinder(Ray ray, Vector3 p, Vector3 q, float radius) {
            Vector3 d = q - p;
            Vector3 m = ray.Position - p;
            Vector3 n = ray.Direction * 10000f - ray.Position;
            float md = Vector3.Dot(m, d);
            float nd = Vector3.Dot(n, d);
            float dd = Vector3.Dot(d, d);
            if(md < 0f && md + nd < 0f) {
                return false;
            }
            if(md > dd && md + nd > dd) {
                return false;
            }
            float nn = Vector3.Dot(n, n);
            float mn = Vector3.Dot(m, n);
            float a = dd * nn - nd * nd;
            float k = Vector3.Dot(m, m) - radius * radius;
            float c = dd * k - md * md;
            if(MathF.Abs(a) < float.Epsilon && c <= 0f) {
                return true;
            }
            float b = dd * mn - nd * md;
            float discr = b * b - a * c;
            if(discr < 0f) {
                return false;
            }
            float t = (-b - MathF.Sqrt(discr)) / a;
            if(t < 0f || t > 1f) {
                return false;
            }
            if(md + t * nd < 0f) {
                if(nd <= 0f) {
                    return false;
                }
                t = -md / nd;
                return k + 2 * t * (mn + t * nn) <= 0f;
            } else if(md + t * nd > dd) {
                if(nd >= 0f) {
                    return false;
                }
                t = (dd - md) / nd;
                return k + dd - 2 * md + t * (2 * (mn - nd) + t * nn) <= 0f;
            }
            return true;
        }

        static void GetBoxToPlanePVertexNVertex(BoundingBox box, Vector3 planeNormal, out Vector3 p, out Vector3 n) {
            p = box.Minimum;
            if (planeNormal.X >= 0)
                p.X = box.Maximum.X;
            if (planeNormal.Y >= 0)
                p.Y = box.Maximum.Y;
            if (planeNormal.Z >= 0)
                p.Z = box.Maximum.Z;

            n = box.Maximum;
            if (planeNormal.X >= 0)
                n.X = box.Minimum.X;
            if (planeNormal.Y >= 0)
                n.Y = box.Minimum.Y;
            if (planeNormal.Z >= 0)
                n.Z = box.Minimum.Z;
        }

        public static bool Overlap(Plane[] planes, Vector3[] corners, BoundingBox aabb) {
            for (int i = 0; i < 6; i++) {
                GetBoxToPlanePVertexNVertex(aabb, planes[i].Normal, out Vector3 p, out _);
                if(Collision.PlaneIntersectsPoint(ref planes[i], ref p) == PlaneIntersectionType.Back) {
                    return false;
                }
            }
            
            int result = 0;
            result = 0; for (int i = 0; i < 8; i++) result += ((corners[i].X > aabb.Maximum.X) ? 1 : 0); if (result == 8 ) return false;
            result = 0; for (int i = 0; i < 8; i++) result += ((corners[i].X < aabb.Minimum.X) ? 1 : 0); if (result == 8 ) return false;
            result = 0; for (int i = 0; i < 8; i++) result += ((corners[i].Y > aabb.Maximum.Y) ? 1 : 0); if (result == 8 ) return false;
            result = 0; for (int i = 0; i < 8; i++) result += ((corners[i].Y < aabb.Minimum.Y) ? 1 : 0); if (result == 8 ) return false;
            result = 0; for (int i = 0; i < 8; i++) result += ((corners[i].Z > aabb.Maximum.Z) ? 1 : 0); if (result == 8 ) return false;
            result = 0; for (int i = 0; i < 8; i++) result += ((corners[i].Z < aabb.Minimum.Z) ? 1 : 0); if (result == 8 ) return false;

            return true;
        }
    }
}