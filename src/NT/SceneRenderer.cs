using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using SharpDX;
using SharpDX.Mathematics;

namespace NT
{
    internal class SceneRenderer : IDisposable {
        public static SceneRenderer Instance {get; private set;}

        public const int MaxScatteringVolumesOnScreen = 32;
        public const int MaxSkinnedMeshsOnScreen = 32;
        public const int MaxSkinnedMeshJoints = 256;
        public const int MaxSkinnedMeshJointVectors = MaxSkinnedMeshJoints * 3;

        Veldrid.DeviceBuffer modelMatricesBuffer;
        Veldrid.DeviceBuffer clusterListBuffer;
        Veldrid.DeviceBuffer clusterItemListBuffer;
        Veldrid.DeviceBuffer lightListBuffer;
        Veldrid.DeviceBuffer decalListBuffer;
        Veldrid.DeviceBuffer shadowListBuffer;
        Veldrid.DeviceBuffer probeListBuffer;
        Veldrid.TextureView shadowsAtlasMap;
        Veldrid.DeviceBuffer scatteringVolumesUniformBuffer;
        Veldrid.DeviceBuffer viewUniformBuffer;
        Veldrid.DeviceBuffer shadowViewUniformBuffer;
        Veldrid.DeviceBufferRange instanceConstantBuffer;
        public Veldrid.Texture lightsAtlasMap {get; private set;}
        public Veldrid.Texture decalsAtlasMap {get; private set;}
        public Veldrid.Texture transatlasMap {get; private set;}

        public readonly DepthPass depthPass;
        public readonly ShadowAtlasPass shadowAtlasPass;
        public readonly OpaquePass opaquePass;
        public readonly SkybackgroundPass skybackgroundPass;
        public readonly TransparencyPass transparencyPass;
        public readonly DebugPass debugPass;
        public readonly PresentPass presentPass;
        readonly FrameGraph frameGraph;

        Material debugViewMaterial;
        MaterialRenderProxy debugViewColorDownresMap;
        MaterialRenderProxy debugViewDpethDownresMap;
        MaterialRenderProxy debugViewDpethDownres2xMap;
        MaterialRenderProxy debugViewNormalMap;
        MaterialRenderProxy debugViewSpecularMap;
        MaterialRenderProxy debugSSAOMap;
        MaterialRenderProxy debugSSGIMap;
        MaterialRenderProxy debugSSRColorMap;
        MaterialRenderProxy debugSSRMaskMap;
        MaterialRenderProxy debugIndirectSpecualrMap;

        int computeSSR;
        int computeTAA;
        int computeDownsampleViewColor;
        int computeDownsampleViewDepth;
        int computeSSAO;
        int computeSSGI;
        int computeSkylightingLUT;
        int computeComposite;
        int computeDeferredProbes;
        int computeAutoExposure;
        int computeGaussianBlur;
        int computeLightScattering;
        int computeLightScatteringIntegral;

