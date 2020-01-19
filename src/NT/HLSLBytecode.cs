using System;
using SharpDX.D3DCompiler;

namespace NT
{
    internal static class HLSLBytecode {
        public static string GetProfile(Veldrid.ShaderStages stage) {
            switch (stage) {
                case Veldrid.ShaderStages.Vertex:
                    return "vs_5_0";
                case Veldrid.ShaderStages.Fragment:
                    return "ps_5_0";
                case Veldrid.ShaderStages.Compute:
                    return "cs_5_0";
                default:
                    return "";
            }
        }

        public static SharpDX.Direct3D.ShaderMacro GetStageMacroDefine(Veldrid.ShaderStages stage) {
            switch (stage) {
                case Veldrid.ShaderStages.Vertex:
                    return new SharpDX.Direct3D.ShaderMacro("__VS__", 1);
                case Veldrid.ShaderStages.Fragment:
                    return new SharpDX.Direct3D.ShaderMacro("__PS__", 1);
                case Veldrid.ShaderStages.Compute:
                    return new SharpDX.Direct3D.ShaderMacro("__CS__", 1);
                default:
                    return new SharpDX.Direct3D.ShaderMacro();
            }
        }

        public static CompilationResult CompileFromText(string text, string entryPointName, string shaderName, Veldrid.ShaderStages stage, bool debug, Include includer, SharpDX.Direct3D.ShaderMacro[] defines) {
            ShaderFlags shaderFlags = ShaderFlags.None;
            if(debug) {
                shaderFlags |= ShaderFlags.Debug | ShaderFlags.SkipOptimization;
            }
            if(defines == null) {
                defines = new SharpDX.Direct3D.ShaderMacro[] {GetStageMacroDefine(stage)};
            } else {
                var tmp = new SharpDX.Direct3D.ShaderMacro[defines.Length + 1];
                defines.CopyTo(tmp, 0);
                tmp[defines.Length] = GetStageMacroDefine(stage);
                defines = tmp;
            }
            return ShaderBytecode.Compile(text, entryPointName, GetProfile(stage), shaderFlags, EffectFlags.None, defines, includer, shaderName);
        }

        public static CompilationResult CompileFromText(byte[] text, string entryPointName, string shaderName, Veldrid.ShaderStages stage, bool debug, Include includer, SharpDX.Direct3D.ShaderMacro[] defines) {
            ShaderFlags shaderFlags = ShaderFlags.None;
            if(debug) {
                shaderFlags |= ShaderFlags.Debug | ShaderFlags.SkipOptimization;
            }
            if(defines == null) {
                defines = new SharpDX.Direct3D.ShaderMacro[] {GetStageMacroDefine(stage)};
            } else {
                var tmp = new SharpDX.Direct3D.ShaderMacro[defines.Length + 1];
                defines.CopyTo(tmp, 0);
                tmp[defines.Length] = GetStageMacroDefine(stage);
                defines = tmp;
            }
            return ShaderBytecode.Compile(text, entryPointName, GetProfile(stage), shaderFlags, EffectFlags.None, defines, includer, shaderName);
        }

        public static CompilationResult CompileFromFile(string filename, Veldrid.ShaderStages stage, bool debug, Include includer, SharpDX.Direct3D.ShaderMacro[] defines) {
            ShaderFlags shaderFlags = ShaderFlags.None;
            if(debug) {
                shaderFlags |= ShaderFlags.Debug | ShaderFlags.SkipOptimization;
            }
            if(defines == null) {
                defines = new SharpDX.Direct3D.ShaderMacro[] {GetStageMacroDefine(stage)};
            } else {
                var tmp = new SharpDX.Direct3D.ShaderMacro[defines.Length + 1];
                defines.CopyTo(tmp, 0);
                tmp[defines.Length] = GetStageMacroDefine(stage);
                defines = tmp;
            }
            return ShaderBytecode.CompileFromFile(filename, GetProfile(stage), shaderFlags, EffectFlags.None, defines, includer);
        }

