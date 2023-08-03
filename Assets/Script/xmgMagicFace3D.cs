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
using System.Runtime.InteropServices;
using System;


public class xmgMagicFace3D : xmgMagicFaceBase
{    
    private xmgMagicFaceBridge.xmgImage m_image;
    public GameObject m_custom3DObject;

    // -------------------------------------------------------------------------------------------------------------------

    void Awake()
    {
        CheckParameters(true);
        int nbFaceFeatures = GetFaceFeaturesNumber();

        if (!m_videoParameters.useNativeCapture)
        {
            if (m_myWebCamEngine == null)
            {
                m_myWebCamEngine = (xmgVideoCapturePlane)gameObject.AddComponent(typeof(xmgVideoCapturePlane));
                m_webcamTexture = m_myWebCamEngine.OpenVideoCapture(ref m_videoParameters);
                m_myWebCamEngine.CreateVideoCapturePlane(1.0f, xmgVideoPlaneFittingMode.FitScreenHorizontally, m_videoParameters);
            }
            if (m_webcamTexture == null)
            {
                Debug.Log("Error - No camera detected!");
                return;
            }

            int captureWidth = GetCaptureWidth(), captureHeight = GetCaptureHeight();
             xmgMagicFaceBridge.PrepareImage(ref m_image, captureWidth, captureHeight, 4, m_myWebCamEngine.m_PixelsHandle.AddrOfPinnedObject());
        }
        InitializeXZIMG(true, false);
        if (m_videoParameters.useNativeCapture)
        {
#if (!UNITY_STANDALONE && !UNITY_EDITOR && UNITY_ANDROID)
            m_myWebCamEngine = (xmgVideoCapturePlane)gameObject.AddComponent(typeof(xmgVideoCapturePlane));
            m_myWebCamEngine.CreateVideoCapturePlane(1.0f, m_videoParameters.videoPlaneFittingMode, m_videoParameters);
            xmgMagicFaceBridge.xzimgMagicFaceInitializeVideoCapture(m_videoParameters.videoCaptureMode, m_videoParameters.UseFrontal);
#elif (!UNITY_STANDALONE && !UNITY_EDITOR && UNITY_IOS)
            m_myWebCamEngine = (xmgVideoCapturePlane)gameObject.AddComponent(typeof(xmgVideoCapturePlane));
            m_myWebCamEngine.CreateVideoCapturePlane(1.0f, m_videoParameters.videoPlaneFittingMode, m_videoParameters);
#endif
        }

        PrepareCamera();
        PrepareRenderObjects(false);
        LoadTextureCoordinates(false);
    }

    // -------------------------------------------------------------------------------------------------------------------

    public override void OnDisable()
    {
#if (UNITY_STANDALONE || UNITY_EDITOR || UNITY_WEBGL)
        if (m_myWebCamEngine != null)
        {
            m_PixelsHandle.Free();
            m_myWebCamEngine.ReleaseVideoCapturePlane();
            m_myWebCamEngine = null;
        }
#endif
        base.OnDisable();
    }
    
    // -------------------------------------------------------------------------------------------------------------------

    void Update()
    {
        Renderer[] renderers;
        if (!m_videoParameters.useNativeCapture)
        {
            if (m_myWebCamEngine == null || !m_myWebCamEngine.GetData()) return;
            xmgMagicFaceBridge.xzimgMagicFaceTrackNonRigidFaces(ref m_image, (int)xmgTools.GetDeviceCurrentOrientation((int)m_captureDeviceOrientation, m_videoParameters.UseFrontal));

            m_myWebCamEngine.ApplyTexture();
        }
        else
        {
#if (UNITY_ANDROID || UNITY_IOS)
            xmgMagicFaceBridge.xzimgMagicFaceTextureVideo(m_myWebCamEngine.GetTexturePtr(), (int)xmgTools.GetDeviceCurrentOrientation((int)m_captureDeviceOrientation, m_videoParameters.UseFrontal));
#endif
        }

		for (int i=0; i< m_nonRigidData.Length; i++)
			xmgMagicFaceBridge.xzimgMagicFaceGetFaceData(i, ref m_nonRigidData[i].m_faceData);
		
        for (int o = 0; o < m_nonRigidData.Length; o++)
        {
            if (m_nonRigidData[o].m_faceData.m_faceDetected > 0)
            {
                int nbFaceFeatures = m_nonRigidData[o].m_faceData.m_nbLandmarks3D;

                // Read and convert 3D vertices
                Vector3[] vertices3D = new Vector3[nbFaceFeatures];
                for (int i = 0; i < nbFaceFeatures; i++)
                {
                    vertices3D[i].x = m_nonRigidData[o].m_dataLandmarks3D[3 * i];

                    // mirror
                    if (m_videoParameters.MirrorVideo)
                        vertices3D[i].x = -vertices3D[i].x;
                    // left handed
                    vertices3D[i].y = -m_nonRigidData[o].m_dataLandmarks3D[3 * i + 1];
                    vertices3D[i].z = m_nonRigidData[o].m_dataLandmarks3D[3 * i + 2];
                }

                // Update the mesh
                if (m_renderedFaceObjects.Count > o && m_renderedFaceObjects[o].m_renderPivot)
                {
                    Mesh msh = m_renderedFaceObjects[o].m_renderPivot.GetComponent<MeshFilter>().mesh;
                    msh.vertices = vertices3D;

                    xmgTools.UpdateObjectPosition(m_nonRigidData[o].m_faceData, m_renderedFaceObjects[o].m_renderPivot, m_videoParameters.VideoPlaneScale, m_videoParameters.MirrorVideo, m_videoParameters.UseFrontal);

                    // enable renderers
                    renderers = m_renderedFaceObjects[o].m_renderPivot.GetComponentsInChildren<Renderer>();
                    foreach (Renderer r in renderers) r.enabled = true;                    
                }
                if (o == 0 && m_custom3DObject)
                {
                    xmgTools.UpdateObjectPosition(m_nonRigidData[o].m_faceData, m_custom3DObject, m_videoParameters.VideoPlaneScale, m_videoParameters.MirrorVideo, m_videoParameters.UseFrontal);

                    // enable renderers
                    renderers = m_custom3DObject.GetComponentsInChildren<Renderer>();
                    foreach (Renderer r in renderers) r.enabled = true;
                }
            }
            else if (m_renderedFaceObjects.Count > o)
            {
                renderers = m_renderedFaceObjects[o].m_renderPivot.GetComponentsInChildren<Renderer>();
                foreach (Renderer r in renderers) r.enabled = false;
            }
        }
    }

    // -------------------------------------------------------------------------------------------------------------------

    public override void OnGUI()
    {
       base.OnGUI();
        if (m_videoParameters.ScreenDebug)
        {
            //GUILayout.Label("Face#: " + m_nonRigidData.m_faceData.m_faceDetected + " - Land#: " + m_nonRigidData.m_faceData.m_nbLandmarks);
            GUILayout.Label(m_debugStatus);
        }
    }
}
