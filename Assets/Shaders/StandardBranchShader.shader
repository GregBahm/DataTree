Shader "Custom/StandardBranchShader" 
{
	Properties 
	{
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

		void vert(inout appdata_full v, out Input o) 
		{
			float vertKey = v.texcoord.y;
			float3 rootPos = GetRootPos(vertKey);
			float3 scaler = GetScaler(vertKey);
			v.vertex.xyz *= scaler;
			float4 newPos = v.vertex + float4(rootPos, 0);
			float colorKey = lerp(_BranchColorStart, _BranchColorEnd, vertKey);

			UNITY_INITIALIZE_OUTPUT(Input, o);

			o.branchColor = lerp(_BranchSmallColor, _BranchLargeColor, colorKey);
			v.vertex = newPos;
		}

		void surf (Input IN, inout SurfaceOutput o) 
		{
			o.Albedo = IN.branchColor;
			//o.Albedo = toStart.xxx;
		}
		ENDCG
	}
}
