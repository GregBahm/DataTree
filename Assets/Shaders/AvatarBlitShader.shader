Shader "Unlit/AvatarBlitShader"
{
	Properties
	{
		_MainTex("Texture", 2D) = "black" {}
		_OutputTex("Texture", 2D) = "black" {}
	}
	SubShader
	{
		Tags { "RenderType"="Opaque" }
		LOD 100

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
			sampler2D _OutputTex;

			float _XOffset;
			float _YOffset;
			#define TextureScale (1024 / 16) // TODO: Softcode the texture resolution
			
			v2f vert (appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				o.uv = v.uv;
				return o;
			}
			 
			fixed4 frag (v2f i) : SV_Target
			{
				fixed4 col = tex2D(_OutputTex, i.uv);
				float2 avatarCoords = i.uv * TextureScale + float2(_XOffset, _YOffset);
				float2 uvs = (i.uv - float2(_XOffset, _YOffset)) * TextureScale;
				float pixelPasses = uvs.x < 1 && uvs.y < 1 && uvs.x > 0 && uvs.y > 0;
				fixed4 avatar = tex2D(_MainTex, uvs) * pixelPasses;
				return avatar + col;
			}
			ENDCG
		}
	}
}
