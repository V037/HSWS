Shader "Custom/InstancedIndirectColor2" {
    SubShader {
        Tags { "RenderType" = "Opaque" }
        //Cull Off

        Pass {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"
            
            struct appdata_t {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
            };

            struct v2f {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
            }; 

            struct ParticleStruct
            {
                float3 pos;
                float3 vel;
                float3 temp;
                uint counts;
            };

            struct MeshProperties {
                float4 color;
            };

            float3 _waterPosition;

            StructuredBuffer<MeshProperties> _Properties;

            StructuredBuffer<ParticleStruct> _dataBuffer;

            v2f vert(appdata_t i, uint instanceID: SV_InstanceID)
            {
                v2f o;

                //Assignment with MAD
                o.vertex = UnityObjectToClipPos(_dataBuffer[instanceID].pos + i.vertex + _waterPosition);
                o.color = _Properties[instanceID].color;

                return o;
            }
            
            fixed4 frag(v2f i) : SV_Target {
                return i.color;
            }
            
            ENDCG
        }
    }
}