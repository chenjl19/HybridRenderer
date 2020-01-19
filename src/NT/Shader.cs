using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using SharpDX.D3DCompiler;

namespace NT
{
    internal class SpvMemberInfo {
        public string name;
        public int offsetInBytes;
        public int sizeInBytes;
    }

    internal class SpvUniformInfo {
        public string name;
        public uint index;
        public uint set;
        public uint binding;
        public Veldrid.ShaderStages stages;
        public Veldrid.ResourceKind kind;
    }

    internal class SpvStructInfo : SpvUniformInfo {
        public int blockSize;
        public List<SpvMemberInfo> memberInfos;

        public SpvStructInfo() {
            memberInfos = new List<SpvMemberInfo>();
        }
    }

    internal class DescriptorStructInfo : SpvUniformInfo {
        public int blockSize;
        public SpvMemberInfo[] memberInfos;
    }

    internal class DescriptorSetLayoutInfo {
        public List<SpvUniformInfo> uniformInfos;
        public Veldrid.ResourceLayoutElementDescription[] lightmapDescriptions;
        public List<Veldrid.BindableResource> renderPassDynamicCBuffers;
        public List<Veldrid.ResourceLayoutElementDescription> dynamicCBufferDescriptions;
        public List<Veldrid.ResourceLayoutElementDescription> layoutElementDescriptions;  

        public DescriptorSetLayoutInfo() {
            uniformInfos = new List<SpvUniformInfo>();
            renderPassDynamicCBuffers = new List<Veldrid.BindableResource>();
            dynamicCBufferDescriptions = new List<Veldrid.ResourceLayoutElementDescription>();
            layoutElementDescriptions = new List<Veldrid.ResourceLayoutElementDescription>();
        }

        public bool FindUniform(string name, out int found) {
            if(uniformInfos != null) {
                for(int i = 0; i < uniformInfos.Count; i++) {
                    if(uniformInfos[i].name == name) {
                        found = i;
                        return true;
                    }
                }
            }
            found = -1;
            return false;
        }      
    }

    internal class ShaderResourceLayoutInfo {
        public List<Veldrid.ResourceLayoutElementDescription> layoutElementDescriptions;
        public List<Veldrid.BindableResource> commonResources;
        public List<SpvUniformInfo> uniformInfos;
        public List<uint> bindings;

        void Swap<T>(ref T a, ref T b) {
            var tmp = a;
            a = b;
            b = tmp;
        }

        public Veldrid.ResourceLayoutElementDescription[] elementDescriptionsSorted;
        public Veldrid.BindableResource[] commonResourcesSorted;

        public void SortByBindings() {
            elementDescriptionsSorted = layoutElementDescriptions.ToArray();
            commonResourcesSorted = commonResources != null ? commonResources.ToArray() : null;
            if(bindings != null) {
                for(int j = 0; j < bindings.Count; j++) {
                    for(int k = 0; k < bindings.Count - 1 - j; k++) {
                        if(bindings[k] > bindings[k + 1]) {
                            var tmp = bindings[k];
                            bindings[k] = bindings[k + 1];
                            bindings[k + 1] = tmp;
                            Swap(ref elementDescriptionsSorted[k], ref elementDescriptionsSorted[k + 1]);
                            if(commonResourcesSorted != null) {
                                Swap(ref commonResourcesSorted[k], ref commonResourcesSorted[k + 1]);
                            }
                        }
                    }
                }
            }
        }

        public int FindLayoutElement(string name) {
            if(layoutElementDescriptions != null) {
                for(int i = 0; i < layoutElementDescriptions.Count; i++) {
                    if(layoutElementDescriptions[i].Name == name) {
                        return i;
                    }
                }
            }
            return -1;
        }

        public int FindUniform(string name) {
            if(uniformInfos != null) {
                for(int i = 0; i < uniformInfos.Count; i++) {
                    if(uniformInfos[i].name == name) {
                        return i;
                    }
                }
            }
            return -1;
        }
    }

    public class ShaderStageDefine {
        public Veldrid.ShaderStages stage;
        public string filename;
        public string sourceText;
        public string entryPointName;
        public SharpDX.Direct3D.ShaderMacro[] macroDefinitions;
        public Veldrid.ResourceLayout[] resourceLayouts;

        public ShaderStageDefine(Veldrid.ShaderStages stage) {
            this.stage = stage;
            entryPointName = "main";
        }
    }

    public class ShaderUniform {
        public enum Type {
            None,
            Image,
            Sampler,
            Float4,
            UniformBuffer
        }

        public string name;
        public Type type;
        public uint uniformIndex;
        public uint index;
        public uint set;
        public uint binding;
        public int offsetInBytes;
        public int sizeInBytes;
    }

    public class ShaderTechnique {
        public string renderPass;
        public string vertexFactory;
        public string shaderFilename;
    }

    public class ShaderInclude : Include {
        readonly string assetPath;

        public ShaderInclude(string inAssetPath) {
            assetPath = inAssetPath;
        }

        public void Close(Stream stream) {
            stream?.Dispose();
        }

        public Stream Open(IncludeType type, string fileName, Stream parentStream) {
            if(type == IncludeType.Local) {
                string fullpath = Path.Combine(FileSystem.basePath, assetPath, fileName);
                return File.OpenRead(fullpath);
            } else {

            }
            return null;
        }

        public void Dispose() {
        }

        public IDisposable Shadow {get {return this;} set{}}
    }

