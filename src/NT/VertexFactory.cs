using System;
using System.Collections.Generic;
using SharpDX;

namespace NT
{
    public struct DeviceBufferBinding {
        public Veldrid.DeviceBuffer buffer;
        public uint offsetInBytes;

        public DeviceBufferBinding(Veldrid.DeviceBuffer buffer, uint bufferOffsetInBytes) {
            this.buffer = buffer;
            this.offsetInBytes = bufferOffsetInBytes;
        }
    }

    public abstract class VertexFactory {
        protected VertexBuffer[] buffers;

        public VertexFactory() {}
        public VertexFactory(Mesh meshData, MeshRenderSystem renderSystem) {}

        public int NumBuffers() {
            return buffers != null ? buffers.Length : 0;
        }

        public int NumPositionBuffers() {
            return 0;
        }

        public VertexBuffer GetBuffer(int index) {
            if(buffers != null && (index >= 0 && index < buffers.Length)) {
                return buffers[index];
            }
            return null;
        }

        public DeviceBufferBinding GetPositionDeviceBuffer() {
            return GetDeviceBuffer(0);
        }

        public DeviceBufferBinding GetDeviceBuffer(int index) {
            if(buffers == null || (index < 0 || index > buffers.Length)) {
                return default(DeviceBufferBinding);
            }
            return new DeviceBufferBinding(buffers[index].bufferObject, (uint)buffers[index].offsetInOtherInBytes);            
        }

        static DeviceBufferBinding[] gpuBindings = new DeviceBufferBinding[16];
        public DeviceBufferBinding[] GetDeviceBuffers() {
            if(buffers == null) {
                return null;
            }
            for(int i = 0; i < buffers.Length; i++) {
                gpuBindings[i] = new DeviceBufferBinding(buffers[i].bufferObject, (uint)buffers[i].offsetInOtherInBytes); 
            }
            return gpuBindings;
        }
    }

    public class DrawVertexPacked0VertexFactory : VertexFactory { 
        readonly static Veldrid.VertexLayoutDescription[] vertexLayoutDescriptions;
        static DrawVertexPacked0VertexFactory() {
            vertexLayoutDescriptions = new Veldrid.VertexLayoutDescription[] {
                new Veldrid.VertexLayoutDescription(
                    new Veldrid.VertexElementDescription("Position", Veldrid.VertexElementSemantic.Position, Veldrid.VertexElementFormat.Float3),
                    new Veldrid.VertexElementDescription("Texcoord", Veldrid.VertexElementSemantic.TextureCoordinate, Veldrid.VertexElementFormat.Half2))
            };  
        }
    }

    public class ScreenCoordinateVertexFactory : VertexFactory { 
        readonly static Veldrid.VertexLayoutDescription[] vertexLayoutDescriptions;
        static ScreenCoordinateVertexFactory() {
            vertexLayoutDescriptions = new Veldrid.VertexLayoutDescription[] {
                new Veldrid.VertexLayoutDescription(
                    new Veldrid.VertexElementDescription("Position", Veldrid.VertexElementSemantic.Position, Veldrid.VertexElementFormat.Float2),
                    new Veldrid.VertexElementDescription("Texcoord", Veldrid.VertexElementSemantic.TextureCoordinate, Veldrid.VertexElementFormat.Float2),
                    new Veldrid.VertexElementDescription("Color", Veldrid.VertexElementSemantic.Color, Veldrid.VertexElementFormat.Byte4_Norm))
            };  
        }
    }

    public class RenderDebugVertexFactory : VertexFactory { 
        readonly static Veldrid.VertexLayoutDescription[] vertexLayoutDescriptions;
        static RenderDebugVertexFactory() {
            vertexLayoutDescriptions = new Veldrid.VertexLayoutDescription[] {
                new Veldrid.VertexLayoutDescription(
                    new Veldrid.VertexElementDescription("Position", Veldrid.VertexElementSemantic.Position, Veldrid.VertexElementFormat.Float3, 0),
                    new Veldrid.VertexElementDescription("Color", Veldrid.VertexElementSemantic.Color, Veldrid.VertexElementFormat.Byte4_Norm, 12))
            };
        }
    }

