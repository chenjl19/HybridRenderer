using System;
using SharpDX;

namespace NT
{
    internal class SkybackgroundPass : RenderPass {
        public override void PostSetup() {}
        public override void Dispose() {}
        public override void Setup() {}   

        public SkybackgroundPass(FrameGraph myFrameGraph, string myName) : base(myFrameGraph, myName) {
            framebuffer = GraphicsDevice.ResourceFactory.CreateFramebuffer(new Veldrid.FramebufferDescription(frameGraph.viewDepthMap.Target, frameGraph.compositeOutputMap.Target));
        }

        unsafe public void Render(CommandBuffer commandBuffer) {
            if(Scene.GlobalScene.skyboxRenderProxy.renderState == null) {
                return;
            }
            Veldrid.CommandList commandList = commandBuffer.commandList;
            commandList.SetFramebuffer(framebuffer);
            commandList.SetFullViewports();
            commandList.SetFullScissorRects();

            commandList.SetPipeline(Scene.GlobalScene.skyboxRenderProxy.renderState.pipeline);
            commandBuffer.context1.InputAssembler.InputLayout = null;
            uint slot = 0;
            if(Scene.GlobalScene.skyboxRenderProxy.renderPassResourceSet != null) {
                commandList.SetGraphicsResourceSet(slot, Scene.GlobalScene.skyboxRenderProxy.renderPassResourceSet);
                slot++;
            }   
            if(Scene.GlobalScene.skyboxRenderProxy.materialResourceSet != null) {
                commandList.SetGraphicsResourceSet(slot, Scene.GlobalScene.skyboxRenderProxy.materialResourceSet);
            }    
            commandList.Draw(240);     
        }     
    }

    internal class TransparencyPass : RenderPass {
        Veldrid.DeviceBuffer particleVertexBuffer;
        Veldrid.DeviceBuffer particleIndexBuffer;
        MaterialRenderProxy particleMaterial;

        public override void PostSetup() {
            Material material = new Material(Common.declManager.FindShader("builtin/particle"));
            particleMaterial = material.MakeLightingProxy();
        }

        public TransparencyPass(FrameGraph myFrameGraph, string myName) : base(myFrameGraph, myName) {
            particleVertexBuffer = GraphicsDevice.ResourceFactory.CreateBuffer(new Veldrid.BufferDescription((uint)(Utilities.SizeOf<DrawVertex>() * 16384), Veldrid.BufferUsage.VertexBuffer));
            particleIndexBuffer = GraphicsDevice.ResourceFactory.CreateBuffer(new Veldrid.BufferDescription((uint)(Utilities.SizeOf<UInt16>() * 24567), Veldrid.BufferUsage.IndexBuffer));
            framebuffer = GraphicsDevice.ResourceFactory.CreateFramebuffer(new Veldrid.FramebufferDescription(frameGraph.viewDepthMap.Target, frameGraph.compositeOutputMap.Target));
        }

        public void UpdateResources(SceneRenderData renderData, Veldrid.CommandList commandList) {
            if(renderData.numParticleVertices > 0 && renderData.numParticleIndices > 0) {
                commandList.UpdateBuffer(particleVertexBuffer, 0, renderData.particleVertexBuffer, (uint)(Utilities.SizeOf<DrawVertex>() * renderData.numParticleVertices));
                commandList.UpdateBuffer(particleIndexBuffer, 0, renderData.particleIndexBuffer, (uint)(Utilities.SizeOf<UInt16>() * renderData.numParticleIndices));
            }
        }

        unsafe public void Render(ViewDef view, uint dynamicUniformOffset, CommandBuffer commandBuffer) {
/*
            Veldrid.CommandList commandList = commandBuffer.commandList;
            commandList.SetFramebuffer(framebuffer);
            commandList.SetFullViewports();
            commandList.SetFullScissorRects();

            commandList.SetPipeline(particleMaterial.renderState.pipeline);
            if(particleMaterial.renderPassResourceSet != null) {
                commandList.SetGraphicsResourceSet(0, particleMaterial.renderPassResourceSet);
            }
            commandList.SetVertexBuffer(0, particleVertexBuffer);
            commandList.SetIndexBuffer(particleIndexBuffer, Veldrid.IndexFormat.UInt16);
            ReadOnlySpan<ParticleStageDrawSurface> surfaces = new ReadOnlySpan<ParticleStageDrawSurface>(view.particleStageDrawSurfaces.ToPointer(), view.numParticleStageDrawSurfaces);
            for(int i = 0; i < surfaces.Length; i++) {
                commandList.DrawIndexed((uint)surfaces[i].numIndices, 1, (uint)surfaces[i].indexOffset, surfaces[i].vertexOffset, 0);
            }
*/
        }

        public override void Dispose() {
        }

        public override void Setup() {}
    }
}