using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Events;
using Intel.RealSense;

public class RSTextureConversionManager : MonoBehaviour
{
	public RSDepthColorSplitter depthColorSplitter;
	public Texture2D inputColorTexture = null;

	// for color/depth image
	private bool is_init = false;
	public bool is_update = true;

	public Intrinsics depthIntrinsic;
	public Intrinsics colorIntrinsic;

	[Serializable]
	public class RealsenseColorStreamActiveEvent : UnityEvent<Intrinsics> { }

	[Space]
	public TextureProvider.TextureEvent colorTextureBinding;
	public TextureProvider.TextureEvent depthTextureBinding;
	public RealsenseColorStreamActiveEvent OnColorCalibrationInit;

	//	private bool isUpdated = false;

	// TODO: load intrinsics from meta data stream
	private void SetCameraParam()
	{
		depthIntrinsic = new Intrinsics();

		depthIntrinsic.width = inputColorTexture.width;
		depthIntrinsic.height = inputColorTexture.height / 2;

		depthIntrinsic.fx = 920.0f * depthIntrinsic.width / 1280.0f;
		depthIntrinsic.fy = 920.0f * depthIntrinsic.width / 1280.0f;
		depthIntrinsic.ppx = 640.0f * depthIntrinsic.width / 1280.0f;
		depthIntrinsic.ppy = 360.0f * depthIntrinsic.width / 1280.0f;

		colorIntrinsic = new Intrinsics();
		colorIntrinsic = depthIntrinsic;
	}

	// Use this for initialization
	void Initialize()
	{
		if(inputColorTexture == null)	return;

		//		To output as matrial
		//		GetComponent<MeshRenderer>().material.mainTexture = movieInputColorTexture;
		depthColorSplitter.Initialize(inputColorTexture);
		SetCameraParam();
		OnColorCalibrationInit.Invoke(colorIntrinsic);
		is_init = true;
	}

	// Update is called once per frame
	void Update()
	{
		if (!is_init)
		{
			Initialize();
			return;
		}

		if (!is_update) return;

		// Split from video stream to color and depth
		depthColorSplitter.UpdateTexture(inputColorTexture);
		colorTextureBinding.Invoke(depthColorSplitter.colorTexture);
		depthTextureBinding.Invoke(depthColorSplitter.depthTexture);
	}

	public void OnColorTextureReady(Texture texture)
	{
		inputColorTexture = (Texture2D)texture;
	}
}
