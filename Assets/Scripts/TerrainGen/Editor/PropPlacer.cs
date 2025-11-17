using UnityEngine;
using UnityEditor;

public static class PropPlacer {
    const int mapResolution = 256;

    public static void PlaceProps(Terrain terrain, WorldGenSettings s) {
        if (s.props == null || s.props.Length == 0)
            return;

        GameObject natureRoot = GameObject.Find("Nature") ?? new GameObject("Nature");
        TerrainData d = terrain.terrainData;

        // 1. Generate & normalize density map
        float[,] densityMap = MapGenerator.GenerateCombined(mapResolution, mapResolution, s.propNoiseScales, s.propAmplitudes);

        // 2. Flatten density map to a 1D cumulative array for weighted random selection
        float[] cumulative = new float[mapResolution * mapResolution];
        float sum = 0f;
        for (int y = 0; y < mapResolution; y++) {
            for (int x = 0; x < mapResolution; x++) {
                sum += Mathf.Max(0f, densityMap[x, y]);
                cumulative[y * mapResolution + x] = sum;
            }
        }

        if (sum <= 0f)
            return; // nothing to spawn

        float cellWidth = d.size.x / mapResolution;
        float cellHeight = d.size.z / mapResolution;

        int spawned = 0;
        int maxTries = s.propCount * 10;
        int tries = 0;

        while (spawned < s.propCount && tries < maxTries) {
            tries++;

            // Weighted random selection
            float r = Random.value * sum;
            int index = System.Array.FindIndex(cumulative, v => v >= r);
            int cellX = index % mapResolution;
            int cellY = index / mapResolution;

            float px = (cellX + Random.value) * cellWidth;
            float pz = (cellY + Random.value) * cellHeight;
            float py = terrain.SampleHeight(new Vector3(px, 0f, pz));
            Vector3 pos = new Vector3(px, py, pz);

            GameObject prefab = s.props[Random.Range(0, s.props.Length)];
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.transform.position = pos;
            inst.transform.parent = natureRoot.transform;

            spawned++;
        }

        Debug.Log($"Placed {spawned} props after {tries} tries.");
    }

    /// <summary>
    /// Visualize the prop density map on the terrain heights
    /// </summary>
    public static void VisualizeTreeDensity(Terrain terrain, WorldGenSettings s) {
        if (terrain == null || s == null)
            return;

        int res = terrain.terrainData.heightmapResolution;
        float[,] densityMap = MapGenerator.GenerateCombined(res, res, s.propNoiseScales, s.propAmplitudes);
        //densityMap = MapGenerator.NormalizeMap(densityMap);

        terrain.terrainData.SetHeights(0, 0, densityMap);
    }

}
