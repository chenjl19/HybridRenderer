using System;
using System.IO;
using SharpDX;
using SharpDX.Mathematics;

namespace NT
{
    public struct Byte4 {
        public byte x;
        public byte y;
        public byte z;
        public byte w;

        public byte this[int index] {
            set {
                if(index == 0) {
                    x = value;
                } else if(index == 1) {
                    y = value;
                } else if(index == 2) {
                    z = value;
                } else if(index == 3) {
                    w = value;
                } else {
                    throw new IndexOutOfRangeException();
                }
            }

            get {
                if(index == 0) {
                    return x;
                } else if(index == 1) {
                    return y;
                } else if(index == 2) {
                    return z;
                } else if(index == 3) {
                    return w;
                } else {
                    throw new IndexOutOfRangeException();
                }
            }
        }

        public static implicit operator Byte4(Color color) {
            return new Byte4(color.R, color.G, color.B, color.A);
        }

        public Byte4(byte _x, byte _y, byte _z) {
            x = _x;
            y = _y;
            z = _z;
            w = 0;
        }

        public Byte4(byte _x, byte _y, byte _z, byte _w) {
            x = _x;
            y = _y;
            z = _z;
            w = _w;
        }

        public Byte4(float _x, float _y, float _z, float _w) {
            x = DrawVertex.FloatToByte(_x * 255f);
            y = DrawVertex.FloatToByte(_y * 255f);
            z = DrawVertex.FloatToByte(_z * 255f);
            w = DrawVertex.FloatToByte(_w * 255f);
        }
    }

    public struct DrawVertexPacked0 {
        public Vector3 position;
        public Half2 st;
        public static readonly int SizeInBytes = Utilities.SizeOf<DrawVertexPacked0>();   
    }

    public struct DrawVertexPacked1 {
        public Byte4 normal;
        public Byte4 tangent;
        public Byte4 color; 
        public Byte4 color1;

        // Must be normalized
        public void SetNormal(float x, float y, float z) {
            normal = new Byte4(DrawVertex.VertexFloatToByte(x), DrawVertex.VertexFloatToByte(y), DrawVertex.VertexFloatToByte(z));
        }

        // Must be normalized
        public void SetNormal(Vector3 n) {
            normal = new Byte4(DrawVertex.VertexFloatToByte(n.X), DrawVertex.VertexFloatToByte(n.Y), DrawVertex.VertexFloatToByte(n.Z));
        }

        public Vector3 GetNormal() {
            return new Vector3(DrawVertex.VertexByteToFloat(normal[0]), DrawVertex.VertexByteToFloat(normal[1]), DrawVertex.VertexByteToFloat(normal[2]));
        }

        public Vector3 GetTangent() {
            return new Vector3(DrawVertex.VertexByteToFloat(tangent[0]), DrawVertex.VertexByteToFloat(tangent[1]), DrawVertex.VertexByteToFloat(tangent[2]));
        }

        public void SetTangent(Vector3 t) {
            tangent = DrawVertex.VertexFloatToByte4(t.X, t.Y, t.Z);
        }

        public void SetTangent(float x, float y, float z) {
            tangent = DrawVertex.VertexFloatToByte4(x, y, z);
        }

        public void SetTangent(Vector4 t) {
            tangent = DrawVertex.VertexFloatToByte4(t.X, t.Y, t.Z);
            tangent.w = (byte)(t.W < 0f ? 0 : 255);
        }

        public void SetBitangentSign(float sign) {
            tangent[3] = (byte)((sign < 0f) ? 0 : 255);
        }

        public void SetBitangentSign(byte sign) {
            tangent[3] = (byte)(sign != 0 ? 0 : 255);
        }

        public void SetBitangent(Vector3 t) {
            Vector3 bitangent = Vector3.Cross(GetNormal(), GetTangent());
            SetBitangentSign(Vector3.Dot(bitangent, t));
        }

        public void SetBitangent(float x, float y, float z) {
           SetBitangent(new Vector3(x, y, z));
        }

        public void SetJointIndices(byte i0, byte i1, byte i2, byte i3) {
            color = new Byte4(i0, i1, i2, i3);
        }

        public void SetJointWeights(float w0, float w1, float w2, float w3) {
            color1 = new Byte4(w0, w1, w2, 1f - (w0 + w1 + w2));
        }

        public static readonly int SizeInBytes = Utilities.SizeOf<DrawVertexPacked1>();   
    }

    public struct DrawVertexStaticPacked1 {
        public Half2 lightmapST;
        public Color normal;
        public Color tangent;
        public Color color0;

	    // must be normalized already!
        public void SetNormal(float x, float y, float z) {
            normal = new Color(DrawVertex.VertexFloatToByte(x), DrawVertex.VertexFloatToByte(y), DrawVertex.VertexFloatToByte(z));
        }

	    // must be normalized already!
        public void SetNormal(Vector3 n) {
            normal = new Color(DrawVertex.VertexFloatToByte(n.X), DrawVertex.VertexFloatToByte(n.Y), DrawVertex.VertexFloatToByte(n.Z));
        }

