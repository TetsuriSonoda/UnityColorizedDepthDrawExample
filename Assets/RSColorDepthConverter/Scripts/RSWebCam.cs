using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RSWebCam : MonoBehaviour {
	public int cameraIndex;
	[Space]
	public TextureProvider.TextureEvent colorTextureBinding;
	private WebCamTexture webcamTexture;

	// Use this for initialization
	void Start () {
		var deviceNames = new List<string>();
		for(int i=0; i < WebCamTexture.devices.Length; i++)
		{
			deviceNames.Add(WebCamTexture.devices[i].name);
			Debug.Log(i + " " + WebCamTexture.devices[i].name);
		}
		webcamTexture = new WebCamTexture(deviceNames[cameraIndex]);
		webcamTexture.Play();
		GetComponent<Renderer>().material.mainTexture = webcamTexture;
		colorTextureBinding.Invoke((Texture)webcamTexture);
	}
}
