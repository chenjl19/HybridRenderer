shader "builtin/Unlit" {
    RenderState "base" {
        ZTest LEqual
        ZWrite on
        CullMode Back
        FillMode Solid
        Topology TriangleList

        RenderPass "OpaquePass"
        VertexFactory "RenderDebugVertexFactory"

        vs {
            shaderFile "glsl/unlit.hlsl"
            entryPoint "main"
        }
        fs {
            shaderFile "glsl/unlit.hlsl"
            entryPoint "main"
        }
    }    
}

shader "hidden/error" {
    RenderState "StaticModel" {
        ZTest Equal
        ZWrite off
        CullMode Back
        FillMode Solid
        Topology TriangleList

        RenderPass "OpaquePass"
        VertexFactory "DrawVertexStaticPackedVertexFactory"
        vs {
            shaderFile "glsl/unlit.hlsl"
            entryPoint "ErrorVS"
            defines "USE_LIGHTMAPS"
        }
        fs {
            shaderFile "glsl/unlit.hlsl"
            entryPoint "ErrorPS"
        }
    }

    RenderState "DynamicModel" {
        ZTest Equal
        ZWrite off
        CullMode Back
        FillMode Solid
        Topology TriangleList

        RenderPass "OpaquePass"
        VertexFactory "DrawVertexPackedVertexFactory"
        vs {
            shaderFile "glsl/unlit.hlsl"
            entryPoint "ErrorVS"
        }
        fs {
            shaderFile "glsl/unlit.hlsl"
            entryPoint "ErrorPS"
        }
    }
}

shader "builtin/ForwardLit" {
    RenderState "StaticModel" {
        ZTest Equal
        ZWrite off
        CullMode Back
        FillMode Solid
        Topology TriangleList

        RenderPass "OpaquePass"
        VertexFactory "DrawVertexStaticPackedVertexFactory"
        vs {
            shaderFile "glsl/lit2.hlsl"
            entryPoint "main"
            defines "USE_LIGHTMAPS"
        }
        fs {
            shaderFile "glsl/lit2.hlsl"
            entryPoint "main"
            defines "USE_LIGHTMAPS"
        }
    }

    RenderState "DynamicModel" {
        ZTest Equal
        ZWrite off
        CullMode Back
        FillMode Solid
        Topology TriangleList

        RenderPass "OpaquePass"
        VertexFactory "DrawVertexPackedVertexFactory"
        vs {
            shaderFile "glsl/lit2.hlsl"
            entryPoint "main"
        }
        fs {
            shaderFile "glsl/lit2.hlsl"
            entryPoint "main"
        }
    }

    RenderState "Skin" {
        ZTest Equal
        ZWrite off
        CullMode Back
        FillMode Solid
        Topology TriangleList

        RenderPass "OpaquePass"
        VertexFactory "DrawVertexPackedVertexFactory"
        vs {
            shaderFile "glsl/lit2.hlsl"
            entryPoint "main"
            defines "USE_FORWARD_SKIN"
        }
        fs {
            shaderFile "glsl/lit2.hlsl"
            entryPoint "main"
            defines "USE_FORWARD_SKIN"
        }
    }
}

shader "builtin/ForwardLitAlphaTest" {
    Queue "AlphaTest"

    RenderState "StaticModel" {
        ZTest Equal
        ZWrite off
        CullMode Back
        FillMode Solid
        Topology TriangleList

        RenderPass "OpaquePass"
        VertexFactory "DrawVertexStaticPackedVertexFactory"
        vs {
            shaderFile "glsl/lit2.hlsl"
            entryPoint "main"
            defines "USE_LIGHTMAPS"
        }
        fs {
            shaderFile "glsl/lit2.hlsl"
            entryPoint "main"
            defines "USE_LIGHTMAPS"
        }
    }

    RenderState "DynamicModel" {
        ZTest Equal
        ZWrite off
        CullMode Back
        FillMode Solid
        Topology TriangleList

        RenderPass "OpaquePass"
        VertexFactory "DrawVertexPackedVertexFactory"
        vs {
            shaderFile "glsl/lit2.hlsl"
            entryPoint "main"
        }
        fs {
            shaderFile "glsl/lit2.hlsl"
            entryPoint "main"
        }
    }
}

shader "builtin/particle" {
    Queue "Transparency"
    RenderState "hq" {
        ZTest Always
        ZWrite off
        CullMode None
        FillMode Solid
        Blend One OneMinusSrcAlpha
        AlphaBlend One OneMinusSrcAlpha
        Topology TriangleList
        RenderPass "TransparencyPass"
        VertexFactory "ParticleVertexFactory"
        vs {
            shaderFile "glsl/particle.hlsl"
            entryPoint "main"
        }
        fs {
            shaderFile "glsl/particle.hlsl"
            entryPoint "main"
        }
    }
}