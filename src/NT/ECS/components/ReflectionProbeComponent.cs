using System;
using System.Collections.Generic;
using SharpDX;

namespace NT
{
    public sealed class ReflectionProbeComponent {
        [Flags]
        internal enum Flags {
            Dirty = 1 << 0,
            Enabled = 1 << 1,
            Baked = 1 << 2,
            Realtime = 1 << 3
        }

        public float specularMultiplier {get; private set;}
        public float innerFalloff {get; private set;}
        public float distanceFade {get; private set;}
        public BoundingBox localBounds {get; private set;}

        internal Flags flags {get; private set;}
        internal int probeID;
        internal Matrix boxProjectionMatrix {get; private set;}

        public bool IsDirty() => flags.HasFlag(Flags.Dirty);
        public bool IsBaked() => flags.HasFlag(Flags.Baked);
        public bool IsRealtime() => flags.HasFlag(Flags.Realtime);
        public bool IsEnabled() => flags.HasFlag(Flags.Enabled);
        public bool IsValid() => IsEnabled() && (IsBaked() || IsRealtime());

        public void SetBaked(int id) {
            SetDirty();
            flags &= ~Flags.Realtime;
            flags |= Flags.Baked;
            probeID = id;
        }

        public void SetRealtime() {
            SetDirty();
            flags &= ~Flags.Baked;
            flags |= Flags.Realtime;            
        }

        public void SetEnabled(bool value = true) {
            SetDirty();
            if(value) {
                flags |= Flags.Enabled;
            } else {
                flags &= ~Flags.Enabled;
            }
        }

        public void SetDirty(bool value = true) {
            if(value) {
                flags |= Flags.Dirty;
            } else {
                flags &= ~Flags.Dirty;
            }
        }

        public ReflectionProbeComponent() {
            SetEnabled();
            SetDirty();
            specularMultiplier = 1f;
            innerFalloff = 0.9f;
            distanceFade = 1f;
            localBounds = new BoundingBox(-Vector3.One, Vector3.One);
        }

        public void SetBounds(Vector3 min, Vector3 max) {
            SetDirty();
            localBounds = new BoundingBox(min, max);
        }

        public void SetSpecularMultiplier(float value) {
            SetDirty();
            specularMultiplier = MathUtil.Clamp(value, 0f, 10f);
        }

        public void SetInnerFalloff(float value) {
            SetDirty();
            innerFalloff = MathUtil.Clamp(value, 0f, 1f);            
        }

        public void SetDistanceFade(float value) {
            SetDirty();
            distanceFade = value;
        }

        internal void Update(BoundingBox globalBounds) {
            Vector3 extents = globalBounds.Size * 0.5f;
            Matrix localToWorldMatrix = Matrix.Translation(globalBounds.Center);
            Matrix projectionMatrix = new Matrix(
                0.5f / extents.X, 0f, 0f, 0f,
                0f, -0.5f / extents.Y, 0f, 0f,
                0f, 0f, 0.5f / extents.Z, 0f,
                0.5f, 0.5f, 0.5f, 1f
            );  
            boxProjectionMatrix = Matrix.Invert(localToWorldMatrix) * projectionMatrix /* MathHelper.ScaleBiasMatrix*/;  
        }
    }
}