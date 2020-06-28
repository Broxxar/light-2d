// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/KawaseBlur"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}
	SubShader
	{
		Cull Off ZWrite Off ZTest Always

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
				float2 uv0 : TEXCOORD0;
				float2 uv1 : TEXCOORD1;
				float2 uv2 : TEXCOORD2;
				float2 uv3 : TEXCOORD3;
			};

			float2 _MainTex_TexelSize;
			float _Steps;

			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);

				float2 halfTexel = (_MainTex_TexelSize * .5) + _Steps * _MainTex_TexelSize;

				o.uv0 = v.uv + float2(-halfTexel.x, -halfTexel.y);
				o.uv1 = v.uv + float2(-halfTexel.x, halfTexel.y);
				o.uv2 = v.uv + float2(halfTexel.x, halfTexel.y);
				o.uv3 = v.uv + float2(halfTexel.x, -halfTexel.y);

				return o;
			}
			
			sampler2D _MainTex;

			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = 0;
			
				col += tex2D(_MainTex, i.uv0);
				col += tex2D(_MainTex, i.uv1);
				col += tex2D(_MainTex, i.uv2);
				col += tex2D(_MainTex, i.uv3);

				col *= 0.25;

				return col;
			}
			ENDCG
		}
	}
}
