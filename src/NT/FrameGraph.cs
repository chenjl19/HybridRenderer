using System;
using System.Collections.Generic;
using SharpDX;

namespace NT
{
    internal struct RenderState {
        public Veldrid.ResourceSet renderPassResourceSet;
        public Veldrid.DeviceBuffer vertexBuffer;
        public Veldrid.DeviceBuffer indexBuffer;
    }

    public abstract class RenderPass : IDisposable {
        public class CommonResourceInfo {
            public bool hightFrequency;
            public int set;
            public int binding;
            public Veldrid.ShaderStages stages;
            public Veldrid.ResourceLayoutElementOptions options;
            public Veldrid.BindableResource resource;
        }

        public readonly string name;
        public Veldrid.DeviceBuffer dynamicUniformBuffer {get; protected set;}
        public Veldrid.Framebuffer framebuffer {get; protected set;}
        protected readonly FrameGraph frameGraph;
        protected readonly Dictionary<string, CommonResourceInfo> commonResources;
        protected Veldrid.ResourceLayout resourceLayoutPerFrame;
        protected Veldrid.ResourceSet resourceSetPerFrame;

        public void SetPerFrameResources(Veldrid.ResourceLayout layout, Veldrid.ResourceSet set) {
            resourceLayoutPerFrame = layout;
            resourceSetPerFrame = set;
        }

        public Veldrid.ResourceLayout GetPerFrameResourceLayout() {
            return resourceLayoutPerFrame;
        }

        public Veldrid.ResourceSet GetPerFrameResourceSet() {
            return resourceSetPerFrame;
        }

        public RenderPass(FrameGraph myFrameGraph, string myName) {
            frameGraph = myFrameGraph;
            name = myName;
            commonResources = new Dictionary<string, CommonResourceInfo>();
        }

        public int NumCommonResources() {
            return commonResources != null ? commonResources.Count : 0;
        }

        public void AddCommonResource(string name, CommonResourceInfo info) {
            if(!commonResources.TryGetValue(name, out CommonResourceInfo found)) {
                commonResources.Add(name, info);
            }
        }

        public void AddPerFrameResource(string name, int set, int binding, Veldrid.ShaderStages stages, Veldrid.BindableResource resource) {
            if(!commonResources.TryGetValue(name, out CommonResourceInfo info)) {
                info = new CommonResourceInfo();
                info.hightFrequency = true;
                info.set = set;
                info.binding = binding;
                info.stages = stages;
                info.options = Veldrid.ResourceLayoutElementOptions.None;
                info.resource = resource;
                commonResources.Add(name, info);
            }
        }

        public void AddPerObjectResource(string name, int set, int binding, Veldrid.ShaderStages stages, Veldrid.ResourceLayoutElementOptions options, Veldrid.BindableResource resource) {
            if(!commonResources.TryGetValue(name, out CommonResourceInfo info)) {
                info = new CommonResourceInfo();
                info.hightFrequency = false;
                info.set = set;
                info.binding = binding;
                info.stages = stages;
                info.options = options;
                info.resource = resource;
                commonResources.Add(name, info);                
            }
        }

        public CommonResourceInfo FindCommonResource(string name) {
            commonResources.TryGetValue(name, out CommonResourceInfo info);
            return info;
        }

        public bool FindCommonResource(string name, out CommonResourceInfo info) {
            return commonResources.TryGetValue(name, out info);
        }

        public abstract void Setup();
        public abstract void PostSetup();
        public abstract void Dispose();

        //ViewEntity currentSpace;

