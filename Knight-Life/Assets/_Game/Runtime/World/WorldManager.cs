using KnightLife.Runtime.Networking.Lobbies;
using Unity.Services.Core;
using UnityEngine;

public class WorldManager : MonoBehaviour
{
    public int Seed { get; private set; }

    [Header("Chunk Settings")]
    public int ChunkSize = 16;
    public int MaxGroundHeight = 16;
    public float ChunkTileSize = 1f;

    [Header("Noise Settings")]
    public float noiseScale = 20f;
    public int octaves = 4;
    public int seed = 42;

    [Header("Tiles")]
    public GameObject[] tilePrefabs; // 0=water, 1=sand, 2=grass, 3=forest, 4=mountain


    public static WorldManager Instance { get; private set; }

    public async void Awake()
    {
        // Set up the singleton instance
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning("Destroying World Manager");
            Destroy(gameObject);
            return;
        }
    }
}
