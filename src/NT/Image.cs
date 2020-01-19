using System;
using System.IO;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Veldrid.ImageSharp;

namespace NT
{
    public enum CPUAccessFlags {
        None,
        Read = 1 << 0,
        Write = 1 << 1
    }

    [StructLayout(LayoutKind.Sequential, Size = 4)]
    public struct MipMapCount : IEquatable<MipMapCount>
    {
        public MipMapCount(bool allMipMaps)
        {
            this.Count = allMipMaps ? 0 : 1;
        }

        public MipMapCount(int count)
        {
            if (count < 0)
                throw new ArgumentException("mipCount must be >= 0");
            this.Count = count;
        }

        public readonly int Count;

        public bool Equals(MipMapCount other)
        {
            return this.Count == other.Count;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is MipMapCount && Equals((MipMapCount)obj);
        }

        public override int GetHashCode()
        {
            return this.Count;
        }

        public static bool operator ==(MipMapCount left, MipMapCount right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(MipMapCount left, MipMapCount right)
        {
            return !left.Equals(right);
        }

        public static implicit operator bool(MipMapCount mipMap)
        {
            return mipMap.Count == 0;
        }

        public static implicit operator MipMapCount(bool mipMapAll)
        {
            return new MipMapCount(mipMapAll);
        }

        public static implicit operator int(MipMapCount mipMap)
        {
            return mipMap.Count;
        }

        public static implicit operator MipMapCount(int mipMapCount)
        {
            return new MipMapCount(mipMapCount);
        }
    }

    public abstract class Image {
        public string name {get; protected set;}
        public Guid guid {get; protected set;}
        public Veldrid.Texture textureObject {get; protected set;}
        public Veldrid.TextureView textureView {get; protected set;}

        public Veldrid.BindableResource GetDeviceResource() {
            if(textureView != null) {
                return textureView;
            } else {
                return textureObject;
            }
        }

        private static int CountMips(int width, int n) {
            int mipLevels = 1;
            while (width > n)
            {
                ++mipLevels;
                if (width > n)
                    width >>= 1;
            }
            return mipLevels;
        }

        private static int CountMips(int width, int height, int n) {
            int mipLevels = 1;
            while (height > 1 || width > 1)
            {
                ++mipLevels;
                if (height > n)
                    height >>= 1;
                if (width > n)
                    width >>= 1;
            }
            return mipLevels;
        }

        private static int CountMipsWithDepth(int width, int height, int depth, int n) {
            int mipLevels = 1;
            while (height > n || width > n || depth > n)
            {
                ++mipLevels;
                if (height > n)
                    height >>= 1;
                if (width > n)
                    width >>= 1;
                if (depth > n)
                    depth >>= 1;
            }
            return mipLevels;
        }

        public static int CalculateMipLevels(int width, MipMapCount mipLevels, int minLevel) {
            if (mipLevels > 1) {
                int maxMips = CountMips(width, minLevel);
                if (mipLevels > maxMips) {
                    throw new InvalidOperationException(String.Format("MipLevels must be <= {0}", maxMips));
                }
            } else if (mipLevels == 0) {
                mipLevels = CountMips(width, minLevel);
            } else {
                mipLevels = 1;
            }
            return mipLevels;
        }

        public static int CalculateMipLevels(int width, int height, MipMapCount mipLevels, int minLevel){
            if (mipLevels > 1) {
                int maxMips = CountMips(width, height, minLevel);
                if (mipLevels > maxMips) {
                    throw new InvalidOperationException(String.Format("MipLevels must be <= {0}", maxMips));
                }
            } else if (mipLevels == 0) {
                mipLevels = CountMips(width, height, minLevel);
            } else {
                mipLevels = 1;
            }
            return mipLevels;
        }

        public static int CalculateMipLevels(int width, int height, int depth, MipMapCount mipLevels, int minLevel){
            if (mipLevels > 1) {
                if (!MathHelper.PowerOfTwo(width) || !MathHelper.PowerOfTwo(height) || !MathHelper.PowerOfTwo(depth)) {
                    throw new InvalidOperationException("Width/Height/Depth must be power of 2");
                }
                int maxMips = CountMipsWithDepth(width, height, depth, minLevel);
                if (mipLevels > maxMips) {
                    throw new InvalidOperationException(String.Format("MipLevels must be <= {0}", maxMips));
                }
            } else if (mipLevels == 0) {
                if (!MathHelper.PowerOfTwo(width) || !MathHelper.PowerOfTwo(height) || !MathHelper.PowerOfTwo(depth)) {
                    throw new InvalidOperationException("Width/Height/Depth must be power of 2");
                }
                mipLevels = CountMipsWithDepth(width, height, depth, minLevel);
            } else {
                mipLevels = 1;
            }
            return mipLevels;
        }

