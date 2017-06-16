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
			float3 _BranchTipColor;

			float _AvatarSize;

			float _BranchHeight;

			float _BranchThickness;
			float _BranchThicknessRamp;
			float _BranchColorRamp;
			float _BranchColorOffset;

			struct MeshData
			{
				float3 Pos;
				float2 Uvs;
				float3 Normal;
			};
			struct FixedBranchData
			{
				int ParentIndex;
				int ImmediateChildenCount;
				int BranchLevel;
				float LevelOffset;
				float BranchParameter;
				int Scale;
			};
			struct VariableBranchData
			{
				float2 Pos;
				float2 CurrentSiblingPressure;
			};

			StructuredBuffer<MeshData> _MeshBuffer;
			StructuredBuffer<VariableBranchData> _VariableDataBuffer;
			StructuredBuffer<FixedBranchData> _FixedDataBuffer;

			struct v2f
			{
				float4 Vertex : SV_POSITION;
				float3 BranchColor : TEXCOORD0;
				float3 Normal : NORMAL;
			};

			float GetBaseScale(int scaleBase)
			{
				return pow(scaleBase * _BranchThickness, _BranchThicknessRamp);
			}

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

			float3 GetThreeDeePos(float2 xzPos, int level, float levelOffset)
			{
				float yPos = level * _BranchHeight + levelOffset;
				return float3(xzPos.x, yPos, xzPos.y);
			}

			float3 GetAdjustedNormal(float3 normal, float3 startPoint, float3 endPoint, float key)
			{
				float2 vect = normalize(startPoint.xz - endPoint.xz);
				float dotty = dot(normal.xz, vect);
				float core =  pow(1 - (length(key - .5) * 2), 1);
				float alpha = length(startPoint.xz - endPoint.xz) / (startPoint.y - endPoint.y);
				float3 newNormal = float3(0, dotty * core * alpha, 0);
				//return abs(alpha);
				return normal - newNormal;
			}

			v2f vert(uint meshId : SV_VertexID, uint instanceId : SV_InstanceID)
			{ 
				v2f o;
				MeshData meshData = _MeshBuffer[meshId];
				FixedBranchData fixedStartData = _FixedDataBuffer[instanceId];
				VariableBranchData variableStartData = _VariableDataBuffer[instanceId];
				FixedBranchData fixedEndData = _FixedDataBuffer[fixedStartData.ParentIndex];
				VariableBranchData variableEndData = _VariableDataBuffer[fixedStartData.ParentIndex];

				float3 startPoint = GetThreeDeePos(variableStartData.Pos, fixedStartData.BranchLevel, fixedStartData.LevelOffset);
				float3 endPoint = GetThreeDeePos(variableEndData.Pos, fixedEndData.BranchLevel, fixedEndData.LevelOffset);
				float startScale = GetBaseScale(fixedStartData.Scale);
				float endScale = GetBaseScale(fixedEndData.Scale);

				float vertKey = meshData.Uvs.y;
				float3 rootPos = GetRootPos(startPoint, endPoint, vertKey);
				float3 scaler = GetScaler(startScale, endScale, vertKey);
				float3 meshVert = meshData.Pos * scaler;
				float4 newPos = float4(meshVert + rootPos, 1);
				float colorKey = lerp(startScale, endScale, vertKey);
				colorKey = pow(colorKey, _BranchColorRamp) - _BranchColorOffset;
				 
				o.Vertex = UnityObjectToClipPos(newPos);
				o.Normal = GetAdjustedNormal(meshData.Normal, startPoint, endPoint, vertKey);
				float branchParam = lerp(fixedStartData.BranchParameter, fixedEndData.BranchParameter, vertKey);
				branchParam = pow(branchParam, 10);
				float3 branchColor = lerp(_BranchSmallColor, _BranchLargeColor, colorKey);
				o.BranchColor = lerp(branchColor, _BranchTipColor, branchParam);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target 
			{
				//return float4(pow(i.BranchColor, 2), 1);
				float shade = dot(normalize(i.Normal), float3(0,.7,.7)) / 2 + .5;
				return float4(i.BranchColor + i.BranchColor * shade, 1);
			}
			ENDCG
		}
	}
}