    public class RenderStateInfo {
        public string name;
        public RenderPass renderPass;
        public Veldrid.VertexLayoutDescription[] vertexLayoutDescriptions;
        public RenderStateBlock stateBlock;
        public ShaderStageDefine[] stageDefines;
        public uint numUniforms;
        public uint numImages;
        public uint numSamplers;
        public uint numUniformBlocks;
        public Dictionary<string, ShaderUniform> uniformInfos;
    }

    public class GPURenderState : IDisposable {
        public int numMaterialResources;
        public int numDynamicCBuffers;
        public Veldrid.Pipeline pipeline;
        public Veldrid.ResourceSet dynamicCBufferResourceSet;
        public Veldrid.ResourceLayout renderPassResourceLayout;
        public Veldrid.ResourceLayout lightmapResourceLayout;
        public Veldrid.ResourceLayout materialResourceLayout;
        public Veldrid.ResourceLayout dynamicCBufferResourceLayout;
        //public Veldrid.ResourceLayout[] resourceLayouts;

        public void Dispose() {
            pipeline?.Dispose();
            dynamicCBufferResourceLayout?.Dispose();
            renderPassResourceLayout?.Dispose();
            materialResourceLayout?.Dispose();
            dynamicCBufferResourceLayout?.Dispose();
        }
    }

    public class Shader : Decl {
        public RenderStateInfo[] renderStateInfos {get; private set;}
        public GPURenderState[] gpuRenderStates {get; private set;}
        public RenderQueue renderQueue {get; private set;}

        public Shader() {}

        public Shader(string inName) {
            name = inName;
            renderQueue = RenderQueue.Opaque;
        }

        public ShaderUniform GetUniformInfo(int index, int renderStateIndex) {
            return null;
        }

        public ShaderUniform GetUniformInfo(string name, int renderStateIndex) {
            if(renderStateInfos == null) {
                return null;
            }
            if(renderStateIndex < 0 || renderStateIndex >= renderStateInfos.Length) {
                return null;
            }
            renderStateInfos[renderStateIndex].uniformInfos.TryGetValue(name, out ShaderUniform info);
            return info;
        }

        bool GetState(Token token) {
            if(token.lexme == "on") {
                return true;
            }
            if(token.lexme == "off") {
                return false;
            }
            return false;
        }

        CullMode ParseCullMode(ref Lexer lexer) {
            Token token = new Token();

            if(!lexer.ReadTokenOnLine(ref token)) {
                throw new InvalidDataException();
            }

            if(token.lexme == "Back") {
                return CullMode.BackSided;
            } else if(token.lexme == "Front") {
                return CullMode.FrontSided;
            } else {
                return CullMode.None;
            }
        }

        PolygonMode ParseFillMode(ref Lexer lexer) {
            Token token = new Token();

            if(!lexer.ReadTokenOnLine(ref token)) {
                throw new InvalidDataException();
            }

            if(token.lexme == "Solid") {
                return PolygonMode.Fill;
            } else if(token.lexme == "Wireframe") {
                return PolygonMode.Line;
            } else {
                return PolygonMode.Line;
            }
        }

        FrontFace ParseFrontFace(ref Lexer lexer) {
            Token token = new Token();
            if(!lexer.ReadTokenOnLine(ref token)) {
                throw new InvalidDataException();
            }
            return (FrontFace)Enum.Parse(typeof(FrontFace), token.lexme, true);                     
        }

        CompareOp ParseDepthFunc(ref Lexer lexer) {
            Token token = new Token();
            if(!lexer.ReadTokenOnLine(ref token)) {
                throw new InvalidDataException();
            }
            return token == "off" ? CompareOp.Always : (CompareOp)Enum.Parse(typeof(CompareOp), token.lexme, true);
        }

        BlendOp ParseBlendOp(ref Lexer lexer) {
            Token token = new Token();
            if(!lexer.ReadTokenOnLine(ref token)) {
                throw new InvalidDataException();
            }
            return (BlendOp)Enum.Parse(typeof(BlendOp), token.lexme, true);            
        }

        BlendFactor ParseBlendFactor(ref Lexer lexer) {
            Token token = new Token();
            if(!lexer.ReadTokenOnLine(ref token)) {
                throw new InvalidDataException();
            }
            return (BlendFactor)Enum.Parse(typeof(BlendFactor), token.lexme, true);                   
        }

        PrimitiveTopologyType ParseTopology(ref Lexer lexer) {
            Token token = new Token();
            if(!lexer.ReadTokenOnLine(ref token)) {
                throw new InvalidDataException();
            }
            return (PrimitiveTopologyType)Enum.Parse(typeof(PrimitiveTopologyType), token.lexme, true);                   
        }

        public override void Dispose() {
            if(gpuRenderStates != null) {
                for(int i = 0; i < gpuRenderStates.Length; i++) {
                    gpuRenderStates[i].Dispose();
                }
            }
            gpuRenderStates = null;
        }

        static readonly string assetPath;
        static Dictionary<string, string> loadedShaderTexts;
        //static Dictionary<Tuple<string, ShaderStages, string>, BytecodeInfo> loadedBytecodes;
        public static readonly ShaderInclude shaderInclude;