        public static int CalculateMipSize(int width, int mipLevel, int minLevel) {
            mipLevel = Math.Min(mipLevel, CountMips(width, minLevel));
            width = width >> mipLevel;
            return width > 0 ? width : 1;
        }
    }

    public class ImageWraper : Image {
        public ImageWraper() {
        }

        public ImageWraper(Veldrid.Texture texture) {
            if(texture != null && texture.Usage.HasFlag(Veldrid.TextureUsage.Sampled)) {
                textureObject = texture;
            }
        } 

        public void SetDeviceTexture(Veldrid.Texture texture) {
            textureObject = texture;
        }
    }

    public class Image2D : Image {
        public int width {get; private set;}
        public int height {get; private set;}
        public int numMips {get; private set;}
        public Veldrid.PixelFormat pixelFormat {get; protected set;}    

        public static readonly Image2D white;
        public static readonly Image2D black;
        public static readonly Image2D nullBump;

        static Image2D() {
            white = new Image2D("_whiteImage", 16, 16, Veldrid.PixelFormat.R8_G8_B8_A8_UNorm, 1);
            black = new Image2D("_blackImage", 16, 16, Veldrid.PixelFormat.R8_G8_B8_A8_UNorm, 1);
            nullBump = new Image2D("_nullBumpImage", 16, 16, Veldrid.PixelFormat.R8_G8_B8_A8_UNorm, 1);
            IntPtr data = SharpDX.Utilities.AllocateMemory(16 * 16 * 4);
            SharpDX.Utilities.ClearMemory(data, 255, 16 * 16 * 4);
            white.SubImageUpload(0, 16, 16, data, 16 * 16 * 4);
            unsafe {
                byte *pic = (byte*)data;
                for(int i = 0; i < 16 * 16; i++) {
                    *pic++ = 125;
                    *pic++ = 125;
                    *pic++ = 255;
                    *pic++ = 255;
                }
                nullBump.SubImageUpload(0, 16, 16, data, 16 * 16 * 4);
                pic = (byte*)data;
                for(int i = 0; i < 16 * 16; i++) {
                    *pic++ = 0;
                    *pic++ = 0;
                    *pic++ = 0;
                    *pic++ = 255;
                }   
                black.SubImageUpload(0, 16, 16, data, 16 * 16 * 4);         
            }            
        }

        public Image2D(int _width, int _height, Veldrid.PixelFormat _pixelFormat, int _numMips = 0) {
            name = $"image2D_{System.Guid.NewGuid().ToString()}";
            width = MathHelper.MakePowerOfTwo(_width);
            height = MathHelper.MakePowerOfTwo(_height);
            pixelFormat = _pixelFormat;
            numMips = CalculateMipLevels(width, height, _numMips, 1);
        } 

        public Image2D(string inName, int _width, int _height, Veldrid.PixelFormat _pixelFormat, int _numMips = 0) {
            name = inName;
            width = MathHelper.MakePowerOfTwo(_width);
            height = MathHelper.MakePowerOfTwo(_height);
            pixelFormat = _pixelFormat;
            numMips = CalculateMipLevels(width, height, _numMips, 1);
        } 

        public Image2D(Veldrid.Texture texture) {
            width = (int)texture.Width;
            height = (int)texture.Height;
            pixelFormat = texture.Format;
            numMips = (int)texture.MipLevels;
            textureObject = texture;
        }

        public void SubImageUpload(int mipLevel, uint w, uint h, IntPtr pic, uint picSizeInBytes) {
            SubImageUpload((uint)mipLevel, 0, 0, w, h, pic, picSizeInBytes);
        }

        public void SubImageUpload(int mipLevel, uint w, uint h, byte[] bytes) {
            SubImageUpload((uint)mipLevel, 0, 0, w, h, bytes);
        }

        public void SetDeviceTexture(Veldrid.Texture texture) {
            textureObject = texture;
        }

