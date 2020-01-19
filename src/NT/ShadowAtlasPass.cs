using System;
using SharpDX;

namespace NT
{
    internal class ShadowAtlasPass : RenderPass {
        public static readonly Vector4 AtlasResolution = new Vector4(8192f, 8192f, 1f / 8192f, 1f / 8192f);

        readonly Veldrid.DeviceBuffer viewUniformBuffer;

        public ShadowAtlasPass(FrameGraph myFrameGraph, string myName, Veldrid.DeviceBuffer viewUniformBuffer, Veldrid.Texture atlasMap) : base(myFrameGraph, myName) {
            framebuffer = GraphicsDevice.ResourceFactory.CreateFramebuffer(new Veldrid.FramebufferDescription(atlasMap));
            this.viewUniformBuffer = viewUniformBuffer;
        }

        RenderState cachedRenderState;

        void RenderShadowView(ViewDef viewDef, ShadowViewDef def, uint dynamicUniformOffset, Veldrid.CommandList commandList) {
            commandList.PushDebugGroup("RenderShadowView");
            commandList.UpdateBuffer(viewUniformBuffer, 0, def.viewConstants);
            commandList.SetViewport(0, new Veldrid.Viewport(def.x, def.y, def.width, def.height, 0f, 1f));
            commandList.SetScissorRect(0, (uint)def.x, (uint)def.y, (uint)def.width, (uint)def.height);

            var opaqueSurfaces = viewDef.shadowReceiverSurfaces.opaqueSurfaces;
            var alphaTestSurfaces = viewDef.shadowReceiverSurfaces.alphaTestSurfaces;
            for(int surfaceIndex = 0; surfaceIndex < opaqueSurfaces.Count; surfaceIndex++) {
                var surface = opaqueSurfaces[surfaceIndex];
                DrawSurface(commandList, dynamicUniformOffset, surface.space, surface.drawInfo, surface.shadowMaterial, true);
            }  
            for(int surfaceIndex = 0; surfaceIndex < alphaTestSurfaces.Count; surfaceIndex++) {
                var surface = alphaTestSurfaces[surfaceIndex];
                DrawSurface(commandList, dynamicUniformOffset, surface.space, surface.drawInfo, surface.shadowMaterial, true);
            }     
            commandList.PopDebugGroup();
        }

        unsafe public void Render(ViewDef view, uint dynamicUniformOffset, Veldrid.CommandList commandList) {
            if(view.numShadowViews == 0) {
                return;
            }
            Span<ShadowViewDef> defs = new Span<ShadowViewDef>(view.shadowViewDefs.ToPointer(), view.numShadowViews);
            commandList.PushDebugGroup("ShadowAtlasPass");
            commandList.SetFramebuffer(framebuffer);
            commandList.ClearDepthStencil(1f);
            for(int i = 0; i < defs.Length; i++) {
                if(defs[i].numOpaqueSurfaces > 0 || defs[i].numAlphaTestSurfaces > 0) {
                    RenderShadowView(view, defs[i], dynamicUniformOffset, commandList);
                }
            }
            commandList.PopDebugGroup();       
        }

        public override void Dispose() {
            framebuffer?.Dispose();
        }

        public override void Setup() {}
        public override void PostSetup() {}        
    }
}