using Mono.Cecil;
using NUnit.Framework;
using NUnit.Framework.Internal.Filters;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.Mesh;

public enum VoxelType
{
    /// <summary>
    /// Air is nothing
    /// </summary>
    Air,
    Grass
}

public static class Direction
{
    public static readonly Vector3 Forward = Vector3.forward;
    public static readonly Vector3 Back = Vector3.back;
    public static readonly Vector3 Left = Vector3.left;
    public static readonly Vector3 Right = Vector3.right;
    public static readonly Vector3 Up = Vector3.up;
    public static readonly Vector3 Down = Vector3.down;

    public static readonly Vector3[] All =
    {
        Forward, Back, Left, Right, Up, Down
    };
}

public struct Voxel
{
    /// <summary>
    /// The x (Left and Right) index of the voxel within the chunk
    /// </summary>
    public int X { get; set; }

    /// <summary>
    /// The y (Up and Down) index of the voxel within the chunk
    /// </summary>
    public int Y { get; set; }

    /// <summary>
    /// The z (Forwards and Back) index of the voxel within the chunk
    /// </summary>
    public int Z { get; set; }

    /// <summary>
    /// The type of voxel to be rendered
    /// </summary>
    public VoxelType VoxelType { get; set; }
}

public class ChunkMeshData
{
    public List<Vector3> Vertices { get; set; } = new();

    public List<int> TriangleVertexIndices { get; set; } = new();

    public List<Vector2> Uvs { get; set; } = new();

    public List<Color> VertexColors { get; set; } = new();
}

//[DefaultExecutionOrder(0)]
//[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
public class Chunk2 : MonoBehaviour
{
    const string MESH_NAME = "Chunk_Mesh";

    Voxel[,,] Voxels;

    ChunkMeshData MeshData;

    private static readonly Dictionary<Vector3, Vector3[]> _faceVertices = new()
        {
            // -Z
            { Direction.Back, new[]
                { new Vector3(1,0,0), new Vector3(0,0,0), new Vector3(0,1,0), new Vector3(1,1,0) } },

            // +Z
            { Direction.Forward, new[]
                { new Vector3(0,0,1), new Vector3(1,0,1), new Vector3(1,1,1), new Vector3(0,1,1) } },

            // -X
            { Direction.Left, new[]
                { new Vector3(0,0,0), new Vector3(0,0,1), new Vector3(0,1,1), new Vector3(0,1,0) } },

            // +X
            { Direction.Right, new[]
                { new Vector3(1,0,1), new Vector3(1,0,0), new Vector3(1,1,0), new Vector3(1,1,1) } },

            // +Y
            { Direction.Up, new[]
                { new Vector3(0,1,0), new Vector3(0,1,1), new Vector3(1,1,1), new Vector3(1,1,0) } },

            // -Y
            { Direction.Down, new[]
                { new Vector3(0,0,1), new Vector3(0,0,0), new Vector3(1,0,0), new Vector3(1,0,1) } },
        };

    void OnEnable()
    {
        // Small delay to ensure WorldManager.OnEnable has run first
#if UNITY_EDITOR
        UnityEditor.EditorApplication.delayCall += () =>
        {
            if (this != null) // Guard against destroyed object
                InitializeChunk();
        };
#endif
    }

    #region Unity Methods
    public void Start()
    {
        UnityEngine.Debug.Log("Starting Chunk");
        InitializeChunk();
    }
    #endregion

    #region Public Methods
    public void InitializeChunk()
    {
        UnityEngine.Debug.Log("Initializing Chunk");
        var stopwatch = Stopwatch.StartNew();
        // Initialize the Voxels array
        this.Voxels = new Voxel[WorldManager.Instance.ChunkSize, WorldManager.Instance.BuildHeight, WorldManager.Instance.ChunkSize];

        // If there is voxel data to read, read it. Otherwise, generate new voxel data.

        // For now, we'll just generate new voxel data every time.
        GenerateVoxelData();
        GenerateMeshDataFromVoxelData();
        GenerateMeshFromMeshData();
        var time = stopwatch.ElapsedMilliseconds.ToString();
        UnityEngine.Debug.Log("Generate Time (ms): " + time);
    }
    #endregion

    #region Private Methods
    private void GenerateVoxelData()
    {


        // If voxel data exists, read it

        // Otherwise create new

        float offset = WorldManager.Instance.Seed * 0.1f;
        for (var x = 0; x < WorldManager.Instance.ChunkSize; x++)
        {
            for (var z = 0; z < WorldManager.Instance.ChunkSize; z++)
            {
                //var noiseValue = SampleNoise(x + transform.position.x + offset, z + transform.position.z + offset);
                var noiseValue = SampleNoise(
                    x * WorldManager.Instance.ChunkTileSize + transform.position.x + offset,
                    z * WorldManager.Instance.ChunkTileSize + transform.position.z + offset
                );

                UnityEngine.Debug.Log(noiseValue);
                for (var y = 0; y < WorldManager.Instance.BuildHeight; y++)
                {
                    var yPercent = y / (float)WorldManager.Instance.BuildHeight;
                    if (yPercent < noiseValue)
                    {
                        Voxels[x, y, z].VoxelType = VoxelType.Grass;
                    }
                    else
                    {
                        Voxels[x, y, z].VoxelType = VoxelType.Air;
                    }

                    Voxels[x, y, z].X = x;
                    Voxels[x, y, z].Y = y;
                    Voxels[x, y, z].Z = z;
                }
            }
        }
    }

