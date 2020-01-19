using System;
using System.Collections.Generic;
using SharpDX;

namespace NT
{
    public abstract class RenderResource {
        public abstract void InitDeviceResource();
        public abstract void InitDeviceResource(Veldrid.CommandList commandList);
        public abstract void ReleaseDeviceResource();
        public abstract void ReleaseLocalResource();
    }

    public class DynamicRenderBuffer : RenderResource {
        public int sizeInBytes {get; protected set;}
        public int strideInBytes {get; protected set;}
        public int offsetInOtherInBytes {get; protected set;}
        public Veldrid.BufferUsage bufferUsage {get; protected set;}
        public Veldrid.DeviceBuffer bufferObject {get; protected set;}

        public int memoryUsed {get; private set;}
        IntPtr mappedData;

        public DynamicRenderBuffer(int inSizeInBytes, Veldrid.BufferUsage inBufferUsage) {
            sizeInBytes = inSizeInBytes;
            strideInBytes = 1;
            bufferUsage = inBufferUsage | Veldrid.BufferUsage.Dynamic;
        }

        public IntPtr Alloc(int inSizeInBytes, out int offset, out int alignedSize) {
            int aligned = MathHelper.Align(inSizeInBytes, 16);
            if(memoryUsed + aligned > sizeInBytes) {
                throw new OutOfMemoryException("");
            }
            IntPtr data = IntPtr.Add(mappedData, memoryUsed);
            offset = memoryUsed;
            alignedSize = aligned;
            memoryUsed += aligned;
            return data;
        }

        public void Map(Veldrid.GraphicsDevice graphicsDevice) {
            if(bufferObject != null && mappedData == IntPtr.Zero) {
                Veldrid.MappedResource mappedResource = graphicsDevice.Map(bufferObject, Veldrid.MapMode.Write);
                mappedData = mappedResource.Data;
                memoryUsed = 0;
            }
        }

        public void Unmap(Veldrid.GraphicsDevice graphicsDevice) {
            if(mappedData != IntPtr.Zero) {
                graphicsDevice.Unmap(bufferObject);
                mappedData = IntPtr.Zero;
                memoryUsed = 0;
            }
        }

        public override void InitDeviceResource() {
            if(bufferObject == null && sizeInBytes > 0) {
                bufferObject = GraphicsDevice.ResourceFactory.CreateBuffer(new Veldrid.BufferDescription((uint)sizeInBytes, bufferUsage));
            }
        }

        public override void InitDeviceResource(Veldrid.CommandList commandList) {
            if(bufferObject == null && sizeInBytes > 0) {
                bufferObject = GraphicsDevice.ResourceFactory.CreateBuffer(new Veldrid.BufferDescription((uint)sizeInBytes, bufferUsage));
            }
        }

        public override void ReleaseDeviceResource() {
            bufferObject?.Dispose();
            bufferObject = null;
            mappedData = IntPtr.Zero;
            memoryUsed = 0;
            offsetInOtherInBytes = 0;
            sizeInBytes = 0;
            strideInBytes = 0;
        }   

        public override void ReleaseLocalResource() {}        
    }

    public class RenderBuffer : RenderResource {
        public int sizeInBytes {get; protected set;}
        public int strideInBytes {get; protected set;}
        public int offsetInOtherInBytes {get; protected set;}
        public Veldrid.BufferUsage bufferUsage {get; protected set;}
        public Veldrid.DeviceBuffer bufferObject {get; protected set;}

        protected IntPtr initData;
        protected RenderBuffer owner {get; private set;}

        public RenderBuffer() {}

        public RenderBuffer(int inSizeInBytes, int inStrideInBytes, Veldrid.BufferUsage inUsage) {
            sizeInBytes = inSizeInBytes;
            strideInBytes = inStrideInBytes;
            bufferUsage = inUsage;
        }

