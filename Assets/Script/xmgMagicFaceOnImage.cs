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
using System.IO;

using System.Text;

/// <summary>
///  DO NOT USE // NOT READY
/// </summary>
public class xmgMagicFaceOnImage : MonoBehaviour
{
    [Tooltip("Static Image to be proceeded")]
    public Texture2D inputImage;

    [Tooltip("Coefficient to indicates the strength of aging filter [0..1]")]
    public float agingCoefficient = 0.7f;
    
    bool mInitialized = false;
    
    private xmgMagicFaceBridge.xmgImage staticImage;
    private GCHandle m_texturePixelsHandle;
    Color32[] m_textureData;
    private xmgMagicFaceBridge.xmgNonRigidFaceData nonRigidData;

    private xmgMagicFaceBridge.xmgImage transformedImage;
    private GCHandle m_transformedImageTexPixelsHandle;
    private Color32[] m_transformedImageTexData;
    private Texture2D m_transformedImageTex;

    float[] m_dataLandmarks2D;
    GCHandle m_dataLandmarks2DHandle;
    float[] m_dataLandmarks3D;
    GCHandle m_dataLandmarks3DHandle;
    int[] m_dataTriangles;
    GCHandle m_dataTrianglesHandle;
    private xmgMagicFaceBridge.xmgVideoCaptureOptions m_videoCaptureOptions;

    // -------------------------------------------------------------------------------------------------------------------

    void Awake()
    {
        mInitialized = false;
        int nbFaceFeatures = 68;


        Debug.Log("Initialization...");
        // first asset
        TextAsset textAsset;
        if (nbFaceFeatures == 51)
            textAsset = Resources.Load("regressor-51LM") as TextAsset;
        else
            textAsset = Resources.Load("regressor-68LM-color-long") as TextAsset;
        GCHandle bytesHandleRegressor = GCHandle.Alloc(textAsset.bytes, GCHandleType.Pinned);

        textAsset = Resources.Load("faceClassifier-51LM") as TextAsset; 
        GCHandle bytesHandleClassifier = GCHandle.Alloc(textAsset.bytes, GCHandleType.Pinned);

        xmgMagicFaceBridge.xmgInitParams initializationParams = new xmgMagicFaceBridge.xmgInitParams();
        xmgMagicFaceBridge.PrepareInitParams(ref initializationParams, false, 640, 480, nbFaceFeatures, 1, 50.0f, System.IntPtr.Zero);
        int classifierFound = xmgMagicFaceBridge.xzimgMagicFaceInitialize(bytesHandleRegressor.AddrOfPinnedObject(), bytesHandleClassifier.AddrOfPinnedObject(), System.IntPtr.Zero, ref initializationParams);
        if (classifierFound <= 0) Debug.Log(" Failed - No classifier loaded!");
        else Debug.Log(" Success - Classifier loaded!");

        bytesHandleRegressor.Free();
        bytesHandleClassifier.Free();

        // Load test image
        xmgMagicFaceBridge.PrepareImage(ref staticImage, inputImage.width, inputImage.height, 4, IntPtr.Zero);

        // Prepare transformed image
        xmgMagicFaceBridge.PrepareImage(ref transformedImage, inputImage.width, inputImage.height, 4, IntPtr.Zero);

        // Data for exchanging information with the plugin
        m_dataLandmarks2D = new float[100 * 2];
        m_dataLandmarks2DHandle = GCHandle.Alloc(m_dataLandmarks2D, GCHandleType.Pinned);
        m_dataLandmarks3D = new float[100 * 3];
        m_dataLandmarks3DHandle = GCHandle.Alloc(m_dataLandmarks3D, GCHandleType.Pinned);
        m_dataTriangles = new int[500];
        m_dataTrianglesHandle = GCHandle.Alloc(m_dataTriangles, GCHandleType.Pinned);

        nonRigidData.m_landmarks = m_dataLandmarks2DHandle.AddrOfPinnedObject();
        nonRigidData.m_landmarks3D = m_dataLandmarks3DHandle.AddrOfPinnedObject();
        nonRigidData.m_triangles = m_dataTrianglesHandle.AddrOfPinnedObject();

        // Face aging engine
        int ret = xmgMagicFaceAgingBridge.xzimgMagicFaceAgingInitialize();
        Debug.Log("aging classifiers correctly loaded" + ret);

        m_transformedImageTex = new Texture2D(inputImage.width, inputImage.height, TextureFormat.RGBA32, false);
        m_transformedImageTexData = new Color32[inputImage.width * inputImage.height];


        if (!inputImage)
            Debug.Log("image error");
        mInitialized = true;
    }

    // -------------------------------------------------------------------------------------------------------------------

    void OnDisable() 
	{
        m_dataLandmarks2DHandle.Free();
        m_dataLandmarks3DHandle.Free();
        m_dataTrianglesHandle.Free();
        xmgMagicFaceAgingBridge.xzimgMagicFaceAgingRelease();
        xmgMagicFaceBridge.xzimgMagicFaceRelease();
    }

    // -------------------------------------------------------------------------------------------------------------------

    void Update()
    {
        Renderer[] renderers;
        renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers) r.enabled = false;
        if (!mInitialized) return;

        // Process image 
        m_textureData = inputImage.GetPixels32();
        m_texturePixelsHandle = GCHandle.Alloc(m_textureData, GCHandleType.Pinned);
        staticImage.m_imageData = m_texturePixelsHandle.AddrOfPinnedObject();
        xmgMagicFaceBridge.xzimgMagicFaceDetectNonRigidFaces2D(ref staticImage, 0);
        xmgMagicFaceBridge.xzimgMagicFaceGetFaceData(0, ref nonRigidData);

        if (nonRigidData.m_faceDetected > 0)
        {
            m_transformedImageTexData = m_transformedImageTex.GetPixels32();
            m_transformedImageTexPixelsHandle = GCHandle.Alloc(m_transformedImageTexData, GCHandleType.Pinned);
            transformedImage.m_imageData = m_transformedImageTexPixelsHandle.AddrOfPinnedObject();
            xmgMagicFaceAgingBridge.xzimgMagicFaceAgingProcess(ref staticImage, nonRigidData.m_landmarks, nonRigidData.m_nbLandmarks, agingCoefficient, ref transformedImage);
            m_transformedImageTex.SetPixels32(m_transformedImageTexData);
            m_transformedImageTex.Apply();
        }

        m_texturePixelsHandle.Free();
        m_transformedImageTexPixelsHandle.Free();
    }

    // -------------------------------------------------------------------------------------------------------------------

    public void OnGUI()
    {
        {
            int dx = 0; int dy = 0;
            float scale = 1.0f;
            GUI.DrawTexture(new Rect(dx, dy, inputImage.width * scale, inputImage.height * scale), m_transformedImageTex, ScaleMode.ScaleToFit);

            GUILayout.Label("Face#: " + nonRigidData.m_faceDetected + " - Land#: " + nonRigidData.m_nbLandmarks);
        }
    }

}