        public SceneRenderer(FrameGraph myFrameGraph, Veldrid.Swapchain mainSwapChain) {
            Instance = this;
            frameGraph = myFrameGraph;

            viewUniformBuffer = GraphicsDevice.ResourceFactory.CreateBuffer(new Veldrid.BufferDescription(ModelPassViewConstants.UniformBlockSizeInBytes, Veldrid.BufferUsage.UniformBuffer));
            shadowViewUniformBuffer = GraphicsDevice.ResourceFactory.CreateBuffer(new Veldrid.BufferDescription((uint)MathHelper.Align(Utilities.SizeOf<ShadowViewConstants>(), (int)GraphicsDevice.gd.UniformBufferMinOffsetAlignment), Veldrid.BufferUsage.UniformBuffer));
            modelMatricesBuffer = GraphicsDevice.ResourceFactory.CreateBuffer(new Veldrid.BufferDescription((uint)(MaxSkinnedMeshsOnScreen * MaxSkinnedMeshJointVectors * (Utilities.SizeOf<Vector4>())), Veldrid.BufferUsage.StructuredBufferReadOnly, (uint)Utilities.SizeOf<Vector4>()));
            instanceConstantBuffer = new Veldrid.DeviceBufferRange(frameGraph.dynamicUniformBuffer, 0, ModelPassInstanceConstants.UniformBlockSizeInBytes);
            clusterListBuffer = GraphicsDevice.ResourceFactory.CreateBuffer(new Veldrid.BufferDescription((uint)Utilities.SizeOf<ClusterData>() * ClusteredLighting.DefaultNumClusters, Veldrid.BufferUsage.StructuredBufferReadOnly, (uint)Utilities.SizeOf<ClusterData>()));
            clusterItemListBuffer = GraphicsDevice.ResourceFactory.CreateBuffer(new Veldrid.BufferDescription(sizeof(UInt32) * ClusteredLighting.DefaultNumClusters * ClusteredLighting.MaxItemsPerCluster, Veldrid.BufferUsage.StructuredBufferReadOnly, sizeof(UInt32)));
            lightListBuffer = GraphicsDevice.ResourceFactory.CreateBuffer(new Veldrid.BufferDescription((uint)Utilities.SizeOf<LightShaderParameter>() * ClusteredLighting.MaxLightsOnScreen, Veldrid.BufferUsage.StructuredBufferReadOnly, (uint)Utilities.SizeOf<LightShaderParameter>()));
            decalListBuffer = GraphicsDevice.ResourceFactory.CreateBuffer(new Veldrid.BufferDescription((uint)Utilities.SizeOf<DecalShaderParameter>() * ClusteredLighting.MaxDecalsOnScreen, Veldrid.BufferUsage.StructuredBufferReadOnly, (uint)Utilities.SizeOf<DecalShaderParameter>()));
            scatteringVolumesUniformBuffer = GraphicsDevice.ResourceFactory.CreateBuffer(new Veldrid.BufferDescription((uint)Utilities.SizeOf<ScatteringVolumeShaderParameter>() * MaxScatteringVolumesOnScreen, Veldrid.BufferUsage.UniformBuffer));
            shadowListBuffer = GraphicsDevice.ResourceFactory.CreateBuffer(new Veldrid.BufferDescription((uint)Utilities.SizeOf<ShadowShaderParameter>() * ClusteredLighting.MaxShadowsOnScreen, Veldrid.BufferUsage.StructuredBufferReadOnly, (uint)Utilities.SizeOf<ShadowShaderParameter>()));

            probeListBuffer = GraphicsDevice.ResourceFactory.CreateBuffer(new Veldrid.BufferDescription((uint)Utilities.SizeOf<LightShaderParameter>() * ClusteredLighting.MaxProbesOnScreen, Veldrid.BufferUsage.StructuredBufferReadOnly, (uint)Utilities.SizeOf<LightShaderParameter>()));
            lightsAtlasMap = GraphicsDevice.ResourceFactory.CreateTexture(new Veldrid.TextureDescription(
                2048, 1024,
                1,
                1,
                1,
                Veldrid.PixelFormat.BC4_UNorm,
                Veldrid.TextureUsage.Sampled,
                Veldrid.TextureType.Texture2D
            ));
            decalsAtlasMap = GraphicsDevice.ResourceFactory.CreateTexture(new Veldrid.TextureDescription(
                4096, 4096,
                1,
                5,
                1,
                Veldrid.PixelFormat.BC7_UNorm,
                Veldrid.TextureUsage.Sampled,
                Veldrid.TextureType.Texture2D
            ));
            var texture = GraphicsDevice.ResourceFactory.CreateTexture(new Veldrid.TextureDescription(
                8192, 8192,
                1,
                1,
                1,
                Veldrid.PixelFormat.R32_Float,
                Veldrid.TextureUsage.DepthStencil | Veldrid.TextureUsage.Sampled,
                Veldrid.TextureType.Texture2D
            ));
            shadowsAtlasMap = GraphicsDevice.ResourceFactory.CreateTextureView(texture);

            Veldrid.ResourceLayout resourceLayoutPerFrame = null;
            Veldrid.ResourceSet resourceSetPerFrame = null;

            depthPass = new DepthPass(Common.frameGraph, "DepthPass");
            {
                depthPass.AddPerFrameResource("_ViewUniforms", 0, 0, Veldrid.ShaderStages.Vertex | Veldrid.ShaderStages.Fragment, viewUniformBuffer);
                depthPass.AddPerFrameResource("_ModelMatrices", 0, 1, Veldrid.ShaderStages.Vertex, modelMatricesBuffer);
                depthPass.AddPerFrameResource("_LinearSampler", 0, 2, Veldrid.ShaderStages.Fragment, GraphicsDevice.gd.LinearSampler);
                depthPass.AddPerObjectResource("_InstanceUniforms", 1, 0, Veldrid.ShaderStages.Vertex | Veldrid.ShaderStages.Fragment, Veldrid.ResourceLayoutElementOptions.DynamicBinding, instanceConstantBuffer);
                resourceLayoutPerFrame = GraphicsDevice.ResourceFactory.CreateResourceLayout(
                    new Veldrid.ResourceLayoutDescription(
                        new Veldrid.ResourceLayoutElementDescription("_ViewUniforms", Veldrid.ResourceKind.UniformBuffer, Veldrid.ShaderStages.Vertex | Veldrid.ShaderStages.Fragment),
                        new Veldrid.ResourceLayoutElementDescription("_ModelMatrices", Veldrid.ResourceKind.StructuredBufferReadOnly, Veldrid.ShaderStages.Vertex),
                        new Veldrid.ResourceLayoutElementDescription("_LinearSampler", Veldrid.ResourceKind.Sampler, Veldrid.ShaderStages.Fragment)
                    )
                );
                resourceSetPerFrame = GraphicsDevice.ResourceFactory.CreateResourceSet(new Veldrid.ResourceSetDescription(resourceLayoutPerFrame, viewUniformBuffer, modelMatricesBuffer, GraphicsDevice.gd.LinearSampler));
                depthPass.SetPerFrameResources(resourceLayoutPerFrame, resourceSetPerFrame);
            }

            shadowAtlasPass = new ShadowAtlasPass(Common.frameGraph, "ShadowAtlasPass", shadowViewUniformBuffer, shadowsAtlasMap.Target);
            {
                shadowAtlasPass.AddPerFrameResource("_ShadowViewUniforms", 0, 0, Veldrid.ShaderStages.Vertex | Veldrid.ShaderStages.Fragment, shadowViewUniformBuffer);
                shadowAtlasPass.AddPerFrameResource("_ModelMatrices", 0, 1, Veldrid.ShaderStages.Vertex, modelMatricesBuffer);
                shadowAtlasPass.AddPerFrameResource("_LinearSampler", 0, 2, Veldrid.ShaderStages.Fragment, GraphicsDevice.gd.LinearSampler);
                shadowAtlasPass.AddPerObjectResource("_InstanceUniforms", 1, 0, Veldrid.ShaderStages.Vertex | Veldrid.ShaderStages.Fragment, Veldrid.ResourceLayoutElementOptions.DynamicBinding, instanceConstantBuffer);
                resourceLayoutPerFrame = GraphicsDevice.ResourceFactory.CreateResourceLayout(
                    new Veldrid.ResourceLayoutDescription(
                        new Veldrid.ResourceLayoutElementDescription("_ShadowViewUniforms", Veldrid.ResourceKind.UniformBuffer, Veldrid.ShaderStages.Vertex | Veldrid.ShaderStages.Fragment),
                        new Veldrid.ResourceLayoutElementDescription("_ModelMatrices", Veldrid.ResourceKind.StructuredBufferReadOnly, Veldrid.ShaderStages.Vertex),
                        new Veldrid.ResourceLayoutElementDescription("_LinearSampler", Veldrid.ResourceKind.Sampler, Veldrid.ShaderStages.Fragment)              
                    )
                );
                resourceSetPerFrame = GraphicsDevice.ResourceFactory.CreateResourceSet(new Veldrid.ResourceSetDescription(resourceLayoutPerFrame, shadowViewUniformBuffer, modelMatricesBuffer, GraphicsDevice.gd.LinearSampler));
                shadowAtlasPass.SetPerFrameResources(resourceLayoutPerFrame, resourceSetPerFrame);            
            }

            opaquePass = new OpaquePass(Common.frameGraph, "OpaquePass");
            {
                opaquePass.AddPerFrameResource("_ViewUniforms", 0, 0, Veldrid.ShaderStages.Vertex | Veldrid.ShaderStages.Fragment, viewUniformBuffer);
                opaquePass.AddPerFrameResource("_ModelMatrices", 0, 1, Veldrid.ShaderStages.Vertex, modelMatricesBuffer);
                opaquePass.AddPerFrameResource("_ClusterList", 0, 2, Veldrid.ShaderStages.Fragment, clusterListBuffer);
                opaquePass.AddPerFrameResource("_ClusterItemList", 0, 3, Veldrid.ShaderStages.Fragment, clusterItemListBuffer);
                opaquePass.AddPerFrameResource("_LightList", 0, 4, Veldrid.ShaderStages.Fragment, lightListBuffer);
                opaquePass.AddPerFrameResource("_DecalList", 0, 5, Veldrid.ShaderStages.Fragment, decalListBuffer);
                opaquePass.AddPerFrameResource("_ShadowList", 0, 6, Veldrid.ShaderStages.Fragment, shadowsAtlasMap);
                opaquePass.AddPerFrameResource("_LightsAtlas", 0, 7, Veldrid.ShaderStages.Fragment, lightsAtlasMap);
                opaquePass.AddPerFrameResource("_DecalsAtlas", 0, 8, Veldrid.ShaderStages.Fragment, decalsAtlasMap);
                opaquePass.AddPerFrameResource("_ShadowsAtlas", 0, 9, Veldrid.ShaderStages.Fragment, shadowsAtlasMap);
                opaquePass.AddPerFrameResource("_AnisoSampler", 0, 10, Veldrid.ShaderStages.Fragment, GraphicsDevice.AnisoSampler);
                opaquePass.AddPerFrameResource("_LinearClampSampler", 0, 11, Veldrid.ShaderStages.Fragment, GraphicsDevice.LinearClampSampler);
                opaquePass.AddPerFrameResource("_DecalsAtlasSampler", 0, 12, Veldrid.ShaderStages.Fragment, GraphicsDevice.gd.Aniso4xSampler);
                opaquePass.AddPerFrameResource("_ShadowsAtlasSampler", 0, 13, Veldrid.ShaderStages.Fragment, GraphicsDevice.ShadowMapSampler);
                opaquePass.AddPerObjectResource("_InstanceUniforms", 1, 0, Veldrid.ShaderStages.Vertex | Veldrid.ShaderStages.Fragment, Veldrid.ResourceLayoutElementOptions.DynamicBinding, instanceConstantBuffer);
                resourceLayoutPerFrame = GraphicsDevice.ResourceFactory.CreateResourceLayout(
                    new Veldrid.ResourceLayoutDescription(
                        new Veldrid.ResourceLayoutElementDescription("_ViewUniforms", Veldrid.ResourceKind.UniformBuffer, Veldrid.ShaderStages.Vertex | Veldrid.ShaderStages.Fragment),
                        new Veldrid.ResourceLayoutElementDescription("_ModelMatrices", Veldrid.ResourceKind.StructuredBufferReadOnly, Veldrid.ShaderStages.Vertex),
                        new Veldrid.ResourceLayoutElementDescription("_ClusterList", Veldrid.ResourceKind.StructuredBufferReadOnly, Veldrid.ShaderStages.Fragment),
                        new Veldrid.ResourceLayoutElementDescription("_ClusterItemList", Veldrid.ResourceKind.StructuredBufferReadOnly, Veldrid.ShaderStages.Fragment),
                        new Veldrid.ResourceLayoutElementDescription("_LightList", Veldrid.ResourceKind.StructuredBufferReadOnly, Veldrid.ShaderStages.Fragment),
                        new Veldrid.ResourceLayoutElementDescription("_DecalList", Veldrid.ResourceKind.StructuredBufferReadOnly, Veldrid.ShaderStages.Fragment),
                        new Veldrid.ResourceLayoutElementDescription("_ShadowList", Veldrid.ResourceKind.StructuredBufferReadOnly, Veldrid.ShaderStages.Fragment),
                        new Veldrid.ResourceLayoutElementDescription("_LightsAtlas", Veldrid.ResourceKind.TextureReadOnly, Veldrid.ShaderStages.Fragment),
                        new Veldrid.ResourceLayoutElementDescription("_DecalsAtlas", Veldrid.ResourceKind.TextureReadOnly, Veldrid.ShaderStages.Fragment),
                        new Veldrid.ResourceLayoutElementDescription("_ShadowsAtlas", Veldrid.ResourceKind.TextureReadOnly, Veldrid.ShaderStages.Fragment),
                        new Veldrid.ResourceLayoutElementDescription("_AnisoSampler", Veldrid.ResourceKind.Sampler, Veldrid.ShaderStages.Fragment),
                        new Veldrid.ResourceLayoutElementDescription("_LinearClampSampler", Veldrid.ResourceKind.Sampler, Veldrid.ShaderStages.Fragment),
                        new Veldrid.ResourceLayoutElementDescription("_DecalsAtlasSampler", Veldrid.ResourceKind.Sampler, Veldrid.ShaderStages.Fragment),
                        new Veldrid.ResourceLayoutElementDescription("_ShadowsAtlasSampler", Veldrid.ResourceKind.Sampler, Veldrid.ShaderStages.Fragment)                        
                    )
                );
                resourceSetPerFrame = GraphicsDevice.ResourceFactory.CreateResourceSet(new Veldrid.ResourceSetDescription(
                    resourceLayoutPerFrame, 
                    viewUniformBuffer, 
                    modelMatricesBuffer, 
                    clusterListBuffer,
                    clusterItemListBuffer,
                    lightListBuffer,
                    decalListBuffer,
                    shadowListBuffer,
                    lightsAtlasMap,
                    decalsAtlasMap,
                    shadowsAtlasMap,
                    GraphicsDevice.AnisoSampler,
                    GraphicsDevice.LinearClampSampler,
                    GraphicsDevice.gd.Aniso4xSampler,
                    GraphicsDevice.ShadowMapSampler));
                opaquePass.SetPerFrameResources(resourceLayoutPerFrame, resourceSetPerFrame);
            }

            skybackgroundPass = new SkybackgroundPass(Common.frameGraph, "SkybackgroundPass");
            {
                skybackgroundPass.AddPerFrameResource("_ViewUniforms", 0, 0, Veldrid.ShaderStages.Vertex | Veldrid.ShaderStages.Fragment, viewUniformBuffer);
                resourceLayoutPerFrame = GraphicsDevice.ResourceFactory.CreateResourceLayout(
                    new Veldrid.ResourceLayoutDescription(
                        new Veldrid.ResourceLayoutElementDescription("_ViewUniforms", Veldrid.ResourceKind.UniformBuffer, Veldrid.ShaderStages.Vertex | Veldrid.ShaderStages.Fragment)               
                    )
                );
                resourceSetPerFrame = GraphicsDevice.ResourceFactory.CreateResourceSet(new Veldrid.ResourceSetDescription(
                    resourceLayoutPerFrame, 
                    viewUniformBuffer));
                skybackgroundPass.SetPerFrameResources(resourceLayoutPerFrame, resourceSetPerFrame);            
            }

            transparencyPass = new TransparencyPass(Common.frameGraph, "TransparencyPass");
            {
                var transatlas = ImageManager.Image2DFromFile(FileSystem.CreateAssetOSPath("textures/effects/particles/fire_loop_small.bimg"));
                transatlasMap = transatlas.textureObject;

                transparencyPass.AddPerFrameResource("_ViewUniforms", 0, 0, Veldrid.ShaderStages.Vertex | Veldrid.ShaderStages.Fragment, viewUniformBuffer);
                transparencyPass.AddPerFrameResource("_ModelMatrices", 0, 1, Veldrid.ShaderStages.Vertex, modelMatricesBuffer);
                transparencyPass.AddPerFrameResource("_ClusterList", 0, 2, Veldrid.ShaderStages.Fragment, clusterListBuffer);
                transparencyPass.AddPerFrameResource("_ClusterItemList", 0, 3, Veldrid.ShaderStages.Fragment, clusterItemListBuffer);
                transparencyPass.AddPerFrameResource("_LightList", 0, 4, Veldrid.ShaderStages.Fragment, lightListBuffer);
                transparencyPass.AddPerFrameResource("_DecalList", 0, 5, Veldrid.ShaderStages.Fragment, decalListBuffer);
                transparencyPass.AddPerFrameResource("_ShadowList", 0, 6, Veldrid.ShaderStages.Fragment, shadowsAtlasMap);
                transparencyPass.AddPerFrameResource("_LightsAtlas", 0, 7, Veldrid.ShaderStages.Fragment, lightsAtlasMap);
                transparencyPass.AddPerFrameResource("_DecalsAtlas", 0, 8, Veldrid.ShaderStages.Fragment, decalsAtlasMap);
                transparencyPass.AddPerFrameResource("_ShadowsAtlas", 0, 9, Veldrid.ShaderStages.Fragment, shadowsAtlasMap);
                transparencyPass.AddPerFrameResource("_ViewDepthTex", 0, 10, Veldrid.ShaderStages.Fragment, frameGraph.viewDepthMap);
                transparencyPass.AddPerFrameResource("_TransatlasTex", 0, 11, Veldrid.ShaderStages.Fragment, transatlasMap);
                transparencyPass.AddPerFrameResource("_AnisoSampler", 0, 12, Veldrid.ShaderStages.Fragment, GraphicsDevice.AnisoSampler);
                transparencyPass.AddPerFrameResource("_LinearClampSampler", 0, 13, Veldrid.ShaderStages.Fragment, GraphicsDevice.LinearClampSampler);
                transparencyPass.AddPerFrameResource("_DecalsAtlasSampler", 0, 14, Veldrid.ShaderStages.Fragment, GraphicsDevice.gd.Aniso4xSampler);
                transparencyPass.AddPerFrameResource("_ShadowsAtlasSampler", 0, 15, Veldrid.ShaderStages.Fragment, GraphicsDevice.ShadowMapSampler);
                transparencyPass.AddPerObjectResource("_InstanceUniforms", 1, 0, Veldrid.ShaderStages.Vertex | Veldrid.ShaderStages.Fragment, Veldrid.ResourceLayoutElementOptions.DynamicBinding, instanceConstantBuffer);
                resourceLayoutPerFrame = GraphicsDevice.ResourceFactory.CreateResourceLayout(
                    new Veldrid.ResourceLayoutDescription(
                        new Veldrid.ResourceLayoutElementDescription("_ViewUniforms", Veldrid.ResourceKind.UniformBuffer, Veldrid.ShaderStages.Vertex | Veldrid.ShaderStages.Fragment),
                        new Veldrid.ResourceLayoutElementDescription("_ModelMatrices", Veldrid.ResourceKind.StructuredBufferReadOnly, Veldrid.ShaderStages.Vertex),
                        new Veldrid.ResourceLayoutElementDescription("_ClusterList", Veldrid.ResourceKind.StructuredBufferReadOnly, Veldrid.ShaderStages.Fragment),
                        new Veldrid.ResourceLayoutElementDescription("_ClusterItemList", Veldrid.ResourceKind.StructuredBufferReadOnly, Veldrid.ShaderStages.Fragment),
                        new Veldrid.ResourceLayoutElementDescription("_LightList", Veldrid.ResourceKind.StructuredBufferReadOnly, Veldrid.ShaderStages.Fragment),
                        new Veldrid.ResourceLayoutElementDescription("_DecalList", Veldrid.ResourceKind.StructuredBufferReadOnly, Veldrid.ShaderStages.Fragment),
                        new Veldrid.ResourceLayoutElementDescription("_ShadowList", Veldrid.ResourceKind.StructuredBufferReadOnly, Veldrid.ShaderStages.Fragment),
                        new Veldrid.ResourceLayoutElementDescription("_LightsAtlas", Veldrid.ResourceKind.TextureReadOnly, Veldrid.ShaderStages.Fragment),
                        new Veldrid.ResourceLayoutElementDescription("_DecalsAtlas", Veldrid.ResourceKind.TextureReadOnly, Veldrid.ShaderStages.Fragment),
                        new Veldrid.ResourceLayoutElementDescription("_ShadowsAtlas", Veldrid.ResourceKind.TextureReadOnly, Veldrid.ShaderStages.Fragment),
                        new Veldrid.ResourceLayoutElementDescription("_ViewDepthTex", Veldrid.ResourceKind.TextureReadOnly, Veldrid.ShaderStages.Fragment),
                        new Veldrid.ResourceLayoutElementDescription("_TransatlasTex", Veldrid.ResourceKind.TextureReadOnly, Veldrid.ShaderStages.Fragment),
                        new Veldrid.ResourceLayoutElementDescription("_AnisoSampler", Veldrid.ResourceKind.Sampler, Veldrid.ShaderStages.Fragment),
                        new Veldrid.ResourceLayoutElementDescription("_LinearClampSampler", Veldrid.ResourceKind.Sampler, Veldrid.ShaderStages.Fragment),
                        new Veldrid.ResourceLayoutElementDescription("_DecalsAtlasSampler", Veldrid.ResourceKind.Sampler, Veldrid.ShaderStages.Fragment),
                        new Veldrid.ResourceLayoutElementDescription("_ShadowsAtlasSampler", Veldrid.ResourceKind.Sampler, Veldrid.ShaderStages.Fragment)                        
                    )
                );
                resourceSetPerFrame = GraphicsDevice.ResourceFactory.CreateResourceSet(new Veldrid.ResourceSetDescription(
                    resourceLayoutPerFrame, 
                    viewUniformBuffer, 
                    modelMatricesBuffer, 
                    clusterListBuffer,
                    clusterItemListBuffer,
                    lightListBuffer,
                    decalListBuffer,
                    shadowListBuffer,
                    lightsAtlasMap,
                    decalsAtlasMap,
                    shadowsAtlasMap,
                    frameGraph.viewDepthMap,
                    transatlasMap,
                    GraphicsDevice.AnisoSampler,
                    GraphicsDevice.LinearClampSampler,
                    GraphicsDevice.gd.Aniso4xSampler,
                    GraphicsDevice.ShadowMapSampler));
                transparencyPass.SetPerFrameResources(resourceLayoutPerFrame, resourceSetPerFrame);
            }

            debugPass = new DebugPass(frameGraph, "DebugPass");
            {
                debugPass.AddPerFrameResource("_ViewUniforms", 0, 0, Veldrid.ShaderStages.Vertex | Veldrid.ShaderStages.Fragment, viewUniformBuffer);
                resourceLayoutPerFrame = GraphicsDevice.ResourceFactory.CreateResourceLayout(
                    new Veldrid.ResourceLayoutDescription(
                        new Veldrid.ResourceLayoutElementDescription("_ViewUniforms", Veldrid.ResourceKind.UniformBuffer, Veldrid.ShaderStages.Vertex | Veldrid.ShaderStages.Fragment)
                    )
                );
                resourceSetPerFrame = GraphicsDevice.ResourceFactory.CreateResourceSet(new Veldrid.ResourceSetDescription(resourceLayoutPerFrame, viewUniformBuffer));
                debugPass.SetPerFrameResources(resourceLayoutPerFrame, resourceSetPerFrame);
            }

            presentPass = new PresentPass(Common.frameGraph, mainSwapChain, "PresentPass");
            {
                presentPass.AddPerFrameResource("_ViewUniforms", 0, 0, Veldrid.ShaderStages.Vertex | Veldrid.ShaderStages.Fragment, viewUniformBuffer);
                resourceLayoutPerFrame = GraphicsDevice.ResourceFactory.CreateResourceLayout(
                    new Veldrid.ResourceLayoutDescription(
                        new Veldrid.ResourceLayoutElementDescription("_ViewUniforms", Veldrid.ResourceKind.UniformBuffer, Veldrid.ShaderStages.Vertex | Veldrid.ShaderStages.Fragment)
                    )
                );  
                presentPass.SetPerFrameResources(resourceLayoutPerFrame, resourceSetPerFrame);              
            }

            frameGraph.AddRenderPass(depthPass);
            frameGraph.AddRenderPass(shadowAtlasPass);
            frameGraph.AddRenderPass(opaquePass);
            frameGraph.AddRenderPass(skybackgroundPass);
            frameGraph.AddRenderPass(transparencyPass);
            frameGraph.AddRenderPass(debugPass);
            frameGraph.AddRenderPass(presentPass);
        }