        static Shader() {
            assetPath = FileSystem.shadersPath;
            loadedShaderTexts = new Dictionary<string, string>();
            //loadedBytecodes = new Dictionary<Tuple<string, ShaderStages, string>, BytecodeInfo>();
            shaderInclude = new ShaderInclude(Path.Combine(assetPath, "glsl"));
        }
        static string GetShaderSourceText(string name, out string fullpath) {
            fullpath = Path.Combine(FileSystem.basePath, assetPath, name);
            if (!loadedShaderTexts.TryGetValue(fullpath, out string text)) {
                if (!File.Exists(fullpath)) {
                    return null;
                }
                text = File.ReadAllText(fullpath);
                loadedShaderTexts.Add(fullpath, text);
            }

            return text;
        }

        static Veldrid.ShaderStages GetShaderStages(ShaderStages stages) {
            Veldrid.ShaderStages result = Veldrid.ShaderStages.None;
            if(stages.HasFlag(ShaderStages.Vertex)) {
                result |= Veldrid.ShaderStages.Vertex;
            }
            if(stages.HasFlag(ShaderStages.Pixel)) {
                result |= Veldrid.ShaderStages.Fragment;
            }
            if(stages.HasFlag(ShaderStages.Compute)) {
                result = Veldrid.ShaderStages.Compute;
            }
            return result;
        } 

        void ParseRenderState(Lexer lex, ref Token token, ref List<RenderStateInfo> renderStates) {
            lex.ReadTokenOnLine(ref token);
            lex.ExpectTokenString("{");

            string stateName = "";
            RenderStateBlock stateBlock = new RenderStateBlock();
            RenderStateHelper.SetDefault(ref stateBlock);
            List<ShaderStageDefine> stageDefines = new List<ShaderStageDefine>();
            Veldrid.VertexLayoutDescription[] vertexLayoutDescriptions = null;
            RenderPass renderPass = null;

            while(lex.ReadToken(ref token)) {
                if(token == "}") {
                    break;
                }

                if(token == "name") {
                    lex.ExpectTokenTypeOnLine(TokenType.String, TokenSubType.None, ref token);
                    stateName = token.lexme;
                    continue;
                }

                if(token == "RenderPass") {
                    lex.ExpectTokenTypeOnLine(TokenType.String, TokenSubType.None, ref token);
                    renderPass = Common.frameGraph.FindRenderPass(token.lexme);
                    if(renderPass == null) {
                        throw new InvalidDataException($"Not found RenderPass:{token.lexme}");
                    }
                    continue;
                }   

                if(token == "VertexFactory") {
                    lex.ExpectTokenTypeOnLine(TokenType.String, TokenSubType.None, ref token);
                    if(!string.IsNullOrEmpty(token.lexme)) {
                        var vertexFactoryClass = Type.GetType("NT." + token.lexme);
                        if (vertexFactoryClass == null) {
                            throw new InvalidDataException($"Shader.Parse():vertexFactory == null");
                        }
                        FieldInfo fieldInfo = vertexFactoryClass.GetField("vertexLayoutDescriptions", BindingFlags.Static | BindingFlags.NonPublic);
                        if (fieldInfo == null) {
                            throw new InvalidDataException($"Shader.Parse():not found vertexLayoutDescriptions in {token.lexme}");
                        }
                        vertexLayoutDescriptions = (Veldrid.VertexLayoutDescription[])fieldInfo.GetValue(null);
                    }
                    continue;                    
                } 

                if(token == "vs") {
                    if(renderPass == null) {
                        throw new InvalidDataException($"RenderPass is null.");
                    }
                    var define = new ShaderStageDefine(Veldrid.ShaderStages.Vertex);
                    ParseShaderStage(lex, ref token, define);
                    stageDefines.Add(define);
                    continue;
                }

                if(token == "fs") {
                    if(renderPass == null) {
                        throw new InvalidDataException($"RenderPass is null.");
                    }
                    var define = new ShaderStageDefine(Veldrid.ShaderStages.Fragment);
                    ParseShaderStage(lex, ref token, define);
                    stageDefines.Add(define);
                    continue;
                }

                if(token == "CullMode") {
                    RenderStateHelper.SetCullMode(ref stateBlock, ParseCullMode(ref lex));
                    continue;
                }

                if(token == "FillMode") {
                    RenderStateHelper.SetPolygonMode(ref stateBlock, ParseFillMode(ref lex));
                    continue;                    
                }

                if(token == "FrontFace") {
                    RenderStateHelper.SetFrontFace(ref stateBlock, ParseFrontFace(ref lex));
                    continue;
                }

                if(token == "ZTest") {
                    RenderStateHelper.SetDepthFunc(ref stateBlock, ParseDepthFunc(ref lex));
                    continue;
                } 

                if(token == "ZWrite") {
                    if(!lex.ReadTokenOnLine(ref token)) {
                        throw new InvalidDataException();
                    }
                    RenderStateHelper.SetDepthWrite(ref stateBlock, GetState(token));
                    continue;
                }

                if(token == "ZClip") {
                    if(!lex.ReadTokenOnLine(ref token)) {
                        throw new InvalidDataException();
                    }
                    if(token.lexme.ToLower() == "off") {
                        RenderStateHelper.DisableDepthClip(ref stateBlock);
                    } else {
                        RenderStateHelper.EnableDepthClip(ref stateBlock);
                    }
                    continue;
                }

                if(token == "Topology") {
                    RenderStateHelper.SetPrimitiveTopology(ref stateBlock, ParseTopology(ref lex));
                    continue;
                }

                if(token == "Blend") {
                    RenderStateHelper.SetBlendFactor(ref stateBlock, ParseBlendFactor(ref lex), ParseBlendFactor(ref lex));
                    continue;
                }

                if(token == "BlendOp") {
                    RenderStateHelper.SetBlendOp(ref stateBlock, ParseBlendOp(ref lex));
                    continue;
                }

                if(token == "AlphaBlend") {
                    RenderStateHelper.SetAlphaBlendFactor(ref stateBlock, ParseBlendFactor(ref lex), ParseBlendFactor(ref lex));
                    continue;
                }

                if(token == "AlphaBlendOp") {
                    RenderStateHelper.SetAlphaBlendOp(ref stateBlock, ParseBlendOp(ref lex));
                    continue;
                }
            }

            RenderStateInfo newState = new RenderStateInfo();
            newState.name = stateName;
            newState.renderPass = renderPass;
            newState.vertexLayoutDescriptions = vertexLayoutDescriptions;
            newState.stateBlock = stateBlock;
            newState.stageDefines = stageDefines.ToArray();
            renderStates.Add(newState);
        }

