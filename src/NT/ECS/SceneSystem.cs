using System;
using System.Collections.Generic;
using System.IO;
using SharpDX;

namespace NT
{
    public enum DecalImageType {
        Albedo,
        Normal,
        Specular,
        Emissive
    }

    public enum SkyboxType {
        Cubemap,
        HDRI
    }

    internal struct FXRenderData {
        public float gameTime;
        public float gameFrameDeltaTime;
        public uint gameFrameCount;
        public float bloomThreshold;
        public bool usePSSM;
        public float volumetricLightingDistance;
        public float autoExposureMinLuminance;
        public float autoExposureLuminanceScale;
        public float autoExposureLuminanceBlendRatio;
        public float autoExposureMin;
        public float autoExposureMax;
        public float autoExposureSpeed;
        public Vector3 globalViewOrigin;
        public Vector3 frustumVectorLT;
        public Vector3 frustumVectorRT;
        public Vector3 frustumVectorLB;
        public Vector3 frustumVectorRB;
        public Vector4 clusteredLightingSize;
        public Vector4 clusteredLightingParms;
        public Vector3 sunForward;
        public Vector3 sunColor;
        public Vector3 rayleighColor;
        public Vector3 mieColor;
        public float sunIntensity;
        public float sunDiskScale;
        public float mieHeight;
        public bool decoupleSunColorFromSky;
        public float viewMinDistance;
        public float viewMaxDistance;
        public float radiusPlanet;
        public float radiusAtmosphere;
        public float mieDensityScale;
        public float rayleighHeight;
        public float mieAnisotropy;
        public float scatteringDensityScale;
        public uint scatteringFlags; // Skylighting (1 << 0) LocalLightScattering (1 << 1)
        public int ssrDitherSampleOffset;
        public float ssrSmoothnessThreshold;
        public float ssrScale;
        public int ssrMode;
        public SkyboxType skyboxType;
        public Image2D skyboxBackground;
        public ImageCubeArray reflectionProbeCubemaps;
        public int reflectionProbeShaderParmsSizeInBytes;
        public IntPtr reflectionProbeShaderParms; // LightShaderParameter[]
    }

    internal struct SceneRenderData {
        public ViewDef[] viewDefs;
        public FXRenderData fxRenderData;
        public DebugToolRenderData debugToolRenderData;
        public int numModelInstances;
        public IntPtr modelInstanceConstantBuffer;
        public int numInstanceMatrices;
        public IntPtr instanceMatricesBuffer;
        public int numParticleVertices;
        public int numParticleIndices;
        public IntPtr particleVertexBuffer;
        public IntPtr particleIndexBuffer;
    }

    internal sealed class Scene : IDisposable {
        public enum LayerMask {
            Default = 1 << 0
        }

        public readonly ComponentManager<NameComponent> names;
        public readonly ComponentManager<TransformComponent> transforms;
        public readonly ComponentManager<HierarchyComponent> hierarchy;
        public readonly ComponentManager<MeshComponent> meshes;
        public readonly ComponentManager<CameraComponent> cameras;
        public readonly ComponentManager<ScatteringVolumeComponent> scatteringVolumes;
        public readonly ComponentManager<BoundsComponent> scatteringVolumeBounds;
        public readonly ComponentManager<LightComponent> lights;
        public readonly ComponentManager<BoundsComponent> lightBounds;
        public readonly ComponentManager<ParticleSystemComponent> particleSystems;
        public readonly ComponentManager<LightComponent> directionalLights;
        public readonly ComponentManager<ReflectionProbeComponent> reflectionProbes;
        public readonly ComponentManager<BoundsComponent> reflectionProbeBounds;
        public readonly ImageCubeArray reflectionProbeCubemaps;
        public readonly ComponentManager<DecalComponent> decals;
        public readonly ComponentManager<BoundsComponent> decalBounds;
        public readonly ComponentManager<SkeletonComponent> skeletons;

        public Entity mainCameraEntity;
        public CameraComponent mainCamera;
        public TransformComponent mainCameraTransform;
        public Entity sunLightEntity;
        public LightComponent sunLight;
        public TransformComponent sunLightTransform;
    
        public readonly RenderDebug renderDebug;
        public readonly MeshRenderSystem meshRenderSystem;
        readonly RenderObjectPool<DrawSurface> drawSurfacePool;
        Material skyboxMaterial;
        public MaterialRenderProxy skyboxRenderProxy {get; private set;}
        public const string defaultSpotProjectorName = "spot01.bimage";
        public Vector4 defaultSpotProjectorScaleBias {get; private set;}
        public const string defaultDecalNormalName = "blood_boot_smears_01_local";
        public Vector4 defaultDecalNormalScaleBias {get; private set;}
        public Texture2DAtlas lightsAtlas {get; private set;}
        public Texture2DAtlas decalsAtlas {get; private set;}

        public readonly static Scene GlobalScene;
        static Scene() {
            GlobalScene = new Scene();
        }

        public Vector4 FindDecalImage(string filename, DecalImageType type, out string assetName) {
            string basePath = null;
            if(type == DecalImageType.Albedo) {
                basePath = "textures/decals/albedos";
            } else if(type == DecalImageType.Specular) {
                basePath = "textures/decals/speculars";
            } else if(type == DecalImageType.Emissive) {
                basePath = "textures/decals/emissives";
            } else {
                basePath = "textures/decals/normals";
            }
            int dot = filename.LastIndexOf(".bimg");
            assetName = filename;
            if(dot != -1) {
                filename = FileSystem.CreateAssetOSPath(Path.Combine(basePath, assetName));
                dot = filename.LastIndexOf(".bimg");
                assetName = filename.Remove(dot);
            } else {
                filename = FileSystem.CreateAssetOSPath(Path.Combine(basePath, assetName), ".bimg");
            }

            Vector4 scaleBias = Vector4.Zero;
            if(!decalsAtlas.FindTexture(filename, out Vector4 rect)) {
                BinaryImage[] datas = AssetLoader.LoadBinaryImgae(filename);
                decalsAtlas.AddTexture(ref rect, filename, datas[0].width, datas[0].height, datas);
            }
            scaleBias = new Vector4(rect.X * decalsAtlas.texelSize.X, rect.Y * decalsAtlas.texelSize.Y, rect.Z * decalsAtlas.texelSize.X, rect.W * decalsAtlas.texelSize.Y);
            return scaleBias;
        }

        public Vector4 FindLightProjector(string filename, out string projectorName) {
            int dot = filename.LastIndexOf(".bimg");
            projectorName = filename;
            if(dot != -1) {
                filename = FileSystem.CreateAssetOSPath(Path.Combine("textures/lights", projectorName));
                projectorName = filename.Remove(dot);
            } else {
                filename = FileSystem.CreateAssetOSPath(Path.Combine("textures/lights", projectorName), ".bimg");
            }

            Vector4 scaleBias = Vector4.Zero;
            if(!lightsAtlas.FindTexture(filename, out Vector4 rect)) {
                BinaryImage[] datas = AssetLoader.LoadBinaryImgae(filename);
                lightsAtlas.AddTexture(ref rect, filename, datas[0].width, datas[0].height, datas);
            }
            scaleBias = new Vector4((rect.X - 0.5f) * lightsAtlas.texelSize.X, (rect.Y - 0.5f) * lightsAtlas.texelSize.Y, (rect.Z + 0.5f) * lightsAtlas.texelSize.X, (rect.W + 0.5f) * lightsAtlas.texelSize.Y);
            return scaleBias;
        }

        void InitAtlas() {
            lightsAtlas = new Texture2DAtlas(new Image2D(SceneRenderer.Instance.lightsAtlasMap));
            decalsAtlas = new Texture2DAtlas(new Image2D(SceneRenderer.Instance.decalsAtlasMap));
            defaultSpotProjectorScaleBias = FindLightProjector(defaultSpotProjectorName, out _);
            defaultDecalNormalScaleBias = FindDecalImage(defaultDecalNormalName, DecalImageType.Normal, out _);
        }

        public Scene() {
            names = new ComponentManager<NameComponent>();
            transforms = new ComponentManager<TransformComponent>();
            hierarchy = new ComponentManager<HierarchyComponent>();
            meshes = new ComponentManager<MeshComponent>();
            cameras = new ComponentManager<CameraComponent>();
            meshRenderSystem = new MeshRenderSystem();
            scatteringVolumes = new ComponentManager<ScatteringVolumeComponent>();
            scatteringVolumeBounds = new ComponentManager<BoundsComponent>();
            lights = new ComponentManager<LightComponent>();
            lightBounds = new ComponentManager<BoundsComponent>();
            directionalLights = new ComponentManager<LightComponent>();
            reflectionProbes = new ComponentManager<ReflectionProbeComponent>();
            reflectionProbeBounds = new ComponentManager<BoundsComponent>();
            reflectionProbeCubemaps = new ImageCubeArray(128, 128, 16, Veldrid.PixelFormat.R16_G16_B16_A16_Float);
            decals = new ComponentManager<DecalComponent>();
            decalBounds = new ComponentManager<BoundsComponent>();
            particleSystems = new ComponentManager<ParticleSystemComponent>();
            skeletons = new ComponentManager<SkeletonComponent>();
            drawSurfacePool = new RenderObjectPool<DrawSurface>(16384 * 2);
            renderDebug = new RenderDebug();

            mainCameraEntity = CreateCamera("MainCamera");
            mainCamera = cameras.GetComponent(mainCameraEntity);
            mainCameraTransform = transforms.GetComponent(mainCameraEntity);
            mainCamera.SetAspect(1920, 1080);
            mainCamera.SetFOV(60f);
            mainCamera.SetClippingPlanes(0.1f, 500f);
            mainCamera.SetUpdateFrustumVectors();
            mainCamera.SetClusteredLighting();
/*
            sunLightEntity = CreateLight("Sun", LightType.Directional);
            sunLight = directionalLights.GetComponent(sunLightEntity);
            sunLight.SetCastShadow(true);
            sunLightTransform = transforms.GetComponent(sunLightEntity);
            sunLightTransform.SetLocalRotation(300f, 0f, 15f);
*/
        }

        public void Init() {
            InitAtlas();
            //InitHDRILightingData("hdri/head1.thdri");
        }

        public void Dispose() {
        }

        int numProbes;
        int numLightmaps;
        LightShaderParameter[] reflectionProbeShaderParms;
        Image2D[] lightmapDirMaps;
        Image2D[] lightmapColorMaps;

        public void SetGIData(Image2D[] _lightmapDirMaps, Image2D[] _lightmapColorMaps) {
            if(_lightmapDirMaps != null && _lightmapColorMaps != null && _lightmapDirMaps.Length == _lightmapColorMaps.Length) {
                lightmapDirMaps = _lightmapDirMaps;
                lightmapColorMaps = _lightmapColorMaps;
            }
        }

        public void SetSunSource(Entity entity) {
            var component = directionalLights.GetComponent(entity);
            var transform = transforms.GetComponent(entity);
            if(component != null) {
                sunLightEntity = entity;
                sunLight = component;
                sunLightTransform = transform;
            }
        }

        public void SetSkybox(Material skyboxMaterial, string cubemapAssetName) {
            BinaryImageFile cubemapFile = AssetLoader.LoadBinaryImgaeFile(FileSystem.CreateAssetOSPath(cubemapAssetName));
            uint numMips = cubemapFile.numLevels / 6;
            ImageCube cubemap = new ImageCube((int)cubemapFile.width, (int)cubemapFile.height, (Veldrid.PixelFormat)cubemapFile.format, (int)numMips);
            for(int face = 0; face < 6; face++) {
                for(int mip = 0; mip < numMips; mip++) {
                    int index = face * (int)numMips + mip;
                    cubemap.SubImageUpload(face, mip, cubemapFile.images[index].width, cubemapFile.images[index].height, cubemapFile.images[index].bytes);
                }
            }
            skyboxMaterial.SetImage("_Cubemap", cubemap);
            skyboxMaterial.SetImage("_SkyLightingLUT", new ImageWraper(Common.frameGraph.skylightingLUT.Target));
            skyboxMaterial.SetImage("_ScatteringPacked0Tex", new ImageWraper(Common.frameGraph.finalLightScatteringPackedMaps[0].Target));
            skyboxMaterial.SetImage("_ScatteringPacked1Tex", new ImageWraper(Common.frameGraph.finalLightScatteringPackedMaps[1].Target));
            skyboxMaterial.SetSampler("_LinearSampler", GraphicsDevice.gd.LinearSampler);
            skyboxMaterial.SetSampler("_LinearClampSampler", GraphicsDevice.LinearClampSampler);
            skyboxRenderProxy = skyboxMaterial.MakeLightingProxy();
        }