        public void Init() {
            computeDownsampleViewColor = frameGraph.builtinComputeShader.FindKernel("ComputeDownsample");
            computeDownsampleViewDepth = frameGraph.builtinComputeShader.FindKernel("ComputeDownsampleDepth");
            computeSSAO = frameGraph.builtinComputeShader.FindKernel("ComputeSSAO");
            computeSSGI = frameGraph.builtinComputeShader.FindKernel("ComputeSSGI");
            computeSkylightingLUT = frameGraph.skylightingComputeShader.FindKernel("ComputeSkylightingLUT");
            computeComposite = frameGraph.compositeComputeShader.FindKernel("main");
            computeDeferredProbes = frameGraph.deferredProbesComputeShader.FindKernel("ComputeDeferredProbes");
            computeAutoExposure = frameGraph.builtinComputeShader.FindKernel("ComputeAutoExposure");
            computeGaussianBlur = frameGraph.builtinComputeShader.FindKernel("ComputeGaussianBlur");
            computeLightScattering = frameGraph.volumetricLightingComputeShader.FindKernel("ComputeLightScattering");
            computeLightScatteringIntegral = frameGraph.volumetricLightingComputeShader.FindKernel("ComputeLightScatteringIntegral");
            computeTAA = frameGraph.taaComputeShader.FindKernel("main");
            computeSSR = frameGraph.ssrComputeShader.FindKernel("main");

            debugViewMaterial = new Material(Common.declManager.FindShader("builtin/ScreenPic"));
            ImageWraper viewColorDownres32xMap = new ImageWraper(frameGraph.viewColorDownresMaps[frameGraph.viewColorDownresMaps.Length - 1].Target);
            ImageWraper viewDepthMap = new ImageWraper(frameGraph.viewDepthMap.Target);
            ImageWraper viewDepthDownres32xMap = new ImageWraper(frameGraph.viewDepthDownresMaps[frameGraph.viewDepthDownresMaps.Length - 1].Target);
            ImageWraper viewNormalMap = new ImageWraper(frameGraph.viewNormalMap.Target);
            ImageWraper viewSpecularMap = new ImageWraper(frameGraph.viewSpecularMap.Target);
            ImageWraper ssaoMap = new ImageWraper(frameGraph.ssaoOutput.Target);
            ImageWraper ssgiMap = new ImageWraper(frameGraph.ssgiOutput.Target);
            ImageWraper indirectSpecularMap = new ImageWraper(frameGraph.indirectSpecularOutput.Target);
            ImageWraper ssrColorMap = new ImageWraper(frameGraph.ssrColorMap.Target);
            ImageWraper ssrMaskMap = new ImageWraper(frameGraph.ssrMaskMap.Target);
            debugViewMaterial.SetMainImage(viewColorDownres32xMap);
            debugViewMaterial.SetSampler("_LinearClampSampler", GraphicsDevice.LinearClampSampler);
            debugViewColorDownresMap = debugViewMaterial.MakeLightingProxy();  
            debugViewMaterial.SetMainImage(viewDepthMap);
            debugViewDpethDownresMap = debugViewMaterial.MakeLightingProxy();
            debugViewMaterial.SetMainImage(viewDepthDownres32xMap);
            debugViewDpethDownres2xMap = debugViewMaterial.MakeLightingProxy();
            debugViewMaterial.SetMainImage(viewNormalMap);
            debugViewNormalMap = debugViewMaterial.MakeLightingProxy();
            debugViewMaterial.SetMainImage(viewSpecularMap);
            debugViewSpecularMap = debugViewMaterial.MakeLightingProxy();
            debugViewMaterial.SetMainImage(ssaoMap);
            debugSSAOMap = debugViewMaterial.MakeLightingProxy();
            debugViewMaterial.SetMainImage(ssgiMap);
            debugSSGIMap = debugViewMaterial.MakeLightingProxy();
            debugViewMaterial.SetMainImage(indirectSpecularMap);
            debugIndirectSpecualrMap = debugViewMaterial.MakeLightingProxy();
            debugViewMaterial.SetMainImage(ssrColorMap);
            debugSSRColorMap = debugViewMaterial.MakeLightingProxy();
            debugViewMaterial.SetMainImage(ssrMaskMap);
            debugSSRMaskMap = debugViewMaterial.MakeLightingProxy();            
        }

