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
Shader "Custom/FaceWire2DShader" {
	Properties {
		_Color("Main Color", Color) = (1,1,1,1)
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
			};

			// vertex shader outputs ("vertex to fragment")
			struct v2f
			{
				float4 vertex : SV_POSITION; // clip space position
			};
			int _Rotation = 0;
			float _ScaleX = 1.0;
			float _ScaleY = 1.0;
			int _Mirror = 0;
			//int _VerticalMirror = 0;

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
				if (_Mirror == 1)
					o.vertex.x = -o.vertex.x;
				//if (_VerticalMirror == 1)
				//	o.vertex.y = -o.vertex.y;	// image is flipped upside down (depending on pixel formats and devices)
				return o;
			}

			// texture we will sample
			sampler2D _MainTex;
			float _Transparency = 0.5f;

			// pixel shader; returns low precision ("fixed4" type)
			// color ("SV_Target" semantic)
			fixed4 frag(v2f i) : SV_Target
			{
				fixed4 col;
				col.a =  _Transparency ;
				col.r = 0;
				col.g = 0;
				col.b = 1.0;
				return col;
			}

			ENDCG
		}
	} 
}

