Shader "Hidden/merge_texture_helper_v2"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
		// x, y, w, h
		_Rect ("Rectangle", Vector) = (0, 0, 1, 1)
		_SrcRect ("SourceRectangle", Vector) = (0, 0, 1, 1)
		_NoClip ("NoClip", Int) = 0
	}
	SubShader
	{
		Cull Off
		ZWrite Off
		ZTest Always

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
				float2 uv : TEXCOORD0;
				float4 vertex : SV_POSITION;
			};

			sampler2D _MainTex;
			float4x4 _MainTexTransform;
			float4 _Rect;
			int _NoClip;

			v2f vert (appdata v)
			{
				v2f o;
				v.vertex.xy = v.vertex.xy * _Rect.zw + _Rect.xy;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}

			fixed4 frag (v2f i) : SV_Target
			{
				float3x3 mat = _MainTexTransform;
				float2 uv = mul(mat, float3(i.uv, 1)).xy;
				fixed4 c = tex2D(_MainTex, uv);
				if (_NoClip == 0)
					clip(c.a - 0.0001);
				return c;
			}
			ENDCG
		}
	}
}
