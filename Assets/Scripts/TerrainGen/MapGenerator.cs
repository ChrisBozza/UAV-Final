using UnityEngine;

/// <summary>
/// Simple 2D map generator for heightmaps or density maps.
/// Only uses noiseScale and amplitude.
/// </summary>
public static class MapGenerator {
    // Single-layer noise map
    public static float[,] Generate(int width, int height, float noiseScale, float amplitude) {
        amplitude = amplitude / 10;
        float[,] map = new float[width, height];
        float offsetX = Random.Range(0f, 10000f);
        float offsetY = Random.Range(0f, 10000f);

        if (noiseScale <= 0f) noiseScale = 0.0001f;

        for (int y = 0; y < height; y++) {
            for (int x = 0; x < width; x++) {
                float nx = (x + offsetX) / width * noiseScale;
                float ny = (y + offsetY) / height * noiseScale;
                map[x, y] = Mathf.Clamp01(Mathf.PerlinNoise(nx, ny) * amplitude);
            }
        }

        return map;
    }

    // Multi-layer combined map
    public static float[,] GenerateCombined(int width, int height, float[] scales, float[] amplitudes) {
        if (scales.Length == 0 || amplitudes.Length == 0)
            return Generate(width, height, 1f, 1f);

        int layers = Mathf.Min(scales.Length, amplitudes.Length);
        float[,] combinedMap = new float[width, height];

        // Call Generate for each layer
        for (int i = 0; i < layers; i++) {
            float[,] layer = Generate(width, height, scales[i], amplitudes[i]);
            for (int y = 0; y < height; y++) {
                for (int x = 0; x < width; x++) {
                    combinedMap[x, y] += layer[x, y];
                }
            }
        }

        return combinedMap;
    }

    public static float[,] RedistributeDensity(float[,] map, float bottomPercent, float topPercent, float bottomReduction = 0f) {
        int w = map.GetLength(0);
        int h = map.GetLength(1);

        int totalCells = w * h;
        int bottomCount = Mathf.FloorToInt(totalCells * Mathf.Clamp01(bottomPercent));
        int topCount = Mathf.FloorToInt(totalCells * Mathf.Clamp01(topPercent));

        if (bottomCount <= 0 || topCount <= 0)
            return map;

        // Flatten values
        var list = new System.Collections.Generic.List<(int x, int y, float v)>(totalCells);
        for (int y = 0; y < h; y++)
            for (int x = 0; x < w; x++)
                list.Add((x, y, Mathf.Max(0f, map[x, y])));

        // Sort ascending
        list.Sort((a, b) => a.v.CompareTo(b.v));

        float removedDensity = 0f;

        // 1. Reduce bottom cells
        bottomReduction = Mathf.Clamp01(bottomReduction);

        for (int i = 0; i < bottomCount; i++) {
            var cell = list[i];
            float removed = cell.v * (1f - bottomReduction);
            removedDensity += removed;
            map[cell.x, cell.y] *= bottomReduction;
        }

        // 2. Compute total of top cells
        float topTotal = 0f;
        for (int i = totalCells - topCount; i < totalCells; i++)
            topTotal += list[i].v;

        if (topTotal <= 0f)
            return map;

        // 3. Redistribute removed density to top cells proportionally
        for (int i = totalCells - topCount; i < totalCells; i++) {
            var cell = list[i];
            float pct = cell.v / topTotal;
            float extra = removedDensity * pct;
            map[cell.x, cell.y] += extra;
        }

        return map;
    }

    public static float[,] NormalizeMap(float[,] map, float targetMax = 1f) {
        int w = map.GetLength(0);
        int h = map.GetLength(1);

        float min = float.MaxValue;
        float max = float.MinValue;

        // Find min and max
        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                float v = map[x, y];
                if (v < min) min = v;
                if (v > max) max = v;
            }
        }

        float range = max - min;
        if (range <= 0f) range = 1f; // avoid division by zero

        float[,] normalized = new float[w, h];

        for (int y = 0; y < h; y++) {
            for (int x = 0; x < w; x++) {
                normalized[x, y] = ((map[x, y] - min) / range) * targetMax;
            }
        }

        return normalized;
    }


}

