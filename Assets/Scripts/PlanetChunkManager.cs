using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Threading;

public class PlanetChunkManager : MonoBehaviour
{
    [System.Serializable]
    public struct LODSettings
    {
        public float distance;
        public int resolution;
        public float colliderDistance;
    }
    
    public class MeshData
    {
        public List<Vector3> vertices = new List<Vector3>();
        public List<int> triangles = new List<int>();
        public List<Vector2> uvs = new List<Vector2>();
        
        public Mesh CreateMesh()
        {
            Mesh mesh = new Mesh();
            mesh.vertices = vertices.ToArray();
            mesh.triangles = triangles.ToArray();
            mesh.uv = uvs.ToArray();
            
            mesh.RecalculateNormals();
            mesh.RecalculateTangents();
            mesh.RecalculateBounds();
            mesh.Optimize();
            
            return mesh;
        }
    }

    [Header("Chunk References")]
    public MarchingSquaresTerraingen planetGenerator;
    public Material chunkMaterial;
    public Transform player;

    [Header("Runtime Info")]
    public int activeChunks;
    public int queuedChunks;

    // Configuration (set by PlanetGenerator)
    [HideInInspector] public int chunkSize;
    [HideInInspector] public int loadRadius;
    [HideInInspector] public int maxChunksPerFrame;
    [HideInInspector] public float chunkLoadDistance;
    [HideInInspector] public LODSettings[] lodSettings;

    private Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();
    private Queue<ChunkTask> generationQueue = new Queue<ChunkTask>();
    private Queue<Chunk> updateQueue = new Queue<Chunk>();
    private bool running = true;

    private class Chunk
    {
        public Vector3Int coord;
        public GameObject gameObject;
        public MeshFilter meshFilter;
        public MeshCollider meshCollider;
        public LODLevel[] lodLevels;
        public bool needsCollider;
        public bool isActive;
        public int currentLOD = -1;
    }

    private class LODLevel
    {
        public MeshData meshData;
        public Mesh mesh;
        public int resolution;
    }

    private struct ChunkTask
    {
        public Vector3Int coord;
        public System.Action<Chunk> callback;
    }

    public void InitializeChunks()
    {
        if (player == null)
        {
            // Use the main camera if player is not assigned
            if (Camera.main != null)
            {
                player = Camera.main.transform;
            }
            else
            {
                Debug.LogError("Player reference not set in PlanetChunkManager and no main camera found");
                return;
            }
        }

        StartCoroutine(UpdateChunks());
        StartCoroutine(ProcessQueue());
        
        // Start background thread for mesh data generation (not Unity objects)
        Thread thread = new Thread(new ThreadStart(ThreadedMeshDataGeneration));
        thread.IsBackground = true;
        thread.Priority = System.Threading.ThreadPriority.BelowNormal;
        thread.Start();
    }

    private void OnDestroy()
    {
        running = false;
    }

    private IEnumerator UpdateChunks()
    {
        while (running)
        {
            if (player == null) yield break;
            
            Vector3Int playerChunkCoord = GetChunkCoord(player.position);
            LoadChunksAround(playerChunkCoord);
            UnloadDistantChunks(playerChunkCoord);
            UpdateChunkLODs();
            
            // Update runtime info
            activeChunks = chunks.Count;
            queuedChunks = generationQueue.Count;
            
            yield return new WaitForSeconds(0.1f);
        }
    }

    private IEnumerator ProcessQueue()
    {
        while (running)
        {
            if (updateQueue.Count > 0)
            {
                int chunksProcessed = 0;
                while (updateQueue.Count > 0 && chunksProcessed < maxChunksPerFrame)
                {
                    Chunk chunk = updateQueue.Dequeue();
                    if (chunk != null)
                    {
                        UpdateChunkMesh(chunk);
                    }
                    chunksProcessed++;
                }
            }
            yield return null;
        }
    }

    private void ThreadedMeshDataGeneration()
    {
        while (running)
        {
            if (generationQueue.Count > 0)
            {
                ChunkTask task = generationQueue.Dequeue();
                Chunk chunk = GenerateChunkData(task.coord);
                task.callback?.Invoke(chunk);
            }
            else
            {
                Thread.Sleep(10);
            }
        }
    }

    private Vector3Int GetChunkCoord(Vector3 position)
    {
        return new Vector3Int(
            Mathf.FloorToInt(position.x / chunkSize),
            Mathf.FloorToInt(position.y / chunkSize),
            Mathf.FloorToInt(position.z / chunkSize)
        );
    }

    private void LoadChunksAround(Vector3Int center)
    {
        for (int x = -loadRadius; x <= loadRadius; x++)
        {
            for (int y = -loadRadius; y <= loadRadius; y++)
            {
                for (int z = -loadRadius; z <= loadRadius; z++)
                {
                    Vector3Int coord = center + new Vector3Int(x, y, z);
                    if (!chunks.ContainsKey(coord))
                    {
                        RequestChunkGeneration(coord);
                    }
                }
            }
        }
    }

