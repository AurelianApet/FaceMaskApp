/**
*
* Copyright (c) 2016 xzimg Limited , All Rights Reserved
* No part of this software and related documentation may be used, copied,
* modified, distributed and transmitted, in any form or by any means,
* without the prior written permission of XZIMG Limited
*
* contact@xzimg.com, www.xzimg.com
*
*/

Shader "Custom/Face2DShader" {
	Properties {
		_Color("Main Color", Color) = (1,1,1,1)
		_MainTex("Base (RGB) Trans (A)", 2D) = "white" {}
	}
	SubShader {
		Tags { "Queue"="Geometry+1"  "RenderType" = "Transparent" }
		LOD 100
		
		Pass { 
			ZWrite Off
			ZTest Always
			//AlphaTest Greater [_Cutoff]
			Blend SrcAlpha OneMinusSrcAlpha

			CGPROGRAM
			// use "vert" function as the vertex shader
			#pragma vertex vert
			// use "frag" function as the pixel (fragment) shader
			#pragma fragment frag

			// vertex shader inputs
			struct appdata
			{
				float4 vertex : POSITION; // vertex position
				float2 uv : TEXCOORD0; // texture coordinate
			};

			// vertex shader outputs ("vertex to fragment")
			struct v2f
			{
				float2 uv : TEXCOORD0; // texture coordinate
				float4 vertex : SV_POSITION; // clip space position
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
					o.vertex.x = o.vertex.y;
					o.vertex.y = -tmp;
				}
				else if (_Rotation == 2)
				{
					o.vertex.x = -o.vertex.x;
					o.vertex.y = -o.vertex.y;
				}
				else if (_Rotation == 3)
				{
					float tmp = o.vertex.x;
					o.vertex.x = -o.vertex.y;
					o.vertex.y = tmp;
				}
				// just pass the texture coordinate
				o.uv = v.uv;
				if (_Mirror == 1)
					o.vertex.x = -o.vertex.x;
				if (_VerticalMirror == 1)			//NB
					o.vertex.y = -o.vertex.y;	// image is flipped upside down (depending on pixel formats and devices)
				return o;
			}

			// texture we will sample
			sampler2D _MainTex;
			//float _Transparency = 0.5f;
            float _Transparency = 1;

			// pixel shader; returns low precision ("fixed4" type)
			// color ("SV_Target" semantic)
			fixed4 frag(v2f i) : SV_Target
			{
				// sample texture and return it
				fixed4 col = tex2D(_MainTex, i.uv);
				col.a =  _Transparency * col.a;
				return col;
			}

			ENDCG
		}
	} 
}

