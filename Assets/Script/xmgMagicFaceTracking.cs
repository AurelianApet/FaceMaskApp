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


public class xmgMagicFaceTracking : xmgMagicFaceBase
{
    
    private xmgMagicFaceBridge.xmgImage m_image;
#if (UNITY_ANDROID || UNITY_IOS)
    private xmgMagicFaceBridge.xmgVideoCaptureOptions videoCaptureOptions;
#endif

    // -------------------------------------------------------------------------------------------------------------------
        
	void Awake()
    {

#if (!UNITY_STANDALONE && !UNITY_EDITOR && (UNITY_IOS ||UNITY_ANDROID || UNITY_WEBGL))
        Debug.Log("Error - unsuported platforms");
        return;
#endif
        CheckParameters(true);
        if (m_myWebCamEngine == null)
        {
            m_myWebCamEngine = (xmgVideoCapturePlane)gameObject.AddComponent(typeof(xmgVideoCapturePlane));
            m_webcamTexture = m_myWebCamEngine.OpenVideoCapture(ref m_videoParameters);
            m_myWebCamEngine.CreateVideoCapturePlane(1.0f, xmgVideoPlaneFittingMode.FitScreenHorizontally, m_videoParameters);
        }
        if (m_myWebCamEngine == null)
        {
            Debug.Log("Error - No camera detected!");
            return;
        }
        int captureWidth = GetCaptureWidth(), captureHeight = GetCaptureHeight();
        xmgMagicFaceBridge.PrepareImage(ref m_image, captureWidth, captureHeight, 4, m_myWebCamEngine.m_PixelsHandle.AddrOfPinnedObject());
        Camera.main.fieldOfView = m_videoParameters.CameraVerticalFOV;

        InitializeXZIMG(false, true);        
        PrepareRenderObjects(false);  
    }

    // -------------------------------------------------------------------------------------------------------------------

    public override void OnDisable()
    {
#if (!UNITY_STANDALONE && !UNITY_EDITOR && (UNITY_IOS ||UNITY_ANDROID || UNITY_WEBGL))
        return;
#endif
#if (UNITY_STANDALONE || UNITY_EDITOR)
        if (m_myWebCamEngine != null)
        {
            m_PixelsHandle.Free();
            m_myWebCamEngine.ReleaseVideoCapturePlane();
            m_myWebCamEngine = null;
        }
#endif
        base.OnDisable();
        xmgMagicFaceBridge.xzimgRigidFaceTrackingRelease();
    }

    // -------------------------------------------------------------------------------------------------------------------

    void Update()
    {
#if (!UNITY_STANDALONE && !UNITY_EDITOR && (UNITY_IOS ||UNITY_ANDROID || UNITY_WEBGL))
        return;
#endif
        Renderer[] renderers;
        
        if (m_myWebCamEngine == null || !m_myWebCamEngine.GetData()) return;
		xmgMagicFaceBridge.xzimgRigidFaceTrackingProcess(ref m_image, (int)m_captureDeviceOrientation, ref m_nonRigidData[0].m_faceData);
        m_myWebCamEngine.ApplyTexture();

        for (int o = 0; o < 1/*m_nonRigidData.Length*/; o++)
        {
            if (m_renderedFaceObjects.Count > o && m_nonRigidData[o].m_faceData.m_faceDetected > 0)
            {
                Vector3[] vertices3D = new Vector3[m_nonRigidData[o].m_faceData.m_nbLandmarks];
                for (int i = 0; i < m_nonRigidData[o].m_faceData.m_nbLandmarks; i++)
                {
                    vertices3D[i].x = m_nonRigidData[o].m_dataLandmarks3D[3 * i];
                    if (m_videoParameters.MirrorVideo)
                        vertices3D[i].x = -vertices3D[i].x;
                    vertices3D[i].y = -m_nonRigidData[o].m_dataLandmarks3D[3 * i + 1];
                    vertices3D[i].z = m_nonRigidData[o].m_dataLandmarks3D[3 * i + 2];
                }


                // triangles
                int[] triangleIndices = new int[m_nonRigidData[o].m_faceData.m_nbTriangles * 3];
                for (int i = 0; i < m_nonRigidData[o].m_faceData.m_nbTriangles * 3; i++)
                {
                    triangleIndices[i] = m_nonRigidData[o].m_dataTriangles[m_nonRigidData[o].m_faceData.m_nbTriangles * 3 - i - 1];
                    //triangleIndices[i] = m_dataTriangles[i];
                }

                // Update the mesh
                if (m_renderedFaceObjects[o].m_renderPivot)
                {
                    Mesh msh = m_renderedFaceObjects[o].m_renderPivot.GetComponent<MeshFilter>().mesh;
                    msh.vertices = vertices3D;

                    if (m_renderedFaceObjects[o].m_renderMode == xmgRenderMode.NonRigidFace)
                    {
                        msh.SetIndices(triangleIndices, MeshTopology.Triangles, 0);
                        msh.RecalculateNormals();
                        msh.RecalculateBounds();
                    }
                    else
                    {
                        // draw the wireframe
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
                        m_renderedFaceObjects[o].m_renderPivot.GetComponent<Renderer>().material.SetFloat("_Transparency", 0.5f);
                    }

                    // rotate object according to screen orientation
                    m_renderedFaceObjects[o].m_renderPivot.GetComponent<Renderer>().material.SetInt("_Rotation", (int)xmgTools.GetRenderOrientation());

                    //if (!m_renderObject.GetComponent<MeshFilter>())
                    //    m_renderObject.AddComponent<MeshFilter>().mesh = msh;
                    //else
                    //    m_renderObject.GetComponent<MeshFilter>().mesh = msh;

                    xmgTools.UpdateObjectPosition(m_nonRigidData[o].m_faceData, m_renderedFaceObjects[o].m_renderPivot, m_videoParameters.VideoPlaneScale, m_videoParameters.MirrorVideo, m_videoParameters.UseFrontal);

                    // enable renderers
                    renderers = m_renderedFaceObjects[o].m_renderPivot.GetComponentsInChildren<Renderer>();
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
}