    public class DebugToolVertexFactory : VertexFactory { 
        readonly static Veldrid.VertexLayoutDescription[] vertexLayoutDescriptions;
        static DebugToolVertexFactory() {
            vertexLayoutDescriptions = new Veldrid.VertexLayoutDescription[] {
                new Veldrid.VertexLayoutDescription(
                    new Veldrid.VertexElementDescription("Position", Veldrid.VertexElementSemantic.Position, Veldrid.VertexElementFormat.Float3),
                    new Veldrid.VertexElementDescription("Texcoord", Veldrid.VertexElementSemantic.TextureCoordinate, Veldrid.VertexElementFormat.Float2),
                    new Veldrid.VertexElementDescription("Color", Veldrid.VertexElementSemantic.Color, Veldrid.VertexElementFormat.Byte4_Norm))
            };          
        }
    }

    public class DrawVertexVertexFactory : VertexFactory {
        readonly static Veldrid.VertexLayoutDescription[] vertexLayoutDescriptions;
        static DrawVertexVertexFactory() {
            vertexLayoutDescriptions = new Veldrid.VertexLayoutDescription[] {
                new Veldrid.VertexLayoutDescription(
                    new Veldrid.VertexElementDescription("POSITION", Veldrid.VertexElementSemantic.Position, Veldrid.VertexElementFormat.Float3, 0), // 12 bytes
                    new Veldrid.VertexElementDescription("TEXCOORD", Veldrid.VertexElementSemantic.TextureCoordinate, Veldrid.VertexElementFormat.Half2, 12), // 4 bytes
                    new Veldrid.VertexElementDescription("NORMAL", Veldrid.VertexElementSemantic.TextureCoordinate, Veldrid.VertexElementFormat.Byte4_Norm, 16), // 4 bytes
                    new Veldrid.VertexElementDescription("TANGENT", Veldrid.VertexElementSemantic.TextureCoordinate, Veldrid.VertexElementFormat.Byte4_Norm, 20), // 4 bytes
                    new Veldrid.VertexElementDescription("COLOR0", Veldrid.VertexElementSemantic.TextureCoordinate, Veldrid.VertexElementFormat.Byte4_Norm, 24), // 4 bytes
                    new Veldrid.VertexElementDescription("COLOR1", Veldrid.VertexElementSemantic.TextureCoordinate, Veldrid.VertexElementFormat.Byte4_Norm, 28)) // 4 bytes
            };
        }
    }

    public class ParticleVertexFactory : VertexFactory {
        readonly static Veldrid.VertexLayoutDescription[] vertexLayoutDescriptions;
        static ParticleVertexFactory() {
            vertexLayoutDescriptions = new Veldrid.VertexLayoutDescription[] {
                new Veldrid.VertexLayoutDescription(
                    new Veldrid.VertexElementDescription("POSITION", Veldrid.VertexElementSemantic.Position, Veldrid.VertexElementFormat.Float3, 0), // 12 bytes
                    new Veldrid.VertexElementDescription("TEXCOORD0", Veldrid.VertexElementSemantic.TextureCoordinate, Veldrid.VertexElementFormat.Half2, 12), // 4 bytes
                    new Veldrid.VertexElementDescription("TEXCOORD1", Veldrid.VertexElementSemantic.TextureCoordinate, Veldrid.VertexElementFormat.Half2, 16), // 4 bytes
                    new Veldrid.VertexElementDescription("TANGENT", Veldrid.VertexElementSemantic.TextureCoordinate, Veldrid.VertexElementFormat.Byte4_Norm, 20), // 4 bytes
                    new Veldrid.VertexElementDescription("COLOR0", Veldrid.VertexElementSemantic.TextureCoordinate, Veldrid.VertexElementFormat.Byte4_Norm, 24), // 4 bytes
                    new Veldrid.VertexElementDescription("COLOR1", Veldrid.VertexElementSemantic.TextureCoordinate, Veldrid.VertexElementFormat.Byte4_Norm, 28)) // 4 bytes
            };
        }
    }

    public class DrawVertexPackedVertexFactory : VertexFactory {
        public DrawVertexPackedVertexFactory(Mesh meshData, MeshRenderSystem renderSystem) : base(meshData, renderSystem) {
            buffers = new VertexBuffer[2];
            buffers[0] = new VertexBuffer();
            buffers[1] = new VertexBuffer();
            buffers[0].InitData(meshData.nativeVerticesPacked0, meshData.nativeVertexBufferSize, DrawVertexPacked0.SizeInBytes);
            buffers[1].InitData(meshData.nativeVerticesPacked1, meshData.nativeVertexBufferSize, DrawVertexPacked1.SizeInBytes);
        }        

