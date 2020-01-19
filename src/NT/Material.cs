using System;
using System.IO;
using SharpDX;

namespace NT
{
    public enum RenderQueue {
        Opaque,
        AlphaTest,
        Transparency,
        HairAlphaTest,
        HairTransparency
    }

    public unsafe struct MaterialUniformBlock {
        public uint uniformIndex;
        public IntPtr dataPtr;
        public uint blockSizeInBytes;

        public void Write(Vector4 value, int offsetInBytes) {
            float* ptr = (float*)(IntPtr.Add(dataPtr, offsetInBytes));
            *ptr = value.X;
            *(ptr + 1) = value.Y;
            *(ptr + 2) = value.Z;
            *(ptr + 3) = value.W;
        }
    }

    public struct MaterialImage {
        public uint uniformIndex;
        public Image image;
    }

    public struct MaterialSampler {
        public uint uniformIndex;
        public Veldrid.Sampler sampler;
    }

    [Flags]
    public enum MaterialFlags {
        None,
        NoShadow = 1 << 0,
        NoMotionVecotrs = 1 << 1,
        NoPrez = 1 << 2
    }

    public struct MaterialRenderProxy {
        public int numPositionStreams;
        public GPURenderState renderState;
        public Veldrid.ResourceSet renderPassResourceSet;
        public Veldrid.ResourceSet lightmapResourceSet;
        public Veldrid.ResourceSet materialResourceSet;
        public Veldrid.ResourceSet dynamicUniformBufferResourceSet;
        public MaterialUniformBlock[] uniformBlocks;
    }

    public class RenderStateResourceSet {
        public MaterialImage[] images;
        public MaterialSampler[] samplers;
        public MaterialUniformBlock[] uniformBlocks;
        public Veldrid.BindableResource[] bindableResources;
    }

    public class Material : Decl {
        public const int MaxBindTextures = 32;
        public const int MaxBindSamplers = 8;
        public const int MaxBindConstantBuffers = 8;

        [Flags]
        public enum DirtFlags
        {
            None = 0,
            Images = 1 << 0,
            Samplers = 1 << 1,
            UniformBlocks = 1 << 2,
            All = Images | Samplers | UniformBlocks
        }

        public Material() {}

        public Material(Shader inShader) {
            shader = inShader;
            renderStateIndex = 0;
            Init();
        }

        public override void Dispose() {
            shader = null;
            if(renderStateResourceSets != null) {
                for(int i = 0; i < renderStateResourceSets.Length; i++) {
                    var uniformBlocks = renderStateResourceSets[i].uniformBlocks;
                    if(uniformBlocks != null) {
                        for(int block = 0; block < uniformBlocks.Length; block++) {
                            Utilities.FreeMemory(uniformBlocks[block].dataPtr);
                        }
                        uniformBlocks = null;
                    }
                }
            }
            renderStateResourceSets = null;
        }

        string textureLoadPath = "";

        Shader shader;
        Shader prezShader;
        Shader shadowShader;
        Image2D opacityImage;
        int renderStateIndex;
        RenderStateResourceSet[] renderStateResourceSets;
        DirtFlags prezDirtFlags;
        DirtFlags shadowCastingDirtFlags;
        DirtFlags dirtFlags;
        MaterialRenderProxy cachedRenderProxy;
        public MaterialFlags materialFlags {get; private set;}
        public RenderQueue renderQueue {get; private set;}

        // default
        MaterialRenderProxy cachedPrezRenderProxy;
        MaterialRenderProxy cachedShadowRenderProxy;
        static Shader defaultPrezShader;
        static Shader defaultShadowShader;

