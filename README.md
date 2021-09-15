# TerrainGenerator

## Introduction
We generate the terrain with Perlin noise. Then we simulate the erosion on the terrain.

## Implementation
* Method 1 <br>
This method first update the height of water. Then it update the height of the terrain by the height of water in each time frame. <br>
Here are some issues I find when implementing.
1. The update function for the height of water is not reliable. I observe that the height of water tend to be zero around the points which have values, and the values change from zero to some value periodically.
For example, <br>

         0.4  0  0.6      0  0.6  0
          0  0.5  0      0.7  0  0.8
         0.3  0  0.8      0  0.7  0
         
            t = n          t = n+1

![resolution50_iteration4000_waterHeight](https://user-images.githubusercontent.com/37297994/133361394-a37860fe-b6e0-4e1c-a4c2-f182fa9bc206.png) <br>
From the picture above, we can see there are zeros staggered in the grids. And this is totally not the real water act.

2. Due to the periodic patern above, most of the time, the height of the terrain remains the same after thousands of iterations.
3. When the resolution is high, this patern of water would result to some spikes.

After all, here are two picture of low resolution. The first one is the original state, and the second one is the terrain after 4000 iterations. <br>
![resolution50_iteration0](https://user-images.githubusercontent.com/37297994/133362215-d050ee99-3207-41e7-8254-a83d21c78612.png)
![resolution50_iteration4000](https://user-images.githubusercontent.com/37297994/133362218-a8f8fcdc-3bfe-4e69-9c89-9e8047e1519e.png)

* Method 2 <br>
This method update the height of terrain by simulating a single droplet at one time. This allows us to parallelly calculate each droplet on GPU (each droplet is independent to others). <br>
The first one is the original state, and the second one is the terrain after 100000 iterations. <br>
![CPU_iteration0](https://user-images.githubusercontent.com/37297994/133362671-acfaaebf-6fd3-4a7a-8e22-bafc7e59685a.png)
![CPU_iteration100000](https://user-images.githubusercontent.com/37297994/133362696-8df0e8c3-b86f-46cd-ac8f-53c307ef51b9.png)

## Reference
* Method 1 <br> http://www-evasion.imag.fr/Publications/2007/MDH07/FastErosion_PG07.pdf <br> https://old.cescg.org/CESCG-2011/papers/TUBudapest-Jako-Balazs.pdf
* Method 2 <br> https://www.firespark.de/resources/downloads/implementation%20of%20a%20methode%20for%20hydraulic%20erosion.pdf <br> https://github.com/SebLague/Hydraulic-Erosion
