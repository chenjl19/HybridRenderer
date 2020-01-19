using System;
using System.Collections.Generic;
using SharpDX;

namespace NT
{
    public class ScatteringVolumeComponent {
        [Flags]
        public enum Flags {
            Dirty = 1 << 0,
            Enabled = 1 << 1,
            HeightFog = 1 << 2
        }

        public Flags flags {get; private set;}
        public Vector3 size {get; private set;}
        public Vector3 absorption {get; private set;}
        public float absorptionIntensity {get; private set;}
        public float density {get; private set;}
        public float densityHeightK {get; private set;}
        public float densityVariationMin {get; private set;}
        public float densityVariationMax {get; private set;}
        public float densityFalloff {get; private set;}

        public void SetSize(Vector3 v) {
            SetDirty();
            size = Vector3.Clamp(v, Vector3.Zero, Vector3.One * 99999f);
        }

        public void SetDensity(float v, float falloff) {
            SetDirty();
            density = MathUtil.Clamp(v, 0f, 1f);
            densityFalloff = MathUtil.Clamp(falloff, 0f, 1f);
        }

        public void SetDensityVariation(float min, float max) {
            SetDirty();
            densityVariationMin = MathUtil.Clamp(min, 0f, 1f);
            densityVariationMax = MathUtil.Clamp(max, 0f, 1f);
        }

        public void SetAbsorption(Vector3 color, float intensity) {
            SetDirty();
            absorption = color;
            absorptionIntensity = MathUtil.Clamp(intensity, 0f, 100f);
        }

        public void DisableHeightFog() {
            SetDirty();
            flags &= ~Flags.HeightFog;
        }

        public void SetDensityHeightK(float heightK) {
            if(IsHeightFog()) {
                SetDirty();
                densityHeightK = MathUtil.Clamp(heightK, 0f, 1f);
            }
        }

        public void SetHeightFog(bool v = true) {
            SetDirty();
            if(v) {
                flags |= Flags.HeightFog;
            } else {
                flags &= ~Flags.HeightFog;
            }
        }

        public bool IsHeightFog() => flags.HasFlag(Flags.HeightFog);

        public void SetEnabled(bool value = true) {
            if(value) {
                flags |= Flags.Enabled;
            } else {
                flags &= ~Flags.Enabled;
            }
        }

        public bool IsEnabled() {
            return flags.HasFlag(Flags.Enabled);
        }

        public void SetDirty(bool value = true) {
            if(value) {
                flags |= Flags.Dirty;
            } else {
                flags &= ~Flags.Dirty;
            }
        }

        public bool IsDirty() {return flags.HasFlag(Flags.Dirty);}

        // frame data
        internal Vector3 origin;
        internal Vector3 right;
        internal Vector3 forward;
        internal Vector3 up;

        internal void ComputeMatrix(out Vector4 worldToVolumeMatrix0, out Vector4 worldToVolumeMatrix1, out Vector4 worldToVolumeMatrix2) {
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
            worldToVolumeMatrix0 = m.Column1;
            worldToVolumeMatrix1 = m.Column2;
            worldToVolumeMatrix2 = m.Column3;
        }

        public ScatteringVolumeComponent(){
            SetDensity(0.5f, 0.1f);
            SetDensityVariation(0.3f, 0.5f);
            SetAbsorption(Vector3.One, 1f);
            SetSize(Vector3.One);
            SetEnabled();
        }
    }    

    public enum LightType {
        Point,
        Spot,
        Directional,
        DirectionalBox,
        Sphere,
        Disc,
        Rectangle,
        Tube
    }

    public enum LightShadowResolutionMode {
        VeryLow,
        Low,
        Medium,
        High,
        VeryHigh
    }

    public enum LightShadowResolution {
        _128 = 128,
        _256 = 256,
        _512 = 512,
        _1024 = 1024,
        _2048 = 2048
    }

    public sealed class LightComponent {
        public const float DefualtDirectionalLightIntensity = MathF.PI; // lux
        public const float DefaultPunctualLightIntensity = 2f; // lumen
        public const float DefaultAreaLightIntensity = 200f; // lumen
        