        void ParseTextures(Lexer lex, ref Token token) {
            lex.ExpectTokenString("{");
            while(lex.ReadToken(ref token)) {
                if(token.lexme == "}") {
                    break;
                }

                string parmName = token.lexme;
                lex.ExpectTokenTypeOnLine(TokenType.String, TokenSubType.None, ref token);
                string imageName = token.lexme;
                string fullpath = Path.Combine(textureLoadPath, Path.GetFileNameWithoutExtension(imageName) + ".bimg");
                if(parmName == "_NormalMap") {
                    parmName = "_BumpMap";
                } else if(parmName == "_MaskMap") {
                    parmName = "_MetallicGlossMap";
                }
                SetImage(parmName, ImageManager.Image2DFromFile(fullpath));
            }
        }

        void ParseConstants(ref Lexer src, ref Token token) {
            src.ExpectTokenString("{");
            while(src.ReadToken(ref token)) {
                if(token.lexme == "}") {
                    break;
                }

                if(token == "texture") {
                    src.ReadTokenOnLine(ref token);
                    var parmName = token.lexme;
                    src.ReadTokenOnLine(ref token);
                    string fullpath = FileSystem.CreateAssetOSPath($"textures/{token.lexme}.bimg");
                    var image = ImageManager.Image2DFromFile(fullpath);
                    SetImage(parmName, image);       
                    if(src.CheckTokenString("ScaleOffset", ref token)) {
                        Vector4 scaleOffset = new Vector4(1f, 1f, 0f, 0f);
                        src.ParseCSharpVector(ref scaleOffset);
                        SetFloat4($"{parmName}_ST", scaleOffset);
                    }
                } else {
                    Vector4 v = Vector4.Zero;
                    src.ParseCSharpVector(ref v);
                    SetFloat4(token.lexme, v);
                }
            }               
        }

        void ParseConstants(Lexer lex, ref Token token) {
            lex.ExpectTokenString("{");
            while(lex.ReadToken(ref token)) {
                if(token.lexme == "}") {
                    break;
                }

                string parmName = token.lexme;
                Vector4 v = Vector4.Zero;
                lex.ParseCSharpVector(ref v);
                SetFloat4(parmName, v);
            }            
        }

        void ParseNew(ref Lexer src, ref Token token) {
            while(src.ReadToken(ref token)) {
                if(token.lexme == "}") {
                    break;
                }
                if(token.lexme == "shader") {
                    src.ReadTokenOnLine(ref token);
                    shader = Common.declManager.FindShader(token.lexme);
                    Init();
                    continue;
                }
                if(token == "renderState") {
                    SetRenderState(src.ParseInteger());
                }
                if(token == "prez") {
                    if(src.ExpectTokenTypeOnLine(TokenType.String, TokenSubType.None, ref token)) {
                        prezShader = Common.declManager.FindShader(token.lexme);
                    } else {
                        materialFlags |= MaterialFlags.NoPrez;
                    }
                    continue;
                }
                if(token == "shadowCaster") {
                    if(src.ExpectTokenTypeOnLine(TokenType.String, TokenSubType.None, ref token)) {
                        
                    } else {
                        materialFlags |= MaterialFlags.NoShadow;
                    }
                    continue;
                }
                if(token == "constants") {
                    ParseConstants(ref src, ref token);
                    continue;
                }
                if(token == "keywords") {
                    src.SkipBracedSection();
                    continue;
                }
            }
        }

        internal override void Parse() {
            if(text == null) {
                return;
            }

            Token token = new Token();
            Lexer lex = new Lexer(text);

            lex.ExpectTokenString("material");
            lex.ExpectTokenTypeOnLine(TokenType.String, TokenSubType.None, ref token);
            lex.ExpectTokenString("{");
            name = token.lexme;

            if(lex.CheckTokenString("version", ref token)) {
                lex.ParseInteger();
                ParseNew(ref lex, ref token);
                return;
            }

            while(lex.ReadToken(ref token)) {
                if(token.lexme == "}") {
                    break;
                }
                if(token.lexme == "textureLoadPath") {
                    lex.ExpectTokenTypeOnLine(TokenType.String, TokenSubType.None, ref token);
                    textureLoadPath = token.lexme;
                    continue;
                }
                if(token.lexme == "shader") {
                    lex.ReadTokenOnLine(ref token);
                    shader = Common.declManager.FindShader(token.lexme);
                    Init();
                    continue;
                }
                if(token == "prez") {
                    if(lex.ExpectTokenTypeOnLine(TokenType.String, TokenSubType.None, ref token)) {
                        prezShader = Common.declManager.FindShader(token.lexme);
                    } else {
                        materialFlags |= MaterialFlags.NoPrez;
                    }
                    continue;
                }
                if(token == "shadowCaster") {
                    if(lex.ExpectTokenTypeOnLine(TokenType.String, TokenSubType.None, ref token)) {
                        
                    } else {
                        materialFlags |= MaterialFlags.NoShadow;
                    }
                    continue;
                }
                if(token.lexme == "textures") {
                    ParseTextures(lex, ref token);
                    continue;
                }
                if(token == "Constants") {
                    ParseConstants(lex, ref token);
                    continue;
                }
            }
        }

