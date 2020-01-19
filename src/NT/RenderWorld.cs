using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Mathematics;

namespace NT
{
    public static class RenderingCVars {
        public static readonly CVarInteger numSunShadowCascades;
        public static readonly CVarVector ambientColor;
        public static readonly CVarFloat ambientIntensity;

        static RenderingCVars() {
            numSunShadowCascades = new CVarInteger("r_numSunShadowCascades", 3, "", CVar.Flags.Dev, 1, 4);
            ambientColor = new CVarVector("r_ambientColor", new Vector4(1, 1, 1, 1), "", CVar.Flags.Color);
        }
    }

    public static class SkylighingCVars {
        public static readonly CVarVector sunColor;
        public static readonly CVarFloat sunIntensity;
        public static readonly CVarFloat sunDiskScale;
        public static readonly CVarBool decoupleSunColorFromSky;
        public static readonly CVarFloat viewMinDistance;
        public static readonly CVarFloat viewMaxDistance;
        public static readonly CVarFloat radiusPlanet;
        public static readonly CVarFloat radiusAtmosphere;
        public static readonly CVarFloat mieDensityScale;
        public static readonly CVarFloat rayleighHeight;
        public static readonly CVarVector rayleighColor;
        public static readonly CVarVector mieColor;
        public static readonly CVarFloat mieHeight;
        public static readonly CVarFloat mieAnisotropy;
        public static readonly CVarFloat scatteringDensityScale;
        public static readonly CVarString skyboxBg;
        public static readonly CVarInteger skyboxType;

        static SkylighingCVars() {
            sunColor = new CVarVector("r_skylighingSunColor", new Vector4(1.00f, 0.98039f, 0.92157f, 0.00f), "Skylighting SunColor", CVar.Flags.Color);
            sunIntensity = new CVarFloat("r_skylighingSunIntensity", 10f, "Skylighting SunIntensity", CVar.Flags.None, 0f, 1000f);
            sunDiskScale = new CVarFloat("r_skylighingSunDiskScale", 1f, "Skylighting SunDiskScale", CVar.Flags.None, 0f, 1000f);
            decoupleSunColorFromSky = new CVarBool("r_skylighingDecoupleSunColorFromSky", false, "Skylighting DecoupleSunColorFromSky", CVar.Flags.None);
            viewMinDistance = new CVarFloat("r_skylighingViewDistance", 0.06f, "Skylighting SunIntensity", CVar.Flags.None, 0f, 1000f);
            viewMaxDistance = new CVarFloat("r_skylighingViewDistance", 8000f, "Skylighting SunIntensity", CVar.Flags.None, 0f, 100000f);
            radiusPlanet = new CVarFloat("r_skylighingRadiusPlanet", 6360f, "Skylighting RadiusPlanet", CVar.Flags.None, 0f, 100000f, 1f);
            radiusAtmosphere = new CVarFloat("r_skylighingRadiusAtmosphere", 6420f, "Skylighting RadiusAtmosphere", CVar.Flags.None, 0f, 100000f, 1f);
            mieDensityScale = new CVarFloat("r_skylighingMieDensityScale", 1.3f, "Skylighting MieDensityScale", CVar.Flags.None, 0f, 100f);
            rayleighHeight = new CVarFloat("r_skylighingRayleighHeight", 7994f, "Skylighting RayleighHeight", CVar.Flags.None, 0f, 100000f, 1f);
            rayleighColor = new CVarVector("r_skylighingRayleighColor", new Vector4(0.36625f, 0.67244f, 1.00f, 0.00f), "Skylighting RayleighColor", CVar.Flags.Color);
            mieColor = new CVarVector("r_skylighingMieColor", new Vector4(1.00f, 0.93333f, 0.87843f, 0.00f), "Skylighting MieColor", CVar.Flags.Color);
            mieHeight = new CVarFloat("r_skylighingMieHeight", 1200f, "Skylighting MieHeight", CVar.Flags.None, 0f, 100000f);
            mieAnisotropy = new CVarFloat("r_skylighingMieAnisotropy", 0.76f, "Skylighting MieAnisotropy", CVar.Flags.None, 0f, 1f, 0.01f);
            scatteringDensityScale = new CVarFloat("r_skylighingScatteringDensityScale", 3f, "Skylighting ScatteringDensityScale", CVar.Flags.None, 0f, 100f);
            skyboxBg = new CVarString("r_skyboxBackground", @"textures/skybgs/03-28_Sunset.bimg", "Skybox background image", CVar.Flags.Dev);
        }
    }