        public void SubImageUpload(uint mipLevel, uint x, uint y, uint w, uint h, byte[] bytes) {
            if(textureObject == null) {
                textureObject = GraphicsDevice.ResourceFactory.CreateTexture(Veldrid.TextureDescription.Texture2D((uint)width, (uint)height, (uint)numMips, 1, pixelFormat, Veldrid.TextureUsage.Sampled));
                textureObject.Name = name;
            }
            GraphicsDevice.gd.UpdateTexture(textureObject, bytes, x, y, 0, w, h, 1, mipLevel, 0);
        }

        public void SubImageUpload(uint mipLevel, uint x, uint y, uint w, uint h, IntPtr pic, uint picSizeInBytes) {
            if(textureObject == null) {
                textureObject = GraphicsDevice.ResourceFactory.CreateTexture(Veldrid.TextureDescription.Texture2D((uint)width, (uint)height, (uint)numMips, 1, pixelFormat, Veldrid.TextureUsage.Sampled));
                textureObject.Name = name;
            }
            GraphicsDevice.gd.UpdateTexture(textureObject, pic, picSizeInBytes, x, y, 0, w, h, 1, mipLevel, 0);
        }
    }

    public class Image3D : Image {
        public int width {get; private set;}
        public int height {get; private set;}
        public int depth {get; private set;}
        public Veldrid.PixelFormat pixelFormat {get; protected set;}    

        public Image3D(int w, int h, int d, Veldrid.PixelFormat format) {
            name = $"image3D_{System.Guid.NewGuid().ToString()}";
            width = MathHelper.MakePowerOfTwo(w);
            height = MathHelper.MakePowerOfTwo(h);
            depth = d;
            pixelFormat = format;
        }

        public void SubImageUpload(byte[] bytes) {
            if(textureObject == null) {
                textureObject = GraphicsDevice.ResourceFactory.CreateTexture(
                    Veldrid.TextureDescription.Texture3D(
                        (uint)width, 
                        (uint)height, 
                        (uint)depth, 
                        1, 
                        pixelFormat, 
                        Veldrid.TextureUsage.Sampled)
                );
                textureObject.Name = name;
            }

            GraphicsDevice.gd.UpdateTexture(textureObject, bytes, 0, 0, 0, (uint)width, (uint)height, (uint)depth, 0, 0);
        }
    }

    public class ImageCube : Image {
        public int width {get; private set;}
        public int height {get; private set;}
        public int numMips {get; private set;}
        public Veldrid.PixelFormat pixelFormat {get; protected set;}  

        public ImageCube(int _width, int _height, Veldrid.PixelFormat _pixelFormat, int _numMips = 0) {
            name = $"ImageCube_{System.Guid.NewGuid().ToString()}";
            width = MathHelper.MakePowerOfTwo(_width);
            height = MathHelper.MakePowerOfTwo(_height);
            pixelFormat = _pixelFormat;
            numMips = CalculateMipLevels(width, height, _numMips, 1);
        } 

        public ImageCube(string inName, int _width, int _height, Veldrid.PixelFormat _pixelFormat, int _numMips = 0) {
            name = inName;
            width = MathHelper.MakePowerOfTwo(_width);
            height = MathHelper.MakePowerOfTwo(_height);
            pixelFormat = _pixelFormat;
            numMips = CalculateMipLevels(width, height, _numMips, 1);
        } 

        public ImageCube(Veldrid.Texture texture) {
            width = (int)texture.Width;
            height = (int)texture.Height;
            pixelFormat = texture.Format;
            numMips = (int)texture.MipLevels;
            textureObject = texture;
        }  

        public void SubImageUpload(int faceID, int mipLevel, byte[] bytes) {
            SubImageUpload(faceID, mipLevel, 0, 0, (uint)width, (uint)height, bytes);
        }

        public void SubImageUpload(int faceID, int mipLevel, uint w, uint h, byte[] bytes) {
            SubImageUpload(faceID, mipLevel, 0, 0, w, h, bytes);
        }

