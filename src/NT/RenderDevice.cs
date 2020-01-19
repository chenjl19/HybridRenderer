using System;
using System.Collections.Generic;

namespace NT
{
    public enum CompareOp {
        LEqual,
        Equal,
        GEqual,
        Less,
        Greater,
        NotEqual,
        Never,
        Always
    };

    public enum StencilOp {
        Zero,
        Keep,
        Replace,
        Increment,
        IncrementAndClamp,
        Decrement,
        DecrementAndClamp,
        Invert
    };

    public enum PolygonMode {
        Fill,
        Line,
        Point
    };

    public enum PrimitiveTopologyType {
        TriangleList,
        TriangleStrip,
        TriangleFan,
        PointList,
        LineList,
        LineStrip
    };

    public enum FrontFace {
        CW,
        CCW
    };

    public enum CullMode {
        None,
        BackSided,
        FrontSided,
    };

    public enum BlendFactor {
        Zero,
        One,
        SrcColor,
        OneMinusSrcColor,
        DstColor,
        OneMinusDstColor,
        SrcAlpha,
        OneMinusSrcAlpha,
        DstAlpha,
        OneMinusDstAlpha,
        SrcAlphaSaturate
    };

    public enum BlendOp {
        Add,
        Sub,
        Min,
        Max
    };  

    public enum DepthWriteMaskFlags {
        Zero,
        All
    }

    public struct RenderStateBlock {
        public Int32 rasterizerState;
        public Int64 depthStencilState;
        public Int64 blendState;
    }

    public static class RenderStateHelper {
        public const int PRIMITIVE_TOPOLOGY_SHIFT = 0;
        public const int PRIMITIVE_TOPOLOGY_MASK = 7;
        public const int DEPTH_BIAS_MASK = 1 << 3;
        public const int DEPTH_CLIP_MASK = 1 << 4;
        public const int FRONT_FACE_SHIFT = 5;
        public const int FRONT_FACE_MASK = 1;
        public const int POLYGON_MODE_SHIFT = 6;
        public const int POLYGON_MODE_MASK = 3;
        public const int CULL_MODE_SHIFT = 8;
        public const int CULL_MODE_MASK = 3;
        public const int SCISSOR_MASK = 1 << 10;
        public const int MULTISAMPLE_MASK = 1 << 11;
        public const int ANTIALIASED_LINE_MASK = 1 << 12;
        public const int DEPTH_BOUNDS_TEST_MASK = 1 << 13;

        public const Int64 DEPTH_MASK = 1L << 0;
        public const Int32 DEPTH_FUNC_SHIFT = 2;
        public const Int64 DEPTH_FUNC_MASK = 7;
        public const Int32 STENCIL_FUNC_SHIFT = 5;
        public const Int64 STENCIL_FUNC_MASK = 7;
        public const Int32 STENCIL_REF_SHIFT = 8;
        public const Int64 STENCIL_REF_MASK = 0xFF;
        public const Int32 STENCIL_FUNC_MASK_SHIFT = 16;
        public const Int64 STENCIL_FUNC_MASK_MASK = 0xFF;
        public const Int64 STENCIL_TEST_MASK = 1L << 24;
        public const Int32 STENCIL_OP_FAIL_SHIFT = 25;
        public const Int64 STENCIL_OP_FAIL_MASK = 7;
        public const Int32 STENCIL_OP_DEPTH_FAIL_SHIFT = 28;
        public const Int64 STENCIL_OP_DEPTH_FAIL_MASK = 7;
        public const Int32 STENCIL_OP_PASS_SHIFT = 31;
        public const Int64 STENCIL_OP_PASS_MASK = 7;
        public const Int64 DEPTH_WRITE_MASK = 1L << 34;
        public const Int32 STENCIL_BACK_OP_FAIL_SHIFT = 35;
        public const Int64 STENCIL_BACK_OP_FAIL_MASK = 7;
        public const Int32 STENCIL_BACK_OP_DEPTH_FAIL_SHIFT = 38;
        public const Int64 STENCIL_BACK_OP_DEPTH_FAIL_MASK = 7;
        public const Int32 STENCIL_BACK_OP_PASS_SHIFT = 41;
        public const Int64 STENCIL_BACK_OP_PASS_MASK = 7;
        public const Int64 SEPARATE_STENCIL = 1L << 44;
        public const Int32 STENCIL_WRITE_MASK_SHIFT = 45;
        public const Int64 STENCIL_WRITE_MASK_MASK = 0xFF;
        public const Int32 STENCIL_BACK_FUNC_SHIFT = 53;
        public const Int64 STENCIL_BACK_FUNC_MASK = 7;