        internal void DrawSurface(
            Veldrid.CommandList commandList, 
            uint dynamicUniformOffset, 
            ViewEntity space,
            SubMesh drawInfo,
            MaterialRenderProxy material, bool positionOnly) 
        {
            var renderState = material.renderState;
            commandList.SetPipeline(renderState.pipeline);

            uint resourceSetSlot = 0;
            if(material.renderPassResourceSet != null) {
                commandList.SetGraphicsResourceSet(resourceSetSlot, material.renderPassResourceSet);
                resourceSetSlot++;
            }
            if(material.materialResourceSet != null) {
                commandList.SetGraphicsResourceSet(resourceSetSlot, material.materialResourceSet);
                resourceSetSlot++;
            }
            if(material.lightmapResourceSet != null) {
                commandList.SetGraphicsResourceSet(resourceSetSlot, material.lightmapResourceSet);
                resourceSetSlot++;
            }
            if(material.dynamicUniformBufferResourceSet != null) {
                uint instanceUniformOffset = dynamicUniformOffset + (uint)space.id * ModelPassInstanceConstants.UniformBlockSizeInBytes;
                uint[] dynamicOffsets = new uint[renderState.numDynamicCBuffers];
                dynamicOffsets[0] = instanceUniformOffset;
                if(material.uniformBlocks != null) {
                    for(int i = 0; i < material.uniformBlocks.Length; i++) {
                        frameGraph.UpdateConstants(commandList, material.uniformBlocks[i].dataPtr, material.uniformBlocks[i].blockSizeInBytes, out dynamicOffsets[i + 1]);
                    }
                }
                commandList.SetGraphicsResourceSet(resourceSetSlot, material.dynamicUniformBufferResourceSet, dynamicOffsets);
            }

            //if(space != currentSpace) {
            //    currentSpace = space;
                int numBindVertexBuffers = positionOnly ? material.numPositionStreams : space.vertexFactory.NumBuffers();
                DeviceBufferBinding[] vertexBuffers = space.vertexFactory.GetDeviceBuffers();
                for(int i = 0; i < numBindVertexBuffers; i++) {
                    commandList.SetVertexBuffer((uint)i, vertexBuffers[i].buffer, vertexBuffers[i].offsetInBytes);
                }
                IndexBuffer indexBuffer = space.indexBuffer;
                if(indexBuffer != null) {
                    commandList.SetIndexBuffer(indexBuffer.bufferObject, indexBuffer.indexFormat, (uint)indexBuffer.offsetInOtherInBytes);
                }
            //}

            commandList.DrawIndexed((uint)drawInfo.numIndices, 1, (uint)drawInfo.indexOffset, drawInfo.vertexOffset, 0);
        }
    }

    public class FrameGraph {
        public const uint DefaultRenderWidth = 1920;
        public const uint DefaultRenderHeight = 1080;

        readonly Dictionary<string, RenderPass> renderPasses;
        public Veldrid.DeviceBuffer dynamicUniformBuffer {get; private set;}
        internal FrameRenderResources frameRenderResources {get; private set;}
        Veldrid.MappedResource mappedDynamicUniformBuffer;

        public Veldrid.TextureView viewOpaqueColorMap {get; private set;}
        public Veldrid.TextureView viewDepthMap {get; private set;}
        public Veldrid.TextureView viewNormalMap {get; private set;}
        public Veldrid.TextureView viewSpecularMap {get; private set;}
        public Veldrid.TextureView indirectSpecularOutput {get; private set;}
        public Veldrid.TextureView[] viewDepthDownresMaps {get; private set;}
        public Veldrid.TextureView[] viewColorDownresMaps {get; private set;}
        public Veldrid.TextureView[] viewLuminanceMaps {get; private set;}
        public Veldrid.TextureView[] autoExposureMaps {get; private set;}
        public Veldrid.TextureView[] bloomDownresMaps {get; private set;}
        public Veldrid.TextureView[] bloomBluredMaps {get; private set;}
        public Veldrid.TextureView ssaoOutput {get; private set;}
        public Veldrid.TextureView ssgiOutput {get; private set;}
        public Veldrid.TextureView skylightingLUT {get; private set;}
        public Veldrid.TextureView[] lightScatteringPackedMaps {get; private set;}
        public Veldrid.TextureView[] finalLightScatteringPackedMaps {get; private set;}
        public Veldrid.TextureView[] viewColorMaps {get; private set;}
        public Veldrid.TextureView compositeOutputMap {get; private set;}
        public Veldrid.TextureView ssrColorMap {get; private set;}
        public Veldrid.TextureView ssrMaskMap {get; private set;}
        public uint renderWidth {get; private set;}
        public uint renderHeight {get; private set;}
        public uint renderHalfWidth {get; private set;}
        public uint renderHalfHeight {get; private set;}
        public Vector2 renderPositionToViewTexture {get; private set;}
        public int viewTextureDownresLevels {get; private set;}
        public Vector4 viewTextureTexelSize {get; private set;}
        public Vector4 viewTextureHalfTexelSize {get; private set;}

