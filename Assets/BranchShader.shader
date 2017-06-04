Shader "Unlit/BranchShader"
{
	Properties
	{
	}
	SubShader
	{
		Tags { "RenderType" = "Opaque" }
		LOD 100

		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			fixed4 _Color;
			float _StartScale;
			float _EndScale;

			float3 _StartPoint;
			float3 _EndPoint;

			float3 _BranchSmallColor;
			float3 _BranchLargeColor;
			float _BranchColorStart;
			float _BranchColorEnd;

			float _AvatarSize;

			struct appdata
			{
				float4 vertex : POSITION;
				float2 uv : TEXCOORD0;
			};

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 branchColor : TEXCOORD0;
				float2 startClipSpace : TEXCOORD1;
				float2 endClipSpace : TEXCOORD2;
				float2 clipSpace : TEXCOORD3;
			};

			float Ease(float t)
			{
				if (t <= 0.5f)
				{
					return 2.0f * (t * t);
				}
				t -= 0.5f;
				return 2.0f * t * (1.0f - t) + 0.5;
			}

			float3 GetRootPos(float key)
			{
				float3 eased = lerp(_StartPoint, _EndPoint, Ease(key));
				float3 base = lerp(_StartPoint, _EndPoint, key);
				return float3(eased.x, base.y, eased.z);
			}

			float3 GetScaler(float key)
			{
				float3 startScale = float3(_StartScale, 0, _StartScale);
				float3 endScale = float3(_EndScale, 0, _EndScale);
				float3 eased = lerp(startScale, endScale, Ease(key));
				return eased;
			}
			
			v2f vert (appdata v)
			{
				v2f o;

				float vertKey = v.uv.y;
				float3 rootPos = GetRootPos(vertKey);
				float3 scaler = GetScaler(vertKey);
				v.vertex.xyz *= scaler;
				float4 newPos = v.vertex + float4(rootPos, 0);
				float colorKey = lerp(_BranchColorStart, _BranchColorEnd, vertKey);
				 
				o.vertex = UnityObjectToClipPos(newPos);
				o.branchColor = lerp(_BranchSmallColor, _BranchLargeColor, colorKey);
				float2 screenRatio = float2(_ScreenParams.x / _ScreenParams.y, 1);
				o.startClipSpace = UnityObjectToClipPos(_StartPoint).xy * screenRatio;
				o.endClipSpace = UnityObjectToClipPos(_EndPoint).xy * screenRatio;
				o.clipSpace = UnityObjectToClipPos(newPos).xy * screenRatio;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float startClip = length(i.startClipSpace - i.clipSpace) * _AvatarSize - _StartScale;
				float endClip = length(i.endClipSpace - i.clipSpace) * _AvatarSize - _EndScale;
				float finalClip = min(startClip, endClip);
				clip(finalClip);
				return float4(i.branchColor, 1);
			}
			ENDCG
		}
	}
}
