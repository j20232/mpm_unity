Shader "Instance/Particle"{
    Properties {
        _Color ("Color", Color) = (1,1,1,1)
        _Size ("Size", float) = 0.035
        _GridSize("Grid Size", int) = 32
    }

    SubShader {
        Pass {
            Tags { "LightMode"="ForwardBase" "Queue" = "Transparent" "RenderType"="Transparent" "IgnoreProjector"="True"}
            ZWrite Off
            Blend SrcAlpha OneMinusSrcAlpha
            
            CGPROGRAM

            #pragma glsl
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fwdbase nolightmap nodirlightmap nodynlightmap novertexlight
            #pragma target 4.5

            #include "UnityCG.cginc"

            struct Particle {
                float2 x;
                float2 v;
                float4 C;
                float mass;
                float padding;
            };

            struct v2f{
                float4 pos : SV_POSITION;
            };

            float _Size;
            fixed4 _Color;
            int _GridSize;

            StructuredBuffer<Particle> particle_buffer;

            v2f vert(appdata_full v, uint instanceID : SV_InstanceID){
                float4 data = float4(
                    (particle_buffer[instanceID].x.xy - float2(_GridSize, _GridSize)) * 0.1, 0, 1.0);
                float3 localPosition = v.vertex.xyz * (_Size * data.w);
                float3 worldPosition = data.xyz + localPosition;

                v2f o;
                o.pos = mul(UNITY_MATRIX_VP, float4(worldPosition, 1.0f));

                return o;
            }

            fixed4 frag(v2f i) : SV_TARGET{
                return _Color;
            }

            ENDCG
        }
    }
    FallBack "Diffuse"
}
