﻿Shader "Unlit/ProceeduralBranchShader"
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
			Cull Front
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			float3 _BranchSmallColor;
			float3 _BranchLargeColor;
			float3 _BranchTipColor;

			float _BranchHeight;

			float _BranchThickness;
			float _BranchThicknessRamp;
			float _BranchColorRamp;
			float _BranchColorOffset;
			float4x4 _TreeTransform;

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
				int2 CurrentSiblingPressure;
				int2 ChildrenPositionSums;
				float Locked;
			};

			StructuredBuffer<VariableBranchData> _VariableDataBuffer;
			StructuredBuffer<FixedBranchData> _FixedDataBuffer;

			struct v2f
			{
				float4 Vertex : POSITION;
				float3 BranchBaseColor : TEXCOORD0;
				float3 BranchLightColor : TEXCOORD1;
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
				float yPos = level * _BranchHeight + levelOffset * _BranchHeight;
				return float3(xzPos.x, yPos, xzPos.y);
			}

			float3 GetAdjustedNormal(float3 normal, float3 startPoint, float3 endPoint, float key)
			{
				float2 vect = normalize(startPoint.xz - endPoint.xz);
				float dotty = dot(normal.xz, vect);
				float core =  pow(1 - (length(key - .5) * 2), 1);
				float alpha = length(startPoint.xz - endPoint.xz) / (startPoint.y - endPoint.y);
				float3 newNormal = float3(0, dotty * core * alpha, 0);
				return normal - newNormal;
			}

			float3 RotatePointAroundAxis(float3 pos, float3 axis, float angle)
			{
				axis = normalize(axis);
				float s = sin(angle);
				float c = cos(angle);
				float oc = 1.0 - c;

				float4x4 rotationMatrix = float4x4(
					oc * axis.x * axis.x + c, oc * axis.x * axis.y - axis.z * s, oc * axis.z * axis.x + axis.y * s, 0.0,
					oc * axis.x * axis.y + axis.z * s, oc * axis.y * axis.y + c, oc * axis.y * axis.z - axis.x * s, 0.0,
					oc * axis.z * axis.x - axis.y * s, oc * axis.y * axis.z + axis.x * s, oc * axis.z * axis.z + c, 0.0,
					0.0, 0.0, 0.0, 1.0);

				float3 rotated = mul(rotationMatrix, pos);
				return rotated;
			}

			float3 GetRotatedVert(float3 baseMeshPoint, float key, float3 startPoint, float3 endPoint)
			{
				float3 axis = cross(float3(0, 1, 0), (startPoint - endPoint));
				float angle = 1 - (abs(Ease(key) - .5) * 2);
				angle *= 3.141 * .25;
				float3 basePoint = float3(baseMeshPoint.x, 0, baseMeshPoint.z);
				return RotatePointAroundAxis(basePoint, axis, angle);
			}

			v2f vert(appdata_full v, uint instanceId : SV_InstanceID)
			{ 
				v2f o;
				FixedBranchData fixedStartData = _FixedDataBuffer[instanceId];
				VariableBranchData variableStartData = _VariableDataBuffer[instanceId];
				FixedBranchData fixedEndData = _FixedDataBuffer[fixedStartData.ParentIndex];
				VariableBranchData variableEndData = _VariableDataBuffer[fixedStartData.ParentIndex];

				float3 startPoint = GetThreeDeePos(variableStartData.Pos, fixedStartData.BranchLevel, fixedStartData.LevelOffset);
				float3 endPoint = GetThreeDeePos(variableEndData.Pos, fixedEndData.BranchLevel, fixedEndData.LevelOffset);
				float startScale = GetBaseScale(fixedStartData.Scale);
				float endScale = GetBaseScale(fixedEndData.Scale);


				float vertKey = v.texcoord.y;
				float lockVal = lerp(variableStartData.Locked, variableEndData.Locked, vertKey);
				float3 rootPos = GetRootPos(startPoint, endPoint, vertKey);
				float3 scaler = GetScaler(startScale, endScale, vertKey);
				float3 meshVert = v.vertex * scaler;
				meshVert = GetRotatedVert(meshVert, vertKey, startPoint, endPoint);
				float3 newPos = meshVert + rootPos;
				newPos = mul(_TreeTransform, float4(newPos, 1));
				float colorKey = lerp(startScale, endScale, vertKey);
				colorKey = pow(colorKey, _BranchColorRamp) - _BranchColorOffset;
				 
				o.Vertex = mul(UNITY_MATRIX_VP, float4(newPos, 1.0f));
				o.Normal = GetAdjustedNormal(v.normal, startPoint, endPoint, vertKey);
				float branchParam = lerp(fixedStartData.BranchParameter, fixedEndData.BranchParameter, vertKey);
				o.BranchLightColor = _BranchTipColor * pow(branchParam, 4);
				o.BranchBaseColor = lerp(_BranchSmallColor, _BranchLargeColor, colorKey) + lockVal;
				return o;
			}
			
			fixed4 frag(v2f i) : SV_Target
			{
				float shade = dot(normalize(i.Normal), float3(0,.7,.7)) / 2 + .5;
				return float4(i.BranchBaseColor + i.BranchLightColor * shade, 1);
			}
			ENDCG
		}
	}
}