        public void SubImageUpload(int faceID, int mipLevel, uint x, uint y, uint w, uint h, byte[] bytes) {
            if(faceID >= 0 && faceID < 6) {
                if(textureObject == null) {
                    textureObject = GraphicsDevice.ResourceFactory.CreateTexture(
                        Veldrid.TextureDescription.Texture2D(
                            (uint)width,
                            (uint)height, 
                            (uint)numMips, 
                            1, 
                            pixelFormat, 
                            Veldrid.TextureUsage.Sampled | Veldrid.TextureUsage.Cubemap));
                    textureObject.Name = name;
                }
                GraphicsDevice.gd.UpdateTexture(textureObject, bytes, x, y, 0, w, h, 1, (uint)mipLevel, (uint)faceID);
            }
        }        
    }

    public class ImageCubeArray : Image {
        public int width {get; private set;}
        public int height {get; private set;}
        public int numMips {get; private set;}
        public int numCubemaps {get; private set;}
        public Veldrid.PixelFormat pixelFormat {get; protected set;}  

        public ImageCubeArray(int _width, int _height, int _numCubemaps, Veldrid.PixelFormat _pixelFormat, int _numMips = 0) {
            name = $"ImageCubeArray_{System.Guid.NewGuid().ToString()}";
            width = MathHelper.MakePowerOfTwo(_width);
            height = MathHelper.MakePowerOfTwo(_height);
            pixelFormat = _pixelFormat;
            numCubemaps = _numCubemaps;
            numMips = CalculateMipLevels(width, height, _numMips, 1);
        } 

        public ImageCubeArray(Veldrid.Texture texture) {
            width = (int)texture.Width;
            height = (int)texture.Height;
            pixelFormat = texture.Format;
            numMips = (int)texture.MipLevels;
            numCubemaps = (int)texture.ArrayLayers / 6;
            textureObject = texture;
        }  

        public void SubImageUpload(int arrayID, int faceID, int mipLevel, uint w, uint h, byte[] bytes) {
            SubImageUpload(arrayID, faceID, mipLevel, 0, 0, w, h, bytes);
        }

        public void SubImageUpload(int arrayID, int faceID, int mipLevel, uint x, uint y, uint w, uint h, byte[] bytes) {
            if(faceID >= 0 && faceID < 6) {
                if(textureObject == null) {
                    textureObject = GraphicsDevice.ResourceFactory.CreateTexture(
                        Veldrid.TextureDescription.Texture2D(
                            (uint)width,
                            (uint)height, 
                            (uint)numMips, 
                            (uint)numCubemaps * 1, 
                            pixelFormat, 
                            Veldrid.TextureUsage.Sampled | Veldrid.TextureUsage.Cubemap));
                    textureObject.Name = name;
                }
                GraphicsDevice.gd.UpdateTexture(textureObject, bytes, x, y, 0, w, h, 1, (uint)mipLevel, (uint)arrayID * 6 + (uint)faceID);
            }
        }        
    }

    public class Attachment : Image {
        public int width {get; private set;}
        public int height {get; private set;}
        public Veldrid.PixelFormat pixelFormat {get; protected set;}   
    }

    public class ImageBuffer : Image {
        public int sizeInBytes {get; private set;}
        public Veldrid.PixelFormat format {get; private set;}
        public Veldrid.DeviceBuffer bufferObject {get; private set;}
        public int stride {get; private set;}
    }

    public static class ImageManager {
        static Dictionary<string, Image> images;

        public static Image2D valueNoise_256x256 {get; private set;}
        public static Image2D blueNoise_512x512 {get; private set;}
        public static Image3D noiseVolume_128x128x128 {get; private set;}