        SharpDX.Direct3D.ShaderMacro[] ParseMacroDefinition(Lexer lex, ref Token token) {
            SharpDX.Direct3D.ShaderMacro[] macroDefinitions = null;
            List<SharpDX.Direct3D.ShaderMacro> defines = new List<SharpDX.Direct3D.ShaderMacro>();
            while(lex.ReadTokenOnLine(ref token)) {
                if(token.type != TokenType.String) {
                    defines.Add(new SharpDX.Direct3D.ShaderMacro(token.lexme, 1));
                } else {
                    string[] defineStrs = token.lexme.Split();
                    if(defineStrs != null) {
                        if(defineStrs.Length == 1) {
                            defines.Add(new SharpDX.Direct3D.ShaderMacro(defineStrs[0], 1));
                        } else {
                            if(int.TryParse(defineStrs[1], out int integerValue)) {
                                defines.Add(new SharpDX.Direct3D.ShaderMacro(defineStrs[0], integerValue));
                            } else if(float.TryParse(defineStrs[1], out float floatValue)) {
                                defines.Add(new SharpDX.Direct3D.ShaderMacro(defineStrs[0], floatValue));
                            } else {
                                defines.Add(new SharpDX.Direct3D.ShaderMacro(defineStrs[0], defineStrs[1]));
                            }
                        }
                    }
                }
            }
            if(defines.Count != 0) {
                macroDefinitions = defines.ToArray();
            }
            return macroDefinitions;
        }

        Veldrid.ResourceKind GetResourceKind(string type) {
            if(type == "texture") {
                return Veldrid.ResourceKind.TextureReadOnly;
            } else if(type == "rwtexture") {
                return Veldrid.ResourceKind.TextureReadWrite;
            } else if(type == "sampler") {
                return Veldrid.ResourceKind.Sampler;
            } else if(type == "buffer") {
                return Veldrid.ResourceKind.StructuredBufferReadOnly;
            } else if(type == "rwbuffer") {
                return Veldrid.ResourceKind.StructuredBufferReadWrite;
            } else if(type == "uniformBlock") {
                return Veldrid.ResourceKind.UniformBuffer;
            } else {
                throw new InvalidDataException();
            }
        }

        Veldrid.ShaderStages ParseResourceStages(Lexer lex, ref Token token) {
            Veldrid.ShaderStages stages = Veldrid.ShaderStages.None;
            while(lex.ReadTokenOnLine(ref token)) {
                if(token == "vs") {
                    stages |= Veldrid.ShaderStages.Vertex;
                } else if(token == "ps" || token == "fs") {
                    stages |= Veldrid.ShaderStages.Fragment;
                } else {
                    throw new InvalidDataException();
                }
            }
            if(stages == Veldrid.ShaderStages.None) {
                throw new InvalidDataException();
            }
            return stages;
        }

        public class ResourceBindDesc {
            public string name;
            public Veldrid.ResourceKind kind;
            public Veldrid.ShaderStages stages;
        }

        void AddResourceLayoutElementDesc(List<ResourceBindDesc> descriptions, string type, string name, Lexer lex, ref Token token) {
            Veldrid.ResourceKind kind = GetResourceKind(type);
            Veldrid.ShaderStages stages = ParseResourceStages(lex, ref token);
            int i = 0;
            for(; i < descriptions.Count; i++) {
                if(descriptions[i].name == name) {
                    if(descriptions[i].stages.HasFlag(stages)) {
                        throw new InvalidDataException();
                    } else {
                        descriptions[i].stages |= stages;
                        return;
                    }
                }
            }

            ResourceBindDesc desc = new ResourceBindDesc();
            desc.name = name;
            desc.kind = kind;
            desc.stages = stages;
            descriptions.Add(desc);
        }

        Veldrid.ResourceLayout ParseResourceLayout(Lexer lex, ref Token token) {
            List<ResourceBindDesc> bindDescs = new List<ResourceBindDesc>();
            while(token != "}") {
                string type = token.lexme;
                lex.ReadTokenOnLine(ref token);
                string name = token.lexme;
                AddResourceLayoutElementDesc(bindDescs, type, name, lex, ref token);
            }

            Veldrid.ResourceLayout layout = null;
            if(bindDescs.Count > 0) {
                Veldrid.ResourceLayoutElementDescription[] descriptions = new Veldrid.ResourceLayoutElementDescription[bindDescs.Count];
                for(int i = 0; i < descriptions.Length; i++) {
                    descriptions[i] = new Veldrid.ResourceLayoutElementDescription(bindDescs[i].name, bindDescs[i].kind, bindDescs[i].stages);
                }
                layout = GraphicsDevice.ResourceFactory.CreateResourceLayout(new Veldrid.ResourceLayoutDescription(descriptions));
            }
            return layout;
        }

