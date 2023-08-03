using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ContentManagement : MonoBehaviour
{

	public xmgMagicFace2D xmg;
	public GameObject faceContainer;
	public GameObject ButtonPrefab;
    public GameObject textGuide;
	public int maskNumber;
	GameObject copyPrefab;
	public GameObject ButtonPrefabPlacement;
	public Sprite[] Icon;
	public Texture2D[] faceMask;
	public int[] width;
	public int[] height;

    public GameObject[] sAnis;
    int nIdxMask = 0;

	bool animate = false;

	public Texture2D[] AnimateEyes;
	public float nextFrametime = 0.2f;
	float currentTime;
    int nIdxAni = 0;

    bool m_bMOpened = false;

	void Start() {
        xmg.m_custom3DObject.SetActive(false);
        textGuide.SetActive(false);
		generateButtons ();
	}

	void generateButtons() {
		RectTransform rt = ButtonPrefabPlacement.GetComponent<RectTransform> ();
		int coef = (150 * (Icon.Length)-Screen.width);
		rt.offsetMax = new Vector2 (coef, 0);
		rt.offsetMin = new Vector2 (0, 0);

		for (int i = 0; i < Icon.Length; i++) {

			if (Icon.Length != 0) {
				ButtonPrefab.GetComponent<Image>().sprite = Icon [i] as Sprite;
			}
			copyPrefab = Instantiate (ButtonPrefab, ButtonPrefabPlacement.transform.position, ButtonPrefabPlacement.transform.rotation) as GameObject;
			if (copyPrefab.activeSelf == false)
				copyPrefab.SetActive (true);
			copyPrefab.transform.SetParent (ButtonPrefabPlacement.transform);
			copyPrefab.transform.localScale = new Vector3 (1, 1, 1);
			copyPrefab.name = i.ToString ();
		}
	}

	public void IconClick() {
		//Debug.Log ("xmg Find: " + GameObject.Find ("MagicFace2DFeatures"));
		nIdxMask = maskNumber;
		xmg.m_renderedFaceObjects [0].m_renderTexture = faceMask [nIdxMask];
		xmg.m_renderedFaceObjects [0].m_renderTextureWidth = width [nIdxMask]; // faceMask [i].width;
		xmg.m_renderedFaceObjects [0].m_renderTextureHeight = height [nIdxMask];// faceMask[i].height;
		faceContainer.GetComponent<Renderer> ().material.mainTexture = faceMask [nIdxMask];
		string faceMaskName = faceMask [nIdxMask].name;
		//Debug.Log ("faceMaskName: " + faceMaskName);

		xmg.LoadCoords ();

        m_bMOpened = false;

        //Disable all animations
        for (int i = 0; i < sAnis.Length; i++){
            if(sAnis[i]){
                sAnis[i].SetActive(false);
            }
        }
        //If animated mask, hide mask
        if(sAnis[nIdxMask]){
            xmg.m_bShowMask = false;
            textGuide.SetActive(true);
            xmg.m_custom3DObject.SetActive(true);
        }else{
            xmg.m_bShowMask = true;
            textGuide.SetActive(false);
            xmg.m_custom3DObject.SetActive(false);
        }

        //Mask Animation
		if (faceMaskName.Contains ("Animate")) {
			animate = true;
			currentTime = Time.time;
			//Debug.Log ("ANIMATE:");
		} else {
			nIdxAni = 0;
			animate = false;
		}
	}

	public void Click ()
	{
		//Debug.Log ("xmg Find: " + GameObject.Find ("MagicFace2DFeatures"));
		xmg.m_renderedFaceObjects [0].m_renderTexture = faceMask [nIdxMask];
		xmg.m_renderedFaceObjects [0].m_renderTextureWidth = faceMask [nIdxMask].width;//width [i];
			xmg.m_renderedFaceObjects [0].m_renderTextureHeight = faceMask [nIdxMask].height;//height [i];
		faceContainer.GetComponent<Renderer> ().material.mainTexture = faceMask [nIdxMask];
		string faceMaskName = faceMask [nIdxMask].name;
		//Debug.Log ("faceMaskName: " + faceMaskName);
		nIdxMask++;
		if (nIdxMask == faceMask.Length)
			nIdxMask = 0;

		xmg.LoadCoords ();

		if (faceMaskName.Contains ("Animate")) {
			animate = true;
			currentTime = Time.time;
			//Debug.Log ("ANIMATE:");
		} else {
			nIdxAni = 0;
			animate = false;
		}
	}

	void Update ()
	{
        //Mouth Open, Additional Object animation
        if(m_bMOpened == false){
            if (xmg.m_bIsMouthOpen)
            {
                //Trigger Mouth
                Debug.Log("Mouth Open!!!!");
                m_bMOpened = true;

                if (sAnis[nIdxMask])
                {
                    textGuide.SetActive(false);
                    sAnis[nIdxMask].SetActive(true);
                }
            }
        }

        if(m_bMOpened == true && sAnis[nIdxMask] && !xmg.m_bShowMask)
        {
            bool isAnimate = sAnis[nIdxMask].GetComponent<Animator>().GetCurrentAnimatorStateInfo(0).IsName(sAnis[nIdxMask].name);
            if(!isAnimate)
            {
                xmg.m_bShowMask = true;
                //sAnis[nIdxMask].SetActive(false);
            }
        }

        //Mask Animation
		if (animate) {
			if (Time.time > currentTime + nextFrametime) {
				currentTime = Time.time;
				nIdxAni++;
				if (AnimateEyes.Length - 1 > nIdxAni) {
					//Debug.Log ("GO" + AnimateEyes [j].name);
					faceContainer.GetComponent<Renderer> ().material.mainTexture = AnimateEyes [nIdxAni];
				} else {
					nIdxAni = 0;
				}
			}
		}
	}
}