        public void Dispose() {
        }

        Vector4 GetTexelSize(Veldrid.Texture texture) {
            return new Vector4(texture.Width, texture.Height, 1f / texture.Width, 1f / texture.Height);
        }

        Vector4 GetTexelSize(Veldrid.TextureView view) {
            return new Vector4(view.Target.Width, view.Target.Height, 1f / view.Target.Width, 1f / view.Target.Height);
        }

        Vector4 GetTexelSize3D(Veldrid.TextureView view) {
            return new Vector4(1f / view.Target.Width, 1f / view.Target.Height, 1f / view.Target.Depth, 0f);
        }

        readonly Vector4[] ssaoParms = {
            new Vector4(0.35f, 0.35f, 0.99812f, 0.06129f),
            new Vector4(-0.19052f, 0.19052f, 0.96698f, 0.25484f),
            new Vector4(0.12374f, -0.12374f, 0.89869f, 0.43859f),
            new Vector4(-0.08854f, -0.08854f, 0.79585f, 0.60549f),
            new Vector4(0.09526f, 0.00f, 0.66244f, 0.74912f),
            new Vector4(-0.07559f, 0.00f, 0.50356f, 0.86396f),
            new Vector4(0.00f, 0.06187f, 0.32534f, 0.9456f),
            new Vector4(0.00f, -0.05185f, 0.13461f, 0.9909f),
            new Vector4(0.01694f, 0.0409f, -0.06129f, 0.99812f),
            new Vector4(-0.01469f, 0.03545f, -0.25484f, 0.96698f),
            new Vector4(0.01289f, -0.03112f, -0.43859f, 0.89869f),
            new Vector4(-0.01143f, -0.02759f, -0.60549f, 0.79585f),
            new Vector4(0.02609f, 0.00578f, -0.74912f, 0.66244f),
            new Vector4(-0.02353f, 0.00522f, -0.86396f, 0.50356f),
            new Vector4(0.02136f, -0.00473f, -0.9456f, 0.32534f),
            new Vector4(-0.0195f, -0.00432f, -0.9909f, 0.13461f)
        };

        public void ComputeSSAO(CommandBuffer commandBuffer) {
            Veldrid.CommandList commandList = commandBuffer.commandList;
            commandBuffer.context1.OutputMerger.SetRenderTargets((SharpDX.Direct3D11.DepthStencilView)null, null, null, null);  
            uint[] constantOffsets = new uint[3];
            frameGraph.AllocConstants(commandList, 16, out var constants, out constantOffsets[0]);
            constants[0] = GetTexelSize(frameGraph.ssaoOutput);
            constants[1] = GetTexelSize(frameGraph.ssaoOutput);
            constants[2] = Vector4.Zero;
            constants[3] = Vector4.Zero;
            constants[4] = new Vector4(Time.frameCount, 0, 100f, 0);
            frameGraph.UpdateConstants(commandList, fxConstants, out constantOffsets[1]);
            frameGraph.AllocConstants(commandList, 16, out constants, out constantOffsets[2]);
            ssaoParms.CopyTo(constants);
            frameGraph.builtinComputeShader.SetConstantBuffer(computeSSAO, 0, new Veldrid.DeviceBufferRange(frameGraph.dynamicUniformBuffer, 0, 256));
            frameGraph.builtinComputeShader.SetConstantBuffer(computeSSAO, 1, new Veldrid.DeviceBufferRange(frameGraph.dynamicUniformBuffer, 0, 512));
            frameGraph.builtinComputeShader.SetConstantBuffer(computeSSAO, 2, new Veldrid.DeviceBufferRange(frameGraph.dynamicUniformBuffer, 0, 256));
            frameGraph.builtinComputeShader.SetSampler(computeSSAO, 3, GraphicsDevice.PointClampSampler);
            frameGraph.builtinComputeShader.SetTexture(computeSSAO, 4, frameGraph.viewDepthDownresMaps[0]);
            frameGraph.builtinComputeShader.SetTexture(computeSSAO, 5, frameGraph.viewDepthDownresMaps[1]);
            frameGraph.builtinComputeShader.SetTexture(computeSSAO, 6, frameGraph.viewNormalMap);
            frameGraph.builtinComputeShader.SetTexture(computeSSAO, 7, frameGraph.viewSpecularMap);
            frameGraph.builtinComputeShader.SetTexture(computeSSAO, 8, frameGraph.ssaoOutput);      
            commandBuffer.context1.ComputeShader.SetSampler(1, GraphicsDevice._PointClampSampler);      
            frameGraph.builtinComputeShader.Dispatch(commandList, computeSSAO, constantOffsets, (uint)frameGraph.ssaoOutput.Target.Width, (uint)frameGraph.ssaoOutput.Target.Height, 1);
            //commandBuffer.context1.ComputeShader.SetShaderResources(0, null, null, null, null);
            //commandBuffer.context1.ComputeShader.SetUnorderedAccessView(0, null);                  
        }

        public void ComputeSSGI(CommandBuffer commandBuffer) {
            Veldrid.CommandList commandList = commandBuffer.commandList;
            commandBuffer.context1.OutputMerger.SetRenderTargets((SharpDX.Direct3D11.DepthStencilView)null, null, null, null);  
            uint[] constantOffsets = new uint[3];
            frameGraph.AllocConstants(commandList, 16, out var constants, out constantOffsets[0]);
            constants[0] = GetTexelSize(frameGraph.ssaoOutput);
            constants[1] = GetTexelSize(frameGraph.ssaoOutput);
            constants[2] = Vector4.Zero;
            constants[3] = Vector4.Zero;
            constants[4] = new Vector4(Time.frameCount, 0, 100f, 0);
            frameGraph.UpdateConstants(commandList, fxConstants, out constantOffsets[1]);
            frameGraph.AllocConstants(commandList, 16, out constants, out constantOffsets[2]);
            ssaoParms.CopyTo(constants);
            frameGraph.builtinComputeShader.SetConstantBuffer(computeSSGI, 0, new Veldrid.DeviceBufferRange(frameGraph.dynamicUniformBuffer, 0, 256));
            frameGraph.builtinComputeShader.SetConstantBuffer(computeSSGI, 1, new Veldrid.DeviceBufferRange(frameGraph.dynamicUniformBuffer, 0, 512));
            frameGraph.builtinComputeShader.SetConstantBuffer(computeSSGI, 2, new Veldrid.DeviceBufferRange(frameGraph.dynamicUniformBuffer, 0, 256));
            frameGraph.builtinComputeShader.SetSampler(computeSSGI, 3, GraphicsDevice.PointClampSampler);
            frameGraph.builtinComputeShader.SetTexture(computeSSGI, 4, frameGraph.viewDepthDownresMaps[0]);
            frameGraph.builtinComputeShader.SetTexture(computeSSGI, 5, frameGraph.viewDepthDownresMaps[1]);
            frameGraph.builtinComputeShader.SetTexture(computeSSGI, 6, frameGraph.viewNormalMap);
            frameGraph.builtinComputeShader.SetTexture(computeSSGI, 7, frameGraph.viewOpaqueColorMap);
            frameGraph.builtinComputeShader.SetTexture(computeSSGI, 8, frameGraph.ssgiOutput);      
            commandBuffer.context1.ComputeShader.SetSampler(1, GraphicsDevice._PointClampSampler);      
            frameGraph.builtinComputeShader.Dispatch(commandList, computeSSGI, constantOffsets, (uint)frameGraph.ssgiOutput.Target.Width, (uint)frameGraph.ssgiOutput.Target.Height, 1);
            commandBuffer.context1.ComputeShader.SetShaderResources(0, null, null, null, null);
            commandBuffer.context1.ComputeShader.SetUnorderedAccessView(0, null);                  
        }

        void ComputeSkylightingLUT(CommandBuffer commandBuffer, SceneRenderData renderData) {
            Skylighting skylighting = new Skylighting();
            skylighting.texelSize = GetTexelSize3D(frameGraph.skylightingLUT);
            skylighting.zNearFar = new Vector4(renderData.fxRenderData.viewMinDistance, renderData.fxRenderData.viewMaxDistance, 0f, 0.00013f);
            skylighting.globalViewOrigin = fxConstants.globalViewOrigin;
            skylighting.radiusPlanet = new Vector4(renderData.fxRenderData.radiusPlanet);
            skylighting.radiusAtmosphere = new Vector4(renderData.fxRenderData.radiusAtmosphere);
            skylighting.rayleighColor = new Vector4(renderData.fxRenderData.rayleighColor, 1f);
            skylighting.mieColor = new Vector4(renderData.fxRenderData.mieColor, 1f);
            skylighting.mieDensityScale = new Vector4(renderData.fxRenderData.mieDensityScale);
            skylighting.rayleighHeight = new Vector4(renderData.fxRenderData.rayleighHeight);
            skylighting.mieHeight = new Vector4(renderData.fxRenderData.mieHeight);
            skylighting.mieAnisotropy = new Vector4(renderData.fxRenderData.mieAnisotropy);
            skylighting.sunLatitude = new Vector4(-0.527f, -0.527f, -0.527f, -0.527f);
            skylighting.sunLongitude = new Vector4(0.128f, 0.128f, 0.128f, 0.128f);
            skylighting.scatteringDensityScale = new Vector4(renderData.fxRenderData.scatteringDensityScale);
            skylighting.sunIntensity = new Vector4(renderData.fxRenderData.sunIntensity);
            skylighting.decoupleSunColorFromSky = new Vector4(renderData.fxRenderData.decoupleSunColorFromSky ? 1f : 0f);
            skylighting.sunColor = new Vector4(renderData.fxRenderData.sunColor, 1f);
            skylighting.sunForward = fxConstants.sunForward;
            Veldrid.CommandList commandList = commandBuffer.commandList;
            frameGraph.UpdateConstants(commandList, skylighting, out uint constantOffset);
            frameGraph.skylightingComputeShader.SetConstantBuffer(computeSkylightingLUT, 0, new Veldrid.DeviceBufferRange(frameGraph.dynamicUniformBuffer, 0, 512));
            frameGraph.skylightingComputeShader.SetTexture(computeSkylightingLUT, 1, frameGraph.skylightingLUT.Target);
            frameGraph.skylightingComputeShader.Dispatch(commandList, computeSkylightingLUT, new [] {constantOffset}, frameGraph.skylightingLUT.Target.Width, frameGraph.skylightingLUT.Target.Height, frameGraph.skylightingLUT.Target.Depth);
        }

