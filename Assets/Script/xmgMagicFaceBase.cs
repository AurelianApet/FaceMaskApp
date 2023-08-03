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

using UnityEngine;
using System;
using System.Runtime.InteropServices;
using System.IO;
using System.Text;
using System.Collections.Generic;
using System.Linq; // reverse

public enum xmgRenderMode
{
    NonRigidFace = 0,
    NonRigidFaceWireframe = 1,
};

[System.Serializable]
public class xmgObjectPivotLink
{
    [Tooltip("Drag and drop a pivot (a GameObject) from the scene")]
    public GameObject m_renderPivot;

    [Tooltip("Texture to be rendered as a mask on the face")]
    public Texture2D m_renderTexture;

    [Tooltip("Width of the texture")]
    public int m_renderTextureWidth = 256;

    [Tooltip("Height of the texture")]
    public int m_renderTextureHeight = 256;

    //[Tooltip("Level of transparency of the mask")]
    // public float m_transparency = 0.5f;

    [Tooltip("Height of the texture")]
    public Shader m_renderShader;// = Shader.Find("Custom/Face3DShaderTransparent");

    [Tooltip("Draw a Texture or a Mesh")]
    public xmgRenderMode m_renderMode = xmgRenderMode.NonRigidFace;

}

public class xmgMagicFaceBase : MonoBehaviour
{
    [Tooltip("Are we using face contour or only internal features")]
    public bool m_useInternalFeaturesOnly = false;

    [Tooltip("Number of face detected simultaneously")]
    public int m_nbMaxFaces = 1;

    [Tooltip("Fill this list with the scene pivot for which you want the pose to be modified")]
    public List<xmgObjectPivotLink> m_renderedFaceObjects;
    
    [Tooltip("Default Orientation PC/Windows feature")]
    public xmgOrientationMode m_captureDeviceOrientation = xmgOrientationMode.LandscapeLeft;
    
    public xmgVideoCaptureParameters m_videoParameters;
    
	private xmgMagicFaceBridge.xmgVideoCaptureOptions m_videoCaptureOptions;
    private xmgMagicFaceBridge.xmgInitParams initializationParams;

    // -------------------------------------------------------------------------------------------------------------------

    protected String m_debugStatus = "";
    protected WebCamTexture m_webcamTexture = null;
    protected xmgVideoCapturePlane m_myWebCamEngine = null;
    protected Color[] m_imageData;
    protected GCHandle m_PixelsHandle;

    // exchange data with the plugin
    protected xmgMagicFaceObject []  m_nonRigidData;

    // -------------------------------------------------------------------------------------------------------------------

    protected void CheckParameters(bool is3D)
    {
        m_videoParameters.CheckVideoCaptureParameters();
        m_nonRigidData = new xmgMagicFaceObject[m_nbMaxFaces];
        for (int i = 0; i < m_nbMaxFaces; i++)
            m_nonRigidData[i] = new xmgMagicFaceObject();

#if (!UNITY_EDITOR && (UNITY_ANDROID || UNITY_IOS))
        if (m_videoParameters.useNativeCapture)
            m_captureDeviceOrientation = xmgOrientationMode.LandscapeLeft;
#endif
        if (is3D && m_useInternalFeaturesOnly)
        {
            Debug.Log("xmgMagicFaceBase (m_useInternalFeaturesOnly) - 3D Tracking requires features on the contour of the face");
            m_useInternalFeaturesOnly = false;
        }

        //if (m_faceFeaturesMode == xmgFaceFeaturesMode.FaceFeatures51)
        //{
        //    for (int i = 0; i < m_renderedFaceObjects.Count; i++)
        //    {
        //        if (m_renderedFaceObjects[i].m_renderMode == xmgRenderMode.NonRigidFace)
        //        {
        //            Debug.Log("xmgMagicFaceBase (drawTexture) -  Draw texture is not available with 51 vertices - forced to FALSE");
        //            m_renderedFaceObjects[i].m_renderMode = xmgRenderMode.NonRigidFace;
        //        }
        //    }                
        //}

        for (int i = 0; i < m_renderedFaceObjects.Count; i++)
        {
            if (i >= m_nbMaxFaces)
            {
                // disable renderers
                Renderer[] renderers;
                renderers = m_renderedFaceObjects[i].m_renderPivot.GetComponentsInChildren<Renderer>();
                foreach (Renderer r in renderers) r.enabled = false;
            }
        }
    }

