using System;
using SharpDX;

namespace NT
{
    public sealed class CameraComponent {
        public enum Flags {
            Empty,
            Dirty = 1 << 1,
            NeedUpdateFrustumVectors = 1 << 2,
            NeedClusteredLighting = 1 << 3
        }

        Flags flags;
        public float fov {get; private set;}
        public float aspect {get; private set;}
        public float nearClipPlane {get; private set;}
        public float farClipPlane {get; private set;}
        public int width {get; private set;}
        public int height {get; private set;}
        public Matrix worldToViewMatrix {get; private set;}
        public Matrix viewToWorldMatrix {get; private set;}
        public Matrix projectionMatrix {get; private set;}
        public Vector3 frustumVectorLT {get; private set;}
        public Vector3 frustumVectorRT {get; private set;}
        public Vector3 frustumVectorLB {get; private set;}
        public Vector3 frustumVectorRB {get; private set;}
        public ClusteredLighting clusteredLighting {get; private set;}

        internal Vector3 viewOrigin {get; private set;}
        internal Vector3 viewRight {get; private set;}
        internal Vector3 viewForward {get; private set;}
        internal Vector3 viewUp {get; private set;}

        public CameraComponent() {
            fov = MathUtil.DegreesToRadians(60f);
            aspect = 1920f / 1080f;
            nearClipPlane = 0.1f;
            farClipPlane = 500f;
            SetDirty();
        }

        public void SetDirty(bool value = true) {
            if(value) {
                flags |= Flags.Dirty;
            } else {
                flags &= ~Flags.Dirty;
            }
        }

        public void SetUpdateFrustumVectors(bool value = true) {
            if(value) {
                flags |= Flags.NeedUpdateFrustumVectors;
            } else {
                flags &= ~Flags.NeedUpdateFrustumVectors;
            }
        }

        public void SetClusteredLighting(bool value = true) {
            if(value) {
                flags |= Flags.NeedClusteredLighting;
            } else {
                flags &= ~Flags.NeedClusteredLighting;
            }
        }

        public bool IsDirty() {return flags.HasFlag(Flags.Dirty);}
        public bool IsClusteredLighting() {return flags.HasFlag(Flags.NeedClusteredLighting);}

        public void SetClippingPlanes(float near, float far) {
            nearClipPlane = near;
            farClipPlane = far;
            SetDirty();
        }

        public void SetAspect(float aspect) {
            this.aspect = aspect;
            SetDirty();
        }

        public void SetAspect(int w, int h) {
            width = w;
            height = h;
            this.aspect = (float)w / (float)h;
            SetDirty();
        }

        public void SetFOV(float fovAngle) {
            fov = MathUtil.DegreesToRadians(fovAngle);
            SetDirty();
        }

        public void UpdateCamera() {
            if(IsDirty()) {
                projectionMatrix = Matrix.PerspectiveFovRH(fov, aspect, nearClipPlane, farClipPlane);
                if(IsClusteredLighting()) {
                    if(clusteredLighting == null) {
                        clusteredLighting = new ClusteredLighting(this);
                    }
                    clusteredLighting.UpdateClusterFrustums();
                }
            }
        }

        public void ComputeFrustumVectors(Vector3 right, Vector3 forward, Vector3 up) {
            float halfHeight = nearClipPlane * MathF.Tan(fov * 0.5f);
            Vector3 toUp = up * halfHeight;
            Vector3 toRight = right * halfHeight * aspect;
            Vector3 toNear = forward * nearClipPlane;
            Vector3 topL = toNear + toUp - toRight;
            Vector3 topR = toNear + toUp + toRight;
            Vector3 bottomL = toNear - toUp - toRight;
            Vector3 bottomR = toNear - toUp + toRight;
            float scale = topL.Length() / nearClipPlane;  
            frustumVectorLT = Vector3.Normalize(topL) * scale;
            frustumVectorRT = Vector3.Normalize(topR) * scale;
            frustumVectorLB = Vector3.Normalize(bottomL) * scale;
            frustumVectorRB = Vector3.Normalize(bottomR) * scale;
        }

        public void TransformCamera(TransformComponent transform) {
            Vector3 origin = transform.GetLocalPosition();
            Vector3 right = transform.GetRight();
            Vector3 forward = transform.GetForward();
            Vector3 up = transform.GetUp();

            viewOrigin = origin;
            viewRight = right;
            viewForward = forward;
            viewUp = up;

            worldToViewMatrix = new Matrix(
                right.X, forward.X, up.X, 0f,
                right.Y, forward.Y, up.Y, 0f,
                right.Z, forward.Z, up.Z, 0f,
                Vector3.Dot(-origin, right), Vector3.Dot(-origin, forward), Vector3.Dot(-origin, up), 1f
            ) * MathHelper.ViewFlipMatrixRH;  
            viewToWorldMatrix = Matrix.Invert(worldToViewMatrix);
            if(flags.HasFlag(Flags.NeedUpdateFrustumVectors)) {
                ComputeFrustumVectors(right, forward, up);
            }               
        }
    }
}