        public const Int32 SRC_BLEND_FACTOR_SHIFT = 0;
        public const Int64 SRC_BLEND_FACTOR_MASK = 0xF;
        public const Int32 DST_BLEND_FACTOR_SHIFT = 4;
        public const Int64 DST_BLEND_FACTOR_MASK = 0xF;
        public const Int32 BLEND_OP_SHIFT = 8;
        public const Int64 BLEND_OP_MASK = 3;
        public const Int64 RED_MASK = 1L << 10;
        public const Int64 GREEN_MASK = 1L << 11;
        public const Int64 BLUE_MASK = 1L << 12;
        public const Int64 ALPHA_MASK = 1L << 13;
        public const Int64 COLOR_MASK = RED_MASK | GREEN_MASK | BLUE_MASK;
        public const Int32 SRC_COLOR_BLEND_FACTOR_SHIFT = 14;
        public const Int64 SRC_COLOR_BLEND_FACTOR_MASK = 0xF;
        public const Int32 DST_COLOR_BLEND_FACTOR_SHIFT = 18;
        public const Int64 DST_COLOR_BLEND_FACTOR_MASK = 0xF;
        public const Int32 SRC_ALPHA_BLEND_FACTOR_SHIFT = 22;
        public const Int64 SRC_ALPHA_BLEND_FACTOR_MASK = 0xF;
        public const Int32 DST_ALPHA_BLEND_FACTOR_SHIFT = 26;
        public const Int64 DST_ALPHA_BLEND_FACTOR_MASK = 0xF;
        public const Int32 COLOR_BLEND_OP_SHIFT = 30;
        public const Int64 COLOR_BLEND_OP_MASK = 3;
        public const Int32 ALPHA_BLEND_OP_SHIFT = 32;
        public const Int64 ALPHA_BLEND_OP_MASK = 3;
        public const Int64 ALPHA_TO_COVERAGE_MASK = 1L << 33;

        public static void SetDefault(ref RenderStateBlock state)  {
            state.blendState = 0;
            state.depthStencilState = 0;
            state.rasterizerState = 0;
            EnableDepthWrite(ref state);
            EnableDepthClip(ref state);
            EnableScissorTest(ref state);
            SetDepthFunc(ref state, CompareOp.LEqual);
            SetCullMode(ref state, CullMode.BackSided);
            SetColorWriteMask(ref state, RED_MASK | GREEN_MASK | BLUE_MASK | ALPHA_MASK);
            SetBlendFactor(ref state, BlendFactor.One, BlendFactor.Zero);
            SetBlendOp(ref state, BlendOp.Add);
            SetPrimitiveTopology(ref state, PrimitiveTopologyType.TriangleList);
        }