        void SetDefault() {
            if(defaultPrezShader == null) {
                defaultPrezShader = Common.declManager.FindShader("builtin/prez");
            }
            if(defaultShadowShader == null) {
                defaultShadowShader = Common.declManager.FindShader("builtin/shadow");
            }
            if(prezShader == null) {
                prezShader = defaultPrezShader;
            }
            if(shadowShader == null) {
                shadowShader = defaultShadowShader;
            }
            if(GetMainImage() == null) {
                SetMainImage(Image2D.white);
            }
            SetFloat4("_MainTex_ST", new Vector4(1f, 1f, 0f, 0f));
        }

        void Init() {
            if(shader == null) {
                throw new Exception($"Material_{name} Init: shader is null.");
            }
            renderQueue = shader.renderQueue;

            renderStateResourceSets = new RenderStateResourceSet[shader.renderStateInfos.Length];
            for(int i = 0; i < shader.renderStateInfos.Length; i++) {
                RenderStateInfo renderStateInfo = shader.renderStateInfos[i];
                RenderStateResourceSet renderStateResourceSet = new RenderStateResourceSet(); 
                renderStateResourceSet.bindableResources = new Veldrid.BindableResource[shader.gpuRenderStates[i].numMaterialResources];
                if(renderStateInfo.numUniformBlocks > 0) {
                    renderStateResourceSet.uniformBlocks = new MaterialUniformBlock[renderStateInfo.numUniformBlocks];
                }
                if(renderStateInfo.numImages > 0) {
                    renderStateResourceSet.images = new MaterialImage[renderStateInfo.numImages];
                }
                if(renderStateInfo.numSamplers > 0) {
                    renderStateResourceSet.samplers = new MaterialSampler[renderStateInfo.numSamplers];
                }
                foreach(var uniformItem in shader.renderStateInfos[i].uniformInfos) {
                    var uniform = uniformItem.Value;
                    if(uniform.type == ShaderUniform.Type.UniformBuffer) {
                        renderStateResourceSet.uniformBlocks[uniform.index] = new MaterialUniformBlock();
                        renderStateResourceSet.uniformBlocks[uniform.index].uniformIndex = uniform.uniformIndex;
                        renderStateResourceSet.uniformBlocks[uniform.index].blockSizeInBytes = (uint)MathHelper.Align(uniform.sizeInBytes, (int)GraphicsDevice.gd.UniformBufferMinOffsetAlignment);
                        renderStateResourceSet.uniformBlocks[uniform.index].dataPtr = Utilities.AllocateClearedMemory(uniform.sizeInBytes);// new Vector4[uniform.sizeInBytes / 16];
                    } else if(uniform.type == ShaderUniform.Type.Image) {
                        renderStateResourceSet.images[uniform.index].uniformIndex = uniform.uniformIndex;
                    } else if(uniform.type == ShaderUniform.Type.Sampler) {
                        renderStateResourceSet.samplers[uniform.index].uniformIndex = uniform.uniformIndex;
                    }
                }
                renderStateResourceSets[i] = renderStateResourceSet;
            }

            SetDefault();
            prezDirtFlags = DirtFlags.All;
            shadowCastingDirtFlags = DirtFlags.All;
            dirtFlags = DirtFlags.All;
        }