        public void SetEnvironmentProbe(string assetName) {
            Entity entity = Entity.Create();
            NameComponent name = names.Create(entity);
            transforms.Create(entity);
            ReflectionProbeComponent probe = reflectionProbes.Create(entity);
            probe.SetBounds(new Vector3(-1000f), new Vector3(1000f));
            reflectionProbeBounds.Create(entity).boundingBox = probe.localBounds;

            AddReflectionProbeCubemap(entity, assetName); 
        }

        void InitHDRILightingData(string assetName) {
            numProbes = 0;
            if(reflectionProbeShaderParms == null) {
                reflectionProbeShaderParms = new LightShaderParameter[ClusteredLighting.MaxProbesOnScreen];
            }

            Token token = new Token();
            Lexer src = new Lexer(File.ReadAllText(FileSystem.CreateAssetOSPath(assetName)));
            src.ReadToken(ref token);
            src.ExpectTokenString("{");
            string hdriAsset = token.lexme;
            string reflectionProbeAsset = null;
            while(src.ReadToken(ref token)) {
                if(token == "}") {
                    break;
                }
                if(token == "ReflectionProbe") {
                    src.ReadTokenOnLine(ref token);
                    reflectionProbeAsset = token.lexme;
                    continue;
                }
                if(token == "AmbientProbe") {
                    continue;
                }
            }

            BinaryImageFile cubemapFile = AssetLoader.LoadBinaryImgaeFile(FileSystem.CreateAssetOSPath($"textures/{hdriAsset}.bimg"));
            uint numMips = cubemapFile.numLevels / 6;
            ImageCube cubemap = new ImageCube((int)cubemapFile.width, (int)cubemapFile.height, (Veldrid.PixelFormat)cubemapFile.format, (int)numMips);
            for(int face = 0; face < 6; face++) {
                for(int mip = 0; mip < numMips; mip++) {
                    int index = face * (int)numMips + mip;
                    cubemap.SubImageUpload(face, mip, cubemapFile.images[index].width, cubemapFile.images[index].height, cubemapFile.images[index].bytes);
                }
            }
            skyboxMaterial = new Material(Common.declManager.FindShader("builtin/skybox"));
            skyboxMaterial.SetImage("_Cubemap", cubemap);
            skyboxMaterial.SetImage("_SkyLightingLUT", new ImageWraper(Common.frameGraph.skylightingLUT.Target));
            skyboxMaterial.SetImage("_ScatteringPacked0Tex", new ImageWraper(Common.frameGraph.finalLightScatteringPackedMaps[0].Target));
            skyboxMaterial.SetImage("_ScatteringPacked1Tex", new ImageWraper(Common.frameGraph.finalLightScatteringPackedMaps[1].Target));
            skyboxMaterial.SetSampler("_LinearSampler", GraphicsDevice.gd.LinearSampler);
            skyboxMaterial.SetSampler("_LinearClampSampler", GraphicsDevice.LinearClampSampler);
            skyboxRenderProxy = skyboxMaterial.MakeLightingProxy();

            Entity entity = Entity.Create();
            NameComponent name = names.Create(entity);
            transforms.Create(entity);
            ReflectionProbeComponent probe = reflectionProbes.Create(entity);
            probe.SetBounds(new Vector3(-1000f), new Vector3(1000f));
            reflectionProbeBounds.Create(entity).boundingBox = probe.localBounds;

            AddReflectionProbeCubemap(entity, FileSystem.CreateAssetOSPath($"textures/{reflectionProbeAsset}.bimg"));

/*
            MeshComponent mesh = meshes.Create(entity);
            Material[] materials = new Material[1];
            materials[0] = new Material(Common.declManager.FindShader("builtin/editor/envprobe"));
            materials[0].SetMainImage(reflectionProbeCubemaps);
            materials[0].SetFloat4("_ProbeID", new Vector4(numProbes, 0f, 0f, 0f));
            if(!meshRenderSystem.FindRuntimeMesh("Editor_Sphere", out var runtimeMesh)) {
                Mesh meshData = AssetLoader.LoadTextModel(FileSystem.CreateAssetOSPath("models/Sphere.tmdl"));
                mesh.SetRenderData(meshData, materials, typeof(DrawVertexPackedVertexFactory), "Editor_Sphere");
            } else {
                mesh.SetRenderData(runtimeMesh, materials);
            }    
*/
/*
            LightShaderParameter parms = new LightShaderParameter();
            parms.flags = MathHelper.PackR8G8B8A8(new Vector4(0f, 0f, 0f, numProbes));
            parms.positionWS = Vector3.Zero;
            parms.specularMultiplier = probe.specularMultiplier;
            parms.worldToLightMatrix = probe.boxProjectionMatrix;
            parms.boxMin = probe.localBounds.Minimum;
            parms.boxMax = probe.localBounds.Maximum;
            reflectionProbeShaderParms[numProbes] = parms;  
*/
            //numProbes++;    
                
        }

        public void AddReflectionProbeCubemap(Entity entity, string assetName) {
            BinaryImageFile cubemapFile = AssetLoader.LoadBinaryImgaeFile(FileSystem.CreateAssetOSPath(assetName));
            uint numMips = cubemapFile.numLevels / 6;
            for(int face = 0; face < 6; face++) {
                for(int mip = 0; mip < numMips; mip++) {
                    int index = face * (int)numMips + mip;
                    reflectionProbeCubemaps.SubImageUpload(numProbes, face, mip, cubemapFile.images[index].width, cubemapFile.images[index].height, cubemapFile.images[index].bytes);
                }
            } 

            ReflectionProbeComponent reflectionProbeComponent = reflectionProbes.GetComponent(entity); 

            MeshComponent mesh = meshes.Create(entity);
            Material[] materials = new Material[1];
            materials[0] = new Material(Common.declManager.FindShader("builtin/editor/envprobe"));
            materials[0].SetMainImage(reflectionProbeCubemaps);
            materials[0].SetFloat4("_ProbeID", new Vector4(numProbes, 0f, 0f, 0f));
            if(!meshRenderSystem.FindRuntimeMesh("Editor_Sphere", out var runtimeMesh)) {
                string meshAssetName = FileSystem.CreateAssetOSPath($"models/Sphere.tmdl");
                Mesh meshData = AssetLoader.LoadTextModel(meshAssetName);
                mesh.SetRenderData(meshData, materials, typeof(DrawVertexPackedVertexFactory), "Editor_Sphere");
            } else {
                mesh.SetRenderData(runtimeMesh, materials);
            }
            mesh.SetCastShadow(false);
            mesh.SetEnabled(false);

            reflectionProbeComponent.SetBaked(numProbes); 
            numProbes++; 
            needUpdateRenderDatas = true;    
        }

/*
        void ParseReflectionProbes(string mapFilename, Lexer lex) {
            numProbes = 0;
            reflectionProbes.Clear();
            reflectionProbeBounds.Clear();
            if(reflectionProbeShaderParms == null) {
                reflectionProbeShaderParms = new LightShaderParameter[ClusteredLighting.MaxProbesOnScreen];
            }

            Token token = new Token();
            lex.ExpectTokenString("{");       
            while(true) {
                if(!lex.ReadToken(ref token)) {
                    throw new InvalidDataException();
                }
                if(token == "}") {
                    break;
                }

                Vector3 origin = Vector3.Zero;
                Vector3 boxMin = Vector3.Zero;
                Vector3 boxMax = Vector3.Zero;
                string probeName = token.lexme;
                lex.ExpectTokenString("{");
                lex.ExpectTokenString("Texture");
                lex.ReadTokenOnLine(ref token);
                string assetName = Path.Combine(FileSystem.assetBasePath, $"textures/{token.lexme}");
                lex.ExpectTokenString("Origin");    
                lex.ParseCSharpVector(ref origin);
                lex.ExpectTokenString("BoxMin");    
                lex.ParseCSharpVector(ref boxMin);
                lex.ExpectTokenString("BoxMax");    
                lex.ParseCSharpVector(ref boxMax);
                lex.ExpectTokenString("}");  
             
                Entity probe = Entity.Create();
                NameComponent name = names.Create(probe);
                ReflectionProbeComponent reflectionProbeComponent = new ReflectionProbeComponent(probeName);
                reflectionProbeComponent.SetOrigin(origin);
                reflectionProbeComponent.SetBounds(boxMin, boxMax);
                reflectionProbeComponent.Update();
                reflectionProbes.Add(reflectionProbeComponent);
                BoundsComponent probeBounds = new BoundsComponent();
                probeBounds.boundingBox = reflectionProbeComponent.bounds;
                reflectionProbeBounds.Add(probeBounds);

                BinaryImageFile cubemapFile = AssetLoader.LoadBinaryImgaeFile(assetName);
                uint numMips = cubemapFile.numLevels / 6;
                for(int face = 0; face < 6; face++) {
                    for(int mip = 0; mip < numMips; mip++) {
                        int index = face * (int)numMips + mip;
                        reflectionProbeCubemaps.SubImageUpload(numProbes, face, mip, cubemapFile.images[index].width, cubemapFile.images[index].height, cubemapFile.images[index].bytes);
                    }
                }

///*
                TransformComponent transform = transforms.Create(probe);
                MeshComponent mesh = meshes.Create(probe);
                name.name = probeName;
                transform.SetLocalPosition(origin);
                transform.UpdateTransform();
                Material[] materials = new Material[1];
                materials[0] = new Material(Common.declManager.FindShader("builtin/editor/envprobe"));
                materials[0].SetMainImage(reflectionProbeCubemaps);
                materials[0].SetFloat4("_ProbeID", new Vector4(numProbes, 0f, 0f, 0f));
                if(!meshRenderSystem.FindRuntimeMesh("Editor_Sphere", out var runtimeMesh)) {
                    string meshAssetName = Path.Combine(Path.GetPathRoot(mapFilename), $"unityExported/models/Sphere.tmodel");
                    Mesh meshData = AssetLoader.LoadTextModel(meshAssetName);
                    mesh.SetRenderData(meshData, materials, typeof(DrawVertexPackedVertexFactory), "Editor_Sphere");
                } else {
                    mesh.SetRenderData(runtimeMesh, materials);
                }
                mesh.UpdateBounds(transform);  
//
 
                LightShaderParameter parms = new LightShaderParameter();
                parms.flags = MathHelper.PackR8G8B8A8(new Vector4(0f, 0f, 0f, numProbes));
                parms.positionWS = reflectionProbeComponent.position;
                parms.specularMultiplier = reflectionProbeComponent.specularMultiplier;
                parms.worldToLightMatrix = reflectionProbeComponent.boxProjectionMatrix;
                parms.boxMin = boxMin;
                parms.boxMax = boxMax;
                reflectionProbeShaderParms[numProbes] = parms;                
                numProbes++;      
            }    
        }
*/
        void ParseLightmaps(string mapFilename, Lexer lex) {
            Token token = new Token();
            lex.ExpectTokenString("{");     

            int i = 0;  
            while(true) {
                if(!lex.ReadToken(ref token)) {
                    throw new InvalidDataException();
                }
                if(token == "}") {
                    break;
                }

                lex.ExpectTokenString("LightmapDir");
                lex.ReadTokenOnLine(ref token);
                lightmapDirMaps[i] = ImageManager.Image2DFromFile(Path.Combine(FileSystem.assetBasePath, $"{token.lexme}"));
                lex.ExpectTokenString("LightmapColor");
                lex.ReadTokenOnLine(ref token);
                lightmapColorMaps[i] = ImageManager.Image2DFromFile(Path.Combine(FileSystem.assetBasePath, $"{token.lexme}"));
                lex.ExpectTokenString("}");
                i++;
            }    
        }

