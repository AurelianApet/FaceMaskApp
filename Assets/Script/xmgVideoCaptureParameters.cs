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
using System.Collections.Generic;
using System.Threading;
using System;

#if UNITY_EDITOR
using UnityEditor;
#endif

public enum xmgVideoPlaneFittingMode
{
    FitScreenHorizontally,
    FitScreenVertically,
};

[System.Serializable]
public class xmgVideoCaptureParameters
{
    [Tooltip("Use Native Capture or Unity WebCameraTexture class")]
    public bool useNativeCapture = false;

    [Tooltip("Video device index \n -1 for automatic research")]
    public int videoCaptureIndex = -1;

    [Tooltip("Video capture mode \n 1 is VGA \n 2 is 720p \n 3 is 1080p")]
    public int videoCaptureMode = 1;
    
    [Tooltip("Use frontal camera (for mobiles only)")]
    public bool UseFrontal = false;

    [Tooltip("Mirror the video")]
    public bool MirrorVideo = false;

    [Tooltip("Choose if the video plane should fit  horizontally or vertically the screen (only relevent in case screen aspect ratio is different from video capture aspect ratio)")]
    public xmgVideoPlaneFittingMode videoPlaneFittingMode = xmgVideoPlaneFittingMode.FitScreenHorizontally;

    [Tooltip("To scale up/down the rendering plane")]
    public float VideoPlaneScale = 1.0f;

    [Tooltip("Camera vertical FOV \nThis value will change the main camera vertical FOV")]
    public float CameraVerticalFOV = 50f;

    [Tooltip("Display debug information")]
    public bool ScreenDebug = true;

    // image is flipped upside down (depending on pixel formats and devices)
    private bool m_isVideoVerticallyFlipped = false;

    public void CheckVideoCaptureParameters()
    {
#if (UNITY_EDITOR || UNITY_STANDALONE || UNITY_WEBGL)
        if (useNativeCapture)
            Debug.Log("xmgVideoCaptureParameters (useNativeCapture) - Video Capture cannot be set to native for PC/MAC platforms => forcing to FALSE");
        if (UseFrontal)
            Debug.Log("xmgVideoCaptureParameters (UseFrontal) - Frontal mode option is not available for PC/MAC platforms - Use camera index edit box instead => forcing to FALSE");
        useNativeCapture = false;
        UseFrontal = false;
#endif

#if (!UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS))
       // useNativeCapture = true;
        if (UseFrontal && !MirrorVideo)
        {
            MirrorVideo = true;
            Debug.Log("xmgVideoCaptureParameters (MirrorVideo) - Mirror mode is forced on mobiles when using frontal camera => forcing to TRUE");       
        }
        if (!UseFrontal && MirrorVideo)
        {
            MirrorVideo = false;
            Debug.Log("xmgVideoCaptureParameters (MirrorVideo) - Mirror mode is deactivate on mobiles when using back camera => forcing to FALSE");       
        }
#endif

#if (!UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS))
        if (useNativeCapture)
            m_isVideoVerticallyFlipped = true;
#endif

        if (videoCaptureMode == 0)
            videoCaptureMode = 1;
#if (!UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS))        
        if (videoCaptureMode == 3)
            videoCaptureMode = 1;
#endif
    }

    public bool GetVerticalMirror() { return m_isVideoVerticallyFlipped;  } 

    public int GetVideoCaptureWidth()
    {
        if (videoCaptureMode == 0) return 320;
        if (videoCaptureMode == 2) return 1280;
        if (videoCaptureMode == 3) return 1920;
        return 640;
    }
    public int GetVideoCaptureHeight()
    {
        if (videoCaptureMode == 0) return 240;
        if (videoCaptureMode == 2) return 720;
        if (videoCaptureMode == 3) return 1080;
        return 480;
    }
    public int GetProcessingWidth()
    {

        if (videoCaptureMode == 0) return 320;
        if (videoCaptureMode == 2) return 640;
        if (videoCaptureMode == 3) return 480;
        return 640;
    }
    public int GetProcessingHeight()
    {
        if (videoCaptureMode == 0) return 240;
        if (videoCaptureMode == 2) return 360;
        if (videoCaptureMode == 3) return 270;
        return 480;
    }

