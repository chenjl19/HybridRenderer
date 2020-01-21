shader "builtin/ScreenPic" {
    RenderState "ScreenPic" {
        RenderPass "PresentPass"
        VertexFactory "ScreenPicVertexFactory"
        vs {
            shaderFile "glsl/screenQuad.hlsl"
            entryPoint "main"
        }
        fs {
            shaderFile "glsl/screenQuad.hlsl"
            entryPoint "main"
        }
        ZTest Always
        ZWrite off
        CullMode none
        FillMode Solid
        Topology TriangleList   
    }
}

shader "builtin/FinalBlit" {
    RenderState "base" {
        ZTest Always
        ZWrite off
        CullMode None
        FillMode Solid
        Topology TriangleStrip

        RenderPass "PresentPass"
        VertexFactory "FullscreenVertexFactory"

        vs {
            shaderFile "glsl/fullscreenQuad.hlsl"
            entryPoint "main"
        }
        fs {
            shaderFile "glsl/fullscreenQuad.hlsl"
            entryPoint "main"
        }
    }    
}