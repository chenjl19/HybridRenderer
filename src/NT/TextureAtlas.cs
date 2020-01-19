    
using System;
using System.Collections.Generic;
using SharpDX;

namespace NT
{
    /// <summary>
    /// Implementation of a "Guillotine" packer.
    /// More information at http://clb.demon.fi/files/RectangleBinPack.pdf.
    /// </summary>
    public class GuillotinePacker
    {
        readonly List<Rectangle> freeRectangles = new List<Rectangle>();
        readonly List<Rectangle> tempFreeRectangles = new List<Rectangle>();

        public delegate void InsertRectangleCallback(int cascadeIndex, ref Rectangle rectangle);

        public int Width { get; private set; }
        public int Height { get; private set; }

        public void Clear(int _width, int _height) {
            freeRectangles.Clear();
            freeRectangles.Add(new Rectangle { X = 0, Y = 0, Width = _width, Height = _height });
            Width = _width;
            Height = _height;
        }

        public virtual void Clear() {
            Clear(Width, Height);
        }

        public void Free(ref Rectangle oldRectangle) {
            freeRectangles.Add(oldRectangle);
        }

        public bool Insert(int width, int height, ref Rectangle bestRectangle) {
            return Insert(width, height, freeRectangles, ref bestRectangle);
        }

        public bool TryInsert(int width, int height, int count, InsertRectangleCallback inserted) {
            var bestRectangle = new Rectangle();
            tempFreeRectangles.Clear();
            foreach (var freeRectangle in freeRectangles) {
                tempFreeRectangles.Add(freeRectangle);
            }

            for (var i = 0; i < count; ++i) {
                if (!Insert(width, height, tempFreeRectangles, ref bestRectangle)) {
                    tempFreeRectangles.Clear();
                    return false;
                }

                inserted(i, ref bestRectangle);
            }

            // if the insertion went well, use the new configuration
            freeRectangles.Clear();
            foreach (var tempFreeRectangle in tempFreeRectangles) {
                freeRectangles.Add(tempFreeRectangle);
            }
            tempFreeRectangles.Clear();

            return true;
        }

        static bool Insert(int width, int height, List<Rectangle> freeRectanglesList, ref Rectangle bestRectangle) {
            // Info on algorithm: http://clb.demon.fi/files/RectangleBinPack.pdf
            int bestScore = int.MaxValue;
            int freeRectangleIndex = -1;

            // Find space for new rectangle
            for (int i = 0; i < freeRectanglesList.Count; ++i) {
                var currentFreeRectangle = freeRectanglesList[i];
                if (width == currentFreeRectangle.Width && height == currentFreeRectangle.Height) {
                    // Perfect fit
                    bestRectangle.X = currentFreeRectangle.X;
                    bestRectangle.Y = currentFreeRectangle.Y;
                    bestRectangle.Width = width;
                    bestRectangle.Height = height;
                    freeRectangleIndex = i;
                    break;
                }
                if (width <= currentFreeRectangle.Width && height <= currentFreeRectangle.Height) {
                    // Can fit inside
                    // Use "BAF" heuristic (best area fit)
                    var score = currentFreeRectangle.Width * currentFreeRectangle.Height - width * height;
                    if (score < bestScore) {
                        bestRectangle.X = currentFreeRectangle.X;
                        bestRectangle.Y = currentFreeRectangle.Y;
                        bestRectangle.Width = width;
                        bestRectangle.Height = height;
                        bestScore = score;
                        freeRectangleIndex = i;
                    }
                }
            }

            // No space could be found
            if (freeRectangleIndex == -1)
                return false;

            var freeRectangle = freeRectanglesList[freeRectangleIndex];

            // Choose an axis to split (trying to minimize the smaller area "MINAS")
            int w = freeRectangle.Width - bestRectangle.Width;
            int h = freeRectangle.Height - bestRectangle.Height;
            var splitHorizontal = (bestRectangle.Width * h > w * bestRectangle.Height);

            // Form the two new rectangles.
            var bottom = new Rectangle { X = freeRectangle.X, Y = freeRectangle.Y + bestRectangle.Height, Width = splitHorizontal ? freeRectangle.Width : bestRectangle.Width, Height = h };
            var right = new Rectangle { X = freeRectangle.X + bestRectangle.Width, Y = freeRectangle.Y, Width = w, Height = splitHorizontal ? bestRectangle.Height : freeRectangle.Height };

            if (bottom.Width > 0 && bottom.Height > 0)
                freeRectanglesList.Add(bottom);
            if (right.Width > 0 && right.Height > 0)
                freeRectanglesList.Add(right);

            // Remove previously selected freeRectangle
            if (freeRectangleIndex != freeRectanglesList.Count - 1)
                freeRectanglesList[freeRectangleIndex] = freeRectanglesList[freeRectanglesList.Count - 1];
            freeRectanglesList.RemoveAt(freeRectanglesList.Count - 1);

            return true;
        }
    }

    public class AtlasAllocator {
        private class AtlasNode {
            public AtlasNode m_RightChild = null;
            public AtlasNode m_BottomChild = null;
            public Vector4 m_Rect = new Vector4(0, 0, 0, 0); // x,y is width and height (scale) z,w offset into atlas (bias)