        public void Reference(RenderBuffer other, int offsetInBytes) {
            if(other == null) {
                throw new InvalidOperationException("RenderBuffer.Reference:other is null.");
            }
            if(sizeInBytes <= 0) {
                throw new InvalidOperationException("RenderBuffer.Reference:sizeInBytes == 0.");
            }
            if(offsetInBytes + sizeInBytes > other.sizeInBytes) {
                throw new InvalidOperationException("RenderBuffer.Reference:offsetInBytes + sizeInBytes > other.sizeInBytes.");                
            }
            owner = other;
            offsetInOtherInBytes = offsetInBytes;
        }

        public override void InitDeviceResource() {
            if(owner == null) {
                if(bufferObject == null && sizeInBytes > 0) {
                    bufferObject = GraphicsDevice.ResourceFactory.CreateBuffer(new Veldrid.BufferDescription((uint)sizeInBytes, bufferUsage));
                }
            } else {
                if(bufferObject == null) {
                    bufferObject = owner.bufferObject;
                }
            }
            if(initData != IntPtr.Zero && bufferObject != null && sizeInBytes > 0) {
                GraphicsDevice.gd.UpdateBuffer(bufferObject, (uint)(owner != null ? offsetInOtherInBytes : 0), initData, (uint)sizeInBytes);
            }
        }

        public override void InitDeviceResource(Veldrid.CommandList commandList) {
            if(owner == null) {
                if(bufferObject == null && sizeInBytes > 0) {
                    bufferObject = GraphicsDevice.ResourceFactory.CreateBuffer(new Veldrid.BufferDescription((uint)sizeInBytes, bufferUsage));
                }
            } else {
                if(bufferObject == null) {
                    bufferObject = owner.bufferObject;
                }
            }
            if(initData != IntPtr.Zero && bufferObject != null && sizeInBytes > 0) {
                commandList.UpdateBuffer(bufferObject, (uint)(owner != null ? offsetInOtherInBytes : 0), initData, (uint)sizeInBytes);
            }
        }

        public override void ReleaseDeviceResource() {
            if(owner == null) {
                bufferObject?.Dispose();
                bufferObject = null;
            }
            owner = null;
            offsetInOtherInBytes = 0;
            sizeInBytes = 0;
            strideInBytes = 0;
        }   

        public override void ReleaseLocalResource() {
            if(initData != IntPtr.Zero) {
                SharpDX.Utilities.FreeMemory(initData);
                initData = IntPtr.Zero;
            }
        }
    }

    public class FrameRenderResources {
        public DynamicRenderBuffer dynamicRenderBuffer {get; private set;}

        public void Init() {
            dynamicRenderBuffer = new DynamicRenderBuffer(32 * 1024 * 1024, Veldrid.BufferUsage.VertexBuffer | Veldrid.BufferUsage.IndexBuffer | Veldrid.BufferUsage.Dynamic);
            dynamicRenderBuffer.InitDeviceResource();
        }

        public void Shutdown() {
            dynamicRenderBuffer.ReleaseDeviceResource();
        }