        void ComputeLightScattering(CommandBuffer commandBuffer, SceneRenderData renderData, int currentFrame) {
            const int numFrames = 2;
            int historyMap = (1 - currentFrame) * numFrames;
            int currentMap = currentFrame * numFrames;
            var prevLightScatteringPackedMap0 = frameGraph.lightScatteringPackedMaps[historyMap];
            var prevLightScatteringPackedMap1 = frameGraph.lightScatteringPackedMaps[historyMap + 1];
            var lightScatteringPackedMap0 = frameGraph.lightScatteringPackedMaps[currentMap];
            var lightScatteringPackedMap1 = frameGraph.lightScatteringPackedMaps[currentMap + 1];

            Veldrid.CommandList commandList = commandBuffer.commandList;   
            uint[] constantOffsets = new uint[2];
            frameGraph.UpdateConstants(commandList, fxConstants, out constantOffsets[0]);
            frameGraph.AllocConstants(commandList, 16, out var constants, out constantOffsets[1]);
            constants[0] = new Vector4(lightScatteringPackedMap0.Target.Width, lightScatteringPackedMap0.Target.Height, lightScatteringPackedMap0.Target.Depth, 0f);
            constants[1] = new Vector4(0.1f, renderData.fxRenderData.volumetricLightingDistance, 0f, 0f);
            constants[2] = new Vector4(0.5f, 0f, 0f, renderData.viewDefs[0].numScatteringVolumes);
            frameGraph.volumetricLightingComputeShader.SetConstantBuffer(computeLightScattering, 0, new Veldrid.DeviceBufferRange(frameGraph.dynamicUniformBuffer, 0, 512));
            frameGraph.volumetricLightingComputeShader.SetConstantBuffer(computeLightScattering, 1, new Veldrid.DeviceBufferRange(frameGraph.dynamicUniformBuffer, 0, 256));
            frameGraph.volumetricLightingComputeShader.SetConstantBuffer(computeLightScattering, 2, scatteringVolumesUniformBuffer);
            frameGraph.volumetricLightingComputeShader.SetSampler(computeLightScattering, 3, GraphicsDevice.ShadowMapSampler);
            frameGraph.volumetricLightingComputeShader.SetSampler(computeLightScattering, 4, GraphicsDevice.LinearClampSampler);
            //frameGraph.volumetricLightingComputeShader.SetSampler(computeLightScattering, 5, GraphicsDevice.gd.LinearSampler);
            frameGraph.volumetricLightingComputeShader.SetBuffer(computeLightScattering, 5, clusterListBuffer);
            frameGraph.volumetricLightingComputeShader.SetBuffer(computeLightScattering, 6, clusterItemListBuffer);
            frameGraph.volumetricLightingComputeShader.SetBuffer(computeLightScattering, 7, lightListBuffer);
            frameGraph.volumetricLightingComputeShader.SetBuffer(computeLightScattering, 8, shadowListBuffer);
            frameGraph.volumetricLightingComputeShader.SetTexture(computeLightScattering, 9, lightsAtlasMap);            
            frameGraph.volumetricLightingComputeShader.SetTexture(computeLightScattering, 10, shadowsAtlasMap);
            frameGraph.volumetricLightingComputeShader.SetTexture(computeLightScattering, 11, ImageManager.blueNoise_512x512.textureObject);
            frameGraph.volumetricLightingComputeShader.SetTexture(computeLightScattering, 12, ImageManager.valueNoise_256x256.textureObject);
            frameGraph.volumetricLightingComputeShader.SetTexture(computeLightScattering, 13, prevLightScatteringPackedMap0);
            frameGraph.volumetricLightingComputeShader.SetTexture(computeLightScattering, 14, prevLightScatteringPackedMap1); 
            frameGraph.volumetricLightingComputeShader.SetTexture(computeLightScattering, 15, frameGraph.viewDepthDownresMaps[frameGraph.viewColorDownresMaps.Length - 2]);                
            frameGraph.volumetricLightingComputeShader.SetTexture(computeLightScattering, 16, lightScatteringPackedMap0);
            frameGraph.volumetricLightingComputeShader.SetTexture(computeLightScattering, 17, lightScatteringPackedMap1);            
            commandBuffer.context1.ComputeShader.SetSamplers(0, GraphicsDevice._ShadowMapSampler, GraphicsDevice._LinearClampSampler/*, GraphicsDevice._LinearSampler*/);
            frameGraph.volumetricLightingComputeShader.Dispatch(commandList, computeLightScattering, constantOffsets, lightScatteringPackedMap0.Target.Width, lightScatteringPackedMap0.Target.Height, lightScatteringPackedMap0.Target.Depth);                          
        }

        void ComputeLightScatteringIntegral(CommandBuffer commandBuffer, SceneRenderData renderData, int currentFrame) {
            const int numFrames = 2;
            int currentMap = currentFrame * numFrames;
            var lightScatteringPackedMap0 = frameGraph.lightScatteringPackedMaps[currentMap];
            var lightScatteringPackedMap1 = frameGraph.lightScatteringPackedMaps[currentMap + 1];
            var finalLightScatteringPackedMap0 = frameGraph.finalLightScatteringPackedMaps[0];
            var finalLightScatteringPackedMap1 = frameGraph.finalLightScatteringPackedMaps[1];

            Veldrid.CommandList commandList = commandBuffer.commandList;   
            uint[] constantOffsets = new uint[2];
            frameGraph.UpdateConstants(commandList, fxConstants, out constantOffsets[0]);
            frameGraph.AllocConstants(commandList, 16, out var constants, out constantOffsets[1]);
            constants[0] = new Vector4(lightScatteringPackedMap0.Target.Width, lightScatteringPackedMap0.Target.Height, lightScatteringPackedMap0.Target.Depth, 0f);
            constants[1] = new Vector4(0.1f, renderData.fxRenderData.volumetricLightingDistance, 0f, 0f);
            frameGraph.volumetricLightingComputeShader.SetConstantBuffer(computeLightScatteringIntegral, 0, new Veldrid.DeviceBufferRange(frameGraph.dynamicUniformBuffer, 0, 512));
            frameGraph.volumetricLightingComputeShader.SetConstantBuffer(computeLightScatteringIntegral, 1, new Veldrid.DeviceBufferRange(frameGraph.dynamicUniformBuffer, 0, 256));
            frameGraph.volumetricLightingComputeShader.SetTexture(computeLightScatteringIntegral, 2, frameGraph.viewDepthDownresMaps[frameGraph.viewDepthDownresMaps.Length - 2]);
            frameGraph.volumetricLightingComputeShader.SetTexture(computeLightScatteringIntegral, 3, lightScatteringPackedMap0);
            frameGraph.volumetricLightingComputeShader.SetTexture(computeLightScatteringIntegral, 4, lightScatteringPackedMap1);
            frameGraph.volumetricLightingComputeShader.SetTexture(computeLightScatteringIntegral, 5, finalLightScatteringPackedMap0);
            frameGraph.volumetricLightingComputeShader.SetTexture(computeLightScatteringIntegral, 6, finalLightScatteringPackedMap1);
            frameGraph.volumetricLightingComputeShader.Dispatch(commandList, computeLightScatteringIntegral, constantOffsets, finalLightScatteringPackedMap0.Target.Width, finalLightScatteringPackedMap0.Target.Height, finalLightScatteringPackedMap0.Target.Depth);                          
        }

        void ComputeComposite(CommandBuffer commandBuffer, SceneRenderData renderData) {
            var finalLightScatteringPackedMap0 = frameGraph.finalLightScatteringPackedMaps[0];
            var finalLightScatteringPackedMap1 = frameGraph.finalLightScatteringPackedMaps[1];

            Veldrid.CommandList commandList = commandBuffer.commandList;
            uint[] constantOffsets = new uint[2];
            frameGraph.AllocConstants(commandList, 16, out var constants, out constantOffsets[0]);
            constants[0] = GetTexelSize(frameGraph.compositeOutputMap);
            constants[1] = GetTexelSize(frameGraph.compositeOutputMap); 

            //float4 _SSAOParms;
            //float4 _ZNearFar;
            //float4 _SunLatitude;
            //float4 _SunLongitude;
            //float4 _SunDiskScale;
            //float4 _SunColor;
            //float4 _SunIntensity;
            //float4 _DecoupleSunColorFromSky;
            constants[2] = Vector4.Zero;
            constants[3] = new Vector4(0.06f, 8000f, 0f, 0.00013f);
            constants[4] = new Vector4(-0.527f, -0.527f, -0.527f, -0.527f);
            constants[5] = new Vector4(0.128f, 0.128f, 0.128f, 0.128f);
            constants[4] = new Vector4(-0.2f, -0.2f, -0.2f, -0.2f);
            constants[5] = new Vector4(0.9f, 0.9f, 0.9f, 0.9f);
            constants[6] = new Vector4(renderData.fxRenderData.sunDiskScale);
            constants[7] = new Vector4(renderData.fxRenderData.sunColor, 0.00f);
            constants[8] = new Vector4(renderData.fxRenderData.sunIntensity);
            constants[9] = new Vector4(renderData.fxRenderData.decoupleSunColorFromSky ? 1f : 0f);
            constants[10] = new Vector4(finalLightScatteringPackedMap0.Target.Width, finalLightScatteringPackedMap0.Target.Height, finalLightScatteringPackedMap0.Target.Depth, 0f);
            constants[11] = new Vector4(0.1f, renderData.fxRenderData.volumetricLightingDistance, 0f, 0f);
            constants[12] = new Vector4(renderData.fxRenderData.scatteringFlags);
            constants[13] = new Vector4(renderData.fxRenderData.ssrMode);
                   
            frameGraph.UpdateConstants(commandList, fxConstants, out constantOffsets[1]);
            frameGraph.compositeComputeShader.SetConstantBuffer(computeComposite, 0, new Veldrid.DeviceBufferRange(frameGraph.dynamicUniformBuffer, 0, 256));
            frameGraph.compositeComputeShader.SetConstantBuffer(computeComposite, 1, new Veldrid.DeviceBufferRange(frameGraph.dynamicUniformBuffer, 0, 512));
            frameGraph.compositeComputeShader.SetSampler(computeComposite, 2, GraphicsDevice.PointClampSampler);
            frameGraph.compositeComputeShader.SetSampler(computeComposite, 3, GraphicsDevice.gd.LinearSampler);
            frameGraph.compositeComputeShader.SetSampler(computeComposite, 4, GraphicsDevice.LinearClampSampler);
            frameGraph.compositeComputeShader.SetTexture(computeComposite, 5, frameGraph.viewOpaqueColorMap);
            frameGraph.compositeComputeShader.SetTexture(computeComposite, 6, frameGraph.viewDepthMap);
            frameGraph.compositeComputeShader.SetTexture(computeComposite, 7, frameGraph.viewDepthDownresMaps[0]);
            frameGraph.compositeComputeShader.SetTexture(computeComposite, 8, frameGraph.ssaoOutput);
            frameGraph.compositeComputeShader.SetTexture(computeComposite, 9, frameGraph.skylightingLUT);
            frameGraph.compositeComputeShader.SetTexture(computeComposite, 10, frameGraph.indirectSpecularOutput);
            frameGraph.compositeComputeShader.SetTexture(computeComposite, 11, finalLightScatteringPackedMap0);
            frameGraph.compositeComputeShader.SetTexture(computeComposite, 12, finalLightScatteringPackedMap1);
            frameGraph.compositeComputeShader.SetTexture(computeComposite, 13, frameGraph.ssrColorMap);
            frameGraph.compositeComputeShader.SetTexture(computeComposite, 14, frameGraph.ssrMaskMap);            
            frameGraph.compositeComputeShader.SetTexture(computeComposite, 15, frameGraph.compositeOutputMap);
            commandBuffer.context1.ComputeShader.SetSamplers(0, GraphicsDevice._PointClampSampler, GraphicsDevice._LinearSampler, GraphicsDevice._LinearClampSampler);
            frameGraph.compositeComputeShader.Dispatch(commandList, computeComposite, constantOffsets, frameGraph.compositeOutputMap.Target.Width, frameGraph.compositeOutputMap.Target.Height, 1);   
            commandBuffer.context1.ComputeShader.SetShaderResources(0, null, null, null, null, null, null, null, null, null, null);
            commandBuffer.context1.ComputeShader.SetUnorderedAccessView(0, null);
        }