        public static void EnableAntialiasedLine(ref RenderStateBlock state) {state.rasterizerState |= ANTIALIASED_LINE_MASK;}
        public static void EnableMultisample(ref RenderStateBlock state) {state.rasterizerState |= MULTISAMPLE_MASK;}
        public static void EnableScissorTest(ref RenderStateBlock state) {state.rasterizerState |= SCISSOR_MASK;}
        public static void EnableDepthBoundsTest(ref RenderStateBlock state) { state.rasterizerState |= DEPTH_BOUNDS_TEST_MASK; }
        public static void EnableDepthBias(ref RenderStateBlock state) { state.rasterizerState |= DEPTH_BIAS_MASK; }
        public static void EnableDepthClip(ref RenderStateBlock state) { state.rasterizerState |= DEPTH_CLIP_MASK; }
        public static void DisableAntialiasedLine(ref RenderStateBlock state) {state.rasterizerState &= ~ANTIALIASED_LINE_MASK;}
        public static void DisableMultisample(ref RenderStateBlock state) {state.rasterizerState &= ~MULTISAMPLE_MASK;}
        public static void DisableScissorTest(ref RenderStateBlock state) {state.rasterizerState &= ~SCISSOR_MASK;}
        public static void DisableDepthBoundsTest(ref RenderStateBlock state) { state.rasterizerState &= ~DEPTH_BOUNDS_TEST_MASK; }
        public static void DisableDepthBias(ref RenderStateBlock state) { state.rasterizerState &= ~DEPTH_BIAS_MASK; }
        public static void DisableDepthClip(ref RenderStateBlock state) { state.rasterizerState &= ~DEPTH_CLIP_MASK; }

        public static void EnableDepthWrite(ref RenderStateBlock state) { SetDepthWriteMask(ref state, DepthWriteMaskFlags.All); }
        public static void EnableStencilTest(ref RenderStateBlock state) { state.depthStencilState |= STENCIL_TEST_MASK; }
        public static void DisableDepthWrite(ref RenderStateBlock state) { SetDepthWriteMask(ref state, DepthWriteMaskFlags.Zero); }
        public static void DisableDepthTest(ref RenderStateBlock state) { SetDepthFunc(ref state, CompareOp.Always); }
        public static void DisableStencilTest(ref RenderStateBlock state) { state.depthStencilState &= ~STENCIL_TEST_MASK; }
        public static void SetDepthWrite(ref RenderStateBlock state, bool value) {
            if(value) {
                EnableDepthWrite(ref state);
            } else {
                DisableDepthWrite(ref state);
            }
        }

        public static bool IsAntialiasedLineEnabled(int state) {return (state & ANTIALIASED_LINE_MASK) != 0;}
        public static bool IsMultisampleEnabled(int state) {return (state & MULTISAMPLE_MASK) != 0;}
        public static bool IsScissorTestEnabled(int state) {return (state & SCISSOR_MASK) != 0;}
        public static bool IsDepthBoundsTestEnabled(int state) { return (state & DEPTH_BOUNDS_TEST_MASK) != 0; }
        public static bool IsDepthBiasEnabled(int state) { return (state & DEPTH_BIAS_MASK) != 0; }
        public static bool IsDepthClipEnabled(int state) { return (state & DEPTH_CLIP_MASK) != 0; }

        public static bool IsDepthTestEnabled(Int64 state) {return (CompareOp)GetDepthFunc(state) != CompareOp.Always;}
        public static bool IsDepthWriteEnabled(Int64 state) { return (state & DEPTH_WRITE_MASK) != 0; }
        public static bool IsStencilTestEnabled(Int64 state) { return (state & STENCIL_TEST_MASK) != 0; }
        public static bool IsSeparateStencil(Int64 state) { return (state & SEPARATE_STENCIL) != 0; }

        public static bool IsBlendEnabled(Int64 state) {
            BlendFactor src = (BlendFactor)GetSrcColorBlendFactor(state);
            BlendFactor dst = (BlendFactor)GetDstColorBlendFactor(state);
            BlendOp op = (BlendOp)GetColorBlendOp(state);
            if(!(op == BlendOp.Add && src == BlendFactor.One && dst == BlendFactor.Zero)) {
                return true;
            }
            src = (BlendFactor)GetSrcAlphaBlendFactor(state);
            dst = (BlendFactor)GetDstAlphaBlendFactor(state);
            op = (BlendOp)GetAlphaBlendOp(state);
            return !(op == BlendOp.Add && src == BlendFactor.One && dst == BlendFactor.Zero);
        }

        public static void EnalbeAlphaToCoverage(ref RenderStateBlock state) {
            state.blendState |= ALPHA_TO_COVERAGE_MASK;
        }

        public static bool IsAlphaToCoverageEnabled(Int64 state) {
            return (state & ALPHA_TO_COVERAGE_MASK) != 0;
        }