    private void GenerateMeshDataFromVoxelData()
    {
        var chunkMeshData = new ChunkMeshData();

        // Process each of the 6 face directions
        foreach (var direction in Direction.All)
        {
            GreedyMeshForDirection(direction, chunkMeshData);
        }

        this.MeshData = chunkMeshData;
    }

    //private void GenerateMeshDataFromVoxelData()
    //{
    //    var chunkMeshData = new ChunkMeshData();

    //    for (var x = 0; x < WorldManager.Instance.ChunkSize; x++)
    //    {
    //        for (var z = 0; z < WorldManager.Instance.ChunkSize; z++)
    //        {
    //            for (var y = 0; y < WorldManager.Instance.BuildHeight; y++)
    //            {
    //                var voxel = this.Voxels[x, y, z];
    //                if (voxel.VoxelType == VoxelType.Air)
    //                {
    //                    continue;
    //                }

    //                foreach (var direction in Direction.All)
    //                {
    //                    // Get the voxel in this direction
    //                    var coordinates = new Vector3(x, y, z);
    //                    var dCoordinates = coordinates + direction;

    //                    // Render faces at chunk edges
    //                    if (CoordinatesAreWithinBounds(dCoordinates))
    //                    { 
    //                        var voxelInDirection = this.Voxels[(int)dCoordinates.x, (int)dCoordinates.y, (int)dCoordinates.z];
    //                        if (voxelInDirection.VoxelType != VoxelType.Air)
    //                        {
    //                            continue;
    //                        }
    //                    }

    //                    // Get the 4 vertices for this face and offset by the voxel's world position
    //                    var vertices = _faceVertices[direction]
    //                        .Select(v => v + coordinates)
    //                        .ToArray();


    //                    // It is important to get the index BEFORE adding verticies
    //                    var vertexIndex = chunkMeshData.Vertices.Count;
    //                    var triangleVertexIndices = new[] { vertexIndex, vertexIndex + 1, vertexIndex + 2, vertexIndex, vertexIndex + 2, vertexIndex + 3 };
    //                    var uvs = new[] { new Vector2(0,0), new Vector2(0,1), new Vector2(1,1), new Vector2(1,0) };
    //                    var color = GetColor(voxel.VoxelType);
    //                    chunkMeshData.Vertices.AddRange(vertices);
    //                    chunkMeshData.TriangleVertexIndices.AddRange(triangleVertexIndices);
    //                    chunkMeshData.Uvs.AddRange(uvs);
    //                    for (var i = 0; i<4;i++)
    //                    {
    //                        chunkMeshData.VertexColors.Add(color);
    //                    }                        
    //                }                   
    //            }
    //        }
    //    }

    //    this.MeshData = chunkMeshData;
    //}

    private Color GetColor(VoxelType voxelType)
    {
        if (voxelType == VoxelType.Grass)
        {
            return new Color(0.3f, 0.7f, 0.2f);
        }

        return new Color(0.2f, 0.2f, 0.2f);
    }

    private void GenerateMeshFromMeshData()
    {
        var mesh = new Mesh { name = MESH_NAME };
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        mesh.hideFlags = HideFlags.DontSave;
        mesh.SetVertices(MeshData.Vertices);
        //for (var submesh = 0; submesh < )
        // TODO: Set up a submesh for each voxel type
        mesh.SetTriangles(MeshData.TriangleVertexIndices, 0);
        mesh.SetUVs(0, MeshData.Uvs);
        mesh.SetColors(MeshData.VertexColors);
        mesh.RecalculateNormals();

        GetComponent<MeshFilter>().mesh = mesh;
        GetComponent<MeshCollider>().sharedMesh = mesh;
    }