        public void SetFloat4(string name, Vector4 value) {
            ShaderUniform uniform = shader.GetUniformInfo(name, renderStateIndex);
            if(uniform != null && uniform.type == ShaderUniform.Type.Float4) {
                renderStateResourceSets[renderStateIndex].uniformBlocks[uniform.uniformIndex].Write(value, uniform.offsetInBytes);
                prezDirtFlags |= DirtFlags.UniformBlocks;
                shadowCastingDirtFlags |= DirtFlags.UniformBlocks;
                dirtFlags |= DirtFlags.UniformBlocks;
            } else {
                Console.WriteLine($"not found uniform '{name}' in '{shader.name}'");
            }
        }

        public void SetImage(string name, Image image) {
            ShaderUniform uniform = shader.GetUniformInfo(name, renderStateIndex);
            if(uniform != null && uniform.type == ShaderUniform.Type.Image) {
                renderStateResourceSets[renderStateIndex].images[uniform.index].image = image;
                prezDirtFlags |= DirtFlags.Images;
                shadowCastingDirtFlags |= DirtFlags.Images;
                dirtFlags |= DirtFlags.Images;
            }
        }

        public void SetMainImage(Image image) {
            SetImage("_MainTex", image);
        }

        public void SetSampler(string name, Veldrid.Sampler sampler) {
            ShaderUniform uniform = shader.GetUniformInfo(name, renderStateIndex);
            if(uniform != null && sampler != null && uniform.type == ShaderUniform.Type.Sampler) {
                renderStateResourceSets[renderStateIndex].samplers[uniform.index].sampler = sampler;
                prezDirtFlags |= DirtFlags.Samplers;
                shadowCastingDirtFlags |= DirtFlags.Samplers;
                dirtFlags |= DirtFlags.Images;
            }            
        }

        public Image GetMainImage() {
            RenderStateResourceSet set = renderStateResourceSets[renderStateIndex];
            return set.images != null ? set.images[0].image : null;
        }

        public void SetOpacityImage(string name, Image2D image) {
        }

        public void SetRenderState(int index) {
            if(index >=0 && index < shader.renderStateInfos.Length) {
                renderStateIndex = index;
                prezDirtFlags = DirtFlags.All;
                shadowCastingDirtFlags = DirtFlags.All;
                dirtFlags = DirtFlags.All;
            } else {
                throw new InvalidDataException();
            }
        }

        internal MaterialRenderProxy MakeStaticModelPrezProxy(int numPositionStreams) {
            return MakePrezProxy(true, numPositionStreams);
        }

        internal MaterialRenderProxy MakeDynamicModelPrezProxy(int numPositionStreams) {
            return MakePrezProxy(false, numPositionStreams);
        }

        private MaterialRenderProxy MakePrezProxy(bool isStaticModel, int numPositionStreams) {
            if(materialFlags.HasFlag(MaterialFlags.NoPrez) || prezShader == null) {
                return default(MaterialRenderProxy);
            }
            if(prezDirtFlags == DirtFlags.None && cachedPrezRenderProxy.numPositionStreams == numPositionStreams) {
                return cachedPrezRenderProxy;
            }
            MaterialRenderProxy renderProxy = new MaterialRenderProxy();
            renderProxy.numPositionStreams = numPositionStreams;
            int offset = isStaticModel ? 0 : 2;
            if(renderQueue == RenderQueue.AlphaTest) {
                //cachedRenderProxy.materialResourceSet?.Dispose();
                //cachedRenderProxy.materialResourceSet = null;
                var renderState = prezShader.gpuRenderStates[offset + 1];
                var renderPass = prezShader.renderStateInfos[offset + 1].renderPass;
                renderProxy.renderState = renderState;
                //renderProxy.allRenderStates = prezShader.gpuRenderStates;
                renderProxy.renderPassResourceSet = renderPass.GetPerFrameResourceSet();
                renderProxy.materialResourceSet = GraphicsDevice.ResourceFactory.CreateResourceSet(new Veldrid.ResourceSetDescription(renderState.materialResourceLayout, GetMainImage().textureObject));
                renderProxy.dynamicUniformBufferResourceSet = renderProxy.renderState.dynamicCBufferResourceSet;
                cachedPrezRenderProxy = renderProxy;
            } else if(renderQueue == RenderQueue.Opaque) {
                var renderPass = prezShader.renderStateInfos[offset + 0].renderPass;
                renderProxy.renderState = prezShader.gpuRenderStates[offset + 0];
                //renderProxy.allRenderStates = prezShader.gpuRenderStates;
                renderProxy.renderPassResourceSet = renderPass.GetPerFrameResourceSet();
                renderProxy.dynamicUniformBufferResourceSet = renderProxy.renderState.dynamicCBufferResourceSet;
                cachedPrezRenderProxy = renderProxy;
            }
            prezDirtFlags = DirtFlags.None;
            return renderProxy;
        }

