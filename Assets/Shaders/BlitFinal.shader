Shader "Tutorial/BlitFinal"
{
  Properties
  {
    _MainTex ("Texture", any) = "" {}
  }
  SubShader {
    Pass {
      ZTest Always Cull Off ZWrite Off

      CGPROGRAM
      #pragma vertex vert
      #pragma fragment frag
      #include "UnityCG.cginc"
      #include "SH.hlsl"

      float3 _MainLightDir;
      UNITY_DECLARE_SCREENSPACE_TEXTURE(_MainTex);
      uniform float4 _MainTex_ST;
      UNITY_DECLARE_SCREENSPACE_TEXTURE(_GBuffer);

      struct appdata_t {
        float4 vertex : POSITION;
        float2 texcoord : TEXCOORD0;
        UNITY_VERTEX_INPUT_INSTANCE_ID
      };

      struct v2f {
        float4 vertex : SV_POSITION;
        float2 texcoord : TEXCOORD0;
        UNITY_VERTEX_OUTPUT_STEREO
      };

      v2f vert (appdata_t v)
      {
        v2f o;
        UNITY_SETUP_INSTANCE_ID(v);
        UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);
        o.vertex = UnityObjectToClipPos(v.vertex);
        o.texcoord = TRANSFORM_TEX(v.texcoord.xy, _MainTex);
        return o;
      }

      float4 frag (v2f i) : SV_Target
      {
        UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);
        float ao = 1.0f - UNITY_SAMPLE_SCREENSPACE_TEXTURE(_MainTex, i.texcoord).r;
        float3 N = UNITY_SAMPLE_SCREENSPACE_TEXTURE(_GBuffer, i.texcoord).rgb;
        return float4((max(0, dot(N, _MainLightDir)) * 0.8f + GetSHColor(N)) * ao, 1);
      }
      ENDCG

    }
  }
  Fallback Off
}