        void ComputeDeferredProbes(CommandBuffer commandBuffer, SceneRenderData renderData) {
            if(renderData.fxRenderData.reflectionProbeCubemaps == null || renderData.fxRenderData.reflectionProbeCubemaps.textureObject == null) {
                return;
            }
            Veldrid.CommandList commandList = commandBuffer.commandList;      
            frameGraph.AllocConstants(commandList, 16, out var constants, out uint constantOffset);
            constants[0] = fxConstants.globalViewOrigin;
            constants[1] = fxConstants.frustumVectorLT;
            constants[2] = fxConstants.frustumVectorRT;
            constants[3] = fxConstants.frustumVectorLB;
            constants[4] = fxConstants.frustumVectorRB;
            constants[5] = fxConstants.depthBufferParms;
            constants[6] = fxConstants.screenParms;
            constants[7] = fxConstants.clusteredLightingSize;
            constants[8] = fxConstants.clusteredLightingParms;
            frameGraph.deferredProbesComputeShader.SetConstantBuffer(computeDeferredProbes, 0, new Veldrid.DeviceBufferRange(frameGraph.dynamicUniformBuffer, 0, 256));
            frameGraph.deferredProbesComputeShader.SetSampler(computeDeferredProbes, 1, GraphicsDevice.PointClampSampler);
            frameGraph.deferredProbesComputeShader.SetSampler(computeDeferredProbes, 2, GraphicsDevice.AnisoSampler);
            frameGraph.deferredProbesComputeShader.SetTexture(computeDeferredProbes, 3, frameGraph.viewSpecularMap);
            frameGraph.deferredProbesComputeShader.SetTexture(computeDeferredProbes, 4, frameGraph.viewDepthMap);
            frameGraph.deferredProbesComputeShader.SetBuffer(computeDeferredProbes, 5, clusterListBuffer);
            frameGraph.deferredProbesComputeShader.SetBuffer(computeDeferredProbes, 6, clusterItemListBuffer);
            frameGraph.deferredProbesComputeShader.SetBuffer(computeDeferredProbes, 7, probeListBuffer);
            frameGraph.deferredProbesComputeShader.SetTexture(computeDeferredProbes, 8, frameGraph.viewNormalMap);
            frameGraph.deferredProbesComputeShader.SetTexture(computeDeferredProbes, 9, renderData.fxRenderData.reflectionProbeCubemaps.textureObject);
            frameGraph.deferredProbesComputeShader.SetTexture(computeDeferredProbes, 10, frameGraph.indirectSpecularOutput);
            commandBuffer.context1.ComputeShader.SetSamplers(0, GraphicsDevice._PointClampSampler, GraphicsDevice._Aniso16xSampler);
            frameGraph.deferredProbesComputeShader.Dispatch(commandList, computeDeferredProbes, new [] {constantOffset}, frameGraph.indirectSpecularOutput.Target.Width, frameGraph.indirectSpecularOutput.Target.Height, 1);
            commandBuffer.context1.ComputeShader.SetShaderResources(0, null, null, null, null, null, null, null, null);
            commandBuffer.context1.ComputeShader.SetUnorderedAccessView(0, null);
        }

        public void ComputeDownsampleViewDepths(CommandBuffer commandBuffer) {
            Veldrid.CommandList commandList = commandBuffer.commandList;
            commandBuffer.context1.OutputMerger.SetRenderTargets((SharpDX.Direct3D11.DepthStencilView)null);      
            for(int i = 0; i < frameGraph.viewDepthDownresMaps.Length; i++) {
                Veldrid.TextureView prev = i == 0 ? frameGraph.viewDepthMap : frameGraph.viewDepthDownresMaps[i - 1];
                frameGraph.AllocConstants(commandList, 8, out var constants, out uint constantOffset);
                constants[0] = GetTexelSize(frameGraph.viewDepthDownresMaps[i]);
                constants[1] = GetTexelSize(prev);
                constants[2] = new Vector4(0f, 0f, 0f, i > 2 ? 2f : 0f);
                constants[3] = Vector4.Zero;
                frameGraph.builtinComputeShader.SetConstantBuffer(computeDownsampleViewDepth, 0, new Veldrid.DeviceBufferRange(frameGraph.dynamicUniformBuffer, 0, 256));
                frameGraph.builtinComputeShader.SetTexture(computeDownsampleViewDepth, 1, prev);
                frameGraph.builtinComputeShader.SetTexture(computeDownsampleViewDepth, 2, frameGraph.viewDepthDownresMaps[i]);
                frameGraph.builtinComputeShader.Dispatch(commandList, computeDownsampleViewDepth, new [] {constantOffset}, (uint)frameGraph.viewDepthDownresMaps[i].Target.Width, (uint)frameGraph.viewDepthDownresMaps[i].Target.Height, 1);
                commandBuffer.context1.ComputeShader.SetShaderResource(0, null);
                commandBuffer.context1.ComputeShader.SetUnorderedAccessView(0, null);     
            }      
        }

        public void ComputeDownsampleViewColors(CommandBuffer commandBuffer, Vector4 firstParms, Vector4 parms, Veldrid.TextureView colorMap, Veldrid.TextureView[] colorDownresMaps, int start = 0) {
            Veldrid.CommandList commandList = commandBuffer.commandList;
            commandBuffer.context1.ComputeShader.SetSampler(0, GraphicsDevice._LinearClampSampler);
            for(int i = start; i < colorDownresMaps.Length; i++) {
                Veldrid.TextureView prev = i == start ? colorMap : colorDownresMaps[i - 1];
                Veldrid.TextureView downres = colorDownresMaps[i];
                ComputeDownsampleViewColor(commandBuffer, prev, i == 0 ? firstParms : parms, downres);
            }
        }

        void ComputeDownsampleViewColor(CommandBuffer commandBuffer, Veldrid.TextureView prevMap, Vector4 parms, Veldrid.TextureView resultMap) {
            Veldrid.CommandList commandList = commandBuffer.commandList;
            frameGraph.AllocConstants(commandList, 8, out var constants, out uint constantOffset);
            constants[0] = GetTexelSize(resultMap);
            constants[1] = GetTexelSize(prevMap);
            constants[2] = parms;
            constants[3] = Vector4.Zero;
            frameGraph.builtinComputeShader.SetConstantBuffer(computeDownsampleViewColor, 0, new Veldrid.DeviceBufferRange(frameGraph.dynamicUniformBuffer, 0, 256));
            frameGraph.builtinComputeShader.SetSampler(computeDownsampleViewColor, 1, GraphicsDevice.gd.PointSampler);
            frameGraph.builtinComputeShader.SetTexture(computeDownsampleViewColor, 2, prevMap);
            frameGraph.builtinComputeShader.SetTexture(computeDownsampleViewColor, 3, resultMap);
            frameGraph.builtinComputeShader.Dispatch(commandList, computeDownsampleViewColor, new [] {constantOffset}, (uint)resultMap.Target.Width, (uint)resultMap.Target.Height, 1);
            commandBuffer.context1.ComputeShader.SetShaderResource(0, null);
            commandBuffer.context1.ComputeShader.SetUnorderedAccessView(0, null);
        }

        void ComputeAutoExposure(CommandBuffer commandBuffer, int currentFrame, SceneRenderData renderData) {
            ComputeDownsampleViewColors(commandBuffer, new Vector4(1f, 0f, 0f, 0f), Vector4.Zero, frameGraph.viewColorDownresMaps[0], frameGraph.viewLuminanceMaps);
            Veldrid.CommandList commandList = commandBuffer.commandList;
            Veldrid.TextureView finalMap = frameGraph.autoExposureMaps[currentFrame];
            Veldrid.TextureView historyMap = frameGraph.autoExposureMaps[1 - currentFrame];
            frameGraph.AllocConstants(commandList, 8, out var constants, out uint constantOffset);
            constants[0] = Vector4.Zero;
            constants[1] = Vector4.Zero;
            constants[2] = Vector4.Zero;
            constants[3] = Vector4.Zero;
            constants[4] = Vector4.Zero;
            constants[5] = new Vector4(renderData.fxRenderData.autoExposureMinLuminance, renderData.fxRenderData.autoExposureLuminanceBlendRatio, renderData.fxRenderData.autoExposureSpeed, renderData.fxRenderData.autoExposureLuminanceScale);
            constants[6] = new Vector4(renderData.fxRenderData.autoExposureMinLuminance, renderData.fxRenderData.autoExposureMax, 0f, 0f);
            frameGraph.builtinComputeShader.SetConstantBuffer(computeAutoExposure, 0, new Veldrid.DeviceBufferRange(frameGraph.dynamicUniformBuffer, 0, 256));
            frameGraph.builtinComputeShader.SetTexture(computeAutoExposure, 1, frameGraph.viewLuminanceMaps[frameGraph.viewLuminanceMaps.Length - 1]);
            frameGraph.builtinComputeShader.SetTexture(computeAutoExposure, 2, historyMap);
            frameGraph.builtinComputeShader.SetTexture(computeAutoExposure, 3, finalMap);
            frameGraph.builtinComputeShader.Dispatch(commandList, computeAutoExposure, new [] {constantOffset}, 1, 1, 1);
            commandBuffer.context1.ComputeShader.SetShaderResources(0, null, null);
            commandBuffer.context1.ComputeShader.SetUnorderedAccessView(0, null);
        }

