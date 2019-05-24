using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.IO;
using UnityEngine.Events;
using UnityEngine.Video;
using Intel.RealSense;

public class RSDepthVideoManager : MonoBehaviour {
	public string fileName = "/capture.mp4";

	public RSDepthColorSplitter depthColorSplitter;
	public GameObject targetCamera;

	public float frameRate = 30;

	// render texture resolution needs to be same to input video resolution
	public RenderTexture movieInputColorTexture;

	// for color/depth image
	public Intrinsics depthIntrinsic;
	public Intrinsics colorIntrinsic;
	public float depthScale;

	[Serializable]
	public class RealsenseColorStreamActiveEvent : UnityEvent<Intrinsics> { }

	[Space]
	public TextureProvider.TextureEvent colorTextureBinding;
	public TextureProvider.TextureEvent depthTextureBinding;
	public RealsenseColorStreamActiveEvent OnColorCalibrationInit;

	private VideoPlayer videoPlayer = null;
	private float startTime = 0;

	//	private bool isUpdated = false;

	// TODO: load intrinsics from meta data stream
	private void SetCameraParam()
	{
		depthIntrinsic = new Intrinsics();

		depthIntrinsic.width = movieInputColorTexture.width;
		depthIntrinsic.height = movieInputColorTexture.height / 2;

		depthIntrinsic.fx = 930.0f * depthIntrinsic.width / 1280.0f;
		depthIntrinsic.fy = 930.0f * depthIntrinsic.width / 1280.0f;
		depthIntrinsic.ppx = 640.0f * depthIntrinsic.width / 1280.0f;
		depthIntrinsic.ppy = 360.0f * depthIntrinsic.width / 1280.0f;
		depthScale = 1.0f;

		colorIntrinsic = new Intrinsics();
		colorIntrinsic = depthIntrinsic;
	}

	// Use this for initialization
	void Start () {
		// for Android player
		/*
				Debug.Log("Loading from Movie folder...");
				string moviePath = "";
				using (AndroidJavaClass jcEnvironment = new AndroidJavaClass("android.os.Environment"))
				using (AndroidJavaObject joExDir = jcEnvironment.CallStatic<AndroidJavaObject>("getExternalStorageDirectory"))
				{
					moviePath = joExDir.Call<string>("toString") + "/Movies";
				}
				// make directly if not exist
				if (!Directory.Exists(moviePath)) Directory.CreateDirectory(moviePath);
				// input file name
				moviePath += fileName;
		*/
		var moviePath = Application.streamingAssetsPath + fileName;
//		Debug.Log(moviePath);

		videoPlayer = targetCamera.AddComponent<VideoPlayer>();
		videoPlayer.playOnAwake = false;
		videoPlayer.renderMode = UnityEngine.Video.VideoRenderMode.RenderTexture;
		videoPlayer.targetTexture = movieInputColorTexture;
		videoPlayer.targetCameraAlpha = 1.0f;
		videoPlayer.url = moviePath;
		videoPlayer.frame = 0;
		//		videoPlayer.isLooping = true;
		//		videoPlayer.loopPointReached += EndReached;
		videoPlayer.Play();
		startTime = Time.time;

		//		To output as matrial
		//		GetComponent<MeshRenderer>().material.mainTexture = movieInputColorTexture;
		depthColorSplitter.Initialize(movieInputColorTexture);

		SetCameraParam();

		OnColorCalibrationInit.Invoke(colorIntrinsic);
	}

	// Update is called once per frame
	void Update()
	{
		if (videoPlayer.frame < (long)videoPlayer.frameCount)
		{
			var frameNumber = (int)(frameRate * (Time.time - startTime));
			for(int i=0; i< (frameNumber - videoPlayer.frame); i++)
			{
				videoPlayer.frame++;
			}

			//	videoPlayer.frame = frameNumber;
		}
		else
		{
			videoPlayer.Play();
			videoPlayer.frame = 0;
			startTime = Time.time;
		}
		// Split from video stream to color and depth
		depthColorSplitter.UpdateTexture(movieInputColorTexture);
		colorTextureBinding.Invoke(depthColorSplitter.colorTexture);
		depthTextureBinding.Invoke(depthColorSplitter.depthTexture);
	}
}
