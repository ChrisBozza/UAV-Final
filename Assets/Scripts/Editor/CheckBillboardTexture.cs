using UnityEngine;
using UnityEditor;

public class CheckBillboardTexture : EditorWindow
{
    [MenuItem("Tools/View Billboard Texture")]
    public static void CheckTexture()
    {
        Texture2D tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Materials/Billboards/Tree_Summer_Billboard.png");
        
        if (tex == null)
        {
            EditorUtility.DisplayDialog("Error", "Could not load texture!", "OK");
            return;
        }

        TextureImporter importer = AssetImporter.GetAtPath("Assets/Materials/Billboards/Tree_Summer_Billboard.png") as TextureImporter;
        
        bool wasReadable = importer.isReadable;
        if (!importer.isReadable)
        {
            importer.isReadable = true;
            importer.SaveAndReimport();
            tex = AssetDatabase.LoadAssetAtPath<Texture2D>("Assets/Materials/Billboards/Tree_Summer_Billboard.png");
        }

        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("=== TEXTURE PIXEL ANALYSIS ===");
        sb.AppendLine($"Size: {tex.width}x{tex.height}");
        sb.AppendLine($"Format: {tex.format}");
        
        Color[] pixels = tex.GetPixels();
        
        int opaqueCount = 0;
        int semiTransparentCount = 0;
        int transparentCount = 0;
        float avgR = 0, avgG = 0, avgB = 0, avgA = 0;

        foreach (Color pixel in pixels)
        {
            if (pixel.a > 0.9f) opaqueCount++;
            else if (pixel.a > 0.1f) semiTransparentCount++;
            else transparentCount++;

            avgR += pixel.r;
            avgG += pixel.g;
            avgB += pixel.b;
            avgA += pixel.a;
        }

        int totalPixels = pixels.Length;
        avgR /= totalPixels;
        avgG /= totalPixels;
        avgB /= totalPixels;
        avgA /= totalPixels;

        sb.AppendLine($"\nPixel Analysis:");
        sb.AppendLine($"  Total Pixels: {totalPixels}");
        sb.AppendLine($"  Opaque (>90% alpha): {opaqueCount} ({(opaqueCount * 100f / totalPixels):F1}%)");
        sb.AppendLine($"  Semi-transparent: {semiTransparentCount} ({(semiTransparentCount * 100f / totalPixels):F1}%)");
        sb.AppendLine($"  Transparent (<10% alpha): {transparentCount} ({(transparentCount * 100f / totalPixels):F1}%)");
        sb.AppendLine($"\nAverage Color:");
        sb.AppendLine($"  R: {avgR:F3}");
        sb.AppendLine($"  G: {avgG:F3}");
        sb.AppendLine($"  B: {avgB:F3}");
        sb.AppendLine($"  A: {avgA:F3}");
        
        sb.AppendLine($"\nSample Pixels (first 10 non-transparent):");
        int samples = 0;
        for (int i = 0; i < pixels.Length && samples < 10; i++)
        {
            if (pixels[i].a > 0.1f)
            {
                sb.AppendLine($"  Pixel {i}: RGBA({pixels[i].r:F2}, {pixels[i].g:F2}, {pixels[i].b:F2}, {pixels[i].a:F2})");
                samples++;
            }
        }

        if (!wasReadable)
        {
            importer.isReadable = false;
            importer.SaveAndReimport();
        }

        string output = sb.ToString();
        Debug.Log(output);
        GUIUtility.systemCopyBuffer = output;

        EditorUtility.DisplayDialog("Texture Analysis", 
            "Analysis complete! Data copied to clipboard.\n\n" +
            $"Opaque pixels: {(opaqueCount * 100f / totalPixels):F1}%\n" +
            $"Transparent: {(transparentCount * 100f / totalPixels):F1}%", 
            "OK");
    }
}
