// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Hidden/LightCameraBlend"
{
	Properties
	{
		_MainTex ("Texture", 2D) = "white" {}
	}

	CGINCLUDE
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

	v2f vert (appdata v)
	{
		v2f o;
		o.vertex = UnityObjectToClipPos(v.vertex);
		o.uv = v.uv;
		return o;
	}
			
	sampler2D _MainTex;

	fixed4 frag (v2f i) : SV_Target
	{
		return tex2D(_MainTex, i.uv);;
	}
	ENDCG

	SubShader
	{
		Cull Off ZWrite Off ZTest Always

		Pass
		{
			Blend DstColor Zero

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}

		Pass
		{
			Blend DstColor SrcColor

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}

		Pass
		{
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}
		Pass
		{
			Blend One One

			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag
			ENDCG
		}
	}
}
