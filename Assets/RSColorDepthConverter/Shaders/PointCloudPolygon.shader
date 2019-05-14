// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

Shader "Custom/PointCloudPolygon"
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
		_AlphaOffsetX("AlphaOffsetX", Range(0, 1)) = 0.005
		_AlphaOffsetY("AlphaOffsetY", Range(0, 1)) = 0.006
		_DiffThreshold("DiffThreshold", Range(0, 10)) = 0.2
		_PolygonQuality("PolygonQuality", Int) = 1
		_ScaleBias("ScaleBias", Range(0, 1000)) = 0.001
		_OffsetZ("OffsetZ", Range(-10, 10)) = 1.0
		_WindowSize("WindowSize", Range(0, 1)) = 0.05
	}
	SubShader
	{
		Tags { "RenderType"="Transparent" }
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
				float2 uv	  : TEXCOORD0;
				float2 uv2	  : TEXCOORD1;
			};

			sampler2D _SpriteTex;
			sampler2D _DepthTex;
			float4 _SpriteTex_ST;
			float _FX;
			float _FY;
			float _PPX;
			float _PPY;
			float _PolygonSizeX;
			float _PolygonSizeY;
			float _AlphaOffsetX;
			float _AlphaOffsetY;
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



			v2g vert (appdata v)
			{
				v2g o;
//				float d = tex2Dlod(_DepthTex, float4(v.uv.xy, 0, 0)).r * _ScaleBias;
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

//				d[0] = tex2Dlod(_DepthTex, float4(uv[0].xy, 0, 0)).r * _ScaleBias;	// right up
//				d[1] = tex2Dlod(_DepthTex, float4(uv[1].xy, 0, 0)).r * _ScaleBias;	// right down 
//				d[2] = tex2Dlod(_DepthTex, float4(uv[2].xy, 0, 0)).r * _ScaleBias;	// left up
//				d[3] = tex2Dlod(_DepthTex, float4(uv[3].xy, 0, 0)).r * _ScaleBias;	// left down

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
						out_pos[j] = float4 (i[0].vertex.z * (_PPX - uv[j].x) / _FX,
											i[0].vertex.z * (_PPY - uv[j].y) / _FY,
											i[0].vertex.z - _OffsetZ,
											1.0);
					}
				}

				if (point_score >= _PolygonQuality)
				{
					g2f out_v;

					for (int j = 0; j < 4; j++)
					{
						// tex1.x is used for depth threshold flag
						out_v.vertex = UnityObjectToClipPos(out_pos[j]);
						out_v.uv = uv[j];
						out_v.uv2 = float2(0, i[0].vertex.z);
						o.Append(out_v);
					}
				}
			}

			fixed4 frag (g2f i) : COLOR
			{
				// sample the texture
				fixed4 col = tex2D(_SpriteTex, i.uv);
				float depth = tex2D(_DepthTex, i.uv).r;
				if (depth < 0.05) discard;
//				if (col.a == 0.0f || (col.g == 1.0 && col.r < 0.05 && col.b < 0.05)) discard;
				fixed4 ret_col = fixed4(col.b, col.g, col.r, col.a);
/*
				float around_a[8];
				around_a[0] = tex2Dlod(_SpriteTex, float4(i.uv.x - _AlphaOffsetX, i.uv.y -_AlphaOffsetX, 0, 0)).a;
				around_a[1] = tex2Dlod(_SpriteTex, float4(i.uv.x		   , i.uv.y - _AlphaOffsetX, 0, 0)).a;
				around_a[2] = tex2Dlod(_SpriteTex, float4(i.uv.x + _AlphaOffsetX, i.uv.y - _AlphaOffsetX, 0, 0)).a;
				around_a[3] = tex2Dlod(_SpriteTex, float4(i.uv.x		   , i.uv.y			, 0, 0)).a;
				around_a[4] = tex2Dlod(_SpriteTex, float4(i.uv.x + _AlphaOffsetX, i.uv.y			, 0, 0)).a;
				around_a[5] = tex2Dlod(_SpriteTex, float4(i.uv.x - _AlphaOffsetX, i.uv.y + _AlphaOffsetX, 0, 0)).a;
				around_a[6] = tex2Dlod(_SpriteTex, float4(i.uv.x		   , i.uv.y + _AlphaOffsetX, 0, 0)).a;
				around_a[7] = tex2Dlod(_SpriteTex, float4(i.uv.x + _AlphaOffsetX, i.uv.y + _AlphaOffsetX, 0, 0)).a;

				col.a = (around_a[0] + around_a[1] + around_a[2] + around_a[3] + around_a[4] + around_a[5] + around_a[6] + around_a[7]) * 0.125f;
*/
				return ret_col;
			}
			ENDCG
		}
	}
}