        readonly int DynamicUniformBufferSizePerFrame;

        public ComputeShader builtinComputeShader {get; private set;}
        public ComputeShader skylightingComputeShader {get; private set;}
        public ComputeShader compositeComputeShader {get; private set;}
        public ComputeShader deferredProbesComputeShader {get; private set;}
        public ComputeShader volumetricLightingComputeShader {get; private set;}
        public ComputeShader taaComputeShader {get; private set;}
        public ComputeShader ssrComputeShader {get; private set;}

        public FrameGraph() {
            renderWidth = DefaultRenderWidth;
            renderHeight = DefaultRenderHeight;
            renderHalfWidth = (uint)MathF.Ceiling(renderWidth / 2f);
            renderHalfHeight = (uint)MathF.Ceiling(renderHeight / 2f);
            viewTextureTexelSize = new Vector4(renderWidth, renderHeight, 1f / renderWidth, 1f / renderHeight);
            viewTextureHalfTexelSize = new Vector4(renderHalfWidth, renderHalfHeight, 1f / renderHalfWidth, 1f / renderHalfHeight);

            DynamicUniformBufferSizePerFrame = MathHelper.Align(6 * 1024 * 1024, (int)GraphicsDevice.gd.UniformBufferMinOffsetAlignment); 
            renderPasses = new Dictionary<string, RenderPass>();
            frameRenderResources = new FrameRenderResources();
            frameRenderResources.Init();
            dynamicUniformBuffer = GraphicsDevice.ResourceFactory.CreateBuffer(new Veldrid.BufferDescription((uint)DynamicUniformBufferSizePerFrame, Veldrid.BufferUsage.UniformBuffer | Veldrid.BufferUsage.Dynamic));

            var texture = GraphicsDevice.ResourceFactory.CreateTexture(new Veldrid.TextureDescription(
                renderWidth,
                renderHeight,
                1,
                1,
                1,
                Veldrid.PixelFormat.D24_UNorm_S8_UInt,
                Veldrid.TextureUsage.DepthStencil | Veldrid.TextureUsage.Sampled,
                Veldrid.TextureType.Texture2D
            ));
            viewDepthMap = GraphicsDevice.ResourceFactory.CreateTextureView(texture);

            texture = GraphicsDevice.ResourceFactory.CreateTexture(new Veldrid.TextureDescription(
                renderWidth,
                renderHeight,
                1,
                1,
                1,
                Veldrid.PixelFormat.R16_G16_Float,
                Veldrid.TextureUsage.RenderTarget | Veldrid.TextureUsage.Sampled,
                Veldrid.TextureType.Texture2D
            ));
            viewNormalMap = GraphicsDevice.ResourceFactory.CreateTextureView(texture);

            texture = GraphicsDevice.ResourceFactory.CreateTexture(new Veldrid.TextureDescription(
                renderWidth,
                renderHeight,
                1,
                1,
                1,
                Veldrid.PixelFormat.R8_G8_B8_A8_UNorm,
                Veldrid.TextureUsage.RenderTarget | Veldrid.TextureUsage.Sampled,
                Veldrid.TextureType.Texture2D
            ));
            viewSpecularMap = GraphicsDevice.ResourceFactory.CreateTextureView(texture);

            texture = GraphicsDevice.ResourceFactory.CreateTexture(new Veldrid.TextureDescription(
                renderWidth,
                renderHeight,
                1,
                1,
                1,
                Veldrid.PixelFormat.R11_G11_B10_Float,
                Veldrid.TextureUsage.RenderTarget | Veldrid.TextureUsage.Sampled,
                Veldrid.TextureType.Texture2D
            ));
            viewOpaqueColorMap = GraphicsDevice.ResourceFactory.CreateTextureView(texture);

            viewColorMaps = new Veldrid.TextureView[2];
            for(int i = 0; i < viewColorMaps.Length; i++) {
                var target = GraphicsDevice.ResourceFactory.CreateTexture(new Veldrid.TextureDescription(
                    renderWidth,
                    renderHeight,
                    1,
                    1,
                    1,
                    Veldrid.PixelFormat.R11_G11_B10_Float,
                    Veldrid.TextureUsage.RenderTarget | Veldrid.TextureUsage.Storage | Veldrid.TextureUsage.Sampled,
                    Veldrid.TextureType.Texture2D
                ));
                viewColorMaps[i] = GraphicsDevice.ResourceFactory.CreateTextureView(target);
                viewColorMaps[i].Name = $"viewColorMap{i}";
            }

            texture = GraphicsDevice.ResourceFactory.CreateTexture(new Veldrid.TextureDescription(
                renderWidth,
                renderHeight,
                1,
                1,
                1,
                Veldrid.PixelFormat.R11_G11_B10_Float,
                Veldrid.TextureUsage.RenderTarget | Veldrid.TextureUsage.Storage | Veldrid.TextureUsage.Sampled,
                Veldrid.TextureType.Texture2D
            ));
            compositeOutputMap = GraphicsDevice.ResourceFactory.CreateTextureView(texture);

            texture = GraphicsDevice.ResourceFactory.CreateTexture(new Veldrid.TextureDescription(
                renderWidth,
                renderHeight,
                1,
                1,
                1,
                Veldrid.PixelFormat.R11_G11_B10_Float,
                Veldrid.TextureUsage.RenderTarget | Veldrid.TextureUsage.Storage | Veldrid.TextureUsage.Sampled,
                Veldrid.TextureType.Texture2D
            ));
            ssrColorMap = GraphicsDevice.ResourceFactory.CreateTextureView(texture);
            texture = GraphicsDevice.ResourceFactory.CreateTexture(new Veldrid.TextureDescription(
                renderWidth,
                renderHeight,
                1,
                1,
                1,
                Veldrid.PixelFormat.R8_UNorm,
                Veldrid.TextureUsage.RenderTarget | Veldrid.TextureUsage.Storage | Veldrid.TextureUsage.Sampled,
                Veldrid.TextureType.Texture2D
            ));
            ssrMaskMap = GraphicsDevice.ResourceFactory.CreateTextureView(texture);

            texture = GraphicsDevice.ResourceFactory.CreateTexture(new Veldrid.TextureDescription(
                renderHalfWidth,
                renderHalfHeight,
                1,
                1,
                1,
                Veldrid.PixelFormat.R8_UNorm,
                Veldrid.TextureUsage.Storage | Veldrid.TextureUsage.Sampled,
                Veldrid.TextureType.Texture2D
            ));
            ssaoOutput = GraphicsDevice.ResourceFactory.CreateTextureView(texture);

            texture = GraphicsDevice.ResourceFactory.CreateTexture(new Veldrid.TextureDescription(
                renderHalfWidth,
                renderHalfHeight,
                1,
                1,
                1,
                Veldrid.PixelFormat.R8_G8_B8_A8_UNorm,
                Veldrid.TextureUsage.Storage | Veldrid.TextureUsage.Sampled,
                Veldrid.TextureType.Texture2D
            ));
            ssgiOutput = GraphicsDevice.ResourceFactory.CreateTextureView(texture);

            texture = GraphicsDevice.ResourceFactory.CreateTexture(new Veldrid.TextureDescription(
                renderWidth,
                renderHeight,
                1,
                1,
                1,
                Veldrid.PixelFormat.R11_G11_B10_Float,
                Veldrid.TextureUsage.Storage | Veldrid.TextureUsage.Sampled,
                Veldrid.TextureType.Texture2D
            ));
            indirectSpecularOutput = GraphicsDevice.ResourceFactory.CreateTextureView(texture);

            texture = GraphicsDevice.ResourceFactory.CreateTexture(new Veldrid.TextureDescription(
                128,
                32,
                32,
                1,
                32,
                Veldrid.PixelFormat.R16_G16_B16_A16_Float,
                Veldrid.TextureUsage.Storage | Veldrid.TextureUsage.Sampled,
                Veldrid.TextureType.Texture3D
            ));
            skylightingLUT = GraphicsDevice.ResourceFactory.CreateTextureView(new Veldrid.TextureViewDescription(texture, Veldrid.PixelFormat.R16_G16_B16_A16_Float, 0, 1, 0, texture.Depth));

            lightScatteringPackedMaps = new Veldrid.TextureView[4];
            for(int i = 0; i < 4; i++) {
                var textureObject = GraphicsDevice.ResourceFactory.CreateTexture(new Veldrid.TextureDescription(
                    (uint)MathF.Ceiling(renderWidth / 8f),
                    (uint)MathF.Ceiling(renderHeight / 8f),
                    64,
                    1,
                    64,
                    Veldrid.PixelFormat.R11_G11_B10_Float,
                    Veldrid.TextureUsage.Storage | Veldrid.TextureUsage.Sampled,
                    Veldrid.TextureType.Texture3D
                ));
                lightScatteringPackedMaps[i] = GraphicsDevice.ResourceFactory.CreateTextureView(new Veldrid.TextureViewDescription(textureObject, Veldrid.PixelFormat.R11_G11_B10_Float, 0, 1, 0, textureObject.Depth));
            }
            finalLightScatteringPackedMaps = new Veldrid.TextureView[2];
            for(int i = 0; i < 2; i++) {
                var textureObject2 = GraphicsDevice.ResourceFactory.CreateTexture(new Veldrid.TextureDescription(
                    (uint)MathF.Ceiling(renderWidth / 8f),
                    (uint)MathF.Ceiling(renderHeight / 8f),
                    64,
                    1,
                    64,
                    Veldrid.PixelFormat.R11_G11_B10_Float,
                    Veldrid.TextureUsage.Storage | Veldrid.TextureUsage.Sampled,
                    Veldrid.TextureType.Texture3D
                ));
                finalLightScatteringPackedMaps[i] = GraphicsDevice.ResourceFactory.CreateTextureView(new Veldrid.TextureViewDescription(textureObject2, Veldrid.PixelFormat.R11_G11_B10_Float, 0, 1, 0, textureObject2.Depth));       
            }

            viewTextureDownresLevels = 1;
            int w = (int)renderHalfWidth;
            int h = (int)renderHalfHeight;
            while((w > 1 || h > 1) && viewTextureDownresLevels < 5) {
                w = Math.Max(1, w >> 1);
                h = Math.Max(1, h >> 1);
                viewTextureDownresLevels++;
            }
            w = (int)renderHalfWidth;
            h = (int)renderHalfHeight;
            viewDepthDownresMaps = new Veldrid.TextureView[viewTextureDownresLevels];
            viewColorDownresMaps = new Veldrid.TextureView[viewTextureDownresLevels];  
            bloomDownresMaps = new Veldrid.TextureView[viewTextureDownresLevels];
            bloomBluredMaps = new Veldrid.TextureView[viewTextureDownresLevels]; 
            for(int i = 0; i < viewTextureDownresLevels; i++) {
                texture = GraphicsDevice.ResourceFactory.CreateTexture(
                    new Veldrid.TextureDescription(
                        (uint)w, 
                        (uint)h, 
                        1, 
                        1, 
                        1, 
                        viewOpaqueColorMap.Format, 
                        Veldrid.TextureUsage.Storage | Veldrid.TextureUsage.Sampled, Veldrid.TextureType.Texture2D
                    )
                );
                viewColorDownresMaps[i] = GraphicsDevice.ResourceFactory.CreateTextureView(texture);

                texture = GraphicsDevice.ResourceFactory.CreateTexture(
                    new Veldrid.TextureDescription(
                        (uint)Math.Max(1, (int)MathF.Ceiling(w / 2f)), 
                        (uint)Math.Max(1, (int)MathF.Ceiling(h / 2f)), 
                        1, 
                        1, 
                        1, 
                        viewOpaqueColorMap.Format, 
                        Veldrid.TextureUsage.Storage | Veldrid.TextureUsage.Sampled, Veldrid.TextureType.Texture2D
                    )
                );                
                bloomDownresMaps[i] = GraphicsDevice.ResourceFactory.CreateTextureView(texture);
                texture = GraphicsDevice.ResourceFactory.CreateTexture(
                    new Veldrid.TextureDescription(
                        (uint)Math.Max(1, (int)MathF.Ceiling(w / 2f)), 
                        (uint)Math.Max(1, (int)MathF.Ceiling(h / 2f)), 
                        1, 
                        1, 
                        1, 
                        viewOpaqueColorMap.Format, 
                        Veldrid.TextureUsage.Storage | Veldrid.TextureUsage.Sampled, Veldrid.TextureType.Texture2D
                    )
                );   
                bloomBluredMaps[i] = GraphicsDevice.ResourceFactory.CreateTextureView(texture);

                texture = GraphicsDevice.ResourceFactory.CreateTexture(
                    new Veldrid.TextureDescription(
                        (uint)w, 
                        (uint)h, 
                        1, 
                        1, 
                        1, 
                        Veldrid.PixelFormat.R32_Float, 
                        Veldrid.TextureUsage.Storage | Veldrid.TextureUsage.Sampled, Veldrid.TextureType.Texture2D
                    )
                );
                viewDepthDownresMaps[i] = GraphicsDevice.ResourceFactory.CreateTextureView(texture);

                w = Math.Max(1, (int)MathF.Ceiling(w / 2f));
                h = Math.Max(1, (int)MathF.Ceiling(h / 2f));
            }  

            w = h = 128;
            viewLuminanceMaps = new Veldrid.TextureView[8];
            for(int i = 0; i < viewLuminanceMaps.Length; i++) {
                texture = GraphicsDevice.ResourceFactory.CreateTexture(
                    new Veldrid.TextureDescription(
                        (uint)w, 
                        (uint)h, 
                        1, 
                        1, 
                        1, 
                        Veldrid.PixelFormat.R16_Float, 
                        Veldrid.TextureUsage.Storage | Veldrid.TextureUsage.Sampled, Veldrid.TextureType.Texture2D
                    )
                );
                viewLuminanceMaps[i] = GraphicsDevice.ResourceFactory.CreateTextureView(texture);       
                w /= 2;
                h /= 2;         
            } 

            autoExposureMaps = new Veldrid.TextureView[2];
            for(int i = 0; i < autoExposureMaps.Length; i++) {
                texture = GraphicsDevice.ResourceFactory.CreateTexture(
                    new Veldrid.TextureDescription(
                        1, 
                        1, 
                        1, 
                        1, 
                        1, 
                        Veldrid.PixelFormat.R16_Float, 
                        Veldrid.TextureUsage.Storage | Veldrid.TextureUsage.Sampled, Veldrid.TextureType.Texture2D
                    )
                );
                autoExposureMaps[i] = GraphicsDevice.ResourceFactory.CreateTextureView(texture);          
            } 
        }

