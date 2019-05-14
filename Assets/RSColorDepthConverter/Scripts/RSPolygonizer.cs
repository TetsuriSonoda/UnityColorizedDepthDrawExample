using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

public class RSPolygonizer : MonoBehaviour
{
	public int point_width = 576 / 2;
    public int point_height = 464 / 4;

	public float depth_threshold1 = 0.66f;
	public float depth_threshold2 = 1.3f;

	private Mesh _mesh;
	private bool is_inited = false;
	private int[] point_indices;
	private List<Vector2> point_uvs;

	private Texture color_texture;
	private Texture depth_texture;

    private Vector3[] point_cloud;
	private Intel.RealSense.Intrinsics depth_intrinsics;
	private Renderer _renderer;

	// Use this for initialization
	void Start()
    {
        _mesh = GetComponent<MeshFilter>().mesh;
		_renderer = GetComponent<Renderer>();
		_mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
		is_inited = false;
	}

	private void Initialize()
	{
		// initialize point cloud
		int num_vertices = point_width * point_height;

		// initialize point cloud
		point_cloud = new Vector3[point_width * point_height];

		for (int y = 0; y < point_height; y++)
		{
			for (int x = 0; x < point_width; x++)
			{
				float depth_value = 1.0f;
				float bias = 1280 / (float)point_width;
				point_cloud[point_width * y + x].x = (point_width / 2 - x) * depth_value * 0.0007f * bias;
				point_cloud[point_width * y + x].y = (point_height / 2 - y) * depth_value * 0.001f * bias;
				if (x < point_width / 2)
				{
					point_cloud[point_width * y + x].z = depth_value;
				}
				else
				{
					point_cloud[point_width * y + x].z = depth_value / 2;
				}
			}
		}

		point_indices = new int[num_vertices];
		for (int i = 0; i < num_vertices; i++)
		{
			point_indices[i] = i++;
		}

		point_uvs = new List<Vector2>();
		for (int y = 0; y < point_height; y++)
		{
			for (int x = 0; x < point_width; x++)
			{
				point_uvs.Add(new Vector2((float)x / (point_width - 1),
										 ((float)y / (point_height - 1))));
			}
		}

		_mesh.Clear();
		_mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
		_mesh.vertices = point_cloud;
		_mesh.SetIndices(point_indices, MeshTopology.Points, 0);
		_mesh.SetUVs(0, point_uvs);

		is_inited = true;
		_renderer.enabled = true;
	}

	// Update is called once per frame
	void Update()
    {
        if (!is_inited) { Initialize(); }

		if(depth_texture){
			_renderer.material.SetTexture("_DepthTex", depth_texture);
        }
		else{	return; }

		if(color_texture){
			_renderer.material.SetTexture("_SpriteTex", color_texture);
		}

		_renderer.material.SetMatrix("_UNITY_MATRIX_M", transform.localToWorldMatrix);
		_renderer.material.SetFloat("_FX", depth_intrinsics.fx);       // 1.896f
		_renderer.material.SetFloat("_FY", depth_intrinsics.fy);        // 1.533f
		_renderer.material.SetFloat("_PPX", depth_intrinsics.ppx);       // 1.896f
		_renderer.material.SetFloat("_PPY", depth_intrinsics.ppy);        // 1.533f
		_renderer.material.SetFloat("_SizeX", 0.0005f);
		_renderer.material.SetFloat("_SizeY", 0.0005f);
		_renderer.material.SetFloat("_DepthThreshold1", depth_threshold1);
		_renderer.material.SetFloat("_DepthThreshold2", depth_threshold2);

		_renderer.material.SetFloat("_GridPitch", 0.2f);
		_renderer.material.SetFloat("_LineWidth", 0.06f);
	}

	public void OnColorCalibrationInit(Intel.RealSense.Intrinsics intrinsic)
	{
		depth_intrinsics = intrinsic;
		depth_intrinsics.fx = intrinsic.fx / intrinsic.width;
		depth_intrinsics.fy = intrinsic.fy / intrinsic.height;
		depth_intrinsics.ppx = intrinsic.ppx / intrinsic.width;
		depth_intrinsics.ppy = intrinsic.ppy / intrinsic.height;
	}

	public void OnDepthTextureReady(Texture texture)
	{
		depth_texture = texture;
		//		Debug.Log("ShadowOccusionPolygon:Texture Set");
	}

	public void OnColorTextureReady(Texture texture)
	{
		color_texture = texture;
		//		Debug.Log("ShadowOccusionPolygon:Texture Set");
	}

}
