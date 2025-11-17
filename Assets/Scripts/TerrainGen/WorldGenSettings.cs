using UnityEngine;

[CreateAssetMenu(fileName = "WorldGenSettings", menuName = "Scriptable Objects/WorldGenSettings")]
public class WorldGenSettings : ScriptableObject {
    // ---------------------------------------------------------
    // Height settings
    // ---------------------------------------------------------
    [Header("Heights")]
    public float[] noiseScales = new float[] { 2f };
    public float[] heightAmplitudes = new float[] { 0.4f };

    // ---------------------------------------------------------
    // Texture settings
    // ---------------------------------------------------------
    [Header("Textures")]
    public int grassLayerIndex = 0;
    public int rockLayerIndex = 1;
    public float slopeThreshold = 15f;

    // ---------------------------------------------------------
    // Town settings
    // ---------------------------------------------------------
    [Header("Towns")]
    public int townCount = 4;
    public int buildingsPerTown = 20;
    public float townRadius = 25f;
    public GameObject[] townBuildings;

    // ---------------------------------------------------------
    // Prop settings
    // ---------------------------------------------------------
    [Header("Props")]
    public int propCount = 200;
    public float[] propNoiseScales = new float[] { 10f };
    public float[] propAmplitudes = new float[] { 1f };
    public GameObject[] props;

}