        public const float DefaultRange = 2f;
        public const float DefaultSpotAngle = 60f;
        public const float MinRange = 0.05f;
        public const float MaxRange = 200f;
        public const float MinSpotAngle = 1f;
        public const float MaxSpotAngle = 179f;
        public const float MinAspectRatio = 0.05f;
        public const float MaxAspectRatio = 20f;
        public const float MinAreaWidth = 0.01f;
        public const float DefaultNearClipDistance = 0.1f;
        public const float DefaultSpecularMultiplier = 0.25f;
        public const float MaxSpecularMultiplier = 10f;
        public const float DefaultShadowBias = 0.0000f;
        public const LightShadowResolution DefaultShadowResolution = LightShadowResolution._512;

        public static float ConvertPointLightLumenToCandela(float intensity) => intensity;// / (4f / MathF.PI);
        public static float ConvertPointLightCandelaToLumen(float intensity) => intensity;// * (4.0f * MathF.PI);

        public enum Flags {
            Empty = 0,
            Enabled = 1 << 0,
            CastShadows = 1 << 1,
            Volumetrics = 1 << 2
        }

        public Flags flags {get; private set;}
        public Vector3 color {get; private set;}
        public LightType type {get; private set;}
        public float intensity {get; private set;}
        public float range {get; private set;}
        public float spotAngle {get; private set;}
        public float nearClipPlane {get; private set;}
        public float specularMultiplier {get; private set;}
        public float lightScattering {get; private set;}
        public float shadowBias {get; private set;}
        public LightShadowResolution shadowResolution {get; private set;}

        public LightComponent() {
            type = LightType.Point;
            range = DefaultRange;
            spotAngle = DefaultSpotAngle;
            nearClipPlane = DefaultNearClipDistance;
            specularMultiplier = DefaultSpecularMultiplier;
            shadowBias = DefaultShadowBias;
            shadowResolution = DefaultShadowResolution;
            lightScattering = 1f;
            SetColor(Vector3.One, DefaultPunctualLightIntensity);
            SetEnabled();
        }

        public void SetEnabled(bool value = true) {
            if(value) {
                flags |= Flags.Enabled;
            } else {
                flags &= ~Flags.Enabled;
            }
        }

        public bool IsEnabled() {
            return flags.HasFlag(Flags.Enabled);
        }

        public void SetFalloff(Vector4 scaleBias) {
            falloffScaleBias = scaleBias;
        }

        internal void SetProjector(string assetName, Vector4 scaleBias) {
            projectorName = assetName;
            projectorScaleBias = scaleBias;
        }

        public void SetSpecularMultiplier(float value) {
            specularMultiplier = MathUtil.Clamp(value, 0f, MaxSpecularMultiplier);
        }

        public void SetScattering(float value) {
            lightScattering = MathUtil.Clamp(value, 0f, 100f);
        }

        public void SetType(LightType lightType) {
            if(type == lightType) {
                return;
            }

            if(type == LightType.Directional || type == LightType.DirectionalBox) {
                if((int)lightType > (int)LightType.DirectionalBox) {
                    SetIntensity(DefaultPunctualLightIntensity);
                    SetSpotAngle(60f);
                }
            } else {
                if(lightType == LightType.Directional || lightType == LightType.DirectionalBox) {
                    SetIntensity(DefualtDirectionalLightIntensity);
                }
            }

            type = lightType;

            if(type == LightType.Spot && projectorScaleBias == Vector4.Zero) {
                SetProjector(Scene.defaultSpotProjectorName, Scene.GlobalScene.defaultSpotProjectorScaleBias);
            }
        }

        public float GetIntensity() {
            switch(type) {
                case LightType.Directional:
                case LightType.DirectionalBox:
                    return intensity;
                default:
                    return ConvertPointLightCandelaToLumen(intensity);
            }
        }

        public void SetIntensity(float value) {
            switch(type) {
                case LightType.Directional:
                case LightType.DirectionalBox:
                    intensity = value; // always in lux
                    break;
                case LightType.Point:
                case LightType.Spot:
                    intensity = ConvertPointLightLumenToCandela(value);
                    break;
            }
        }

