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

    public Material chunkMaterial;

    public static WorldManager Instance { get; private set; }

    void OnEnable()
    {
        GenerateChunkGrid();

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

    private void GenerateChunkGrid()
    {
        ClearChunks();

        int gridRadius = 1; // 1 = 3x3 grid (radius of 1 from center)
        float chunkWorldSize = ChunkSize * ChunkTileSize;

        for (int x = -gridRadius; x <= gridRadius; x++)
        {
            for (int z = -gridRadius; z <= gridRadius; z++)
            {
                var position = new Vector3(x * chunkWorldSize, 0, z * chunkWorldSize);
                SpawnChunk(position);
            }
        }
    }

    private void SpawnChunk(Vector3 position)
    {
        var chunkObject = new GameObject($"Chunk_{position.x}_{position.z}");
        chunkObject.transform.position = position;
        chunkObject.transform.parent = transform; // Parent to WorldManager for organization
        chunkObject.AddComponent<Chunk2>(); // Chunk2.Start() will call InitializeChunk()

        var meshRenderer = chunkObject.GetComponent<MeshRenderer>();
        meshRenderer.material = chunkMaterial;
    }

    private void ClearChunks()
    {
        // Iterate backwards when destroying children
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            var child = transform.GetChild(i);
            if (Application.isPlaying)
                Destroy(child.gameObject);
            else
                DestroyImmediate(child.gameObject); // Required in edit mode
        }
    }
}
