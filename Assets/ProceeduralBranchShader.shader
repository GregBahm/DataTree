Shader "Unlit/ProceeduralBranchShader"
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

			float3 _BranchSmallColor;
			float3 _BranchLargeColor;

			float _AvatarSize;

			struct MeshData
			{
				float3 Pos;
				float2 Uvs;
			};


			struct BranchData
			{
				int StartIndex;
				int EndIndex;
			};

			struct BranchPointData
			{
				float3 Pos;
				float Scale;
			};

			StructuredBuffer<MeshData> _MeshBuffer;
			StructuredBuffer<BranchData> _BranchBuffer;
			StructuredBuffer<BranchPointData> _BranchPointBuffer;

			struct v2f
			{
				float4 vertex : SV_POSITION;
				float3 branchColor : TEXCOORD0;
				float2 startClipSpace : TEXCOORD1;
				float2 endClipSpace : TEXCOORD2;
				float2 clipSpace : TEXCOORD3;
				float startScale : TEXCOORD4;
				float endScale : TEXCOORD5;
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

			float3 GetRootPos(float3 startPoint, float3 endPoint, float key)
			{
				float3 eased = lerp(startPoint, endPoint, Ease(key));
				float3 base = lerp(startPoint, endPoint, key);
				return float3(eased.x, base.y, eased.z);
			}

			float3 GetScaler(float startScale, float endScale, float key)
			{
				float3 startScaleVector = float3(startScale, 0, startScale);
				float3 endScaleVector = float3(endScale, 0, endScale);
				float3 eased = lerp(startScaleVector, endScaleVector, Ease(key));
				return eased;
			}

			v2f vert(uint meshId : SV_VertexID, uint instanceId : SV_InstanceID)
			{
				MeshData meshData = _MeshBuffer[meshId];
				BranchData myBranchData = _BranchBuffer[instanceId];
				BranchPointData startData = _BranchPointBuffer[myBranchData.StartIndex];
				BranchPointData endData = _BranchPointBuffer[myBranchData.EndIndex];
				float3 startPoint = startData.Pos;
				float3 endPoint = endData.Pos;
				float startScale = startData.Scale;
				float endScale = endData.Scale;

				v2f o;

				float vertKey = meshData.Uvs.y;
				float3 rootPos = GetRootPos(startPoint, endPoint, vertKey);
				float3 scaler = GetScaler(startScale, endScale, vertKey);
				float3 meshVert = meshData.Pos * scaler;
				float4 newPos = float4(meshVert + rootPos, 1);
				float colorKey = lerp(startScale, endScale, vertKey);
				 
				o.vertex = UnityObjectToClipPos(newPos);
				o.branchColor = lerp(_BranchSmallColor, _BranchLargeColor, colorKey);
				float2 screenRatio = float2(_ScreenParams.x / _ScreenParams.y, 1);
				o.startClipSpace = UnityObjectToClipPos(startPoint).xy * screenRatio;
				o.endClipSpace = UnityObjectToClipPos(endPoint).xy * screenRatio;
				o.clipSpace = UnityObjectToClipPos(newPos).xy * screenRatio;
				o.startScale = startScale;
				o.endScale = endScale;
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				float startClip = length(i.startClipSpace - i.clipSpace) * _AvatarSize - i.startScale;
				float endClip = length(i.endClipSpace - i.clipSpace) * _AvatarSize - i.endScale;
				float finalClip = min(startClip, endClip);
				clip(finalClip);
				return float4(i.branchColor, 1);
			}
			ENDCG
		}
	}
}