        void ParseShaderStage(Lexer lex, ref Token token, ShaderStageDefine define) {
            lex.ExpectTokenString("{");
            while(lex.ReadToken(ref token)) {
                if(token == "}") {
                    break;
                }

                if(token == "shaderFile") {
                    lex.ReadTokenOnLine(ref token);
                    define.filename = token.lexme;
                    define.sourceText = GetShaderSourceText(define.filename, out _);
                    if(string.IsNullOrEmpty(define.sourceText)) {
                        throw new InvalidDataException($"Not found shader file {token.lexme}");
                    }
                    continue;
                }

                if(token == "defines") {
                    define.macroDefinitions = ParseMacroDefinition(lex, ref token);
                    continue;
                }

                if(token == "entryPoint") {
                    lex.ReadTokenOnLine(ref token);
                    define.entryPointName = token.lexme;
                    continue;
                }
            }
        }

        static Veldrid.ComparisonKind GetCompareOp(CompareOp op) {
            switch(op) {
                case CompareOp.Never:
                    return Veldrid.ComparisonKind.Never;
                case CompareOp.Always:
                    return Veldrid.ComparisonKind.Always;
                case CompareOp.Equal:
                    return Veldrid.ComparisonKind.Equal;
                case CompareOp.GEqual:
                    return Veldrid.ComparisonKind.GreaterEqual;
                case CompareOp.Greater:
                    return Veldrid.ComparisonKind.Greater;
                case CompareOp.Less:
                    return Veldrid.ComparisonKind.Less;
                case CompareOp.NotEqual:
                    return Veldrid.ComparisonKind.NotEqual;
                case CompareOp.LEqual:
                default:
                    return Veldrid.ComparisonKind.LessEqual;
            }
        }

        static Veldrid.BlendFunction GetBlendOp(BlendOp op) {
            switch(op) {
                default:
                case BlendOp.Add:
                    return Veldrid.BlendFunction.Add;
                case BlendOp.Sub:
                    return Veldrid.BlendFunction.Subtract;
                case BlendOp.Min:
                    return Veldrid.BlendFunction.Minimum;
                case BlendOp.Max:
                    return Veldrid.BlendFunction.Maximum;
            }
        }

        static Veldrid.BlendFactor GetBlendFactor(BlendFactor factor) {
            switch(factor) {
                default:
                case BlendFactor.One:
                    return Veldrid.BlendFactor.One;
                case BlendFactor.Zero:
                    return Veldrid.BlendFactor.Zero;
                case BlendFactor.SrcAlpha:
                    return Veldrid.BlendFactor.SourceAlpha;
                case BlendFactor.SrcColor:
                    return Veldrid.BlendFactor.SourceColor;
                case BlendFactor.OneMinusSrcAlpha:
                    return Veldrid.BlendFactor.InverseSourceAlpha;
                case BlendFactor.OneMinusSrcColor:
                    return Veldrid.BlendFactor.InverseSourceColor;
                case BlendFactor.DstColor:
                    return Veldrid.BlendFactor.DestinationColor;
                case BlendFactor.DstAlpha:
                    return Veldrid.BlendFactor.DestinationAlpha;
                case BlendFactor.OneMinusDstAlpha:
                    return Veldrid.BlendFactor.InverseDestinationAlpha;
                case BlendFactor.OneMinusDstColor:
                    return Veldrid.BlendFactor.InverseDestinationColor;
                case BlendFactor.SrcAlphaSaturate:
                    throw new InvalidDataException();
            }
        }

        static Veldrid.StencilOperation GetStencilOp(StencilOp op) {
            switch(op) {
                case StencilOp.Keep:
                default:
                    return Veldrid.StencilOperation.Keep;
                case StencilOp.Increment:
                    return Veldrid.StencilOperation.IncrementAndWrap;
                case StencilOp.IncrementAndClamp:
                    return Veldrid.StencilOperation.IncrementAndClamp;
                case StencilOp.Decrement:
                    return Veldrid.StencilOperation.DecrementAndWrap;
                case StencilOp.DecrementAndClamp:
                    return Veldrid.StencilOperation.DecrementAndClamp;
                case StencilOp.Invert:
                    return Veldrid.StencilOperation.Invert;
                case StencilOp.Replace:
                    return Veldrid.StencilOperation.Replace;
                case StencilOp.Zero:
                    return Veldrid.StencilOperation.Zero;
            }
        }