        public static bool IsRedMask(Int64 state) { return (state & RED_MASK) != 0; }
        public static bool IsGreenMask(Int64 state) { return (state & GREEN_MASK) != 0; }
        public static bool IsBlueMask(Int64 state) { return (state & BLUE_MASK) != 0; }
        public static bool IsAlphaMask(Int64 state) { return (state & ALPHA_MASK) != 0; }

        public static void SetPrimitiveTopology(ref RenderStateBlock state, PrimitiveTopologyType type) {
            state.rasterizerState &= ~(PRIMITIVE_TOPOLOGY_MASK << PRIMITIVE_TOPOLOGY_SHIFT);
            state.rasterizerState |= (Int32)type << PRIMITIVE_TOPOLOGY_SHIFT;
        }

        public static Int32 GetPrimitiveTopology(int state) {
            return ((state >> PRIMITIVE_TOPOLOGY_SHIFT) & PRIMITIVE_TOPOLOGY_MASK);
        }

        public static void SetFrontFace(ref RenderStateBlock state, FrontFace type) {
            state.rasterizerState &= ~(FRONT_FACE_MASK << FRONT_FACE_SHIFT);
            state.rasterizerState |= (Int32)type << FRONT_FACE_SHIFT;
        }

        public static Int32 GetFrontFace(int state) {
            return ((state >> FRONT_FACE_SHIFT) & FRONT_FACE_MASK);
        }

        public static void SetPolygonMode(ref RenderStateBlock state, PolygonMode mode) {
            state.rasterizerState &= ~(POLYGON_MODE_MASK << POLYGON_MODE_SHIFT);
            state.rasterizerState |= (Int32)mode << POLYGON_MODE_SHIFT;
        }

        public static Int32 GetPolygonMode(int state) {
            return (state >> POLYGON_MODE_SHIFT) & POLYGON_MODE_MASK;
        }

        public static void SetCullMode(ref RenderStateBlock state, CullMode mode) {
            state.rasterizerState &= ~(CULL_MODE_MASK << CULL_MODE_SHIFT);
            state.rasterizerState |= (Int32)mode << CULL_MODE_SHIFT;
        }

        public static Int32 GetCullMode(int state) {
            return (state >> CULL_MODE_SHIFT) & CULL_MODE_MASK;
        }

        public static void SetBlendFactor(ref RenderStateBlock state, BlendFactor src, BlendFactor dst) {
            SetColorBlendFactor(ref state, src, dst);
            SetAlphaBlendFactor(ref state, src, dst);
        }

        public static void SetColorBlendFactor(ref RenderStateBlock state, BlendFactor src, BlendFactor dst) {
            state.blendState &= ~(SRC_COLOR_BLEND_FACTOR_MASK << SRC_COLOR_BLEND_FACTOR_SHIFT);
            state.blendState &= ~(DST_COLOR_BLEND_FACTOR_MASK << DST_COLOR_BLEND_FACTOR_SHIFT);
            state.blendState |= (Int64)src << SRC_COLOR_BLEND_FACTOR_SHIFT;
            state.blendState |= (Int64)dst << DST_COLOR_BLEND_FACTOR_SHIFT;
        }

        public static void SetAlphaBlendFactor(ref RenderStateBlock state, BlendFactor src, BlendFactor dst) {
            state.blendState &= ~(SRC_ALPHA_BLEND_FACTOR_MASK << SRC_ALPHA_BLEND_FACTOR_SHIFT);
            state.blendState &= ~(DST_ALPHA_BLEND_FACTOR_MASK << DST_ALPHA_BLEND_FACTOR_SHIFT);
            state.blendState |= (Int64)src << SRC_ALPHA_BLEND_FACTOR_SHIFT;
            state.blendState |= (Int64)dst << DST_ALPHA_BLEND_FACTOR_SHIFT;
        }

        public static Int32 GetSrcColorBlendFactor(Int64 state) { 
            return (Int32)((state >> SRC_COLOR_BLEND_FACTOR_SHIFT) & SRC_COLOR_BLEND_FACTOR_MASK); 
        }