        public static void ParseComputeShader(byte[] bytecode, string shaderName, out DescriptorSetLayoutInfo setLayoutInfo, out int[] groupSize) {
            Veldrid.ShaderStages stages = Veldrid.ShaderStages.Compute;
            setLayoutInfo = new DescriptorSetLayoutInfo(); 
                       
            Console.WriteLine($"ShaderReflection:{shaderName} stage:{stages}");
        
            ShaderReflection reflection = new ShaderReflection(bytecode);
            groupSize = new int[3];
            reflection.GetThreadGroupSize(out groupSize[0], out groupSize[1], out groupSize[2]);
            uint numCBuffers = 0;
            uint numImages = 0;
            uint numSamplers = 0;
            for(int i = 0; i < reflection.Description.ConstantBuffers; i++) {
                var cbuffer = reflection.GetConstantBuffer(i);
                if(cbuffer.Description.Type != ConstantBufferType.ConstantBuffer) {
                    continue;
                }
                if(setLayoutInfo.FindUniform(cbuffer.Description.Name, out int found)) {
                    setLayoutInfo.uniformInfos[found].stages |= stages;
                    continue;
                }
                setLayoutInfo.dynamicCBufferDescriptions.Add(new Veldrid.ResourceLayoutElementDescription(cbuffer.Description.Name, Veldrid.ResourceKind.UniformBuffer, stages, Veldrid.ResourceLayoutElementOptions.DynamicBinding));
                DescriptorStructInfo cbufferStructInfo = new DescriptorStructInfo();
                cbufferStructInfo.name = cbuffer.Description.Name;
                cbufferStructInfo.set = 0;
                cbufferStructInfo.binding = numCBuffers;
                cbufferStructInfo.index = numCBuffers;
                cbufferStructInfo.kind = Veldrid.ResourceKind.UniformBuffer;
                cbufferStructInfo.blockSize = cbuffer.Description.Size;
                cbufferStructInfo.stages = stages;
                cbufferStructInfo.memberInfos = new SpvMemberInfo[cbuffer.Description.VariableCount];
                for(int v = 0; v < cbuffer.Description.VariableCount; v++) {
                    var varDesc = cbuffer.GetVariable(v);
                    cbufferStructInfo.memberInfos[v] = new SpvMemberInfo();
                    cbufferStructInfo.memberInfos[v].name = varDesc.Description.Name;
                    cbufferStructInfo.memberInfos[v].offsetInBytes = varDesc.Description.StartOffset;
                    cbufferStructInfo.memberInfos[v].sizeInBytes = varDesc.Description.Size;
                }

                setLayoutInfo.uniformInfos.Add(cbufferStructInfo);
                numCBuffers++;
            }  
            for(int i = 0; i < reflection.Description.BoundResources; i++) {
                var bindingDesc = reflection.GetResourceBindingDescription(i);
                if(bindingDesc.Type == ShaderInputType.ConstantBuffer) {
                    continue;
                }
                if(setLayoutInfo.FindUniform(bindingDesc.Name, out int found)) {
                    setLayoutInfo.uniformInfos[found].stages |= stages;
                    continue;
                }

                switch(bindingDesc.Type) {
                    case ShaderInputType.Sampler: {
                        SpvUniformInfo sampler = new SpvUniformInfo();
                        sampler.name = bindingDesc.Name;
                        sampler.set = 0;
                        sampler.binding = (uint)bindingDesc.BindPoint;
                        sampler.index = numSamplers++;
                        sampler.kind = Veldrid.ResourceKind.Sampler;
                        sampler.stages = stages;
                        setLayoutInfo.uniformInfos.Add(sampler);
                        Veldrid.ResourceLayoutElementDescription elementDescription = new Veldrid.ResourceLayoutElementDescription(bindingDesc.Name, Veldrid.ResourceKind.Sampler, stages);
                        setLayoutInfo.layoutElementDescriptions.Add(elementDescription);
                        break;
                    }
                    case ShaderInputType.Structured:
                    case ShaderInputType.ByteAddress:
                    case ShaderInputType.TextureBuffer: {
                        SpvUniformInfo texture = new SpvUniformInfo();
                        texture.name = bindingDesc.Name;
                        texture.set = 0;
                        texture.binding = (uint)bindingDesc.BindPoint;
                        texture.index = numImages++;
                        texture.kind = Veldrid.ResourceKind.StructuredBufferReadOnly;
                        texture.stages = stages;
                        setLayoutInfo.uniformInfos.Add(texture);      
                        Veldrid.ResourceLayoutElementDescription elementDescription = new Veldrid.ResourceLayoutElementDescription(bindingDesc.Name, Veldrid.ResourceKind.StructuredBufferReadOnly, stages);
                        setLayoutInfo.layoutElementDescriptions.Add(elementDescription);                            
                        break;
                    }
                    case ShaderInputType.Texture: {
                        SpvUniformInfo texture = new SpvUniformInfo();
                        texture.name = bindingDesc.Name;
                        texture.set = 0;
                        texture.binding = (uint)bindingDesc.BindPoint;
                        texture.index = numImages++;
                        texture.kind = Veldrid.ResourceKind.TextureReadOnly;
                        texture.stages = stages;
                        setLayoutInfo.uniformInfos.Add(texture);      
                        Veldrid.ResourceLayoutElementDescription elementDescription = new Veldrid.ResourceLayoutElementDescription(bindingDesc.Name, Veldrid.ResourceKind.TextureReadOnly, stages);
                        setLayoutInfo.layoutElementDescriptions.Add(elementDescription);                  
                        break;
                    }
                    case ShaderInputType.UnorderedAccessViewRWByteAddress:
                    case ShaderInputType.UnorderedAccessViewRWTyped: {
                        SpvUniformInfo texture = new SpvUniformInfo();
                        texture.name = bindingDesc.Name;
                        texture.set = 0;
                        texture.binding = (uint)bindingDesc.BindPoint;
                        texture.index = numImages++;
                        texture.kind = Veldrid.ResourceKind.StructuredBufferReadWrite;
                        texture.stages = stages;
                        setLayoutInfo.uniformInfos.Add(texture);      
                        Veldrid.ResourceLayoutElementDescription elementDescription = new Veldrid.ResourceLayoutElementDescription(bindingDesc.Name, Veldrid.ResourceKind.TextureReadWrite, stages);
                        setLayoutInfo.layoutElementDescriptions.Add(elementDescription);  
                        break;                           
                    }
                }
            }                      
        }

