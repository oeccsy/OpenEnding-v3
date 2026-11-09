// 선분을 SDF로 그리는 셰이더. 선분 끝은 둥글다.
// uv0 : (±alongExtent, ±acrossExtent, halfLength, halfWidth). uv0.xy 값은 보간되어 픽셀의 선분 기준 위치가 된다
Shader "Oeccsy/StrokeMotion/StrokeLine"
{
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" "IgnoreProjector"="True" }

        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float4 uv0 : TEXCOORD0;
                float4 color : COLOR;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float4 segment : TEXCOORD0;
                float4 color : COLOR;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.color = v.color;
                o.segment = v.uv0;

                // 정점 색은 sRGB 그대로 들어오므로, Linear 색 공간 프로젝트에서는 Linear로 바꿔야 원래 색이 나온다
                #ifndef UNITY_COLORSPACE_GAMMA
                o.color.rgb = GammaToLinearSpace(v.color.rgb);
                #endif

                return o;
            }

            float segmentSDF(float2 position, float halfLength, float halfWidth)
            {
                float dx = max(abs(position.x) - halfLength, 0.0);
                float dy = position.y;
                
                float signedDistance = length(float2(dx, dy)) - halfWidth;
                return signedDistance;
            }

            float4 frag (v2f i) : SV_Target
            {
                float2 position = i.segment.xy;
                float halfLength = i.segment.z;
                float halfWidth = i.segment.w;

                float distance = segmentSDF(position, halfLength, halfWidth);
                float featherLength = max(fwidth(distance), 0.0001);

                // 안쪽으로 반 픽셀 이상 : 1, 선의 윤곽 : 0.5, 바깥으로 반 픽셀 이상 : 0
                float alphaScale = saturate(0.5 - distance / featherLength);

                float4 color = i.color;
                color.a *= alphaScale;
                
                return color;
            }
            ENDCG
        }
    }
}
