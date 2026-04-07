using UnityEngine;

public class Chunk : MonoBehaviour
{
       
    void Start()
    {
        // TODO: Only generate the first time, otherwise load from saved world data
        Generate();
    }

    void Generate()
    {
        float offset = WorldManager.Instance.Seed * 0.1f;

        for (int x = 0; x < WorldManager.Instance.ChunkSize; x++)
            for (int z = 0; z < WorldManager.Instance.ChunkSize; z++)
            {
                float n = SampleNoise(x + offset, z + offset);
                int index = GetTileIndex(n);

                Vector3 pos = new Vector3(x * WorldManager.Instance.ChunkTileSize, 0f , z * WorldManager.Instance.ChunkTileSize);
               
                Instantiate(WorldManager.Instance.tilePrefabs[index], pos, Quaternion.Euler(90f, 0f, 0f), transform);
            }
    }

    float SampleNoise(float x, float y)
    {
        float value = 0f, amplitude = 1f, frequency = 1f, max = 0f;

        for (int i = 0; i < WorldManager.Instance.octaves; i++)
        {
            value += Mathf.PerlinNoise(x / WorldManager.Instance.noiseScale * frequency,
                                           y / WorldManager.Instance.noiseScale * frequency) * amplitude;
            max += amplitude;
            amplitude *= 0.5f;
            frequency *= 2f;
        }

        return value / max; // always 0..1
    }

    int GetTileIndex(float n)
    {
        if (n < 0.33f) return 0; 
        if (n < 0.66f) return 1;
        return 2;
    }
}