        void ComputeBloom(CommandBuffer commandBuffer, SceneRenderData renderData) {
            Veldrid.CommandList commandList = commandBuffer.commandList;
            ComputeDownsampleViewColors(commandBuffer, new Vector4(2f, 0f, 0f, renderData.fxRenderData.bloomThreshold), Vector4.Zero, frameGraph.viewColorDownresMaps[0], frameGraph.bloomBluredMaps);            
            float[] blendRatio = {
                1,
                1,
                1,
                0.2f,
                0.25f,
                1
            };
            commandBuffer.context1.ComputeShader.SetSampler(0, GraphicsDevice._LinearClampSampler);
            for(int i = frameGraph.bloomBluredMaps.Length - 1; i >= 0; i--) {
                Veldrid.TextureView rt = frameGraph.bloomBluredMaps[i];
                frameGraph.AllocConstants(commandList, 8, out var constants, out uint constantOffset);
                constants[0] = GetTexelSize(rt);
                constants[1] = Vector4.Zero;
                constants[2] = Vector4.Zero;
                constants[3] = new Vector4(GetTexelSize(rt).Z, 0f, 1f, 0f);  
                frameGraph.builtinComputeShader.SetConstantBuffer(computeGaussianBlur, 0, new Veldrid.DeviceBufferRange(frameGraph.dynamicUniformBuffer, 0, 256));
                frameGraph.builtinComputeShader.SetSampler(computeGaussianBlur, 1, GraphicsDevice.PointClampSampler);
                frameGraph.builtinComputeShader.SetTexture(computeGaussianBlur, 2, rt);
                frameGraph.builtinComputeShader.SetTexture(computeGaussianBlur, 3, Image2D.black.textureObject);
                frameGraph.builtinComputeShader.SetTexture(computeGaussianBlur, 4, frameGraph.bloomDownresMaps[i]);
                frameGraph.builtinComputeShader.Dispatch(commandList, computeGaussianBlur, new [] {constantOffset}, (uint)rt.Target.Width, (uint)rt.Target.Height, 1);
  
                frameGraph.AllocConstants(commandList, 8, out constants, out constantOffset);
                constants[0] = GetTexelSize(rt);
                constants[1] = Vector4.Zero;
                constants[2] = Vector4.Zero;
                constants[3] = new Vector4(0f, GetTexelSize(rt).W, blendRatio[i], 1f);  
                frameGraph.builtinComputeShader.SetConstantBuffer(computeGaussianBlur, 0, new Veldrid.DeviceBufferRange(frameGraph.dynamicUniformBuffer, 0, 256));
                frameGraph.builtinComputeShader.SetSampler(computeGaussianBlur, 1, GraphicsDevice.PointClampSampler);
                frameGraph.builtinComputeShader.SetTexture(computeGaussianBlur, 2, frameGraph.bloomDownresMaps[i]);
                if(i == frameGraph.bloomDownresMaps.Length - 1) {
                    frameGraph.builtinComputeShader.SetTexture(computeGaussianBlur, 3, Image2D.black.textureObject);
                } else {
                    frameGraph.builtinComputeShader.SetTexture(computeGaussianBlur, 3, frameGraph.bloomBluredMaps[i + 1]);
                }
                frameGraph.builtinComputeShader.SetTexture(computeGaussianBlur, 4, rt);
                frameGraph.builtinComputeShader.Dispatch(commandList, computeGaussianBlur, new [] {constantOffset}, (uint)rt.Target.Width, (uint)rt.Target.Height, 1);                  
            }
        }

        void ComputeSSR(CommandBuffer commandBuffer, Veldrid.TextureView lastFrameColorMap, SceneRenderData renderData) {
            Veldrid.CommandList commandList = commandBuffer.commandList;
            var constantOffsets = new uint[2];
            frameGraph.UpdateConstants(commandList, fxConstants, out constantOffsets[0]);
            frameGraph.AllocConstants(commandList, 16, out var constants, out constantOffsets[1]);
            constants[0] = GetTexelSize(frameGraph.ssrColorMap);
            constants[1] = new Vector4(renderData.fxRenderData.ssrSmoothnessThreshold, renderData.fxRenderData.ssrScale, 0f, renderData.fxRenderData.ssrDitherSampleOffset);
            frameGraph.ssrComputeShader.SetConstantBuffer(computeSSR, 0, new Veldrid.DeviceBufferRange(frameGraph.dynamicUniformBuffer, 0, 512 + 256));
            frameGraph.ssrComputeShader.SetConstantBuffer(computeSSR, 1, new Veldrid.DeviceBufferRange(frameGraph.dynamicUniformBuffer, 0, 256));
            frameGraph.ssrComputeShader.SetSampler(computeSSR, 2, GraphicsDevice.PointClampSampler);
            frameGraph.ssrComputeShader.SetTexture(computeSSR, 3, lastFrameColorMap);
            frameGraph.ssrComputeShader.SetTexture(computeSSR, 4, frameGraph.viewDepthMap);
            frameGraph.ssrComputeShader.SetTexture(computeSSR, 5, frameGraph.viewNormalMap);
            frameGraph.ssrComputeShader.SetTexture(computeSSR, 6, frameGraph.viewSpecularMap);
            frameGraph.ssrComputeShader.SetTexture(computeSSR, 7, frameGraph.ssrColorMap);
            frameGraph.ssrComputeShader.SetTexture(computeSSR, 8, frameGraph.ssrMaskMap);
            commandBuffer.context1.ComputeShader.SetSampler(0, GraphicsDevice._LinearClampSampler);
            frameGraph.ssrComputeShader.Dispatch(commandList, computeSSR, constantOffsets, (uint)frameGraph.ssrColorMap.Target.Width, (uint)frameGraph.ssrColorMap.Target.Height, 1);                  
            commandBuffer.context1.ComputeShader.SetShaderResources(0, null, null, null, null);
            commandBuffer.context1.ComputeShader.SetUnorderedAccessView(0, null);            
        }

        void ComputeTAA(CommandBuffer commandBuffer, Veldrid.TextureView historyMap, Veldrid.TextureView outputMap, SceneRenderData renderData) {
            Veldrid.CommandList commandList = commandBuffer.commandList;
            var constantOffsets = new uint[2];
            frameGraph.UpdateConstants(commandList, fxConstants, out constantOffsets[0]);
            frameGraph.AllocConstants(commandList, 16, out var constants, out constantOffsets[1]);
            constants[0] =GetTexelSize(outputMap);
            constants[1] = new Vector4(0f, -0.05495f, -0.01997f, -0.03119f);
            constants[2] = new Vector4(1.08147f, 0.11487f, 0f, -0.03227f);
            constants[3] = new Vector4(0.02329f, 0.09283f, 0.06253f, 0.07075f);
            constants[4] = new Vector4(0.28199f, 0.18996f, 0.03632f, 0.14478f);
            constants[5] = new Vector4(-0.05797f, 0f, 0f, 0f);
            constants[6] = new Vector4(0.09753f, 0f, 0f, 0f);
            frameGraph.taaComputeShader.SetConstantBuffer(computeTAA, 0, new Veldrid.DeviceBufferRange(frameGraph.dynamicUniformBuffer, 0, 512));
            frameGraph.taaComputeShader.SetConstantBuffer(computeTAA, 1, new Veldrid.DeviceBufferRange(frameGraph.dynamicUniformBuffer, 0, 256));
            frameGraph.taaComputeShader.SetSampler(computeTAA, 2, GraphicsDevice.LinearClampSampler);
            frameGraph.taaComputeShader.SetTexture(computeTAA, 3, frameGraph.compositeOutputMap);
            frameGraph.taaComputeShader.SetTexture(computeTAA, 4, historyMap);
            frameGraph.taaComputeShader.SetTexture(computeTAA, 5, frameGraph.viewOpaqueColorMap);
            frameGraph.taaComputeShader.SetTexture(computeTAA, 6, frameGraph.viewDepthMap);
            frameGraph.taaComputeShader.SetTexture(computeTAA, 7, outputMap);
            commandBuffer.context1.ComputeShader.SetSampler(0, GraphicsDevice._LinearClampSampler);
            frameGraph.taaComputeShader.Dispatch(commandList, computeTAA, constantOffsets, (uint)outputMap.Target.Width, (uint)outputMap.Target.Height, 1);                  
            commandBuffer.context1.ComputeShader.SetShaderResources(0, null, null, null, null);
            commandBuffer.context1.ComputeShader.SetUnorderedAccessView(0, null);
        }

        enum JitterMode {
            None = 0,
            Uniform2x = 1,
            Hammersley4x = 2,
            Hammersley8x = 3,
            Hammersley16x = 4,
            Halton,
            Count
        }

