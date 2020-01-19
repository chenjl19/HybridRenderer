using System;
using System.IO;
using SharpDX;
using SharpDX.Mathematics;

namespace NT
{
    public enum NPOTScale {
        ToNearest,
        ToLarger,
        ToSmaller,
        None
    } 

    public enum CubemapFaceId {
        POSITIVE_X,
        NEGATIVE_X,
        POSITIVE_Y,
        NEGATIVE_Y,
        POSITIVE_Z,
        NEGATIVE_Z,
    }

    public static class VectorExt {
        // v must be normalized.
        public static void NormalVectors(this Vector3 v, out Vector3 right, out Vector3 up) {
            float d = v.X * v.X + v.Y * v.Y;
            if(SharpDX.MathUtil.IsZero(d)) {
                up = new Vector3(1f, 0f, 0f);
            } else {
                d = 1f / MathF.Sqrt(d);
                up = new Vector3(v.Y * d, v.X * d, 0f);
            }
            right = Vector3.Cross(v, up);
        }

        public static Quaternion ToQuaternion(this Vector3 cq) {
            float w = 1f - cq.X * cq.X - cq.Y * cq.Y - cq.Z * cq.Z;
            w = w < 0f ? 0f : -MathF.Sqrt(w);
            return new Quaternion(cq, w);            
        }

        public static Vector2 ReadVector2(this BinaryReader reader) {
            return new Vector2(reader.ReadSingle(), reader.ReadSingle());
        }

        public static Vector3 ReadVector3(this BinaryReader reader) {
            return new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        public static Vector4 ReadVector4(this BinaryReader reader) {
            return new Vector4(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle());
        }

        public static short[] ReadShort4(this BinaryReader reader) {
            return new short[] {
                reader.ReadInt16(),
                reader.ReadInt16(),
                reader.ReadInt16(),
                reader.ReadInt16()
            };
        }

        public static Color ReadRGBA32(this BinaryReader reader) {
            return new Color(reader.ReadByte(), reader.ReadByte(), reader.ReadByte(), reader.ReadByte());
        }

        public static int[] ReadTriangle(this BinaryReader reader) {
            return new int[] {
                (int)reader.ReadUInt32(),
                (int)reader.ReadUInt32(),
                (int)reader.ReadUInt32()
            };
        }
    }
/*
    public static class SIMDProcessor {
        public static void BlendJoints(Span<JointTransform> joints, Span<JointTransform> inBlendJoints, float lerp, int numLerpJoints, Span<int> inLerpIndices) {
            for(int i = 0; i < numLerpJoints; i++) {
                int j = inLerpIndices[i];
                joints[j].q = Quaternion.Slerp(joints[j].q, inBlendJoints[j].q, lerp);
                joints[j].t = Vector3.Lerp(joints[j].t, inBlendJoints[j].t, lerp);
                joints[j].w = 0f;
            }
        }

        public static void ConvertPoseToMatrices(Matrix[] outMatrices, Span<JointTransform> jointFrame, float scale) {
            for(int i = 0; i < jointFrame.Length; i++) {
                Matrix.RotationQuaternion(ref jointFrame[i].q, out outMatrices[i]);
                outMatrices[i].TranslationVector = jointFrame[i].t * scale;
            }
        }
    }
 */
    public static class MathHelper {
        const int g_iExpBias = 127;
        const int g_iMantissaBits = 23;
        const float INV_PI = 1f / MathF.PI;    

        public static void QuatCalcW(ref Quaternion cq) {
            float w = 1f - cq.X * cq.X - cq.Y * cq.Y - cq.Z * cq.Z;
            cq.W = w < 0f ? 0f : -MathF.Sqrt(w);              
        } 

        public static uint[] PackR15G15B15A15(Vector4 v) {
            uint[] pack = new uint[2];
            pack[0] = ((uint)(v.X * 32767f) << 16) | (uint)(v.Y * 32767f);
            pack[1] = ((uint)(v.Z * 32767f) << 16) | (uint)(v.W * 32767f);
            return pack;
        }

        public static void PackR15G15B15A15(Vector4 v, out uint scaleBias0, out uint scaleBias1) {
            scaleBias0 = ((uint)(v.X * 32767f) << 16) | (uint)(v.Y * 32767f);
            scaleBias1 = ((uint)(v.Z * 32767f) << 16) | (uint)(v.W * 32767f);
        }

        public static Vector4 UnpackR15G15B15A15(uint[] v) {
            return new Vector4((v[0] >> 16 ) & 0xFFFF, v[0] & 0xFFFF, (v[1] >> 16 ) & 0xFFFF, v[1] & 0xFFFF ) * (1f / 32767f);            
        }

        // 2 clocks instead of 4 
        public static float ApproxLog2( float f )  {
            return ( (float)( (uint)BitConverter.SingleToInt32Bits( f ) ) / ( 1 << g_iMantissaBits ) - g_iExpBias );
        }

        // 2 clocks instead of 4    
        public static float ApproxExp2( float f )  {
            return BitConverter.Int32BitsToSingle( (int)( ( f + g_iExpBias ) * ( 1 << g_iMantissaBits ) ) );
        }

        // 4 clocks instead of 9 (3 clocks if power is a constant, 2 clocks if base is a constant)
        public static float ApproxPow( float fBase, float fPower )  {
            return BitConverter.Int32BitsToSingle( (int)( fPower * (float)( (uint)BitConverter.SingleToInt32Bits( fBase ) ) - ( fPower - 1 ) * g_iExpBias * ( 1 << g_iMantissaBits ) ) );
        }

        // color pack/unpack utils
	    public static uint PackR8G8B8A8(Vector4 value) {
		    value = Saturate( value );
		    return ( ( ( (uint)( value.X * 255.0 ) ) << 24 ) | ( ( (uint)( value.Y * 255.0 ) ) << 16 ) | ( ( (uint)( value.Z * 255.0 ) ) << 8 ) | ( (uint)( value.W * 255.0 ) ) );
	    }

	    public static uint PackR8G8B8A8(float x, float y, float z, float w) {
            return PackR8G8B8A8(new Vector4(x, y, z, w));
	    }

	    public static uint PackR8G8B8A8(Color value) {
		    return ( ( ( (uint)( value.R ) ) << 24 ) | ( ( (uint)( value.G ) ) << 16 ) | ( ( (uint)( value.B ) ) << 8 ) | ( (uint)( value.A ) ) );
	    }

	    public static Vector4 UnpackR8G8B8A8(uint value) {
		    return new Vector4( ( value >> 24 ) & 0xFF, ( value >> 16 ) & 0xFF, ( value >> 8 ) & 0xFF, value & 0xFF ) / 255f;
	    }

        public static float Saturate(float v) {
            return Math.Clamp(v, 0f, 1f);
        }

        public static Color Saturate(Color v) {
            return Color.Clamp(v, Color.Black, Color.White);
        }

        public static Vector3 Saturate(Vector3 v) {
            return Vector3.Clamp(v, Vector3.Zero, Vector3.One);
        }

        public static Vector4 Saturate(Vector4 v) {
            return Vector4.Clamp(v, Vector4.Zero, Vector4.One);
        }

