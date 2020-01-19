using SharpDX;

namespace NT
{
    internal class DepthPass : RenderPass {
        public DepthPass(FrameGraph myFrameGraph, string myName) : base(myFrameGraph, myName) {
            framebuffer = GraphicsDevice.ResourceFactory.CreateFramebuffer(new Veldrid.FramebufferDescription(myFrameGraph.viewDepthMap.Target));
        }

        public override void Dispose() {
            framebuffer?.Dispose();
        }

        public override void Setup() {}
        public override void PostSetup() {}

        //RenderState cachedRenderState;

        public void Render(ViewDef viewDef, uint dynamicUniformOffset, Veldrid.CommandList commandList) {
            commandList.PushDebugGroup("DepthOnlyPass");
            commandList.SetFramebuffer(framebuffer);
            commandList.SetViewport(0, new Veldrid.Viewport(0, 0, 1920f, 1080f, 0f, 1f));
            commandList.SetScissorRect(0, 0, 0, 1920, 1080);
            commandList.ClearDepthStencil(1, 0);

            var opaqueSurfaces = viewDef.dynamicSurfaces.opaqueSurfaces;
            var alphaTestSurfaces = viewDef.dynamicSurfaces.alphaTestSurfaces;
            for(int surfaceIndex = 0; surfaceIndex < opaqueSurfaces.Count; surfaceIndex++) {
                var surface = opaqueSurfaces[surfaceIndex];
                if(surface.prezMaterial.renderState != null) {
                    DrawSurface(commandList, dynamicUniformOffset, surface.space, surface.drawInfo, surface.prezMaterial, false);
                    //DrawDynamicSurfaceDepth(commandList, dynamicUniformOffset, surface.space, surface.drawInfo, surface.prezMaterial, 2);
                }
            }  
            for(int surfaceIndex = 0; surfaceIndex < alphaTestSurfaces.Count; surfaceIndex++) {
                var surface = alphaTestSurfaces[surfaceIndex];
                if(surface.prezMaterial.renderState != null) {
                    DrawSurface(commandList, dynamicUniformOffset, surface.space, surface.drawInfo, surface.prezMaterial, false);
                    //DrawDynamicSurfaceDepth(commandList, dynamicUniformOffset, surface.space, surface.drawInfo, surface.prezMaterial, 3);
                }
            }  

            opaqueSurfaces = viewDef.staticSurfaces.opaqueSurfaces;
            alphaTestSurfaces = viewDef.staticSurfaces.alphaTestSurfaces;
            for(int surfaceIndex = 0; surfaceIndex < opaqueSurfaces.Count; surfaceIndex++) {
                var surface = opaqueSurfaces[surfaceIndex];
                if(surface.prezMaterial.renderState != null) {
                    DrawSurface(commandList, dynamicUniformOffset, surface.space, surface.drawInfo, surface.prezMaterial, true);
                }
            }  
            for(int surfaceIndex = 0; surfaceIndex < alphaTestSurfaces.Count; surfaceIndex++) {
                var surface = alphaTestSurfaces[surfaceIndex];
                if(surface.prezMaterial.renderState != null) {
                    DrawSurface(commandList, dynamicUniformOffset, surface.space, surface.drawInfo, surface.prezMaterial, true);
                }
            }  
            commandList.PopDebugGroup();             
        }
    }
}