    public float GetVideoAspectRatio()
    {
        return (float)GetVideoCaptureWidth() / (float)GetVideoCaptureHeight();
    }

    public float GetScreenAspectRatio()
    {
        float screen_AR = (float)Screen.width / (float)Screen.height;
        if (Screen.width < Screen.height)
            screen_AR = 1.0f / screen_AR;
        return screen_AR;

    }
    public double GetMainCameraFovV()
    {
        float video_AR = (float)GetVideoAspectRatio();
        float screen_AR = GetScreenAspectRatio();
        double trackingCamera_fovh_radian = xmgTools.ConvertToRadian((double)CameraVerticalFOV);
        double trackingCamera_fovv_radian;
        if (videoPlaneFittingMode == xmgVideoPlaneFittingMode.FitScreenHorizontally)
            trackingCamera_fovv_radian = xmgTools.ConvertHorizontalFovToVerticalFov(trackingCamera_fovh_radian, (double)screen_AR);
        else
            trackingCamera_fovv_radian = xmgTools.ConvertHorizontalFovToVerticalFov(trackingCamera_fovh_radian, (double)video_AR);
        return xmgTools.ConvertToDegree(trackingCamera_fovv_radian);
    }

    // Usefull for portrait and reverse protraits modes
    public double GetPortraitMainCameraFovV()
    {
        float video_AR = (float)GetVideoAspectRatio();
        float screen_AR = GetScreenAspectRatio();

        double trackingCamera_fovh_radian = xmgTools.ConvertToRadian((double)CameraVerticalFOV);
        double trackingCamera_fovv_radian;
        if (videoPlaneFittingMode == xmgVideoPlaneFittingMode.FitScreenHorizontally)
            trackingCamera_fovv_radian = trackingCamera_fovh_radian;
        else
        {
            trackingCamera_fovv_radian = xmgTools.ConvertHorizontalFovToVerticalFov(trackingCamera_fovh_radian, (double)video_AR);
            trackingCamera_fovv_radian = xmgTools.ConvertVerticalFovToHorizontalFov(trackingCamera_fovv_radian, (double)screen_AR);
        }

        return xmgTools.ConvertToDegree(trackingCamera_fovv_radian);
    }


    public double[] GetVideoPlaneScale(double videoPlaneDistance)
    {
        double[] ret = new double[2];

        float video_AR = (float)GetVideoAspectRatio();
        float screen_AR = GetScreenAspectRatio();
        double scale_u, scale_v;

        if (videoPlaneFittingMode == xmgVideoPlaneFittingMode.FitScreenHorizontally)
        {
            double mainCamera_fovv_radian = xmgTools.ConvertToRadian((double)GetMainCameraFovV());
            double mainCamera_fovh_radian = xmgTools.ConvertVerticalFovToHorizontalFov(mainCamera_fovv_radian, (double)screen_AR);
            scale_u = (videoPlaneDistance * Math.Tan(mainCamera_fovh_radian / 2.0));
            scale_v = (videoPlaneDistance * Math.Tan(mainCamera_fovh_radian / 2.0) * 1.0 / video_AR);
        }
        else
        {
            double mainCamera_fovv_radian = xmgTools.ConvertToRadian((double)GetMainCameraFovV());
            scale_u = (videoPlaneDistance * Math.Tan(mainCamera_fovv_radian / 2.0) * video_AR);
            scale_v = (videoPlaneDistance * Math.Tan(mainCamera_fovv_radian / 2.0));
        }
        ret[0] = scale_u;
        ret[1] = scale_v;
        return ret;
    }
}


class xmgDebug
{
    public static string m_debugMessage = "";
}