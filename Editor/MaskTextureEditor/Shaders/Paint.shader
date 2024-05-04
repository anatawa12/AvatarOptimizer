Shader "Hidden/AvatarOptimizer/MaskTextureEditor/Paint"
{
    Properties
    {
        _MainTex("Texture", 2D) = "white" {}
        _BrushLine("Brush Line", Vector) = (0.5, 0.5, 0.5, 0.5)
        _BrushSize("Brush Size", Float) = 100.0
        _BrushColor("Brush Color", Color) = (1.0, 1.0, 1.0, 1.0)
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
        }

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            
            float4 _BrushLine;
            float _BrushSize;
            float4 _BrushColor;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                float2 a = _BrushLine.xy * _MainTex_TexelSize.xy;
                float2 b = _BrushLine.zw * _MainTex_TexelSize.xy;
                float r = _BrushSize * _MainTex_TexelSize.xy * 0.5;
                float d =
                    dot(i.uv - a, b - a) <= 0 ? distance(i.uv, a) :
                    dot(i.uv - b, a - b) <= 0 ? distance(i.uv, b) :
                    abs((i.uv - a).x * (b - a).y - (i.uv - a).y * (b - a).x) / distance(a, b);
                return lerp(tex2D(_MainTex, i.uv), _BrushColor, d < r);
            }
            ENDCG
        }
    }
}
