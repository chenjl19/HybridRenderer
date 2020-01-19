using System;
using System.IO;
using System.Collections.Generic;

namespace NT
{
    public enum ImageType : uint {
        Default,
        Cubemap,
        Normal
    }

    public struct BinaryImage {
        public ushort level;
        public ushort destZ;
        public ushort width;
        public ushort height;
        public UInt32 dataSizeInBytes;
        public byte[] bytes;

        public void ReadHeader(BinaryReader reader) {
            level = reader.ReadUInt16();
            destZ = reader.ReadUInt16();
            width = reader.ReadUInt16();
            height = reader.ReadUInt16();
            dataSizeInBytes = reader.ReadUInt32();
        }

        public void WriteHeader(BinaryWriter writer) {
            writer.Write(level);
            writer.Write(destZ);
            writer.Write(width);
            writer.Write(height);
            writer.Write(dataSizeInBytes);
        }
    }

    public struct BinaryImageFile {
        public const uint Version = 10;
        public const uint Magic = (('B' << 0) | ('I' << 8) | ('M' << 16) | (Version << 24));

        public System.Int64 sourceFileTime;
        public uint headerMagic;
        public uint textureType;
        public uint format;
        public uint width;
        public uint height;
        public uint numLevels;
        // one or more BinaryImage structures follow

        public void ReadHeader(BinaryReader reader) {
            sourceFileTime = reader.ReadInt64();
            headerMagic = reader.ReadUInt32();
            textureType = reader.ReadUInt32();
            format = reader.ReadUInt32();
            width = reader.ReadUInt32();
            height = reader.ReadUInt32();
            numLevels = reader.ReadUInt32();
        }

        public void WriteHeader(BinaryWriter writer) {
            writer.Write(sourceFileTime);
            writer.Write(headerMagic);
            writer.Write(textureType);
            writer.Write(format);
            writer.Write(width);
            writer.Write(height);
            writer.Write(numLevels);
        }

        public BinaryImage[] images;
    }
}