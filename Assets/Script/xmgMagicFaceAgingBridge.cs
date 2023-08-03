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


using UnityEngine;
using System.Collections;
using System.Text;
using System;
using System.Runtime.InteropServices;

/**
 * This class contains the interface with the plugin for different platforms
 */
public class xmgMagicFaceAgingBridge
{
    
#if (UNITY_STANDALONE || UNITY_EDITOR || UNITY_ANDROID || UNITY_IOS)

    [DllImport("xzimgMagicFace")]
    public static extern int xzimgMagicFaceAgingInitialize();
   
    [DllImport("xzimgMagicFace")]
    public static extern void xzimgMagicFaceAgingRelease();
  
    [DllImport("xzimgMagicFace")]
    public static extern int xzimgMagicFaceAgingProcess([In][Out] ref xmgMagicFaceBridge.xmgImage imageIn, IntPtr faceFeatures2D,  int nbFaceFeatures, float coefficient, [In][Out] ref xmgMagicFaceBridge.xmgImage imageOut);

#elif UNITY_WEBGL
	[DllImport ("__Internal")] 
    public static extern int xzimgMagicFaceAgingInitialize();

   	[DllImport ("__Internal")] 
    public static extern void xzimgMagicFaceAgingRelease();

  	[DllImport ("__Internal")] 
    public static extern int xzimgMagicFaceAgingProcess([In][Out] ref xmgMagicFaceBridge.xmgImage imageIn, IntPtr faceFeatures2D,  int nbFaceFeatures, float coefficient, [In][Out] ref xmgMagicFaceBridge.xmgImage imageOut);

#endif
}
