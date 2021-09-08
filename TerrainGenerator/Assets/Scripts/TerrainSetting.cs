using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TerrainSetting
{
	[Header("Terrain settings")]
	public int layers = 4;
	public float lacunarity = 2.74f;
	public float persistence = 0.29f;
	public int resolution = 200;
	public float scale = 2.55f;
	public Vector2 shift = new Vector2(0f, 0f);
	public float amplitude = 0.33f;
	public float smoothness = 0.62f;
	
	[Header("Erosion settings")]
	public int iterations = 100;
	public float delta_time = 0.1f;
	[Range(0, 0.5f)]
	public float rain_rate = 0.01f;
	[Range(0, 1f)]
	public float evaporation_rate = 0.015f;
	[Range(0.001f, 1000f)]
	public float pipe_area = 10f;
	public float gravity = 9.81f;
	public float sediment_capacity = 1f;
	public float sediment_suspension_rate = 0.5f;
	public float sediment_deposition_rate = 1f;

	[Header("Drop settings")]
	public int drop_life_time = 30;
	public float inertia = 0.05f;
	public float drop_volume = 0.1f;
	public int brush_radius = 3;
	public int seed = 5731;
	
}	