        // RGBE (ward 1984)
        public static uint PackRGBE(Vector3 value ) {  
            float sharedExp = MathF.Ceiling( ApproxLog2( MathF.Max( MathF.Max( value.X, value.Y ), value.Z ) ) );
            return PackR8G8B8A8( Saturate(new Vector4( value / ApproxExp2( sharedExp ), ( sharedExp + 128.0f ) / 255.0f ) ) );
        }

        public static Vector3 UnpackRGBE( uint value ) {
            Vector4 rgbe = UnpackR8G8B8A8( value );
            return (Vector3)rgbe * ApproxExp2( rgbe.W * 255.0f - 128.0f );
        }

        public static Vector3 UnpackRGBE( Vector4 rgbe ) {
            return (Vector3)rgbe * ApproxExp2( rgbe.W * 255.0f - 128.0f );
        }

        public static float Integrate(Vector2 v, float frac) {
            return Integrate(v.X, v.Y, frac);
        }

        public static float Integrate(float from, float to, float frac) {
            return (from + frac * (to - from) * 0.5f) * frac;
        }

        public static void SinCos16(float a, out float s, out float c) {
	        float t, d;
            if((a < 0.0f ) || (a >= (MathF.PI * 2f))) {
                a -= MathF.Floor( a * (1f / MathF.PI * 2f)) * (MathF.PI * 2f);
            }
            if(a < MathF.PI) {
                if( a > (MathF.PI * 0.5f) ) {
                    a = MathF.PI - a;
                    d = -1.0f;
                } else {
                    d = 1.0f;
                }
            } else {
                if(a > MathF.PI + (MathF.PI * 0.5f)) {
                    a = a - MathF.PI * 2f;
                    d = 1.0f;
                } else {
                    a = MathF.PI - a;
                    d = -1.0f;
                }
            }
            t = a * a;
            s = a * ( ( ( ( ( -2.39e-08f * t + 2.7526e-06f ) * t - 1.98409e-04f ) * t + 8.3333315e-03f ) * t - 1.666666664e-01f ) * t + 1.0f );
            c = d * ( ( ( ( ( -2.605e-07f * t + 2.47609e-05f ) * t - 1.3888397e-03f ) * t + 4.16666418e-02f ) * t - 4.999999963e-01f ) * t + 1.0f );            
        }

        public static readonly Vector3 Vec3Right = new Vector3(1f, 0f, 0f);
        public static readonly Vector3 Vec3Forward = new Vector3(0f, 1f, 0f);
        public static readonly Vector3 Vec3Up = new Vector3(0f, 0f, 1f);
        public static readonly Vector3 Vec3Left = new Vector3(-1f, 0f, 0f);
        public static readonly Vector3 Vec3Backward = new Vector3(0f, -1f, 0f);
        public static readonly Vector3 Vec3Down = new Vector3(0f, 0f, -1f);
        public static readonly Matrix ViewFlipMatrixRH = new Matrix(
                                                        1f, 0f, 0f, 0f,
                                                        0f, 0f, -1f, 0f,
                                                        0f, 1f, 0f, 0f,
                                                        0f, 0f, 0f, 1f
                                                    );
        public static readonly Matrix ViewFlipMatrixLH = new Matrix(
                                                        1f, 0f, 0f, 0f,
                                                        0f, 0f, 1f, 0f,
                                                        0f, 1f, 0f, 0f,
                                                        0f, 0f, 0f, 1f            
        );

        // POSITIVE_X
        // NEGATIVE_X 
        // POSITIVE_Y
        // NEGATIVE_Y
        // POSITIVE_Z
        // NEGATIVE_Z
        public static readonly Matrix[] PointLightViewMatrices = {
            new Matrix(
                0f, 1f, 0f, 0f,
                -1f, 0f, 0f, 0f,
                0f, 0f, 1f, 0f,
                0f, 0f, 0f, 1f
            ) * MathHelper.ViewFlipMatrixRH,

            new Matrix(
                0f, -1f, 0f, 0f,
                1f, 0f, 0f, 0f,
                0f, 0f, 1f, 0f,
                0f, 0f, 0f, 1f
            ) * MathHelper.ViewFlipMatrixRH,

            new Matrix(
                1f, 0f, 0f, 0f,
                0f, 1f, 0f, 0f,
                0f, 0f, 1f, 0f,
                0f, 0f, 0f, 1f
            ) * MathHelper.ViewFlipMatrixRH,

            new Matrix(
                -1f, 0f, 0f, 0f,
                0f, -1f, 0f, 0f,
                0f, 0f, 1f, 0f,
                0f, 0f, 0f, 1f
            ) * MathHelper.ViewFlipMatrixRH,

            new Matrix(
                1f, 0f, 0f, 0f,
                0f, 0f, -1f, 0f,
                0f, 1f, 0f, 0f,
                0f, 0f, 0f, 1f
            ) * MathHelper.ViewFlipMatrixRH,

            new Matrix(
                1f, 0f, 0f, 0f,
                0f, 0f, 1f, 0f,
                0f, -1f, 0f, 0f,
                0f, 0f, 0f, 1f
            ) * MathHelper.ViewFlipMatrixRH   
        };

        public static readonly Matrix ScaleBiasMatrix = new Matrix(
            0.5f, 0f, 0f, 0f,
            0f, -0.5f, 0f, 0f,
            0f, 0f, 1f, 0f,
            0.5f, 0.5f, 0f, 1f
        );

        public static readonly Matrix[] CubemapViewMatrices = new Matrix[] {
            // POSITIVE_X
            new Matrix(
                0f, 1f, 0f, 0f,
                -1f, 0f, 0f, 0f,
                0f, 0f, 1f, 0f,
                0f, 0f, 0f, 1f
            ) * MathHelper.ViewFlipMatrixRH,
            // NEGATIVE_X 
             new Matrix(
                0f, -1f, 0f, 0f,
                1f, 0f, 0f, 0f,
                0f, 0f, 1f, 0f,
                0f, 0f, 0f, 1f
            ) * MathHelper.ViewFlipMatrixRH,

            // POSITIVE_Z
            new Matrix(
                1f, 0f, 0f, 0f,
                0f, 0f, -1f, 0f,
                0f, 1f, 0f, 0f,
                0f, 0f, 0f, 1f
            ) * MathHelper.ViewFlipMatrixRH,
            // NEGATIVE_Z
            new Matrix(
                1f, 0f, 0f, 0f,
                0f, 0f, 1f, 0f,
                0f, -1f, 0f, 0f,
                0f, 0f, 0f, 1f
            ) * MathHelper.ViewFlipMatrixRH,

            // POSITIVE_Y
            new Matrix(
                1f, 0f, 0f, 0f,
                0f, 1f, 0f, 0f,
                0f, 0f, 1f, 0f,
                0f, 0f, 0f, 1f
            ) * MathHelper.ViewFlipMatrixRH,
            // NEGATIVE_Y
            new Matrix(
                -1f, 0f, 0f, 0f,
                0f, -1f, 0f, 0f,
                0f, 0f, 1f, 0f,
                0f, 0f, 0f, 1f
            ) * MathHelper.ViewFlipMatrixRH,     
        };