        FXConstants fxConstants;
        Matrix prevViewProjectionMatrixNojitter;
        Matrix prevViewProjectionMatrix;
        void UpdateViewConstants(SceneRenderData renderData, ViewDef viewDef, out ModelPassViewConstants viewConstants) {
            viewConstants = new ModelPassViewConstants();
            Matrix inverseViewMatrix = Matrix.Invert(viewDef.viewMatrix);
            Matrix projectionMatrix = viewDef.projectionMatrix;

            const float jitterScale = 1f;
            Vector2 temporalAAJitter = Vector2.Zero;

            JitterMode jitterMode = JitterMode.Halton;
            switch(jitterMode) {
                case JitterMode.Uniform2x: {
                    temporalAAJitter = new Vector2(Time.frameCount % 2 == 0 ? -0.5f : 0.5f);
                    break;
                }
                case JitterMode.Hammersley4x: {
                    uint idx = Time.frameCount % 4;
                    temporalAAJitter = MathHelper.Hammersley2D(4, idx) * 2f - 1f;
                    break;
                }
                case JitterMode.Hammersley8x: {
                    uint idx = Time.frameCount % 8;
                    temporalAAJitter = MathHelper.Hammersley2D(8, idx) * 2f - 1f;
                    break;
                }
                case JitterMode.Hammersley16x: {
                    uint idx = Time.frameCount % 16;
                    temporalAAJitter = MathHelper.Hammersley2D(16, idx) * 2f - 1f;
                    break;
                }
                case JitterMode.Halton: {
                    Vector4 halton = MathHelper.GetHaltonSequence(Time.frameCount % 256);
                    temporalAAJitter.X = halton.X * 2f - 1f;
                    temporalAAJitter.Y = halton.Y * 2f - 1f;
                    break;
                }
            }
            temporalAAJitter *= jitterScale;
            temporalAAJitter.X /= (float)frameGraph.renderWidth;
            temporalAAJitter.Y /= (float)frameGraph.renderHeight;
            projectionMatrix[2, 0] = temporalAAJitter.X;
            projectionMatrix[2, 1] = temporalAAJitter.Y;  
            Matrix viewProjectionMatrix = viewDef.viewMatrix * projectionMatrix;
            viewConstants.viewProjectionMatrix0 = viewProjectionMatrix.Column1;
            viewConstants.viewProjectionMatrix1 = viewProjectionMatrix.Column2;
            viewConstants.viewProjectionMatrix2 = viewProjectionMatrix.Column3;
            viewConstants.viewProjectionMatrix3 = viewProjectionMatrix.Column4;
            viewConstants.frustumVectorLT = new Vector4(viewDef.frustumVectorLT, 0f);
            viewConstants.frustumVectorRT = new Vector4(viewDef.frustumVectorRT, 0f);
            viewConstants.frustumVectorLB = new Vector4(viewDef.frustumVectorLB, 0f);
            viewConstants.frustumVectorRB = new Vector4(viewDef.frustumVectorRB, 0f);            
            viewConstants.clusteredLightingSize = viewDef.clusteredLightingSize;
            viewConstants.clusteredLightingParms = viewDef.clusteredLightingParms;
            viewConstants.screenParms = new Vector4(viewDef.width, viewDef.height, 1f / viewDef.width, 1f / viewDef.height);
            viewConstants.sunForward = renderData.fxRenderData.sunForward != Vector3.Zero ? new Vector4(renderData.fxRenderData.sunForward, renderData.fxRenderData.usePSSM ? 1f : 0f) : Vector4.Zero;
            viewConstants.viewOrigin = new Vector4(viewDef.viewOrigin, 0f);
            viewConstants.shadowsAtlasResolution = new Vector4(8192, 8192, 1f / 8192f, 1f / 8192f);
            viewConstants.sunColor = new Vector4(renderData.fxRenderData.sunColor, 1f);
            viewConstants.volumetricLightingResolution = new Vector4(frameGraph.finalLightScatteringPackedMaps[0].Target.Width, frameGraph.finalLightScatteringPackedMaps[0].Target.Height, frameGraph.finalLightScatteringPackedMaps[0].Target.Depth, 0f);
            viewConstants.volumetricLightingZNearFar = new Vector4(0.1f, renderData.fxRenderData.volumetricLightingDistance, 0f, 0.00013f);
            viewConstants.volumetricLightingParms = new Vector4(renderData.fxRenderData.sunDiskScale, renderData.fxRenderData.sunIntensity, renderData.fxRenderData.decoupleSunColorFromSky ? 1f : 0f, renderData.fxRenderData.scatteringFlags);
            viewConstants.timeParms = new Vector4(renderData.fxRenderData.gameFrameDeltaTime, renderData.fxRenderData.gameTime, 0f, renderData.fxRenderData.gameFrameCount);;
            // http://www.humus.name/temp/Linearize%20depth.txt
            float n = viewDef.nearClipPlane;
            float f = viewDef.farClipPlane;
            viewConstants.depthBufferParms = new Vector4(1 - f / n, f / n, 1 / f - 1 / n, 1 / n);

            fxConstants.globalViewOrigin = new Vector4(viewDef.viewOrigin, 0f);
            fxConstants.frustumVectorLT = new Vector4(viewDef.frustumVectorLT, 0f);
            fxConstants.frustumVectorRT = new Vector4(viewDef.frustumVectorRT, 0f);
            fxConstants.frustumVectorLB = new Vector4(viewDef.frustumVectorLB, 0f);
            fxConstants.frustumVectorRB = new Vector4(viewDef.frustumVectorRB, 0f);
            fxConstants.depthBufferParms = new Vector4(1 - f / n, f / n, 1 / f - 1 / n, 1 / n);
            fxConstants.sunForward = renderData.fxRenderData.sunForward != Vector3.Zero ? new Vector4(renderData.fxRenderData.sunForward, 1f) : Vector4.Zero;
            fxConstants.shadowsAtlasResolution = viewConstants.shadowsAtlasResolution;
            fxConstants.timeParms = viewConstants.timeParms;
            fxConstants.viewMatrix = viewDef.viewMatrix;
            fxConstants.inverseViewMatrix = inverseViewMatrix;
            fxConstants.inverseProjectionMatrix = Matrix.Invert(viewDef.projectionMatrix);
            fxConstants.inverseViewProjectionMatrix = Matrix.Invert(viewProjectionMatrix);
            fxConstants.prevViewProjectionMatrixNoJitter = renderData.fxRenderData.gameFrameCount > 1 ? prevViewProjectionMatrixNojitter : viewDef.viewProjectionMatrix;
            fxConstants.screenParms = viewConstants.screenParms;
            fxConstants.clusteredLightingSize = viewConstants.clusteredLightingSize;
            fxConstants.clusteredLightingParms = viewConstants.clusteredLightingParms;
            fxConstants.viewProjectionMatrix = viewProjectionMatrix;
            fxConstants.prevViewProjectionMatrix = renderData.fxRenderData.gameFrameCount > 1 ? prevViewProjectionMatrix : fxConstants.viewProjectionMatrix;
            prevViewProjectionMatrixNojitter = viewDef.viewProjectionMatrix;
            prevViewProjectionMatrix = fxConstants.viewProjectionMatrix;
        }

        public void OnMainSwapchainResized(Veldrid.Swapchain swapchain) {
            presentPass.SetFramebuffer(swapchain);
        }

        public void Render(CommandBuffer commandBuffer, SceneRenderData renderData) {
            Veldrid.CommandList commandList = commandBuffer.commandList;
            uint modelInstanceUniformOffset = 0;
            if(renderData.fxRenderData.reflectionProbeShaderParms != IntPtr.Zero) {
                commandList.UpdateBuffer(probeListBuffer, 0, renderData.fxRenderData.reflectionProbeShaderParms, (uint)renderData.fxRenderData.reflectionProbeShaderParmsSizeInBytes);
            }
            if(renderData.numModelInstances > 0) {
                frameGraph.UpdateConstants(commandList, renderData.modelInstanceConstantBuffer, (uint)(renderData.numModelInstances * ModelPassInstanceConstants.UniformBlockSizeInBytes), out modelInstanceUniformOffset);
            }
            if(renderData.numInstanceMatrices > 0) {
                commandList.UpdateBuffer(modelMatricesBuffer, 0, renderData.instanceMatricesBuffer, (uint)(renderData.numInstanceMatrices * 3 * Utilities.SizeOf<Vector4>()));
            }
            transparencyPass.UpdateResources(renderData, commandList);

            int currentFrame = renderData.fxRenderData.gameFrameCount % 2 == 0 ? 0 : 1;
            var viewColorMap = frameGraph.viewColorMaps[currentFrame];
            var viewLastFrameColorMap = frameGraph.viewColorMaps[1 - currentFrame];

            ViewDef[] viewDefs = renderData.viewDefs;
            if(viewDefs != null) {
                for(int i = 0; i < viewDefs.Length; i++) {
                    UpdateViewConstants(renderData, viewDefs[i], out ModelPassViewConstants viewConstants);
                    commandList.UpdateBuffer(viewUniformBuffer, 0, viewConstants);
                    if(viewDefs[i].clusterListSizeInBytes > 0 && viewDefs[i].clusterItemListSizeInBytes > 0) {
                        commandList.UpdateBuffer(clusterListBuffer, 0, viewDefs[i].clusterList, (uint)viewDefs[i].clusterListSizeInBytes);
                        commandList.UpdateBuffer(clusterItemListBuffer, 0, viewDefs[i].clusterItemList, (uint)viewDefs[i].clusterItemListSizeInBytes);
                        if(viewDefs[i].lightShaderParmsSizeInBytes > 0) {
                            commandList.UpdateBuffer(lightListBuffer, 0, viewDefs[i].lightShaderParms, (uint)viewDefs[i].lightShaderParmsSizeInBytes);
                        }
                        if(viewDefs[i].shadowShaderParmsSizeInBytes > 0) {
                            commandList.UpdateBuffer(shadowListBuffer, 0, viewDefs[i].shadowShaderParms, (uint)viewDefs[i].shadowShaderParmsSizeInBytes);
                        }
                        if(viewDefs[i].decalShaderParmsSizeInBytes > 0) {
                            commandList.UpdateBuffer(decalListBuffer, 0, viewDefs[i].decalShaderParms, (uint)viewDefs[i].decalShaderParmsSizeInBytes);
                        }
                        if(viewDefs[i].scatteringVolumeShaderParmsSizeInBytes > 0) {
                            commandList.UpdateBuffer(scatteringVolumesUniformBuffer, 0, viewDefs[i].scatteringVolumeShaderParms, (uint)viewDefs[i].scatteringVolumeShaderParmsSizeInBytes);                            
                        }
                    }

                    depthPass.Render(viewDefs[i], modelInstanceUniformOffset, commandList);
                    shadowAtlasPass.Render(viewDefs[i], modelInstanceUniformOffset, commandList);
                    opaquePass.Render(viewDefs[i], modelInstanceUniformOffset, commandList);
                }

                ComputeDownsampleViewDepths(commandBuffer);
                if((renderData.fxRenderData.scatteringFlags & (1 << 0)) != 0) {
                    ComputeSkylightingLUT(commandBuffer, renderData);
                }
                if((renderData.fxRenderData.scatteringFlags & (1 << 1)) != 0) {
                    ComputeLightScattering(commandBuffer, renderData, currentFrame);
                    ComputeLightScatteringIntegral(commandBuffer, renderData, currentFrame);
                }
                ComputeSSAO(commandBuffer);
                ComputeDeferredProbes(commandBuffer, renderData);
                ComputeSSR(commandBuffer, viewLastFrameColorMap, renderData);
                ComputeComposite(commandBuffer, renderData);

                skybackgroundPass.Render(commandBuffer);
                transparencyPass.Render(viewDefs[0], modelInstanceUniformOffset, commandBuffer);
                commandBuffer.context1.PixelShader.SetShaderResource(0, null);
                commandBuffer.context1.OutputMerger.SetRenderTargets((SharpDX.Direct3D11.DepthStencilView)null, null, null, null);

                ComputeTAA(commandBuffer, viewLastFrameColorMap, viewColorMap, renderData);
                ComputeDownsampleViewColor(commandBuffer, viewColorMap, Vector4.Zero, frameGraph.viewColorDownresMaps[0]);
                ComputeAutoExposure(commandBuffer, currentFrame, renderData);
                ComputeBloom(commandBuffer, renderData);
            }

            debugPass.Render(commandList, renderData.debugToolRenderData, currentFrame);

            //presentPass.AddScreenPic(0, 0, 512, 512, debugSSGIMap, new Vector4(3f, 0f, 0f, 5f));
            //presentPass.AddScreenPic(0, 0, 512, 512, debugSSRColorMap, new Vector4(3f, 0f, 0f, 5f));
            //presentPass.AddScreenPic(512, 0, 512, 512, debugSSRMaskMap, new Vector4(3f, 0f, 0f, 0f));
            //presentPass.AddScreenPic(0, 0, 256, 256, debugViewColorDownresMap, new Vector4(3f, 0, 0, 5f));
            //presentPass.AddScreenPic(0, 0, 256, 256, debugViewDpethDownresMap, Vector4.Zero);
            //presentPass.AddScreenPic(256, 0, 256, 256, debugViewDpethDownres2xMap, Vector4.Zero);
            //presentPass.AddScreenPic(0, 256, 256, 256, debugViewNormalMap, Vector4.One);
            //presentPass.AddScreenPic(256, 256, 256, 256, debugViewSpecularMap, new Vector4(2f, 2f, 2f, 2f));
            //presentPass.AddScreenPic(0, 0, 512, 512, debugSSAOMap, new Vector4(3f, 0f, 0f, 0f));
            presentPass.Render(commandList, currentFrame);
        }
    }
}