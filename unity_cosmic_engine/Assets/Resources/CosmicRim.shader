Shader "Cosmic/Rim"
{
    Properties { _Color ("Color", Color) = (0.3,0.7,1,0.4) _Power ("Rim power", Float) = 4 }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        Blend SrcAlpha One
        ZWrite Off
        Cull Back
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"
            struct v2f { float4 pos : SV_POSITION; float3 normal : TEXCOORD0; float3 view : TEXCOORD1; };
            fixed4 _Color;
            float _Power;
            v2f vert(appdata_base v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.normal = UnityObjectToWorldNormal(v.normal);
                o.view = _WorldSpaceCameraPos - mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }
            fixed4 frag(v2f i) : SV_Target
            {
                float rim = pow(1 - saturate(dot(normalize(i.normal), normalize(i.view))), _Power);
                return fixed4(_Color.rgb, _Color.a * rim);
            }
            ENDCG
        }
    }
}
