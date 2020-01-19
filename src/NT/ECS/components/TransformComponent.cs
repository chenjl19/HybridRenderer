using System;
using System.Collections.Generic;
using SharpDX;

namespace NT
{
    public sealed class HierarchyComponent {
        public Entity parent {get; internal set;}
        public Matrix localToParentMatrix {get; internal set;}
    }

    public sealed class TransformComponent {
        public enum Flags {
            Empty = 0,
            Dirty = 1 << 0,
            CacheInverted = 1 << 1,
            CacheLocalAngles = 1 << 2,
            CacheGlobal = 1 << 3,
            Child = 1 << 4,
            Static = 1 << 5,

        }

        Vector3 localScale;
        Vector3 localPosition;
        Quaternion localRotation;

        internal Flags flags;
        internal Vector3 localAngles;
        // Global
        Vector3 scale;
        Vector3 position;
        Quaternion rotation;

        public Matrix localToWorldMatrix {get; private set;}
        public Matrix worldToLocalMatrix {get; private set;}

        public TransformComponent() {
            SetDirty();
            localScale = Vector3.One;
            localRotation = Quaternion.Identity;
            localToWorldMatrix = Matrix.Identity;
        }

        public void SetLocalRotation(Vector3 angles) {
            SetDirty();
            localRotation = Quaternion.RotationAxis(MathHelper.Vec3Up, MathUtil.DegreesToRadians(angles.Z)) * 
                            Quaternion.RotationAxis(MathHelper.Vec3Right, MathUtil.DegreesToRadians(angles.X)) * 
                            Quaternion.RotationAxis(MathHelper.Vec3Forward, MathUtil.DegreesToRadians(-angles.Y));
        }

        public void SetLocalRotation(Quaternion r) {
            SetDirty();
            localRotation = r;
        }

        public void SetLocalRotation(float pitch, float roll, float yaw) {
            SetLocalRotation(new Vector3(pitch, roll, yaw));
        }

        public void SetLocalPosition(Vector3 v) {
            SetDirty();
            localPosition = v;
        }

        public void SetLocalPosition(float x, float y, float z) {
            SetLocalPosition(new Vector3(x, y, z));
        }

        public void AddLocalPosition(Vector3 p) {
            SetDirty();
            localPosition += p;
        }

        public void AddLocalPosition(float x, float y, float z) {
            AddLocalPosition(new Vector3(x, y, z));
        }

        public void SetLocalScale(Vector3 v) {
            SetDirty();
            localScale = v;
        }

        public void SetLocalScale(float x, float y, float z) {
            SetLocalScale(new Vector3(x, y, z));
        }

        public void SetLocalScale(float s) {
            SetLocalScale(new Vector3(s, s, s));
        }

        public Vector3 GetLocalPosition() {
            return localPosition;
        }

        public Quaternion GetLocalRotation() {
            return localRotation;
        }

        public Vector3 GetLocalScale() {
            return localScale;
        }

        public Vector3 GetLocalRight() {
            return Vector3.Transform(MathHelper.Vec3Right, localRotation);
        }

        public Vector3 GetLocalForward() {
            return Vector3.Transform(MathHelper.Vec3Forward, localRotation);
        }

        public Vector3 GetLocalUp() {
            return Vector3.Transform(MathHelper.Vec3Up, localRotation);
        }

        public Vector3 GetPosition() {
            return position;
        }

        public Quaternion GetRotation() {
            return rotation;
        }

        public Vector3 GetScale() {
            return scale;
        }

        public Vector3 GetRight() {
            return Vector3.Transform(MathHelper.Vec3Right, rotation);
        }

        public Vector3 GetForward() {
            return Vector3.Transform(MathHelper.Vec3Forward, rotation);
        }

        public Vector3 GetUp() {
            return Vector3.Transform(MathHelper.Vec3Up, rotation);
        }

        public void SetDirty(bool value = true) {
            if(value) {
                flags |= Flags.Dirty;
            } else {
                flags &= ~Flags.Dirty;
            }
        }

        public void SetStatic(bool value = true) {
            if(value) {
                flags |= Flags.Static;
            } else {
                flags &= ~Flags.Static;
            }            
        }

        public bool IsDirty() {return flags.HasFlag(Flags.Dirty);}
        public bool IsStatic() {return flags.HasFlag(Flags.Static);}
        public bool IsChild() => flags.HasFlag(Flags.Child);

        public void SetAxis(Vector3 right, Vector3 forward, Vector3 up) {
            SetDirty();
            Matrix3x3 mat = new Matrix3x3(
                right.X, right.Y, right.Z,
                forward.X, forward.Y, forward.Z,
                up.X, up.Y, up.Z
            );
            Quaternion.RotationMatrix(ref mat, out localRotation);            
        }