        public static void CreateBlendStateDescription(RenderStateBlock stateBlock, out Veldrid.BlendStateDescription description) {
            description = Veldrid.BlendStateDescription.SingleDisabled;
            description.AttachmentStates[0].BlendEnabled = false;
            if(RenderStateHelper.IsBlendEnabled(stateBlock.blendState)) {
                description.AlphaToCoverageEnabled = RenderStateHelper.IsAlphaToCoverageEnabled(stateBlock.blendState);
                description.AttachmentStates[0].BlendEnabled = true;
                description.AttachmentStates[0].AlphaFunction = GetBlendOp((BlendOp)RenderStateHelper.GetAlphaBlendOp(stateBlock.blendState));
                description.AttachmentStates[0].ColorFunction = GetBlendOp((BlendOp)RenderStateHelper.GetColorBlendOp(stateBlock.blendState));
                description.AttachmentStates[0].SourceAlphaFactor = GetBlendFactor((BlendFactor)RenderStateHelper.GetSrcAlphaBlendFactor(stateBlock.blendState));
                description.AttachmentStates[0].DestinationAlphaFactor = GetBlendFactor((BlendFactor)RenderStateHelper.GetDstAlphaBlendFactor(stateBlock.blendState));
                description.AttachmentStates[0].SourceColorFactor = GetBlendFactor((BlendFactor)RenderStateHelper.GetSrcColorBlendFactor(stateBlock.blendState));
                description.AttachmentStates[0].DestinationColorFactor = GetBlendFactor((BlendFactor)RenderStateHelper.GetDstColorBlendFactor(stateBlock.blendState));       
            }
        }

        public static void CreateDepthStencilStateDescription(RenderStateBlock stateBlock, out Veldrid.DepthStencilStateDescription description) {
            description = new Veldrid.DepthStencilStateDescription();
            description.DepthTestEnabled = RenderStateHelper.IsDepthTestEnabled(stateBlock.depthStencilState);
            description.DepthWriteEnabled = RenderStateHelper.IsDepthWriteEnabled(stateBlock.depthStencilState);
            description.DepthComparison = GetCompareOp((CompareOp)RenderStateHelper.GetDepthFunc(stateBlock.depthStencilState));
            if(RenderStateHelper.IsStencilTestEnabled(stateBlock.depthStencilState)) {
                description.StencilTestEnabled = true;
                description.StencilReference = RenderStateHelper.GetStencilRef(stateBlock.depthStencilState);
                description.StencilReadMask = RenderStateHelper.GetStencilFuncMask(stateBlock.depthStencilState);
                description.StencilWriteMask = RenderStateHelper.GetStencilWriteMask(stateBlock.depthStencilState);
                description.StencilFront.Comparison = GetCompareOp((CompareOp)RenderStateHelper.GetStencilFunc(stateBlock.depthStencilState));
                description.StencilFront.Pass = GetStencilOp((StencilOp)RenderStateHelper.GetStencilPassOp(stateBlock.depthStencilState));
                description.StencilFront.Fail = GetStencilOp((StencilOp)RenderStateHelper.GetStencilFailOp(stateBlock.depthStencilState));
                description.StencilFront.DepthFail = GetStencilOp((StencilOp)RenderStateHelper.GetStencilDepthFailOp(stateBlock.depthStencilState));
                if(RenderStateHelper.IsSeparateStencil(stateBlock.depthStencilState)) {
                    description.StencilBack.Comparison = GetCompareOp((CompareOp)RenderStateHelper.GetStencilFunc(stateBlock.depthStencilState));
                    description.StencilBack.Pass = GetStencilOp((StencilOp)RenderStateHelper.GetStencilPassOp(stateBlock.depthStencilState));
                    description.StencilBack.Fail = GetStencilOp((StencilOp)RenderStateHelper.GetStencilFailOp(stateBlock.depthStencilState));
                    description.StencilBack.DepthFail = GetStencilOp((StencilOp)RenderStateHelper.GetStencilDepthFailOp(stateBlock.depthStencilState));
                } else {
                    description.StencilBack = description.StencilFront;
                }
            }
        }

        public static void CreateRasterizerStateDescription(RenderStateBlock stateBlock, out Veldrid.RasterizerStateDescription description) {
            description = new Veldrid.RasterizerStateDescription();
            switch((CullMode)RenderStateHelper.GetCullMode(stateBlock.rasterizerState)) {
                case CullMode.BackSided:
                    description.CullMode = Veldrid.FaceCullMode.Back;
                    break;
                case CullMode.FrontSided:
                    description.CullMode = Veldrid.FaceCullMode.Front;
                    break;
                case CullMode.None:
                    description.CullMode = Veldrid.FaceCullMode.None;
                    break;
            }
            switch((PolygonMode)RenderStateHelper.GetPolygonMode(stateBlock.rasterizerState)) {
                case PolygonMode.Fill:
                    description.FillMode = Veldrid.PolygonFillMode.Solid;
                    break;
                case PolygonMode.Line:
                default:
                    description.FillMode = Veldrid.PolygonFillMode.Wireframe;
                    break;
            }
            description.FrontFace = (FrontFace)RenderStateHelper.GetFrontFace(stateBlock.rasterizerState) == FrontFace.CW ? Veldrid.FrontFace.Clockwise : Veldrid.FrontFace.CounterClockwise;
            description.DepthClipEnabled = RenderStateHelper.IsDepthClipEnabled(stateBlock.rasterizerState);
            description.ScissorTestEnabled = RenderStateHelper.IsScissorTestEnabled(stateBlock.rasterizerState);
        }

        Veldrid.PrimitiveTopology GetPrimitiveTopology(RenderStateBlock stateBlock) {
            switch((PrimitiveTopologyType)RenderStateHelper.GetPrimitiveTopology(stateBlock.rasterizerState)) {
                case PrimitiveTopologyType.TriangleList:
                default:
                    return Veldrid.PrimitiveTopology.TriangleList;
                case PrimitiveTopologyType.TriangleStrip:
                    return Veldrid.PrimitiveTopology.TriangleStrip;
                case PrimitiveTopologyType.LineList:
                    return Veldrid.PrimitiveTopology.LineList;
                case PrimitiveTopologyType.LineStrip:
                    return Veldrid.PrimitiveTopology.LineStrip;
                case PrimitiveTopologyType.PointList:
                    return Veldrid.PrimitiveTopology.PointList;
            }
        }