        public void SetColor(Vector3 lightColor) {
            color = lightColor;
        }

        public void SetColor(Vector3 lightColor, float lightIntensity) {
            color = lightColor;
            SetIntensity(lightIntensity);
        }

        public void SetRange(float value) {
            if(type == LightType.Point || type == LightType.Spot) {
                range = MathUtil.Clamp(value, MinRange, MaxRange);
            }
        }

        public void SetSpotAngle(float value) {
            if(type == LightType.Spot) {
                spotAngle = MathUtil.Clamp(value, MinSpotAngle, MaxSpotAngle);
            }
        }

        public void SetCastShadow(bool value) {
            if(value) {
                flags |= Flags.CastShadows;
            } else {
                flags &= ~Flags.CastShadows;
            }
        }

        public bool IsCastingShadows() {return flags.HasFlag(Flags.CastShadows);}

        public void SetShadowBias(float v) {
            shadowBias = v;
        } 

        public void SetShadowNearClip(float v) {
            nearClipPlane = v < 0.1f ? 0.1f : v;
        }

        public void SetShadowResolution(LightShadowResolution resolution) {
            shadowResolution = resolution;
        }

        internal string name;
        internal Vector4 falloffScaleBias {get; private set;}
        internal Vector4 projectorScaleBias {get; private set;} // spotlight
        internal string falloffName {get; private set;}
        internal string projectorName {get; private set;}
        // frame data
        internal int shadowIndex;
        internal int shadowMapSize;
        internal Vector3 worldPosition;
        internal Vector3 right;
        internal Vector3 forward;
        internal Vector3 up;

        internal int ComputeShadowMapSize(ViewDef view) {
            shadowMapSize = (int)shadowResolution;
            return shadowMapSize;
            /*
            switch(type) {
                case LightType.Point: {
                }
                case LightType.Spot: {
                    break;
                }
                default: {
                    return (int)shadowResolution;
                }
            }
            */
        }

        public static float CalcGuardAnglePerspective(float angleInDeg, float resolution, float filterWidth, float normalBiasMax, float guardAngleMaxInDeg) {
            float angleInRad = MathUtil.DegreesToRadians(angleInDeg * 0.5f);
            float res = 2.0f / resolution;
            float texelSize = MathF.Cos(angleInRad) * res;
            float beta = normalBiasMax * texelSize * 1.4142135623730950488016887242097f;
            float guardAngle = MathF.Atan(beta);
            texelSize = MathF.Tan(angleInRad + guardAngle) * res;
            guardAngle = MathUtil.RadiansToDegrees(MathF.Atan((resolution + MathF.Ceiling(filterWidth)) * texelSize * 0.5f) * 2.0f) - angleInDeg;
            guardAngle *= 2.0f;
            return guardAngle < guardAngleMaxInDeg ? guardAngle : guardAngleMaxInDeg;
        }

        internal Matrix ComputePointLightShadowMatrix(int faceID, int shadowMapSize) {
            float fovBias = CalcGuardAnglePerspective(90f, shadowMapSize, 5f, 0, 79f);
            Matrix projectionMatrix = Matrix.PerspectiveFovRH(MathUtil.DegreesToRadians(90f + fovBias), 1f, nearClipPlane, range);
            return Matrix.Translation(-worldPosition) * MathHelper.PointLightViewMatrices[faceID] * projectionMatrix;
        }

        internal Matrix ComputeSpotLightShadowMatrix() {
            Matrix viewMatrix = new Matrix(
                right.X, forward.X, up.X, 0f,
                right.Y, forward.Y, up.Y, 0f,
                right.Z, forward.Z, up.Z, 0f,
                Vector3.Dot(-worldPosition, right), Vector3.Dot(-worldPosition, forward), Vector3.Dot(-worldPosition, up), 1f
            ) * MathHelper.ViewFlipMatrixRH; 
            Matrix projectionMatrix = Matrix.PerspectiveFovRH(MathUtil.DegreesToRadians(spotAngle), 1f, nearClipPlane, range); 
            return viewMatrix * projectionMatrix;
        }   