        public void Translate(Vector3 v) {
            SetDirty();
            localPosition += v;
        }

        public void Translate(float x, float y, float z) {
            Translate(new Vector3(x, y, z));
        }

        public void Scale(Vector3 v) {
            SetDirty();
            localScale *= v;
        }

        public void Scale(float x, float y, float z) {
            Scale(new Vector3(x, y, z));
        }

        public void Scale(float s) {
            Scale(new Vector3(s, s, s));
        }

        public void Rotate(float pitch, float roll, float yaw) {
            Rotate(new Vector3(pitch, roll, yaw));
        }

        public void Rotate(Vector3 angles) {
            SetDirty();
            localRotation *= Quaternion.RotationAxis(MathHelper.Vec3Up, MathUtil.DegreesToRadians(angles.Z)) * 
                            Quaternion.RotationAxis(MathHelper.Vec3Right, MathUtil.DegreesToRadians(angles.X)) * 
                            Quaternion.RotationAxis(MathHelper.Vec3Forward, MathUtil.DegreesToRadians(-angles.Y));
        }

        public void Rotate(Quaternion q) {
            SetDirty();
            localRotation *= q;
            localRotation.Normalize();
        }

        public void Reset() {
            SetDirty();
            localScale = Vector3.One;
            localRotation = Quaternion.Identity;
            localPosition = Vector3.Zero;
        }

        static Vector3 UpdateAngles(Quaternion rotation) {
            Matrix3x3 m = Matrix3x3.RotationQuaternion(rotation);
            m.Transpose();
            float yaw = 0f;
            float pitch = 0f;
            float roll = 0f;
            
            if(m[2, 1] < 0.9999f) {
                if(m[2, 1] > -0.9999f) {
                    pitch = MathF.Asin(m[2, 1]);
                    yaw = MathF.Atan2(-m[0, 1], m[1, 1]);
                    roll = MathF.Atan2(m[2, 0], m[2, 2]);
                } else {
                    // Not a unique solution: thetaY - thetaZ = atna2(r02, r00)
                    pitch = -MathF.PI * 0.5f;
                    yaw = -MathF.Atan2(m[0, 2], m[0, 0]);
                }
            } else {
                // Not a unique solution: thetaY + thetaZ = atna2(r02, r00)
                pitch = MathF.PI * 0.5f;
                yaw = MathF.Atan2(m[0, 2], m[0, 0]);
            }

            return new Vector3(MathUtil.RadiansToDegrees(pitch), MathUtil.RadiansToDegrees(roll), MathUtil.RadiansToDegrees(yaw));
        }

        internal uint lastFrameNum;
        internal void UpdateTransform() {
            if(IsDirty()) {
                lastFrameNum = Time.frameCount;
                SetDirty(false);
                localToWorldMatrix = Matrix.Scaling(localScale) * Matrix.RotationQuaternion(localRotation) * Matrix.Translation(localPosition);
                if(!IsChild()) {
                    if(flags.HasFlag(Flags.CacheInverted)) {
                        worldToLocalMatrix = Matrix.Invert(localToWorldMatrix);
                    }
                    if(flags.HasFlag(Flags.CacheGlobal)) {
                        scale = localScale;
                        rotation = localRotation;
                        position = localPosition;
                    }
                }
                if(flags.HasFlag(Flags.CacheLocalAngles)) {
                    localAngles = UpdateAngles(localRotation);
                }
            }
        }

        internal void UpdateTransform(TransformComponent parent, Matrix localToParentMatrix) {
            // 当前节点改变及父层次改变则需要更新最终全局矩阵，否则直接返回
            if(lastFrameNum != Time.frameCount && parent.lastFrameNum != Time.frameCount) {
                return;
            }
            //Console.WriteLine("UpdateInHierarchy");
            // 同步帧号
            lastFrameNum = Time.frameCount;
            Matrix localMatrix;
            if(parent.IsDirty()) {
                SetDirty();
                localMatrix = localToWorldMatrix;
            } else {
                localMatrix = Matrix.Scaling(localScale) * Matrix.RotationQuaternion(localRotation) * Matrix.Translation(localPosition);
            }
            localToWorldMatrix = localMatrix * localToParentMatrix * parent.localToWorldMatrix;
            if(flags.HasFlag(Flags.CacheInverted)) {
                worldToLocalMatrix = Matrix.Invert(localToWorldMatrix);
            }
            if(flags.HasFlag(Flags.CacheGlobal)) {
                localToWorldMatrix.Decompose(out scale, out rotation, out position);
            }
        }

        internal void ApplyTransform() {
            SetDirty();
            localToWorldMatrix.Decompose(out localScale, out localRotation, out localPosition);
        }
    }
}