        public static void BoundingBoxClear(ref BoundingBox self) {
            self.Minimum = MathHelper.BoundingBoxMax;
            self.Maximum = MathHelper.BoundingBoxMin;
        }

        public static void BoundingBoxAddBounds(ref SharpDX.BoundingBox self, SharpDX.BoundingBox b) {
            if(b.Minimum.X < self.Minimum.X) {
                self.Minimum.X = b.Minimum.X;
            }
            if(b.Minimum.Y < self.Minimum.Y) {
                self.Minimum.Y = b.Minimum.Y;
            }
            if(b.Minimum.Z < self.Minimum.Z) {
                self.Minimum.Z = b.Minimum.Z;
            }
            if(b.Maximum.X > self.Maximum.X) {
                self.Maximum.X = b.Maximum.X;
            }
            if(b.Maximum.Y > self.Maximum.Y) {
                self.Maximum.Y = b.Maximum.Y;
            }
            if(b.Maximum.Z > self.Maximum.Z) {
                self.Maximum.Z = b.Maximum.Z;
            }
        }

        public static void BoundingBoxAddPoint(ref SharpDX.BoundingBox self, Vector3 pt) {
            if(pt.X < self.Minimum.X) {
                self.Minimum.X = pt.X;
            }
            if(pt.X > self.Maximum.X) {
                self.Maximum.X = pt.X;
            }
            if(pt.Y < self.Minimum.Y) {
                self.Minimum.Y = pt.Y;
            }
            if(pt.Y > self.Maximum.Y) {
                self.Maximum.Y = pt.Y;
            }
            if(pt.Z < self.Minimum.Z) {
                self.Minimum.Z = pt.Z;
            }
            if(pt.Z > self.Maximum.Z) {
                self.Maximum.Z = pt.Z;
            }
        }

        public static void BoundingBoxExpand(ref BoundingBox self, float d) {
            self.Minimum[0] -= d;
            self.Minimum[1] -= d;
            self.Minimum[2] -= d;
            self.Maximum[0] += d;
            self.Maximum[1] += d;
            self.Maximum[2] += d;
        }

        public static uint ReverseBits2( uint idx ) {
            idx = ( ( idx & 1 ) << 1) | ( ( idx & 0x2 ) >> 1 );
            return idx;
        }

        public static uint ReverseBits4( uint idx ) {
            idx = ( ( idx & 0x5 ) << 1) | ( ( idx & 0xA ) >> 1 );
            idx = ( ( idx & 0x3 ) << 2) | ( ( idx & 0xC ) >> 2 );
            return idx;
        }

        public static uint ReverseBits8( uint idx ) {
            idx = ( ( idx & 0x55 ) << 1) | ( ( idx & 0xAA ) >> 1 );
            idx = ( ( idx & 0x33 ) << 2) | ( ( idx & 0xCC ) >> 2 );
            idx = ( ( idx & 0x0F ) << 4) | ( ( idx & 0xF0 ) >> 4 );
            return idx;
        }

        public static uint ReverseBits16( uint idx ) {
            idx = ( ( idx & 0x5555 ) << 1) | ( ( idx & 0xAAAA ) >> 1 );
            idx = ( ( idx & 0x3333 ) << 2) | ( ( idx & 0xCCCC ) >> 2 );
            idx = ( ( idx & 0x0F0F ) << 4) | ( ( idx & 0xF0F0 ) >> 4 );
            idx = ( ( idx & 0x00FF ) << 8) | ( ( idx & 0xFF00 ) >> 8 );
            return idx;
        }

        public static uint ReverseBits32( uint idx ) {
            idx = ( idx << 16 ) | ( idx >> 16);
            idx = ( ( idx & 0x55555555 ) << 1) | ( ( idx & 0xAAAAAAAA ) >> 1 );
            idx = ( ( idx & 0x33333333 ) << 2) | ( ( idx & 0xCCCCCCCC ) >> 2 );
            idx = ( ( idx & 0x0F0F0F0F ) << 4) | ( ( idx & 0xF0F0F0F0 ) >> 4 );
            idx = ( ( idx & 0x00FF00FF ) << 8) | ( ( idx & 0xFF00FF00 ) >> 8 );
            return idx;
        }

        public static float ReverseBits32ToFloat( uint idx ) {
            idx = ReverseBits32( idx );
            return (float)( idx ) * 2.3283064365386963e-10f;
        }

        public static Vector2 Hammersley2D(uint numSamples, uint idx) {
            float rcpNumSamples = 1.0f / (float)(numSamples);
            return new Vector2((float)(idx + 1) * rcpNumSamples, ReverseBits32ToFloat(idx + 1));
        }

        public static Vector2 Hammersley2D_4bits( uint numSamples, uint idx ) {
            float rcpNumSamples = 1.0f / (float)( numSamples );
            return new Vector2( (float)( idx + 1 ) * rcpNumSamples, (float)( ReverseBits4( idx + 1 ) ) / 16.0f );
        }

        public static Vector4 ScreenToViewPoint(ref Matrix projection, int width, int height, Vector4 p) {
            Vector2 uv = new Vector2(p.X / width, p.Y / height);
            Vector4 clip = new Vector4(new Vector2(uv.X, 1f - uv.Y) * 2f - 1f, p.Z, p.W);
            clip.X /= projection[0, 0] != 0f ? projection[0, 0] : 1f;
            clip.Y /= projection[1, 1] != 0f ? projection[1, 1] : 1f;
            return clip;
        }

        public static Vector3 ClipToViewPoint(float x, float y, float z, ref Matrix projectionMatrix) {
            float m00 = projectionMatrix[0, 0];
            float m11 = projectionMatrix[1, 1];
            Vector3 view = new Vector3(m00 != 0f ? (x / m00) : 1f, m11 != 0f ? (y / m11) : 1f, z);
            view.X *= -z;
            view.Y *= -z;
            return view;
        }

        public static Vector3 ClipToWorldPoint(float x, float y, float z, ref Matrix invViewProjectionMatrix) {
            //var v = Vector3.Transform(new Vector3(x, y, z), invViewProjectionMatrix);
            //return (Vector3)(v / v.W);
            return Vector3.TransformCoordinate(new Vector3(x, y, z), invViewProjectionMatrix);
        }

        public static Vector2 ScreenToClip(int width, int height, Vector2 p) {
            Vector2 uv = new Vector2(p.X / width, p.Y / height);
            return new Vector2(uv.X, 1f - uv.Y) * 2f - 1f;
        }

        public static BoundingBox TransformBoundingBox(BoundingBox boundingBox, Matrix m) {
            return TransformBoundingBox(ref boundingBox, ref m);
        }

        public static BoundingBox TransformBoundingBox(BoundingBox boundingBox, ref Matrix m) {
            return TransformBoundingBox(ref boundingBox, ref m);
        }

