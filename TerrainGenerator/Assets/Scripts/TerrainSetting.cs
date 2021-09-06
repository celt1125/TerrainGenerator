using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class TerrainSetting
{
	[Header("Terrain settings")]
	public int layers = 4;
	public float lacunarity = 2f;
	public float persistence = 0.5f;
	public int resolution = 500;
	public float scale = 100f;
	public Vector2 shift = new Vector2(0f, 0f);
	public float amplitude = 1f;
	public float smoothness = 0.8f;
	
	[Header("Erosion settings")]
	public int iterations = 100;
	public float delta_time = 0.02f;
	public float rain_rate = 0.012f;
	public float evaporation_rate = 0.015f;
	public float pipe_area = 20f;
	public float gravity = 9.81f;
	public float sediment_capacity = 1f;
	public float sediment_suspension_rate = 0.5f;
	public float sediment_deposition_rate = 1f;
	
}	