using UnityEngine;
using UnityEditor;

public static class WorldGenerator {
    // ---------------------------------------------------------
    // Heights & Textures
    // ---------------------------------------------------------
    public static void ApplyHeights(Terrain terrain, WorldGenSettings s) {
        int res = terrain.terrainData.heightmapResolution;

        float[,] heights = MapGenerator.GenerateCombined(res, res, s.noiseScales, s.heightAmplitudes);

        terrain.terrainData.SetHeights(0, 0, heights);
    }

    public static void PaintTextures(Terrain terrain, WorldGenSettings s) {
        var data = terrain.terrainData;
        int w = data.alphamapWidth;
        int h = data.alphamapHeight;

        float[,,] map = new float[w, h, data.terrainLayers.Length];

        for (int x = 0; x < w; x++) {
            for (int y = 0; y < h; y++) {
                float normX = (float)x / w;
                float normY = (float)y / h;
                float slope = data.GetSteepness(normX, normY);

                if (slope < s.slopeThreshold)
                    map[y, x, s.grassLayerIndex] = 1f;
                else
                    map[y, x, s.rockLayerIndex] = 1f;
            }
        }

        data.SetAlphamaps(0, 0, map);
    }

    // ---------------------------------------------------------
    // Towns
    // ---------------------------------------------------------
    public static void PlaceTowns(Terrain terrain, WorldGenSettings s) {
        if (s.townBuildings == null || s.townBuildings.Length == 0)
            return;

        GameObject townsRoot = GameObject.Find("Towns") ?? new GameObject("Towns");

        for (int i = 0; i < s.townCount; i++) {
            CreateTown(terrain, s, townsRoot.transform, i + 1);
        }
    }

    static void CreateTown(Terrain terrain, WorldGenSettings s, Transform parent, int townIndex) {
        TerrainData d = terrain.terrainData;

        float x = Random.Range(50f, d.size.x - 50f);
        float z = Random.Range(50f, d.size.z - 50f);
        float y = terrain.SampleHeight(new Vector3(x, 0f, z));

        Vector3 center = new Vector3(x, y, z);

        GameObject townRoot = new GameObject($"Town {townIndex}");
        townRoot.transform.parent = parent;

        for (int i = 0; i < s.buildingsPerTown; i++) {
            Vector3 offset = Random.insideUnitSphere * s.townRadius;
            offset.y = 0;
            Vector3 pos = center + offset;
            pos.y = terrain.SampleHeight(pos);

            GameObject prefab = s.townBuildings[Random.Range(0, s.townBuildings.Length)];
            GameObject inst = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            inst.transform.position = pos;
            inst.transform.parent = townRoot.transform;
        }
    }

    // ---------------------------------------------------------
    // Full World Generation
    // ---------------------------------------------------------
    public static void GenerateWorld(Terrain terrain, WorldGenSettings settings) {
        ApplyHeights(terrain, settings);
        PaintTextures(terrain, settings);
        PlaceTowns(terrain, settings);
        PropPlacer.PlaceProps(terrain, settings);
    }

    public static void ClearPreviousGeneration() {
        // Delete all Towns
        GameObject townsRoot = GameObject.Find("Towns");
        if (townsRoot != null) {
            Object.DestroyImmediate(townsRoot);
        }

        // Delete all Nature props
        GameObject natureRoot = GameObject.Find("Nature");
        if (natureRoot != null) {
            Object.DestroyImmediate(natureRoot);
        }
    }

}