    private void RequestChunkGeneration(Vector3Int coord)
    {
        // Create placeholder chunk
        Chunk chunk = new Chunk
        {
            coord = coord,
            isActive = false
        };
        chunks.Add(coord, chunk);

        // Enqueue generation task
        generationQueue.Enqueue(new ChunkTask
        {
            coord = coord,
            callback = (generatedChunk) =>
            {
                lock (updateQueue)
                {
                    updateQueue.Enqueue(generatedChunk);
                }
            }
        });
    }

    private Chunk GenerateChunkData(Vector3Int coord)
    {
        if (planetGenerator == null) return null;
        
        Chunk chunk = new Chunk();
        chunk.coord = coord;
        chunk.lodLevels = new LODLevel[lodSettings.Length];

        // Generate all LOD levels (mesh data only, no Unity objects)
        for (int i = 0; i < lodSettings.Length; i++)
        {
            chunk.lodLevels[i] = new LODLevel
            {
                resolution = lodSettings[i].resolution,
                meshData = planetGenerator.GenerateChunkData(coord, lodSettings[i].resolution)
            };
        }

        return chunk;
    }

    private void UpdateChunkMesh(Chunk chunk)
    {
        // Create Unity objects only on the main thread
        if (chunk.gameObject == null)
        {
            chunk.gameObject = new GameObject($"Chunk_{chunk.coord}");
            chunk.gameObject.transform.SetParent(transform);
            chunk.gameObject.transform.position = new Vector3(
                chunk.coord.x * chunkSize,
                chunk.coord.y * chunkSize,
                chunk.coord.z * chunkSize
            );
            chunk.meshFilter = chunk.gameObject.AddComponent<MeshFilter>();
            chunk.meshCollider = chunk.gameObject.AddComponent<MeshCollider>();
            MeshRenderer renderer = chunk.gameObject.AddComponent<MeshRenderer>();
            renderer.material = chunkMaterial;
            
            // Initialize with empty mesh to prevent errors
            chunk.meshFilter.mesh = new Mesh();
        }

        // Convert mesh data to Unity meshes
        for (int i = 0; i < chunk.lodLevels.Length; i++)
        {
            if (chunk.lodLevels[i].mesh == null && chunk.lodLevels[i].meshData != null)
            {
                try
                {
                    chunk.lodLevels[i].mesh = chunk.lodLevels[i].meshData.CreateMesh();
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"Error creating mesh for chunk {chunk.coord} LOD {i}: {e.Message}");
                    // Create fallback mesh
                    chunk.lodLevels[i].mesh = new Mesh();
                }
                
                // Clear data to save memory
                chunk.lodLevels[i].meshData = null;
            }
        }

        // Determine appropriate LOD level
        float distance = Vector3.Distance(
            player.position,
            chunk.gameObject.transform.position
        );

        int lodIndex = 0;
        for (int i = 0; i < lodSettings.Length - 1; i++)
        {
            if (distance > lodSettings[i].distance)
            {
                lodIndex = i + 1;
            }
            else
            {
                break;
            }
        }

        // Only update if LOD changed
        if (chunk.currentLOD != lodIndex && chunk.lodLevels[lodIndex].mesh != null)
        {
            chunk.meshFilter.mesh = chunk.lodLevels[lodIndex].mesh;
            chunk.currentLOD = lodIndex;
        }

        // Update collider
        if (chunk.needsCollider && distance <= lodSettings[lodIndex].colliderDistance)
        {
            if (chunk.lodLevels[lodIndex].mesh != null)
            {
                chunk.meshCollider.sharedMesh = chunk.lodLevels[lodIndex].mesh;
                chunk.needsCollider = false;
            }
        }
        else if (chunk.meshCollider.sharedMesh != null && distance > lodSettings[lodIndex].colliderDistance)
        {
            chunk.meshCollider.sharedMesh = null;
            chunk.needsCollider = true;
        }

        chunk.isActive = true;
    }

    private void UpdateChunkLODs()
    {
        foreach (KeyValuePair<Vector3Int, Chunk> pair in chunks)
        {
            if (pair.Value.isActive)
            {
                lock (updateQueue)
                {
                    updateQueue.Enqueue(pair.Value);
                }
            }
        }
    }

    private void UnloadDistantChunks(Vector3Int center)
    {
        List<Vector3Int> toRemove = new List<Vector3Int>();

        foreach (KeyValuePair<Vector3Int, Chunk> pair in chunks)
        {
            float distance = Vector3.Distance(
                new Vector3(pair.Key.x, pair.Key.y, pair.Key.z) * chunkSize,
                new Vector3(center.x, center.y, center.z) * chunkSize
            );

            if (distance > chunkLoadDistance * loadRadius)
            {
                if (pair.Value.gameObject != null)
                {
                    Destroy(pair.Value.gameObject);
                }
                toRemove.Add(pair.Key);
            }
        }

        foreach (Vector3Int coord in toRemove)
        {
            chunks.Remove(coord);
        }
    }
}