        void ParseRealtimeLights(string mapFilename, Lexer lex) {
            Token token = new Token();
            lex.ExpectTokenString("{");       
            while(true) {
                if(!lex.ReadToken(ref token)) {
                    throw new InvalidDataException();
                }
                if(token == "}") {
                    break;
                }

                string lightName = token.lexme;
                Vector3 position = Vector3.Zero;
                Vector3 right = MathHelper.Vec3Right;
                Vector3 forward = MathHelper.Vec3Forward;
                Vector3 up = MathHelper.Vec3Up;
                LightType lightType = LightType.Point;
                Color color = Color.White;
                float intensity = 1f;
                float range = 1f;
                float spotAngle = 30f;
                bool enabled = true;
                bool castShadow = false;
                lex.ExpectTokenString("{");
                lex.ExpectTokenString("Enabled");
                lex.ReadTokenOnLine(ref token);
                if(token == "False") {
                    enabled = false;
                }
                lex.ExpectTokenString("Position");
                lex.ParseCSharpVector(ref position);
                lex.ExpectTokenString("Axis");
                lex.ParseCSharpVector(ref right); 
                lex.ParseCSharpVector(ref forward); 
                lex.ParseCSharpVector(ref up);
                lex.ExpectTokenString("Type");
                lex.ReadTokenOnLine(ref token);
                lightType = (LightType)Enum.Parse(typeof(LightType), token.lexme, true);
                lex.ExpectTokenString("Color");
                lex.ParseCSharpColor(ref color);
                lex.ExpectTokenString("Intensity");
                intensity = lex.ParseFloat();
                lex.ExpectTokenString("Range");
                range = lex.ParseFloat();
                lex.ExpectTokenString("SpotAngle");
                spotAngle = lex.ParseFloat();
                lex.ExpectTokenString("CastShadow");
                lex.ReadTokenOnLine(ref token);
                if(token != "False") {
                    castShadow = true;
                }
                lex.ExpectTokenString("ShadowNearPlane");
                float shadowNearPlane = lex.ParseFloat();
                
                Entity light = Entity.Create();
                names.Create(light).name = lightName;
                LightComponent lightComponent = lightType == LightType.Directional ? directionalLights.Create(light) : lights.Create(light);
                lightComponent.SetType(lightType);
                lightComponent.SetColor(color.ToVector3(), intensity);
                lightComponent.SetRange(range);
                lightComponent.SetSpotAngle(spotAngle);
                lightComponent.SetEnabled(enabled);
                lightComponent.SetCastShadow(castShadow);
                lightComponent.SetShadowNearClip(shadowNearPlane);
                lightComponent.name = lightName;
                TransformComponent transform = transforms.Create(light);
                transform.SetLocalPosition(position);
                transform.SetAxis(right, forward, up);
                switch(lightType) {
                    case LightType.Sphere:
                    case LightType.Point:
                        lightBounds.Create(light).InitFromHalWidth(Vector3.Zero, new Vector3(lightComponent.range, lightComponent.range, lightComponent.range));
                        break;
                    case LightType.Spot: {
                        lightBounds.Create(light).InitFromHalWidth(Vector3.Zero, new Vector3(lightComponent.range, lightComponent.range, lightComponent.range));
                        break;
                    }
                }

                lex.ExpectTokenString("}");
            }   
        }

        void ParseRenderers(string mapFilename, Lexer lex) {
            Token token = new Token();
            lex.ExpectTokenString("{");
            while(true) {
                if(!lex.ReadToken(ref token)) {
                    throw new InvalidDataException();
                }
                if(token == "}") {
                    break;
                }
                string rendererName = token.lexme;
                lex.ExpectTokenString("{");
                lex.ExpectTokenString("IsStatic");
                lex.ReadTokenOnLine(ref token);
                lex.ExpectTokenString("Enabled");
                lex.ReadTokenOnLine(ref token);
                lex.ExpectTokenString("Position");
                Vector3 position = Vector3.Zero;
                Vector3 right = MathHelper.Vec3Right;
                Vector3 forward = MathHelper.Vec3Forward;
                Vector3 up = MathHelper.Vec3Up;
                Vector3 scale = Vector3.One;
                lex.ParseCSharpVector(ref position);
                lex.ExpectTokenString("Axis");
                lex.ParseCSharpVector(ref right); lex.ParseCSharpVector(ref forward); lex.ParseCSharpVector(ref up);
                lex.ExpectTokenString("Scale");
                lex.ParseCSharpVector(ref scale);
                lex.ExpectTokenString("Model");
                lex.ReadTokenOnLine(ref token);
                string model = token.lexme;
                lex.ExpectTokenString("Materials");
                lex.ExpectTokenString("{");
                int numMateirals = lex.ParseInteger();
                Material[] materials = new Material[numMateirals];
                int i = 0;
                while(true) {
                    if(!lex.ReadToken(ref token)) {
                        throw new InvalidDataException();
                    }
                    if(token == "}") {
                        break;
                    }
                    materials[i] = Common.declManager.FindMaterial(token.lexme);
                    i++;
                }
                lex.ExpectTokenString("LightmapIndex");
                int lightmapIndex = lex.ParseInteger();
                lex.ExpectTokenString("LightmapScaleOffset");
                Vector4 lightmapScaleOffset = Vector4.Zero;
                lex.ParseCSharpVector(ref lightmapScaleOffset);

                Entity renderer = Entity.Create();
                NameComponent name = names.Create(renderer);
                TransformComponent transform = transforms.Create(renderer);
                MeshComponent mesh = meshes.Create(renderer);
                name.name = rendererName;
                transform.SetLocalPosition(position);
                transform.SetAxis(right, forward, up);
                transform.SetLocalScale(scale);
                transform.flags |= TransformComponent.Flags.CacheInverted;
                //transform.UpdateTransform();
                string assetName = Path.Combine(FileSystem.assetBasePath, $"models/{model}.tmodel");
                if(!meshRenderSystem.FindRuntimeMesh(assetName, out var runtimeMesh)) {
                    Mesh meshData = AssetLoader.LoadTextModel(assetName);
                    mesh.SetRenderData(meshData, materials, lightmapScaleOffset != Vector4.Zero ? typeof(DrawVertexStaticPackedVertexFactory) : typeof(DrawVertexPackedVertexFactory), assetName);
                } else {
                    mesh.SetRenderData(runtimeMesh, materials);
                }
                //mesh.UpdateBounds(transform);
                mesh.UpdateLightmapData(lightmapIndex, lightmapScaleOffset);
                lex.ExpectTokenString("}");
            }
        }

        bool needUpdateRenderDatas;

        public void InitFromMap(string filename) {
            if(!File.Exists(filename)) {
                Console.WriteLine($"map '{filename}' not found.");
                return;
            }

            Token token = new Token();
            Lexer lex = new Lexer(File.ReadAllText(filename), Lexer.Flags.NoStringConcat);

            lex.ExpectTokenString("map");
            lex.ReadTokenOnLine(ref token);
            lex.ExpectTokenString("{");
            lex.ExpectTokenString("NumRenderers");
            lex.ParseInteger();
            lex.ExpectTokenString("NumRealtimeLights");
            lex.ParseInteger();
            lex.ExpectTokenString("NumReflectionProbes");
            lex.ParseInteger();
            lex.ExpectTokenString("NumLightmaps");
            numLightmaps = lex.ParseInteger();
            lex.ExpectTokenString("NumMaterials");
            lex.ParseInteger();
            lex.ExpectTokenString("NumMeshes");
            lex.ParseInteger();
            lex.ExpectTokenString("NumTextures");
            lex.ParseInteger();

            if(numLightmaps > 0) {
                lightmapDirMaps = new Image2D[numLightmaps];
                lightmapColorMaps = new Image2D[numLightmaps];
            }

            while(true) {
                if(!lex.ReadToken(ref token)) {
                    throw new InvalidDataException();
                }
                if(token == "}") {
                    break;
                }

                if(token == "Renderers") {
                    ParseRenderers(filename, lex);
                    continue;
                }

                if(token == "RealtimeLights") {
                    ParseRealtimeLights(filename, lex);
                    continue;
                }

                if(token == "ReflectionProbes") {
                    //ParseReflectionProbes(filename, lex);
                    continue;
                }

                if(token == "Lightmaps") {
                    ParseLightmaps(filename, lex);
                    continue;
                }
            }

            if(numLightmaps > 0) {
                for(int i = 0; i < meshes.Num(); i++) {
                    int lightmapIndex = meshes[i].lightmapIndex;
                    if(lightmapIndex >= 0 && lightmapIndex < numLightmaps) {
                        //meshes[i].UpdateLightmapData(lightmapDirMaps[lightmapIndex], lightmapColorMaps[lightmapIndex]);
                    }
                }
            }

            if(directionalLights.Num() > 0) {
                sunLightEntity = directionalLights.GetEntity(0);
                sunLight = directionalLights[0];
                sunLightTransform = transforms.GetComponent(sunLightEntity);
            }

            needUpdateRenderDatas = true;
        }

        public void Remove(Entity entity) {
            Detach(entity);
            names.Remove(entity);
            transforms.Remove(entity);
            meshes.Remove(entity);
            skeletons.Remove(entity);
            cameras.Remove(entity);
            lights.Remove(entity);
            lightBounds.Remove(entity);
            decals.Remove(entity);
            decalBounds.Remove(entity);
            scatteringVolumes.Remove(entity);
            scatteringVolumeBounds.Remove(entity);
            reflectionProbes.Remove(entity);
            reflectionProbeBounds.Remove(entity);
            particleSystems.Remove(entity);
        }

        public Entity CreateCamera(string name) {
            Entity entity = Entity.Create();
            names.Create(entity).name = name;
            var transform = transforms.Create(entity);
            transform.flags |= TransformComponent.Flags.CacheGlobal;
            CameraComponent camera = cameras.Create(entity);
            ClusteredLighting clusteredLighting = new ClusteredLighting(camera);
            return entity;
        }

        public Entity CreateReflectionProbe(string name) {
            Entity entity = Entity.Create();
            names.Create(entity).name = name;
            var transform = transforms.Create(entity);
            transform.flags |= TransformComponent.Flags.CacheGlobal;
            var probe = reflectionProbes.Create(entity);
            reflectionProbeBounds.Create(entity).boundingBox = probe.localBounds;
            return entity;            
        }

        public Entity CreateParticleSystem(string declName) {
            ParticleSystem system = Common.declManager.FindByType(declName, DeclType.ParticleSystem) as ParticleSystem;
            if(system == null) {
                return Entity.Invalid();
            }

            Entity entity = Entity.Create();
            names.Create(entity).name = declName;
            TransformComponent transform = transforms.Create(entity);
            transform.flags |= TransformComponent.Flags.CacheGlobal;
            ParticleSystemComponent particleSystemComponent = particleSystems.Create(entity);
            particleSystemComponent.system = system;
            return entity;               
        }

        public void Detach(Entity entity) {
            HierarchyComponent parent = hierarchy.GetComponent(entity);
            if(parent != null) {
                TransformComponent transform = transforms.GetComponent(entity);
                if(transform != null) {
                    transform.ApplyTransform();
                }
                hierarchy.RemoveKeepSorted(entity);
            }
        }

        public void DetachChildren(Entity parent) {
            for(int i = 0; i < hierarchy.Num();) {
                if(hierarchy[i].parent == parent) {
                    Detach(hierarchy.GetEntity(i));
                } else {
                    ++i;
                }
            }
        }

        public void Attach(Entity entity, Entity parent) {
            if(entity.id != parent.id) {
                if(hierarchy.Contains(entity)) {
                    Detach(entity);
                }

                hierarchy.Create(entity).parent = parent;

                if(hierarchy.Num() > 1) {
                    for(int i = hierarchy.Num() - 1; i > 0; --i) {
                        Entity parentCandidateEntity = hierarchy.GetEntity(i);
                        HierarchyComponent parentCandidate = hierarchy[i];
                        for(int j = 0; j < i; ++j) {
                            HierarchyComponent childCandidate = hierarchy[i];
                            if(childCandidate.parent == parentCandidateEntity) {
                                hierarchy.MoveItem(i, j);
                                ++i;
                                break;
                            }
                        }
                    }
                }

                HierarchyComponent parentComponent = hierarchy.GetComponent(entity);

                TransformComponent transformParent = transforms.GetComponent(parent);
                if(transformParent == null) {
                    transformParent = transforms.Create(parent);
                }
                parentComponent.localToParentMatrix = Matrix.Invert(transformParent.localToWorldMatrix);

                TransformComponent transformChild = transforms.GetComponent(entity);
                if(transformChild == null) {
                    transformChild = transforms.Create(entity);
                }
                transformChild.flags |= TransformComponent.Flags.Child;
                transformChild.UpdateTransform(transformParent, parentComponent.localToParentMatrix);
            }
        }

