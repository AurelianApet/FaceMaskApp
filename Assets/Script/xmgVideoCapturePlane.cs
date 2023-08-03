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
using System;
using System.Runtime.InteropServices;
using System.Collections;
using System.Collections.Generic;	//List
using System.Text;

public class xmgVideoCapturePlane : MonoBehaviour
{
    public int CaptureWidth = 640, CaptureHeight = 480;

    // private variables
    private WebCamTexture m_webcamTexture = null;
    private Color32[] m_data;
    private String deviceName;
    public GCHandle m_PixelsHandle;
    private Texture2D l_texture = null;

    private Mesh createPlanarMesh(bool mirror, xmgVideoPlaneFittingMode fittingMode, xmgVideoCaptureParameters videoParameters, ScreenOrientation screenOrientation)
    {
        Mesh mesh = new Mesh();
        mesh.vertices = new Vector3[] { new Vector3(-1, 1, 0), new Vector3(1, 1, 0), new Vector3(1, -1, 0), new Vector3(-1, -1, 0) };
        mesh.triangles = new int[] { 0, 1, 2, 0, 2, 3 };
        mesh.uv = new Vector2[] { new Vector2(0, 1), new Vector2(1, 1), new Vector2(1, 0), new Vector2(0, 0) };
        return mesh;
    }

    public WebCamTexture OpenVideoCapture(ref xmgVideoCaptureParameters videoParameters)
    {

        CaptureWidth = videoParameters.GetVideoCaptureWidth();
        CaptureHeight = videoParameters.GetVideoCaptureHeight();

        // Reset
        Camera.main.clearFlags = CameraClearFlags.Skybox;
        Camera.main.transform.position = new Vector3(0, 0, 0);
        Camera.main.transform.eulerAngles = new Vector3(0, 0, 0);
        transform.localPosition = new Vector3(0.0f, 0.0f, 0.0f);

        Debug.Log("webcam names:");
        for (int cameraIndex = 0; cameraIndex < WebCamTexture.devices.Length; cameraIndex++)
        {
            Debug.Log(WebCamTexture.devices[cameraIndex].name);
        }

        if (videoParameters.videoCaptureIndex == -1)
        {
            for (int cameraIndex = 0; cameraIndex < WebCamTexture.devices.Length; cameraIndex++)
            {
                // We want the back camera
                if (!WebCamTexture.devices[cameraIndex].isFrontFacing && !videoParameters.UseFrontal)
                {
                    deviceName = WebCamTexture.devices[cameraIndex].name;
                    m_webcamTexture = new WebCamTexture(deviceName, CaptureWidth, CaptureHeight, 30);
                    break;
                }
                else if (WebCamTexture.devices[cameraIndex].isFrontFacing && videoParameters.UseFrontal)
                {
                    deviceName = WebCamTexture.devices[cameraIndex].name;
                    m_webcamTexture = new WebCamTexture(deviceName, CaptureWidth, CaptureHeight, 30);
                    break;
                }
            }
        }
        else
        {
            deviceName = WebCamTexture.devices[videoParameters.videoCaptureIndex].name;
           // deviceName = "device #0";
            m_webcamTexture = new WebCamTexture(deviceName, CaptureWidth, CaptureHeight, 30);
        }
        if (!m_webcamTexture)   // try with the first idx
        {
            if (!videoParameters.UseFrontal || WebCamTexture.devices.Length == 1)
                deviceName = WebCamTexture.devices[0].name;
            else
                deviceName = WebCamTexture.devices[1].name;
            m_webcamTexture = new WebCamTexture(deviceName, CaptureWidth, CaptureHeight, 30);
        }

        if (!m_webcamTexture)
            Debug.Log("No camera detected!");
        else
        {
            if (m_webcamTexture.isPlaying)
                m_webcamTexture.Stop();

            m_webcamTexture.Play();
        }
        return m_webcamTexture;
    }