	    // must be normalized already!
        public void SetTangent(Vector4 n) {
            tangent = new Color(DrawVertex.VertexFloatToByte(n.X), DrawVertex.VertexFloatToByte(n.Y), DrawVertex.VertexFloatToByte(n.Z), n.W < 0 ? 0 : 255);            
        }

        public Vector3 GetNormal() {
            return new Vector3(DrawVertex.VertexByteToFloat(normal[0]), DrawVertex.VertexByteToFloat(normal[1]), DrawVertex.VertexByteToFloat(normal[2]));
        }

        public void SetLightmapTexCoord(Vector2 texcoord) {
            lightmapST = texcoord;
        }

        public Vector2 GetLightmapTexCoord() {
            return lightmapST;
        }

        public static readonly int SizeInBytes = Utilities.SizeOf<DrawVertexStaticPacked1>();
    }

    public struct ParticleVertex {
        public Vector3 position;
        public Half2 st0;
        public Half2 st1;
        public Byte4 normal;
        public Byte4 color;
        public Byte4 color1;

        public static byte FloatToByte(float f) {
            int i = (int)f;
            if(i < 0) {
                return 0;
            } else if(i > 255) {
                return 255;
            }
            return (byte)i;
        }

        public static float VertexByteToFloat(byte x) {
            return ( (x) * ( 2.0f / 255.0f ) - 1.0f );
        }

        public static byte VertexFloatToByte(float f) {
            return FloatToByte(((f) + 1.0f ) * ( 255.0f / 2.0f ) + 0.5f);
        }

        public static Byte4 VertexFloatToByte4(float x, float y, float z) {
            return new Byte4(VertexFloatToByte(x), VertexFloatToByte(y), VertexFloatToByte(z));
        }

        public static Byte4 VertexFloatToByte4(float x, float y, float z, float w) {
            return new Byte4(VertexFloatToByte(x), VertexFloatToByte(y), VertexFloatToByte(z), VertexFloatToByte(w));
        }

	    // must be normalized already!
        public void SetNormal(float x, float y, float z) {
            normal = new Byte4(VertexFloatToByte(x), VertexFloatToByte(y), VertexFloatToByte(z));
        }

	    // must be normalized already!
        public void SetNormal(Vector3 n) {
            normal = new Byte4(VertexFloatToByte(n.X), VertexFloatToByte(n.Y), VertexFloatToByte(n.Z));
        }

        public Vector3 GetNormal() {
            return new Vector3(VertexByteToFloat(normal[0]), VertexByteToFloat(normal[1]), VertexByteToFloat(normal[2]));
        }

        public static readonly int SizeInBytes = Utilities.SizeOf<ParticleVertex>();
    }

    public struct DrawVertex {
        public Vector3 position;
        public Half2 st;
        public Byte4 normal;
        public Byte4 tangent;
        public Byte4 color;
        public Byte4 color1;

        public static DrawVertex DrawVertexColored(Vector3 xyz, Vector2 st, Byte4 color) {
            DrawVertex vertex = new DrawVertex();
            vertex.position = xyz;
            vertex.st = st;
            vertex.normal = color;
            return vertex;
        }

        public static byte FloatToByte(float f) {
            int i = (int)f;
            if(i < 0) {
                return 0;
            } else if(i > 255) {
                return 255;
            }
            return (byte)i;
        }

        public static float VertexByteToFloat(byte x) {
            return ( (x) * ( 2.0f / 255.0f ) - 1.0f );
        }

        public static byte VertexFloatToByte(float f) {
            return FloatToByte(((f) + 1.0f ) * ( 255.0f / 2.0f ) + 0.5f);
        }

        public static Byte4 VertexFloatToByte4(float x, float y, float z) {
            return new Byte4(VertexFloatToByte(x), VertexFloatToByte(y), VertexFloatToByte(z));
        }

        public static Byte4 VertexFloatToByte4(float x, float y, float z, float w) {
            return new Byte4(VertexFloatToByte(x), VertexFloatToByte(y), VertexFloatToByte(z), VertexFloatToByte(w));
        }

	    // must be normalized already!
        public void SetNormal(float x, float y, float z) {
            normal = new Byte4(VertexFloatToByte(x), VertexFloatToByte(y), VertexFloatToByte(z));
        }

	    // must be normalized already!
        public void SetNormal(Vector3 n) {
            normal = new Byte4(VertexFloatToByte(n.X), VertexFloatToByte(n.Y), VertexFloatToByte(n.Z));
        }

	    // must be normalized already!
        public void SetTangent(Vector4 n) {
            tangent = new Byte4(VertexFloatToByte(n.X), VertexFloatToByte(n.Y), VertexFloatToByte(n.Z), (byte)(n.W < 0 ? 0 : 255));            
        }

        public Vector3 GetNormal() {
            return new Vector3(VertexByteToFloat(normal[0]), VertexByteToFloat(normal[1]), VertexByteToFloat(normal[2]));
        }

        public void SetTexCoord(Vector2 texcoord) {
            st = texcoord;
        }

        public Vector2 GetTexCoord() {
            return st;
        }

        public static readonly int SizeInBytes = Utilities.SizeOf<DrawVertex>();
    }
}