        public void CreateSkeleton(Entity entity, Entity[] joints, Matrix[] bindposes, out Entity rootJoint) {
            if(entity.IsValid() && joints != null && bindposes != null && joints.Length == bindposes.Length) {
                SkeletonComponent skeletonComponent = skeletons.Create(entity);
                skeletonComponent.SetJoints(joints, bindposes);
                rootJoint = skeletonComponent.rootJoint;
            } else {
                rootJoint = Entity.Invalid();
            } 
        }

        public Entity CreateMD5Actor(string assetName, Vector3 origin, Quaternion rotation, float scale = 1f) {
            string assetFullpath = Path.Combine(FileSystem.assetBasePath, assetName);
            if(!File.Exists(assetFullpath)) {
                return Entity.Invalid();
            }

            var model = AssetLoader.LoadMD5Model(assetFullpath, scale);   
            Mesh mesh = new Mesh();
            mesh.indexFormat = Veldrid.IndexFormat.UInt16;
            mesh.bindposes = model.bindposes;
            mesh.subMeshes = new SubMesh[model.meshes.Length];
            int vertexOffset = 0;
            int indexOffset = 0;
            int numVertices = 0;
            int numIndices = 0;
            for(int i = 0; i < mesh.subMeshes.Length; i++) {
                mesh.subMeshes[i].vertexOffset = vertexOffset;
                mesh.subMeshes[i].indexOffset = indexOffset;
                mesh.subMeshes[i].numVertices = model.meshes[i].numVertices;
                mesh.subMeshes[i].numIndices = model.meshes[i].numIndices;
                mesh.subMeshes[i].materialIndex = i;
                vertexOffset += model.meshes[i].numVertices;
                indexOffset += model.meshes[i].numIndices;
                numVertices += model.meshes[i].numVertices;
                numIndices += model.meshes[i].numIndices;
                MathHelper.BoundingBoxAddBounds(ref mesh.boundingBox, model.meshes[i].bounds);
            }

            int vbSize = DrawVertexPacked0.SizeInBytes * numVertices;
            int ibSize = sizeof(UInt16) * numIndices;
            IntPtr vb = Utilities.AllocateMemory(vbSize);
            IntPtr vb1 = Utilities.AllocateMemory(vbSize);
            IntPtr ib = Utilities.AllocateMemory(ibSize);
            DataStream vertexBuffer0 = new DataStream(vb, vbSize, false, true);
            DataStream vertexBuffer1 = new DataStream(vb1, vbSize, false, true);
            DataStream indexBuffer = new DataStream(ib, ibSize, false, true);
            for(int i = 0; i < model.meshes.Length; i++) {
                for(int v = 0; v < model.meshes[i].numVertices; v++) {
                    DrawVertexPacked0 packed0 = new DrawVertexPacked0();
                    packed0.position = model.meshes[i].vertices[v].position;
                    packed0.st = model.meshes[i].vertices[v].st;
                    DrawVertexPacked1 packed1 = new DrawVertexPacked1();
                    packed1.normal = model.meshes[i].vertices[v].normal;
                    packed1.tangent = model.meshes[i].vertices[v].tangent;
                    packed1.color = model.meshes[i].vertices[v].color;
                    packed1.color1 = model.meshes[i].vertices[v].color1;
                    vertexBuffer0.Write(packed0);
                    vertexBuffer1.Write(packed1);
                }
                indexBuffer.WriteRange(model.meshes[i].indices);
            }

            vertexBuffer0.Position = 0;
            vertexBuffer1.Position = 0;
            indexBuffer.Position = 0;
            mesh.numVertices = numVertices;
            mesh.numIndices = numIndices;
            mesh.nativeVertexBufferSize = vbSize;
            mesh.nativeIndexBufferSize = ibSize;
            mesh.nativeVerticesPacked0 = vertexBuffer0.DataPointer;
            mesh.nativeVerticesPacked1 = vertexBuffer1.DataPointer;
            mesh.nativeIndices = indexBuffer.DataPointer;

            Skeleton skeleton = new Skeleton();
            skeleton.name = assetName;
            skeleton.joints = new SkeletonJoint[model.joints.Length];
            for(int i = 0; i < skeleton.joints.Length; i++) {
                skeleton.joints[i].name = model.joints[i].name;
                skeleton.joints[i].parent = model.joints[i].parent;
                skeleton.joints[i].localPosition = model.defaultPose[i].t;
                skeleton.joints[i].localRotation = model.defaultPose[i].q;
                skeleton.joints[i].localScale = Vector3.One;
            }

            Entity actor = Entity.Create();
            Entity skeletonEntity = Entity.Invalid();
            names.Create(actor).name = assetName;
            TransformComponent transform = transforms.Create(actor);
            MeshComponent meshComponent = meshes.Create(actor);
            transform.SetLocalPosition(origin);
            transform.SetLocalRotation(rotation);
            transform.UpdateTransform();

            Material[] md5Materials = new Material[model.meshes.Length];
            for(int i = 0; i < md5Materials.Length; i++) {
                md5Materials[i] = Common.declManager.FindMaterial("ZombieAB_MAT3"); 
            } 

            meshComponent.SetRenderData(mesh, md5Materials, typeof(DrawVertexPackedVertexFactory), assetName);
            //meshComponent.UpdateBounds(transform);    
            //skeletonEntity = CreateSkeleton(actor, skeleton, model.bindposes, out var rootJoint);
            //meshComponent.SetSkeleton(skeletonEntity, rootJoint);

            return actor;
        }

        public Entity CreateDecal(string name) {
            Entity entity = Entity.Create();
            names.Create(entity).name = name;
            var transform = transforms.Create(entity);
            transform.flags |= TransformComponent.Flags.CacheGlobal;
            var decal = decals.Create(entity);
            decal.SetNormalImage(defaultDecalNormalName, defaultDecalNormalScaleBias);
            decalBounds.Create(entity);
            return entity;            
        }

        public Entity CreateScatteringVolume(string name) {
            Entity entity = Entity.Create();
            names.Create(entity).name = name;
            var transform = transforms.Create(entity);
            transform.flags |= TransformComponent.Flags.CacheGlobal;
            scatteringVolumes.Create(entity);
            scatteringVolumeBounds.Create(entity);
            return entity;              
        }

        public Entity CreateLight(string name, LightType lightType) {
            Entity entity = Entity.Create();
            names.Create(entity).name = name;
            var transform = transforms.Create(entity);

            LightComponent light = null;
            if(lightType == LightType.Directional) {
                light = directionalLights.Create(entity);
                light.SetType(lightType);
            } else {
                light = lights.Create(entity);
                light.SetType(lightType);
                switch(lightType) {
                    case LightType.Sphere:
                    case LightType.Point:
                        lightBounds.Create(entity).InitFromHalWidth(Vector3.Zero, new Vector3(light.range, light.range, light.range));
                        break;
                    case LightType.Spot: {
                        lightBounds.Create(entity).InitFromHalWidth(Vector3.Zero, new Vector3(light.range, light.range, light.range));
                        break;
                    }
                }
            }
            transform.flags |= TransformComponent.Flags.CacheGlobal;
            return entity;
        }

        void UpdateTransforms() {
            for(int i = 0; i < transforms.Num(); i++) {
                TransformComponent transform = transforms[i];
                transform.UpdateTransform();
            }            
        }

        void UpdateHierarchy() {
            for(int i = 0; i < hierarchy.Num(); i++) {
                HierarchyComponent component = hierarchy[i];
                TransformComponent child = transforms.GetComponent(hierarchy.GetEntity(i));
                TransformComponent parent = transforms.GetComponent(component.parent);
                if(child != null && parent != null) {
                    child.UpdateTransform(parent, component.localToParentMatrix);
                }
            }            
        }

        void UpdateSkeletons() {
            for(int i = 0; i < skeletons.Num(); i++) {
                SkeletonComponent skeleton = skeletons[i];
                TransformComponent transform = transforms.GetComponent(skeletons.GetEntity(i));
                TransformComponent rootJointTransform = transforms.GetComponent(skeleton.rootJoint);
                Matrix rootWorldToLocalMatrix = Matrix.Invert(rootJointTransform.localToWorldMatrix);
                Entity[] joints = skeleton.joints;
                Matrix[] bindPoses = skeleton.bindPoses;
                for(int jointIndex = 0; jointIndex < joints.Length; jointIndex++) {
                    TransformComponent jointTransform = transforms.GetComponent(joints[jointIndex]);
                    skeleton.matrices[jointIndex] = bindPoses[jointIndex] * jointTransform.localToWorldMatrix * rootWorldToLocalMatrix;
                    Matrix m = jointTransform.localToWorldMatrix;
                    m.Decompose(out var scale, out var rotation, out var position);
                    renderDebug.AddSphere(16, 0.01f, position, Color.Yellow);
                }
            }            
        }

        int numActivedViews;
        void UpdateCameras() {
            for(int i = 0; i < cameras.Num(); i++) {
                Entity entity = cameras.GetEntity(i);
                TransformComponent transform = transforms.GetComponent(entity);
                CameraComponent camera = cameras[i];
                if(transform != null) {
                    camera.TransformCamera(transform);
                }
                camera.UpdateCamera();
                numActivedViews++;
            }
        }

        void UpdateMeshes() {    
            for(int i = 0; i < meshes.Num(); i++) {
                Entity entity = meshes.GetEntity(i);
                TransformComponent transform = null;
                if(meshes[i].skeletonID.IsValid()) {
                    transform = transforms.GetComponent(meshes[i].rootJoint);
                } else {
                    transform = transforms.GetComponent(entity);
                }
                meshes[i].UpdateWorldBounds(transform);
                //renderDebug.AddBox(meshes[i].localBoundingBox, Quaternion.Identity, Color.Yellow);
            }
        }

        void UpdateDecals() {
            for(int i = 0; i < decalBounds.Num(); i++) {
                Entity entity = decals.GetEntity(i);
                DecalComponent decal = decals[i];
                TransformComponent transform = transforms.GetComponent(entity);
                BoundsComponent bounds = decalBounds[i];
                bounds.InitFromHalWidth(Vector3.Zero, Vector3.One);
                Matrix m = Matrix.Scaling(decal.size) * transform.localToWorldMatrix;
                MathHelper.TransformBoundingBox(ref bounds.boundingBox, ref m);
                decal.origin = transform.GetPosition();
                decal.right = transform.GetRight();
                decal.forward = transform.GetForward();
                decal.up = transform.GetUp();
            }                
        }

        List<ViewDef> bakedProbeViewDefs;

        public void ClearBakedProbes() {
            if(bakedProbeViewDefs == null) {
                bakedProbeViewDefs = new List<ViewDef>();
            } else {
                bakedProbeViewDefs.Clear();
            }
        }