    public void CreateVideoCapturePlane(float screenScaleFactor, xmgVideoPlaneFittingMode fittingMode, xmgVideoCaptureParameters videoParameters)
    {
        CaptureWidth = videoParameters.GetVideoCaptureWidth();
        CaptureHeight = videoParameters.GetVideoCaptureHeight();

        Debug.Log("CreateVideoCapturePlane - Capture " + CaptureWidth + " " + CaptureHeight);

        // Create the mesh (plane)
        Mesh mesh = createPlanarMesh(videoParameters.MirrorVideo, fittingMode, videoParameters, Screen.orientation);

        // Attach it to the current GO
        gameObject.AddComponent<MeshFilter>().mesh = mesh;
        gameObject.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
        gameObject.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
        gameObject.transform.position = new Vector3(0.0f, 0.0f, 1.0f);

        // Assign video texture to the renderer
        if (!gameObject.GetComponent<Renderer>())
            gameObject.AddComponent<MeshRenderer>();

        if (!videoParameters.useNativeCapture)
        {
            // Apply shader to set the texture as the background
            GetComponent<Renderer>().material = new Material(Shader.Find("Custom/VideoShader"));
            m_data = new Color32[CaptureWidth * CaptureHeight];
            m_PixelsHandle = GCHandle.Alloc(m_data, GCHandleType.Pinned);

            // GetComponent<Renderer>().material.mainTexture = m_webcamTexture;
            l_texture = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGBA32, false);
            GetComponent<Renderer>().material.mainTexture = l_texture;
        }
        else
        {
            // Mobile video capture
			GetComponent<Renderer>().material = new Material(Shader.Find("Custom/VideoShader"));
			#if (UNITY_ANDROID)
			l_texture = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);
			#elif (UNITY_IOS)
		    l_texture = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.BGRA32, false);
			#endif
            GetComponent<Renderer>().material.mainTexture = l_texture;
        }
        // shader parameters
		GetComponent<Renderer>().material.SetInt("_Rotation", (int)xmgTools.GetVideoOrientation(videoParameters.useNativeCapture, videoParameters.UseFrontal));
        GetComponent<Renderer>().material.SetFloat("_ScaleX", (float)GetScaleX(videoParameters));
        GetComponent<Renderer>().material.SetFloat("_ScaleY", (float)GetScaleY(videoParameters));
        GetComponent<Renderer>().material.SetInt("_Mirror", (int)(videoParameters.MirrorVideo == true ? 1 : 0));
        GetComponent<Renderer>().material.SetInt("_VerticalMirror", (int)((videoParameters.GetVerticalMirror() == true) ? 1 : 0));

        Debug.Log("CreateVideoCapturePlane - _Scale " + (float)GetScaleX(videoParameters) + " " + (float)GetScaleY(videoParameters));
    }

    static public float GetScaleX(xmgVideoCaptureParameters videoParameters)
    {
        int CaptureWidth = videoParameters.GetVideoCaptureWidth();
        int CaptureHeight = videoParameters.GetVideoCaptureHeight();

        float arVideo = (float)CaptureWidth / (float)CaptureHeight;
        float arScreen = (float)Screen.width / (float)Screen.height;

#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR && !UNITY_STANDALONE)
		if (Screen.orientation == ScreenOrientation.Portrait || Screen.orientation == ScreenOrientation.PortraitUpsideDown)
		arScreen =  (float)Screen.height / (float)Screen.width;
#endif
        if (Math.Abs(arVideo - arScreen) > 0.001f && videoParameters.videoPlaneFittingMode == xmgVideoPlaneFittingMode.FitScreenVertically)
            //return 3.0f / 4.0f;
            return arVideo/arScreen;
        else
            return 1.0f;
    }

    static public float GetScaleY(xmgVideoCaptureParameters videoParameters)
    {
        int CaptureWidth = videoParameters.GetVideoCaptureWidth();
        int CaptureHeight = videoParameters.GetVideoCaptureHeight();

        float arVideo = (float)CaptureWidth / (float)CaptureHeight;
        float arScreen = (float)Screen.width / (float)Screen.height;

#if ((UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR && !UNITY_STANDALONE)
		if (Screen.orientation == ScreenOrientation.Portrait || Screen.orientation == ScreenOrientation.PortraitUpsideDown)
		arScreen =  (float)Screen.height / (float)Screen.width;
#endif
        if (Math.Abs(arVideo - arScreen) > 0.001f && videoParameters.videoPlaneFittingMode == xmgVideoPlaneFittingMode.FitScreenHorizontally)
            // return 4.0f / 3.0f;
            return arScreen / arVideo;
        else
            return 1.0f;
    }

#if (UNITY_IOS || UNITY_ANDROID)
    public IntPtr GetTexturePtr()
    {
        return l_texture.GetNativeTexturePtr();
    }
    
#endif

    public bool GetData()
    {
        if (m_webcamTexture)
        {
            // don't change - sequenced to avoid crash
            if (m_webcamTexture.didUpdateThisFrame)
            {
                m_webcamTexture.GetPixels32(m_data);
                return true;
            }
            else
            {
                return false;
            }
        }
        return false;
    }

    public void SetVideoPlaneData(byte[] frame)
    {
        if (frame != null)
        {
            l_texture.LoadRawTextureData(frame);
            l_texture.Apply();
        }
    }

    public void ReleaseVideoCapturePlane()
    {
        m_PixelsHandle.Free();
        m_webcamTexture.Stop();
    }

    public void ApplyTexture()
    {
        // don't change - sequenced to avoid crash
        l_texture.SetPixels32(m_data);
        l_texture.Apply();
    }

    public void ActivateVideo(bool activate)
    {
        if (activate)
        {

            // deactivate renderers
            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers) r.enabled = true;
        }
        else
        {
            // deactivate renderers
            Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers) r.enabled = false;
        }
    }


}