            public AtlasNode Allocate(int width, int height) {
                // not a leaf node, try children
                if (m_RightChild != null) {
                    AtlasNode node = m_RightChild.Allocate(width, height);
                    if (node == null) {
                        node = m_BottomChild.Allocate(width, height);
                    }
                    return node;
                }

                //leaf node, check for fit
                if ((width <= m_Rect.X) && (height <= m_Rect.Y)) {
                    // perform the split
                    m_RightChild = new AtlasNode();
                    m_BottomChild = new AtlasNode();

                    if (width > height) { // logic to decide which way to split                                                                               //  +--------+------+
                        m_RightChild.m_Rect.Z = m_Rect.Z + width;               //  |        |      |
                        m_RightChild.m_Rect.W = m_Rect.W;                       //  +--------+------+
                        m_RightChild.m_Rect.X = m_Rect.X - width;               //  |               |
                        m_RightChild.m_Rect.Y = height;                         //  |               |
                                                                                //  +---------------+
                        m_BottomChild.m_Rect.Z = m_Rect.Z;
                        m_BottomChild.m_Rect.W = m_Rect.W + height;
                        m_BottomChild.m_Rect.X = m_Rect.X;
                        m_BottomChild.m_Rect.Y = m_Rect.Y - height;
                    } else {                                                    //  +---+-----------+
                        m_RightChild.m_Rect.Z = m_Rect.Z + width;               //  |   |           |
                        m_RightChild.m_Rect.W = m_Rect.W;                       //  |   |           |
                        m_RightChild.m_Rect.X = m_Rect.X - width;               //  +---+           +
                        m_RightChild.m_Rect.Y = m_Rect.Y;                       //  |   |           |
                                                                                //  +---+-----------+
                        m_BottomChild.m_Rect.Z = m_Rect.Z;
                        m_BottomChild.m_Rect.W = m_Rect.W + height;
                        m_BottomChild.m_Rect.X = width;
                        m_BottomChild.m_Rect.Y = m_Rect.Y - height;
                    }
                    m_Rect.X = width;
                    m_Rect.Y = height;
                    return this;
                }
                return null;
            }

            public void Release() {
                if (m_RightChild != null) {
                    m_RightChild.Release();
                    m_BottomChild.Release();
                }
                m_RightChild = null;
                m_BottomChild = null;
            }
        }

        private AtlasNode m_Root;
        private int m_Width;
        private int m_Height;

        public AtlasAllocator(int width, int height) {
            m_Root = new AtlasNode();
            m_Root.m_Rect = new Vector4(width, height, 0, 0);
            m_Width = width;
            m_Height = height;
        }

        public bool Allocate(ref Vector4 result, int width, int height) {
            AtlasNode node = m_Root.Allocate(width, height);
            if (node != null) {
                result = node.m_Rect;
                return true;
            } else {
                result = Vector4.Zero;
                return false;
            }
        }

        public void Release() {
            m_Root.Release();
            m_Root = new AtlasNode();
            m_Root.m_Rect = new Vector4(m_Width, m_Height, 0, 0);
        }
    }

    public struct ImageSubData {
        public int level;
        public int width;
        public int height;
        public byte[] bytes;
    }

    public class Texture2DAtlas {
        private Image2D m_AtlasTexture = null;
        private int m_Width;
        private int m_Height;
        private Veldrid.PixelFormat m_Format;
        private AtlasAllocator m_AtlasAllocator = null;
        public Vector2 texelSize {get; private set;}
        private Dictionary<string, Vector4> m_AllocationCache = new Dictionary<string, Vector4>();

        public Image2D AtlasTexture {
            get {
                return m_AtlasTexture;
            }
        }

        public Texture2DAtlas(Image2D atlas) {
            m_AtlasTexture = atlas;
            m_Width = atlas.width;
            m_Height = atlas.height;
            m_Format = atlas.pixelFormat;
            m_AtlasAllocator = new AtlasAllocator(m_Width, m_Height);
            texelSize = new Vector2(1f / m_Width, 1f / m_Height);
        }

        public Texture2DAtlas(int width, int height, Veldrid.PixelFormat format, bool mipChain = false, bool linear = true) {
            m_Width = width;
            m_Height = height;
            m_Format = format;
            m_AtlasTexture = new Image2D(width, height, format, mipChain ? 0 : 1);
            m_AtlasAllocator = new AtlasAllocator(width, height);
            texelSize = new Vector2(1f / m_Width, 1f / m_Height);
        }

        public void Release() {
            ResetAllocator();
        }

        public void ResetAllocator() {
            m_AtlasAllocator.Release();
            m_AllocationCache.Clear();
        }

        public bool FindTexture(string key, out Vector4 scaleBias) {
            return m_AllocationCache.TryGetValue(key, out scaleBias);
        }

        public bool AddTexture(ref Vector4 scaleBias, string key, int width, int height, BinaryImage[] mipmpas)  {
            if (!m_AllocationCache.TryGetValue(key, out scaleBias)) {
                if (m_AtlasAllocator.Allocate(ref scaleBias, width, height))  {
                    int numMips = Math.Min(m_AtlasTexture.numMips, mipmpas.Length);
                    uint x = (uint)scaleBias.Z;
                    uint y = (uint)scaleBias.W;
                    for(int level = 0; level < numMips; level++) {
                        m_AtlasTexture.SubImageUpload((uint)level, x, y, (uint)mipmpas[level].width, (uint)mipmpas[level].height, mipmpas[level].bytes);
                        x /= 2;
                        y /= 2;
                    }
                    m_AllocationCache.Add(key, scaleBias);
                    return true;
                } else {
                    return false;
                }
            }
            return true;
        }
    }
}