        internal MaterialRenderProxy MakeStaticModelShadowCastingProxy(int numPositionStreams) {
            return MakeShadowCastingProxy(true, numPositionStreams);
        }

        internal MaterialRenderProxy MakeDynamicModelShadowCastingProxy(int numPositionStreams) {
            return MakeShadowCastingProxy(false, numPositionStreams);
        }

        private MaterialRenderProxy MakeShadowCastingProxy(bool isStaticModel, int numPositionStreams) {
            if(materialFlags.HasFlag(MaterialFlags.NoShadow) || shadowShader == null) {
                return default(MaterialRenderProxy);
            }
            if(shadowCastingDirtFlags == DirtFlags.None && cachedShadowRenderProxy.numPositionStreams == numPositionStreams) {
                return cachedShadowRenderProxy;
            }
            MaterialRenderProxy renderProxy = new MaterialRenderProxy();
            renderProxy.numPositionStreams = numPositionStreams;
            int offset = isStaticModel ? 0 : 2;
            if(renderQueue == RenderQueue.AlphaTest) {
                //cachedRenderProxy.materialResourceSet?.Dispose();
                //cachedRenderProxy.materialResourceSet = null;
                var renderState = shadowShader.gpuRenderStates[offset + 1];
                var renderPass = shadowShader.renderStateInfos[offset + 1].renderPass;
                renderProxy.renderState = renderState;
                renderProxy.renderPassResourceSet = renderPass.GetPerFrameResourceSet();
                renderProxy.materialResourceSet = GraphicsDevice.ResourceFactory.CreateResourceSet(new Veldrid.ResourceSetDescription(renderState.materialResourceLayout, GetMainImage().textureObject));
                renderProxy.dynamicUniformBufferResourceSet = renderProxy.renderState.dynamicCBufferResourceSet;
                cachedShadowRenderProxy = renderProxy;
            } else if(renderQueue == RenderQueue.Opaque) {
                var renderPass = shadowShader.renderStateInfos[offset + 0].renderPass;
                renderProxy.renderState = shadowShader.gpuRenderStates[offset + 0];
                renderProxy.renderPassResourceSet = renderPass.GetPerFrameResourceSet();
                renderProxy.dynamicUniformBufferResourceSet = renderProxy.renderState.dynamicCBufferResourceSet;
                cachedShadowRenderProxy = renderProxy;
            }
            shadowCastingDirtFlags = DirtFlags.None;
            return renderProxy;
        }