        public static BoundingBox TransformBoundingBox(ref BoundingBox boundingBox, ref Matrix m) {
            
            var xa = m.Right * boundingBox.Minimum.X;
            var xb = m.Right * boundingBox.Maximum.X;
        
            var ya = m.Up * boundingBox.Minimum.Y;
            var yb = m.Up * boundingBox.Maximum.Y;
        
            var za = m.Backward * boundingBox.Minimum.Z;
            var zb = m.Backward * boundingBox.Maximum.Z;
        
            boundingBox = new BoundingBox(
                Vector3.Min(xa, xb) + Vector3.Min(ya, yb) + Vector3.Min(za, zb) + m.TranslationVector,
                Vector3.Max(xa, xb) + Vector3.Max(ya, yb) + Vector3.Max(za, zb) + m.TranslationVector
            );
            return boundingBox;
            
            /*
            var corners = boundingBox.GetCorners();
            for(int i = 0; i < corners.Length; i++) {
                corners[i] = Vector3.TransformCoordinate(corners[i], m);
            }
            return BoundingBox.FromPoints(corners);
            */
        }

        public static readonly Vector3 BoundingBoxMin = new Vector3(-1e30f);
        public static readonly Vector3 BoundingBoxMax = new Vector3(1e30f);
        public static readonly BoundingBox NullBoundingBox = new BoundingBox(BoundingBoxMax, BoundingBoxMin);

        public static readonly Vector3[][] cubemapFaceAxis = {
            new []{new Vector3(0f, -1f, 0f), new Vector3(1f, 0f, 0f), new Vector3(0f, 0f, 1f)}, // posx
            new []{new Vector3(0f, 1f, 0f), new Vector3(-1f, 0f, 0f), new Vector3(0f, 0f, 1f)}, // negx
                        
            new []{new Vector3(1f, 0f, 0f), new Vector3(0f, 1f, 0f), new Vector3(0f, 0f, 1f)}, // posz
            new []{new Vector3(-1f, 0f, 0f), new Vector3(0f, -1f, 0f), new Vector3(0f, 0f, 1f)}, // negz

            new []{new Vector3(1f, 0f, 0f), new Vector3(0f, 0f, 1f), new Vector3(0f, -1f, 0f)}, // posy
            new []{new Vector3(1f, 0f, 0f), new Vector3(0f, 0f, -1f), new Vector3(0f, 1f, 0f)}, // negy
        };

        // Compute the square distance between a point p and an AABB b.
        // Source: Real-time collision detection, Christer Ericson (2005)
        static float SqDistancePointAABB(Vector3 p, BoundingBox b) {
            float sqDistance = 0.0f;

            for ( int i = 0; i < 3; ++i ) {
                float v = p[i];
                if ( v < b.Minimum[i] ) sqDistance += MathF.Pow( b.Minimum[i] - v, 2 );
                if ( v > b.Maximum[i] ) sqDistance += MathF.Pow( v - b.Maximum[i], 2 );
            }

            return sqDistance;
        }

        public static bool SphereInsideAABB(ref BoundingSphere sphere, ref BoundingBox aabb) {
            float sqDistance = SqDistancePointAABB(sphere.Center, aabb);
            return sqDistance <= sphere.Radius * sphere.Radius;
        }

        public static float InverseSafe(float f) {
            return MathUtil.IsZero(MathF.Abs(f)) ? 0f : 1f / f;
        }

        public static Vector3 InverseSafe(Vector3 v) {
            return new Vector3(InverseSafe(v.X), InverseSafe(v.Y), InverseSafe(v.Z));
        }

        public static bool PowerOfTwo(int i) {
            return (i & (i - 1)) == 0;
        }

        public static int MakePowerOfTwo(int n) {
            int	pot;
            for ( pot = 1; pot < n; pot <<= 1 ) {
            }
            return pot;
        }

        static readonly float log2 = 1f / MathF.Log(2);

        public static float Log2(float v) {
            return MathF.Log(v) * log2;
        }        

        public static int MakePowerOfTwo(int n, NPOTScale scale) {
            int pot = MakePowerOfTwo(n);
            switch(scale) {
                case NPOTScale.ToNearest:
                default:
                    int prev = pot >> 1;
                    return (n - prev) < (pot - n) ? prev : pot;
                case NPOTScale.ToLarger:
                    return pot;
                case NPOTScale.ToSmaller:
                    return pot >> 1;
            }
        }
/*
        public static Color CorrelatedColorTemperatureToRGB(float kelvin) {
            float tmpKelvin = MathUtil.Clamp(kelvin, 1000, 40000);
            tmpKelvin = (int)(tmpKelvin / 100);
            Color rgb = Color.Black;
            // Red
            if(tmpKelvin <= 66) {
                rgb.R = 255;
            } else {

            }
        }
*/
        public static int Align(int size, int align) {
            return (size + align - 1) & ~(align - 1);
        }

        public static void Vector3FromLatLong(ref Vector3 _vec, float _u, float _v)
        {
            float phi = _u * 2.0f * MathUtil.Pi;
            float theta = _v * MathUtil.Pi;

            float st = MathF.Sin(theta);
            float sp = MathF.Sin(phi);
            float ct = MathF.Cos(theta);
            float cp = MathF.Cos(phi);
            
            _vec[0] = -st * sp;
            _vec[1] = -st * cp;
            _vec[2] = ct;
        }

        public static void LatLongFromVector3(ref float _u, ref float _v, Vector3 _vec)
        {
            float phi = MathF.Atan2(_vec[0], _vec[1]);
            float theta = MathF.Acos(_vec[2]);

            _u = (MathUtil.Pi + phi) * (1.0f / MathUtil.Pi) * 0.5f;
            _v = theta * (1.0f / MathUtil.Pi);
        }

        public static byte Ftob(float f) {
            int i = (int)f;
            if ( i < 0 ) {
                return 0;
            } else if ( i > 255 ) {
                return 255;
            } else {
                return (byte)i;
            }
        }

        public static Vector3 AngleTo(Vector3 from, Vector3 location)
        {
            Vector3 angle = new Vector3();
            Vector3 v3 = Vector3.Normalize(location - from);

            angle.X = (float)Math.Asin(v3.Y);
            angle.Y = (float)Math.Atan2((double)-v3.X, (double)-v3.Z);

            return angle;
        }

        public static void Vector3OrthoNormalize(ref Vector3 normal, ref Vector3 tangent) {
            normal.Normalize();

            Vector3 proj = normal * Vector3.Dot(tangent, normal);
            tangent = tangent - proj;
            tangent.Normalize();
        }

        public static Vector3 QuaternionToEulerAngleVector3(Quaternion rotation)
        {
            Vector3 rotationaxes = Vector3.Zero;
            Vector3 forward = Vector3.Transform(MathHelper.Vec3Forward, rotation);
            Vector3 up = Vector3.Transform(MathHelper.Vec3Up, rotation);

            rotationaxes = AngleTo(Vector3.Zero, forward);

            if (rotationaxes.X == MathUtil.PiOverTwo)
            {
                rotationaxes.Y = (float)Math.Atan2((double)up.X, (double)up.Z);
                rotationaxes.Z = 0;
            }
            else if (rotationaxes.X == -MathUtil.PiOverTwo)
            {
                rotationaxes.Y = (float)Math.Atan2((double)-up.X, (double)-up.Z);
                rotationaxes.Z = 0;
            }
            else
            {
                up = Vector3.Transform(up, Matrix3x3.RotationY(-rotationaxes.Y));
                up = Vector3.Transform(up, Matrix3x3.RotationX(-rotationaxes.X));

                rotationaxes.Z = (float)Math.Atan2((double)-up.Z, (double)up.Y);
            }

            return new Vector3(MathUtil.RadiansToDegrees(rotationaxes.X), MathUtil.RadiansToDegrees(rotationaxes.Y), MathUtil.RadiansToDegrees(rotationaxes.Z));
        }