    private bool CoordinatesAreWithinBounds(Vector3 coordinates)
    {
        var x = coordinates.x;
        var y = coordinates.y;
        var z = coordinates.z;

        if (x < 0 || x >= WorldManager.Instance.ChunkSize)
        {
            return false;
        }

        if (y < 0 || y >= WorldManager.Instance.BuildHeight)
        {
            return false;
        }

        if (z < 0 || z >= WorldManager.Instance.ChunkSize)
        {
            return false;
        }

        return true;       
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

    private void GreedyMeshForDirection(Vector3 direction, ChunkMeshData chunkMeshData)
    {
        int normalAxis, uAxis, vAxis;

        if (direction == Direction.Left || direction == Direction.Right)
        { normalAxis = 0; uAxis = 2; vAxis = 1; }
        else if (direction == Direction.Up || direction == Direction.Down)
        { normalAxis = 1; uAxis = 0; vAxis = 2; }
        else
        { normalAxis = 2; uAxis = 0; vAxis = 1; }

        // Whether the face sits at n+1 (positive side) or n (negative side)
        // and whether to flip winding order
        bool isPositiveFace = direction[normalAxis] > 0;

        int[] size = {
        WorldManager.Instance.ChunkSize,
        WorldManager.Instance.BuildHeight,
        WorldManager.Instance.ChunkSize
    };

        int normalSize = size[normalAxis];
        int uSize = size[uAxis];
        int vSize = size[vAxis];

        for (int n = 0; n < normalSize; n++)
        {
            VoxelType?[,] mask = new VoxelType?[uSize, vSize];

            for (int u = 0; u < uSize; u++)
            {
                for (int v = 0; v < vSize; v++)
                {
                    int[] coord = new int[3];
                    coord[normalAxis] = n;
                    coord[uAxis] = u;
                    coord[vAxis] = v;

                    var voxel = Voxels[coord[0], coord[1], coord[2]];
                    if (voxel.VoxelType == VoxelType.Air) { mask[u, v] = null; continue; }

                    int[] neighborCoord = (int[])coord.Clone();
                    neighborCoord[normalAxis] += (int)direction[normalAxis];

                    var neighborPos = new Vector3(neighborCoord[0], neighborCoord[1], neighborCoord[2]);
                    if (CoordinatesAreWithinBounds(neighborPos))
                    {
                        var neighbor = Voxels[neighborCoord[0], neighborCoord[1], neighborCoord[2]];
                        mask[u, v] = neighbor.VoxelType == VoxelType.Air ? voxel.VoxelType : null;
                    }
                    else
                    {
                        mask[u, v] = voxel.VoxelType;
                    }
                }
            }

            bool[,] merged = new bool[uSize, vSize];

            for (int u = 0; u < uSize; u++)
            {
                for (int v = 0; v < vSize; v++)
                {
                    if (mask[u, v] == null || merged[u, v]) continue;

                    VoxelType faceType = mask[u, v].Value;

                    int width = 1;
                    while (u + width < uSize && mask[u + width, v] == faceType && !merged[u + width, v])
                        width++;

                    int height = 1;
                    bool canExpand = true;
                    while (v + height < vSize && canExpand)
                    {
                        for (int k = 0; k < width; k++)
                        {
                            if (mask[u + k, v + height] != faceType || merged[u + k, v + height])
                            { canExpand = false; break; }
                        }
                        if (canExpand) height++;
                    }

                    for (int du = 0; du < width; du++)
                        for (int dv = 0; dv < height; dv++)
                            merged[u + du, v + dv] = true;

                    // Face offset: positive faces sit at n+1, negative at n
                    float faceN = isPositiveFace ? n + 1f : n;

                    //Vector3 MakeVertex(float fu, float fv)
                    //{
                    //    float[] p = new float[3];
                    //    p[normalAxis] = faceN;
                    //    p[uAxis] = fu;
                    //    p[vAxis] = fv;
                    //    return new Vector3(p[0], p[1], p[2]);
                    //}
                    Vector3 MakeVertex(float fu, float fv)
                    {
                        float tileSize = WorldManager.Instance.ChunkTileSize;
                        float[] p = new float[3];
                        p[normalAxis] = faceN * tileSize;
                        p[uAxis] = fu * tileSize;
                        p[vAxis] = fv * tileSize;
                        return new Vector3(p[0], p[1], p[2]);
                    }


                    Vector3 v0 = MakeVertex(u, v);
                    Vector3 v1 = MakeVertex(u + width, v);
                    Vector3 v2 = MakeVertex(u + width, v + height);
                    Vector3 v3 = MakeVertex(u, v + height);

                    // Swap these — positive faces were winding the wrong way
                    Vector3[] quad;
                    if (direction == Direction.Forward || direction == Direction.Back)
                    {
                        // Z axis needs opposite winding to X and Y
                        quad = isPositiveFace
                            ? new[] { v0, v1, v2, v3 }
                            : new[] { v1, v0, v3, v2 };
                    }
                    else
                    {
                        quad = isPositiveFace
                            ? new[] { v1, v0, v3, v2 }
                            : new[] { v0, v1, v2, v3 };
                    }

                    int vertexIndex = chunkMeshData.Vertices.Count;
                    chunkMeshData.Vertices.AddRange(quad);
                    chunkMeshData.TriangleVertexIndices.AddRange(new[]
                    {
                    vertexIndex,     vertexIndex + 1, vertexIndex + 2,
                    vertexIndex,     vertexIndex + 2, vertexIndex + 3
                });

                    chunkMeshData.Uvs.AddRange(new[]
                    {
                    new Vector2(0,     0      ),
                    new Vector2(width, 0      ),
                    new Vector2(width, height ),
                    new Vector2(0,     height )
                });

                    var color = GetColor(faceType);
                    for (int i = 0; i < 4; i++)
                        chunkMeshData.VertexColors.Add(color);
                }
            }
        }
    }



    #endregion
}