        void UpdateLights() {
            for(int i = 0; i < directionalLights.Num(); i++) {
                Entity entity = directionalLights.GetEntity(i);
                LightComponent light = directionalLights[i];
                TransformComponent transform = transforms.GetComponent(entity);
                light.forward = transform.GetForward();
                light.right = transform.GetRight();
                light.up = transform.GetUp();              
            }
            for(int i = 0; i < lightBounds.Num(); i++) {
                Entity entity = lights.GetEntity(i);
                LightComponent light = lights[i];
                TransformComponent transform = transforms.GetComponent(entity);
                BoundsComponent bounds = lightBounds[i];
                light.worldPosition  = transform.GetPosition();
                light.forward = transform.GetForward();
                light.right = transform.GetRight();
                light.up = transform.GetUp();
                bounds.InitFromHalWidth(light.worldPosition, new Vector3(light.range, light.range, light.range));
            }   
            for(int i = 0; i < reflectionProbes.Num(); i++) {
                ReflectionProbeComponent probe = reflectionProbes[i];
                BoundsComponent bounds = reflectionProbeBounds[i];

                if(probe.IsDirty()) {
                    probe.SetDirty(false);
                    Entity entity = reflectionProbes.GetEntity(i);
                    TransformComponent transform = transforms.GetComponent(entity);

                    //Vector3 boundsCenter = transform.GetPosition() + probe.localBounds.Center;
                    //Vector3 boundsExtents = probe.localBounds.Size * 0.5f;
                    //bounds.boundingBox = new BoundingBox(boundsCenter - boundsExtents, boundsCenter + boundsExtents);
                    bounds.boundingBox = probe.localBounds;

                    probe.Update(bounds.boundingBox);

                    LightShaderParameter parms = new LightShaderParameter();
                    parms.flags = MathHelper.PackR8G8B8A8(new Vector4(0f, probe.innerFalloff, 0f, probe.probeID));
                    var v = MathHelper.UnpackR8G8B8A8(parms.flags);
                    parms.positionWS = transform.GetPosition();
                    parms.specularMultiplier = probe.specularMultiplier;
                    parms.worldToLightMatrix = probe.boxProjectionMatrix;
                    parms.boxMin = bounds.boundingBox.Minimum;
                    parms.boxMax = bounds.boundingBox.Maximum;
                    parms.lightScattering = probe.distanceFade;
                    if(reflectionProbeShaderParms == null) {
                        reflectionProbeShaderParms = new LightShaderParameter[ClusteredLighting.MaxProbesOnScreen];
                    }
                    reflectionProbeShaderParms[probe.probeID] = parms;  
                    needUpdateRenderDatas = true;
                }
                //renderDebug.AddBox(bounds.boundingBox, Quaternion.Identity, Color.Yellow);
            }
            for(int i = 0; i < scatteringVolumeBounds.Num(); i++) {
                Entity entity = scatteringVolumes.GetEntity(i);
                ScatteringVolumeComponent scatteringVolume = scatteringVolumes[i];
                TransformComponent transform = transforms.GetComponent(entity);
                BoundsComponent bounds = scatteringVolumeBounds[i];
                bounds.InitFromHalWidth(Vector3.Zero, Vector3.One);
                Matrix m = Matrix.Scaling(scatteringVolume.size) * transform.localToWorldMatrix;
                bounds.boundingBox = MathHelper.TransformBoundingBox(bounds.boundingBox, ref m);
                scatteringVolume.origin = transform.GetPosition();
                scatteringVolume.right = transform.GetRight();
                scatteringVolume.forward = transform.GetForward();
                scatteringVolume.up = transform.GetUp();
            }                                  
        }

        void WriteParticleStageInstanceConstants(ref Span<ModelPassInstanceConstants> buffer, int instanceOffset, int modelIndex) {
            Entity entity = meshes.GetEntity(modelIndex);
            TransformComponent transform = transforms.GetComponent(entity);
            ModelPassInstanceConstants instanceConstants = new ModelPassInstanceConstants();
            Matrix worldToLocalMatrix = Matrix.Invert(transform.localToWorldMatrix);
            Vector3 worldToLocalTranslation = worldToLocalMatrix.TranslationVector;
            Vector3 localToWorldTranslation = transform.localToWorldMatrix.TranslationVector;
            instanceConstants.modelIndex = new Vector4(modelIndex, 0, 0, 0);
            instanceConstants.modelParms = new Vector4(0f, 0f, 0f, 0f);
            instanceConstants.localToWorldMatrix0 = new Vector4((Vector3)transform.localToWorldMatrix.Column1, localToWorldTranslation.X);
            instanceConstants.localToWorldMatrix1 = new Vector4((Vector3)transform.localToWorldMatrix.Column2, localToWorldTranslation.Y);
            instanceConstants.localToWorldMatrix2 = new Vector4((Vector3)transform.localToWorldMatrix.Column3, localToWorldTranslation.Z);
            buffer[instanceOffset] = instanceConstants;
        }

        void WriteInstanceBuffer(
            int modelIndex, 
            ref Span<ModelPassInstanceConstants> instanceConstantBuffer, 
            ref int instanceOffset, 
            ref Span<Vector4> instanceMatricesBuffer,
            ref int instanceMatrixOffset) 
        {
            Entity entity = meshes.GetEntity(modelIndex);
            MeshComponent model = meshes[modelIndex];
            TransformComponent transform = transforms.GetComponent(entity);
            ModelPassInstanceConstants instanceConstants = new ModelPassInstanceConstants();
            Matrix localToWorldMatrix;

            int modelMatrixOffset = instanceMatrixOffset;
            if(model.IsSkinned()) {
                SkeletonComponent skeletonComponent = skeletons.GetComponent(model.skeletonID);
                TransformComponent rootJointTransform = transforms.GetComponent(skeletonComponent.rootJoint);
                localToWorldMatrix = rootJointTransform.localToWorldMatrix;
                Matrix[] matrices = skeletonComponent.matrices;
                int matrixOffset = instanceMatrixOffset * 3;
                for(int i = 0; i < matrices.Length; i++, matrixOffset += 3) {
                    Vector3 translation = matrices[i].TranslationVector;
                    instanceMatricesBuffer[matrixOffset] = new Vector4((Vector3)matrices[i].Column1, translation.X);
                    instanceMatricesBuffer[matrixOffset + 1] = new Vector4((Vector3)matrices[i].Column2, translation.Y);
                    instanceMatricesBuffer[matrixOffset + 2] = new Vector4((Vector3)matrices[i].Column3, translation.Z);
                }
                instanceMatrixOffset += matrices.Length;
            } else {
                localToWorldMatrix = transform.localToWorldMatrix;
            }
            Matrix worldToLocalMatrix = transform.worldToLocalMatrix;// Matrix.Invert(localToWorldMatrix);
            Vector3 worldToLocalTranslation = worldToLocalMatrix.TranslationVector;
            Vector3 localToWorldTranslation = localToWorldMatrix.TranslationVector;
            instanceConstants.modelIndex = new Vector4(modelIndex, instanceOffset, modelMatrixOffset, model.IsSkinned() ? 1 : 0);
            instanceConstants.modelParms = new Vector4(model.IsStatic() ? 0 : 1, 0f, 0f, 0f);
            instanceConstants.lightmapScaleOffset = model.lightmapScaleOffset;
            instanceConstants.localToWorldMatrix0 = new Vector4((Vector3)localToWorldMatrix.Column1, localToWorldTranslation.X);
            instanceConstants.localToWorldMatrix1 = new Vector4((Vector3)localToWorldMatrix.Column2, localToWorldTranslation.Y);
            instanceConstants.localToWorldMatrix2 = new Vector4((Vector3)localToWorldMatrix.Column3, localToWorldTranslation.Z);
            instanceConstants.worldToLocalMatrix0 = new Vector4((Vector3)worldToLocalMatrix.Column1, worldToLocalTranslation.X);
            instanceConstants.worldToLocalMatrix1 = new Vector4((Vector3)worldToLocalMatrix.Column2, worldToLocalTranslation.Y);
            instanceConstants.worldToLocalMatrix2 = new Vector4((Vector3)worldToLocalMatrix.Column3, worldToLocalTranslation.Z);
            instanceConstantBuffer[instanceOffset] = instanceConstants;
            instanceOffset++;
        }

        const int LF_SPHERE = (1 << 2);
        const int LF_DISC = (2 << 2);
        const int LF_RECTANGLE = (3 << 2);
        const int LF_TUBE = (4 << 2);
        const int LF_AREA_MASK = (LF_SPHERE | LF_DISC | LF_RECTANGLE | LF_TUBE);
        const int LF_PROJECTOR_2D = (1 << 5);
        const int LF_PROJECTOR_CUBEMAP = (2 << 5);
        const int LF_PROJECTOR_MASK = (LF_PROJECTOR_2D | LF_PROJECTOR_CUBEMAP);

        void AddShadowReceiverSurfaces(
            ViewDef view,
            MeshComponent model, 
            int modelIndex, 
            ref Span<ModelPassInstanceConstants> instanceConstantBuffer, 
            ref int instanceOffset,
            ref Span<Vector4> instanceMatricesBuffer,
            ref int instanceMatrixOffset,
            ref int numOpaqueSurfaces,
            ref int numAlphaTestSurfaces,
            ref int numTransparentSurfaces) 
        {
            ViewEntity space = new ViewEntity();
            if(model.frameCount != Time.frameCount) {
                space.id = instanceOffset;
                model.frameCount = Time.frameCount;
                model.instanceOffset = instanceOffset;
                //WriteInstanceConstants(ref modelInstanceBuffer, modelInstanceOffset, modelIndex, model.lightmapScaleOffset);
                //++modelInstanceOffset;
                WriteInstanceBuffer(
                    modelIndex,
                    ref instanceConstantBuffer, 
                    ref instanceOffset, 
                    ref instanceMatricesBuffer, 
                    ref instanceMatrixOffset);
            } else {
                space.id = model.instanceOffset;
            }
            space.vertexFactory = model.runtimeMesh.Item1;
            space.indexBuffer = model.runtimeMesh.Item2;
            MeshSurface[] meshSurfaces = model.surfaces;
            for(int surfaceIndex = 0; surfaceIndex < meshSurfaces.Length; surfaceIndex++) {
                SubMesh drawInfo = meshSurfaces[surfaceIndex].drawInfo;
                var material = meshSurfaces[surfaceIndex].material;
                //var materials = material.MakeRenderProxy();
                DrawSurface drawSurface = view.shadowReceiverSurfaces.Alloc(material, ref numOpaqueSurfaces, ref numAlphaTestSurfaces, ref numTransparentSurfaces);
                drawSurface.space = space;
                drawSurface.drawInfo = drawInfo;
                drawSurface.shadowMaterial= model.IsStatic() ? material.MakeStaticModelShadowCastingProxy(model.numPositionStreams) : material.MakeDynamicModelShadowCastingProxy(model.numPositionStreams);
            }                
        }

        void CullingPointLightShadowSurfaces(
            ViewDef view,
            LightComponent light, 
            BoundsComponent bounds,
            ref Span<ModelPassInstanceConstants> instanceConstantBuffer, 
            ref int modelInstanceOffset, 
            ref Span<Vector4> instanceMatricesBuffer,
            ref int instanceMatrixOffset,
            ref Span<ShadowViewDef> shadowViewDefs, 
            ref int offset) 
        {
            int numDefs = 0;
            int opaqueSurfaceOffset = view.shadowReceiverSurfaces.opaqueSurfaces.Count;
            int alphaTestSurfaceOffset = view.shadowReceiverSurfaces.alphaTestSurfaces.Count;
            int numOpaqueSurfaces = 0;
            int numAlphaTestSurfaces = 0;
            int numTransparentSurfaces = 0;
            int numModels = 0;
            Span<int> viewModels = FrameAllocator.Alloc<int>(1024);
            BoundingSphere sphere = new BoundingSphere(light.worldPosition, light.range);
            for(int modelIndex = 0; modelIndex < meshes.Num(); modelIndex++) {
                MeshComponent model = meshes[modelIndex];
                BoundingBox boundingBox = model.boundingBox;
                if(model.IsCastShadow() && sphere.Contains(ref boundingBox) != ContainmentType.Disjoint) {
                    viewModels[numModels] = modelIndex;
                    numModels++;
                }
            }

            light.shadowIndex = offset;
            int shadowMapSize = light.ComputeShadowMapSize(view);
            for(int faceID = 0; faceID < 6; faceID++) {
                Matrix shadowMatrix = light.ComputePointLightShadowMatrix(faceID, shadowMapSize);
                BoundingFrustum frustum = new BoundingFrustum(shadowMatrix);
                for(int i = 0; i < numModels; i++) {
                    int modelIndex = viewModels[i];
                    MeshComponent model = meshes[modelIndex];
                    if(frustum.Contains(model.boundingBox) != ContainmentType.Disjoint) {
                        numDefs = 1;
                        AddShadowReceiverSurfaces(
                            view, 
                            model, 
                            modelIndex, 
                            ref instanceConstantBuffer, 
                            ref modelInstanceOffset, 
                            ref instanceMatricesBuffer,
                            ref instanceMatrixOffset,
                            ref numOpaqueSurfaces, 
                            ref numAlphaTestSurfaces, 
                            ref numTransparentSurfaces);
                    }
                }
                if(numDefs > 0) {
                    Rectangle viewport = Rectangle.Empty;
                    if(shadowAtlasPacker.Insert(shadowMapSize, shadowMapSize, ref viewport)) {
                        shadowViewDefs[offset].x = viewport.X;
                        shadowViewDefs[offset].y = viewport.Y;
                        shadowViewDefs[offset].width = viewport.Width;
                        shadowViewDefs[offset].height = viewport.Height;  
                        shadowViewDefs[offset].viewConstants.biasParms = new Vector4(light.shadowBias);                          
                        shadowViewDefs[offset].viewConstants.shadowMatrix = shadowMatrix;
                        shadowViewDefs[offset].opaqueSurfaceOffset = opaqueSurfaceOffset + view.shadowReceiverSurfaces.opaqueSurfaces.Offset;
                        shadowViewDefs[offset].numOpaqueSurfaces = numOpaqueSurfaces;
                        shadowViewDefs[offset].alphaTestSurfaceOffset = alphaTestSurfaceOffset + view.shadowReceiverSurfaces.alphaTestSurfaces.Offset;
                        shadowViewDefs[offset].numAlphaTestSurfaces = numAlphaTestSurfaces;
                        ++offset;
                    }
                }    
            }
        }

