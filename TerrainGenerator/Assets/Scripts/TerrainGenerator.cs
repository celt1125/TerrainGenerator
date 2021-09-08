using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainGenerator : MonoBehaviour
{
	private static Dictionary<int, BaseData> base_terrain;
	private float[] height;
	private float[] water_height;
	private BaseData current_base;
	private Mesh mesh;
	private Vector2 min_max = new Vector2(0f, 0f);
	private Texture2D texture;
	private int texture_resolution = 50;
	private Erosion erosion;
	
	public Material material;
	public TerrainSetting settings;
	public Gradient gradient;
	public bool enable_erosion = false;
	public bool ShowWaterHeight = false;
	public bool UsingGPU = false;
	
	
	void Start(){
		if (GetComponent<MeshFilter>() == null){
			gameObject.AddComponent<MeshFilter>();
			gameObject.AddComponent<MeshRenderer>();
		}
		if (GetComponent<MeshFilter>().sharedMesh == null)
			GetComponent<MeshFilter>().sharedMesh = new Mesh();
		
		mesh = GetComponent<MeshFilter>().sharedMesh;
		GetComponent<MeshRenderer>().material = material;
		
		SetBase(settings.resolution);
		SetHeight(settings);
		
		texture = new Texture2D(texture_resolution, 1);
		SetMaterial();
		
		erosion = GetComponent<Erosion>();
	}
	
	void Update(){
		int n = settings.resolution;
		if (current_base == null)
			SetBase(n);
		if (current_base.resolution != n)
			SetBase(n);

		//UpdateHeight();
		if (enable_erosion){
			if (erosion == null)
				erosion = GetComponent<Erosion>();

			//Debug.Log(min_max.y);
			//erosion.ErodeMethod1(height, 1f / n, n, settings, water_height);
			if (UsingGPU)
				erosion.ErodeMethod2_GPU(height, 1f / n, n, settings);
			else
				erosion.ErodeMethod2_CPU(height, 1f / n, n, settings);
			enable_erosion = false;
			UpdateHeight();
			//Debug.Log(min_max.y);
		}
	}
	
	private void SetBase(int n){
		if (base_terrain == null)
			base_terrain = new Dictionary<int, BaseData>();
		if (texture == null)
			texture = new Texture2D(texture_resolution, 1);
		
		if (!base_terrain.ContainsKey(n))
			base_terrain.Add(n, new BaseData(n));
		
		current_base = base_terrain[n];
		
		mesh.Clear();
		mesh.indexFormat = (current_base.vertices.Length < 65535) ?
					UnityEngine.Rendering.IndexFormat.UInt16 : UnityEngine.Rendering.IndexFormat.UInt32;
		mesh.SetVertices(current_base.vertices);
		mesh.SetTriangles(current_base.triangles, 0, true);
	}
	
	private void SetHeight(TerrainSetting s){
		int layers = s.layers;
		float lacunarity = s.lacunarity;
		float persistence = s.persistence;
		float scale = s.scale;
		Vector2 shift = s.shift;
		float amplitude = s.amplitude;
		float smoothness = s.smoothness;
		
		int n = current_base.resolution;
		Vector3[] new_vertices = new Vector3[n * n];
		height = new float[n * n]; 
		water_height = new float[n * n];

		for (int i = 0; i < n; i++){ // z
			for (int j = 0; j < n; j++){ // x
				scale = s.scale;
				amplitude = s.amplitude;
				for (int layer = 0; layer < layers; layer++){
					height[i * n + j] += amplitude * Mathf.PerlinNoise(	(float)j / (n - 1) * scale + shift.x,
																		(float)i / (n - 1) * scale + shift.y);
					//height = SmoothMax(height, 0f, smoothness);
					scale *= lacunarity;
					amplitude *= persistence;
				}
				new_vertices[i * n + j] += new Vector3(0f, height[i * n + j], 0f);
				new_vertices[i * n + j] += current_base.vertices[i * n + j];
				
				min_max.x = min_max.x > new_vertices[i * n + j].y ? new_vertices[i * n + j].y : min_max.x;
				min_max.y = min_max.y > new_vertices[i * n + j].y ? min_max.y : new_vertices[i * n + j].y;
			}
		}
		
		mesh.SetVertices(new_vertices);
		SetMaterial();
		mesh.RecalculateNormals();
	}
	
	private void UpdateHeight(){
		int n = current_base.resolution;
		Vector3[] new_vertices = new Vector3[n * n];
		
		for(int i = 0; i < n * n; i++){
			float h = ShowWaterHeight ? water_height[i] : height[i];
			new_vertices[i] = new Vector3(current_base.vertices[i].x, h, current_base.vertices[i].z);
			min_max.x = min_max.x > h ? h : min_max.x;
			min_max.y = min_max.y > h ? min_max.y : h;
		}
		mesh.SetVertices(new_vertices);
		SetMaterial();
		mesh.RecalculateNormals();
	}
	
	private void SetMaterial(){
		material.SetVector("_min_max", new Vector4(min_max.x, min_max.y));
		Color[] colors = new Color[texture_resolution];
		for (int i = 0; i < texture_resolution; i++)
			colors[i] = gradient.Evaluate(i / (texture_resolution - 1f));
			
		texture.SetPixels(colors);
		texture.Apply();
		material.SetTexture("_texture", texture);
	}
	
	float SmoothMax(float a, float b, float k){
		k = Mathf.Min(0, -k);
		float h = Mathf.Max(0, Mathf.Min(1, (b - a + k) / (2 * k)));
		return a * h + b * (1 - h) - k * h * (1 - h);
	}
}

public class BaseData{
	public Vector3[] vertices;
	public int[] triangles;
	public int resolution;
	
	public BaseData(int n){
		vertices = new Vector3[n * n];
		for (int i = 0; i < n; i++) // z
			for (int j = 0; j < n; j++) // x
				vertices[i * n + j] = new Vector3((float)j / (n - 1), 0f, (float)i / (n - 1));
		
		triangles = new int[((n - 1) * (n - 1) * 3) << 1];
		int ptr = 0;
		for (int i = 0; i < n - 1; i++){ // z
			for (int j = 0; j < n - 1; j++){ // x
				triangles[ptr] = i * n + j;
				triangles[ptr + 1] = (i + 1) * n + j;
				triangles[ptr + 2] = i * n + j + 1;
				
				triangles[ptr + 3] = i * n + j + 1;
				triangles[ptr + 4] = (i + 1) * n + j;
				triangles[ptr + 5] = (i + 1) * n + j + 1;
				ptr += 6;
			}
		}
		
		resolution = n;
	}
	
	public void Print(){
		foreach(Vector3 v in vertices)
			Debug.Log(v);
		for (int i = 0; i < resolution; i += 3)
			Debug.Log($"{triangles[i]}, {triangles[i + 1]}, {triangles[i + 2]}");
	}
}