        public static void Parse(byte[] bytecode, string shaderName, RenderPass renderPass, DescriptorSetLayoutInfo setLayoutInfo, Veldrid.ShaderStages stages) {
            Console.WriteLine($"ShaderReflection:{shaderName} stage:{stages} renderPass:{renderPass.name}");
            
            ShaderReflection reflection = new ShaderReflection(bytecode);
            uint numCBuffers = 0;
            uint numImages = 0;
            uint numSamplers = 0;
            for(int i = 0; i < reflection.Description.ConstantBuffers; i++) {
                var cbuffer = reflection.GetConstantBuffer(i);
                if(cbuffer.Description.Type != ConstantBufferType.ConstantBuffer) {
                    continue;
                }
                if(renderPass.FindCommonResource(cbuffer.Description.Name, out RenderPass.CommonResourceInfo info)) {
                    if(info.hightFrequency == false) {
                        var kind = Veldrid.ResourceKind.UniformBuffer;
                        var desc = new Veldrid.ResourceLayoutElementDescription(cbuffer.Description.Name, kind, info.stages, info.options);
                        setLayoutInfo.dynamicCBufferDescriptions.Add(desc);
                        setLayoutInfo.renderPassDynamicCBuffers.Add(info.resource);
                    }
                    numCBuffers++;
                    continue;
                }
                if(setLayoutInfo.FindUniform(cbuffer.Description.Name, out int found)) {
                    setLayoutInfo.uniformInfos[found].stages |= stages;
                    continue;
                }
                
                DescriptorStructInfo cbufferStructInfo = new DescriptorStructInfo();
                cbufferStructInfo.name = cbuffer.Description.Name;
                cbufferStructInfo.set = 0;
                cbufferStructInfo.binding = numCBuffers;
                cbufferStructInfo.index = numCBuffers;
                cbufferStructInfo.kind = Veldrid.ResourceKind.UniformBuffer;
                cbufferStructInfo.blockSize = cbuffer.Description.Size;
                cbufferStructInfo.stages = stages;
                cbufferStructInfo.memberInfos = new SpvMemberInfo[cbuffer.Description.VariableCount];
                for(int v = 0; v < cbuffer.Description.VariableCount; v++) {
                    var varDesc = cbuffer.GetVariable(v);
                    cbufferStructInfo.memberInfos[v] = new SpvMemberInfo();
                    cbufferStructInfo.memberInfos[v].name = varDesc.Description.Name;
                    cbufferStructInfo.memberInfos[v].offsetInBytes = varDesc.Description.StartOffset;
                    cbufferStructInfo.memberInfos[v].sizeInBytes = varDesc.Description.Size;
                }

                setLayoutInfo.uniformInfos.Add(cbufferStructInfo);
                numCBuffers++;
            } 
            string lightmapDirMap = string.Empty;
            string lightmapColorMap = string.Empty;
            for(int i = 0; i < reflection.Description.BoundResources; i++) {
                var bindingDesc = reflection.GetResourceBindingDescription(i);
                if(bindingDesc.Type == ShaderInputType.ConstantBuffer) {
                    continue;
                }
                if(renderPass.FindCommonResource(bindingDesc.Name) != null) {
                    continue;
                }
                if(bindingDesc.Name == "_LightmapDirTex") {
                    lightmapDirMap = bindingDesc.Name;
                    continue;
                }
                if(bindingDesc.Name == "_LightmapColorTex") {
                    lightmapColorMap = bindingDesc.Name;
                    continue;
                }
                if(setLayoutInfo.FindUniform(bindingDesc.Name, out int found)) {
                    setLayoutInfo.uniformInfos[found].stages |= stages;
                    continue;
                }

                switch(bindingDesc.Type) {
                    case ShaderInputType.Sampler: {
                        SpvUniformInfo sampler = new SpvUniformInfo();
                        sampler.name = bindingDesc.Name;
                        sampler.set = 0;
                        sampler.binding = (uint)bindingDesc.BindPoint;
                        sampler.index = numSamplers++;
                        sampler.kind = Veldrid.ResourceKind.Sampler;
                        sampler.stages = stages;
                        setLayoutInfo.uniformInfos.Add(sampler);
                        Veldrid.ResourceLayoutElementDescription elementDescription = new Veldrid.ResourceLayoutElementDescription(bindingDesc.Name, Veldrid.ResourceKind.Sampler, stages);
                        setLayoutInfo.layoutElementDescriptions.Add(elementDescription);
                        break;
                    }
                    case ShaderInputType.Structured:
                    case ShaderInputType.ByteAddress:
                    case ShaderInputType.TextureBuffer: {
                        SpvUniformInfo texture = new SpvUniformInfo();
                        texture.name = bindingDesc.Name;
                        texture.set = 0;
                        texture.binding = (uint)bindingDesc.BindPoint;
                        texture.index = numImages++;
                        texture.kind = Veldrid.ResourceKind.StructuredBufferReadOnly;
                        texture.stages = stages;
                        setLayoutInfo.uniformInfos.Add(texture);      
                        Veldrid.ResourceLayoutElementDescription elementDescription = new Veldrid.ResourceLayoutElementDescription(bindingDesc.Name, Veldrid.ResourceKind.StructuredBufferReadOnly, stages);
                        setLayoutInfo.layoutElementDescriptions.Add(elementDescription);                            
                        break;
                    }
                    case ShaderInputType.Texture: {
                        SpvUniformInfo texture = new SpvUniformInfo();
                        texture.name = bindingDesc.Name;
                        texture.set = 0;
                        texture.binding = (uint)bindingDesc.BindPoint;
                        texture.index = numImages++;
                        texture.kind = Veldrid.ResourceKind.TextureReadOnly;
                        texture.stages = stages;
                        setLayoutInfo.uniformInfos.Add(texture);      
                        Veldrid.ResourceLayoutElementDescription elementDescription = new Veldrid.ResourceLayoutElementDescription(bindingDesc.Name, Veldrid.ResourceKind.TextureReadOnly, stages);
                        setLayoutInfo.layoutElementDescriptions.Add(elementDescription);                  
                        break;
                    }
                }
            } 

            if(!string.IsNullOrEmpty(lightmapDirMap) && !string.IsNullOrEmpty(lightmapColorMap)) {
                setLayoutInfo.lightmapDescriptions = new Veldrid.ResourceLayoutElementDescription[2];
                setLayoutInfo.lightmapDescriptions[0] = new Veldrid.ResourceLayoutElementDescription(lightmapDirMap, Veldrid.ResourceKind.TextureReadOnly, Veldrid.ShaderStages.Fragment);
                setLayoutInfo.lightmapDescriptions[1] = new Veldrid.ResourceLayoutElementDescription(lightmapColorMap, Veldrid.ResourceKind.TextureReadOnly, Veldrid.ShaderStages.Fragment);
            }         
        }
    }
}