        static readonly Vector4[] HALTON = {
                new Vector4(0.5000000000f, 0.3333333333f, 0.2000000000f, 0.1428571429f),
                new Vector4(0.2500000000f, 0.6666666667f, 0.4000000000f, 0.2857142857f),
                new Vector4(0.7500000000f, 0.1111111111f, 0.6000000000f, 0.4285714286f),
                new Vector4(0.1250000000f, 0.4444444444f, 0.8000000000f, 0.5714285714f),
                new Vector4(0.6250000000f, 0.7777777778f, 0.0400000000f, 0.7142857143f),
                new Vector4(0.3750000000f, 0.2222222222f, 0.2400000000f, 0.8571428571f),
                new Vector4(0.8750000000f, 0.5555555556f, 0.4400000000f, 0.0204081633f),
                new Vector4(0.0625000000f, 0.8888888889f, 0.6400000000f, 0.1632653061f),
                new Vector4(0.5625000000f, 0.0370370370f, 0.8400000000f, 0.3061224490f),
                new Vector4(0.3125000000f, 0.3703703704f, 0.0800000000f, 0.4489795918f),
                new Vector4(0.8125000000f, 0.7037037037f, 0.2800000000f, 0.5918367347f),
                new Vector4(0.1875000000f, 0.1481481481f, 0.4800000000f, 0.7346938776f),
                new Vector4(0.6875000000f, 0.4814814815f, 0.6800000000f, 0.8775510204f),
                new Vector4(0.4375000000f, 0.8148148148f, 0.8800000000f, 0.0408163265f),
                new Vector4(0.9375000000f, 0.2592592593f, 0.1200000000f, 0.1836734694f),
                new Vector4(0.0312500000f, 0.5925925926f, 0.3200000000f, 0.3265306122f),
                new Vector4(0.5312500000f, 0.9259259259f, 0.5200000000f, 0.4693877551f),
                new Vector4(0.2812500000f, 0.0740740741f, 0.7200000000f, 0.6122448980f),
                new Vector4(0.7812500000f, 0.4074074074f, 0.9200000000f, 0.7551020408f),
                new Vector4(0.1562500000f, 0.7407407407f, 0.1600000000f, 0.8979591837f),
                new Vector4(0.6562500000f, 0.1851851852f, 0.3600000000f, 0.0612244898f),
                new Vector4(0.4062500000f, 0.5185185185f, 0.5600000000f, 0.2040816327f),
                new Vector4(0.9062500000f, 0.8518518519f, 0.7600000000f, 0.3469387755f),
                new Vector4(0.0937500000f, 0.2962962963f, 0.9600000000f, 0.4897959184f),
                new Vector4(0.5937500000f, 0.6296296296f, 0.0080000000f, 0.6326530612f),
                new Vector4(0.3437500000f, 0.9629629630f, 0.2080000000f, 0.7755102041f),
                new Vector4(0.8437500000f, 0.0123456790f, 0.4080000000f, 0.9183673469f),
                new Vector4(0.2187500000f, 0.3456790123f, 0.6080000000f, 0.0816326531f),
                new Vector4(0.7187500000f, 0.6790123457f, 0.8080000000f, 0.2244897959f),
                new Vector4(0.4687500000f, 0.1234567901f, 0.0480000000f, 0.3673469388f),
                new Vector4(0.9687500000f, 0.4567901235f, 0.2480000000f, 0.5102040816f),
                new Vector4(0.0156250000f, 0.7901234568f, 0.4480000000f, 0.6530612245f),
                new Vector4(0.5156250000f, 0.2345679012f, 0.6480000000f, 0.7959183673f),
                new Vector4(0.2656250000f, 0.5679012346f, 0.8480000000f, 0.9387755102f),
                new Vector4(0.7656250000f, 0.9012345679f, 0.0880000000f, 0.1020408163f),
                new Vector4(0.1406250000f, 0.0493827160f, 0.2880000000f, 0.2448979592f),
                new Vector4(0.6406250000f, 0.3827160494f, 0.4880000000f, 0.3877551020f),
                new Vector4(0.3906250000f, 0.7160493827f, 0.6880000000f, 0.5306122449f),
                new Vector4(0.8906250000f, 0.1604938272f, 0.8880000000f, 0.6734693878f),
                new Vector4(0.0781250000f, 0.4938271605f, 0.1280000000f, 0.8163265306f),
                new Vector4(0.5781250000f, 0.8271604938f, 0.3280000000f, 0.9591836735f),
                new Vector4(0.3281250000f, 0.2716049383f, 0.5280000000f, 0.1224489796f),
                new Vector4(0.8281250000f, 0.6049382716f, 0.7280000000f, 0.2653061224f),
                new Vector4(0.2031250000f, 0.9382716049f, 0.9280000000f, 0.4081632653f),
                new Vector4(0.7031250000f, 0.0864197531f, 0.1680000000f, 0.5510204082f),
                new Vector4(0.4531250000f, 0.4197530864f, 0.3680000000f, 0.6938775510f),
                new Vector4(0.9531250000f, 0.7530864198f, 0.5680000000f, 0.8367346939f),
                new Vector4(0.0468750000f, 0.1975308642f, 0.7680000000f, 0.9795918367f),
                new Vector4(0.5468750000f, 0.5308641975f, 0.9680000000f, 0.0029154519f),
                new Vector4(0.2968750000f, 0.8641975309f, 0.0160000000f, 0.1457725948f),
                new Vector4(0.7968750000f, 0.3086419753f, 0.2160000000f, 0.2886297376f),
                new Vector4(0.1718750000f, 0.6419753086f, 0.4160000000f, 0.4314868805f),
                new Vector4(0.6718750000f, 0.9753086420f, 0.6160000000f, 0.5743440233f),
                new Vector4(0.4218750000f, 0.0246913580f, 0.8160000000f, 0.7172011662f),
                new Vector4(0.9218750000f, 0.3580246914f, 0.0560000000f, 0.8600583090f),
                new Vector4(0.1093750000f, 0.6913580247f, 0.2560000000f, 0.0233236152f),
                new Vector4(0.6093750000f, 0.1358024691f, 0.4560000000f, 0.1661807580f),
                new Vector4(0.3593750000f, 0.4691358025f, 0.6560000000f, 0.3090379009f),
                new Vector4(0.8593750000f, 0.8024691358f, 0.8560000000f, 0.4518950437f),
                new Vector4(0.2343750000f, 0.2469135802f, 0.0960000000f, 0.5947521866f),
                new Vector4(0.7343750000f, 0.5802469136f, 0.2960000000f, 0.7376093294f),
                new Vector4(0.4843750000f, 0.9135802469f, 0.4960000000f, 0.8804664723f),
                new Vector4(0.9843750000f, 0.0617283951f, 0.6960000000f, 0.0437317784f),
                new Vector4(0.0078125000f, 0.3950617284f, 0.8960000000f, 0.1865889213f),
                new Vector4(0.5078125000f, 0.7283950617f, 0.1360000000f, 0.3294460641f),
                new Vector4(0.2578125000f, 0.1728395062f, 0.3360000000f, 0.4723032070f),
                new Vector4(0.7578125000f, 0.5061728395f, 0.5360000000f, 0.6151603499f),
                new Vector4(0.1328125000f, 0.8395061728f, 0.7360000000f, 0.7580174927f),
                new Vector4(0.6328125000f, 0.2839506173f, 0.9360000000f, 0.9008746356f),
                new Vector4(0.3828125000f, 0.6172839506f, 0.1760000000f, 0.0641399417f),
                new Vector4(0.8828125000f, 0.9506172840f, 0.3760000000f, 0.2069970845f),
                new Vector4(0.0703125000f, 0.0987654321f, 0.5760000000f, 0.3498542274f),
                new Vector4(0.5703125000f, 0.4320987654f, 0.7760000000f, 0.4927113703f),
                new Vector4(0.3203125000f, 0.7654320988f, 0.9760000000f, 0.6355685131f),
                new Vector4(0.8203125000f, 0.2098765432f, 0.0240000000f, 0.7784256560f),
                new Vector4(0.1953125000f, 0.5432098765f, 0.2240000000f, 0.9212827988f),
                new Vector4(0.6953125000f, 0.8765432099f, 0.4240000000f, 0.0845481050f),
                new Vector4(0.4453125000f, 0.3209876543f, 0.6240000000f, 0.2274052478f),
                new Vector4(0.9453125000f, 0.6543209877f, 0.8240000000f, 0.3702623907f),
                new Vector4(0.0390625000f, 0.9876543210f, 0.0640000000f, 0.5131195335f),
                new Vector4(0.5390625000f, 0.0041152263f, 0.2640000000f, 0.6559766764f),
                new Vector4(0.2890625000f, 0.3374485597f, 0.4640000000f, 0.7988338192f),
                new Vector4(0.7890625000f, 0.6707818930f, 0.6640000000f, 0.9416909621f),
                new Vector4(0.1640625000f, 0.1152263374f, 0.8640000000f, 0.1049562682f),
                new Vector4(0.6640625000f, 0.4485596708f, 0.1040000000f, 0.2478134111f),
                new Vector4(0.4140625000f, 0.7818930041f, 0.3040000000f, 0.3906705539f),
                new Vector4(0.9140625000f, 0.2263374486f, 0.5040000000f, 0.5335276968f),
                new Vector4(0.1015625000f, 0.5596707819f, 0.7040000000f, 0.6763848397f),
                new Vector4(0.6015625000f, 0.8930041152f, 0.9040000000f, 0.8192419825f),
                new Vector4(0.3515625000f, 0.0411522634f, 0.1440000000f, 0.9620991254f),
                new Vector4(0.8515625000f, 0.3744855967f, 0.3440000000f, 0.1253644315f),
                new Vector4(0.2265625000f, 0.7078189300f, 0.5440000000f, 0.2682215743f),
                new Vector4(0.7265625000f, 0.1522633745f, 0.7440000000f, 0.4110787172f),
                new Vector4(0.4765625000f, 0.4855967078f, 0.9440000000f, 0.5539358601f),
                new Vector4(0.9765625000f, 0.8189300412f, 0.1840000000f, 0.6967930029f),
                new Vector4(0.0234375000f, 0.2633744856f, 0.3840000000f, 0.8396501458f),
                new Vector4(0.5234375000f, 0.5967078189f, 0.5840000000f, 0.9825072886f),
                new Vector4(0.2734375000f, 0.9300411523f, 0.7840000000f, 0.0058309038f),
                new Vector4(0.7734375000f, 0.0781893004f, 0.9840000000f, 0.1486880466f),
                new Vector4(0.1484375000f, 0.4115226337f, 0.0320000000f, 0.2915451895f),
                new Vector4(0.6484375000f, 0.7448559671f, 0.2320000000f, 0.4344023324f),
                new Vector4(0.3984375000f, 0.1893004115f, 0.4320000000f, 0.5772594752f),
                new Vector4(0.8984375000f, 0.5226337449f, 0.6320000000f, 0.7201166181f),
                new Vector4(0.0859375000f, 0.8559670782f, 0.8320000000f, 0.8629737609f),
                new Vector4(0.5859375000f, 0.3004115226f, 0.0720000000f, 0.0262390671f),
                new Vector4(0.3359375000f, 0.6337448560f, 0.2720000000f, 0.1690962099f),
                new Vector4(0.8359375000f, 0.9670781893f, 0.4720000000f, 0.3119533528f),
                new Vector4(0.2109375000f, 0.0164609053f, 0.6720000000f, 0.4548104956f),
                new Vector4(0.7109375000f, 0.3497942387f, 0.8720000000f, 0.5976676385f),
                new Vector4(0.4609375000f, 0.6831275720f, 0.1120000000f, 0.7405247813f),
                new Vector4(0.9609375000f, 0.1275720165f, 0.3120000000f, 0.8833819242f),
                new Vector4(0.0546875000f, 0.4609053498f, 0.5120000000f, 0.0466472303f),
                new Vector4(0.5546875000f, 0.7942386831f, 0.7120000000f, 0.1895043732f),
                new Vector4(0.3046875000f, 0.2386831276f, 0.9120000000f, 0.3323615160f),
                new Vector4(0.8046875000f, 0.5720164609f, 0.1520000000f, 0.4752186589f),
                new Vector4(0.1796875000f, 0.9053497942f, 0.3520000000f, 0.6180758017f),
                new Vector4(0.6796875000f, 0.0534979424f, 0.5520000000f, 0.7609329446f),
                new Vector4(0.4296875000f, 0.3868312757f, 0.7520000000f, 0.9037900875f),
                new Vector4(0.9296875000f, 0.7201646091f, 0.9520000000f, 0.0670553936f),
                new Vector4(0.1171875000f, 0.1646090535f, 0.1920000000f, 0.2099125364f),
                new Vector4(0.6171875000f, 0.4979423868f, 0.3920000000f, 0.3527696793f),
                new Vector4(0.3671875000f, 0.8312757202f, 0.5920000000f, 0.4956268222f),
                new Vector4(0.8671875000f, 0.2757201646f, 0.7920000000f, 0.6384839650f),
                new Vector4(0.2421875000f, 0.6090534979f, 0.9920000000f, 0.7813411079f),
                new Vector4(0.7421875000f, 0.9423868313f, 0.0016000000f, 0.9241982507f),
                new Vector4(0.4921875000f, 0.0905349794f, 0.2016000000f, 0.0874635569f),
                new Vector4(0.9921875000f, 0.4238683128f, 0.4016000000f, 0.2303206997f),
                new Vector4(0.0039062500f, 0.7572016461f, 0.6016000000f, 0.3731778426f),
                new Vector4(0.5039062500f, 0.2016460905f, 0.8016000000f, 0.5160349854f),
                new Vector4(0.2539062500f, 0.5349794239f, 0.0416000000f, 0.6588921283f),
                new Vector4(0.7539062500f, 0.8683127572f, 0.2416000000f, 0.8017492711f),
                new Vector4(0.1289062500f, 0.3127572016f, 0.4416000000f, 0.9446064140f),
                new Vector4(0.6289062500f, 0.6460905350f, 0.6416000000f, 0.1078717201f),
                new Vector4(0.3789062500f, 0.9794238683f, 0.8416000000f, 0.2507288630f),
                new Vector4(0.8789062500f, 0.0288065844f, 0.0816000000f, 0.3935860058f),
                new Vector4(0.0664062500f, 0.3621399177f, 0.2816000000f, 0.5364431487f),
                new Vector4(0.5664062500f, 0.6954732510f, 0.4816000000f, 0.6793002915f),
                new Vector4(0.3164062500f, 0.1399176955f, 0.6816000000f, 0.8221574344f),
                new Vector4(0.8164062500f, 0.4732510288f, 0.8816000000f, 0.9650145773f),
                new Vector4(0.1914062500f, 0.8065843621f, 0.1216000000f, 0.1282798834f),
                new Vector4(0.6914062500f, 0.2510288066f, 0.3216000000f, 0.2711370262f),
                new Vector4(0.4414062500f, 0.5843621399f, 0.5216000000f, 0.4139941691f),
                new Vector4(0.9414062500f, 0.9176954733f, 0.7216000000f, 0.5568513120f),
                new Vector4(0.0351562500f, 0.0658436214f, 0.9216000000f, 0.6997084548f),
                new Vector4(0.5351562500f, 0.3991769547f, 0.1616000000f, 0.8425655977f),
                new Vector4(0.2851562500f, 0.7325102881f, 0.3616000000f, 0.9854227405f),
                new Vector4(0.7851562500f, 0.1769547325f, 0.5616000000f, 0.0087463557f),
                new Vector4(0.1601562500f, 0.5102880658f, 0.7616000000f, 0.1516034985f),
                new Vector4(0.6601562500f, 0.8436213992f, 0.9616000000f, 0.2944606414f),
                new Vector4(0.4101562500f, 0.2880658436f, 0.0096000000f, 0.4373177843f),
                new Vector4(0.9101562500f, 0.6213991770f, 0.2096000000f, 0.5801749271f),
                new Vector4(0.0976562500f, 0.9547325103f, 0.4096000000f, 0.7230320700f),
                new Vector4(0.5976562500f, 0.1028806584f, 0.6096000000f, 0.8658892128f),
                new Vector4(0.3476562500f, 0.4362139918f, 0.8096000000f, 0.0291545190f),
                new Vector4(0.8476562500f, 0.7695473251f, 0.0496000000f, 0.1720116618f),
                new Vector4(0.2226562500f, 0.2139917695f, 0.2496000000f, 0.3148688047f),
                new Vector4(0.7226562500f, 0.5473251029f, 0.4496000000f, 0.4577259475f),
                new Vector4(0.4726562500f, 0.8806584362f, 0.6496000000f, 0.6005830904f),
                new Vector4(0.9726562500f, 0.3251028807f, 0.8496000000f, 0.7434402332f),
                new Vector4(0.0195312500f, 0.6584362140f, 0.0896000000f, 0.8862973761f),
                new Vector4(0.5195312500f, 0.9917695473f, 0.2896000000f, 0.0495626822f),
                new Vector4(0.2695312500f, 0.0082304527f, 0.4896000000f, 0.1924198251f),
                new Vector4(0.7695312500f, 0.3415637860f, 0.6896000000f, 0.3352769679f),
                new Vector4(0.1445312500f, 0.6748971193f, 0.8896000000f, 0.4781341108f),
                new Vector4(0.6445312500f, 0.1193415638f, 0.1296000000f, 0.6209912536f),
                new Vector4(0.3945312500f, 0.4526748971f, 0.3296000000f, 0.7638483965f),
                new Vector4(0.8945312500f, 0.7860082305f, 0.5296000000f, 0.9067055394f),
                new Vector4(0.0820312500f, 0.2304526749f, 0.7296000000f, 0.0699708455f),
                new Vector4(0.5820312500f, 0.5637860082f, 0.9296000000f, 0.2128279883f),
                new Vector4(0.3320312500f, 0.8971193416f, 0.1696000000f, 0.3556851312f),
                new Vector4(0.8320312500f, 0.0452674897f, 0.3696000000f, 0.4985422741f),
                new Vector4(0.2070312500f, 0.3786008230f, 0.5696000000f, 0.6413994169f),
                new Vector4(0.7070312500f, 0.7119341564f, 0.7696000000f, 0.7842565598f),
                new Vector4(0.4570312500f, 0.1563786008f, 0.9696000000f, 0.9271137026f),
                new Vector4(0.9570312500f, 0.4897119342f, 0.0176000000f, 0.0903790087f),
                new Vector4(0.0507812500f, 0.8230452675f, 0.2176000000f, 0.2332361516f),
                new Vector4(0.5507812500f, 0.2674897119f, 0.4176000000f, 0.3760932945f),
                new Vector4(0.3007812500f, 0.6008230453f, 0.6176000000f, 0.5189504373f),
                new Vector4(0.8007812500f, 0.9341563786f, 0.8176000000f, 0.6618075802f),
                new Vector4(0.1757812500f, 0.0823045267f, 0.0576000000f, 0.8046647230f),
                new Vector4(0.6757812500f, 0.4156378601f, 0.2576000000f, 0.9475218659f),
                new Vector4(0.4257812500f, 0.7489711934f, 0.4576000000f, 0.1107871720f),
                new Vector4(0.9257812500f, 0.1934156379f, 0.6576000000f, 0.2536443149f),
                new Vector4(0.1132812500f, 0.5267489712f, 0.8576000000f, 0.3965014577f),
                new Vector4(0.6132812500f, 0.8600823045f, 0.0976000000f, 0.5393586006f),
                new Vector4(0.3632812500f, 0.3045267490f, 0.2976000000f, 0.6822157434f),
                new Vector4(0.8632812500f, 0.6378600823f, 0.4976000000f, 0.8250728863f),
                new Vector4(0.2382812500f, 0.9711934156f, 0.6976000000f, 0.9679300292f),
                new Vector4(0.7382812500f, 0.0205761317f, 0.8976000000f, 0.1311953353f),
                new Vector4(0.4882812500f, 0.3539094650f, 0.1376000000f, 0.2740524781f),
                new Vector4(0.9882812500f, 0.6872427984f, 0.3376000000f, 0.4169096210f),
                new Vector4(0.0117187500f, 0.1316872428f, 0.5376000000f, 0.5597667638f),
                new Vector4(0.5117187500f, 0.4650205761f, 0.7376000000f, 0.7026239067f),
                new Vector4(0.2617187500f, 0.7983539095f, 0.9376000000f, 0.8454810496f),
                new Vector4(0.7617187500f, 0.2427983539f, 0.1776000000f, 0.9883381924f),
                new Vector4(0.1367187500f, 0.5761316872f, 0.3776000000f, 0.0116618076f),
                new Vector4(0.6367187500f, 0.9094650206f, 0.5776000000f, 0.1545189504f),
                new Vector4(0.3867187500f, 0.0576131687f, 0.7776000000f, 0.2973760933f),
                new Vector4(0.8867187500f, 0.3909465021f, 0.9776000000f, 0.4402332362f),
                new Vector4(0.0742187500f, 0.7242798354f, 0.0256000000f, 0.5830903790f),
                new Vector4(0.5742187500f, 0.1687242798f, 0.2256000000f, 0.7259475219f),
                new Vector4(0.3242187500f, 0.5020576132f, 0.4256000000f, 0.8688046647f),
                new Vector4(0.8242187500f, 0.8353909465f, 0.6256000000f, 0.0320699708f),
                new Vector4(0.1992187500f, 0.2798353909f, 0.8256000000f, 0.1749271137f),
                new Vector4(0.6992187500f, 0.6131687243f, 0.0656000000f, 0.3177842566f),
                new Vector4(0.4492187500f, 0.9465020576f, 0.2656000000f, 0.4606413994f),
                new Vector4(0.9492187500f, 0.0946502058f, 0.4656000000f, 0.6034985423f),
                new Vector4(0.0429687500f, 0.4279835391f, 0.6656000000f, 0.7463556851f),
                new Vector4(0.5429687500f, 0.7613168724f, 0.8656000000f, 0.8892128280f),
                new Vector4(0.2929687500f, 0.2057613169f, 0.1056000000f, 0.0524781341f),
                new Vector4(0.7929687500f, 0.5390946502f, 0.3056000000f, 0.1953352770f),
                new Vector4(0.1679687500f, 0.8724279835f, 0.5056000000f, 0.3381924198f),
                new Vector4(0.6679687500f, 0.3168724280f, 0.7056000000f, 0.4810495627f),
                new Vector4(0.4179687500f, 0.6502057613f, 0.9056000000f, 0.6239067055f),
                new Vector4(0.9179687500f, 0.9835390947f, 0.1456000000f, 0.7667638484f),
                new Vector4(0.1054687500f, 0.0329218107f, 0.3456000000f, 0.9096209913f),
                new Vector4(0.6054687500f, 0.3662551440f, 0.5456000000f, 0.0728862974f),
                new Vector4(0.3554687500f, 0.6995884774f, 0.7456000000f, 0.2157434402f),
                new Vector4(0.8554687500f, 0.1440329218f, 0.9456000000f, 0.3586005831f),
                new Vector4(0.2304687500f, 0.4773662551f, 0.1856000000f, 0.5014577259f),
                new Vector4(0.7304687500f, 0.8106995885f, 0.3856000000f, 0.6443148688f),
                new Vector4(0.4804687500f, 0.2551440329f, 0.5856000000f, 0.7871720117f),
                new Vector4(0.9804687500f, 0.5884773663f, 0.7856000000f, 0.9300291545f),
                new Vector4(0.0273437500f, 0.9218106996f, 0.9856000000f, 0.0932944606f),
                new Vector4(0.5273437500f, 0.0699588477f, 0.0336000000f, 0.2361516035f),
                new Vector4(0.2773437500f, 0.4032921811f, 0.2336000000f, 0.3790087464f),
                new Vector4(0.7773437500f, 0.7366255144f, 0.4336000000f, 0.5218658892f),
                new Vector4(0.1523437500f, 0.1810699588f, 0.6336000000f, 0.6647230321f),
                new Vector4(0.6523437500f, 0.5144032922f, 0.8336000000f, 0.8075801749f),
                new Vector4(0.4023437500f, 0.8477366255f, 0.0736000000f, 0.9504373178f),
                new Vector4(0.9023437500f, 0.2921810700f, 0.2736000000f, 0.1137026239f),
                new Vector4(0.0898437500f, 0.6255144033f, 0.4736000000f, 0.2565597668f),
                new Vector4(0.5898437500f, 0.9588477366f, 0.6736000000f, 0.3994169096f),
                new Vector4(0.3398437500f, 0.1069958848f, 0.8736000000f, 0.5422740525f),
                new Vector4(0.8398437500f, 0.4403292181f, 0.1136000000f, 0.6851311953f),
                new Vector4(0.2148437500f, 0.7736625514f, 0.3136000000f, 0.8279883382f),
                new Vector4(0.7148437500f, 0.2181069959f, 0.5136000000f, 0.9708454810f),
                new Vector4(0.4648437500f, 0.5514403292f, 0.7136000000f, 0.1341107872f),
                new Vector4(0.9648437500f, 0.8847736626f, 0.9136000000f, 0.2769679300f),
                new Vector4(0.0585937500f, 0.3292181070f, 0.1536000000f, 0.4198250729f),
                new Vector4(0.5585937500f, 0.6625514403f, 0.3536000000f, 0.5626822157f),
                new Vector4(0.3085937500f, 0.9958847737f, 0.5536000000f, 0.7055393586f),
                new Vector4(0.8085937500f, 0.0013717421f, 0.7536000000f, 0.8483965015f),
                new Vector4(0.1835937500f, 0.3347050754f, 0.9536000000f, 0.9912536443f),
                new Vector4(0.6835937500f, 0.6680384088f, 0.1936000000f, 0.0145772595f),
                new Vector4(0.4335937500f, 0.1124828532f, 0.3936000000f, 0.1574344023f),
                new Vector4(0.9335937500f, 0.4458161866f, 0.5936000000f, 0.3002915452f),
                new Vector4(0.1210937500f, 0.7791495199f, 0.7936000000f, 0.4431486880f),
                new Vector4(0.6210937500f, 0.2235939643f, 0.9936000000f, 0.5860058309f),
                new Vector4(0.3710937500f, 0.5569272977f, 0.0032000000f, 0.7288629738f),
                new Vector4(0.8710937500f, 0.8902606310f, 0.2032000000f, 0.8717201166f),
                new Vector4(0.2460937500f, 0.0384087791f, 0.4032000000f, 0.0349854227f),
                new Vector4(0.7460937500f, 0.3717421125f, 0.6032000000f, 0.1778425656f),
                new Vector4(0.4960937500f, 0.7050754458f, 0.8032000000f, 0.3206997085f),
                new Vector4(0.9960937500f, 0.1495198903f, 0.0432000000f, 0.4635568513f),
                new Vector4(0.0019531250f, 0.4828532236f, 0.2432000000f, 0.6064139942f),
        };

        public static Vector4 GetHaltonSequence(UInt64 idx) {
            return HALTON[idx % (UInt64)HALTON.Length];
        }

        public static float SRGBlinear(float value) { 
            if (value <= 0.04045) {
                return value / 12.92f;
            } else {
                return MathF.Pow((value / 1.055f) + 0.0521327f, 2.4f);
            }
        }

        public static Vector3 SRGBlinear(Vector3 sRGB ) {
            Vector3 outLinear;
            outLinear.X = SRGBlinear(sRGB.X);
            outLinear.Y = SRGBlinear(sRGB.Y);
            outLinear.Z = SRGBlinear(sRGB.Z);
            return outLinear;
        }
    }
}