// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/GenerateDepthAndShowWoldPos" {
    Properties {
    }
    SubShader {
        Tags { "RenderType"="Opaque" }
        LOD 200
        Blend  Off
        


        Pass{
            CGPROGRAM

            #include "UnityCG.cginc"
            #pragma vertex vert
            #pragma fragment frag


            struct v2f {
                float4 pos: SV_POSITION;
                float4 worldpos : TEXCOORD0;
            };

            v2f vert( appdata_img v ) 
            {
                v2f o;
                o.pos = UnityObjectToClipPos( v.vertex ) ;
                o.worldpos = mul(unity_ObjectToWorld, v.vertex);
                o.worldpos.w = o.pos.z / o.pos.w;
                return o;
            }

            float4 frag( v2f o ) : COLOR
            {
                return float4( o.worldpos.xyz, 1.0) ; // o.worldpos.xyz/255 是为了颜色输出。 
            }

            ENDCG
        }
    } 
    FallBack "Diffuse"
}
