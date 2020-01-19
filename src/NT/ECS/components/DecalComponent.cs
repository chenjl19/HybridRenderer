using System;
using System.Collections.Generic;
using SharpDX;

namespace NT
{
    public sealed class DecalComponent {
        [Flags]
        public enum Flags
        {
            // DF_ALBEDO		 	( 1 << 0 )
            // DF_SPECULAR		 	( 1 << 1 )
            // DF_SMOOTHNESS		( 1 << 2 )
            // DF_ALPHA_MASK		( 1 << 3 )
            // DF_EMISSION			( 1 << 4 )
            // DF_THRESHOLD_BLEND	( 1 << 5 )
            // DF_NORMAL_OVERRIDE	( 1 << 6 )
            // DF_NORMAL_INVERT	( 1 << 7 )
            // DF_NORMAL_OVERLAY	( 1 << 8 )
            // DF_DYNAMIC_ONLY		( 1 << 9 )	// stamped but attached to a dynamic object and it should only project onto dynamic geometry
            // DF_STATIC_ONLY		( 1 << 10 )	// stamped but not attached to dynamic geometry and should only project onto static geometry
            // DF_IGNORE_FADE		( 1 << 11 ) // cpu ignore flag for fading
            Empty,
            Albedo = 1 << 0,
            Specular = 1 << 1,
            Smoothness = 1 << 2,
            NormalOverride = 1 << 6,
            NormalInvert = 1 << 7,
            NormalOverlay = 1 << 8,
            DynamicModelOnly = 1 << 9,
            StaticModelOnly = 1 << 10,

            Enabled = 1 << 11,
            Dirty = 1 << 12
        }

        public enum NormalBlend {
            Linear,
            Override,
            Overlay
        }

        public enum Receiver {
            StaticModel,
            DynamicModel,
            All
        }  

        public Flags flags {get; private set;}
        public Color baseColor {get; private set;}
        public Color emissiveColor {get; private set;}
        public Color specularColor {get; private set;}
        public float emissiveIntensity {get; private set;}
        public float specularIntensity {get; private set;}
        public float smoothness {get; private set;}
        public float albedoBlendRatio {get; private set;}
        public float specularBlendRatio {get; private set;}
        public float smoothnessBlendRatio {get; private set;}
        public float normalBlendRatio {get; private set;}
        public Vector3 size {get; private set;}
        public Receiver receiver {get; private set;}
        public NormalBlend normalBlend {get; private set;}
        public bool invertNormal {get; private set;}

        public DecalComponent() {
            size = Vector3.One;
            baseColor = Color.White;
            emissiveColor = Color.Black;
            specularColor = Color.White;
            smoothness = 1f;
            albedoBlendRatio = 1f;
            specularBlendRatio = 1f;
            smoothnessBlendRatio = 1f;
            normalBlendRatio = 1f;
            receiver = Receiver.All;
            normalBlend = NormalBlend.Overlay;
            emissiveIntensity = 1f;
            specularIntensity = 1f;
            albedoImageName = "";
            specularImageName = "";
            SetDirty();
        }

        public bool IsEnabled() => flags.HasFlag(Flags.Enabled);

        public void SetEnabled(bool v = true) {
            if(v) {
               flags |= Flags.Enabled;
            } else {
                flags &= ~Flags.Enabled;
            }
        }

        public void SetSize(Vector3 value) {
            size = Vector3.Clamp(value, Vector3.Zero, Vector3.One * 1000f);
        }

        public void SetSmoothness(float v) {
            SetDirty();
            flags |= Flags.Smoothness;
            smoothness = MathUtil.Clamp(v, 0f, 1f);
        }

        public void SetBaseColor(Color color) {
            SetDirty();
            baseColor = color;
        }

        public void SetAlbedoImage(string assetName, Vector4 scaleBias) {
            SetDirty();
            flags |= Flags.Albedo;
            albedoImageName = assetName;
            albedoImageScaleBias = scaleBias;
        }

