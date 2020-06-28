// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Light2D/Alpha Blend"
{
	Properties
	{
		_MainTex ("Light Texture", 2D) = "white" {}
	}
		
	SubShader
	{
		Tags
		{ 
			"Queue"="Transparent" 
			"IgnoreProjector"="True" 
			"RenderType"="Transparent" 
		}

		Cull Off
		Lighting Off
		ZWrite Off
		Blend SrcAlpha OneMinusSrcAlpha
	
		Pass
		{
			CGPROGRAM
				#pragma vertex vert
				#pragma fragment frag

				struct appdata_t
				{
					float4 vertex   : POSITION;
					float4 color    : COLOR;
					float2 texcoord : TEXCOORD0;
				};

				struct v2f
				{
					float4 vertex   : SV_POSITION;
					fixed4 color    : COLOR;
					half2 texcoord  : TEXCOORD0;
				};
				
				v2f vert(appdata_t IN)
				{
					v2f OUT;
					OUT.vertex = UnityObjectToClipPos(IN.vertex);
					OUT.texcoord = IN.texcoord;
					OUT.color = IN.color;
					return OUT;
				}

				sampler2D _MainTex;

				fixed4 frag(v2f IN) : SV_Target
				{
					fixed4 c = tex2D(_MainTex, IN.texcoord) * IN.color;
					return c;
				}
			ENDCG
		}
	}
}