        public static Int32 GetDstColorBlendFactor(Int64 state) { 
            return (Int32)((state >> DST_COLOR_BLEND_FACTOR_SHIFT) & DST_COLOR_BLEND_FACTOR_MASK); 
        }

        public static Int32 GetSrcAlphaBlendFactor(Int64 state) {
            return (Int32)((state >> SRC_ALPHA_BLEND_FACTOR_SHIFT) & SRC_ALPHA_BLEND_FACTOR_MASK);
        }

        public static Int32 GetDstAlphaBlendFactor(Int64 state) {
            return (Int32)((state >> DST_ALPHA_BLEND_FACTOR_SHIFT) & DST_ALPHA_BLEND_FACTOR_MASK);
        }

        public static void SetBlendOp(ref RenderStateBlock state, BlendOp op) {
            SetColorBlendOp(ref state, op);
            SetAlphaBlendOp(ref state, op);
        }

        public static void SetColorBlendOp(ref RenderStateBlock state, BlendOp op) {
            state.blendState &= ~(COLOR_BLEND_OP_MASK << COLOR_BLEND_OP_SHIFT);
            state.blendState |= (Int64)op << COLOR_BLEND_OP_SHIFT;
        }

        public static void SetAlphaBlendOp(ref RenderStateBlock state, BlendOp op) {
            state.blendState &= ~(ALPHA_BLEND_OP_MASK << ALPHA_BLEND_OP_SHIFT);
            state.blendState |= (Int64)op << ALPHA_BLEND_OP_SHIFT;
        }

        public static Int32 GetColorBlendOp(Int64 state) {
            return (Int32)((state >> COLOR_BLEND_OP_SHIFT) & COLOR_BLEND_OP_MASK);
        }

        public static Int32 GetAlphaBlendOp(Int64 state) {
            return (Int32)((state >> ALPHA_BLEND_OP_SHIFT) & ALPHA_BLEND_OP_MASK);
        }

        public static void SetColorWriteMask(ref RenderStateBlock state, Int64 mask) {
            if((mask & RED_MASK) != 0) {
                state.blendState |= RED_MASK;
            } else {
                state.blendState &= ~RED_MASK;
            }
            if((mask & GREEN_MASK) != 0) {
                state.blendState |= GREEN_MASK;
            } else {
                state.blendState &= ~GREEN_MASK;
            }
            if((mask & BLUE_MASK) != 0) {
                state.blendState |= BLUE_MASK;
            } else {
                state.blendState &= ~BLUE_MASK;
            }
            if((mask & ALPHA_MASK) != 0) {
                state.blendState |= ALPHA_MASK;
            } else {
                state.blendState &= ~ALPHA_MASK;
            }
        }

        public static void SetDepthFunc(ref RenderStateBlock state, CompareOp func) {
            state.depthStencilState &= ~(DEPTH_FUNC_MASK << DEPTH_FUNC_SHIFT);
            state.depthStencilState |= (Int64)func << DEPTH_FUNC_SHIFT;
        }

        public static Int32 GetDepthFunc(Int64 state) {
            return (Int32)((state >> DEPTH_FUNC_SHIFT) & DEPTH_FUNC_MASK);
        }

        public static void SetDepthWriteMask(ref RenderStateBlock state, DepthWriteMaskFlags mask) {
            if(mask == DepthWriteMaskFlags.All) {
                state.depthStencilState |= DEPTH_WRITE_MASK;
            } else {
                state.depthStencilState &= ~DEPTH_WRITE_MASK;
            }
        }

        public static int GetDepthWriteMask(Int64 state) {
            return (state & DEPTH_WRITE_MASK) != 0 ? 1 : 0;
        }

        public static void SetStencilFunc(ref RenderStateBlock state, CompareOp func) {
            EnableStencilTest(ref state);
            state.depthStencilState &= ~(STENCIL_FUNC_MASK << STENCIL_FUNC_SHIFT);
            state.depthStencilState |= (Int64)func << STENCIL_FUNC_SHIFT;
        }