    public static class PostProcessingCVars {
        public static readonly CVarFloat bloomThreshold;
        public static readonly CVarFloat aoDiffusePower;
        public static readonly CVarFloat aoSpecularPower;
        public static readonly CVarFloat volumetricLightingDistance;
        public static readonly CVarFloat autoExposureMinLuminance;
        public static readonly CVarFloat autoExposureLuminanceScale;
        public static readonly CVarFloat autoExposureLuminanceBlendRatio;
        public static readonly CVarFloat autoExposureMin;
        public static readonly CVarFloat autoExposureMax;
        public static readonly CVarInteger taaJitterMode;
        public static readonly CVarBool useSkylighting;
        public static readonly CVarBool useLocalLightScattering;
        public static readonly CVarBool useSSR;
        public static readonly CVarInteger ssrDitherSampleOffset;
        public static readonly CVarFloat ssrSmoothnessThreshold;
        public static readonly CVarFloat ssrScale;

        static PostProcessingCVars() {
            bloomThreshold = new CVarFloat("r_bloomThreshold", 2f, "Bloom luma threshold", CVar.Flags.None, 0f, 1000f);
            aoDiffusePower = new CVarFloat("r_aoDiffusePower", 1f, "AO DiffusePower", CVar.Flags.None, 0f, 10f);
            aoSpecularPower = new CVarFloat("r_aoSpecularPower", 1f, "AO SpecularPower", CVar.Flags.None, 0f, 10f);
            volumetricLightingDistance = new CVarFloat("r_volumetricLightingDistance", 100f, "", CVar.Flags.None, 50f, 200f);
            autoExposureMinLuminance = new CVarFloat("r_autoExposureMinLuminance", 0.01f, "AutoExposure MinLuminance", CVar.Flags.None, 0f, 10f, 0.01f);
            autoExposureLuminanceScale = new CVarFloat("r_autoExposureLuminanceScale", 1f, "AutoExposure LuminanceScale", CVar.Flags.None, 0f, 10f, 0.01f);
            autoExposureLuminanceBlendRatio = new CVarFloat("r_autoExposureLuminanceBlendRatio", 1f, "AutoExposure LuminanceBlendRatio", CVar.Flags.None, 0f, 1f, 0.01f);
            autoExposureMin = new CVarFloat("r_autoExposureMin", 0.5f, "AutoExposure MinValue", CVar.Flags.None, 0f, 1f, 0.01f);
            autoExposureMax = new CVarFloat("r_autoExposureMax", 0.75f, "AutoExposure MaxValue", CVar.Flags.None, 0f, 10f, 0.01f);
            taaJitterMode = new CVarInteger("r_taaJitterMode", 1, "TAA JitterMode: 0 - nonjitter 1 - Uniform2x 2 - Hattlon", CVar.Flags.None, 0, 5);
            useSkylighting = new CVarBool("r_useSkylighting", false, "", CVar.Flags.Dev);
            useLocalLightScattering = new CVarBool("r_useLocalLighScattering", true, "", CVar.Flags.None);
            ssrDitherSampleOffset = new CVarInteger("r_ssrDitherSampleOffset", 1, "", CVar.Flags.Dev, 0, 100);
            ssrSmoothnessThreshold = new CVarFloat("r_smoothnessThreshold", 0.1f, "", CVar.Flags.Dev, 0f, 1f);
            ssrScale = new CVarFloat("r_ssrScale", 1f, "", CVar.Flags.Dev, 0f, 100f);
            useSSR = new CVarBool("r_useSSR", true, "", CVar.Flags.Dev);
        }
    }

    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    public struct ClusterData {
        public UInt32 offset;
        public UInt32 count;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    public struct LightShaderParameter {
        /*
        float3 positionWS;
        float range;
        uint colorPacked;
        uint flags;
        float specularMultiplier;
        float lightScattering;
        uint2 falloffScaleBias;
        uint2 projectorScaleBias;
        float4x4 worldToLightMatrix;
        float3 boxMin;
        uint flags2;
        float3 boxMax;
        float padding;
        */
        public Vector3 positionWS;
        public float range;
        public uint colorPacked;
        public uint flags;
        public float specularMultiplier;
	    public float lightScattering;
        public uint falloffScaleBias0;
        public uint falloffScaleBias1;
        public uint projectorScaleBias0;
        public uint projectorScaleBias1;
        public Matrix worldToLightMatrix;
        public Vector3 boxMin;
        public uint flags2;
        public Vector3 boxMax;
        public float padding;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    public struct DecalShaderParameter {
        public uint flags;
        public uint flags2;
        public uint baseColorPacked;
        public uint emissivePacked;
        public uint opacityPacked;
        public uint specualrOverridePacked;
        public uint normalScaleBias0;
        public uint normalScaleBias1;
        public uint albedoScaleBias0;
        public uint albedoScaleBias1;
        public uint specularScaleBias0;
        public uint specularScaleBias1;
        public Vector4 worldToDecalMatrix0;
        public Vector4 worldToDecalMatrix1;
        public Vector4 worldToDecalMatrix2;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    public struct ShadowShaderParameter {
        public Matrix shadowMatrix;
        public Vector4 atlasScaleBias;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    public struct ScatteringVolumeShaderParameter {
        public Vector4 worldToVolumeMatrix0;    
        public Vector4 worldToVolumeMatrix1;    
        public Vector4 worldToVolumeMatrix2;    
        public uint parms0;
        public uint parms1;
        public uint colorPacked;
        public uint padding;
    }

    [StructLayout(LayoutKind.Sequential, Pack = 16)]
    public struct ShadowViewConstants {
        public Matrix shadowMatrix;
        public Vector4 biasParms;
    }

    public class ViewEntity {
        public int id;
        public VertexFactory vertexFactory;
        public IndexBuffer indexBuffer;
    }

    public class DrawSurface {
        public ViewEntity space;
        public SubMesh drawInfo;
        public MaterialRenderProxy material;
        public MaterialRenderProxy prezMaterial;
        public MaterialRenderProxy shadowMaterial;
    }

    internal class DrawSurfaceCollector {
        public RenderObjectList<DrawSurface> opaqueSurfaces;
        public RenderObjectList<DrawSurface> alphaTestSurfaces;
        public RenderObjectList<DrawSurface> transparentSurfaces;

        public DrawSurfaceCollector(RenderObjectPool<DrawSurface> pool) {
            opaqueSurfaces = new RenderObjectList<DrawSurface>(pool, 1024);
            alphaTestSurfaces = new RenderObjectList<DrawSurface>(pool, 128);
            transparentSurfaces = new RenderObjectList<DrawSurface>(pool, 128);
        }

        public DrawSurfaceCollector(RenderObjectPool<DrawSurface> pool, int maxOpaqueSurfaces, int maxAlphaTestSurfaces) {
            opaqueSurfaces = new RenderObjectList<DrawSurface>(pool, maxOpaqueSurfaces);
            alphaTestSurfaces = new RenderObjectList<DrawSurface>(pool, maxAlphaTestSurfaces);
        }

        class SurfaceComparer : IComparer<DrawSurface> {
            Matrix viewProjectionMatrix;

            public SurfaceComparer(ref Matrix viewProjectionMatrix) {
                this.viewProjectionMatrix = viewProjectionMatrix;
            }

            public int Compare(DrawSurface a, DrawSurface b) {
                var origin1 = Vector3.TransformCoordinate(a.drawInfo.boundingBox.Center, viewProjectionMatrix);
                var origin2 = Vector3.TransformCoordinate(b.drawInfo.boundingBox.Center, viewProjectionMatrix);
                if(origin1.Z < origin2.Z) {
                    return -1;
                } else if(origin1.Z > origin2.Z) {
                    return 1;
                }
                return 0;
            }
        }

        public void SortOpaqueSurfaces(ref Matrix viewProjectionMatrix) {
            Array.Sort(opaqueSurfaces.Elements, opaqueSurfaces.Offset, opaqueSurfaces.Count, new SurfaceComparer(ref viewProjectionMatrix));
        }

        public void SortTransparentSurfaces() {
        }

        public DrawSurface Alloc(Material material) {
            if(material.renderQueue == RenderQueue.AlphaTest) {
                return alphaTestSurfaces.Alloc();
            } else if(material.renderQueue == RenderQueue.Transparency) {
                return transparentSurfaces.Alloc();
            } else {
                return opaqueSurfaces.Alloc();
            }
        }

        public DrawSurface Alloc(Material material, ref int numOpaqueSurfaces, ref int numAlphaTestSurfaces, ref int numTransparentSurfaces) {
            if(material.renderQueue == RenderQueue.AlphaTest) {
                numAlphaTestSurfaces++;
                return alphaTestSurfaces.Alloc();
            } else if(material.renderQueue == RenderQueue.Transparency) {
                numTransparentSurfaces++;
                return transparentSurfaces.Alloc();
            } else {
                numOpaqueSurfaces++;
                return opaqueSurfaces.Alloc();
            }
        }
    }

    internal struct ShadowViewDef {
        public int x;
        public int y;
        public int width;
        public int height;
        public ShadowViewConstants viewConstants;
        public int opaqueSurfaceOffset;
        public int numOpaqueSurfaces;
        public int alphaTestSurfaceOffset;
        public int numAlphaTestSurfaces;

        public Vector4 AtlasScaleBias() {
            return new Vector4(
                width * ShadowAtlasPass.AtlasResolution.Z, 
                height * ShadowAtlasPass.AtlasResolution.W,
                x * ShadowAtlasPass.AtlasResolution.Z,
                y * ShadowAtlasPass.AtlasResolution.W);
        }
    }

    public struct RenderView {
        public float time;
        public float nearClipPlane;
        public float farClipPlane;
        public Vector3 origin;
        public Vector3 right;
        public Vector3 forward;
        public Vector3 up;
    }

    public struct ParticleStageDrawSurface {
        public int id;
        public int vertexOffset;
        public int indexOffset;
        public int numVertices;
        public int numIndices;
    }

    internal class ViewDef {
        public BoundingFrustum frustum;

        public int x;
        public int y;
        public int width;
        public int height;
        public float nearClipPlane;
        public float farClipPlane;
        public Vector3 viewOrigin;
        public Vector3 viewRight;
        public Vector3 viewForward;
        public Vector3 viewUp;
        public Vector3 frustumVectorLT;
        public Vector3 frustumVectorRT;
        public Vector3 frustumVectorLB;
        public Vector3 frustumVectorRB;
        public Vector4 clusteredLightingSize;
        public Vector4 clusteredLightingParms;

        public Matrix viewMatrix;
        public Matrix projectionMatrix;
        public Matrix viewProjectionMatrix;

        public int clusterListSizeInBytes;
        public int clusterItemListSizeInBytes;
        public IntPtr clusterList; // ClusterData[]
        public IntPtr clusterItemList; // UInt32[]

        public int lightShaderParmsSizeInBytes;
        public IntPtr lightShaderParms; // LightShaderParameter[]

        public int decalShaderParmsSizeInBytes;
        public IntPtr decalShaderParms; // DecalShaderParameter[]
        
        public int numShadowViews;
        public IntPtr shadowViewDefs; // ShadowViewDef[]
        public int shadowShaderParmsSizeInBytes;
        public IntPtr shadowShaderParms; // ShadowShaderParameter[]

        public int numScatteringVolumes;
        public int scatteringVolumeShaderParmsSizeInBytes;
        public IntPtr scatteringVolumeShaderParms; // ScatteringVolumeShaderParameter[]

        public int numParticleStageDrawSurfaces;
        public IntPtr particleStageDrawSurfaces; // ParticleStageDrawSurface[]

        public DrawSurfaceCollector staticSurfaces;
        public DrawSurfaceCollector dynamicSurfaces;
        public DrawSurfaceCollector shadowReceiverSurfaces;

        public ViewDef(RenderObjectPool<DrawSurface> pool) {
            staticSurfaces = new DrawSurfaceCollector(pool);
            dynamicSurfaces = new DrawSurfaceCollector(pool);
            shadowReceiverSurfaces = new DrawSurfaceCollector(pool, 1024, 256);
        }
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct FXConstants {
        public Vector4 globalViewOrigin;
        public Vector4 frustumVectorLT;
        public Vector4 frustumVectorRT;
        public Vector4 frustumVectorLB;
        public Vector4 frustumVectorRB;   
        public Vector4 screenParms;
        public Vector4 depthBufferParms;
        public Vector4 sunForward;
        public Vector4 clusteredLightingSize;
        public Vector4 clusteredLightingParms;
        public Vector4 shadowsAtlasResolution;
        public Vector4 timeParms;
        public Matrix viewMatrix;
        public Matrix inverseViewMatrix;
        public Matrix inverseProjectionMatrix;
        public Matrix prevViewProjectionMatrixNoJitter;
        public Matrix viewProjectionMatrix;
        public Matrix prevViewProjectionMatrix;
        public Matrix inverseViewProjectionMatrix;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ModelPassViewConstants {
        public Vector4 viewOrigin;
        public Vector4 frustumVectorLT;
        public Vector4 frustumVectorRT;
        public Vector4 frustumVectorLB;
        public Vector4 frustumVectorRB;   
        public Vector4 screenParms;
        public Vector4 depthBufferParms;
        public Vector4 sunForward;
        public Vector4 clusteredLightingSize;
        public Vector4 clusteredLightingParms;
        public Vector4 shadowsAtlasResolution;
        public Vector4 timeParms;
	    public Vector4 volumetricLightingResolution;
	    public Vector4 volumetricLightingZNearFar;
        public Vector4 volumetricLightingParms;
        public Vector4 sunColor;
        public Vector4 viewProjectionMatrix0;
        public Vector4 viewProjectionMatrix1;
        public Vector4 viewProjectionMatrix2;
        public Vector4 viewProjectionMatrix3;
        public static readonly uint UniformBlockSizeInBytes = (uint)MathHelper.Align(Utilities.SizeOf<ModelPassViewConstants>(), (int)GraphicsDevice.gd.UniformBufferMinOffsetAlignment); 
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct ModelPassInstanceConstants {
        public Vector4 modelIndex;
        public Vector4 modelParms;
        public Vector4 lightmapScaleOffset; // StaticModel only
        public Vector4 localToWorldMatrix0;
        public Vector4 localToWorldMatrix1;
        public Vector4 localToWorldMatrix2;
        public Vector4 worldToLocalMatrix0;
        public Vector4 worldToLocalMatrix1;
        public Vector4 worldToLocalMatrix2;
        public Vector4 mvpMatrix0;
        public Vector4 mvpMatrix1;
        public Vector4 mvpMatrix2;
        public Vector4 mvpMatrix3;

        public Vector4 pad5;
        public Vector4 pad6;
        public Vector4 pad7;

        public static readonly uint UniformBlockSizeInBytes = (uint)MathHelper.Align(Utilities.SizeOf<ModelPassInstanceConstants>(), 256);
        public static readonly uint NumConstants = UniformBlockSizeInBytes / (uint)Utilities.SizeOf<Vector4>();
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct Skylighting {
        public Vector4 texelSize;
        public Vector4 zNearFar;
        public Vector4 globalViewOrigin;
        public Vector4 radiusPlanet;
        public Vector4 radiusAtmosphere;
        public Vector4 mieDensityScale;
        public Vector4 rayleighHeight;
        public Vector4 rayleighColor;
        public Vector4 mieColor;
        public Vector4 mieHeight;
        public Vector4 mieAnisotropy;
        public Vector4 sunLatitude;
        public Vector4 sunLongitude;
        public Vector4 scatteringDensityScale;
        public Vector4 sunIntensity;
        public Vector4 decoupleSunColorFromSky;
        public Vector4 sunColor;
        public Vector4 sunForward;
    }

    public class ArrayView<T> {
        public T[] array {get; private set;}
        public int start {get; private set;}
        public int num {get; private set;}

        public ArrayView(T[] inArray, int inStart, int inNum) {
            array = inArray;
            start = inStart;
            num = inNum;
        }

        public T this[int index] {
            get => array[start + index];
            set => array[start + index] = value;
        }

        public void CopyTo(ArrayView<T> to, int n) {
            Array.Copy(array, start, to.array, to.start, n);
        }

        public int Length => num;
    }

    public class RenderObjectPool<T> where T: new() {
        T[] elements;
        int maxElements;
        int usedElements;

        public RenderObjectPool(int maxObjects) {
            maxElements = Math.Max(maxObjects, 8);
            elements = new T[maxElements];
            for(int i = 0; i < elements.Length; i++) {
                elements[i] = new T();
            }
        }

        public ArrayView<T> Alloc(int num) {
            if(usedElements + num >= maxElements) {
                throw new OutOfMemoryException();
            }
            ArrayView<T> slice = new ArrayView<T>(elements, usedElements, num);
            usedElements += num;
            return slice;
        }

        public void Reset() {
            usedElements = 0;
        }
    }

    public class RenderObjectList<T> where T: new() {
        readonly RenderObjectPool<T> pool;
        ArrayView<T> elements;
        int numElements;

        public T this[int index] {
            get {return elements[index];}
            set {elements[index] = value;}
        }
        public T[] Elements {get {return elements.array;}}
        public int Offset {get {return elements.start;}}
        public int Count {get {return numElements;}}

        public RenderObjectList(RenderObjectPool<T> myPool, int n) {
            pool = myPool;
            n = Math.Max(n, 32);
            elements = pool.Alloc(n);
        }

        public T Alloc() {
            if(numElements + 1 >= elements.Length) {
                ArrayView<T> newList = pool.Alloc((numElements + 1) * 2);
                elements.CopyTo(newList, numElements);
                elements = newList;
            }
            int index = numElements;
            numElements++;
            return elements[index];
        }
    }
}