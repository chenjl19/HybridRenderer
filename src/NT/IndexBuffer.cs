using System;
using System.Collections.Generic;
using SharpDX.DXGI;

namespace NT
{
    public class IndexBuffer : RenderBuffer {
        public Veldrid.IndexFormat indexFormat {get; protected set;}

        public IndexBuffer() {}

        public IndexBuffer(Veldrid.IndexFormat format) {
            indexFormat = format;
            strideInBytes = indexFormat == Veldrid.IndexFormat.UInt16 ? sizeof(UInt16) : sizeof(UInt32);
        }

        public IndexBuffer(int numIndices, Veldrid.IndexFormat inIndexFormat) : this(inIndexFormat) {
            indexFormat = inIndexFormat;
            sizeInBytes = numIndices * strideInBytes;
        }

        public void InitData(IntPtr inData, int inDataSize) {
            initData = inData;
            sizeInBytes = inDataSize;
        }
    }

    public class IndexBuffer16 : IndexBuffer {
        public IndexBuffer16() : base(Veldrid.IndexFormat.UInt16) {}

        public IndexBuffer16(int inNumIndices) : this() {
            sizeInBytes = inNumIndices * strideInBytes;
        }
    }

    public class IndexBuffer32 : IndexBuffer {
        public IndexBuffer32() : base(Veldrid.IndexFormat.UInt32) {}        
    }

    public class IndexBufferRef : IndexBuffer {
        public sealed override void InitDeviceResource() {}
        public sealed override void InitDeviceResource(Veldrid.CommandList commandList) {}
        public sealed override void ReleaseDeviceResource() {}
        public sealed override void ReleaseLocalResource() {}  

        public void Reference(DynamicRenderBuffer other, int offsetInBytes, int inSizeInBytes, Veldrid.IndexFormat inIndexFormat) {
            if(other == null || !other.bufferUsage.HasFlag(Veldrid.BufferUsage.IndexBuffer)) {
                throw new InvalidOperationException("IndexBufferRef.Reference:other is null.");
            }
            if(inSizeInBytes <= 0) {
                throw new InvalidOperationException("IndexBufferRef.Reference:sizeInBytes == 0.");
            }
            if(offsetInBytes + inSizeInBytes > other.sizeInBytes) {
                throw new InvalidOperationException("IndexBufferRef.Reference:offsetInBytes + sizeInBytes > other.sizeInBytes.");                
            }
            bufferObject = other.bufferObject;
            bufferUsage = other.bufferUsage;
            offsetInOtherInBytes = offsetInBytes;
            indexFormat = inIndexFormat;
            sizeInBytes = inSizeInBytes;
            strideInBytes = indexFormat == Veldrid.IndexFormat.UInt16 ? sizeof(UInt16) : sizeof(UInt32);            
        }         
    }
}