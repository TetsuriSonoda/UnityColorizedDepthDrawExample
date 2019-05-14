// Upgrade NOTE: replaced '_World2Object' with 'unity_WorldToObject'

// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/PointCloudShadedPolygon"
{
	Properties
	{
		_SpriteTex("RGBTexture", 2D) = "white" {}
		_DepthTex("DepthTexture", 2D) = "white" {}
		_FX("Focal length X", Range(0, 100)) = 7.584
		_FY("Focal length Y", Range(0, 100)) = 6.132
		_PPX("Center X", Range(0, 10000)) = 0.5
		_PPY("Center Y", Range(0, 10000)) = 0.5
		_PolygonSizeX("PolygonSizeX", Range(0, 1)) = 0.005
		_PolygonSizeY("PolygonSizeY", Range(0, 1)) = 0.006
		_DiffThreshold("DiffThreshold", Range(0, 10)) = 0.2
		_PolygonQuality("PolygonQuality", Int) = 1
		_ScaleBias("ScaleBias", Range(0, 1000)) = 0.001
		_OffsetZ("OffsetZ", Range(-10, 10)) = 0.0
		_WindowSize("WindowSize", Range(0, 1)) = 0.05
	}
		SubShader
	{
		Tags{ "RenderType" = "Transparent" }
		Blend SrcAlpha OneMinusSrcAlpha

		Pass
	{
		CGPROGRAM
		//			#pragma target 5.0
#pragma vertex vert
#pragma fragment frag
#pragma geometry geom

		struct appdata
	{
		float4 vertex : POSITION;
		float2 uv	  : TEXCOORD0;
	};

	struct v2g
	{
		float4 vertex : POSITION;
		float2 uv	  : TEXCOORD0;
	};

	struct g2f
	{
		float4 vertex : SV_POSITION;
		float4 color  : COLOR;
		float2 uv	  : TEXCOORD0;
		float2 uv2	  : TEXCOORD1;
	};

	// Unity Defined Variables	
	uniform float4 _LightColor0;

	sampler2D _SpriteTex;
	sampler2D _DepthTex;
	float4 _SpriteTex_ST;
	float _FX;
	float _FY;
	float _PPX;
	float _PPY;
	float _PolygonSizeX;
	float _PolygonSizeY;
	float _DiffThreshold;
	float _ScaleBias;
	float _WindowSize;
	float _OffsetZ;
	int _PolygonQuality;

	float SimpleMedianFilter(float2 in_xy, float window_size)
	{
		float d0 = tex2Dlod(_DepthTex, float4(float2(in_xy.x - window_size, in_xy.y), 0, 0)).r * _ScaleBias;
		float d1 = tex2Dlod(_DepthTex, float4(float2(in_xy.x, in_xy.y), 0, 0)).r * _ScaleBias;
		float d2 = tex2Dlod(_DepthTex, float4(float2(in_xy.x + window_size, in_xy.y), 0, 0)).r * _ScaleBias;

		int num = (d0 > 0) + (d1 > 0) + (d2 > 0);
		if (num == 0) return 0;
		else return (d0 + d1 + d2) / num;

		if (d2 > d0 && d2 < d1)
		{
			return d2;
		}
		else if (d1 > d0 && d1 < d2)
		{
			return d1;
		}
		else
		{
			return d0;
		}
	};

	v2g vert(appdata v)
	{
		v2g o;
		float d = SimpleMedianFilter(v.uv.xy, _WindowSize);

		o.vertex.z = d;
		o.vertex.x = d * (_PPX - v.uv.x) / _FX;
		o.vertex.y = d * (_PPY - v.uv.y) / _FY;
		o.vertex.w = 1.0f;

		//o.vertex = v.vertex; // UnityObjectToClipPos(o.vertex);
		o.uv = v.uv;
		return o;
	}

	[maxvertexcount(4)]
	void geom(point v2g i[1], inout TriangleStream<g2f> o)
	{
		if (i[0].vertex.z < 0.01f) return;

		float2 uv[4];
		float d[4];
		float4 out_pos[4];

		// I. 4 point depth reference
		uv[0] = float2(i[0].uv.x - _PolygonSizeX, i[0].uv.y - _PolygonSizeY);	// right up
		uv[1] = float2(i[0].uv.x - _PolygonSizeX, i[0].uv.y + _PolygonSizeY);	// right down
		uv[2] = float2(i[0].uv.x + _PolygonSizeX, i[0].uv.y - _PolygonSizeY);	// left up
		uv[3] = float2(i[0].uv.x + _PolygonSizeX, i[0].uv.y + _PolygonSizeY);	// left down

		d[0] = SimpleMedianFilter(uv[0], _WindowSize);	// right up
		d[1] = SimpleMedianFilter(uv[1], _WindowSize);	// right down
		d[2] = SimpleMedianFilter(uv[2], _WindowSize);	// left up
		d[3] = SimpleMedianFilter(uv[3], _WindowSize);	// left down

		// II. Boundary scoring
		int point_score = 0;

		for (int j = 0; j < 4; j++)
		{
			if (abs(d[j] - i[0].vertex.z) < _DiffThreshold * i[0].vertex.z && d[j] > 0)
			{
				out_pos[j] = float4 (d[j] * (_PPX - uv[j].x) / _FX,
					d[j] * (_PPY - uv[j].y) / _FY,
					d[j] - _OffsetZ,
					1.0);
				point_score++;
			}
			else
			{
				out_pos[j] = float4 (i[0].vertex.z * (_PPX- uv[j].x) / _FX,
					i[0].vertex.z * (_PPY - uv[j].y) / _FY,
					i[0].vertex.z - _OffsetZ,
					1.0);
			}
		}


		if (point_score >= _PolygonQuality)
		{
			g2f out_v;
			// compute normal
			float4 vertex_normal[4];
			vertex_normal[0] = float4(normalize(cross((float3)(out_pos[2] - out_pos[0]), (float3)(out_pos[0] - out_pos[1]))), 1);
			vertex_normal[1] = float4(normalize(cross((float3)(out_pos[1] - out_pos[3]), (float3)(out_pos[1] - out_pos[0]))), 1);
			vertex_normal[2] = float4(normalize(cross((float3)(out_pos[2] - out_pos[0]), (float3)(out_pos[2] - out_pos[3]))), 1);
			vertex_normal[3] = float4(normalize(cross((float3)(out_pos[3] - out_pos[1]), (float3)(out_pos[2] - out_pos[3]))), 1);

			for (int j = 0; j < 4; j++)
			{
				float3 normalDirection = normalize(mul(float4(vertex_normal[j].x, vertex_normal[j].y, -vertex_normal[j].z, vertex_normal[j].w), unity_WorldToObject).xyz);
				float3 lightDirection = normalize(_WorldSpaceLightPos0.xyz);
				float3 diffuseReflection = _LightColor0.xyz * max(0.0, dot(normalDirection, lightDirection));

				// tex1.x is used for depth threshold flag
				out_v.vertex = UnityObjectToClipPos(out_pos[j]);
				out_v.color = float4(diffuseReflection, 1);
				out_v.uv = uv[j];
				out_v.uv2 = float2(0, i[0].vertex.z);
				o.Append(out_v);
			}
		}
	}

	fixed4 frag(g2f i) : COLOR
	{
		// sample the texture
		fixed4 col = tex2D(_SpriteTex, i.uv);
		if (col.a == 0.0f || (col.g == 1.0 && col.r == 0.0 && col.b == 0.0)) discard;
		fixed4 ret_col = fixed4(i.color.r, i.color.g, i.color.b, i.color.a);

		return ret_col;
	}
		ENDCG
	}
	}
}