        uint usedDynamicUniformBytes = 0;
        public void UpdateConstants(Veldrid.CommandList cl, IntPtr data, uint sizeInBytes, out uint uniformOffset) {
            if(sizeInBytes + usedDynamicUniformBytes > DynamicUniformBufferSizePerFrame) {
                throw new OutOfMemoryException("");
            }
            uniformOffset = usedDynamicUniformBytes;
            usedDynamicUniformBytes += sizeInBytes;
            IntPtr dst = IntPtr.Add(mappedDynamicUniformBuffer.Data, (int)uniformOffset);
            SharpDX.Utilities.CopyMemory(dst, data, (int)sizeInBytes);
        }

        public void UpdateConstants<T>(Veldrid.CommandList cl, T data, out uint uniformOffset) where T: struct {
            uint sizeInBytes = (uint)MathHelper.Align(Utilities.SizeOf<T>(), (int)GraphicsDevice.gd.UniformBufferMinOffsetAlignment);
            if(sizeInBytes + usedDynamicUniformBytes > DynamicUniformBufferSizePerFrame) {
                throw new OutOfMemoryException("");
            }
            uniformOffset = usedDynamicUniformBytes;
            usedDynamicUniformBytes += sizeInBytes;
            DataStream cbuffer = new DataStream(IntPtr.Add(mappedDynamicUniformBuffer.Data, (int)uniformOffset), (int)sizeInBytes, false, true);
            cbuffer.Write(data);
        }        

