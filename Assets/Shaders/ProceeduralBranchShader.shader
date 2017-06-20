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
				float3 Color;
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
				meshVert = GetRotatedVert(meshVert, vertKey, startPoint, endPoint);
				float4 newPos = float4(meshVert + rootPos, 1);
				float colorKey = lerp(startScale, endScale, vertKey);
				colorKey = pow(colorKey, _BranchColorRamp) - _BranchColorOffset;
				 
				o.Vertex = UnityObjectToClipPos(newPos);
				o.Normal = GetAdjustedNormal(meshData.Normal, startPoint, endPoint, vertKey);
				float branchParam = lerp(fixedStartData.BranchParameter, fixedEndData.BranchParameter, vertKey);
				o.BranchLightColor = _BranchTipColor * pow(branchParam, 4);
				o.BranchBaseColor = lerp(_BranchSmallColor, _BranchLargeColor, colorKey);
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target 
			{
				float shade = dot(normalize(i.Normal), float3(0,.7,.7)) / 2 + .5;
				return float4(i.BranchBaseColor + i.BranchLightColor * shade, 1);
			}
			ENDCG
		}
		Pass
		{
			CGPROGRAM
			#pragma vertex vert
			#pragma fragment frag

			#include "UnityCG.cginc"

			float _AvatarSize;

			float _BranchHeight;

			struct MeshData
			{
				float3 Pos;
				float2 Uvs;
				float3 Normal;
				float3 Color;
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

			StructuredBuffer<MeshData> _CardMeshBuffer;
			StructuredBuffer<VariableBranchData> _VariableDataBuffer;
			StructuredBuffer<FixedBranchData> _FixedDataBuffer;

			struct v2f
			{
				float4 Vertex : SV_POSITION;
				float Alpha : COLOR0;
				float2 Uvs : TEXCOORD0;
			};

			float3 GetThreeDeePos(float2 xzPos, int level, float levelOffset)
			{
				float yPos = level * _BranchHeight + levelOffset * _BranchHeight;
				return float3(xzPos.x, yPos, xzPos.y);
			}

			v2f vert(uint meshId : SV_VertexID, uint instanceId : SV_InstanceID)
			{ 
				v2f o;
				MeshData meshData = _CardMeshBuffer[meshId];
				FixedBranchData fixedStartData = _FixedDataBuffer[instanceId];
				VariableBranchData variableStartData = _VariableDataBuffer[instanceId];
;
				float3 endPoint = GetThreeDeePos(variableStartData.Pos, fixedStartData.BranchLevel, fixedStartData.LevelOffset);
				o.Vertex = mul(UNITY_MATRIX_P, mul(UNITY_MATRIX_V, float4(endPoint, 1)) + float4(meshData.Pos * _AvatarSize, 0));
				o.Vertex.z += .04;
				o.Alpha = meshData.Color.x;
				o.Uvs = meshData.Uvs;
				return o;
			} 
			
			fixed4 frag (v2f i) : SV_Target 
			{ 
				return float4(i.Uvs, 0, 1);
			}
			ENDCG
		}
	}
}
