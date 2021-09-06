using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Erosion : MonoBehaviour
{
	private float[] water_height;
	
	public void ErodeMethod1(float[] height, float cell_len, int n, TerrainSetting s){
		water_height = new float[n * n];
		Vector4[] flux = new Vector4[n * n];
		Vector2[] velocity = new Vector2[n * n];
		float[] flux_out = new float[n * n];
		float [] sediment = new float[n * n];
		cell_len = 4f;
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
			for (int z = 1; z < n - 1; z++){
				for (int x = 1; x < n - 1; x++){
					water_height[z*n+x] += RainFunction(x, z) * rain_rate * delta_time;
				}
			}
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
						Debug.Log("Zero flux detected.");
						float K = Mathf.Min(1f, water_height[z*n+x] * squared_cell_len / delta_time / flux_out[z*n+x]);
						flux[z*n+x] *= K;
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
									(flux[z*n+x+1].x + flux[z*n+x-1].y + flux[(z-1)*n+x].z + flux[(z+1)*n+x].w - flux_out[z*n+x]);
					//float water_height_avg = Mathf.Max(0f, water_height[z*n+x] + water_height_change * 0.5f);
					water_height[z*n+x] += water_height_change / squared_cell_len;
					
					// Calculate velocity field
					velocity[z*n+x].x = (flux[z*n+x-1].y - flux[z*n+x].x + flux[z*n+x].y - flux[z*n+x+1].x) *
										0.5f; // / water_height_avg / cell_len;
					velocity[z*n+x].y = (flux[(z-1)*n+x].z - flux[z*n+x].w + flux[z*n+x].z - flux[(z+1)*n+x].w) *
										0.5f; // / water_height_avg / cell_len;
										
					if (float.IsNaN(velocity[z*n+x].x)){
						Debug.Log($"{flux[z*n+x+1].x} + {flux[z*n+x-1].y} + {flux[(z-1)*n+x].z} + {flux[(z+1)*n+x].w} - {flux_out[z*n+x]}");
						//Debug.Log(water_height_avg);
						Debug.Log($"{i}, {x}, {z}");
						return;
					}
					
					// Calculate local tilt angle
					Vector3 tangent_x = new Vector3(2f * cell_len, height[z*n+x+1] - height[z*n+x-1], 0f);
					Vector3 tangent_z = new Vector3(0f, height[(z+1)*n+x] - height[(z-1)*n+x], cell_len * 2f);
					Vector3 normal = Vector3.Cross(tangent_x, tangent_z).normalized;
					float sin_alpha = normal.y < 0 ? -normal.y : normal.y;
					
					// Calculate sediment transport capacity
					float C = sediment_capacity * Mathf.Min(0.05f, sin_alpha) * velocity[z*n+x].magnitude;
					
					// Update height and set sediment value
					if (C > sediment[z*n+x]){
						float sediment_change = delta_time * sediment_suspension_rate * (C - sediment[z*n+x]);
						height[z*n+x] -= sediment_change;
						sediment[z*n+x] += sediment_change;
						water_height[z*n+x] += sediment_change;
					}
					else{
						float sediment_change = delta_time * sediment_deposition_rate * (sediment[z*n+x] - C);
						height[z*n+x] += sediment_change;
						sediment[z*n+x] -= sediment_change;
						water_height[z*n+x] -= sediment_change;
					}
					
					// Clamping all variables
					height[z*n+x] = Mathf.Max(0f, height[z*n+x]);
					water_height[z*n+x] = Mathf.Max(0f, water_height[z*n+x]);
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
	}
	
	private float RainFunction(int x, int z){
		return 10f;
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
}