        public unsafe void AllocConstants(Veldrid.CommandList cl, int numVectors, out Span<Vector4> constants, out uint uniformOffset) {
            uint sizeInBytes = (uint)MathHelper.Align((numVectors * SharpDX.Utilities.SizeOf<SharpDX.Vector4>()), (int)GraphicsDevice.gd.UniformBufferMinOffsetAlignment);
            numVectors = (int)(sizeInBytes / Utilities.SizeOf<Vector4>());
            if(sizeInBytes + usedDynamicUniformBytes > DynamicUniformBufferSizePerFrame) {
                throw new OutOfMemoryException("");
            }
            uniformOffset = usedDynamicUniformBytes;
            usedDynamicUniformBytes += sizeInBytes;    
            constants = new Span<SharpDX.Vector4>((void*)IntPtr.Add(mappedDynamicUniformBuffer.Data, (int)uniformOffset), numVectors);        
        }

        public void BackendBeginFrame() {
            frameRenderResources.MapDynamicBuffers();
            mappedDynamicUniformBuffer = GraphicsDevice.gd.Map(dynamicUniformBuffer, Veldrid.MapMode.Write);
        }

        public void BackendEndFrame() {
            usedDynamicUniformBytes = 0;
            GraphicsDevice.gd.Unmap(dynamicUniformBuffer);
            frameRenderResources.UnmapDynamicBuffers();
        }

