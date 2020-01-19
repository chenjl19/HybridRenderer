using System;
using System.Collections.Generic;
using SharpDX;

namespace NT
{
    
    public struct ScreenPicVertex {
        public Vector2 position;
        public Vector2 texcoord;

        public ScreenPicVertex(Vector2 p, Vector2 st) {
            position = p;
            texcoord = st;
        }
    }
    
    public class PresentPass : RenderPass {
        MaterialRenderProxy[] blitMaterials;
        const int MaxPicsOnScreen = 128;
        int numScreenPics;
        ScreenPicVertex[] picVertices;
        UInt16[] picIndices;
        MaterialRenderProxy[] picMaterials;
        VertexBufferRef picVertexBuffer;
        IndexBufferRef picIndexBuffer;
        Vector4[] picDrawParms;

        public PresentPass(FrameGraph myFrameGraph, Veldrid.Swapchain swapchain, string myName) : base(myFrameGraph, myName) {
            SetFramebuffer(swapchain);
            picVertices = new ScreenPicVertex[MaxPicsOnScreen * 4];
            picIndices = new ushort[MaxPicsOnScreen * 6];
            picMaterials = new MaterialRenderProxy[MaxPicsOnScreen];
            picVertexBuffer = new VertexBufferRef();
            picIndexBuffer = new IndexBufferRef();
            picDrawParms = new Vector4[MaxPicsOnScreen];
        }

        public override void Dispose() {
        }

        public override void Setup() {
        }

        public override void PostSetup() {
            blitMaterials = new MaterialRenderProxy[2];
            var material = new Material(Common.declManager.FindShader("builtin/FinalBlit"));
            ImageWraper viewColorMap = new ImageWraper(frameGraph.viewColorMaps[0].Target);
            ImageWraper viewLastFrameColorMap = new ImageWraper(frameGraph.viewColorMaps[1].Target);
            ImageWraper autoExposureMap = new ImageWraper(frameGraph.autoExposureMaps[0].Target);
            ImageWraper lastFrameAutoExposureMap = new ImageWraper(frameGraph.autoExposureMaps[1].Target);            
            ImageWraper bloomMap = new ImageWraper(frameGraph.bloomBluredMaps[0].Target);
            material.SetMainImage(viewColorMap);
            material.SetImage("_AutoExposureTex", autoExposureMap);
            material.SetImage("_BloomTex", bloomMap);
            material.SetSampler("_PointClampSampler", GraphicsDevice.PointClampSampler);
            material.SetSampler("_LinearClampSampler", GraphicsDevice.LinearClampSampler);
            blitMaterials[0] = material.MakeLightingProxy();
            material.SetMainImage(viewLastFrameColorMap);
            material.SetImage("_AutoExposureTex", lastFrameAutoExposureMap);
            blitMaterials[1] = material.MakeLightingProxy();
        }

        public void AddScreenPic(int x, int y, int w, int h, MaterialRenderProxy material, Vector4 parms) {
            Vector2 scale = new Vector2(2f / (float)framebuffer.Width, -2f / (float)framebuffer.Height);
            Vector2 translate = new Vector2(-1f, 1f);
            Span<ScreenPicVertex> quad = new Span<ScreenPicVertex>(picVertices, numScreenPics * 4, 4);
            Span<UInt16> indices = new Span<ushort>(picIndices, numScreenPics * 6, 6);
            quad[0].position = new Vector2(x, y) * scale + translate;
            quad[0].texcoord = new Vector2(0f, 0f);
            quad[1].position = new Vector2(x + w, y) * scale + translate;
            quad[1].texcoord = new Vector2(1f, 0f);
            quad[2].position = new Vector2(x, y + h) * scale + translate;
            quad[2].texcoord = new Vector2(0f, 1f);
            quad[3].position = new Vector2(x + w, y + h) * scale + translate;
            quad[3].texcoord = new Vector2(1f, 1f);
            indices[0] = 0;
            indices[1] = 1;
            indices[2] = 2;
            indices[3] = 1;
            indices[4] = 3;
            indices[5] = 2;
            picMaterials[numScreenPics] = material;
            picDrawParms[numScreenPics] = parms;
            ++numScreenPics;
        }

        public void SetFramebuffer(Veldrid.Swapchain swapchain) {
            framebuffer = swapchain.Framebuffer;
        }

        //ImageWraper autoExposureMap = new ImageWraper();
        public void Render(Veldrid.CommandList commandList, int currentFrame) {
            //autoExposureMap.SetDeviceTexture(_autoExposureMap);
            //material.SetImage("_AutoExposureTex", autoExposureMap);
            //finalBlitMaterial = material.MakeLightingProxy();            

            commandList.PushDebugGroup("Present pass");
            commandList.SetFramebuffer(framebuffer);
            commandList.SetFullViewports();
            commandList.SetFullScissorRects();
            commandList.SetPipeline(blitMaterials[currentFrame].renderState.pipeline);
            uint resourceSetSlot = 0;
            if(blitMaterials[currentFrame].renderPassResourceSet != null) {
                commandList.SetGraphicsResourceSet(resourceSetSlot, blitMaterials[currentFrame].renderPassResourceSet);
                resourceSetSlot++;
            }
            commandList.SetGraphicsResourceSet(resourceSetSlot, blitMaterials[currentFrame].materialResourceSet);
            commandList.Draw(4);  

            if(numScreenPics > 0) {
                frameGraph.frameRenderResources.AllocVertices(picVertices, ref picVertexBuffer);
                frameGraph.frameRenderResources.AllocIndices(picIndices, ref picIndexBuffer);
                commandList.SetVertexBuffer(0, picVertexBuffer.bufferObject, (uint)picVertexBuffer.offsetInOtherInBytes);
                commandList.SetIndexBuffer(picIndexBuffer.bufferObject, picIndexBuffer.indexFormat, (uint)picIndexBuffer.offsetInOtherInBytes);
                for(int picIndex = 0; picIndex < numScreenPics; picIndex++) {
                    MaterialRenderProxy material = picMaterials[picIndex];
                    GPURenderState renderState = picMaterials[picIndex].renderState;
                    commandList.SetPipeline(renderState.pipeline);
                    resourceSetSlot = 0;
                    if(material.renderPassResourceSet != null) {
                        commandList.SetGraphicsResourceSet(resourceSetSlot, material.renderPassResourceSet);
                        resourceSetSlot++;
                    }
                    if(material.materialResourceSet != null) {
                        commandList.SetGraphicsResourceSet(resourceSetSlot, material.materialResourceSet);
                        resourceSetSlot++;
                    }
                    if(material.dynamicUniformBufferResourceSet != null) {
                        if(material.uniformBlocks != null) {
                            frameGraph.AllocConstants(commandList, 1, out var constants, out uint dynamicOffsets);
                            constants[0] = picDrawParms[picIndex];
                            commandList.SetGraphicsResourceSet(resourceSetSlot, material.dynamicUniformBufferResourceSet, 1, ref dynamicOffsets);
                        }
                    }
                    commandList.DrawIndexed(6, 1, (uint)picIndex * 6, picIndex * 4, 0);
                }
                numScreenPics = 0;
            }   

            commandList.PopDebugGroup();
        }
    }
}