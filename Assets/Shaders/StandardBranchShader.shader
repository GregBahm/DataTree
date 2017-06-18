Shader "Custom/StandardBranchShader" 
{
	Properties 
	{
		_RotationTest("RotationTest", Float) = 0
	}
	SubShader 
	{
		Tags { "RenderType"="Opaque" }
		LOD 200
		
		CGPROGRAM
		#pragma surface surf Lambert vertex:vert
		#pragma target 3.0

		sampler2D _MainTex;

		struct Input 
		{
			float3 branchColor;
		};

		fixed4 _Color;
		UNITY_INSTANCING_CBUFFER_START(Props)
		UNITY_INSTANCING_CBUFFER_END

		float _StartScale;
		float _EndScale;

		float3 _StartPoint;
		float3 _EndPoint;

		float3 _BranchSmallColor;
		float3 _BranchLargeColor;
		float _BranchColorStart;
		float _BranchColorEnd;

		float _RotationTest;

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
		
		float4 RotatePointAroundAxis(float4 pos, float3 axis, float angle)
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

			float4 rotated = mul(rotationMatrix, pos);
			return rotated;
		}

		void vert(inout appdata_full v, out Input o) 
		{
			float vertKey = v.texcoord.y;
			float3 axis = cross(float3(0, 1, 0), (_StartPoint - _EndPoint));
			float angle = 1 - (abs(Ease(vertKey) - .5) * 2);
			angle *= 3.141 * -.25;
			float4 basePoint = float4(v.vertex.x, 0, v.vertex.z, v.vertex.w);
			float4 rotated = RotatePointAroundAxis(basePoint, axis, angle);

			float3 rootPos = GetRootPos(vertKey);
			float3 scaler = GetScaler(vertKey);
			v.vertex.xyz *= scaler;
			float4 newPos = rotated + float4(rootPos, 0);
			float colorKey = lerp(_BranchColorStart, _BranchColorEnd, vertKey);

			UNITY_INITIALIZE_OUTPUT(Input, o);

			o.branchColor = lerp(_BranchSmallColor, _BranchLargeColor, colorKey);
			v.vertex = newPos;
		}

		void surf (Input IN, inout SurfaceOutput o) 
		{
			o.Albedo = 1;
		}
		ENDCG
	}
} 
