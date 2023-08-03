/**
*
* Copyright (c) 2016 xzimg Limited , All Rights Reserved
* No part of this software and related documentation may be used, copied,
* modified, distributed and transmitted, in any form or by any means,
* without the prior written permission of XZIMG Limited.
*
* contact@xzimg.com, www.xzimg.com
*
*/

using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.Collections;


public class xmgMagicFace2D : xmgMagicFaceBase
{
    const float MASK_OPACITY = 0.8f;
    private xmgMagicFaceBridge.xmgImage m_image;
    public bool m_bIsMouthOpen = false;
    public GameObject m_custom3DObject;
    public bool m_bShowMask = true;
    float m_fMaskOpacity = 0.0f;
    // -------------------------------------------------------------------------------------------------------------------

	public void Awake()
    {
        CheckParameters(false);
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
            xmgMagicFaceBridge.PrepareImage(ref m_image, GetCaptureWidth(), GetCaptureHeight(), 4, m_myWebCamEngine.m_PixelsHandle.AddrOfPinnedObject());
			Debug.Log ("Awake 1");
        }
		Debug.Log ("Awake 2");
        InitializeXZIMG(false, false);

        if (m_videoParameters.useNativeCapture)
        {

#if (UNITY_ANDROID)	   
            m_myWebCamEngine = (xmgVideoCapturePlane)gameObject.AddComponent(typeof(xmgVideoCapturePlane));
            m_myWebCamEngine.CreateVideoCapturePlane(1.0f, m_videoParameters.videoPlaneFittingMode, m_videoParameters);
		    xmgMagicFaceBridge.xzimgMagicFaceInitializeVideoCapture(m_videoParameters.videoCaptureMode, m_videoParameters.UseFrontal);
#elif (UNITY_IOS)
		    m_myWebCamEngine = (xmgVideoCapturePlane)gameObject.AddComponent(typeof(xmgVideoCapturePlane));
		    m_myWebCamEngine.CreateVideoCapturePlane(1.0f, m_videoParameters.videoPlaneFittingMode, m_videoParameters);
#endif
			Debug.Log ("Awake 3");
        }