        public void MapDynamicBuffers() {
            dynamicRenderBuffer.Map(GraphicsDevice.gd);
        }
/*
        public void AllocDynamicVertices<T>(Veldrid.CommandList cl, T[] vertices, ref VertexBufferRef vertexBuffer) where T: struct {
            int vertexStrideInBytes = Utilities.SizeOf<T>();
            int uploadSizeInBytes = vertices.Length * vertexStrideInBytes;
            int aligendSize = MathHelper.Align(uploadSizeInBytes, 16);
            vertexBuffer.Reference(dynamicRenderBuffer, usedDynamicRenderBufferBytes, aligendSize, vertexStrideInBytes);
            cl.UpdateBuffer(dynamicRenderBuffer.bufferObject, (uint)usedDynamicRenderBufferBytes, vertices);
            usedDynamicRenderBufferBytes += aligendSize;
        }

        public void AllocDynamicVertices(Veldrid.CommandList cl, IntPtr vertices, int sizeInBytes, int strideInBytes, ref VertexBufferRef vertexBuffer) {
            int uploadSizeInBytes =  sizeInBytes;
            int aligendSize = MathHelper.Align(uploadSizeInBytes, 16);
            vertexBuffer.Reference(dynamicRenderBuffer, usedDynamicRenderBufferBytes, aligendSize, strideInBytes);
            cl.UpdateBuffer(dynamicRenderBuffer.bufferObject, (uint)usedDynamicRenderBufferBytes, ref vertices, (uint)uploadSizeInBytes);
            usedDynamicRenderBufferBytes += aligendSize;
        }

         public void AllocDynamicIndices(Veldrid.CommandList cl, IntPtr indices, int numIndices, Veldrid.IndexFormat format, ref IndexBufferRef indexBuffer) {
            int uploadSizeInBytes = numIndices * (format == Veldrid.IndexFormat.UInt16 ? sizeof(UInt16) : sizeof(UInt32));
            int aligendSize = uploadSizeInBytes;
            indexBuffer.Reference(dynamicRenderBuffer, usedDynamicRenderBufferBytes, aligendSize, format);
            cl.UpdateBuffer(dynamicRenderBuffer.bufferObject, (uint)usedDynamicRenderBufferBytes, ref indices, (uint)uploadSizeInBytes);
            usedDynamicRenderBufferBytes += aligendSize;
        }    

         public void AllocDynamicIndices(Veldrid.CommandList cl, UInt16[] indices, ref IndexBufferRef indexBuffer) {
            int uploadSizeInBytes = indices.Length * sizeof(UInt16);
            int aligendSize = uploadSizeInBytes;
            indexBuffer.Reference(dynamicRenderBuffer, usedDynamicRenderBufferBytes, aligendSize, Veldrid.IndexFormat.UInt16);
            cl.UpdateBuffer(dynamicRenderBuffer.bufferObject, (uint)usedDynamicRenderBufferBytes, indices);
            usedDynamicRenderBufferBytes += aligendSize;
        }

         public void AllocDynamicIndices(Veldrid.CommandList cl, UInt32[] indices, ref IndexBufferRef indexBuffer) {
            int uploadSizeInBytes = indices.Length * sizeof(UInt32);
            int aligendSize = uploadSizeInBytes;
            indexBuffer.Reference(dynamicRenderBuffer, usedDynamicRenderBufferBytes, aligendSize, Veldrid.IndexFormat.UInt32);
            cl.UpdateBuffer(dynamicRenderBuffer.bufferObject, (uint)usedDynamicRenderBufferBytes, indices);
            usedDynamicRenderBufferBytes += aligendSize;
        }  
 */       

        public IntPtr AllocVertices(int numVertices, int vertexStrideInBytes, ref VertexBufferRef vertexBuffer) {
            IntPtr data = dynamicRenderBuffer.Alloc(numVertices * vertexStrideInBytes, out int offset, out int alignedSize);
            vertexBuffer.Reference(dynamicRenderBuffer, offset, alignedSize, vertexStrideInBytes);
            return data;
        }

        public void AllocVertices<T>(T[] vertices, ref VertexBufferRef vertexBuffer) where T: struct {
            IntPtr data = dynamicRenderBuffer.Alloc(Utilities.SizeOf<T>() * vertices.Length, out int offset, out int alignedSize);
            DataStream stream = new DataStream(data, alignedSize, false, true);
            stream.WriteRange(vertices);
            vertexBuffer.Reference(dynamicRenderBuffer, offset, alignedSize, Utilities.SizeOf<T>());
        }

        public IntPtr AllocIndices(int numIndices, Veldrid.IndexFormat format, ref IndexBufferRef indexBuffer) {
            int stride = format == Veldrid.IndexFormat.UInt16 ? sizeof(UInt16) : sizeof(UInt32);
            IntPtr data = dynamicRenderBuffer.Alloc(numIndices * stride, out int offset, out int alignedSize);
            indexBuffer.Reference(dynamicRenderBuffer, offset, alignedSize, format);
            return data;
        }

        public void AllocIndices(UInt16[] indices, ref IndexBufferRef indexBuffer) {
            int sizeInBytes = indices.Length * sizeof(UInt16);
            IntPtr data = dynamicRenderBuffer.Alloc(sizeInBytes, out int offset, out int alignedSize);
            DataStream stream = new DataStream(data, alignedSize, false, true);
            stream.WriteRange(indices);
            indexBuffer.Reference(dynamicRenderBuffer, offset, alignedSize, Veldrid.IndexFormat.UInt16);
        }

