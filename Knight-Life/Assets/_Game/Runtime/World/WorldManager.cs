using KnightLife.Runtime.Networking.Lobbies;
using Unity.Services.Core;
using UnityEngine;

[DefaultExecutionOrder(-100)]
[ExecuteAlways]
public class WorldManager : MonoBehaviour
{
    public int Seed { get; private set; }

    [Header("Chunk Settings")]
    public int ChunkSize = 64;
    public int BuildHeight = 256;
    public float ChunkTileSize = 1f;

    [Header("Noise Settings")]
    public float noiseScale = 20f;
    public int octaves = 4;
    public int seed = 42;

    public static WorldManager Instance { get; private set; }

    void OnEnable()
    {
        if (Instance == null)
            Instance = this;
    }

    public async void Awake()
    {
        Debug.Log("Initializing World Manager");
        // Set up the singleton instance
        if (Instance == null)
        {
            Instance = this;
            if (Application.isPlaying) { }
                //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Debug.LogWarning("Destroying World Manager");
            if (Application.isPlaying) { }
                //Destroy(gameObject);
            return;
        }
    }
}