        void CullingDirectionalLightShadowSurfaces(
            ViewDef view,
            LightComponent light, 
            BoundsComponent bounds,
            ref Span<ModelPassInstanceConstants> instanceConstantBuffer, 
            ref int modelInstanceOffset, 
            ref Span<Vector4> instanceMatricesBuffer,
            ref int instanceMatrixOffset,
            ref Span<ShadowViewDef> shadowViewDefs, 
            ref int offset) 
        {
            int numDefs = 0;
            int opaqueSurfaceOffset = view.shadowReceiverSurfaces.opaqueSurfaces.Count;
            int alphaTestSurfaceOffset = view.shadowReceiverSurfaces.alphaTestSurfaces.Count;
            int numOpaqueSurfaces = 0;
            int numAlphaTestSurfaces = 0;
            int numTransparentSurfaces = 0;

            light.shadowIndex = offset;
            int shadowMapSize = light.ComputeShadowMapSize(view);
            for(int cascadeIndex = 0; cascadeIndex < RenderingCVars.numSunShadowCascades.value; cascadeIndex++) {
                Matrix shadowMatrix = light.ComputeDirectionalLightShadowMatrix(cascadeIndex, RenderingCVars.numSunShadowCascades.value, LightComponent.fourCascadesSplit, 0f, 200f, shadowMapSize, ref view.viewMatrix, ref view.projectionMatrix, out BoundingSphere cullingSphere);
                BoundingFrustum frustum = new BoundingFrustum(shadowMatrix);
                for(int modelIndex = 0; modelIndex < meshes.Num(); modelIndex++) {
                    MeshComponent model = meshes[modelIndex];
                    BoundingBox boundingBox = model.boundingBox;
                    if(model.IsCastShadow() && cullingSphere.Contains(ref boundingBox) != ContainmentType.Disjoint) {
                        numDefs = 1;
                        AddShadowReceiverSurfaces(
                            view, 
                            model, 
                            modelIndex, 
                            ref instanceConstantBuffer, 
                            ref modelInstanceOffset, 
                            ref instanceMatricesBuffer,
                            ref instanceMatrixOffset,
                            ref numOpaqueSurfaces, 
                            ref numAlphaTestSurfaces, 
                            ref numTransparentSurfaces);
                    }
                }

                if(numDefs > 0) {
                    Rectangle viewport = Rectangle.Empty;
                    if(shadowAtlasPacker.Insert(shadowMapSize, shadowMapSize, ref viewport)) {
                        shadowViewDefs[offset].x = viewport.X;
                        shadowViewDefs[offset].y = viewport.Y;
                        shadowViewDefs[offset].width = viewport.Width;
                        shadowViewDefs[offset].height = viewport.Height;  
                        shadowViewDefs[offset].viewConstants.biasParms = new Vector4(light.shadowBias);                          
                        shadowViewDefs[offset].viewConstants.shadowMatrix = shadowMatrix;
                        shadowViewDefs[offset].opaqueSurfaceOffset = opaqueSurfaceOffset + view.shadowReceiverSurfaces.opaqueSurfaces.Offset;
                        shadowViewDefs[offset].numOpaqueSurfaces = numOpaqueSurfaces;
                        shadowViewDefs[offset].alphaTestSurfaceOffset = alphaTestSurfaceOffset + view.shadowReceiverSurfaces.alphaTestSurfaces.Offset;
                        shadowViewDefs[offset].numAlphaTestSurfaces = numAlphaTestSurfaces;
                        ++offset;
                    }
                }    
            }
        }

        void CullingSpotLightShadowSurfaces(
            ViewDef view,
            LightComponent light, 
            BoundsComponent bounds,
            ref Span<ModelPassInstanceConstants> instanceConstantBuffer, 
            ref int instanceOffset, 
            ref Span<Vector4> instanceMatricesBuffer,
            ref int instanceMatrixOffset,
            ref Span<ShadowViewDef> shadowViewDefs, 
            ref int offset) 
        {
            int numDefs = 0;
            int opaqueSurfaceOffset = view.shadowReceiverSurfaces.opaqueSurfaces.Count;
            int alphaTestSurfaceOffset = view.shadowReceiverSurfaces.alphaTestSurfaces.Count;
            int numOpaqueSurfaces = 0;
            int numAlphaTestSurfaces = 0;
            int numTransparentSurfaces = 0;
            Matrix shadowMatrix = light.ComputeSpotLightShadowMatrix();
            BoundingFrustum frustum = new BoundingFrustum(shadowMatrix);
            for(int modelIndex = 0; modelIndex < meshes.Num(); modelIndex++) {
                MeshComponent model = meshes[modelIndex];
                if(model.IsCastShadow() && frustum.Contains(model.boundingBox) != ContainmentType.Disjoint) {
                    numDefs = 1;
                    AddShadowReceiverSurfaces(
                        view, 
                        model, 
                        modelIndex, 
                        ref instanceConstantBuffer, 
                        ref instanceOffset, 
                        ref instanceMatricesBuffer,
                        ref instanceMatrixOffset,
                        ref numOpaqueSurfaces, 
                        ref numAlphaTestSurfaces, 
                        ref numTransparentSurfaces);
                }
            }
            if(numDefs > 0) {
                int shadowMapSize = light.ComputeShadowMapSize(view);
                Rectangle viewport = Rectangle.Empty;
                if(shadowAtlasPacker.Insert(shadowMapSize, shadowMapSize, ref viewport)) {
                    light.shadowIndex = offset;
                    shadowViewDefs[offset].x = viewport.X;
                    shadowViewDefs[offset].y = viewport.Y;
                    shadowViewDefs[offset].width = viewport.Width;
                    shadowViewDefs[offset].height = viewport.Height;  
                    shadowViewDefs[offset].viewConstants.biasParms = new Vector4(light.shadowBias);                          
                    shadowViewDefs[offset].viewConstants.shadowMatrix = shadowMatrix;
                    shadowViewDefs[offset].opaqueSurfaceOffset = opaqueSurfaceOffset + view.shadowReceiverSurfaces.opaqueSurfaces.Offset;
                    shadowViewDefs[offset].numOpaqueSurfaces = numOpaqueSurfaces;
                    shadowViewDefs[offset].alphaTestSurfaceOffset = alphaTestSurfaceOffset + view.shadowReceiverSurfaces.alphaTestSurfaces.Offset;
                    shadowViewDefs[offset].numAlphaTestSurfaces = numAlphaTestSurfaces;
                    ++offset;
                } else {
                    throw new InvalidOperationException();
                }
            }
        }

        void CreateShadowViews(
            ViewDef view, 
            LightComponent light, 
            BoundsComponent bounds, 
            ref Span<ModelPassInstanceConstants> instanceConstantBuffer, 
            ref int instanceOffset,
            ref Span<Vector4> instanceMatricesBuffer,
            ref int instanceMatrixOffset, 
            ref Span<ShadowViewDef> shadowViewDefs, 
            ref int offset) 
        {
            switch(light.type) {
                case LightType.Point: {
                    CullingPointLightShadowSurfaces(
                        view, 
                        light, 
                        bounds, 
                        ref instanceConstantBuffer, 
                        ref instanceOffset, 
                        ref instanceMatricesBuffer,
                        ref instanceMatrixOffset,
                        ref shadowViewDefs, 
                        ref offset);
                    break;
                }
                case LightType.Spot: {
                    CullingSpotLightShadowSurfaces(
                        view, 
                        light, 
                        bounds, 
                        ref instanceConstantBuffer, 
                        ref instanceOffset, 
                        ref instanceMatricesBuffer,
                        ref instanceMatrixOffset,
                        ref shadowViewDefs, 
                        ref offset);
                    break;
                }
                case LightType.Directional: {
                    CullingDirectionalLightShadowSurfaces(
                        view, 
                        light, 
                        bounds, 
                        ref instanceConstantBuffer, 
                        ref instanceOffset, 
                        ref instanceMatricesBuffer,
                        ref instanceMatrixOffset,
                        ref shadowViewDefs, 
                        ref offset);
                    break;
                }
            }
        }

        internal void Update() {
            numActivedViews = 0;
            UpdateTransforms();
            UpdateHierarchy();
            UpdateSkeletons();
            UpdateMeshes();
            UpdateCameras();
            UpdateLights();
            UpdateDecals();
        }

        void AddModels(
            ViewDef view, 
            ref Span<ModelPassInstanceConstants> instanceConstantBuffer, 
            ref int instanceOffset,
            ref Span<Vector4> instanceMatricesBuffer,
            ref int instanceMatrixOffset) 
        {
            for(int i = 0; i < meshes.Num(); i++) {
                MeshComponent model = meshes[i];
                if(model.IsEnabled() && view.frustum.Contains(model.boundingBox) != ContainmentType.Disjoint) {
                    ViewEntity space = new ViewEntity();
                    if(model.frameCount != Time.frameCount) {
                        space.id = instanceOffset;
                        model.frameCount = Time.frameCount;
                        model.instanceOffset = instanceOffset;
                        WriteInstanceBuffer(
                            i,
                            ref instanceConstantBuffer, 
                            ref instanceOffset, 
                            ref instanceMatricesBuffer, 
                            ref instanceMatrixOffset);
                    } else {
                        space.id = model.instanceOffset;
                    }

                    space.vertexFactory = model.runtimeMesh.Item1;
                    space.indexBuffer = model.runtimeMesh.Item2;
                    MeshSurface[] meshSurfaces = model.surfaces;
                    for(int surfaceIndex = 0; surfaceIndex < meshSurfaces.Length; surfaceIndex++) {
                        SubMesh drawInfo = meshSurfaces[surfaceIndex].drawInfo;
                        var material = meshSurfaces[surfaceIndex].material;
                        //var materials = material.MakeRenderProxy();
                        DrawSurface drawSurface = model.IsStatic() ? view.staticSurfaces.Alloc(material) : view.dynamicSurfaces.Alloc(material);
                        drawSurface.space = space;
                        drawSurface.drawInfo = drawInfo;
                        if(model.IsLightmapStatic()) {
                            drawSurface.material= material.MakeLightingProxy(lightmapDirMaps[model.lightmapIndex], lightmapColorMaps[model.lightmapIndex]);
                        } else {
                            drawSurface.material= material.MakeLightingProxy();
                        }
                        // materials.Item1;
                        drawSurface.prezMaterial = model.IsStatic() ? material.MakeStaticModelPrezProxy(model.numPositionStreams) : material.MakeDynamicModelPrezProxy(model.numPositionStreams);// materials.Item2;
                        //renderDebug.AddBox(drawInfo.boundingBox.Minimum, drawInfo.boundingBox.Maximum, drawInfo.boundingBox.Center, Quaternion.Identity, Color.Blue);
                    }    
                }
            }
        }

        ParticleSystemTest particleSystemTest = new ParticleSystemTest();

        unsafe void AddParticleStageModels(            
            ViewDef view, 
            ref Span<ParticleVertex> vertexBuffer,
            ref int vertexOffset,
            ref Span<UInt16> indexBuffer,
            ref int indexOffset,
            ref Span<ModelPassInstanceConstants> instanceConstantBuffer, 
            ref int instanceOffset)  
        {
            view.numParticleStageDrawSurfaces = 1;
            view.particleStageDrawSurfaces = FrameAllocator.AllocMemory(Utilities.SizeOf<ParticleStageDrawSurface>());
            Span<ParticleStageDrawSurface> drawSurfaces = new Span<ParticleStageDrawSurface>(view.particleStageDrawSurfaces.ToPointer(), 1);
            int surfaceOffset = 0;

            RenderView renderView = new RenderView();
            renderView.time = Time.time;
            renderView.origin = view.viewOrigin;
            renderView.right = view.viewRight;
            renderView.forward = view.viewForward;
            renderView.up = view.viewUp;

            particleSystemTest.Update(this);
            particleSystemTest.Update(
                ref renderView,
                ref instanceOffset,
                ref vertexBuffer,
                ref vertexOffset,
                ref indexBuffer,
                ref indexOffset,
                ref drawSurfaces,
                ref surfaceOffset
            );
        }