        PrepareRenderObjects(true);
        LoadTextureCoordinates(false, false);
    }

	public void LoadCoords() {
		LoadTextureCoordinates(false, false);
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

        // Read the results from XZIMG API
        for (int i = 0; i < m_nonRigidData.Length; i++)
            xmgMagicFaceBridge.xzimgMagicFaceGetFaceData(i, ref m_nonRigidData[i].m_faceData);

        // Draw 3D Objects
        //Debug.Log("RigidData Length " + m_nonRigidData.Length);

        for (int o = 0; o < m_nonRigidData.Length; o++)
        {
            //Show Hide Custom Object when face detected or not
            if (o == 0 && m_custom3DObject)
            {
                if (m_nonRigidData[o].m_faceData.m_faceDetected > 0)
                {
                    renderers = m_custom3DObject.GetComponentsInChildren<Renderer>();
                    foreach (Renderer r in renderers) r.enabled = true;
                }
                else
                {
                    renderers = m_custom3DObject.GetComponentsInChildren<Renderer>();
                    foreach (Renderer r in renderers) r.enabled = false;
                }
            }

            if (m_nonRigidData[o].m_faceData.m_faceDetected < 0)
            {
                // Initialization of the video capture not ready yet, disable video plane
                m_bIsMouthOpen = false;
                m_myWebCamEngine.ActivateVideo(false);
                return;
            }
            m_myWebCamEngine.ActivateVideo(true);

            if (m_nonRigidData[o].m_faceData.m_faceDetected > 0)
            {
                int nbFaceFeatures = GetFaceFeaturesNumber();
                
                Vector2[] vertices2D = new Vector2[nbFaceFeatures];
                int halfWidth = m_videoParameters.GetVideoCaptureWidth() / 2;
                int halfHeight = m_videoParameters.GetVideoCaptureHeight() / 2;
                for (int i = 0; i < m_nonRigidData[o].m_faceData.m_nbLandmarks; i++)
                {
                    vertices2D[i].x = (m_nonRigidData[o].m_dataLandmarks2D[2 * i] - halfWidth) / halfWidth;
                    vertices2D[i].y = -(m_nonRigidData[o].m_dataLandmarks2D[2 * i + 1] - halfHeight) / halfHeight; // y inverted because the change of projected coordinate system
                }

                // Create the Vector3 vertices
                Vector3[] vertices = new Vector3[vertices2D.Length];
                for (int i = 0; i < vertices.Length; i++)
                {
                    vertices[i] = new Vector3(vertices2D[i].x, vertices2D[i].y, 0);
                }

                m_bIsMouthOpen = IsMouthOpen(vertices);

                // triangles
                int nbTriangles = m_nonRigidData[o].m_faceData.m_nbTriangles;

                int[] triangleIndices = new int[nbTriangles * 3];
                for (int i = 0; i < nbTriangles * 3; i++)
                {
                    if (!m_videoParameters.MirrorVideo)
                        triangleIndices[i] = m_nonRigidData[o].m_dataTriangles[nbTriangles * 3 - i - 1];
                    else
                        triangleIndices[i] = m_nonRigidData[o].m_dataTriangles[i];
                }
                if(m_bShowMask){
                    if(m_fMaskOpacity < MASK_OPACITY){
                        m_fMaskOpacity += 0.2f;
                    }else if(m_fMaskOpacity > MASK_OPACITY){
                        m_fMaskOpacity = MASK_OPACITY;
                    }
                }else{
                    m_fMaskOpacity = 0.0f;
                }

                // Update the mesh
                if (m_renderedFaceObjects.Count > o && m_renderedFaceObjects[o].m_renderPivot)
                {
                    Mesh msh = m_renderedFaceObjects[o].m_renderPivot.GetComponent<MeshFilter>().mesh;
                    msh.vertices = vertices;
                    if (m_renderedFaceObjects[o].m_renderMode == xmgRenderMode.NonRigidFace)
                    {
                        msh.SetIndices(triangleIndices, MeshTopology.Triangles, 0);
                        //m_renderedFaceObjects[o].m_renderPivot.GetComponent<Renderer>().material.SetFloat("_Transparency", 0.8f);
                        m_renderedFaceObjects[o].m_renderPivot.GetComponent<Renderer>().material.SetFloat("_Transparency", m_fMaskOpacity);
                        msh.RecalculateNormals();
                        msh.RecalculateBounds();
                    }
                    else
                    {
                        int[] lineIndices = new int[m_nonRigidData[o].m_faceData.m_nbTriangles * 6];
                        for (int i = 0; i < m_nonRigidData[o].m_faceData.m_nbTriangles; i++)
                        {
                            lineIndices[6 * i] = m_nonRigidData[o].m_dataTriangles[3 * i];
                            lineIndices[6 * i + 1] = m_nonRigidData[o].m_dataTriangles[3 * i + 1];
                            lineIndices[6 * i + 2] = m_nonRigidData[o].m_dataTriangles[3 * i + 1];
                            lineIndices[6 * i + 3] = m_nonRigidData[o].m_dataTriangles[3 * i + 2];
                            lineIndices[6 * i + 4] = m_nonRigidData[o].m_dataTriangles[3 * i + 2];
                            lineIndices[6 * i + 5] = m_nonRigidData[o].m_dataTriangles[3 * i];
                        }
                        msh.SetIndices(lineIndices, MeshTopology.Lines, 0);
                        //m_renderedFaceObjects[o].m_renderPivot.GetComponent<Renderer>().material.SetFloat("_Transparency", 0.8f);
                        m_renderedFaceObjects[o].m_renderPivot.GetComponent<Renderer>().material.SetFloat("_Transparency", m_fMaskOpacity);

                    }

                    // rotate object according to screen orientation
                    m_renderedFaceObjects[o].m_renderPivot.GetComponent<Renderer>().material.SetInt("_Rotation", (int)xmgTools.GetRenderOrientation(m_videoParameters.UseFrontal));
                    m_renderedFaceObjects[o].m_renderPivot.GetComponent<Renderer>().material.SetFloat("_ScaleX", (float)xmgVideoCapturePlane.GetScaleX(m_videoParameters));
                    m_renderedFaceObjects[o].m_renderPivot.GetComponent<Renderer>().material.SetFloat("_ScaleY", (float)xmgVideoCapturePlane.GetScaleY(m_videoParameters));
                    m_renderedFaceObjects[o].m_renderPivot.GetComponent<Renderer>().material.SetInt("_Mirror", (int)(m_videoParameters.MirrorVideo == true ? 1 : 0));
                    
                    // enable renderers
                    renderers = m_renderedFaceObjects[o].m_renderPivot.GetComponentsInChildren<Renderer>();
                    foreach (Renderer r in renderers) r.enabled = true;
                    //foreach (Renderer r in renderers) r.enabled = m_bShowMask;
                }
            }
            else if (m_renderedFaceObjects.Count > o)
            {
                renderers = m_renderedFaceObjects[o].m_renderPivot.GetComponentsInChildren<Renderer>();
                foreach (Renderer r in renderers) r.enabled = false;
            }
        }
    }

    public bool IsMouthOpen(Vector3[] vertices){
        //Check Mouth Open
        float x = Math.Abs(vertices[66].x - vertices[62].x);
        float y = Math.Abs(vertices[66].y - vertices[62].y);

        if (x > 0.15 || y > 0.15)
        {
            return true;
        }
        return false;
    }
    // -------------------------------------------------------------------------------------------------------------------

    public override void OnGUI()
    {
        if (m_videoParameters.ScreenDebug)
        {
            if (m_webcamTexture != null)
                GUILayout.Label("Screen: " + Screen.width + "x" + Screen.height + " - " + m_webcamTexture.requestedWidth + "x" + m_webcamTexture.requestedHeight);
            GUILayout.Label(m_debugStatus);
        }
    }
}