        public static Int32 GetStencilFunc(Int64 state) {
            return (Int32)((state >> STENCIL_FUNC_SHIFT) & STENCIL_FUNC_MASK);
        }

        public static void SetStencilRef(ref RenderStateBlock state, byte value) {
            EnableStencilTest(ref state);
            state.depthStencilState &= ~(STENCIL_REF_MASK << STENCIL_REF_SHIFT);
            state.depthStencilState |= (Int64)value << STENCIL_REF_SHIFT;
        }

        public static byte GetStencilRef(Int64 state) {
            return (byte)((state >> STENCIL_REF_SHIFT) & STENCIL_REF_MASK);
        }

        public static void SetStencilFuncMask(ref RenderStateBlock state, byte mask) {
            EnableStencilTest(ref state);
            state.depthStencilState &= ~(STENCIL_FUNC_MASK_MASK << STENCIL_FUNC_MASK_SHIFT);
            state.depthStencilState |= (Int64)mask << STENCIL_FUNC_MASK_SHIFT;
        }

        public static byte GetStencilFuncMask(Int64 state) {
            return (byte)((state >> STENCIL_FUNC_MASK_SHIFT) & STENCIL_FUNC_MASK_MASK);
        }

        public static void SetStencilWriteMask(ref RenderStateBlock state, byte mask) {
            EnableStencilTest(ref state);
            state.depthStencilState &= ~(STENCIL_WRITE_MASK_MASK << STENCIL_WRITE_MASK_SHIFT);
            state.depthStencilState |= (Int64)mask << STENCIL_WRITE_MASK_SHIFT;
        }

        public static byte GetStencilWriteMask(Int64 state) {
            return (byte)((state >> STENCIL_WRITE_MASK_SHIFT) & STENCIL_WRITE_MASK_MASK);
        }

        public static void SetStencilFailOp(ref RenderStateBlock state, StencilOp op) {
            EnableStencilTest(ref state);
            state.depthStencilState &= ~(STENCIL_OP_FAIL_MASK << STENCIL_OP_FAIL_SHIFT);
            state.depthStencilState |= (Int64)op << STENCIL_OP_FAIL_SHIFT;
        }

        public static Int32 GetStencilFailOp(Int64 state) {
            return (Int32)((state >> STENCIL_OP_FAIL_SHIFT) & STENCIL_OP_FAIL_MASK);
        }

        public static void SetStencilDepthFailOp(ref RenderStateBlock state, StencilOp op) {
            EnableStencilTest(ref state);
            state.depthStencilState &= ~(STENCIL_OP_DEPTH_FAIL_MASK << STENCIL_OP_DEPTH_FAIL_SHIFT);
            state.depthStencilState |= (Int64)op << STENCIL_OP_DEPTH_FAIL_SHIFT;
        }

        public static Int32 GetStencilDepthFailOp(Int64 state) {
            return (Int32)((state >> STENCIL_OP_DEPTH_FAIL_SHIFT) & STENCIL_OP_DEPTH_FAIL_MASK);
        }

        public static void SetStencilPassOp(ref RenderStateBlock state, StencilOp op) {
            EnableStencilTest(ref state);
            state.depthStencilState &= ~(STENCIL_OP_PASS_MASK << STENCIL_OP_PASS_SHIFT);
            state.depthStencilState |= (Int64)op << STENCIL_OP_PASS_SHIFT;
        }

        public static Int32 GetStencilPassOp(Int64 state) {
            return (Int32)((state >> STENCIL_OP_PASS_SHIFT) & STENCIL_OP_PASS_MASK);
        }

        public static void SetStencilBackFunc(ref RenderStateBlock state, CompareOp func) {
            EnableStencilTest(ref state);
            state.depthStencilState &= ~(STENCIL_BACK_FUNC_MASK << STENCIL_BACK_FUNC_SHIFT);
            state.depthStencilState |= (Int64)func << STENCIL_BACK_FUNC_SHIFT;
        }

        public static Int32 GetStencilBackFunc(Int64 state) {
            return (Int32)((state >> STENCIL_BACK_FUNC_SHIFT) & STENCIL_BACK_FUNC_MASK);
        }

