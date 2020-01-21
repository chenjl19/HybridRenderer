shader "builtin/prez" {
    RenderState "StaticModel" {
        RenderPass "DepthPass"
        VertexFactory "DrawVertexPacked0VertexFactory"
        vs {
            shaderFile "glsl/prez.hlsl"
            entryPoint "StaticModelDepthOnlyVS"
        }
        ZTest LEqual
        ZWrite on
        CullMode Back
        FillMode Solid
        Topology TriangleList
    }

    RenderState "StaticModelAlphaTest" {
        RenderPass "DepthPass"
        VertexFactory "DrawVertexPacked0VertexFactory"
        vs {
            shaderFile "glsl/prez.hlsl"
            entryPoint "StaticModelAlphaTestVS"
        }
        fs {
            shaderFile "glsl/prez.hlsl"
            entryPoint "AlphaTestPS"
        }
        ZTest LEqual
        ZWrite on
        CullMode Back
        FillMode Solid
        Topology TriangleList
    }

    RenderState "DynamicModel" {
        RenderPass "DepthPass"
        VertexFactory "DrawVertexPackedVertexFactory"
        vs {
            shaderFile "glsl/prez.hlsl"
            entryPoint "DepthOnlyVS"
        }
        ZTest LEqual
        ZWrite on
        CullMode Back
        FillMode Solid
        Topology TriangleList
    }

    RenderState "DynamicModelAlphaTest" {
        RenderPass "DepthPass"
        VertexFactory "DrawVertexPackedVertexFactory"
        vs {
            shaderFile "glsl/prez.hlsl"
            entryPoint "AlphaTestVS"
        }
        fs {
            shaderFile "glsl/prez.hlsl"
            entryPoint "AlphaTestPS"
        }
        ZTest LEqual
        ZWrite on
        CullMode Back
        FillMode Solid
        Topology TriangleList
    }
}

shader "builtin/shadow" {
    RenderState "StaticModel" {
        RenderPass "ShadowAtlasPass"
        VertexFactory "DrawVertexPacked0VertexFactory"
        vs {
            shaderFile "glsl/shadow.hlsl"
            entryPoint "StaticModelDepthOnlyVS"
        }
        ZTest LEqual
        ZWrite on
        CullMode Back
        FillMode Solid
        Topology TriangleList
    }

    RenderState "StaticModelAlphaTest" {
        RenderPass "ShadowAtlasPass"
        VertexFactory "DrawVertexPacked0VertexFactory"
        vs {
            shaderFile "glsl/shadow.hlsl"
            entryPoint "StaticModelAlphaTestVS"
        }
        fs {
            shaderFile "glsl/shadow.hlsl"
            entryPoint "AlphaTestPS"
        }
        ZTest LEqual
        ZWrite on
        CullMode Back
        FillMode Solid
        Topology TriangleList
    }

    RenderState "DynamicModel" {
        RenderPass "ShadowAtlasPass"
        VertexFactory "DrawVertexPackedVertexFactory"
        vs {
            shaderFile "glsl/shadow.hlsl"
            entryPoint "DepthOnlyVS"
        }
        ZTest LEqual
        ZWrite on
        CullMode Back
        FillMode Solid
        Topology TriangleList
    }

    RenderState "DynamicModelAlphaTest" {
        RenderPass "ShadowAtlasPass"
        VertexFactory "DrawVertexPackedVertexFactory"
        vs {
            shaderFile "glsl/shadow.hlsl"
            entryPoint "AlphaTestVS"
        }
        fs {
            shaderFile "glsl/shadow.hlsl"
            entryPoint "AlphaTestPS"
        }
        ZTest LEqual
        ZWrite on
        CullMode Back
        FillMode Solid
        Topology TriangleList
    }
}

shader "builtin/skybox" {
    RenderState "Cubemap" {
        ZTest LEqual
        ZWrite off
        ZClip off
        CullMode Front
        FillMode Solid
        Topology TriangleList
        Blend One OneMinusSrcAlpha
        AlphaBlend One OneMinusSrcAlpha
        RenderPass "SkybackgroundPass"
        VertexFactory "FullscreenVertexFactory"

        vs {
            shaderFile "glsl/skybox.hlsl"
            entryPoint "main"
        }
        fs {
            shaderFile "glsl/skybox.hlsl"
            entryPoint "SkyboxCubemap"
        }
    }  

    RenderState "HDRI" {
        ZTest LEqual
        ZWrite off
        ZClip off
        CullMode Front
        FillMode Solid
        Topology TriangleList
        Blend One OneMinusSrcAlpha
        AlphaBlend One OneMinusSrcAlpha
        RenderPass "SkybackgroundPass"
        VertexFactory "FullscreenVertexFactory"

        vs {
            shaderFile "glsl/skybox.hlsl"
            entryPoint "main"
        }
        fs {
            shaderFile "glsl/skybox.hlsl"
            entryPoint "SkyboxLatLong"
        }
    }   

}


shader "builtin/editor/envprobe" {
    RenderState "base" {
        ZTest LEqual
        ZWrite off
        CullMode Back
        FillMode Solid
        Topology TriangleList

        RenderPass "OpaquePass"
        VertexFactory "DrawVertexPackedVertexFactory"

        vs {
            shaderFile "glsl/editor_envprobe.hlsl"
            entryPoint "main"
        }
        fs {
            shaderFile "glsl/editor_envprobe.hlsl"
            entryPoint "main"
        }
    }    
}
