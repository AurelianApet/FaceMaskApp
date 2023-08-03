/**
*
* Copyright (c) 2016 xzimg Limited , All Rights Reserved
* No part of this software and related documentation may be used, copied,
* modified, distributed and transmitted, in any form or by any means,
* without the prior written permission of xzimg
*
* contact@xzimg.com, www.xzimg.com
*
*/

Shader "Custom/YUVVideoShader" {
	Properties {
		[NoScaleOffset] _MainTex("Texture", 2D) = "white" {}
	}
	SubShader {
		Tags { "Queue"="Geometry" }
		LOD 100
		
		Pass { 
			Lighting Off
			ZWrite Off
			ZTest Always

			CGPROGRAM
			// use "vert" function as the vertex shader
			#pragma vertex vert
			// use "frag" function as the pixel (fragment) shader
			#pragma fragment frag

			// vertex shader inputs
			struct appdata
			{
				float4 vertex : POSITION;	// vertex position
				float2 uv : TEXCOORD0;		// texture coordinate
			};

			// vertex shader outputs ("vertex to fragment")
			struct v2f
			{
				float2 uv : TEXCOORD0;			// texture coordinate
				float4 vertex : SV_POSITION;	// clip space position
			};

			int _Rotation = 0;
			float _ScaleX = 1.0;
			float _ScaleY = 1.0;
			int _Mirror = 0;
			int _VerticalMirror = 0;

			// vertex shader
			v2f vert(appdata v)
			{
				v2f o;
				// transform position to clip space
				o.vertex = float4(v.vertex.x*_ScaleX, v.vertex.y*_ScaleY, 0.0, 1.0);

				if (_Rotation == 1)
				{
					float tmp = o.vertex.x;
					o.vertex.x = -o.vertex.y;
					o.vertex.y = tmp;
				}
				else if (_Rotation == 2)
				{
					o.vertex.x = -o.vertex.x;
					o.vertex.y = -o.vertex.y;
				}
				else if (_Rotation == 3)
				{
					float tmp = o.vertex.x;
					o.vertex.x = o.vertex.y;
					o.vertex.y = -tmp;
				}

				// pass the texture coordinate
				o.uv = v.uv;
				if (_Mirror == 1)
					o.uv.x = 1.0 - o.uv.x;
				if (_VerticalMirror == 1)
					o.uv.y = 1.0 - o.uv.y;		// image is flipped upside down (depending on pixel formats and devices)
				return o;
			}

			// texture we will sample
			sampler2D _MainTex;
			uniform sampler2D _UVTex;

			fixed4 frag(v2f i) : SV_Target
			{
				// sample texture and return it
				float y = tex2D(_MainTex, i.uv).r;
				float u = tex2D(_UVTex, i.uv).a - 0.5;
				float v = tex2D(_UVTex, i.uv).r - 0.5;

				float r = y + 1.370705*v;
				float g = y - 0.337633*u - 0.698001*v;
				float b = y + 1.732446*u;
				return fixed4(b, g, r, 1.0);

				// 2nd option depending on the yuv mode
				//float r = y + 1.13983*v;
				//float g = y - 0.39465*u - 0.58060*v;
				//float b = y + 2.03211*u;
			}

			ENDCG
		}
	} 
}