        public RenderPass FindRenderPass(string name) {
            renderPasses.TryGetValue(name, out RenderPass pass);
            return pass;
        }

        public void AddRenderPass(RenderPass inPass) {
            renderPasses.TryAdd(inPass.name, inPass);
        }

        public void SetupPasses() {
            foreach(var pass in renderPasses) {
                pass.Value.Setup();
            }
        }

        public void PostInit() {
            foreach(var pass in renderPasses) {
                pass.Value.PostSetup();
            }
            builtinComputeShader = Common.declManager.FindComputeShader("builtin/computeShaders");
            skylightingComputeShader = Common.declManager.FindComputeShader("builtin/skylighting");
            compositeComputeShader = Common.declManager.FindComputeShader("builtin/composite");
            deferredProbesComputeShader = Common.declManager.FindComputeShader("builtin/deferredProbes");
            volumetricLightingComputeShader = Common.declManager.FindComputeShader("builtin/volumetricLighting");  
            taaComputeShader = Common.declManager.FindComputeShader("builtin/taa");        
            ssrComputeShader = Common.declManager.FindComputeShader("builtin/ssr");
        }

        public void Shutdown() {
            viewDepthMap.Dispose();
            viewOpaqueColorMap.Dispose();

            foreach(var pass in renderPasses) {
                pass.Value?.Dispose();
            }
            frameRenderResources.Shutdown();
            dynamicUniformBuffer?.Dispose();
        }
    }
}