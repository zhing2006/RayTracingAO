Shader "Tutorial/AmbientOcclusion"
{
   Properties
   {
   }
   SubShader
   {
      Tags { "RenderType"="Opaque" }
      LOD 100

      Pass
      {
        Name "GBuffer"
        CGPROGRAM
        #pragma vertex vert
        #pragma fragment frag
        // make fog work
        #pragma multi_compile_fog

        #include "UnityCG.cginc"

        struct appdata
        {
           float4 vertex : POSITION;
           float3 normal : NORMAL;
        };

        struct v2f
        {
           float3 normal : TEXCOORD0;
           UNITY_FOG_COORDS(1)
           float4 vertex : SV_POSITION;
        };

        v2f vert (appdata v)
        {
           v2f o;
           o.vertex = UnityObjectToClipPos(v.vertex);
           o.normal = UnityObjectToWorldNormal(v.normal);
           UNITY_TRANSFER_FOG(o, o.vertex);
           return o;
        }

        float4 frag (v2f i) : SV_Target
        {
          float4 col = float4(i.normal, i.vertex.z);
          return col;
        }
        ENDCG
      }
   }

   SubShader
   {
      Pass
      {
        Name "RayTracing"
        Tags { "LightMode" = "RayTracing" }

        HLSLPROGRAM

        #pragma raytracing test

        #include "./Common.hlsl"

        [shader("closesthit")]
        void ClosestHitShader(inout RayIntersectionAO rayIntersection : SV_RayPayload, AttributeData attributeData : SV_IntersectionAttributes)
        {
           rayIntersection.ao = 1.0f;
        }

        ENDHLSL
      }
   }
}