        readonly static Veldrid.VertexLayoutDescription[] vertexLayoutDescriptions;
        static DrawVertexPackedVertexFactory() {
            vertexLayoutDescriptions = new Veldrid.VertexLayoutDescription[] {
                new Veldrid.VertexLayoutDescription(
                    new Veldrid.VertexElementDescription("POSITION", Veldrid.VertexElementSemantic.Position, Veldrid.VertexElementFormat.Float3, 0),
                    new Veldrid.VertexElementDescription("TEXCOORD", Veldrid.VertexElementSemantic.TextureCoordinate, Veldrid.VertexElementFormat.Half2, 12)),
                new Veldrid.VertexLayoutDescription(
                    new Veldrid.VertexElementDescription("NORMAL", Veldrid.VertexElementSemantic.TextureCoordinate, Veldrid.VertexElementFormat.Byte4_Norm, 0),
                    new Veldrid.VertexElementDescription("TANGENT", Veldrid.VertexElementSemantic.TextureCoordinate, Veldrid.VertexElementFormat.Byte4_Norm, 4),
                    new Veldrid.VertexElementDescription("COLOR0", Veldrid.VertexElementSemantic.TextureCoordinate, Veldrid.VertexElementFormat.Byte4_Norm, 8),
                    new Veldrid.VertexElementDescription("COLOR1", Veldrid.VertexElementSemantic.TextureCoordinate, Veldrid.VertexElementFormat.Byte4_Norm, 12))
            };
        }
    }

    public class DrawVertexStaticPackedVertexFactory : VertexFactory {
        public DrawVertexStaticPackedVertexFactory(Mesh meshData, MeshRenderSystem renderSystem) : base(meshData, renderSystem) {
            buffers = new VertexBuffer[2];
            buffers[0] = new VertexBuffer();
            buffers[1] = new VertexBuffer();
            buffers[0].InitData(meshData.nativeVerticesPacked0, meshData.nativeVertexBufferSize, DrawVertexPacked0.SizeInBytes);
            buffers[1].InitData(meshData.nativeVerticesPacked1, meshData.nativeVertexBufferSize, DrawVertexPacked1.SizeInBytes);
        }        

        readonly static Veldrid.VertexLayoutDescription[] vertexLayoutDescriptions;
        static DrawVertexStaticPackedVertexFactory() {
            vertexLayoutDescriptions = new Veldrid.VertexLayoutDescription[] {
                new Veldrid.VertexLayoutDescription(
                    new Veldrid.VertexElementDescription("POSITION", Veldrid.VertexElementSemantic.Position, Veldrid.VertexElementFormat.Float3, 0),
                    new Veldrid.VertexElementDescription("TEXCOORD0", Veldrid.VertexElementSemantic.TextureCoordinate, Veldrid.VertexElementFormat.Half2, 12)),
                new Veldrid.VertexLayoutDescription(
                    new Veldrid.VertexElementDescription("TEXCOORD1", Veldrid.VertexElementSemantic.TextureCoordinate, Veldrid.VertexElementFormat.Half2, 0),
                    new Veldrid.VertexElementDescription("NORMAL", Veldrid.VertexElementSemantic.TextureCoordinate, Veldrid.VertexElementFormat.Byte4_Norm, 4),
                    new Veldrid.VertexElementDescription("TANGENT", Veldrid.VertexElementSemantic.TextureCoordinate, Veldrid.VertexElementFormat.Byte4_Norm, 8),
                    new Veldrid.VertexElementDescription("COLOR0", Veldrid.VertexElementSemantic.TextureCoordinate, Veldrid.VertexElementFormat.Byte4_Norm, 12))
            };
        }
    }

    public class FullscreenVertexFactory : VertexFactory {
        readonly static Veldrid.VertexLayoutDescription[] vertexLayoutDescriptions;
        static FullscreenVertexFactory() {
            vertexLayoutDescriptions = new Veldrid.VertexLayoutDescription[] {
                new Veldrid.VertexLayoutDescription(
                    new Veldrid.VertexElementDescription("SV_VERTEXID", Veldrid.VertexElementSemantic.Position, Veldrid.VertexElementFormat.UInt1)
                )
            };  
        }        
    }

    public class ScreenPicVertexFactory : VertexFactory {
        readonly static Veldrid.VertexLayoutDescription[] vertexLayoutDescriptions;
        static ScreenPicVertexFactory() {
            vertexLayoutDescriptions = new Veldrid.VertexLayoutDescription[] {
                new Veldrid.VertexLayoutDescription(
                    new Veldrid.VertexElementDescription("Position", Veldrid.VertexElementSemantic.Position, Veldrid.VertexElementFormat.Float2, 0),
                    new Veldrid.VertexElementDescription("Texcoord", Veldrid.VertexElementSemantic.TextureCoordinate, Veldrid.VertexElementFormat.Float2, 8)
                )
            };  
        }
    }
}