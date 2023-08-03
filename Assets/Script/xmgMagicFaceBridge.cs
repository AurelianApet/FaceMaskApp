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

public class xmgMagicFaceObject
{
    public xmgMagicFaceBridge.xmgNonRigidFaceData m_faceData;

    public float[] m_dataLandmarks2D;
    protected GCHandle m_dataLandmarks2DHandle;
    public float[] m_dataLandmarks3D;
    protected GCHandle m_dataLandmarks3DHandle;
    public int[] m_dataTriangles;
    protected GCHandle m_dataTrianglesHandle;
    public float[] m_dataKeyLandmarks3D;
    protected GCHandle m_dataKeyLandmarks3DHandle;

    public xmgMagicFaceObject()
    {
        m_dataLandmarks2D = new float[100 * 2];
        m_dataLandmarks2DHandle = GCHandle.Alloc(m_dataLandmarks2D, GCHandleType.Pinned);
        m_dataLandmarks3D = new float[800 * 3];
        m_dataLandmarks3DHandle = GCHandle.Alloc(m_dataLandmarks3D, GCHandleType.Pinned);
        m_dataTriangles = new int[500];
        m_dataTrianglesHandle = GCHandle.Alloc(m_dataTriangles, GCHandleType.Pinned);
        m_dataKeyLandmarks3D = new float[100 * 3];
        m_dataKeyLandmarks3DHandle = GCHandle.Alloc(m_dataKeyLandmarks3D, GCHandleType.Pinned);

        m_faceData.m_landmarks = m_dataLandmarks2DHandle.AddrOfPinnedObject();
        m_faceData.m_landmarks3D = m_dataLandmarks3DHandle.AddrOfPinnedObject();
        m_faceData.m_triangles = m_dataTrianglesHandle.AddrOfPinnedObject();
        m_faceData.m_keyLandmarks3D = m_dataKeyLandmarks3DHandle.AddrOfPinnedObject();
        m_faceData.m_faceDetected = 0;
        m_faceData.m_facePoseComputed = 0;
    }



    ~xmgMagicFaceObject()
    {
        m_dataLandmarks2DHandle.Free();
        m_dataLandmarks3DHandle.Free();
        m_dataTrianglesHandle.Free();
        m_dataKeyLandmarks3DHandle.Free();
    }


}

/**
 * This class contains the interface with the plugin for different platforms
 */
public class xmgMagicFaceBridge
{
    [StructLayout(LayoutKind.Sequential)]
    public struct xmgImage
    {
        public int m_width;                 // Image dimension
        public int m_height;                // Image dimension
        public IntPtr m_imageData;          // Image data
        public int m_iWStep;                // Image Width Step (set to 0 for automatic computation)        
        public int m_colorType;             // pixel format XMG_BW=0, XMG_RGB=1, XMG_BGR=2, XMG_YUV=3, XMG_RGBA=4, XMG_BGRA=5, XMG_ARGB=6  */        
        public int m_type;                  // internal parameter do not change        
        public bool m_flippedHorizontaly;   // True if image is horizontally flipped 
    }