        public void AllocIndices(UInt32[] indices, ref IndexBufferRef indexBuffer) {
            int sizeInBytes = indices.Length * sizeof(UInt32);
            IntPtr data = dynamicRenderBuffer.Alloc(sizeInBytes, out int offset, out int alignedSize);
            DataStream stream = new DataStream(data, alignedSize, false, true);
            stream.WriteRange(indices);
            indexBuffer.Reference(dynamicRenderBuffer, offset, alignedSize, Veldrid.IndexFormat.UInt32);
        }
 
        public void UnmapDynamicBuffers() {
            dynamicRenderBuffer.Unmap(GraphicsDevice.gd);
        }
    }

    public static class FrameAllocator {
        const int maxFrameMemorySize = 32 * 1024 * 1024;
        const int memoryAllocAlignment = 128;
        static int memoryUsed;
        static IntPtr frameMemory; 

        public static void NewFrame() {
            memoryUsed = 0;
        }

        public static void Shutdown() {
            memoryUsed = 0;
            Utilities.FreeMemory(frameMemory);
            frameMemory = IntPtr.Zero;
        }

        static FrameAllocator() {
            frameMemory = Utilities.AllocateMemory(maxFrameMemorySize, memoryAllocAlignment);
        }

        public static IntPtr AllocMemory(int sizeInBytes, uint align = memoryAllocAlignment) {
            if(sizeInBytes > 0) {
                int bytes = (int)((sizeInBytes + align - 1) & ~(align - 1));
                if ((memoryUsed + bytes) > maxFrameMemorySize) {
                    throw new OutOfMemoryException("FrameAlloc ran out of memory.");
                }

                int offset = memoryUsed;
	            memoryUsed += bytes;
                IntPtr ptr = Utilities.IntPtrAdd(frameMemory, offset);
                Utilities.ClearMemory(ptr, 0, bytes);
                return ptr;
            }
            return IntPtr.Zero;
        }

        public static Span<T> Alloc<T>(int count, uint align = memoryAllocAlignment) where T:struct {
            unsafe {
                int sizeInBytes = Utilities.SizeOf<T>() * count;
                Span<T> buffer = new Span<T>(AllocMemoryPtr(sizeInBytes, align), count);
                return buffer;
            }
        }

        public unsafe static void* AllocMemoryPtr(int sizeInBytes, uint align = memoryAllocAlignment) {
            return (void*)AllocMemory(sizeInBytes, align);
        }               
    }

    public ref struct FrameList<T> where T: struct {
        int numElements;
        Span<T> elements;

        public int Count { get {return numElements;} }

        public T this[int index] {
            set {
                elements[index] = value;
            }
            get {
                return elements[index];
            }
        }

        public FrameList(int num) {
            numElements = 0;
            if(num > 0) {
                elements = FrameAllocator.Alloc<T>(num);
            } else {
                elements = new Span<T>();
            }
        }

        public void Resize(int num, bool keepOld = true) {
            if(elements.Length < num) {
                var newBuffer = FrameAllocator.Alloc<T>(num);
                if(keepOld) {
                    elements.CopyTo(newBuffer);
                }
                elements = newBuffer;
            }
        }

        public void Add(T element) {
            if((numElements + 1) >= elements.Length) {
                int newSize = elements.Length * 2;
                if(elements.Length == 0) {
                    newSize = 512;
                }
                Resize(newSize);
            }
            elements[numElements] = element;
            numElements++;
        }

        public void AddFast(T element) {
            elements[numElements] = element;
            numElements++;
        }

        public ref T Alloc() {
            if((numElements + 1) >= elements.Length) {
                int newSize = elements.Length * 2;
                if(elements.Length == 0) {
                    newSize = 512;
                }
                Resize(newSize);
            }
            int index = numElements++;
            return ref elements[index];            
        }

        public Span<T> GetView(int start, int count) {
            return elements.Slice(start, count);
        }

        public ReadOnlySpan<T> GetReadOnlyView(int start, int count) {
            return elements.Slice(start, count);
        }
    }    
}