        ShaderUniform.Type ParseUniformType(Veldrid.ResourceKind kind) {
            switch(kind) {
                case Veldrid.ResourceKind.Sampler:
                    return ShaderUniform.Type.Sampler;
                case Veldrid.ResourceKind.StructuredBufferReadOnly:
                case Veldrid.ResourceKind.StructuredBufferReadWrite:
                case Veldrid.ResourceKind.TextureReadOnly:
                case Veldrid.ResourceKind.TextureReadWrite:
                    return ShaderUniform.Type.Image;
                case Veldrid.ResourceKind.UniformBuffer:
                    return ShaderUniform.Type.UniformBuffer;
                default:
                    return ShaderUniform.Type.None;
            }
        }

        void AddUniformInfos(DescriptorSetLayoutInfo layoutInfo, RenderStateInfo renderStateInfo) {
            if(layoutInfo.uniformInfos == null) {
                return;
            }

            if(renderStateInfo.uniformInfos == null) {
                renderStateInfo.uniformInfos = new Dictionary<string, ShaderUniform>();
            }

            for(int i = 0; i < layoutInfo.uniformInfos.Count; i++) {
                ShaderUniform info = new ShaderUniform();
                Veldrid.ShaderStages stages = layoutInfo.uniformInfos[i].stages;
                info.name = layoutInfo.uniformInfos[i].name;
                info.set = layoutInfo.uniformInfos[i].set;
                info.binding = layoutInfo.uniformInfos[i].binding;
                info.uniformIndex = renderStateInfo.numUniforms++;
                info.type = ParseUniformType(layoutInfo.uniformInfos[i].kind);
                if(renderStateInfo.uniformInfos.ContainsKey(info.name)) {
                    continue;
                }
                renderStateInfo.uniformInfos.Add(info.name, info);
                if(info.type == ShaderUniform.Type.UniformBuffer) {
                    DescriptorStructInfo blockInfo = layoutInfo.uniformInfos[i] as DescriptorStructInfo;
                    info.sizeInBytes = blockInfo.blockSize;
                    info.index = renderStateInfo.numUniformBlocks++;
                    for(int u = 0; u < blockInfo.memberInfos.Length; u++) {
                        ShaderUniform member = new ShaderUniform();
                        member.name = blockInfo.memberInfos[u].name;
                        member.offsetInBytes = blockInfo.memberInfos[u].offsetInBytes;
                        member.sizeInBytes = blockInfo.memberInfos[u].sizeInBytes;
                        member.set = layoutInfo.uniformInfos[i].set;
                        member.binding = layoutInfo.uniformInfos[i].binding;
                        member.index = renderStateInfo.numUniformBlocks;
                        member.type = ShaderUniform.Type.Float4;   
                        renderStateInfo.uniformInfos.Add(member.name, member);                     
                    }
                    Veldrid.ResourceLayoutElementDescription elementDescription = new Veldrid.ResourceLayoutElementDescription(info.name, Veldrid.ResourceKind.UniformBuffer, stages, Veldrid.ResourceLayoutElementOptions.DynamicBinding);
                    layoutInfo.dynamicCBufferDescriptions.Add(elementDescription);
                } else if(info.type == ShaderUniform.Type.Image) {
                    info.index = renderStateInfo.numImages++;
                } else if(info.type == ShaderUniform.Type.Sampler) {
                    info.index = renderStateInfo.numSamplers++;
                }
            }
        }

        int GetUniformBlockSize(string name, RenderStateInfo renderStateInfo) {
            foreach(var block in renderStateInfo.uniformInfos) {
                if(block.Value.type == ShaderUniform.Type.UniformBuffer && block.Key == name) {
                    return MathHelper.Align(block.Value.sizeInBytes, (int)GraphicsDevice.gd.UniformBufferMinOffsetAlignment);
                }
            }
            return 0;
        }

