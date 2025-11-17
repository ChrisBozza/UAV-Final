using UnityEngine;

public class VisualizeMap : MonoBehaviour {
    /// <summary>
    /// Apply a 2D density map to a terrain's heights for visualization.
    /// </summary>
    /// <param name="terrain">Terrain to apply the map to</param>
    /// <param name="densityMap">2D array of values 0–1 representing the map</param>
    /// <param name="heightScale">Maximum height in world units for scaling the map</param>
    public void ApplyDensityMap(Terrain terrain, float[,] densityMap, float heightScale) {
        if (terrain == null) {
            Debug.LogWarning("VisualizeMap: Terrain is null.");
            return;
        }

        if (heightScale <= 0f) {
            Debug.LogWarning("VisualizeMap: HeightScale must be greater than 0.");
            return;
        }

        TerrainData data = terrain.terrainData;

        int mapW = densityMap.GetLength(0);
        int mapH = densityMap.GetLength(1);
        int terrainRes = data.heightmapResolution;

        float[,] heights = new float[terrainRes, terrainRes];

        for (int y = 0; y < terrainRes; y++) {
            for (int x = 0; x < terrainRes; x++) {
                int mx = Mathf.Clamp(x * mapW / terrainRes, 0, mapW - 1);
                int my = Mathf.Clamp(y * mapH / terrainRes, 0, mapH - 1);

                heights[y, x] = Mathf.Clamp01(densityMap[mx, my]) * heightScale / data.size.y;
            }
        }

        data.SetHeights(0, 0, heights);

        Debug.Log(densityMap);
    }
}