        unsafe void PrepareClusterdDatas(
            ViewDef view, 
            ClusteredLighting clusteredLighting, 
            ref Span<ModelPassInstanceConstants> instanceConstantBuffer, 
            ref int instanceOffset,
            ref Span<Vector4> instanceMatricesBuffer,
            ref int instanceMatrixOffset) 
        {
            view.clusteredLightingSize = clusteredLighting.clusteredLightingSize;
            view.clusteredLightingParms = clusteredLighting.clusteredLightingParms;
            view.clusterListSizeInBytes = Utilities.SizeOf<ClusterData>() * clusteredLighting.numClusters;
            view.clusterItemListSizeInBytes = sizeof(UInt32) * clusteredLighting.numClusters * ClusteredLighting.MaxItemsPerCluster;
            view.clusterList = FrameAllocator.AllocMemory(view.clusterListSizeInBytes);
            view.clusterItemList = FrameAllocator.AllocMemory(view.clusterItemListSizeInBytes);

            int numClustersX = clusteredLighting.numClustersX;
            int numClustersY = clusteredLighting.numClustersY;
            int numClustersZ = clusteredLighting.numClustersZ;
            int numClusters = numClustersX * numClustersY * numClustersZ;

            Span<ClusterData> clusterList = new Span<ClusterData>(view.clusterList.ToPointer(), numClusters);
            Span<UInt32> clusterItemList = new Span<UInt32>(view.clusterItemList.ToPointer(), numClusters * ClusteredLighting.MaxItemsPerCluster);

            int numDirectionalLights = 0;
            for(int lightIndex = 0; lightIndex < directionalLights.Num(); lightIndex++) {
                if(directionalLights[lightIndex].IsEnabled()) {
                    ++numDirectionalLights;
                }
            }
            for(int clusterIndex = 0; clusterIndex < numClusters; clusterIndex++) {
                int offset = clusterIndex * ClusteredLighting.MaxItemsPerCluster;
                for(int lightIndex = 0; lightIndex < numDirectionalLights; lightIndex++) {
                    if(directionalLights[lightIndex].IsEnabled()) {
                        clusterItemList[offset + lightIndex] = (UInt32)lightIndex;
                    }
                }
                clusterList[clusterIndex].offset = (UInt32)offset;
                clusterList[clusterIndex].count = (UInt32)(numDirectionalLights);
            }

            BoundingFrustum frustum = view.frustum;

            view.numShadowViews = 0;
            view.shadowViewDefs = FrameAllocator.AllocMemory(Utilities.SizeOf<ShadowViewDef>() * ClusteredLighting.MaxShadowsOnScreen);
            Span<ShadowViewDef> shadowViewDefs = new Span<ShadowViewDef>(view.shadowViewDefs.ToPointer(), ClusteredLighting.MaxShadowsOnScreen);
            if(directionalLights.Num() > 0 && directionalLights[0].IsEnabled() && directionalLights[0].IsCastingShadows()) {
                CreateShadowViews(
                    view, 
                    directionalLights[0], 
                    null, 
                    ref instanceConstantBuffer, 
                    ref instanceOffset, 
                    ref instanceMatricesBuffer,
                    ref instanceMatrixOffset,
                    ref shadowViewDefs, 
                    ref view.numShadowViews);
            }
            FrameList<int> viewLights = new FrameList<int>(1024);
            for(int i = 0; i < lightBounds.Num(); i++) {
                BoundsComponent bounds = lightBounds[i];
                if(lights[i].IsEnabled() && frustum.Contains(bounds.boundingSphere) != ContainmentType.Disjoint) {
                    Entity entity = lights.GetEntity(i);
                    TransformComponent transform = transforms.GetComponent(entity);
                    bounds.UpdateClusteredLightBounds(view.viewMatrix, lights[i], transform);
                    viewLights.Add(i);
                    if(lights[i].IsCastingShadows()) {
                        CreateShadowViews(
                            view, 
                            lights[i], 
                            bounds, 
                            ref instanceConstantBuffer, 
                            ref instanceOffset, 
                            ref instanceMatricesBuffer,
                            ref instanceMatrixOffset,
                            ref shadowViewDefs, 
                            ref view.numShadowViews);
                    }
                }
            }
            if(view.numShadowViews > 0) {
                view.shadowShaderParmsSizeInBytes = Utilities.SizeOf<ShadowShaderParameter>() * view.numShadowViews;
                view.shadowShaderParms = FrameAllocator.AllocMemory(view.shadowShaderParmsSizeInBytes);
                Span<ShadowShaderParameter> parms = new Span<ShadowShaderParameter>(view.shadowShaderParms.ToPointer(), view.numShadowViews);
                for(int i = 0; i < view.numShadowViews; i++) {
                    parms[i].atlasScaleBias = shadowViewDefs[i].AtlasScaleBias();
                    parms[i].shadowMatrix = shadowViewDefs[i].viewConstants.shadowMatrix * MathHelper.ScaleBiasMatrix;
                }
            }

            FrameList<int> viewProbes = new FrameList<int>(128);
            for(int i = 0; i < reflectionProbes.Num(); i++) {
                BoundsComponent bounds = reflectionProbeBounds[i];
                if(reflectionProbes[i].IsValid() &&  frustum.Contains(bounds.boundingBox) != ContainmentType.Disjoint) {
                    bounds.UpdateClusteredProbeBounds(view.viewMatrix);
                    viewProbes.Add(i);
                }
            }

            FrameList<int> viewDecals = new FrameList<int>(1024);
            for(int i = 0; i < decals.Num(); i++) {
                DecalComponent decal = decals[i];
                TransformComponent transform = transforms.GetComponent(decals.GetEntity(i));
                BoundsComponent bounds = decalBounds[i];
                if(frustum.Contains(bounds.boundingBox) != ContainmentType.Disjoint) {
                    bounds.UpdateClusteredDecalBounds(view.viewMatrix, decal.size, transform);
                    viewDecals.Add(i);
                }
            }

            int numViewScatteringVolumes = 0;
            Span<int> viewScatteringVolumes = FrameAllocator.Alloc<int>(SceneRenderer.MaxScatteringVolumesOnScreen);
            for(int i = 0; i < scatteringVolumeBounds.Num(); i++) {
                BoundsComponent bounds = scatteringVolumeBounds[i];
                if(frustum.Contains(bounds.boundingBox) != ContainmentType.Disjoint) {
                    viewScatteringVolumes[numViewScatteringVolumes] = i;
                    numViewScatteringVolumes++;
                }                
            }

            int numLights = viewLights.Count;
            int numDecals = viewDecals.Count;
            int numProbes = viewProbes.Count;
            ClusterFrustum[] clusterFrustums = clusteredLighting.frustums;
            for(int clusterIndex = 0; clusterIndex < numClusters; clusterIndex++) {
                int offset = clusterIndex * ClusteredLighting.MaxItemsPerCluster;
                int clusterLightCount = numDirectionalLights;
                int clusterDecalCount = 0;
                int clusterProbeCount = 0;
                for(int clusterLightIndex = 0; clusterLightIndex < numLights; clusterLightIndex++) {
                    int viewLightIndex = viewLights[clusterLightIndex];
                    BoundsComponent bounds = lightBounds[viewLightIndex];
                    if(CollisionEx.Overlap(bounds.planes, bounds.corners, clusterFrustums[clusterIndex].aabb)) {
                        clusterItemList[offset + clusterLightCount] |= (UInt32)(clusterLightIndex + numDirectionalLights);
                        ++clusterLightCount;
                    }
                }
                for(int clusterDecalIndex = 0; clusterDecalIndex < numDecals; clusterDecalIndex++) {
                    int viewDecalIndex = viewDecals[clusterDecalIndex];
                    BoundsComponent bounds = decalBounds[viewDecalIndex];
                    if(CollisionEx.Overlap(bounds.planes, bounds.corners, clusterFrustums[clusterIndex].aabb)) {
                        clusterItemList[offset + clusterProbeCount] |= (UInt32)(viewDecalIndex << 12);
                        ++clusterDecalCount;
                    }
                }
                for(int clusterProbeIndex = 0; clusterProbeIndex < numProbes; clusterProbeIndex++) {
                    int viewProbeIndex = viewProbes[clusterProbeIndex];
                    BoundsComponent bounds = reflectionProbeBounds[viewProbeIndex];
                    if(CollisionEx.Overlap(bounds.planes, bounds.corners, clusterFrustums[clusterIndex].aabb)) {
                        clusterItemList[offset + clusterProbeCount] |= (UInt32)(viewProbeIndex << 24);
                        ++clusterProbeCount;
                    }
                }
                clusterList[clusterIndex].offset = (UInt32)offset;
                clusterList[clusterIndex].count = (UInt32)(clusterLightCount | (clusterDecalCount << 8) | (clusterProbeCount << 16));
            }

            view.lightShaderParmsSizeInBytes = Utilities.SizeOf<LightShaderParameter>() * (numDirectionalLights + viewLights.Count);
            view.lightShaderParms = FrameAllocator.AllocMemory(view.lightShaderParmsSizeInBytes);
            Span<LightShaderParameter> lightShaderParms = new Span<LightShaderParameter>(view.lightShaderParms.ToPointer(), numDirectionalLights + viewLights.Count);
            view.decalShaderParmsSizeInBytes = Utilities.SizeOf<DecalShaderParameter>() * viewDecals.Count;
            view.decalShaderParms = FrameAllocator.AllocMemory(view.decalShaderParmsSizeInBytes);
            Span<DecalShaderParameter> decalShaderParms = new Span<DecalShaderParameter>(view.decalShaderParms.ToPointer(), viewDecals.Count);
            view.numScatteringVolumes = numViewScatteringVolumes;
            view.scatteringVolumeShaderParmsSizeInBytes = Utilities.SizeOf<ScatteringVolumeShaderParameter>() * numViewScatteringVolumes;
            view.scatteringVolumeShaderParms = FrameAllocator.AllocMemory(view.scatteringVolumeShaderParmsSizeInBytes);
            Span<ScatteringVolumeShaderParameter> scatteringVolumeShaderParms = new Span<ScatteringVolumeShaderParameter>(view.scatteringVolumeShaderParms.ToPointer(), numViewScatteringVolumes);            
            for(int i = 0; i < numDirectionalLights; i++) {
                LightComponent light = directionalLights[i];
                LightShaderParameter parms = new LightShaderParameter();
                parms.flags |= (uint)light.type;
                if(light.IsCastingShadows()) {
                    parms.flags |= 1 << 7;
                    parms.flags |= (uint)light.shadowIndex << 24;
                    parms.flags2 = (uint)light.shadowMapSize;
                }
                parms.colorPacked = MathHelper.PackRGBE(light.color * light.intensity);
                parms.positionWS = light.forward;
                parms.specularMultiplier = light.specularMultiplier;
                parms.lightScattering = light.lightScattering;
                lightShaderParms[i] = parms;
            }
            for(int i = 0; i < viewLights.Count; i++) {
                int viewLightIndex = viewLights[i];                
                LightComponent light = lights[viewLightIndex];
                LightShaderParameter parms = new LightShaderParameter();
                parms.flags |= (uint)(light.type) > 3 ? ((uint)(light.type - 3)) << 2 : (uint)(light.type);
                if(light.type == LightType.Spot) {
                    parms.flags |= 1 << 5;
                    uint[] scaleBias = MathHelper.PackR15G15B15A15(light.projectorScaleBias);
                    parms.projectorScaleBias0 = scaleBias[0];
                    parms.projectorScaleBias1 = scaleBias[1];
                }
                if(light.IsCastingShadows()) {
                    parms.flags |= 1 << 7;
                    parms.flags |= (uint)light.shadowIndex << 24;
                    parms.flags2 = (uint)light.shadowMapSize;
                }
                parms.colorPacked = MathHelper.PackRGBE(light.color * light.intensity);
                parms.positionWS = light.worldPosition;
                parms.range = light.range;
                parms.specularMultiplier = light.specularMultiplier;
                parms.lightScattering = light.lightScattering;
                light.ComputeLightMatrix(out parms.worldToLightMatrix);
                lightShaderParms[i + numDirectionalLights] = parms;
            }
            for(int i = 0; i < viewDecals.Count; i++) {
                int viewDecalIndex = viewDecals[i];                
                DecalComponent decal = decals[viewDecalIndex];
                DecalShaderParameter parms = new DecalShaderParameter();
                //parms.flags2 = decal.normalBlend != DecalComponent.NormalBlend.Linear ? (uint)(1 << (int)decal.normalBlend) : 0;
                //if(decal.invertNormal) {
                //    parms.flags2 |= 1 << 7;
                //}
                parms.flags2 = (uint)decal.flags;
                MathHelper.PackR15G15B15A15(decal.normalImageScaleBias, out parms.normalScaleBias0, out parms.normalScaleBias1);             
                if(decal.HasAlbedo()) {
                    MathHelper.PackR15G15B15A15(decal.albedoImageScaleBias, out parms.albedoScaleBias0, out parms.albedoScaleBias1);
                }
                if(decal.HasSpecular()) {
                    MathHelper.PackR15G15B15A15(decal.specularImageScaleBias, out parms.specularScaleBias0, out parms.specularScaleBias1);
                }
                parms.baseColorPacked = MathHelper.PackR8G8B8A8(decal.baseColor);
                parms.specualrOverridePacked = MathHelper.PackR8G8B8A8(decal.specularColor);
                parms.emissivePacked = MathHelper.PackR8G8B8A8(decal.emissiveColor);
                parms.opacityPacked = MathHelper.PackR8G8B8A8(decal.albedoBlendRatio, decal.specularBlendRatio, decal.smoothnessBlendRatio, decal.normalBlendRatio);
                decal.ComputeMatrix(out parms.worldToDecalMatrix0, out parms.worldToDecalMatrix1, out parms.worldToDecalMatrix2);
                decalShaderParms[i] = parms;
            }
            for(int i = 0; i < numViewScatteringVolumes; i++) {
                ScatteringVolumeComponent scatteringVolume = scatteringVolumes[viewScatteringVolumes[i]];
                ScatteringVolumeShaderParameter parms = new ScatteringVolumeShaderParameter();
                parms.colorPacked = MathHelper.PackRGBE(scatteringVolume.absorption * scatteringVolume.absorptionIntensity);
                parms.parms0 = MathHelper.PackR8G8B8A8(new Vector4(0f, 0f, scatteringVolume.densityFalloff, scatteringVolume.IsHeightFog() ? (1 << 1) : 0));
                parms.parms1 = MathHelper.PackR8G8B8A8(new Vector4(scatteringVolume.density, scatteringVolume.densityHeightK, scatteringVolume.densityVariationMax, scatteringVolume.densityVariationMin));
                float densityHeightK = ((parms.parms1 >> 16) & 0xFF ) / 255.0f;
                float densityVariationMax = ((parms.parms1 >> 8 ) & 0xFF) / 255.0f;
                float densityVariationMin = (parms.parms1 & 0xFF) / 255.0f;
                float density = ((parms.parms1 >> 24) & 0xFF) / 255.0f;
                float falloffRadius = ((parms.parms0 >> 8) & 0xff) / 255.0f;
                scatteringVolume.ComputeMatrix(out parms.worldToVolumeMatrix0, out parms.worldToVolumeMatrix1, out parms.worldToVolumeMatrix2);
                scatteringVolumeShaderParms[i] = parms;                
            }
        }