        public static void SetStencilBackFailOp(ref RenderStateBlock state, StencilOp op) {
            EnableStencilTest(ref state);
            state.depthStencilState &= ~(SEPARATE_STENCIL | (STENCIL_BACK_OP_FAIL_MASK << STENCIL_BACK_OP_FAIL_SHIFT));
            state.depthStencilState |= SEPARATE_STENCIL | ((Int64)op << STENCIL_BACK_OP_FAIL_SHIFT);
        }

        public static UInt32 GetStencilBackFailOp(Int64 state) {
            return (UInt32)((state >> STENCIL_BACK_OP_FAIL_SHIFT) & STENCIL_BACK_OP_FAIL_MASK);
        }

        public static void SetStencilBackDepthFailOp(ref RenderStateBlock state, StencilOp op) {
            EnableStencilTest(ref state);
            state.depthStencilState &= ~(SEPARATE_STENCIL | (STENCIL_BACK_OP_DEPTH_FAIL_MASK << STENCIL_BACK_OP_DEPTH_FAIL_SHIFT));
            state.depthStencilState |= SEPARATE_STENCIL | ((Int64)op << STENCIL_BACK_OP_DEPTH_FAIL_SHIFT);
        }

        public static UInt32 GetStencilBackDepthFailOp(Int64 state) {
            return (UInt32)((state >> STENCIL_BACK_OP_DEPTH_FAIL_SHIFT) & STENCIL_BACK_OP_DEPTH_FAIL_MASK);
        }

        public static void SetStencilBackPassOp(ref RenderStateBlock state, StencilOp op) {
            EnableStencilTest(ref state);
            state.depthStencilState &= ~(SEPARATE_STENCIL | (STENCIL_BACK_OP_PASS_MASK << STENCIL_BACK_OP_PASS_SHIFT));
            state.depthStencilState |= SEPARATE_STENCIL | ((Int64)op << STENCIL_BACK_OP_PASS_SHIFT);
        }

        public static Int32 GetStencilBackPassOp(Int64 state) {
            return (Int32)((state >> STENCIL_BACK_OP_PASS_SHIFT) & STENCIL_BACK_OP_PASS_MASK);
        }
    }  

    public enum ShaderStages {
        None = 0,
        Vertex = 1,
        Geometry = 1 << 2,
        Hull = 1 << 3,
        Domain = 1 << 4,
        Pixel = 1 << 5,
        AllGraphics = Vertex | Geometry | Hull | Domain | Pixel,
        Compute
    }

    public static class GraphicsDevice {
        public static Veldrid.GraphicsDevice gd {get; private set;}
        public static Veldrid.ResourceFactory ResourceFactory {get {return gd.ResourceFactory;}}
        public static Veldrid.Sampler LinearClampSampler {get; private set;}
        public static Veldrid.Sampler PointClampSampler {get; private set;}
        public static Veldrid.Sampler ShadowMapSampler {get; private set;}
        public static Veldrid.Sampler AnisoSampler {get; private set;}
        public static Veldrid.Sampler AnisoClampSampler {get; private set;}
        public static Veldrid.Sampler BilinearSampler {get; private set;}
        public static SharpDX.Direct3D11.SamplerState _LinearClampSampler {get; private set;}
        public static SharpDX.Direct3D11.SamplerState _LinearSampler {get; private set;}
        public static SharpDX.Direct3D11.SamplerState _BilinearSampler {get; private set;}
        public static SharpDX.Direct3D11.SamplerState _PointSampler {get; private set;}
        public static SharpDX.Direct3D11.SamplerState _PointClampSampler {get; private set;}
        public static SharpDX.Direct3D11.SamplerState _ShadowMapSampler {get; private set;}
        public static SharpDX.Direct3D11.SamplerState _Aniso16xSampler {get; private set;}