    // -------------------------------------------------------------------------------------------------------------------
    // -------------------------------------------------------------------------------------------------------------------

    public virtual void OnApplicationPaused(bool pauseStatus)
    {
        // Do something here
    }

    // -------------------------------------------------------------------------------------------------------------------

    public virtual void OnApplicationFocus(bool status)
    {
        // Do something here
    }

    // -------------------------------------------------------------------------------------------------------------------

    public virtual void OnGUI()
    {
#if (!UNITY_EDITOR && !UNITY_ANDROID)
        if (m_videoParameters.ScreenDebug)
        {
            if (Screen.orientation == ScreenOrientation.Unknown) GUILayout.Label("Unknown");
            if (Screen.orientation == ScreenOrientation.Portrait) GUILayout.Label("Portrait");
            if (Screen.orientation == ScreenOrientation.PortraitUpsideDown) GUILayout.Label("PortraitUpsideDown");
            if (Screen.orientation == ScreenOrientation.LandscapeLeft) GUILayout.Label("LandscapeLeft");
            if (Screen.orientation == ScreenOrientation.LandscapeRight) GUILayout.Label("LandscapeRight");
        }
#endif
    }


    public virtual void OnDisable()
    {
#if (UNITY_ANDROID && !UNITY_EDITOR && !UNITY_WEBGL)
        xmgMagicFaceBridge.xzimgMagicFaceReleaseVideoCapture();
#endif
        xmgMagicFaceBridge.xzimgMagicFaceRelease();
    }

    // -------------------------------------------------------------------------------------------------------------------
    // -------------------------------------------------------------------------------------------------------------------

    public int GetCaptureWidth()
    {
        if (m_myWebCamEngine)
        {
            int captureWidth = m_webcamTexture.width;
            if (captureWidth < 100)
                captureWidth = m_webcamTexture.requestedWidth;
            return captureWidth;
        }
        else
            return m_videoParameters.GetVideoCaptureWidth();
    }

    public int GetCaptureHeight()
    {
        if (m_myWebCamEngine)
        {
                int captureHeight = m_webcamTexture.height;
            if (captureHeight < 100)
                captureHeight = m_webcamTexture.requestedHeight;
            return captureHeight;
        }
        else
            return m_videoParameters.GetVideoCaptureHeight();
    }

    // -------------------------------------------------------------------------------------------------------------------
    