        public static readonly Vector3 fourCascadesSplit = new Vector3(0.03f, 0.08f, 0.4f);
        static Vector3[] cascadeFrustumCornersVS = new Vector3[8];
        static Vector3[] cascadeFrustumCornersWS = new Vector3[8];
        static Vector3[] cascadeFrustumCornersLS = new Vector3[8];

        void UpdateCascadeFrustumCornersVS(float near, float far, ref Matrix projectionMatrix, ref Vector3[] corners) {
            corners[0] = MathHelper.ClipToViewPoint(-1f, 1f, -near, ref projectionMatrix);
            corners[1] = MathHelper.ClipToViewPoint(1f, 1f, -near, ref projectionMatrix);
            corners[2] = MathHelper.ClipToViewPoint(1f, -1f, -near, ref projectionMatrix);
            corners[3] = MathHelper.ClipToViewPoint(-1f, -1f, -near, ref projectionMatrix);
            corners[4] = MathHelper.ClipToViewPoint(-1f, 1f, -far, ref projectionMatrix);
            corners[5] = MathHelper.ClipToViewPoint(1f, 1f, -far, ref projectionMatrix);
            corners[6] = MathHelper.ClipToViewPoint(1f, -1f, -far, ref projectionMatrix);
            corners[7] = MathHelper.ClipToViewPoint(-1f, -1f, -far, ref projectionMatrix);
        }

        void UpdateCascadeFrustumCornersWS(ref Matrix invViewMatrix, Vector3[] cornersVS, ref Vector3[] cornersWS) {
            for (int i = 0; i < cornersVS.Length; i++) {
                cornersWS[i] = Vector3.TransformCoordinate(cornersVS[i], invViewMatrix);
            }
        }

        void UpdateCascadeFrustumCornersWS(float near, float far, ref Matrix invViewProjectionMatrix, ref Vector3[] cornersWS) {
            cornersWS[0] = MathHelper.ClipToWorldPoint(-1f, 1f, -near, ref invViewProjectionMatrix);
            cornersWS[1] = MathHelper.ClipToWorldPoint(1f, 1f, -near, ref invViewProjectionMatrix);
            cornersWS[2] = MathHelper.ClipToWorldPoint(1f, -1f, -near, ref invViewProjectionMatrix);
            cornersWS[3] = MathHelper.ClipToWorldPoint(-1f, -1f, -near, ref invViewProjectionMatrix);
            cornersWS[4] = MathHelper.ClipToWorldPoint(-1f, 1f, -far, ref invViewProjectionMatrix);
            cornersWS[5] = MathHelper.ClipToWorldPoint(1f, 1f, -far, ref invViewProjectionMatrix);
            cornersWS[6] = MathHelper.ClipToWorldPoint(1f, -1f, -far, ref invViewProjectionMatrix);
            cornersWS[7] = MathHelper.ClipToWorldPoint(-1f, -1f, -far, ref invViewProjectionMatrix);
        }

        void UpdateCascadeFrustumCorners(float near, float far, ref Matrix invViewMatrix, ref Matrix projectionMatrix, ref Vector3[] cornersVS, ref Vector3[] cornersWS) {
            UpdateCascadeFrustumCornersVS(near, far, ref projectionMatrix, ref cornersVS);
            UpdateCascadeFrustumCornersWS(ref invViewMatrix, cornersVS, ref cornersWS);
        }

