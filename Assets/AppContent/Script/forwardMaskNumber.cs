using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class forwardMaskNumber : MonoBehaviour {
	public ContentManagement ContentManagement;
	Button btn;
	// Use this for initialization
	void Start () {
		btn = GetComponent<Button> ();
		btn.onClick.AddListener (forward);
	}
	
	// Update is called once per frame
	void forward () {
		ContentManagement.maskNumber = int.Parse(this.gameObject.name);
		ContentManagement.IconClick();
	}
}
