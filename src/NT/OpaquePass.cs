using SharpDX;

namespace NT
{
    internal class OpaquePass : RenderPass {
        Veldrid.DeviceBufferRange instanceConstantBuffer;

        public OpaquePass(FrameGraph myFrameGraph, string myName) : base(myFrameGraph, myName) {
            framebuffer = GraphicsDevice.ResourceFactory.CreateFramebuffer(
                new Veldrid.FramebufferDescription(
                    myFrameGraph.viewDepthMap.Target, 
                    myFrameGraph.viewOpaqueColorMap.Target,
                    myFrameGraph.viewSpecularMap.Target,
                    myFrameGraph.viewNormalMap.Target)
            );
        }

        RenderState cachedRenderState;

        public void Render(ViewDef viewDef, uint dynamicUniformOffset, Veldrid.CommandList commandList) {
            commandList.PushDebugGroup("OpaquePass");
            commandList.SetFramebuffer(framebuffer);
            commandList.SetFullScissorRects();
            commandList.SetFullViewports();                     
            commandList.ClearColorTarget(0, new Veldrid.RgbaFloat(0.5f, 0.5f, 0.5f, 1f));
            commandList.ClearColorTarget(1, new Veldrid.RgbaFloat(0, 0, 0, 0));
            commandList.ClearColorTarget(2, new Veldrid.RgbaFloat(0, 0, 0, 0));

            var opaqueSurfaces = viewDef.dynamicSurfaces.opaqueSurfaces;
            var alphaTestSurfaces = viewDef.dynamicSurfaces.alphaTestSurfaces;
            for(int surfaceIndex = 0; surfaceIndex < opaqueSurfaces.Count; surfaceIndex++) {
                var surface = opaqueSurfaces[surfaceIndex];
                DrawSurface(commandList, dynamicUniformOffset, surface.space, surface.drawInfo, surface.material, false);
            }  
            for(int surfaceIndex = 0; surfaceIndex < alphaTestSurfaces.Count; surfaceIndex++) {
                var surface = alphaTestSurfaces[surfaceIndex];
                DrawSurface(commandList, dynamicUniformOffset, surface.space, surface.drawInfo, surface.material, false);
            } 

            opaqueSurfaces = viewDef.staticSurfaces.opaqueSurfaces;
            alphaTestSurfaces = viewDef.staticSurfaces.alphaTestSurfaces;
            for(int surfaceIndex = 0; surfaceIndex < opaqueSurfaces.Count; surfaceIndex++) {
                var surface = opaqueSurfaces[surfaceIndex];
                DrawSurface(commandList, dynamicUniformOffset, surface.space, surface.drawInfo, surface.material, false);
            }  
            for(int surfaceIndex = 0; surfaceIndex < alphaTestSurfaces.Count; surfaceIndex++) {
                var surface = alphaTestSurfaces[surfaceIndex];
                DrawSurface(commandList, dynamicUniformOffset, surface.space, surface.drawInfo, surface.material, false);
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