    public void InitializeXZIMG(bool is3D, bool isRigid)
    {
        int nbFaceFeatures = GetFaceFeaturesNumber(is3D);

		int captureWidth = GetCaptureWidth ();
		int captureHeight = GetCaptureHeight();

		Debug.Log ("captureWidth: " + captureWidth + " captureHeight: " + captureHeight);
        // first asset
        TextAsset textAsset;
        if (nbFaceFeatures == 51)
            textAsset = Resources.Load("regressor-51LM") as TextAsset;
        else
            textAsset = Resources.Load("regressor-68LM") as TextAsset;        
        GCHandle bytesHandleRegressor = GCHandle.Alloc(textAsset.bytes, GCHandleType.Pinned);
        
        textAsset = Resources.Load("faceClassifier-51LM") as TextAsset;
        GCHandle bytesHandleClassifier = GCHandle.Alloc(textAsset.bytes, GCHandleType.Pinned);

        textAsset = Resources.Load("model-3D") as TextAsset;
        GCHandle bytesHandle3DModel = GCHandle.Alloc(textAsset.bytes, GCHandleType.Pinned);

        if (!isRigid)
        {
            xmgMagicFaceBridge.PrepareNativeVideoCaptureDefault(ref m_videoCaptureOptions, m_videoParameters.videoCaptureMode, m_videoParameters.UseFrontal ? 1 : 0);
            GCHandle CameraModelHandle = GCHandle.Alloc(m_videoCaptureOptions, GCHandleType.Pinned);
            xmgMagicFaceBridge.PrepareInitParams(ref initializationParams, is3D, m_videoParameters.GetProcessingWidth(), m_videoParameters.GetProcessingHeight(), 
                (nbFaceFeatures == 730) ? 68: nbFaceFeatures, m_nbMaxFaces, m_videoParameters.CameraVerticalFOV, 
                (m_videoParameters.useNativeCapture) ? CameraModelHandle.AddrOfPinnedObject() : System.IntPtr.Zero);
             int status = xmgMagicFaceBridge.xzimgMagicFaceInitialize(bytesHandleRegressor.AddrOfPinnedObject(), bytesHandleClassifier.AddrOfPinnedObject(), (nbFaceFeatures != 730) ? System.IntPtr.Zero : bytesHandle3DModel.AddrOfPinnedObject(), ref initializationParams);
            CameraModelHandle.Free();

            if (status <= 0) Debug.Log("Initialization failed!");
        }
        else {

            int status = xmgMagicFaceBridge.xzimgRigidFaceTrackingInitialize(bytesHandleRegressor.AddrOfPinnedObject(), bytesHandleClassifier.AddrOfPinnedObject(), captureWidth, captureHeight, m_videoParameters.CameraVerticalFOV);
            if (status <= 0) Debug.Log("Initialization failed!");
        }
        bytesHandleRegressor.Free();
        bytesHandleClassifier.Free();
        bytesHandle3DModel.Free();
    }

    // -------------------------------------------------------------------------------------------------------------------

    public void SwitchCameraMobile()
    {
		#if (UNITY_ANDROID)
        if (m_videoParameters.UseFrontal)
        {
            xmgMagicFaceBridge.xzimgMagicFaceReleaseVideoCapture();
            m_videoParameters.MirrorVideo = false;
            m_videoParameters.UseFrontal = false;
            m_myWebCamEngine.GetComponent<Renderer>().material.SetInt("_Mirror", (int)(m_videoParameters.MirrorVideo == true ? 1 : 0));
            xmgMagicFaceBridge.xzimgMagicFaceInitializeVideoCapture(m_videoParameters.videoCaptureMode, m_videoParameters.UseFrontal);
        }
        else
        {
            xmgMagicFaceBridge.xzimgMagicFaceReleaseVideoCapture();
            m_videoParameters.MirrorVideo = true;
            m_videoParameters.UseFrontal = true;
            m_myWebCamEngine.GetComponent<Renderer>().material.SetInt("_Mirror", (int)(m_videoParameters.MirrorVideo == true ? 1 : 0));
            xmgMagicFaceBridge.xzimgMagicFaceInitializeVideoCapture(m_videoParameters.videoCaptureMode, m_videoParameters.UseFrontal);
        }
		#endif
    }

    // -------------------------------------------------------------------------------------------------------------------

    public void PrepareRenderObjects(bool is2D)
    {
        for (int i = 0; i < m_renderedFaceObjects.Count; i++)
        {

            if (m_renderedFaceObjects[i].m_renderPivot == null)
                m_renderedFaceObjects[i].m_renderPivot = new GameObject("FaceLayer");

            m_renderedFaceObjects[i].m_renderPivot.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
            m_renderedFaceObjects[i].m_renderPivot.transform.localScale = new Vector3(1.0f, 1.0f, 1.0f);
            m_renderedFaceObjects[i].m_renderPivot.transform.localPosition = new Vector3(0.0f, 0.0f, 1.0f);

            if (!m_renderedFaceObjects[i].m_renderPivot.GetComponent<Renderer>())
                m_renderedFaceObjects[i].m_renderPivot.AddComponent<MeshRenderer>();

            if (m_renderedFaceObjects[i].m_renderShader)
                m_renderedFaceObjects[i].m_renderPivot.GetComponent<Renderer>().material = new Material(m_renderedFaceObjects[i].m_renderShader);
            else if (is2D && m_renderedFaceObjects[i].m_renderMode == xmgRenderMode.NonRigidFace)
                m_renderedFaceObjects[i].m_renderPivot.GetComponent<Renderer>().material = new Material(Shader.Find("Custom/FaceWire2DShader"));
            else if (!is2D && m_renderedFaceObjects[i].m_renderMode == xmgRenderMode.NonRigidFaceWireframe)
                m_renderedFaceObjects[i].m_renderPivot.GetComponent<Renderer>().material = new Material(Shader.Find("Custom/FaceWire3DShader"));


            // Load Textures (if any)
            if (m_renderedFaceObjects[i].m_renderMode == xmgRenderMode.NonRigidFace && 
                m_renderedFaceObjects[i].m_renderTexture && !m_renderedFaceObjects[i].m_renderPivot.GetComponent<Renderer>().material.mainTexture)
                m_renderedFaceObjects[i].m_renderPivot.GetComponent<Renderer>().material.mainTexture = m_renderedFaceObjects[i].m_renderTexture;
        }
    }

