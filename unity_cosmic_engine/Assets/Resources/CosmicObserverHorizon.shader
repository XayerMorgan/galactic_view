Shader "Cosmic/ObserverHorizon"
{
    Properties { _Zenith ("Zenith", Vector) = (0,1,0,0) _North ("North", Vector) = (0,0,1,0) _East ("East", Vector) = (1,0,0,0) }
    SubShader
    {
        Tags { "Queue"="Geometry" "RenderType"="Opaque" }
        Cull Off
        CGINCLUDE
        #include "UnityCG.cginc"
        float3 _Zenith, _North, _East;
        struct v2f { float4 pos : SV_POSITION; float3 world : TEXCOORD0; };
        v2f vert(float4 vertex : POSITION) { v2f o; o.pos = UnityObjectToClipPos(vertex); o.world = mul(unity_ObjectToWorld, vertex).xyz; return o; }
        ENDCG
        // The opaque ground writes depth, hiding stars and catalog geometry below the horizon.
        Pass
        {
            ZWrite On
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            fixed4 frag(v2f i) : SV_Target
            {
                float3 d = normalize(i.world);
                float h = dot(d, _Zenith);
                clip(-h);
                float bearing = atan2(dot(d, _East), dot(d, _North));
                float distantRidge = -.025 - .018 * sin(bearing * 7 + 1) - .007 * sin(bearing * 19);
                float nearRidge = -.10 - .035 * sin(bearing * 4) - .018 * sin(bearing * 11 + 2);
                float3 c = lerp(float3(.011,.024,.030), float3(.090,.145,.158), exp(h * 18));
                c = lerp(c, float3(.023,.049,.061), step(h, distantRidge));
                c = lerp(c, float3(.009,.024,.033), step(h, nearRidge));
                c += float3(.07,.15,.16) * exp(h * 520);
                return fixed4(c, 1);
            }
            ENDCG
        }
        // A subtle band of atmosphere gives the eye a readable edge even in a sparse sky.
        Pass
        {
            ZWrite Off
            Blend One One
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            fixed4 frag(v2f i) : SV_Target
            {
                float h = dot(normalize(i.world), _Zenith);
                clip(h);
                return fixed4(float3(.024,.063,.085) * exp(-h * 24), 1);
            }
            ENDCG
        }
    }
}
