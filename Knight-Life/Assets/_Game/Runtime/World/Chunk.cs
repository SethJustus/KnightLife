using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{

    // Block types — extend as needed
    public enum Block { Air, Water, Sand, Grass, Dirt, Stone }

    // --- private mesh building state ---
    private Block[,,] blocks;
    private List<Vector3> Verts = new();
    private List<int> tris = new();
    private List<Vector2> uvs = new();
    private List<Color> colors = new();

    void Start() => Generate();

    // -------------------------------------------------------
    // 1. Fill the block data array from noise
    // -------------------------------------------------------
    void Generate()
    {
        blocks = new Block[WorldManager.Instance.ChunkSize, WorldManager.Instance.BuildHeight, WorldManager.Instance.ChunkSize];
        float offset = WorldManager.Instance.Seed * 0.1f;

        for (int x = 0; x < WorldManager.Instance.ChunkSize; x++)
            for (int z = 0; z < WorldManager.Instance.ChunkSize; z++)
            {
                float n = SampleNoise(x + offset, z + offset);
                int surfaceY = Mathf.RoundToInt(n * (WorldManager.Instance.BuildHeight - 1));

                for (int y = 0; y < WorldManager.Instance.BuildHeight; y++)
                    blocks[x, y, z] = GetBlock(y, surfaceY);
            }

        BuildMesh();
    }

    Color GetBlockColor(Block block) => block switch
    {
        Block.Water => new Color(0.2f, 0.4f, 0.9f),
        Block.Sand => new Color(0.9f, 0.85f, 0.5f),
        Block.Grass => new Color(0.3f, 0.7f, 0.2f),
        Block.Dirt => new Color(0.5f, 0.35f, 0.15f),
        Block.Stone => new Color(0.5f, 0.5f, 0.5f),
        _ => Color.white
    };

    Block GetBlock(int y, int surfaceY)
    {
        if (y > surfaceY) return Block.Air;
        if (surfaceY < 2) return Block.Water;
        if (y == surfaceY)
        {
            if (surfaceY < 3) return Block.Sand;
            return Block.Grass;
        }
        if (y >= surfaceY - 2) return Block.Dirt;
        return Block.Stone;
    }

    // -------------------------------------------------------
    // 2. Walk every block — only add visible faces
    // -------------------------------------------------------
    void BuildMesh()
    {
        Verts.Clear(); tris.Clear(); uvs.Clear(); colors.Clear();

        for (int x = 0; x < WorldManager.Instance.ChunkSize; x++)
            for (int y = 0; y < WorldManager.Instance.BuildHeight; y++)
                for (int z = 0; z < WorldManager.Instance.ChunkSize; z++)
                {
                    Block block = blocks[x, y, z]; // read it once here
                    if (block == Block.Air) continue;

                    Vector3 pos = new(x, y, z);

                    if (IsAir(x, y + 1, z)) AddFace(pos, Direction.Top, block);
                    if (IsAir(x, y - 1, z)) AddFace(pos, Direction.Bottom, block);
                    if (IsAir(x + 1, y, z)) AddFace(pos, Direction.Right, block);
                    if (IsAir(x - 1, y, z)) AddFace(pos, Direction.Left, block);
                    if (IsAir(x, y, z + 1)) AddFace(pos, Direction.Front, block);
                    if (IsAir(x, y, z - 1)) AddFace(pos, Direction.Back, block);
                }

        ApplyMesh();
    }

    // -------------------------------------------------------
    // 3. Add a single quad face in the given direction
    // -------------------------------------------------------
    enum Direction { Top, Bottom, Right, Left, Front, Back }

    void AddFace(Vector3 pos, Direction dir, Block block)
    {
        int vi = Verts.Count; // vertex index before adding
        
        switch (dir)
        {
            case Direction.Top:
                Verts.Add(pos + new Vector3(0, 1, 0));
                Verts.Add(pos + new Vector3(0, 1, 1));
                Verts.Add(pos + new Vector3(1, 1, 1));
                Verts.Add(pos + new Vector3(1, 1, 0));
                break;
            case Direction.Bottom:
                Verts.Add(pos + new Vector3(0, 0, 0));
                Verts.Add(pos + new Vector3(1, 0, 0));
                Verts.Add(pos + new Vector3(1, 0, 1));
                Verts.Add(pos + new Vector3(0, 0, 1));
                break;
            case Direction.Right:
                Verts.Add(pos + new Vector3(1, 0, 0));
                Verts.Add(pos + new Vector3(1, 1, 0));
                Verts.Add(pos + new Vector3(1, 1, 1));
                Verts.Add(pos + new Vector3(1, 0, 1));
                break;
            case Direction.Left:
                Verts.Add(pos + new Vector3(0, 0, 0));
                Verts.Add(pos + new Vector3(0, 0, 1));
                Verts.Add(pos + new Vector3(0, 1, 1));
                Verts.Add(pos + new Vector3(0, 1, 0));
                break;
            case Direction.Front:
                Verts.Add(pos + new Vector3(0, 0, 1));
                Verts.Add(pos + new Vector3(1, 0, 1));
                Verts.Add(pos + new Vector3(1, 1, 1));
                Verts.Add(pos + new Vector3(0, 1, 1));
                break;
            case Direction.Back:
                Verts.Add(pos + new Vector3(0, 0, 0));
                Verts.Add(pos + new Vector3(0, 1, 0));
                Verts.Add(pos + new Vector3(1, 1, 0));
                Verts.Add(pos + new Vector3(1, 0, 0));
                break;
        }

        Color c = GetBlockColor(block);
        colors.Add(c); colors.Add(c); colors.Add(c); colors.Add(c);

        // Two triangles per face (quad = 2 tris)
        tris.AddRange(new[] { vi, vi + 1, vi + 2, vi, vi + 2, vi + 3 });

        // Simple UVs — replace with a texture atlas later
        uvs.AddRange(new[] {
            new Vector2(0,0), new Vector2(0,1),
            new Vector2(1,1), new Vector2(1,0)
        });
    }

    // -------------------------------------------------------
    // 4. Push lists into the actual Unity Mesh
    // -------------------------------------------------------
    void ApplyMesh()
    {
        var mesh = new Mesh { name = "Chunk" };
        mesh.SetVertices(Verts);
        mesh.SetTriangles(tris, 0);
        mesh.SetUVs(0, uvs);
        mesh.SetColors(colors);
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    // -------------------------------------------------------
    // Helpers
    // -------------------------------------------------------
    bool IsAir(int x, int y, int z)
    {
        // Out of chunk bounds counts as Air (exposed edge face)
        if (x < 0 || x >= WorldManager.Instance.ChunkSize) return true;
        if (y < 0 || y >= WorldManager.Instance.BuildHeight) return true;
        if (z < 0 || z >= WorldManager.Instance.ChunkSize) return true;
        return blocks[x, y, z] == Block.Air;
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
        return value / max;
    }
}