    // -------------------------------------------------------------------------------------------------------------------

    public int GetFaceFeaturesNumber(bool is3D = true)
    {
        int nbFaceFeatures = 51;
        if (!m_useInternalFeaturesOnly && !is3D)
            nbFaceFeatures = 73;
        else if (is3D)
            nbFaceFeatures = 730;
        return nbFaceFeatures;
    }

    // -------------------------------------------------------------------------------------------------------------------

    public int LoadTextureCoordinates(bool buildDelaunayMesh = false, bool is3D = true)
    {
        
        int nbFaceFeatures = GetFaceFeaturesNumber(is3D);
        for (int o = 0; o < m_renderedFaceObjects.Count; o++)
        {
            // Load coordinates
            StreamReader SReader;
            float[] faceFeatures = new float[nbFaceFeatures * 2];
            if (is3D)
            {
                // Get texture coordinates from object file
                if (m_renderedFaceObjects[o].m_renderPivot && m_renderedFaceObjects[o].m_renderPivot.GetComponent<MeshFilter>())
                {
                    Mesh msh = m_renderedFaceObjects[o].m_renderPivot.GetComponent<MeshFilter>().mesh;
                    if (msh)
                    {
                        if (m_videoParameters.MirrorVideo)
                            msh.triangles = msh.triangles.Reverse().ToArray();
                    }
                }
            }
            else if (m_renderedFaceObjects[o].m_renderTexture)
            {
                String strFilename = m_renderedFaceObjects[o].m_renderTexture.name;// + ".txt";
                TextAsset asset = Resources.Load<TextAsset>(strFilename);

                int width = m_renderedFaceObjects[o].m_renderTextureWidth;
                int height = m_renderedFaceObjects[o].m_renderTextureHeight;
                if (asset)
                {
                    byte[] byteArray = Encoding.UTF8.GetBytes(asset.text);
                    //byte[] byteArray = Encoding.ASCII.GetBytes(contents);
                    MemoryStream stream = new MemoryStream(byteArray);

                    SReader = new StreamReader(stream);
                    Vector2[] maskUVs = new Vector2[nbFaceFeatures];
                    for (int i = 0; i < nbFaceFeatures; i++)
                    {
                        string str = SReader.ReadLine();
						//Debug.Log ("str: " + str);
                        float u = Convert.ToInt32(str.Substring(0, str.IndexOf(',')));
                        String tmp = str.Substring(str.IndexOf(',') + 1);
                        float v = Convert.ToInt32(tmp);

                        u = u / (float)width;
                        v = 1.0f - v / (float)height;
                        faceFeatures[2 * i] = u;
                        faceFeatures[2 * i + 1] = v;
                        //msh.uv[i] = new Vector2(u,v);
                        maskUVs[i].x = u;
                        maskUVs[i].y = v;

                    }

                    if (buildDelaunayMesh)
                    {
                        Debug.Log("DelaunayMesh");
                        int[] m_maskTriangles = new int[500];
                        int[] m_maskNbTriangles = new int[1];

                        // Get triangulation for the mask features
                        GCHandle maskFaceFeaturesHandle = GCHandle.Alloc(faceFeatures, GCHandleType.Pinned);
                        GCHandle maskTrianglesHandle = GCHandle.Alloc(m_maskTriangles, GCHandleType.Pinned);
                        GCHandle maskNbTrianglesHandle = GCHandle.Alloc(m_maskNbTriangles, GCHandleType.Pinned);

                        // Construct the delaunay triangulation
                        xmgMagicFaceBridge.xzimgMagicFaceDelaunayTriangulation2D(maskFaceFeaturesHandle.AddrOfPinnedObject(), faceFeatures.Length / 2, maskTrianglesHandle.AddrOfPinnedObject(), maskNbTrianglesHandle.AddrOfPinnedObject(), 0, 0);

                        // Release the 
                        maskTrianglesHandle.Free();
                        maskFaceFeaturesHandle.Free();
                        maskNbTrianglesHandle.Free();
                    }

                    Mesh msh = new Mesh();
                    msh.vertices = new Vector3[nbFaceFeatures];
                    msh.uv = maskUVs;
					if (m_renderedFaceObjects [o].m_renderPivot.GetComponent<MeshFilter> () != null)
						DestroyImmediate(m_renderedFaceObjects [o].m_renderPivot.GetComponent<MeshFilter> ());
					
                    m_renderedFaceObjects[o].m_renderPivot.AddComponent<MeshFilter>().mesh = msh;
                }
            }
            else Debug.Log("No texture defined for face replacement");
        }
        return 1;
    }