        internal Matrix ComputeDirectionalLightShadowMatrix(
            int cascadeIndex, 
            int numCascades, 
            Vector3 splitRatio, 
            float nearClip, 
            float farClip,
            int shadowMapSize,
            ref Matrix viewMatrix,
            ref Matrix projectionMatrix, 
            out BoundingSphere cullingSphere)
         {
            float halfShadowMapSize = shadowMapSize * 0.5f;
            float range = farClip - nearClip;
            float min = cascadeIndex == 0 ? nearClip : (nearClip + splitRatio[cascadeIndex - 1] * range);
            float max = numCascades == 1 || cascadeIndex == 3 ? farClip : (nearClip + splitRatio[cascadeIndex] * range);
            Matrix invViewMatrix = Matrix.Invert(viewMatrix);
            Vector3[] VectorUps = { MathHelper.Vec3Right, MathHelper.Vec3Forward, MathHelper.Vec3Up };
            Vector3 side = MathHelper.Vec3Right;
            Vector3 upDirection = MathHelper.Vec3Right;
            Vector3 direction = forward;
            foreach (var vectorUp in VectorUps) {
                if (Math.Abs(Vector3.Dot(direction, vectorUp)) < (1.0 - 0.0001)) {
                    side = Vector3.Normalize(Vector3.Cross(direction, vectorUp));
                    upDirection = Vector3.Normalize(Vector3.Cross(side, direction));
                    break;
                }
            }
            UpdateCascadeFrustumCornersVS(min, max, ref projectionMatrix, ref cascadeFrustumCornersVS);

            // Snap camera to texel units 
            // Technique from ShaderX7 - Practical Cascaded Shadows Maps -  p310-311 
            BoundingSphere sphereVS = BoundingSphere.FromPoints(cascadeFrustumCornersVS);
            Vector3 originWS = Vector3.TransformCoordinate(sphereVS.Center, invViewMatrix);
            float radius = sphereVS.Radius;
            cullingSphere = new BoundingSphere(originWS, radius);
            //cullingSphere.Radius += cullingSphere.Radius * 0.5f;
            Vector3 cascadeMaxBoundLS = new Vector3(radius);
            Vector3 cascadeMinBoundLS = -cascadeMaxBoundLS;
            float z = (float)Math.Ceiling(Vector3.Dot(originWS, upDirection) * halfShadowMapSize / radius) * radius / halfShadowMapSize;
            float x = (float)Math.Ceiling(Vector3.Dot(originWS, side) * halfShadowMapSize / radius) * radius / halfShadowMapSize;
            float y = Vector3.Dot(originWS, direction);
            originWS = upDirection * z + side * x + direction * y;
            originWS = originWS - direction * radius;
            Matrix sunViewMatrix = new Matrix(
                side.X, direction.X, upDirection.X, 0f,
                side.Y, direction.Y, upDirection.Y, 0f,
                side.Z, direction.Z, upDirection.Z, 0f,
                Vector3.Dot(-originWS, side), Vector3.Dot(-originWS, direction), Vector3.Dot(-originWS, upDirection), 1f
            ) * MathHelper.ViewFlipMatrixRH;

            Matrix lightProjectionMatrix = Matrix.OrthoOffCenterRH(-1f, 1f, -1f, 1f, -farClip, farClip);
            Vector3 bmin = Vector3.TransformCoordinate(cascadeMinBoundLS, lightProjectionMatrix);
            Vector3 bmax = Vector3.TransformCoordinate(cascadeMaxBoundLS, lightProjectionMatrix);

            Vector2 scale = new Vector2(2f / (bmax.X - bmin.X), 2f / (bmax.Y - bmin.Y));
            Vector2 offset = -0.5f * new Vector2(bmax.X + bmin.X, bmax.Y + bmin.Y) * scale;
            offset.X = MathF.Ceiling(offset.X * halfShadowMapSize) / halfShadowMapSize;
            offset.Y = MathF.Ceiling(offset.Y * halfShadowMapSize) / halfShadowMapSize;

            Matrix cropMatrix = new Matrix(
                scale.X, 0f, 0f, 0f,
                0f, scale.Y, 0f, 0f,
                0f, 0f, 1f, 0f,
                offset.X, offset.Y, 0f, 1f
            );
            return sunViewMatrix * lightProjectionMatrix * cropMatrix;
        }

