shader "tools/debugDraw" {
    RenderState "Wireframe" {
        RenderPass "DebugPass"
        VertexFactory "RenderDebugVertexFactory"
        vs {
            shaderFile "glsl/debugDraw.hlsl"
            entryPoint "main"
        }
        fs {
            shaderFile "glsl/debugDraw.hlsl"
            entryPoint "main"
        }
        ZTest Always
        ZWrite off
        CullMode none
        FillMode Wireframe
        Topology LineList
    }

    RenderState "Filled" {
        RenderPass "DebugPass"
        VertexFactory "RenderDebugVertexFactory"
        vs {
            shaderFile "glsl/debugDraw.hlsl"
            entryPoint "main"
        }
        fs {
            shaderFile "glsl/debugDraw.hlsl"
            entryPoint "main"
        }
        ZTest Always
        ZWrite off
        CullMode none
        FillMode Solid
        Topology TriangleList
    }

    RenderState "ScreenPic" {
        RenderPass "DebugPass"
        VertexFactory "DrawVertexVertexFactory"
        vs {
            shaderFile "glsl/debugDraw.hlsl"
            entryPoint "ScreenPicVS"
        }
        fs {
            shaderFile "glsl/debugDraw.hlsl"
            entryPoint "ScreenPicPS"
        }
        ZTest Always
        ZWrite off
        CullMode none
        FillMode Solid
        Topology TriangleStrip        
    }
}