    // -------------------------------------------------------------------------------------------------------------------

    public void PrepareCamera()
	{
		// Compute correct focal length according to video capture crops and different available modes
		if (m_videoParameters.videoPlaneFittingMode == xmgVideoPlaneFittingMode.FitScreenHorizontally &&
			(xmgTools.GetRenderOrientation() == xmgOrientationMode.LandscapeLeft || xmgTools.GetRenderOrientation() == xmgOrientationMode.LandscapeRight))
		{
            float fovx = (float)xmgTools.ConvertFov(m_videoParameters.CameraVerticalFOV, m_videoParameters.GetVideoAspectRatio());
            Camera.main.fieldOfView = (float)xmgTools.ConvertFov(fovx, 1.0f / m_videoParameters.GetScreenAspectRatio());
		}
		if (m_videoParameters.videoPlaneFittingMode == xmgVideoPlaneFittingMode.FitScreenVertically &&
			(xmgTools.GetRenderOrientation() == xmgOrientationMode.LandscapeLeft || xmgTools.GetRenderOrientation() == xmgOrientationMode.LandscapeRight))
		{
            //float scaleY = (float)xmgVideoCapturePlane.GetScaleY(m_videoParameters);
            Camera.main.fieldOfView = m_videoParameters.CameraVerticalFOV;// / scaleY;
		}

		if (m_videoParameters.videoPlaneFittingMode == xmgVideoPlaneFittingMode.FitScreenHorizontally &&
			(xmgTools.GetRenderOrientation() == xmgOrientationMode.Portrait || xmgTools.GetRenderOrientation() == xmgOrientationMode.PortraitUpsideDown))
		{
			Camera.main.fieldOfView = (float)xmgTools.ConvertFov(m_videoParameters.CameraVerticalFOV, m_videoParameters.GetVideoAspectRatio());
		}        

		if (m_videoParameters.videoPlaneFittingMode == xmgVideoPlaneFittingMode.FitScreenVertically &&
			(xmgTools.GetRenderOrientation() == xmgOrientationMode.Portrait || xmgTools.GetRenderOrientation() == xmgOrientationMode.PortraitUpsideDown))
		{
			Camera.main.fieldOfView = (float)xmgTools.ConvertFov(m_videoParameters.CameraVerticalFOV, m_videoParameters.GetScreenAspectRatio());
		}
		//Debug.Log("fovy = "+ Camera.main.fieldOfView);

		Camera.main.transform.rotation = Quaternion.Euler(0.0f, 0.0f, 0.0f);
		Camera.main.transform.position = new Vector3(0.0f, 0.0f, 0.0f);
	}
}