        public void SetSpecularImage(string assetName, Vector4 scaleBias) {
            SetDirty();
            flags |= Flags.Specular;
            specularImageName = assetName;
            specularImageScaleBias = scaleBias;
        }

        internal void SetNormalImage(string assetName, Vector4 scaleBias) {
            SetDirty();
            normalImageName = assetName;
            normalImageScaleBias = scaleBias;
        }

        public void SetEmissiveColor(Color color, float intensity) {
            SetDirty();
            emissiveColor = color;
            emissiveIntensity = MathUtil.Clamp(intensity, 0f, 1000f);
        }

        public void SetSpecularColor(Color color, float intensity) {
            SetDirty();
            specularColor = color;
            specularIntensity = MathUtil.Clamp(intensity, 0f, 1000f);
        }

        public void SetAlbedoBlendRatio(float v) {
            SetDirty();
            albedoBlendRatio = MathUtil.Clamp(v, 0f, 1f);
        }

        public void SetSpecularBlendRatio(float v) {
            SetDirty();
            specularBlendRatio = MathUtil.Clamp(v, 0f, 1f);
        }

        public void SetSmoothnessBlendRatio(float v) {
            SetDirty();
            smoothnessBlendRatio = MathUtil.Clamp(v, 0f, 1f);
        }

        public void SetNormalBlendRatio(float v) {
            SetDirty();
            normalBlendRatio = MathUtil.Clamp(v, 0f, 1f);
        }

        public void SetNormalBlendMode(NormalBlend mode, bool invert = false) {
            SetDirty();
            normalBlend = mode;
            invertNormal = invert;
            if(normalBlend == NormalBlend.Overlay) {
                flags |= Flags.NormalOverlay;
                flags &= ~Flags.NormalOverride;
            } else if(normalBlend == NormalBlend.Override) {
                flags |= Flags.NormalOverride;
                flags &= ~Flags.NormalOverlay;
            } else {
                flags &= ~Flags.NormalOverlay;
                flags &= ~Flags.NormalOverride;
            }
            if(invertNormal) {
                flags |= Flags.NormalInvert;
            } else {
                flags &= ~Flags.NormalInvert;
            }
        }

        public bool HasAlbedo() {return flags.HasFlag(Flags.Albedo);}
        public bool HasSpecular() {return flags.HasFlag(Flags.Specular);}

        internal void SetDirty(bool v = true) {
            if(v) {
               flags |= Flags.Dirty;
            } else {
                flags &= ~Flags.Dirty;
            }
        }

        // frame data
        internal Vector3 origin;
        internal Vector3 right;
        internal Vector3 forward;
        internal Vector3 up;
        internal string normalImageName;
        internal string specularImageName;
        internal string albedoImageName;
        internal Vector4 albedoImageScaleBias;
        internal Vector4 specularImageScaleBias;
        internal Vector4 normalImageScaleBias;

        internal void ComputeMatrix(out Vector4 worldToDecalMatrix0, out Vector4 worldToDecalMatrix1, out Vector4 worldToDecalMatrix2) {
            Vector3 extents = size * 0.5f;
            Matrix worldToLocalMatrix = new Matrix(
                right.X, forward.X, up.X, 0f,
                right.Y, forward.Y, up.Y, 0f,
                right.Z, forward.Z, up.Z, 0f,
                Vector3.Dot(-origin, right), Vector3.Dot(-origin, forward), Vector3.Dot(-origin, up), 1f
            );
            Matrix projectionMatrix = new Matrix(
                0.5f / extents.X, 0f, 0f, 0f,
                0f, -0.5f / extents.Y, 0f, 0f,
                0f, 0f, 0.5f / extents.Z, 0f,
                0.5f, 0.5f, 0.5f, 1f
            );   
            Matrix m = worldToLocalMatrix * projectionMatrix;
            worldToDecalMatrix0 = m.Column1;
            worldToDecalMatrix1 = m.Column2;
            worldToDecalMatrix2 = m.Column3;
        }
    }
}