        public static void Init() {
            Veldrid.GraphicsDeviceOptions gdOptions = new Veldrid.GraphicsDeviceOptions(true, null, true);
            gd = Veldrid.GraphicsDevice.CreateD3D11(gdOptions);

            LinearClampSampler = GraphicsDevice.ResourceFactory.CreateSampler(
                new Veldrid.SamplerDescription(
                    Veldrid.SamplerAddressMode.Clamp, Veldrid.SamplerAddressMode.Clamp, Veldrid.SamplerAddressMode.Clamp,
                    Veldrid.SamplerFilter.MinLinear_MagLinear_MipPoint,
                    null,
                    1,
                    0,
                    13,
                    0,
                    Veldrid.SamplerBorderColor.OpaqueBlack
            ));
            var property = LinearClampSampler.GetType().GetProperty("DeviceSampler");
            _LinearClampSampler = property.GetValue(LinearClampSampler) as SharpDX.Direct3D11.SamplerState;
            property = gd.LinearSampler.GetType().GetProperty("DeviceSampler");
            _LinearSampler = property.GetValue(gd.LinearSampler) as SharpDX.Direct3D11.SamplerState;

            BilinearSampler = GraphicsDevice.ResourceFactory.CreateSampler(new Veldrid.SamplerDescription(
                Veldrid.SamplerAddressMode.Wrap, Veldrid.SamplerAddressMode.Wrap, Veldrid.SamplerAddressMode.Wrap,
                Veldrid.SamplerFilter.MinLinear_MagLinear_MipPoint,
                null,
                1,
                0,
                13,
                0,
                Veldrid.SamplerBorderColor.OpaqueBlack
            ));
            property = BilinearSampler.GetType().GetProperty("DeviceSampler");
            _BilinearSampler = property.GetValue(BilinearSampler) as SharpDX.Direct3D11.SamplerState;         

            PointClampSampler = GraphicsDevice.ResourceFactory.CreateSampler(
                new Veldrid.SamplerDescription(
                    Veldrid.SamplerAddressMode.Clamp, Veldrid.SamplerAddressMode.Clamp, Veldrid.SamplerAddressMode.Clamp,
                    Veldrid.SamplerFilter.MinPoint_MagPoint_MipPoint,
                    null,
                    1,
                    0,
                    13,
                    0,
                    Veldrid.SamplerBorderColor.OpaqueBlack
            ));
            property = PointClampSampler.GetType().GetProperty("DeviceSampler");
            _PointClampSampler = property.GetValue(PointClampSampler) as SharpDX.Direct3D11.SamplerState;
            property = gd.PointSampler.GetType().GetProperty("DeviceSampler");
            _PointSampler = property.GetValue(gd.PointSampler) as SharpDX.Direct3D11.SamplerState;

            AnisoSampler = GraphicsDevice.ResourceFactory.CreateSampler(
                new Veldrid.SamplerDescription(
                    Veldrid.SamplerAddressMode.Wrap, Veldrid.SamplerAddressMode.Wrap, Veldrid.SamplerAddressMode.Wrap,
                    Veldrid.SamplerFilter.Anisotropic,
                    null,
                    16, 
                    0,
                    13,
                    0,
                    Veldrid.SamplerBorderColor.OpaqueBlack
            ));
            property = AnisoSampler.GetType().GetProperty("DeviceSampler");
            _Aniso16xSampler = property.GetValue(AnisoSampler) as SharpDX.Direct3D11.SamplerState;        

            ShadowMapSampler = GraphicsDevice.ResourceFactory.CreateSampler(
                new Veldrid.SamplerDescription(
                    Veldrid.SamplerAddressMode.Border, Veldrid.SamplerAddressMode.Border, Veldrid.SamplerAddressMode.Border,
                    Veldrid.SamplerFilter.MinLinear_MagLinear_MipPoint,
                    Veldrid.ComparisonKind.LessEqual,
                    1,
                    0,
                    13,
                    0,
                    Veldrid.SamplerBorderColor.OpaqueWhite
            ));
            property = ShadowMapSampler.GetType().GetProperty("DeviceSampler");
            _ShadowMapSampler = property.GetValue(ShadowMapSampler) as SharpDX.Direct3D11.SamplerState;            
        }

        public static void Shutdown() {
            gd.WaitForIdle();
            gd.Dispose();
        }
    }
}