        internal Matrix ComputeDirectionalLightShadowMatrix2(
            int cascadeIndex, 
            int numCascades, 
            Vector3 splitRatio, 
            float nearClip, 
            float farClip,
            int shadowMapSize,
            ref Matrix viewMatrix,
            ref Matrix projectionMatrix, 
            out BoundingSphere cullingSphere)
         {
            float range = farClip - nearClip;
            float min = cascadeIndex == 0 ? nearClip : (nearClip + splitRatio[cascadeIndex - 1] * range);
            float max = numCascades == 1 || cascadeIndex == 3 ? farClip : (nearClip + splitRatio[cascadeIndex] * range);
            float halfShadowMapSize = (float)shadowMapSize * 0.5f;
            Matrix viewToWorld = Matrix.Invert(viewMatrix);// Matrix.Invert(viewInfo.viewMatrix * viewInfo.projectionMatrixNojitter);

            // Fake value
            // It will be setup by next loop
            Vector3 side = MathHelper.Vec3Right;
            Vector3 upDirection = MathHelper.Vec3Right;
            Vector3 direction = forward;

            Vector3[] VectorUps = { MathHelper.Vec3Right, MathHelper.Vec3Forward, MathHelper.Vec3Up };
            // Select best Up vector
            // TODO: User preference?
            foreach (var vectorUp in VectorUps) {
                if (Math.Abs(Vector3.Dot(direction, vectorUp)) < (1.0 - 0.0001)) {
                    side = Vector3.Normalize(Vector3.Cross(vectorUp, direction));
                    upDirection = Vector3.Normalize(Vector3.Cross(direction, side));
                    break;
                }
            }

            UpdateCascadeFrustumCorners(min, max, ref viewToWorld, ref projectionMatrix, ref cascadeFrustumCornersVS, ref cascadeFrustumCornersWS);

            BoundingSphere boundingVS = BoundingSphere.FromPoints(cascadeFrustumCornersVS);
            Vector3 originWS = Vector3.TransformCoordinate(boundingVS.Center, viewToWorld);

            float radius = boundingVS.Radius;

            // Snap camera to texel units (so that shadow doesn't jitter when light doesn't change direction but camera is moving)
            // Technique from ShaderX7 - Practical Cascaded Shadows Maps -  p310-311 
            float z = (float)Math.Ceiling(Vector3.Dot(originWS, upDirection) * halfShadowMapSize / radius) * radius / halfShadowMapSize;
            float x = (float)Math.Ceiling(Vector3.Dot(originWS, side) * halfShadowMapSize / radius) * radius / halfShadowMapSize;
            float y = Vector3.Dot(originWS, direction);

            //target = up * x + side * y + direction * R32G32B32_Float.Dot(target, direction);
            originWS = upDirection * z + side * x + direction * y;

            Vector3 cascadeMaxBoundLS = new Vector3(radius, radius, radius);
            Vector3 cascadeMinBoundLS = -cascadeMaxBoundLS;
            Matrix lightViewMatrix = new Matrix(
                side.X, direction.X, upDirection.X, 0f,
                side.Y, direction.Y, upDirection.Y, 0f,
                side.Z, direction.Z, upDirection.Z, 0f,
                Vector3.Dot(-originWS, side), Vector3.Dot(-originWS, direction), Vector3.Dot(-originWS, upDirection), 1f
            ) * MathHelper.ViewFlipMatrixRH;
            float n = cascadeMaxBoundLS.Z - cascadeMinBoundLS.Z;

            Matrix lightProjectionMatrix = Matrix.OrthoOffCenterRH(-1f, 1f, -1f, 1f, -farClip, farClip);
            Vector3 mmin = Vector3.TransformCoordinate(cascadeMinBoundLS, lightProjectionMatrix);
            Vector3 mmax = Vector3.TransformCoordinate(cascadeMaxBoundLS, lightProjectionMatrix);

            Vector2 scale = new Vector2(2f / (mmax.X - mmin.X), 2f / (mmax.Y - mmin.Y));
            Vector2 offset = -0.5f * new Vector2(mmax.X + mmin.X, mmax.Y + mmin.Y) * scale;
            offset.X = MathF.Ceiling(offset.X * halfShadowMapSize) / halfShadowMapSize;
            offset.Y = MathF.Ceiling(offset.Y * halfShadowMapSize) / halfShadowMapSize;

            cullingSphere = new BoundingSphere();

            Matrix cropMatrix = Matrix.Identity;
            cropMatrix.ScaleVector = new Vector3(scale.X, scale.Y, 1f);
            cropMatrix.TranslationVector = new Vector3(offset.X, offset.Y, 0f);
            return lightViewMatrix * lightProjectionMatrix * cropMatrix;
        }

