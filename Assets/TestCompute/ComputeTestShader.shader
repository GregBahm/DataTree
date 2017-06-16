Shader "Unlit/ComputeTestShader"
{
	Properties
	{
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

			struct v2f
			{
				float4 vertex : SV_POSITION;
			};

			StructuredBuffer<float3> _MeshBuffer;
			StructuredBuffer<float2> _PositionsBuffer;

			v2f vert(uint meshId : SV_VertexID, uint instanceId : SV_InstanceID)
			{
				v2f o;
				float3 meshData = _MeshBuffer[meshId];
				float2 positionData = _PositionsBuffer[instanceId];

				o.vertex = UnityObjectToClipPos(meshData / 1 + float3(positionData.x, 0, positionData.y));
				return o;
			}
			
			fixed4 frag (v2f i) : SV_Target
			{
				return 1;
			}
			ENDCG
		}
	}
}
