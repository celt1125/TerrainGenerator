# TerrainGenerator

## Introduction
We generate the terrain with Perlin noise. Then we simulate the erosion on the terrain.

## Implementation
* Method1 <br>
This method first update the height of water. Then it update the height of the terrain by the height of water in each time frame. <br>
Here are some issues I find when implementing.
1. The update function for the height of water is not reliable. I observe that the height of water tend to be zero around the points which have values, and the values change from zero to some value periodically.
For example, <br>
At t = n, 
$$\begin{array}
0.5 & 0 & 0.6 \\
0 & 0.4 & 0 \\
0.7 & 0 & 0.5
\end{array}$

2. 

## Reference
* Method1 <br> http://www-evasion.imag.fr/Publications/2007/MDH07/FastErosion_PG07.pdf <br> https://old.cescg.org/CESCG-2011/papers/TUBudapest-Jako-Balazs.pdf
* Method2 <br> https://www.firespark.de/resources/downloads/implementation%20of%20a%20methode%20for%20hydraulic%20erosion.pdf <br> https://github.com/SebLague/Hydraulic-Erosion
