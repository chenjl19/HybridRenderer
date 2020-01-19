using System;
using System.IO;
using System.Collections.Generic;

namespace NT
{
        public class ComputeShader : Decl {
        internal class ComputeKernel : IDisposable {
            public string entryPoint;
            public uint[] groupSize;
            public Veldrid.Pipeline pipeline;
            public Veldrid.ResourceLayout resourceLayout;
            public Veldrid.ResourceSet resourceSet;
            public Veldrid.BindableResource[] bindableResources;
            public DescriptorSetLayoutInfo setLayoutInfo;
            bool dirty;

            public ComputeKernel() {
                dirty = true;
            }

            public void Dispose() {
                entryPoint = string.Empty;
                groupSize = null;
                pipeline?.Dispose();
                resourceLayout?.Dispose();
                resourceSet?.Dispose();
                bindableResources = null;
            }

            public void BindResource(int slot, Veldrid.BindableResource resource) {
                bindableResources[slot] = resource;
                dirty = true;
            }

            public void Dispatch(Veldrid.CommandList commandList, uint[] dynamicOffsets, uint width, uint height, uint depth) {
                if(dirty) {
                    resourceSet?.Dispose();
                    resourceSet = GraphicsDevice.ResourceFactory.CreateResourceSet(new Veldrid.ResourceSetDescription(resourceLayout, bindableResources));
                    dirty = false;
                }
                commandList.SetPipeline(pipeline);
                commandList.SetComputeResourceSet(0, resourceSet, dynamicOffsets);
                commandList.Dispatch(
                    (uint)Math.Max(1, MathF.Ceiling((float)width / groupSize[0])), 
                    (uint)Math.Max(1, MathF.Ceiling((float)height / groupSize[1])), 
                    (uint)Math.Max(1, MathF.Ceiling((float)depth / groupSize[2]))
                );
            }
        }

        string sourceFileName;
        ComputeKernel[] kernels;

        void ParseKernels() {
            if(string.IsNullOrEmpty(sourceFileName)) {
                throw new InvalidDataException();
            }
            AssetLoader.CreateShaders(FileSystem.CreateOSPath(Path.Combine("shaders", sourceFileName)), (bytes) => {
                for(int i = 0; i < kernels.Length; i++) {
                    var result = HLSLBytecode.CompileFromText(bytes, kernels[i].entryPoint, name, Veldrid.ShaderStages.Compute, true, Shader.shaderInclude, null);
                    HLSLBytecode.ParseComputeShader(result, name, out kernels[i].setLayoutInfo, out int[] groupSize);
                    DescriptorSetLayoutInfo setLayoutInfo = kernels[i].setLayoutInfo;
                    int numLayoutElements = setLayoutInfo.dynamicCBufferDescriptions.Count + setLayoutInfo.layoutElementDescriptions.Count;
                    Veldrid.ResourceLayoutElementDescription[] descs = new Veldrid.ResourceLayoutElementDescription[numLayoutElements];
                    setLayoutInfo.dynamicCBufferDescriptions.CopyTo(descs);
                    setLayoutInfo.layoutElementDescriptions.CopyTo(descs, setLayoutInfo.dynamicCBufferDescriptions.Count);
                    kernels[i].resourceLayout = GraphicsDevice.ResourceFactory.CreateResourceLayout(new Veldrid.ResourceLayoutDescription(descs));
                    kernels[i].bindableResources = new Veldrid.BindableResource[numLayoutElements];
                    kernels[i].groupSize = new uint[3];
                    kernels[i].groupSize[0] = (uint)groupSize[0];
                    kernels[i].groupSize[1] = (uint)groupSize[1];
                    kernels[i].groupSize[2] = (uint)groupSize[2];
                    Veldrid.Shader shader = GraphicsDevice.ResourceFactory.CreateShader(new Veldrid.ShaderDescription(Veldrid.ShaderStages.Compute, result, kernels[i].entryPoint));
                    kernels[i].pipeline = GraphicsDevice.ResourceFactory.CreateComputePipeline(new Veldrid.ComputePipelineDescription(
                        shader,
                        new Veldrid.ResourceLayout[] {kernels[i].resourceLayout},
                        kernels[i].groupSize[0],
                        kernels[i].groupSize[1],
                        kernels[i].groupSize[2]
                    ));
                }
            });
        }

        public int FindKernel(string name) {
            if(kernels != null) {
                for(int i = 0; i < kernels.Length; i++) {
                    if(kernels[i].entryPoint == name) {
                        if(kernels[i].pipeline == null) {
                            ParseKernels();
                            if(kernels[i].pipeline == null) {
                                return -1;
                            }
                        }
                        return i;
                    }
                }
            }
            return -1;
        }

        public void SetConstantBuffer(int index, int slot, Veldrid.DeviceBufferRange buffer) {
            kernels[index].BindResource(slot, buffer);
        }

        public void SetConstantBuffer(int index, int slot, Veldrid.DeviceBuffer buffer) {
            kernels[index].BindResource(slot, buffer);
        }

        public void SetSampler(int index, int slot, Veldrid.Sampler sampler) {
            kernels[index].BindResource(slot, sampler);
        }

        public void SetTexture(int index, int slot, Veldrid.Texture texture) {
            kernels[index].BindResource(slot, texture);
        }

        public void SetTexture(int index, int slot, Veldrid.TextureView texture) {
            kernels[index].BindResource(slot, texture);
        }

        public void SetBuffer(int index, int slot, Veldrid.DeviceBuffer buffer) {
            kernels[index].BindResource(slot, buffer);
        }

        public void Dispatch(Veldrid.CommandList commandList, int index, uint[] dynamicOffsets, uint width, uint height, uint depth) {
            kernels[index].Dispatch(commandList, dynamicOffsets, width, height, depth);
        }

        public override void Dispose() {
            if(kernels != null) {
                foreach(var kernel in kernels) {
                    kernel.Dispose();
                }
                kernels = null;
            }
        }

        internal override void Parse() {
            if(text == null) {
                return;
            }

            Token token = new Token();
            Lexer lex = new Lexer(text, Lexer.Flags.NoStringConcat);

            List<string> kernelNames = new List<string>();
            lex.ExpectTokenString("compute");
            lex.ExpectTokenTypeOnLine(TokenType.String, TokenSubType.None, ref token);
            this.name = token.lexme;
            lex.ExpectTokenString("{");
            while(lex.ReadToken(ref token)) {
                if(token.lexme == "}") {
                    break;
                }
                if(token == "source") {
                    lex.ExpectTokenTypeOnLine(TokenType.String, TokenSubType.None, ref token);
                    sourceFileName = token.lexme;
                    continue;
                }
                if(token == "kernel") {
                    lex.ExpectTokenTypeOnLine(TokenType.String, TokenSubType.None, ref token);
                    kernelNames.Add(token.lexme);
                    continue;
                }
            }

            if(kernelNames.Count > 0) {
                kernels = new ComputeKernel[kernelNames.Count];
                for(int i = 0; i < kernels.Length; i++) {
                    kernels[i] = new ComputeKernel();
                    kernels[i].entryPoint = kernelNames[i];
                }
            }
        }
    }
}