        internal MaterialRenderProxy MakeLightingProxy(Image2D lightmapDirMap = null, Image2D lightmapColorMap = null) {
            var renderState = shader.gpuRenderStates[renderStateIndex];
            if(dirtFlags == DirtFlags.None) {
                if(renderState.lightmapResourceLayout != null && lightmapDirMap != null && lightmapColorMap != null) {
                    cachedRenderProxy.lightmapResourceSet = GraphicsDevice.ResourceFactory.CreateResourceSet(
                        new Veldrid.ResourceSetDescription(renderState.lightmapResourceLayout, lightmapDirMap.textureObject, lightmapColorMap.textureObject)
                    );
                }
                return cachedRenderProxy;
            }

            MaterialRenderProxy renderProxy = new MaterialRenderProxy();
            var renderPass = shader.renderStateInfos[renderStateIndex].renderPass;
            renderProxy.renderState = renderState;
            renderProxy.renderPassResourceSet = renderPass.GetPerFrameResourceSet();

            if(renderState.lightmapResourceLayout != null && lightmapDirMap != null && lightmapColorMap != null) {
                renderProxy.lightmapResourceSet = GraphicsDevice.ResourceFactory.CreateResourceSet(
                    new Veldrid.ResourceSetDescription(renderState.lightmapResourceLayout, lightmapDirMap.textureObject, lightmapColorMap.textureObject)
                );
            }

            if(renderState.materialResourceLayout != null) {
                var images = renderStateResourceSets[renderStateIndex].images;
                var samplers = renderStateResourceSets[renderStateIndex].samplers;
                var bindableResources = renderStateResourceSets[renderStateIndex].bindableResources;
                if(images != null && dirtFlags.HasFlag(DirtFlags.Images)) {
                    for(int i = 0; i < images.Length; i++) {
                        var imageRef = images[i].image;
                        int binding = (int)images[i].uniformIndex - (renderState.numDynamicCBuffers > 0 ? 1 : 0);
                        binding = binding < 0 ? 0 : binding;
                        if (imageRef == null) {
                            bindableResources[binding] = Image2D.black.textureObject;
                        } else {
                            if(imageRef.textureView != null) {
                                bindableResources[binding] = images[i].image.textureView;
                            } else {
                                bindableResources[binding] = images[i].image.textureObject;
                            }
                        }
                    }
                    dirtFlags &= ~DirtFlags.Images;
                }
                if(samplers != null && dirtFlags.HasFlag(DirtFlags.Samplers)) {
                    for(int i = 0; i < samplers.Length; i++) {
                        int binding = (int)samplers[i].uniformIndex - (renderState.numDynamicCBuffers > 0 ? 1 : 0);
                        bindableResources[binding] = samplers[i].sampler;
                    }
                    dirtFlags &= ~DirtFlags.Samplers;
                }
                renderProxy.materialResourceSet = GraphicsDevice.ResourceFactory.CreateResourceSet(new Veldrid.ResourceSetDescription(renderState.materialResourceLayout, bindableResources));
            }

            renderProxy.dynamicUniformBufferResourceSet = renderState.dynamicCBufferResourceSet;
            if(renderState.dynamicCBufferResourceLayout != null) {
                var uniformBlocks = renderStateResourceSets[renderStateIndex].uniformBlocks;
                if(uniformBlocks != null && dirtFlags.HasFlag(DirtFlags.UniformBlocks)) {
                    renderProxy.uniformBlocks = new MaterialUniformBlock[uniformBlocks.Length];
                    for(int i = 0; i < uniformBlocks.Length; i++) {
                        uint blockSize = uniformBlocks[i].blockSizeInBytes;
                        IntPtr blockData = FrameAllocator.AllocMemory((int)blockSize);
                        Utilities.CopyMemory(blockData, uniformBlocks[i].dataPtr, (int)uniformBlocks[i].blockSizeInBytes);
                        renderProxy.uniformBlocks[i].blockSizeInBytes = blockSize;
                        renderProxy.uniformBlocks[i].dataPtr = blockData;
                    }
                } else {
                    renderProxy.uniformBlocks = uniformBlocks;
                }
                dirtFlags &= ~DirtFlags.UniformBlocks;
            }

            //cachedRenderProxy.materialResourceSet?.Dispose();
            cachedRenderProxy = renderProxy;
            // updated uniform blocks...
            cachedRenderProxy.uniformBlocks = renderStateResourceSets[renderStateIndex].uniformBlocks;
            return renderProxy;
        }

    }
}