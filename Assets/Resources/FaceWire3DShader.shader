// Upgrade NOTE: replaced 'mul(UNITY_MATRIX_MVP,*)' with 'UnityObjectToClipPos(*)'

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

Shader "Custom/FaceWire3DShader" {
	Properties {
		_Color("Main Color", Color) = (1,1,1,1)
	}
	SubShader {
		Tags { "Queue"="Geometry+1"  "RenderType" = "Transparent" }
		LOD 100
		
		Pass { 
			ZWrite On
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

			// vertex shader
			v2f vert(appdata v)
			{
				v2f o;
				o.vertex = UnityObjectToClipPos(v.vertex);
				return o;
			}

			// texture we will sample
			float _Transparency = 0.5f;

			// pixel shader; returns low precision ("fixed4" type)
			// color ("SV_Target" semantic)
			fixed4 frag(v2f i) : SV_Target
			{
				// sample texture and return it
				fixed4 col;
				col.a = _Transparency;
				col.r = 0;
				col.g = 0;
				col.b = 1.0;
				return col;
			}

			ENDCG
		}
	} 
}