    static public void PrepareImage(ref xmgImage dstimage, int width, int height, int colorType, IntPtr ptrdata)
    {
        dstimage.m_width = width;
        dstimage.m_height = height;
        dstimage.m_colorType = colorType;
        dstimage.m_type = 0;
        dstimage.m_flippedHorizontaly = true;
        dstimage.m_iWStep = 0;
        dstimage.m_imageData = ptrdata;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct xmgRigidFaceData
    {
        public int m_faceDetected;
        public Vector3 m_position;
        public Vector3 m_euler;
        public Quaternion m_quatRot;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct xmgMatrix3x3
    {
        public float x11, x12, x13;
        public float x21, x22, x23;
        public float x31, x32, x33;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct xmgNonRigidFaceData
    {
        public int m_faceDetected;
        public int m_facePoseComputed;

        public Vector3 m_position;
        public Vector3 m_euler;
        public Quaternion m_quatRot;
        public xmgMatrix3x3 m_matRot;

        public int m_nbLandmarks3D;
        public int m_nbLandmarks;
        public IntPtr m_landmarks3D;
        public IntPtr m_landmarks;
        public int m_nbTriangles;
        public IntPtr m_triangles;
        public IntPtr m_keyLandmarks3D;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct xmgVideoCaptureOptions
    {
        public int m_resolutionMode;                // 0 is 320x240; 1, is 640x480; 2 is 720p (-1 if no internal capture)
        public int m_frontal;                       // 0 is frontal; 1 is back
        public int m_focusMode;                     // 0 auto-focus now; 1 auto-focus continually; 2 locked; 3; focus to point
        public int m_exposureMode;                  // 0 auto-focus now; 1 auto-focus continually; 2 locked; 3; focus to point
        public int m_whileBalanceMode;              // 0 auto-focus now; 1 auto-focus continually; 2 locked; 3; focus to point
        public System.IntPtr m_textureVideo;        // Pointer to the video plane texture 
    }
    

	static public void PrepareNativeVideoCapture(ref xmgVideoCaptureOptions videoCaptureOptions, int resolutionMode, int frontal, int focusMode, int exposureMode, int whileBalanceMode, System.IntPtr textureVideo, System.IntPtr textureUV)
	{
		videoCaptureOptions.m_resolutionMode = resolutionMode;
		videoCaptureOptions.m_frontal = frontal;
		videoCaptureOptions.m_focusMode = focusMode;
		videoCaptureOptions.m_exposureMode = exposureMode;
		videoCaptureOptions.m_whileBalanceMode = whileBalanceMode;
		videoCaptureOptions.m_textureVideo = textureVideo;
	}

	static public void PrepareNativeVideoCaptureDefault(ref xmgVideoCaptureOptions videoCaptureOptions, int resolutionMode, int frontal)
	{
		videoCaptureOptions.m_resolutionMode = resolutionMode;
		videoCaptureOptions.m_frontal = frontal;
		videoCaptureOptions.m_focusMode = 1;
		videoCaptureOptions.m_exposureMode = 1;
		videoCaptureOptions.m_whileBalanceMode = 1;
		videoCaptureOptions.m_textureVideo = IntPtr.Zero;
    }

    [StructLayout(LayoutKind.Sequential)]
    public struct xmgInitParams
    {
        public int m_3DFacialFeatures;                                // Detect facial feature in 2D image (simpler) or in 3D space.
        public int m_processingWidth;                                 // Size of the image to process
        public int m_processingHeight;                                // Size of the image to process
        public int m_nbFacialFeatures;                                // Number of facial features to be detected
        public int m_nbMaxFaceObjects;                                // Maximum number of face to be detected simultaneously
        public float m_fovVerticalDegree;                             // fov (vertical) in degree (round 50)
        public System.IntPtr m_videoCaptureOptions;                   // video capture options (if specified internal capture will be used)
    }

    static public void PrepareInitParams(ref xmgInitParams initializationParams, bool detect3DFacialFeatures, int processingWidth, int processingHeight, int nbFacialFeatures, int nbMaxFaceObjects, float fovVerticalDegree, System.IntPtr videoCaptureOptions)
    {
        initializationParams.m_videoCaptureOptions = videoCaptureOptions;
        initializationParams.m_3DFacialFeatures = detect3DFacialFeatures?1:0;
        initializationParams.m_processingWidth = processingWidth;
        initializationParams.m_processingHeight = processingHeight;
        initializationParams.m_nbFacialFeatures = nbFacialFeatures;
        initializationParams.m_nbMaxFaceObjects = nbMaxFaceObjects;
        initializationParams.m_fovVerticalDegree = fovVerticalDegree;
    }

#if ((UNITY_STANDALONE || UNITY_EDITOR || UNITY_ANDROID)) //&& !UNITY_IOS)

    [DllImport("xzimgMagicFace")]
    public static extern int xzimgRigidFaceTrackingInitialize(System.IntPtr bytesRegressor, System.IntPtr bytesClassifier, int processingWidth, int processingHeight, float fovVerticalDegree);
     [DllImport("xzimgMagicFace")]
    public static extern void xzimgRigidFaceTrackingRelease();
    [DllImport("xzimgMagicFace")]
    public static extern int xzimgRigidFaceTrackingProcess([In][Out] ref xmgImage imageIn, int orientation, [In][Out] ref xmgNonRigidFaceData rigidData);

    [DllImport("xzimgMagicFace")]
	public static extern int xzimgMagicFaceInitialize(System.IntPtr bytesRegressor, System.IntPtr bytesClassifier, System.IntPtr bytes3DModel, [In][Out] ref xmgInitParams initializationParams);
    [DllImport("xzimgMagicFace")]
    public static extern void xzimgMagicFaceRelease();
    [DllImport("xzimgMagicFace")]
    public static extern void xzimgMagicFacePause(int pause);
    [DllImport("xzimgMagicFace")]
    public static extern int xzimgMagicFaceDetectNonRigidFaces2D([In][Out] ref xmgImage imageIn, int orientation);
    [DllImport("xzimgMagicFace")]
    public static extern int xzimgMagicFaceTrackNonRigidFaces([In][Out] ref xmgImage imageIn, int orientation);
    [DllImport("xzimgMagicFace")]
    public static extern int xzimgMagicFaceGetFaceData(int idxObject, [In][Out] ref xmgNonRigidFaceData nonRigidData);
    [DllImport("xzimgMagicFace")]
    public static extern int xzimgMagicFaceDelaunayTriangulation2D(IntPtr vertices2D, int nbVertices, IntPtr outTriangles, IntPtr nbTriangles, int fillEyes, int fillMouth);
    //[DllImport("xzimgMagicFace")]
   // public static extern int xzimgGetNonRigidFullModel(float** oVertices, int* oNbVertices, int** oTriangles, int* oNbTriangles);


    public static void xzimgMagicFaceInitializeVideoCapture(int cameraMode, bool isFrontal)
    {
        AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject jo = jc.GetStatic<AndroidJavaObject>("currentActivity");
        jo.Call("xzimgMagicFaceInitializeVideoCapture", cameraMode, isFrontal);
    }

    public static void xzimgMagicFaceReleaseVideoCapture()
    {
        AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject jo = jc.GetStatic<AndroidJavaObject>("currentActivity");
        jo.Call("xzimgMagicFaceReleaseVideoCapture");
    }

    public static void xzimgMagicFaceTextureVideo(System.IntPtr textureID, int idxOrientation)
    {
        AndroidJavaClass jc = new AndroidJavaClass("com.unity3d.player.UnityPlayer");
        AndroidJavaObject jo = jc.GetStatic<AndroidJavaObject>("currentActivity");
        jo.Call("xzimgMagicFaceTextureVideo", textureID.ToInt32(), idxOrientation);
    }

#elif UNITY_WEBGL
   
	[DllImport ("__Internal")] 
	public static extern int xzimgRigidFaceTrackingInitialize(System.IntPtr bytesRegressor, System.IntPtr bytesClassifier, int processingWidth, int processingHeight, float fovVerticalDegree);
	[DllImport ("__Internal")] 
    public static extern void xzimgRigidFaceTrackingRelease();
    [DllImport ("__Internal")] 
    public static extern int xzimgRigidFaceTrackingProcess([In][Out] ref xmgImage imageIn, int orientation, [In][Out] ref xmgNonRigidFaceData rigidData);

    [DllImport ("__Internal")] 
	public static extern int xzimgMagicFaceInitialize(System.IntPtr bytesRegressor, System.IntPtr bytesClassifier, System.IntPtr bytes3DModel, [In][Out] ref xmgInitParams initializationParams);
    [DllImport ("__Internal")] 
    public static extern void xzimgMagicFaceRelease();
    [DllImport ("__Internal")] 
    public static extern void xzimgMagicFacePause(int pause);
    [DllImport ("__Internal")] 
    public static extern int xzimgMagicFaceDetectNonRigidFaces2D([In][Out] ref xmgImage imageIn, int orientation);
    [DllImport ("__Internal")] 
    public static extern int xzimgMagicFaceTrackNonRigidFaces([In][Out] ref xmgImage imageIn, int orientation);
    [DllImport ("__Internal")] 
    public static extern int xzimgMagicFaceGetFaceData(int idxObject, [In][Out] ref xmgNonRigidFaceData nonRigidData);
    [DllImport ("__Internal")] 
    public static extern int xzimgMagicFaceDelaunayTriangulation2D(IntPtr vertices2D, int nbVertices, IntPtr outTriangles, IntPtr nbTriangles, int fillEyes, int fillMouth);
#elif (UNITY_IOS)

	[DllImport("__Internal")]
	public static extern int xzimgRigidFaceTrackingInitialize(System.IntPtr bytesRegressor, System.IntPtr bytesClassifier, int processingWidth, int processingHeight, float fovVerticalDegree);
	[DllImport("__Internal")]
	public static extern void xzimgRigidFaceTrackingRelease();
	[DllImport("__Internal")]
	public static extern int xzimgRigidFaceTrackingProcess([In][Out] ref xmgImage imageIn, int orientation, [In][Out] ref xmgNonRigidFaceData rigidData);


	[DllImport ("__Internal")]	
	public static extern int xzimgMagicFaceInitialize(System.IntPtr bytesRegressor, System.IntPtr bytesClassifier, System.IntPtr bytes3DModel, [In][Out] ref xmgInitParams initializationParams);
	[DllImport ("__Internal")]
	public static extern void xzimgMagicFaceRelease();
    [DllImport ("__Internal")] 
    public static extern void xzimgMagicFacePause(int pause);
	[DllImport("__Internal")]
	public static extern int xzimgMagicFaceDetectNonRigidFaces2D([In][Out] ref xmgImage imageIn, int orientation);
	[DllImport ("__Internal")]
	public static extern int xzimgMagicFaceTrackNonRigidFaces([In][Out] ref xmgImage imageIn, int orientation);
	[DllImport ("__Internal")]
	public static extern int xzimgMagicFaceGetFaceData(int idxObject, [In][Out] ref xmgNonRigidFaceData nonRigidData);
	[DllImport ("__Internal")]
    public static extern int xzimgMagicFaceDelaunayTriangulation2D(IntPtr vertices2D, int nbVertices, IntPtr outTriangles, IntPtr nbTriangles, int fillEyes, int fillMouth);

	[DllImport ("__Internal")]
	public static extern void xzimgMagicFaceTextureVideo(IntPtr textureID, int deviceOrientation);


#endif
}
