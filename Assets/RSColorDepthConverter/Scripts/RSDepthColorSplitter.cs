using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;

public class RSDepthColorSplitter: MonoBehaviour
{
	public ComputeShader computeShader;

	public float depthMin = 0.3f;
	public float depthMax = 2.0f;
	public float depthUnits = 0.001f;
	public int	 postErode = 2;

	private int imageWidth = 0;
	private int imageHeight = 0;
	private float stereoBaseline = 0.05f;
	private bool isInit = false;

	public RenderTexture colorTexture { get; private set; }
	public RenderTexture depthTexture { get; private set; }

	public void Initialize(RenderTexture inColorTexture)
	{
		if (inColorTexture == null) return;

		imageWidth = inColorTexture.width;
		imageHeight = inColorTexture.height / 2;

		computeShader.SetInt("image_width", imageWidth);
		computeShader.SetInt("image_height", imageHeight);

		colorTexture = new RenderTexture(imageWidth, imageHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
		colorTexture.filterMode = FilterMode.Point;
		colorTexture.enableRandomWrite = true;
		colorTexture.Create();

		depthTexture = new RenderTexture(imageWidth, imageHeight, 0, RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear);
		depthTexture.filterMode = FilterMode.Point;
		depthTexture.enableRandomWrite = true;
		depthTexture.Create();

		GetComponent<Renderer>().material.mainTexture = depthTexture;

		isInit = true;
	}

	public void Initialize(Texture2D inColorTexture)
	{
		if (inColorTexture == null) return;

		imageWidth = inColorTexture.width;
		imageHeight = inColorTexture.height / 2;

		computeShader.SetInt("image_width", imageWidth);
		computeShader.SetInt("image_height", imageHeight);

		colorTexture = new RenderTexture(imageWidth, imageHeight, 0, RenderTextureFormat.ARGB32, RenderTextureReadWrite.Linear);
		colorTexture.filterMode = FilterMode.Point;
		colorTexture.enableRandomWrite = true;
		colorTexture.Create();

		depthTexture = new RenderTexture(imageWidth, imageHeight, 0, RenderTextureFormat.RHalf, RenderTextureReadWrite.Linear);
		depthTexture.filterMode = FilterMode.Point;
		depthTexture.enableRandomWrite = true;
		depthTexture.Create();

//		GetComponent<Renderer>().material.mainTexture = depthTexture;

		isInit = true;
	}


	// Update is called once per frame
	public void UpdateTexture(RenderTexture inColorTexture)
	{
		if (!isInit) return;

		// set uniform values to shader
		var kernelNo = 0;
		computeShader.SetFloat("depth_min", depthMin);
		computeShader.SetFloat("depth_max", depthMax);
		computeShader.SetFloat("depth_units", depthUnits);
		computeShader.SetFloat("stereo_baseline", stereoBaseline);
		computeShader.SetTexture(kernelNo, "in_color_texture", inColorTexture);
		computeShader.SetTexture(kernelNo, "out_depth_texture", depthTexture);
		computeShader.SetTexture(kernelNo, "out_color_texture", colorTexture);
		computeShader.Dispatch(kernelNo, imageWidth / 8, imageHeight / 8, 1);

		kernelNo = 2;
		// post erode
		computeShader.SetTexture(kernelNo, "out_depth_texture", depthTexture);
		for (int i = 0; i < postErode; i++)
		{
			computeShader.Dispatch(kernelNo, imageWidth / 8, imageHeight / 8, 1);
		}
	}

	public void UpdateTexture(Texture2D inColorTexture)
	{
		if (!isInit) return;

		// set uniform values to shader
		var kernelNo = 0;
		computeShader.SetFloat("depth_min", depthMin);
		computeShader.SetFloat("depth_max", depthMax);
		computeShader.SetFloat("depth_units", depthUnits);
		computeShader.SetFloat("stereo_baseline", stereoBaseline);
		computeShader.SetTexture(kernelNo, "in_color_texture", inColorTexture);
		computeShader.SetTexture(kernelNo, "out_depth_texture", depthTexture);
		computeShader.SetTexture(kernelNo, "out_color_texture", colorTexture);
		computeShader.Dispatch(kernelNo, imageWidth / 8, imageHeight / 8, 1);

		kernelNo = 2;
		// post erode
		computeShader.SetTexture(kernelNo, "out_depth_texture", depthTexture);
		for (int i = 0; i < postErode; i++)
		{
			computeShader.Dispatch(kernelNo, imageWidth / 8, imageHeight / 8, 1);
		}
	}

	public void OnStereoBaselineInit(float inBaseline)
	{
		stereoBaseline = inBaseline;
	}
	public void OnDepthUnitsInit(float inDepthUnits)
	{
		depthUnits = inDepthUnits;
	}
}