        GPURenderState PostParseRenderState(RenderStateInfo info) {
            Dictionary<uint, ShaderResourceLayoutInfo> resourceLayoutInfos = new Dictionary<uint, ShaderResourceLayoutInfo>();
            DescriptorSetLayoutInfo setLayoutInfo = new DescriptorSetLayoutInfo();
            Veldrid.Shader[] shaders = new Veldrid.Shader[info.stageDefines.Length];

            for(int i = 0; i < shaders.Length; i++) {
                var stageDefines = info.stageDefines;
                var result = HLSLBytecode.CompileFromText(stageDefines[i].sourceText, stageDefines[i].entryPointName, stageDefines[i].filename, stageDefines[i].stage, true, shaderInclude, stageDefines[i].macroDefinitions);
                HLSLBytecode.Parse(result.Bytecode, stageDefines[i].filename, info.renderPass, setLayoutInfo, stageDefines[i].stage);
                shaders[i] = GraphicsDevice.ResourceFactory.CreateShader(new Veldrid.ShaderDescription(stageDefines[i].stage, result.Bytecode, stageDefines[i].entryPointName, true));
            }
            Veldrid.ShaderSetDescription shaderSetDescription = new Veldrid.ShaderSetDescription(info.vertexLayoutDescriptions, shaders);

            AddUniformInfos(setLayoutInfo, info);

            GPURenderState renderState = new GPURenderState();
            renderState.renderPassResourceLayout = info.renderPass.GetPerFrameResourceLayout();
            int numResourceLayouts = renderState.renderPassResourceLayout != null ? 1 : 0;
            if(setLayoutInfo.lightmapDescriptions != null) {
                numResourceLayouts++;
                renderState.lightmapResourceLayout = GraphicsDevice.ResourceFactory.CreateResourceLayout(new Veldrid.ResourceLayoutDescription(setLayoutInfo.lightmapDescriptions));
            }
            if(setLayoutInfo.layoutElementDescriptions != null && setLayoutInfo.layoutElementDescriptions.Count > 0) {
                numResourceLayouts++;
                renderState.numMaterialResources = setLayoutInfo.layoutElementDescriptions.Count;
                renderState.materialResourceLayout = GraphicsDevice.ResourceFactory.CreateResourceLayout(new Veldrid.ResourceLayoutDescription(setLayoutInfo.layoutElementDescriptions.ToArray()));
            }
            if(setLayoutInfo.dynamicCBufferDescriptions != null && setLayoutInfo.dynamicCBufferDescriptions.Count > 0) {
                numResourceLayouts++;
                renderState.numDynamicCBuffers = setLayoutInfo.dynamicCBufferDescriptions.Count;
                renderState.dynamicCBufferResourceLayout = GraphicsDevice.ResourceFactory.CreateResourceLayout(new Veldrid.ResourceLayoutDescription(setLayoutInfo.dynamicCBufferDescriptions.ToArray()));
                Veldrid.BindableResource[] dynamicCBuffers = new Veldrid.BindableResource[setLayoutInfo.dynamicCBufferDescriptions.Count];
                int numDynamicCBuffers = 0;
                if(setLayoutInfo.renderPassDynamicCBuffers != null) {
                    setLayoutInfo.renderPassDynamicCBuffers.CopyTo(dynamicCBuffers);
                    numDynamicCBuffers += setLayoutInfo.renderPassDynamicCBuffers.Count;
                }
                for(; numDynamicCBuffers < setLayoutInfo.dynamicCBufferDescriptions.Count; numDynamicCBuffers++) {
                    dynamicCBuffers[numDynamicCBuffers] = new Veldrid.DeviceBufferRange(Common.frameGraph.dynamicUniformBuffer, 0, (uint)GetUniformBlockSize(setLayoutInfo.dynamicCBufferDescriptions[numDynamicCBuffers].Name, info));
                }
                renderState.dynamicCBufferResourceSet = GraphicsDevice.ResourceFactory.CreateResourceSet(new Veldrid.ResourceSetDescription(renderState.dynamicCBufferResourceLayout, dynamicCBuffers));
            }

            int slot = 0;
            var resourceLayouts = new Veldrid.ResourceLayout[numResourceLayouts];
            if(renderState.renderPassResourceLayout != null) {
                resourceLayouts[slot] = renderState.renderPassResourceLayout;
                slot++;
            }
            if(renderState.materialResourceLayout != null) {
                resourceLayouts[slot] = renderState.materialResourceLayout;
                slot++;
            }
            if(renderState.lightmapResourceLayout != null) {
                resourceLayouts[slot] = renderState.lightmapResourceLayout;
                slot++;
            }
            if(renderState.dynamicCBufferResourceLayout != null) {
                resourceLayouts[slot] = renderState.dynamicCBufferResourceLayout;
                slot++;
            }
            CreateRasterizerStateDescription(info.stateBlock, out Veldrid.RasterizerStateDescription rasterizerStateDescription);
            CreateDepthStencilStateDescription(info.stateBlock, out Veldrid.DepthStencilStateDescription depthStencilStateDescription);
            CreateBlendStateDescription(info.stateBlock, out Veldrid.BlendStateDescription blendStateDescription);
            renderState.pipeline = GraphicsDevice.ResourceFactory.CreateGraphicsPipeline(
                new Veldrid.GraphicsPipelineDescription(
                    blendStateDescription,
                    depthStencilStateDescription,
                    rasterizerStateDescription,
                    GetPrimitiveTopology(info.stateBlock),
                    shaderSetDescription,
                    resourceLayouts,
                    info.renderPass.framebuffer.OutputDescription,
                    Veldrid.ResourceBindingModel.Improved)
            );
            return renderState;
        }

        internal override void Parse() {
            Token token = new Token();
            Lexer lex = new Lexer(text);

            lex.ExpectTokenString("shader");
            lex.ExpectTokenTypeOnLine(TokenType.String, TokenSubType.None, ref token);
            name = token.lexme;

            List<RenderStateInfo> newRenderStateInfos = new List<RenderStateInfo>();
            lex.ExpectTokenString("{");
            while(lex.ReadToken(ref token)) {
                if(token == "}") {
                    break;
                }
                
                if(token == "Queue") {
                    lex.ReadTokenOnLine(ref token);
                    renderQueue = (RenderQueue)Enum.Parse(typeof(RenderQueue), token.lexme);
                    continue;
                }

                if(token == "RenderState") {
                    ParseRenderState(lex, ref token, ref newRenderStateInfos);
                    continue;
                }
            }

            renderStateInfos = newRenderStateInfos.ToArray();
            gpuRenderStates = new GPURenderState[renderStateInfos.Length];
            for(int i = 0; i < gpuRenderStates.Length; i++) {
                gpuRenderStates[i] = PostParseRenderState(renderStateInfos[i]);
            }
        }
    }
}