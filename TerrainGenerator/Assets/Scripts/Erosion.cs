using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Erosion : MonoBehaviour
{
	public ComputeShader erosion_compute;

	private Brush[,] erosion_brush;
	private int previous_n;
	private int previous_radius;
	private int[] compute_kernels = new int[2];
	
	void Awake()
    {
		previous_n = 0;
		previous_radius = 0;
		compute_kernels[0] = erosion_compute.FindKernel("InitializeErosionBrush");

		InitializeErosionBrush(10, 3);
	}

	void Start()
    {
		//Debug.DrawLine(new Vector3(0f, 0f, 0f), new Vector3(0f, 3f, 0f), Color.white, 5f);
    }

	public void ErodeMethod1(float[] height, float cell_len, int n, TerrainSetting s, float[] water_height){
		//water_height = new float[n * n];
		Vector4[] flux = new Vector4[n * n];
		Vector2[] velocity = new Vector2[n * n];
		float[] flux_out = new float[n * n];
		float [] sediment = new float[n * n];
		float squared_cell_len = cell_len * cell_len;
		
		float delta_time = s.delta_time;
		float rain_rate = s.rain_rate;
		float evaporation_rate = s.evaporation_rate;
		float pipe_area = s.pipe_area;
		float gravity = s.gravity;
		float sediment_capacity = s.sediment_capacity;
		float sediment_suspension_rate = s.sediment_suspension_rate;
		float sediment_deposition_rate = s.sediment_deposition_rate;
		
		for (int i = 0; i < s.iterations; i++){
			// Update the water_height
			for (int z = 1; z < n - 1; z++)
				for (int x = 1; x < n - 1; x++)
					water_height[z*n+x] += RainFunction(x, z, n) * rain_rate * delta_time;

			// Update the flux
			for (int z = 1; z < n - 1; z++){
				for (int x = 1; x < n - 1; x++){
					// (x, y, z, w) = (f^L, f^R, f^T, f^B)
					flux[z*n+x].x = Mathf.Max(0f, flux[x*n+x].x + delta_time * pipe_area * gravity / cell_len 
									* (height[z*n+x] - height[z*n+x-1] + water_height[z*n+x] - water_height[z*n+x-1]));
					flux[z*n+x].y = Mathf.Max(0f, flux[x*n+x].y + delta_time * pipe_area * gravity / cell_len 
									* (height[z*n+x] - height[z*n+x+1] + water_height[z*n+x] - water_height[z*n+x+1]));
					flux[z*n+x].z = Mathf.Max(0f, flux[x*n+x].z + delta_time * pipe_area * gravity / cell_len 
									* (height[z*n+x] - height[(z+1)*n+x] + water_height[z*n+x] - water_height[(z+1)*n+x]));
					flux[z*n+x].w = Mathf.Max(0f, flux[x*n+x].w + delta_time * pipe_area * gravity / cell_len 
									* (height[z*n+x] - height[(z-1)*n+x] + water_height[z*n+x] - water_height[(z-1)*n+x]));
					
					flux_out[z*n+x] = flux[z*n+x].x + flux[z*n+x].y + flux[z*n+x].z + flux[z*n+x].w;
					if (flux_out[z*n+x] != 0f){
						float K = Mathf.Min(1f, water_height[z*n+x] * squared_cell_len / delta_time / flux_out[z*n+x]);
						flux[z*n+x] *= K;
						flux_out[z*n+x] *= K;
					}
					
					if (float.IsNaN(flux[z*n+x].x) || float.IsNaN(flux[z*n+x].y) ||
						float.IsNaN(flux[z*n+x].z) || float.IsNaN(flux[z*n+x].w)){
						Debug.Log($"{flux[z*n+x].x} + {flux[z*n+x].y} + {flux[z*n+x].z} + {flux[z*n+x].w}");
						Debug.Log(water_height[z*n+x]);
						Debug.Log(water_height[(z+1)*n+x]);
						Debug.Log(flux_out[z*n+x]);
						Debug.Log($"{i}, {x}, {z}");
						return;
					}
				}
			}
			
			for (int z = 1; z < n - 1; z++){
				for (int x = 1; x < n - 1; x++){
					// Update water_height
					float water_height_change = delta_time *
									(flux[z*n+x+1].x + flux[z*n+x-1].y + flux[(z-1)*n+x].z + flux[(z+1)*n+x].w - flux_out[z*n+x])	;
					//float water_height_avg = Mathf.Max(0.01f, water_height[z*n+x] + water_height_change * 0.5f);
					water_height[z*n+x] += water_height_change / squared_cell_len;
					water_height[z * n + x] = Mathf.Max(0f, water_height[z * n + x]);

					// Calculate velocity field
					velocity[z * n + x].x = (flux[z * n + x - 1].y - flux[z * n + x].x + flux[z * n + x].y - flux[z * n + x + 1].x) *
										0.5f; // / water_height_avg / cell_len;
					velocity[z * n + x].y = (flux[(z - 1) * n + x].z - flux[z * n + x].w + flux[z * n + x].z - flux[(z + 1) * n + x].w) *
										0.5f; // / water_height_avg / cell_len;
										
					if (float.IsNaN(velocity[z*n+x].x)){
						Debug.Log($"{flux[z*n+x+1].x} + {flux[z*n+x-1].y} + {flux[(z-1)*n+x].z} + {flux[(z+1)*n+x].w} - {flux_out[z*n+x]}");
						//Debug.Log(water_height_avg);
						Debug.Log($"{i}, {x}, {z}");
						return;
					}
					
					// Calculate local tilt angle
					Vector3 tangent_x = new Vector3(2f * cell_len, height[z*n+x+1] - height[z*n+x-1], 0f);
					Vector3 tangent_z = new Vector3(0f, height[(z+1)*n+x] - height[(z-1)*n+x], 2f * cell_len);
					Vector3 normal = Vector3.Cross(tangent_x, tangent_z).normalized;
					float sin_alpha = normal.y < 0 ? -normal.y : normal.y;
					
					// Calculate sediment transport capacity
					float l_max = Mathf.Clamp(1 - Mathf.Max(0, 0.5f - water_height[z*n+x]) / 0.5f, 0f, 1f);
					float magic = 20f;
					float C = sediment_capacity * Mathf.Max(0.05f, sin_alpha) * velocity[z*n+x].magnitude * l_max * magic;
					
					// Update height and set sediment value
					if (C > sediment[z*n+x]){
						float sediment_change = delta_time * sediment_suspension_rate * (C - sediment[z*n+x]) * magic;
						//Debug.Log(sediment_change);
						height[z*n+x] -= sediment_change;
						sediment[z*n+x] += sediment_change;
						//water_height[z*n+x] += sediment_change;
					}
					else{
						float sediment_change = delta_time * sediment_deposition_rate * (sediment[z*n+x] - C) * magic;
						height[z*n+x] += sediment_change;
						sediment[z*n+x] -= sediment_change;
						//water_height[z*n+x] -= sediment_change;
					}

					// Clamping all variables
					height[z*n+x] = Mathf.Max(0f, height[z*n+x]);
					sediment[z*n+x] = Mathf.Max(0f, sediment[z*n+x]);
				}
			}
			
			
			for (int z = 1; z < n - 1; z++){
				for (int x = 1; x < n - 1; x++){
					// Update sediment for sediment transportation
					float sediment_x = x - velocity[z*n+x].x * delta_time;
					float sediment_z = z - velocity[z*n+x].y * delta_time;
					//Debug.Log($"{x}, {z}, {velocity[z*n+x].x}, {velocity[z*n+x].y}");
					sediment[z*n+x] = Interpolation(sediment, sediment_x, sediment_z, n);
					
					// Evaporation
					water_height[z*n+x] *= (1 - evaporation_rate * delta_time);
				}
			}
		}
		Debug.Log("finished");
	}

	private float RainFunction(int x, int z, int n){
		int r = Random.Range(0, 2);
		return r == 0 ? 0f : 1f;
	}
	
	private float Interpolation(float[] arr, float x, float y, int n){
		x = Mathf.Clamp(x, 0, n - 1);
		y = Mathf.Clamp(y, 0, n - 1);
		int grid_x = (int)x;
		int grid_y = (int)y;
		//Debug.Log($"{grid_x}, {grid_y}, {x}, {y}");
		float y1 = Mathf.Lerp(arr[grid_y*n+grid_x], arr[grid_y*n+grid_x+1], x-grid_x);
		float y2 = Mathf.Lerp(arr[(grid_y+1)*n+grid_x], arr[(grid_y+1)*n+grid_x+1], x-grid_x);
		return Mathf.Lerp(y1, y2, y-grid_y);
	}

	public void ErodeMethod2(float[] height, float cell_len, int n, TerrainSetting s, int seed = 5731)
    {
		if (previous_n != n || previous_radius != s.brush_radius)
        {
			previous_n = n;
			previous_radius = s.brush_radius;
			InitializeErosionBrush(n, s.brush_radius);
        }
		seed = s.seed;

		Random.InitState(seed);
		float sediment_capacity = s.sediment_capacity;
		float sediment_suspension_rate = s.sediment_suspension_rate;
		float sediment_deposition_rate = s.sediment_deposition_rate;
		float evaporation_rate = s.evaporation_rate;
		float drop_volume = s.drop_volume;
		float inertia = s.inertia;
		float gravity = s.gravity;

		Vector2[] rain_map = new Vector2[s.iterations];
		for (int i = 0; i < s.iterations; i++)
			rain_map[i] = new Vector2(Random.Range(0f, (float)(n - 1)), Random.Range(0f, (float)(n - 1)));

		foreach (Vector2 pos in rain_map)
		{
			Vector2 drop_pos = pos;
			Vector2 direction = new Vector2(0f, 0f);
			float volume = s.drop_volume;
			float sediment = 0f;
			float speed = 1f;

			for (int t = 0; t < s.drop_life_time; t++)
            {
				int grid_position_x = (int)drop_pos.x;
				int grid_position_y = (int)drop_pos.y;
				int position = grid_position_y * n + grid_position_x;
				float cell_offset_x = drop_pos.x - grid_position_x;
				float cell_offset_y = drop_pos.y - grid_position_y;

				// Calculate the height and gradient of the drop;
				// grid_LU     *     grid_RU
				//        *      drop         *
				// grid_LD     *     grid_RD
				Vector2 gradient = new Vector2();
				float drop_height = 0f;
				(gradient, drop_height) = DropStateCalculate(height, n, drop_pos);

				// Calculate the moving direction;
				direction *= inertia;
				direction -= gradient * (1 - inertia);
				direction = direction.normalized;

				// Update the position of the drop
				Vector2 previous_pos = drop_pos;
				drop_pos += direction;
				if (drop_pos.x <= 0f || drop_pos.x >= n - 1 || drop_pos.y <= 0f || drop_pos.y >= n - 1)
					break;

				// Calculate the sediment change from the movement of the drop
				float new_drop_height = 0f;
				(gradient, new_drop_height) = DropStateCalculate(height, n, drop_pos);
				float height_change = new_drop_height - drop_height;
				float C = Mathf.Max(-height_change * speed * drop_volume * sediment_capacity, 0.01f);

				/*
				Debug.DrawLine(new Vector3(previous_pos.x * 0.005f, drop_height, previous_pos.y * 0.005f),
													new Vector3(drop_pos.x * 0.005f, new_drop_height, drop_pos.y * 0.005f),
													Color.white, 10f);
				*/

				if (C >= sediment && height_change < 0)
                {
					// The amount of erosion can not exceed the height change; otherwise, a hole will be generated
					float suspension_amount = Mathf.Min((C - sediment) * sediment_suspension_rate, -height_change);
					for (int effected_position = 0; effected_position < erosion_brush.GetLength(1); effected_position++)
					{
						Brush brush = erosion_brush[position, effected_position];
						if (brush.weight < 0)
							break;
						float weighted_change = suspension_amount * brush.weight;
						float sediment_change = Mathf.Min(height[brush.position], weighted_change); // Not exceed the height
						height[brush.position] -= sediment_change;
						sediment += sediment_change;
					}
				}
                else
                {
					// increase the height when moving uphill.
					float deposition_amount = (height_change > 0) ? Mathf.Min(height_change, sediment) : (sediment - C) * sediment_deposition_rate;
					sediment -= deposition_amount;

					// Add the sediment to the four nodes of the current cell using bilinear interpolation
					height[position] += deposition_amount * (1 - cell_offset_x) * (1 - cell_offset_y);
					height[position + 1] += deposition_amount * cell_offset_x * (1 - cell_offset_y);
					height[position + n] += deposition_amount * (1 - cell_offset_x) * cell_offset_y;
					height[position + n + 1] += deposition_amount * cell_offset_x * cell_offset_y;
				}

				speed = Mathf.Sqrt(speed * speed + height_change * gravity);
				drop_volume *= (1 - evaporation_rate);
			}
		}
    }

	private void InitializeErosionBrush(int n, int radius)
    {
		int brush_array_len = radius * radius * 4;
		erosion_brush = new Brush[n* n, brush_array_len];
		ComputeBuffer erosion_brush_buffer = new ComputeBuffer(n * n * brush_array_len, sizeof(float) + sizeof(int));
		erosion_brush_buffer.SetData(erosion_brush);

		erosion_compute.SetBuffer(0, "erosion_brush", erosion_brush_buffer);
		erosion_compute.SetInt("n", n);
		erosion_compute.SetInt("brush_array_len", brush_array_len);
		erosion_compute.SetInt("radius", radius);
		int thread_group_num = Mathf.CeilToInt((float)(n * n) / 64);
		erosion_compute.Dispatch(compute_kernels[0], thread_group_num, 1, 1);

		erosion_brush_buffer.GetData(erosion_brush);

		erosion_brush_buffer.Release();
	}
	
	private (Vector2, float) DropStateCalculate(float[] height, int n, Vector2 drop_pos)
    {
		int grid_position_x = (int)drop_pos.x;
		int grid_position_y = (int)drop_pos.y;
		int position = grid_position_y * n + grid_position_x;
		float cell_offset_x = drop_pos.x - grid_position_x;
		float cell_offset_y = drop_pos.y - grid_position_y;

		Vector4 height_map = new Vector4(
							height[position],
							height[position + 1],
							height[position + n],
							height[position + n + 1]);
		Vector2 gradient = new Vector2(
						(height_map.y - height_map.x) * (1 - cell_offset_y) + (height_map.w - height_map.z) * cell_offset_y,
						(height_map.z - height_map.x) * (1 - cell_offset_x) + (height_map.w - height_map.y) * cell_offset_x);
		float drop_height =	(height_map.x * (1 - cell_offset_x) + height_map.y * cell_offset_x) * (1 - cell_offset_y) +
												(height_map.z * (1 - cell_offset_x) + height_map.w * cell_offset_x) * cell_offset_y;

		return (gradient, drop_height);
	}

	public struct Brush
    {
		public int position;
		public float weight;
    }
}