        unsafe void AddViewDefs(ref SceneRenderData renderData) {
            renderData.numModelInstances = 0;
            renderData.numInstanceMatrices = 0;
            renderData.modelInstanceConstantBuffer = FrameAllocator.AllocMemory(Utilities.SizeOf<ModelPassInstanceConstants>() * 4096 * 1);
            renderData.instanceMatricesBuffer = FrameAllocator.AllocMemory(Utilities.SizeOf<Vector4>() * SceneRenderer.MaxSkinnedMeshsOnScreen * SceneRenderer.MaxSkinnedMeshJointVectors);
            Span<ModelPassInstanceConstants> instanceConstantBuffer = new Span<ModelPassInstanceConstants>(renderData.modelInstanceConstantBuffer.ToPointer(), 4096 * 1);
            Span<Vector4> instanceMatricesBuffer = new Span<Vector4>(renderData.instanceMatricesBuffer.ToPointer(), SceneRenderer.MaxSkinnedMeshsOnScreen * SceneRenderer.MaxSkinnedMeshJointVectors);
            int particleVertexBufferSizeInBytes = Utilities.SizeOf<ParticleVertex>() * 16384;
            int particleIndexBufferSizeInBytes = Utilities.SizeOf<UInt16>() * 24576;
            renderData.particleVertexBuffer = FrameAllocator.AllocMemory(particleVertexBufferSizeInBytes);
            renderData.particleIndexBuffer = FrameAllocator.AllocMemory(particleIndexBufferSizeInBytes);
            Span<ParticleVertex> particleVertexBuffer = new Span<ParticleVertex>(renderData.particleVertexBuffer.ToPointer(), 16384);
            Span<UInt16> particleIndexBuffer = new Span<ushort>(renderData.particleIndexBuffer.ToPointer(), 24567);
            renderData.numParticleVertices = 0;
            renderData.numParticleIndices = 0;

            renderData.viewDefs = new ViewDef[numActivedViews];
            for(int i = 0; i < cameras.Num(); i++) {
                CameraComponent camera = cameras[i];
                ViewDef view = new ViewDef(drawSurfacePool);
                view.x = 0;
                view.y = 0;
                view.width = camera.width;
                view.height = camera.height;
                view.viewMatrix = camera.worldToViewMatrix;
                view.projectionMatrix = camera.projectionMatrix;
                view.viewOrigin = camera.viewOrigin;
                view.viewRight = camera.viewRight;
                view.viewForward = camera.viewForward;
                view.viewUp = camera.viewUp;
                view.frustumVectorLT = camera.frustumVectorLT;
                view.frustumVectorRT = camera.frustumVectorRT;
                view.frustumVectorLB = camera.frustumVectorLB;
                view.frustumVectorRB = camera.frustumVectorRB;
                view.nearClipPlane = camera.nearClipPlane;
                view.farClipPlane = camera.farClipPlane;
                view.viewProjectionMatrix = view.viewMatrix * view.projectionMatrix;
                view.frustum = new BoundingFrustum(view.viewProjectionMatrix);

                AddModels(
                    view,
                    ref instanceConstantBuffer,
                    ref renderData.numModelInstances,
                    ref instanceMatricesBuffer,
                    ref renderData.numInstanceMatrices
                );
/*
                AddParticleStageModels(
                    view,
                    ref particleVertexBuffer,
                    ref renderData.numParticleVertices,
                    ref particleIndexBuffer,
                    ref renderData.numParticleIndices,
                    ref instanceConstantBuffer,
                    ref renderData.numModelInstances
                );
*/
                if(camera.IsClusteredLighting()) {
                    PrepareClusterdDatas(
                        view, 
                        camera.clusteredLighting, 
                        ref instanceConstantBuffer, 
                        ref renderData.numModelInstances,
                        ref instanceMatricesBuffer,
                        ref renderData.numInstanceMatrices);
                }

                renderData.viewDefs[i] = view;
            }        
        }

        readonly GuillotinePacker shadowAtlasPacker = new GuillotinePacker();

        internal void GetRenderData(out SceneRenderData renderData) {
            renderData = new SceneRenderData();

            if(numActivedViews == 0) {
                return;
            }

            drawSurfacePool.Reset();
            shadowAtlasPacker.Clear(8192, 8192);

            if(needUpdateRenderDatas) {
                renderData.fxRenderData.reflectionProbeShaderParmsSizeInBytes = Utilities.SizeOf<LightShaderParameter>() * numProbes;
                renderData.fxRenderData.reflectionProbeShaderParms = FrameAllocator.AllocMemory(renderData.fxRenderData.reflectionProbeShaderParmsSizeInBytes);
                unsafe {
                    var buffer = new Span<LightShaderParameter>(renderData.fxRenderData.reflectionProbeShaderParms.ToPointer(), numProbes);
                    for(int i = 0; i < numProbes; i++) {
                        buffer[i] = reflectionProbeShaderParms[i];
                    }
                }
                needUpdateRenderDatas = false;
            }
            renderData.fxRenderData.reflectionProbeCubemaps = reflectionProbeCubemaps;

            if(sunLight != null && sunLight.IsEnabled()) {
                renderData.fxRenderData.usePSSM = sunLight.IsCastingShadows();
                renderData.fxRenderData.sunForward = sunLightTransform.GetForward();
                renderData.fxRenderData.sunColor = (Vector3)SkylighingCVars.sunColor.value;
                renderData.fxRenderData.sunIntensity = SkylighingCVars.sunIntensity.value;
                renderData.fxRenderData.sunDiskScale = SkylighingCVars.sunDiskScale.value;
                renderData.fxRenderData.decoupleSunColorFromSky = SkylighingCVars.decoupleSunColorFromSky.value;
                renderData.fxRenderData.radiusPlanet = SkylighingCVars.radiusPlanet.value;
                renderData.fxRenderData.radiusAtmosphere = SkylighingCVars.radiusAtmosphere.value;
                renderData.fxRenderData.mieAnisotropy = SkylighingCVars.mieAnisotropy.value;
                renderData.fxRenderData.mieDensityScale = SkylighingCVars.mieDensityScale.value;
                renderData.fxRenderData.mieHeight = SkylighingCVars.mieHeight.value;
                renderData.fxRenderData.rayleighHeight = SkylighingCVars.rayleighHeight.value;
                renderData.fxRenderData.rayleighColor = (Vector3)SkylighingCVars.rayleighColor.value;
                renderData.fxRenderData.mieColor = (Vector3)SkylighingCVars.mieColor.value;
                renderData.fxRenderData.viewMinDistance = SkylighingCVars.viewMinDistance.value;
                renderData.fxRenderData.viewMaxDistance = SkylighingCVars.viewMaxDistance.value;
                renderData.fxRenderData.scatteringDensityScale = SkylighingCVars.scatteringDensityScale.value;
                if(PostProcessingCVars.useSkylighting.value) {
                    renderData.fxRenderData.scatteringFlags |= 1 << 0;
                }
                if(PostProcessingCVars.useLocalLightScattering.value) {
                    renderData.fxRenderData.scatteringFlags |= 1 << 1;
                }
            }
            //renderData.fxRenderData.skyboxBackground = skyboxBackground;

            renderData.fxRenderData.gameTime = Time.time;
            renderData.fxRenderData.gameFrameDeltaTime = Time.delteTime;
            renderData.fxRenderData.gameFrameCount = Time.frameCount;
            renderData.fxRenderData.bloomThreshold = PostProcessingCVars.bloomThreshold.value;
            renderData.fxRenderData.volumetricLightingDistance = PostProcessingCVars.volumetricLightingDistance.value;
            renderData.fxRenderData.autoExposureMinLuminance = PostProcessingCVars.autoExposureMinLuminance.value;
            renderData.fxRenderData.autoExposureLuminanceScale = PostProcessingCVars.autoExposureLuminanceScale.value;
            renderData.fxRenderData.autoExposureLuminanceBlendRatio = PostProcessingCVars.autoExposureLuminanceBlendRatio.value;
            renderData.fxRenderData.autoExposureMin = PostProcessingCVars.autoExposureMin.value;
            renderData.fxRenderData.autoExposureMax = PostProcessingCVars.autoExposureMax.value;
            renderData.fxRenderData.autoExposureSpeed = Time.delteTime * 1f;
            renderData.fxRenderData.ssrScale = PostProcessingCVars.ssrScale.value;
            renderData.fxRenderData.ssrSmoothnessThreshold = PostProcessingCVars.ssrSmoothnessThreshold.value;
            renderData.fxRenderData.ssrDitherSampleOffset = PostProcessingCVars.ssrDitherSampleOffset.value;
            renderData.fxRenderData.ssrMode = PostProcessingCVars.useSSR.value ? 1 : 0;

            AddViewDefs(ref renderData);

            renderDebug.GetRenderData(out renderData.debugToolRenderData);
        }
    }
}