        static ImageManager() {
            images = new Dictionary<string, Image>();
            blueNoise_512x512 = Image2DFromFile(FileSystem.CreateAssetOSPath("textures/luts/blueNoise_512x512.bmp"), true);
            valueNoise_256x256 = Image2DFromFile(FileSystem.CreateAssetOSPath("textures/luts/valueNoise_256x256.bmp"), true);
            /*
            string noiseVolumeFilename = FileSystem.CreateAssetOSPath("textures/luts/NoiseVolume.bytes");
            if(File.Exists(noiseVolumeFilename)) {
                var bytes = File.ReadAllBytes(noiseVolumeFilename);
                uint height = BitConverter.ToUInt32(bytes, 12);
                uint width = BitConverter.ToUInt32(bytes, 16);
                uint pitch = BitConverter.ToUInt32(bytes, 20);
                uint depth = BitConverter.ToUInt32(bytes, 24);
                uint formatFlags = BitConverter.ToUInt32(bytes, 20 * 4);
                uint bitDepth = BitConverter.ToUInt32(bytes, 22 * 4);
                if(bitDepth == 0) {
                    bitDepth = pitch / width * 8;
                }
                byte[] colors = new byte[width * height * depth];
                uint index = 128;
                if(bytes[21 * 4] == 'D' && bytes[21 * 4 + 1] == 'X' && bytes[21 * 4 + 2] == '1' && bytes[21 * 4 + 3] == '0' && (formatFlags & 0x4) != 0) {
                    uint format = BitConverter.ToUInt32(bytes, (int)index);
                    if(format >= 60 && format <= 65) {
                        bitDepth = 8;
                    } else if(format >= 48 && format <= 52) {
                        bitDepth = 16;
                    } else if(format >= 27 && format <= 32) {
                        bitDepth = 32;
                    }
                    index += 20;
                }   
                uint byteDepth = bitDepth / 8;
                pitch = (width * bitDepth + 7) / 8;
                for(int d = 0; d < depth; d++) {
                    //index = 128;
                    for(int h = 0; h < height; ++h) {
                        for(int w = 0; w < width; ++w) {
                            byte v = bytes[index + w * byteDepth];
                            colors[w + h * width + d * width * height] = v;
                        }
                        index += pitch;
                    }                    
                }

                volumetricLightingNoiseVolume = new Image3D((int)width, (int)height, (int)depth, Veldrid.PixelFormat.R8_UNorm);   
                volumetricLightingNoiseVolume.SubImageUpload(colors);          
            }
            */
        }

        public static void Init() {
        }

        public static void Shutdown() {
        }

        static Veldrid.PixelFormat GetLinearFormat(Veldrid.PixelFormat format) {
            switch(format) {
                case Veldrid.PixelFormat.B8_G8_R8_A8_UNorm_SRgb:
                    return Veldrid.PixelFormat.B8_G8_R8_A8_UNorm;
                case Veldrid.PixelFormat.R8_G8_B8_A8_UNorm_SRgb:
                    return Veldrid.PixelFormat.R8_G8_B8_A8_UNorm;
                default:
                    return format;
            }
        }    

        public static Image2D Image2DFromFile(string filename, bool linear = false) {
            if(!images.TryGetValue(filename, out Image image)) {
                Image2D image2D;
                if(File.Exists(filename)) {
                    if(Path.GetExtension(filename) != ".bimg") {
                        var imageFile = new Veldrid.ImageSharp.ImageSharpTexture(filename, true, true);
                        image2D = new Image2D(filename, (int)imageFile.Width, (int)imageFile.Height, linear ? GetLinearFormat(imageFile.Format) : imageFile.Format, (int)imageFile.MipLevels);
                        image2D.SetDeviceTexture(imageFile.CreateDeviceTexture(GraphicsDevice.gd, GraphicsDevice.ResourceFactory));
                        images.Add(filename, image2D);    
                        return image2D;     
                    } else {
                        using(System.IO.BinaryReader reader = new System.IO.BinaryReader(System.IO.File.OpenRead(filename))) {
                            BinaryImageFile file = new BinaryImageFile();
                            file.ReadHeader(reader);
                            image2D = new Image2D(filename, (int)file.width, (int)file.height, (Veldrid.PixelFormat)file.format, (int)file.numLevels);
                            BinaryImage[] imageDatas = new BinaryImage[file.numLevels];
                            for(int i = 0; i < imageDatas.Length; i++) {
                                imageDatas[i].ReadHeader(reader);
                                var bytes = reader.ReadBytes((int)imageDatas[i].dataSizeInBytes);
                                image2D.SubImageUpload(i, imageDatas[i].width, imageDatas[i].height, bytes);
                            }
                            images.Add(filename, image2D);
                        }
                        return image2D;
                    }
                }
            }
            if(image == null) {
                Console.WriteLine($"Could't load file '{filename}'");
                image = Image2D.black;
            }
            return image as Image2D;
        }

        public static Image GetImage(string name) {return null;}

        public static void FreeImage(Image image) {}

        public static void UpdateImage(string name, IntPtr data, int w, int h) {}

        public static void UpdateImage(Image2D image, int mipLevel, int x, int y, int w, int h, IntPtr pic, int pixelPitch) {
            if(!images.TryGetValue(image.name, out Image allocedImage)) {
                images.Add(image.name, image);
            }
        }
    }
}