using System;
using System.Collections.Generic;
using SharpDX.DXGI;

namespace NT
{
    public class VertexBuffer : RenderBuffer {
        public VertexBuffer() {}
        
        public VertexBuffer(int numVertices, int vertexStrideInBytes) {
            strideInBytes = vertexStrideInBytes;
            sizeInBytes = MathHelper.Align(numVertices * strideInBytes, 16);
            bufferUsage = Veldrid.BufferUsage.VertexBuffer;
        }

        public void InitData(IntPtr inData, int inSizeInBytes, int inStrideInBytes) {
            initData = inData;
            sizeInBytes = inSizeInBytes;
            strideInBytes = inStrideInBytes;
        }
    }

    public class VertexBufferRef : VertexBuffer {
        public sealed override void InitDeviceResource() {}
        public sealed override void InitDeviceResource(Veldrid.CommandList commandList) {}
        public sealed override void ReleaseDeviceResource() {}
        public sealed override void ReleaseLocalResource() {}  

        public void Reference(DynamicRenderBuffer other, int offsetInBytes, int inSizeInBytes, int inStrideInBytes) {
            if(other == null || !other.bufferUsage.HasFlag(Veldrid.BufferUsage.VertexBuffer)) {
                throw new InvalidOperationException("RenderBuffer.Reference:other is null.");
            }
            if(inSizeInBytes <= 0) {
                throw new InvalidOperationException("RenderBuffer.Reference:sizeInBytes == 0.");
            }
            if(offsetInBytes + inSizeInBytes > other.sizeInBytes) {
                throw new InvalidOperationException("RenderBuffer.Reference:offsetInBytes + sizeInBytes > other.sizeInBytes.");                
            }
            bufferObject = other.bufferObject;
            bufferUsage = other.bufferUsage;
            offsetInOtherInBytes = offsetInBytes;
            sizeInBytes = inSizeInBytes;
            strideInBytes = inStrideInBytes;
        }         
    }
}