        internal Matrix ComputeDirectionalLightShadowMatrix1(
            int cascadeIndex, 
            int numCascades, 
            Vector3 splitRatio, 
            float nearClip, 
            float farClip,
            int shadowMapSize,
            ref Matrix viewMatrix,
            ref Matrix projectionMatrix, 
            out BoundingSphere cullingSphere)
         {
            float range = farClip - nearClip;
            float min = cascadeIndex == 0 ? nearClip : (nearClip + splitRatio[cascadeIndex - 1] * range);
            float max = numCascades == 1 || cascadeIndex == 3 ? farClip : (nearClip + splitRatio[cascadeIndex] * range);
            float halfShadowMapSize = (float)shadowMapSize * 0.5f;
            Matrix viewToWorld = Matrix.Invert(viewMatrix);// Matrix.Invert(viewInfo.viewMatrix * viewInfo.projectionMatrixNojitter);

            UpdateCascadeFrustumCorners(min, max, ref viewToWorld, ref projectionMatrix, ref cascadeFrustumCornersVS, ref cascadeFrustumCornersWS);
            Vector3 cascadeFrustumCenter = Vector3.Zero;
            for(int i = 0; i < 8; i++) {
                cascadeFrustumCenter += cascadeFrustumCornersWS[i];
            }
            cascadeFrustumCenter *= 1f / 8f;

            Vector3 lightUp = MathHelper.Vec3Up;
            float sphereRadius = 0f;
            for(int i = 0; i < 8; i++) {
                float dist = Vector3.Distance(cascadeFrustumCornersWS[i], cascadeFrustumCenter);
                sphereRadius = MathF.Max(sphereRadius, dist);
            }
            cullingSphere = new BoundingSphere(cascadeFrustumCenter, sphereRadius);
            Vector3 minExtents = new Vector3(-sphereRadius);
            Vector3 maxExtents = new Vector3(sphereRadius);
            Vector3 cascadeExtents = maxExtents - minExtents;
            Vector3 lightViewOrigin = cascadeFrustumCenter + (-forward * -minExtents.Y);
            Matrix lightViewMatrix = Matrix.LookAtRH(lightViewOrigin, cascadeFrustumCenter, lightUp);
            Matrix lightProjectionMatrix = Matrix.OrthoOffCenterRH(minExtents.X, maxExtents.X, minExtents.Z, maxExtents.Z, 0f, cascadeExtents.Y);
            return lightViewMatrix * lightProjectionMatrix;
        }

        internal void ComputeLightMatrix(out Matrix matrix) {
            matrix = Matrix.Identity;
            switch(type) {
                case LightType.Point: {
                    break;
                }
                case LightType.Spot: {
                    float halfSpotRad = MathUtil.DegreesToRadians(spotAngle * 0.5f);
                    float cs = MathF.Cos(halfSpotRad);
                    float ss = MathF.Sin(halfSpotRad);
                    float cotanHalfSpotAngle = cs / ss;
                    float scale = 1f / range;
                    Matrix viewMatrix = new Matrix(
                        right.X, forward.X, up.X, 0f,
                        right.Y, forward.Y, up.Y, 0f,
                        right.Z, forward.Z, up.Z, 0f,
                        Vector3.Dot(-worldPosition, right), Vector3.Dot(-worldPosition, forward), Vector3.Dot(-worldPosition, up), 1f
                    ) * MathHelper.ViewFlipMatrixRH; 
                    Matrix projectionMatrix = Matrix.Identity; 
                    projectionMatrix[2, 3] = 2.0f / cotanHalfSpotAngle;
                    projectionMatrix[3, 3] = 0;
                    Matrix scaleMatrix = Matrix.Scaling(new Vector3(1f / range, 1f / range, -1f / range));
                    matrix = viewMatrix * scaleMatrix * projectionMatrix;
                    break;
                }
            }
        }
    }
}