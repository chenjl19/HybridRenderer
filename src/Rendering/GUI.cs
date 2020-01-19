using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct3D;
using ImGuiNET;

using NVGcolor = SharpDX.Vector4;
using NVGVertex = SharpDX.Vector4;
/*
namespace NT
{
    public enum NVGDrawCallType {
        Fill,
        ConvexFill,
        Stroke,
        Triangles
    }

    public class NVGDrawCall {
        public NVGDrawCallType type;
        public Image2D image;
        public int numPaths;
        public int vertexOffset;
        public int numVertices;
        public int uniformBlockOffset;
        public int numUniformBlocks;
        public IntPtr paths;
    }

    public struct NVGDrawPath {
        public int fillOffset;
        public int numFills;
        public int strokeOffset;
        public int numStrokes;
    }

    [StructLayout(LayoutKind.Sequential)]
    struct NVGConstants {
        public Matrix scissorMat;
        public Matrix paintMat;
        public NVGcolor innerCol;
        public NVGcolor outerCol;
        public Vector4 scissorParams; // ExtScale
        public Vector4 packedParams; // Extent, Radius
        public Vector4 packedParams1; // feather, strokeMult, texType, type
        public Vector4 viewParams;

        public void SetViewSize(float x, float y) {
            viewParams[0] = x;
            viewParams[1] = y;
        }

        public void SetScissorExt(float x, float y) {
            scissorParams[0] = x;
            scissorParams[1] = y;
        }

        public void SetScissorScale(float x, float y) {
            scissorParams[2] = x;
            scissorParams[3] = y;
        }

        public void SetExtent(float x, float y) {
            packedParams[0] = x;
            packedParams[1] = y;
        }

        public void SetRadius(float radius) {
            packedParams[2] = radius;
        }

        public void SetStrokeThreshold(float threshold) {
            packedParams[3] = threshold;
        }

        public void SetFeather(float feather) {
            packedParams1[0] = feather;
        }

        public void SetStrokeMult(float strokeMult) {
            packedParams1[1] = strokeMult;
        }

        public void SetTextureType(int type) {
            packedParams1[2] = (float)type;
        }

        public void SetShaderType(NVGShaderType type) {
            packedParams1[3] = (float)type;
        }

        public static readonly int SizeInBytes = MathHelper.Align(Utilities.SizeOf<NVGConstants>(), 256);
    }

    public class NVGRenderData {
        public int numVertices;
        public int numUniformBlocks;
        public IntPtr vertexBuffer;
        public IntPtr uniformBuffer;
        public NVGDrawCall[] drawCalls;
        public NVGImageCopyCommand[] imageCopyCommands;
    }

    public class NVGImageCopyCommand {
        public int x, y, w, h;
        public IntPtr data;
        public int dataSize;
        public Image2D destImage;
    }

    public unsafe class MyNVGParameters : NVGParameters {
        List<NVGImage> images = new List<NVGImage>();
        List<NVGDrawCall> drawCalls = new List<NVGDrawCall>();
        List<NVGImageCopyCommand> imageCopyCommands = new List<NVGImageCopyCommand>();
        int maxNumVertices;
        int numVertices;
        int maxNumUniformBlocks;
        int numUniformBlocks;
        IntPtr vertexBuffer;
        IntPtr uniformBuffer;

        class NVGImage {
            public Image2D handle;
            public NVGtextureType type;
            public NVGImageFlags flags;
        }

        void D3Dnvg__xformToMat3x3(float[] m3, float[] t) {
            m3[0] = t[0];
            m3[1] = t[1];
            m3[2] = 0.0f;
            m3[3] = t[2];
            m3[4] = t[3];
            m3[5] = 0.0f;
            m3[6] = t[4];
            m3[7] = t[5];
            m3[8] = 1.0f;
        }

        NVGcolor D3Dnvg__premulColor(NVGcolor c) {
            c.X *= c.W;
            c.Y *= c.W;
            c.Z *= c.W;
            return c;
        }

        void D3Dnvg_copyMatrix3to4(ref Matrix m, float[] src) {
            m.M11 = src[0];
            m.M12 = src[1];
            m.M13 = src[2];
            m.M21 = src[3];
            m.M22 = src[4];
            m.M23 = src[5];
            m.M31 = src[6];
            m.M32 = src[7];
            m.M33 = src[8];
        }

        void xformToMatrix4x4(ref Matrix m, float[] src) {
            m = Matrix.Identity;
            m.M11 = src[0];
            m.M12 = src[1];
            m.M13 = 0f;
            m.M14 = 0f;
            m.M21 = src[2];
            m.M22 = src[3];
            m.M23 = 0f;
            m.M24 = 0f;
            m.M31 = src[4];
            m.M32 = src[5];
            m.M33 = 1f;
            //m.Transpose();
        }

        int AllocVertices(int n) {
            if(numVertices + n > maxNumVertices || vertexBuffer == IntPtr.Zero) {
                int vertexCount = Math.Max(numVertices + n, 4096) + maxNumVertices / 2;
                IntPtr buffer = FrameAllocator.AllocMemory(Utilities.SizeOf<NVGVertex>() * vertexCount);
                if(numVertices > 0) {
                    Utilities.CopyMemory(buffer, vertexBuffer, numVertices * Utilities.SizeOf<NVGVertex>());
                }
                vertexBuffer = buffer;
                maxNumVertices = vertexCount;
            }
            int offset = numVertices;
            numVertices += n;
            return offset;
        }

        int AllocUniformBlock(int n) {
            if(numUniformBlocks + n > maxNumUniformBlocks || uniformBuffer == IntPtr.Zero) {
                int blockCount = Math.Max(numUniformBlocks + n, 64) + maxNumUniformBlocks / 2;
                IntPtr buffer = FrameAllocator.AllocMemory(NVGConstants.SizeInBytes * blockCount);
                if(numUniformBlocks > 0) {
                    Utilities.CopyMemory(buffer, uniformBuffer, numUniformBlocks * NVGConstants.SizeInBytes);
                }
                uniformBuffer = buffer;
                maxNumUniformBlocks = blockCount;
            }
            int offset = numUniformBlocks;
            numUniformBlocks += n;
            return offset;            
        }

        int ConvertPaint(NVGConstants* frag, NVGPaint* paint, NVGScissor* scissor, float width, float fringe, float strokeThr) {
            frag->SetViewSize(viewWidth, viewHeight);
            
            NVGImage tex = null;

            float[] invxform = new float[6];
            float[] paintMat = {paint->XForm1, paint->XForm2, paint->XForm3, paint->XForm4, paint->XForm5, paint->XForm6};
            float[] scissorMat = { scissor->XForm1, scissor->XForm2, scissor->XForm3, scissor->XForm4, scissor->XForm5, scissor->XForm6 };

            frag->innerCol = D3Dnvg__premulColor(paint->InnerColor);
            frag->outerCol = D3Dnvg__premulColor(paint->OuterColor);

            if (scissor->Extent1 < -0.5f || scissor->Extent2 < -0.5f) {
                frag->scissorMat = Matrix.Zero;
                frag->SetScissorExt(1f, 1f);
                frag->SetScissorScale(1f, 1f);
            } else {
                fixed(float* inv = invxform, t = scissorMat) {
                    NVG.TransformInverse(inv, t);
                }
                xformToMatrix4x4(ref frag->scissorMat, invxform);
                frag->SetScissorExt(scissor->Extent1, scissor->Extent2);
                frag->SetScissorScale(MathF.Sqrt(scissor->XForm1 * scissor->XForm1 + scissor->XForm3 * scissor->XForm3) / fringe, MathF.Sqrt(scissor->XForm2 * scissor->XForm2 + scissor->XForm4 * scissor->XForm4) / fringe);
            }

            frag->SetExtent(paint->Extent1, paint->Extent2);
            frag->SetStrokeMult((width * 0.5f + fringe * 0.5f) / fringe);
            frag->SetStrokeThreshold(strokeThr);

            if (paint->Image != 0) {
                tex = FindImage(paint->Image);
                if (tex == null) {
                    return 0;
                }

                if ((tex.flags & NVGImageFlags.NVG_IMAGE_FLIPY) != 0) {
                    float[] flipped = new float[6];
                    fixed (float* flippedPtr = flipped, paintXFormPtr = paintMat, invXFormPtr = invxform) {
                        NVG.TransformScale(flippedPtr, 1.0f, -1.0f);
                        NVG.TransformMultiply(flippedPtr, paintXFormPtr);
                        NVG.TransformInverse(invXFormPtr, flippedPtr);
                    }
                } else {
                    fixed(float *invxformPtr = invxform, paintXFormPtr = paintMat) {
                        NVG.TransformInverse(invxformPtr, paintXFormPtr);
                    }
                }
 
                frag->SetShaderType(NVGShaderType.FillImage);

                if (tex.type == NVGtextureType.NVG_TEXTURE_RGBA) {
                    frag->SetTextureType((tex.flags & NVGImageFlags.NVG_IMAGE_PREMULTIPLIED) != 0 ? 0 : 1);
                } else {
                    frag->SetTextureType(2);
                }
            } else {
                frag->SetShaderType(NVGShaderType.FillGrad);
                frag->SetRadius(paint->Radius);
                frag->SetFeather(paint->Feather);
                fixed(float *invxformPtr = invxform, paintXFormPtr = paintMat) {
                    NVG.TransformInverse(invxformPtr, paintXFormPtr);
                }
            }
            xformToMatrix4x4(ref frag->paintMat, invxform);
            //D3Dnvg__xformToMat3x3(paintMat, invxform);
            //D3Dnvg_copyMatrix3to4(ref frag->paintMat, paintMat);

            return 1;
        }

        int ConvertPaint2(NVGConstants* frag, NVGPaint* paint, NVGScissor* scissor, float width, float fringe, float strokeThr) {
            frag->SetViewSize(viewWidth, viewHeight);
            
            NVGImage tex = null;

            float[] invxform = new float[6];
            float[] paintMat = new float[9];
            float[] scissorMat = new float[9];

            float[] xformMat = new float[] {
                scissor->XForm1, scissor->XForm2, scissor->XForm3, scissor->XForm4, scissor->XForm5, scissor->XForm6
            };

            fixed(float* invxformPtr = invxform, paintMatPtr = paintMat, scissorMatPtr = scissorMat, xformMatPtr = xformMat) {

                frag->innerCol = D3Dnvg__premulColor(paint->InnerColor);
                frag->outerCol = D3Dnvg__premulColor(paint->OuterColor);
                
                if (scissor->Extent1 < -0.5f || scissor->Extent2 < -0.5f)  {
                    frag->scissorMat = Matrix.Zero;
                    frag->SetScissorExt(1f, 1f);
                    frag->SetScissorScale(1f, 1f);
                } else  {
                    NVG.TransformInverse(invxformPtr, xformMatPtr);
                    D3Dnvg__xformToMat3x3(scissorMat, invxform);
                    frag->SetScissorExt(scissor->Extent1, scissor->Extent2);
                    frag->SetScissorScale(MathF.Sqrt(scissor->XForm1 * scissor->XForm1 + scissor->XForm3 * scissor->XForm3) / fringe, MathF.Sqrt(scissor->XForm2 * scissor->XForm2 + scissor->XForm4 * scissor->XForm4) / fringe);
                    D3Dnvg_copyMatrix3to4(ref frag->scissorMat, scissorMat);
                }
                
                frag->SetExtent(paint->Extent1, paint->Extent2);
                frag->SetStrokeMult((width * 0.5f + fringe * 0.5f) / fringe);
                frag->SetStrokeThreshold(strokeThr);

                if (paint->Image != 0) {
                    tex = FindImage(paint->Image);
                    if (tex == null) {
                        return 0;
                    }
                    
                    if ((tex.flags & NVGImageFlags.NVG_IMAGE_FLIPY) != 0)  {
                        float[] flipped = new float[6];
                        fixed(float* flippedPtr = flipped) {
                            NVG.TransformScale(flippedPtr, 1.0f, -1.0f);
                            NVG.TransformMultiply(flippedPtr, xformMatPtr);
                            NVG.TransformInverse(invxformPtr, flippedPtr);
                        }
                    }  else {
                        NVG.TransformInverse(invxformPtr, xformMatPtr);
                    }

                    frag->SetShaderType(NVGShaderType.FillImage);

                    if (tex.type == NVGtextureType.NVG_TEXTURE_RGBA) {
                        frag->SetTextureType((tex.flags & NVGImageFlags.NVG_IMAGE_PREMULTIPLIED) != 0 ? 0 : 1);
                    } else {
                        frag->SetTextureType(2);
                    }
                } else  {
                    frag->SetShaderType(NVGShaderType.FillGrad);
                    frag->SetRadius(paint->Radius);
                    frag->SetFeather(paint->Feather);
                    NVG.TransformInverse(invxformPtr, xformMatPtr);
                }

                D3Dnvg__xformToMat3x3(paintMat, invxform);
                D3Dnvg_copyMatrix3to4(ref frag->paintMat, paintMat);
            }

            return 1;
        }

		public override int RenderCreate(IntPtr uptr) {
            return 1;
        }

		public override int RenderCreateTexture(IntPtr uptr, int type, int width, int height, NVGImageFlags imageFlags, IntPtr data) {
            NVGImage image = new NVGImage();
            image.type = (NVGtextureType)type;
            image.flags = imageFlags;
            int bytesPerPixel = image.type == NVGtextureType.NVG_TEXTURE_RGBA ? 4 : 1;
            image.handle = new Image2D(width, height, bytesPerPixel == 4 ? DXGI.Format.R8G8B8A8_UNorm : DXGI.Format.R8_UNorm, 1);
            if(data != IntPtr.Zero) {
                image.handle.SetSubData(data);
            }
            image.handle.UpdateResource();
            int id = images.Count;
            images.Add(image);
            return id + 1;
        }

        NVGImage FindImage(int id) {
            --id;
            if(id >= 0 && id < images.Count) {
                return images[id];
            } else {
                return null;
            }
        }

		public override int RenderDeleteTexture(IntPtr uptr, int id) {
            NVGImage image = FindImage(id);
            if(image != null) {
                image.handle.ReleaseResource();
                images.RemoveAt(id - 1);
                return 1;
            } 
            return 0;
        }

        void MemCopy(IntPtr _dst, IntPtr _src, int size, int num, int srcPitch, int dstPitch) {
            IntPtr dst = _dst;
            IntPtr src = _src;
            for (int ii = 0; ii < num; ++ii) {
                Utilities.CopyMemory(dst, src, size);
                src = IntPtr.Add(src, srcPitch);
                dst = IntPtr.Add(dst, dstPitch);
            }            
        }

		public override int RenderUpdateTexture(IntPtr uptr, int id, int x, int y, int w, int h, IntPtr data) {
            NVGImage image = FindImage(id);
            if(image != null) {
                int bytesPerPixel = image.handle.bytesPrePixel;
                int pitch = image.handle.width * bytesPerPixel;
                NVGImageCopyCommand copyCommand = new NVGImageCopyCommand();
                copyCommand.x = x;
                copyCommand.y = y;
                copyCommand.w = w;
                copyCommand.h = h;
                copyCommand.dataSize = MathHelper.Align(w * h * bytesPerPixel, 16);
                copyCommand.data = Utilities.AllocateMemory(copyCommand.dataSize);
                MemCopy(copyCommand.data, IntPtr.Add(data, y * pitch + x * bytesPerPixel),  w * bytesPerPixel, h, pitch, w * bytesPerPixel);
                copyCommand.destImage = image.handle;
                imageCopyCommands.Add(copyCommand);
                return 1;
            } 
            return 0;
        }

		public override int RenderGetTextureSize(IntPtr uptr, int image, out int width, out int height) {
            width = height = 0;
            if(image >= 0 && image < images.Count) {
                width = images[image].handle.width;
                height = images[image].handle.height;
                return 1;
            } else {
                return 0;
            }
        }

        float viewWidth;
        float viewHeight;
		public override void RenderViewport(IntPtr uptr, float w, float h, float devPixelRatio) {
            viewWidth = w;
            viewHeight = h;
        }

		public override void RenderCancel(IntPtr uptr) {

        }

		public override void RenderFlush(IntPtr uptr) {
            maxNumVertices = 0;
            maxNumUniformBlocks = 0;
        }

		public override void RenderDelete(IntPtr uptr) {
            for(int i = 0; i < images.Count; i++) {
                images[i].handle.ReleaseResource();
            }
        }

        int MaxVerts(NVGPath *paths, int num) {
            int count = 0;
            for(int i = 0; i < num; i++) {
                count += paths[i].NFill;
                count += paths[i].NStroke;
            }
            return count;
        }

        static readonly int VertexSize = Utilities.SizeOf<NVGVertex>();

		// TODO
		public override void RenderFill(IntPtr uptr, NVGPaint* paint, NVGCompositeOpState op, NVGScissor* scissor, float fringe, float* bounds, NVGPath* paths, int num) {
            int maxVerts = MaxVerts(paths, num) + 6;
            NVGDrawCall call = new NVGDrawCall();
            call.type = (num == 1 && paths[0].Convex != 0) ? NVGDrawCallType.ConvexFill : NVGDrawCallType.Fill;
            call.numPaths = num;
            NVGImage nvgimage = FindImage(paint->Image);
            call.image = nvgimage != null ? nvgimage.handle : null;
            call.paths = FrameAllocator.AllocMemory(Utilities.SizeOf<NVGDrawPath>() * num);

            int offset = AllocVertices(maxVerts);
            for(int i = 0; i < num; i++) {
                NVGDrawPath *copy = (NVGDrawPath*)IntPtr.Add(call.paths, Utilities.SizeOf<NVGDrawPath>() * i);
                if(paths[i].NFill > 0) {
                    copy->fillOffset = offset;
                    copy->numFills = paths[i].NFill;
                    Utilities.CopyMemory(IntPtr.Add(vertexBuffer, VertexSize * offset), (IntPtr)paths[i].Fill, VertexSize * paths[i].NFill);
                    offset += paths[i].NFill;
                }
                if(paths[i].NStroke > 0) {
                    copy->strokeOffset = offset;
                    copy->numStrokes = paths[i].NStroke;
                    Utilities.CopyMemory(IntPtr.Add(vertexBuffer, VertexSize * offset), (IntPtr)paths[i].Stroke, VertexSize * paths[i].NStroke);
                    offset += paths[i].NStroke;
                }
            }

            call.vertexOffset = offset;
            call.numVertices = 6;
            NVGVertex *quad = (NVGVertex*)IntPtr.Add(vertexBuffer, VertexSize * offset);
		    quad[0] = new NVGVertex(bounds[0], bounds[3], 0.5f, 1.0f);
		    quad[1] = new NVGVertex(bounds[2], bounds[3], 0.5f, 1.0f);
		    quad[2] = new NVGVertex(bounds[2], bounds[1], 0.5f, 1.0f);

		    quad[3] = new NVGVertex(bounds[0], bounds[3], 0.5f, 1.0f);
		    quad[4] = new NVGVertex(bounds[2], bounds[1], 0.5f, 1.0f);
		    quad[5] = new NVGVertex(bounds[0], bounds[1], 0.5f, 1.0f);

            if(call.type == NVGDrawCallType.Fill) {
                call.numUniformBlocks = 2;
                call.uniformBlockOffset = AllocUniformBlock(2);
                // Simple shader for stencil
                NVGConstants *frag = (NVGConstants*)(IntPtr.Add(uniformBuffer, call.uniformBlockOffset * NVGConstants.SizeInBytes));
                Utilities.ClearMemory((IntPtr)frag, 0, NVGConstants.SizeInBytes);
                frag->SetShaderType(NVGShaderType.Simple);
                frag->SetStrokeThreshold(-1f);
                frag->SetViewSize(viewWidth, viewHeight);
                frag = (NVGConstants*)(IntPtr.Add(uniformBuffer, (call.uniformBlockOffset + 1) * NVGConstants.SizeInBytes));
                ConvertPaint(frag, paint, scissor, fringe, fringe, -1f);        
            } else {
                call.numUniformBlocks = 1;
                call.uniformBlockOffset = AllocUniformBlock(1);
                ConvertPaint((NVGConstants*)(IntPtr.Add(uniformBuffer, call.uniformBlockOffset * NVGConstants.SizeInBytes)), paint, scissor, fringe, fringe, -1f);        
            }

            drawCalls.Add(call);
		}

		public override void RenderStroke(IntPtr uptr, NVGPaint* paint, NVGCompositeOpState op, NVGScissor* scissor, float fringe, float StrokeWidth, NVGPath* paths, int num) {
            NVGDrawCall call = new NVGDrawCall();
            call.type = NVGDrawCallType.Stroke;
            call.paths = FrameAllocator.AllocMemory(Utilities.SizeOf<NVGDrawPath>() * num);
            call.numPaths = num;
            NVGImage image = FindImage(paint->Image);
            call.image = image != null ? image.handle : null;

            int maxVerts = MaxVerts(paths, num);
            int offset = AllocVertices(maxVerts);

            for(int i = 0; i < num; i++) {
                NVGDrawPath *copy = (NVGDrawPath*)IntPtr.Add(call.paths, Utilities.SizeOf<NVGDrawPath>() * i);
                copy->numFills = 0;
                if(paths[i].NStroke > 0) {
                    copy->strokeOffset = offset;
                    copy->numStrokes = paths[i].NStroke;
                    Utilities.CopyMemory(IntPtr.Add(vertexBuffer, VertexSize * offset), (IntPtr)paths[i].Stroke, VertexSize * paths[i].NStroke);    
                    offset += paths[i].NStroke;                        
                }
            }

            // Fill shader
            call.uniformBlockOffset = AllocUniformBlock(1);
            ConvertPaint((NVGConstants*)(IntPtr.Add(uniformBuffer, call.uniformBlockOffset * NVGConstants.SizeInBytes)), paint, scissor, StrokeWidth, fringe, -1f);       

            drawCalls.Add(call); 
		}

		public override void RenderTriangles(IntPtr uptr, NVGPaint* paint, NVGCompositeOpState op, NVGScissor* scissor, NVGVertex* verts, int num) {
            NVGDrawCall call = new NVGDrawCall();
            call.type = NVGDrawCallType.Triangles;
            NVGImage image = FindImage(paint->Image);
            call.image = image != null ? image.handle : null;
            call.vertexOffset = AllocVertices(num);
            call.numVertices = num;
            Utilities.CopyMemory(IntPtr.Add(vertexBuffer, call.vertexOffset * VertexSize), (IntPtr)verts, VertexSize * num);
            // Fill shader
            call.uniformBlockOffset = AllocUniformBlock(1);
            NVGConstants *frag = (NVGConstants*)(IntPtr.Add(uniformBuffer, call.uniformBlockOffset * NVGConstants.SizeInBytes));
            ConvertPaint(frag, paint, scissor, 1f, 1f, -1f); 
            frag->SetShaderType(NVGShaderType.Image);
            drawCalls.Add(call);            
		}

		public override void RenderFillSafe(IntPtr uptr, NVGPaint paint, NVGCompositeOpState op, NVGScissor scissor, float fringe, float[] bounds, NVGPath[] paths) {

        }

		public override void RenderStrokeSafe(IntPtr uptr, NVGPaint paint, NVGCompositeOpState op, NVGScissor scissor, float fringe, float strokeWidth, NVGPath[] paths) {

        }

		public override void RenderTrianglesSafe(IntPtr uptr, NVGPaint Paint, NVGCompositeOpState Op, NVGScissor Scsr, NVGVertex[] Vrts) {

        }

        public void GetRenderData(out NVGRenderData renderData) {
            if(drawCalls.Count == 0) {
                renderData = null;
            } else {
                renderData = new NVGRenderData();
                renderData.numVertices = numVertices;
                renderData.numUniformBlocks = numUniformBlocks;
                renderData.vertexBuffer = vertexBuffer;
                renderData.uniformBuffer = uniformBuffer;
                renderData.drawCalls = drawCalls.ToArray();
                drawCalls.Clear();
                if(imageCopyCommands.Count > 0) {
                    renderData.imageCopyCommands = imageCopyCommands.ToArray();
                    imageCopyCommands.Clear();
                }
                numVertices = 0;
                numUniformBlocks = 0;
                uniformBuffer = IntPtr.Zero;
                vertexBuffer = IntPtr.Zero;
            }
        }
    }

    public class NVGSystem {
        public NanoVGContext context {get; private set;}
        MyNVGParameters nvgParameters;
        DynamicVertexBuffer vertexBuffer;
        DynamicIndexBuffer indexBuffer;
        DynamicUniformBuffer uniformBuffer;
        D3D11.BlendState blend;
        D3D11.BlendState blendNoWrite;
        D3D11.RasterizerState noCull;
        D3D11.RasterizerState cull;
        D3D11.DepthStencilState depthStencilDrawShapes;
        D3D11.DepthStencilState depthStencilDrawAA;
        D3D11.DepthStencilState depthStencilFill;
        D3D11.DepthStencilState depthStencilDefault;
        D3D11.InputLayout inputLayout;
        D3D11.VertexShader vertexShader;
        D3D11.PixelShader pixelShader;
        D3D11.Texture2D imageBufferR8;
        D3D11.Texture2D imageBufferRGBA8;
        public int defaultFont {get; private set;}
        public int msyh {get; private set;}

        public NVGSystem() {
            NVG.SetLibraryDirectory(null, @"E:\cpp_projects\neo - test_1");
            nvgParameters = new MyNVGParameters();
            context = NVG.CreateContext(nvgParameters);
            vertexBuffer = new DynamicVertexBuffer();
            indexBuffer = new DynamicIndexBuffer(IndexFormat.UINT32);
            uniformBuffer = new DynamicUniformBuffer();

            defaultFont = context.CreateFont("sans", Database.GetAssetPath("font/roboto-regular.ttf"));
            context.CreateFont("sans-bold", Database.GetAssetPath(@"font/roboto-bold.ttf"));
            msyh = context.CreateFont("msyh", Database.GetAssetPath("font/simkai.ttf"));

            D3D11.InputElement[] inputElements = new [] {
                new D3D11.InputElement("POSITION", 0, DXGI.Format.R32G32_Float, 0, 0),
                new D3D11.InputElement("TEXCOORD", 0, DXGI.Format.R32G32_Float, 8, 0)
            };    

            Shader.CreateShaderFromFileWithCallback(@"nanovg.fx", (bytes) => {
                using(var bytecode = Shader.LoadBytecode(bytes, "MainVS", ShaderStages.Vertex, "nanovgVS", new ShaderMacro("__VS__", 1))) {
                    vertexShader = new D3D11.VertexShader(RenderDevice.device, bytecode);
                    inputLayout = new D3D11.InputLayout(RenderDevice.device, bytecode, inputElements);
                }  
                using(var bytecode = Shader.LoadBytecode(bytes, "MainPS", ShaderStages.Pixel, "nanovgPS", new ShaderMacro("__PS__", 1), new ShaderMacro("EDGE_AA", 1))) {
                    pixelShader = new D3D11.PixelShader(RenderDevice.device, bytecode);
                }
            });      

            RenderStateBlock state = new RenderStateBlock();
            RenderStateHelper.SetDefault(ref state);
            RenderStateHelper.SetPolygonMode(ref state, PolygonMode.Fill);
            RenderStateHelper.SetCullMode(ref state, CullMode.None);
            RenderStateHelper.SetFrontFace(ref state, FrontFace.CCW);
            noCull = RenderDevice.CreateRasterizerState(state.rasterizerState);

            RenderStateHelper.SetCullMode(ref state, CullMode.BackSided);
            cull = RenderDevice.CreateRasterizerState(state.rasterizerState);

            RenderStateHelper.SetDefault(ref state);
            RenderStateHelper.SetBlendOp(ref state, BlendOp.Add);
            RenderStateHelper.SetBlendFactor(ref state, BlendFactor.One, BlendFactor.OneMinusSrcAlpha);
            blend = RenderDevice.CreateBlendState(state.blendState);

            RenderStateHelper.SetDefault(ref state);
            RenderStateHelper.SetColorWriteMask(ref state, 0);
            blendNoWrite = RenderDevice.CreateBlendState(state.blendState);

            RenderStateHelper.SetDefault(ref state);
            RenderStateHelper.DisableDepthTest(ref state);
            RenderStateHelper.DisableDepthWrite(ref state);
            RenderStateHelper.EnableStencilTest(ref state);
            RenderStateHelper.SetStencilFuncMask(ref state, 0xff);
            RenderStateHelper.SetStencilWriteMask(ref state, 0xff);
            RenderStateHelper.SetStencilFailOp(ref state, StencilOp.Keep);
            RenderStateHelper.SetStencilDepthFailOp(ref state, StencilOp.Keep);
            RenderStateHelper.SetStencilPassOp(ref state, StencilOp.Increment);
            RenderStateHelper.SetStencilFunc(ref state, CompareOp.Always);
            RenderStateHelper.SetStencilBackFailOp(ref state, StencilOp.Keep);
            RenderStateHelper.SetStencilBackDepthFailOp(ref state, StencilOp.Keep);
            RenderStateHelper.SetStencilBackPassOp(ref state, StencilOp.Decrement);
            RenderStateHelper.SetStencilBackFunc(ref state, CompareOp.Always);
            depthStencilDrawShapes = RenderDevice.CreateDepthStencilState(state.depthStencilState);

            RenderStateHelper.SetDefault(ref state);
            RenderStateHelper.DisableDepthTest(ref state);
            RenderStateHelper.DisableDepthWrite(ref state);
            RenderStateHelper.SetStencilFuncMask(ref state, 0xff);
            RenderStateHelper.SetStencilWriteMask(ref state, 0xff);
            RenderStateHelper.SetStencilFailOp(ref state, StencilOp.Keep);
            RenderStateHelper.SetStencilDepthFailOp(ref state, StencilOp.Keep);
            RenderStateHelper.SetStencilPassOp(ref state, StencilOp.Keep);
            RenderStateHelper.SetStencilFunc(ref state, CompareOp.Equal);
            depthStencilDrawAA = RenderDevice.CreateDepthStencilState(state.depthStencilState);

            RenderStateHelper.SetDefault(ref state);
            RenderStateHelper.DisableDepthTest(ref state);
            RenderStateHelper.DisableDepthWrite(ref state);
            RenderStateHelper.SetStencilFuncMask(ref state, 0xff);
            RenderStateHelper.SetStencilWriteMask(ref state, 0xff);
            RenderStateHelper.SetStencilFailOp(ref state, StencilOp.Zero);
            RenderStateHelper.SetStencilDepthFailOp(ref state, StencilOp.Zero);
            RenderStateHelper.SetStencilPassOp(ref state, StencilOp.Zero);
            RenderStateHelper.SetStencilFunc(ref state, CompareOp.NotEqual);
            depthStencilFill = RenderDevice.CreateDepthStencilState(state.depthStencilState);

            RenderStateHelper.SetDefault(ref state);
            RenderStateHelper.DisableDepthTest(ref state);
            RenderStateHelper.DisableDepthWrite(ref state);
            depthStencilDefault = RenderDevice.CreateDepthStencilState(state.depthStencilState);
        }

        public void GetRenderData(out NVGRenderData renderData) {
            nvgParameters.GetRenderData(out renderData);
        }

        public void UpdateResource_RenderThread(D3D11.DeviceContext1 context, NVGRenderData renderData) {
            if(renderData != null) {
                if(renderData.imageCopyCommands != null) {
                    for(int i = 0; i < renderData.imageCopyCommands.Length; i++) {
                        NVGImageCopyCommand cmd = renderData.imageCopyCommands[i];
                        context.UpdateSubresource(
                            cmd.destImage.handle, 0, 
                            new D3D11.ResourceRegion(cmd.x, cmd.y, 0, cmd.x + cmd.w, cmd.y + cmd.h, 1), 
                            cmd.data, 
                            cmd.w * cmd.destImage.bytesPrePixel, 
                            cmd.dataSize);
                        Utilities.FreeMemory(cmd.data);
                    }
                }
                if(renderData.numVertices > 0 && renderData.vertexBuffer != IntPtr.Zero) {
                    DataStream vertexStream = vertexBuffer.Map_RenderThread(renderData.numVertices, Utilities.SizeOf<NVGVertex>());
                    DataStream indexStream = indexBuffer.Map_RenderThread((renderData.numVertices * 2 - 3) * sizeof(UInt32));
                    int i0 = 0, i1 = 1, i2 = 2;
                    for(int i = 0; i < renderData.numVertices * 2 - 3; i += 3) {
                        indexStream.Write(i0);
                        indexStream.Write(i1++);
                        indexStream.Write(i2++);
                    }
                    vertexStream.WriteRange(renderData.vertexBuffer, renderData.numVertices * Utilities.SizeOf<NVGVertex>());
                }
            }
        }

        unsafe void Fill(D3D11.DeviceContext1 context, NVGDrawCall call, bool edgeAA) {
            NVGDrawPath *paths = (NVGDrawPath*)call.paths;
            int numPaths = call.numPaths;

            context.OutputMerger.DepthStencilState = depthStencilDrawShapes;
            context.Rasterizer.State = noCull;
            context.OutputMerger.BlendState = blendNoWrite;
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;

            D3D11.Buffer[] buffers = {uniformBuffer.GetDeviceHandle()};
            int[] constantOffset = {uniformBuffer.GetConstantOffset() + call.uniformBlockOffset * NVGConstants.SizeInBytes / 16};
            int[] numConstantOffsets = {NVGConstants.SizeInBytes / 16};
            context.VSSetConstantBuffers1(0, 1, buffers, constantOffset, numConstantOffsets);
            context.PSSetConstantBuffers1(0, 1, buffers, constantOffset, numConstantOffsets);

            for(int i = 0; i < numPaths; i++) {
                int numIndices = (paths[i].numFills - 2) * 3;
                context.DrawIndexed(numIndices, 0, paths[i].fillOffset);
            }

            constantOffset[0] = uniformBuffer.GetConstantOffset() + (call.uniformBlockOffset + 1) * NVGConstants.SizeInBytes / 16;
            context.VSSetConstantBuffers1(0, 1, buffers, constantOffset, numConstantOffsets);
            context.PSSetConstantBuffers1(0, 1, buffers, constantOffset, numConstantOffsets);

            context.Rasterizer.State = cull;
            context.OutputMerger.BlendState = blend;
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
            
            if(edgeAA) {
                context.OutputMerger.DepthStencilState = depthStencilDrawAA;
                for(int i = 0; i < numPaths; i++) {
                    context.Draw(paths[i].numStrokes, paths[i].strokeOffset);
                }
            }

            // Draw fill
            context.Rasterizer.State = noCull;
            context.OutputMerger.DepthStencilState = depthStencilFill;
            context.Draw(call.numVertices, call.vertexOffset);
            context.OutputMerger.DepthStencilState = depthStencilDefault;
        }        

        unsafe void ConvexFill(D3D11.DeviceContext1 context, NVGDrawCall call, bool edgeAA) {
            NVGDrawPath *paths = (NVGDrawPath*)call.paths;
            int numPaths = call.numPaths;

            D3D11.Buffer[] buffers = {uniformBuffer.GetDeviceHandle()};
            int[] constantOffset = {uniformBuffer.GetConstantOffset() + call.uniformBlockOffset * NVGConstants.SizeInBytes / 16};
            int[] numConstantOffsets = {NVGConstants.SizeInBytes / 16};
            context.VSSetConstantBuffers1(0, 1, buffers, constantOffset, numConstantOffsets);
            context.PSSetConstantBuffers1(0, 1, buffers, constantOffset, numConstantOffsets);

            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            for(int i = 0; i < numPaths; i++) {
                if(paths[i].numFills > 2) {
                    int numIndices = (paths[i].numFills - 2) * 3;
                    context.DrawIndexed(numIndices, 0, paths[i].fillOffset);
                }
            }

            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;
            if(edgeAA) {
                for(int i = 0; i < numPaths; i++) {
                    context.Draw(paths[i].numStrokes, paths[i].strokeOffset);
                }
            }
        }

        unsafe void Triangles(D3D11.DeviceContext1 context, NVGDrawCall call) {
            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleList;
            D3D11.Buffer[] buffers = {uniformBuffer.GetDeviceHandle()};
            int[] constantOffset = {uniformBuffer.GetConstantOffset() + call.uniformBlockOffset * NVGConstants.SizeInBytes / 16};
            int[] numConstantOffsets = {NVGConstants.SizeInBytes / 16};
            context.VSSetConstantBuffers1(0, 1, buffers, constantOffset, numConstantOffsets);
            context.PSSetConstantBuffers1(0, 1, buffers, constantOffset, numConstantOffsets);
            context.Draw(call.numVertices, call.vertexOffset);
        }

        unsafe void Stroke(D3D11.DeviceContext1 context, NVGDrawCall call) {
            NVGDrawPath *paths = (NVGDrawPath*)call.paths;
            int numPaths = call.numPaths;

            context.InputAssembler.PrimitiveTopology = PrimitiveTopology.TriangleStrip;

            D3D11.Buffer[] buffers = {uniformBuffer.GetDeviceHandle()};
            int[] constantOffset = {uniformBuffer.GetConstantOffset() + call.uniformBlockOffset * NVGConstants.SizeInBytes / 16};
            int[] numConstantOffsets = {NVGConstants.SizeInBytes / 16};
            context.VSSetConstantBuffers1(0, 1, buffers, constantOffset, numConstantOffsets);
            context.PSSetConstantBuffers1(0, 1, buffers, constantOffset, numConstantOffsets);

            for(int i = 0; i < numPaths; i++) {
                context.Draw(paths[i].numStrokes, paths[i].strokeOffset);
            }      
        }

        public void FillCommandBuffer_RenderThread(D3D11.DeviceContext1 context, NVGRenderData renderData) {
            if(renderData != null) {
                uniformBuffer.UpdateRange_RenderThread(renderData.uniformBuffer, renderData.numUniformBlocks * NVGConstants.SizeInBytes, NVGConstants.SizeInBytes);
                
                context.VertexShader.Set(vertexShader);
                context.PixelShader.Set(pixelShader);

                context.InputAssembler.InputLayout = inputLayout;
                context.InputAssembler.SetVertexBuffers(0, new D3D11.VertexBufferBinding(vertexBuffer.GetDeviceHandle(), vertexBuffer.stride, vertexBuffer.offset));
                context.InputAssembler.SetIndexBuffer(indexBuffer.GetDeviceHandle(), indexBuffer.GetDeviceFormat(), indexBuffer.offset);

                // Draw shapes
                context.OutputMerger.DepthStencilState = depthStencilDefault;
                context.OutputMerger.BlendState = blend;
                context.Rasterizer.State = noCull;

                context.PixelShader.SetSampler(0, RenderSystem.GetImmutableSampler(ImmutableSampler.PointClamp));

                for(int i = 0; i < renderData.drawCalls.Length; i++) {
                    NVGDrawCall call = renderData.drawCalls[i];
                    if(call.image != null) {
                        context.PixelShader.SetShaderResource(0, call.image.shaderResourceView);
                    } else {
                        context.PixelShader.SetShaderResource(0, Image2D.missingImage.shaderResourceView);
                    }
                    if(call.type == NVGDrawCallType.Fill) {
                        Fill(context, call, true);
                    } else if(call.type == NVGDrawCallType.ConvexFill) {
                        ConvexFill(context, call, true);
                    } else if(call.type == NVGDrawCallType.Stroke) {
                        Stroke(context, call);
                    } else if(call.type == NVGDrawCallType.Triangles) {
                        Triangles(context, call);
                    }
                }
            }
        }
    }
}
*/