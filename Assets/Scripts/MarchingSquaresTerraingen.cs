using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(PlanetChunkManager))]
public class MarchingSquaresTerraingen : MonoBehaviour
{
    [Header("Planet Settings")]
    public float planetRadius = 100f;
    public int baseFaceResolution = 20;
    public float noiseFrequency = 0.1f;
    public float noiseAmplitude = 10f;
    public Material planetMaterial;
    
    [Header("Chunk Settings")]
    public int chunkSize = 16;
    public int loadRadius = 3;
    public int maxChunksPerFrame = 2;
    public float chunkLoadDistance = 100f;
    public PlanetChunkManager.LODSettings[] lodSettings;
    
    [Header("Debug Settings")]
    public bool showGizmos = true;
    
    private PlanetChunkManager chunkManager;
    private GameObject planetContainer;
    
    private void Start()
    {
        // Get or create chunk manager
        chunkManager = GetComponent<PlanetChunkManager>();
        if (chunkManager == null)
        {
            chunkManager = gameObject.AddComponent<PlanetChunkManager>();
        }
        
        // Configure chunk manager
        chunkManager.chunkSize = chunkSize;
        chunkManager.loadRadius = loadRadius;
        chunkManager.maxChunksPerFrame = maxChunksPerFrame;
        chunkManager.chunkLoadDistance = chunkLoadDistance;
        chunkManager.lodSettings = lodSettings;
        chunkManager.planetGenerator = this;
        chunkManager.chunkMaterial = planetMaterial;
        
        // Create planet container
        planetContainer = new GameObject("Planet");
        planetContainer.transform.position = transform.position;
        
        // Start generation
        chunkManager.InitializeChunks();
    }
    
    public PlanetChunkManager.MeshData GenerateChunkData(Vector3Int coord, int resolution)
    {
        // Create mesh data structure
        PlanetChunkManager.MeshData meshData = new PlanetChunkManager.MeshData();
        
        // Calculate chunk center in world space
        Vector3 chunkCenter = new Vector3(
            (coord.x + 0.5f) * chunkSize,
            (coord.y + 0.5f) * chunkSize,
            (coord.z + 0.5f) * chunkSize
        );
        
        // Generate the 6 faces of this chunk
        Vector3[] faceNormals = {
            Vector3.up, Vector3.down,
            Vector3.left, Vector3.right,
            Vector3.forward, Vector3.back
        };
        
        foreach (Vector3 normal in faceNormals)
        {
            GenerateChunkFace(coord, resolution, chunkCenter, normal, meshData);
        }
        
        return meshData;
    }
    
    private void GenerateChunkFace(Vector3Int coord, int resolution, Vector3 chunkCenter, 
                                  Vector3 localUp, PlanetChunkManager.MeshData meshData)
    {
        // Calculate perpendicular axes
        Vector3 axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        Vector3 axisB = Vector3.Cross(localUp, axisA);
        
        // Face offset from chunk center
        Vector3 faceOffset = localUp * (chunkSize / 2f);
        
        // Store start index for this face
        int faceStartIndex = meshData.vertices.Count;
        
        // Create vertices for this face
        for (int y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++)
            {
                Vector2 percent = new Vector2(x, y) / Mathf.Max(1, resolution - 1);
                Vector3 pointOnCube = faceOffset + 
                    (percent.x - 0.5f) * chunkSize * axisA + 
                    (percent.y - 0.5f) * chunkSize * axisB;
                
                Vector3 pointOnSphere = ProjectToSphere(pointOnCube + chunkCenter);
                
                meshData.vertices.Add(pointOnSphere);
                meshData.uvs.Add(new Vector2(percent.x, percent.y));
            }
        }
        
        // Create triangles for this face (using faceStartIndex as offset)
        for (int y = 0; y < resolution - 1; y++)
        {
            for (int x = 0; x < resolution - 1; x++)
            {
                int i0 = faceStartIndex + y * resolution + x;
                int i1 = faceStartIndex + y * resolution + x + 1;
                int i2 = faceStartIndex + (y + 1) * resolution + x;
                int i3 = faceStartIndex + (y + 1) * resolution + x + 1;
                
                // First triangle (i0 → i2 → i1)
                meshData.triangles.Add(i0);
                meshData.triangles.Add(i2);
                meshData.triangles.Add(i1);
                
                // Second triangle (i1 → i2 → i3)
                meshData.triangles.Add(i1);
                meshData.triangles.Add(i2);
                meshData.triangles.Add(i3);
            }
        }
    }
    
    private Vector3 ProjectToSphere(Vector3 point)
    {
        // Ensure we don't normalize a zero vector
        if (point == Vector3.zero) point = Vector3.one * 0.001f;
        
        // Project cube point to sphere
        Vector3 spherePoint = point.normalized * planetRadius;
        
        // Add noise to create terrain features
        float noiseValue = PerlinNoise3D(spherePoint * noiseFrequency) * noiseAmplitude;
        return spherePoint + spherePoint.normalized * noiseValue;
    }
    
    private float PerlinNoise3D(Vector3 coord)
    {
        // Improved 3D noise implementation
        float noise = 0f;
        float frequency = 1f;
        float amplitude = 1f;
        float maxValue = 0f;
        int octaves = 4;
        
        for (int i = 0; i < octaves; i++)
        {
            // Sample noise at different rotations for better 3D coverage
            noise += Mathf.PerlinNoise(coord.x * frequency, coord.y * frequency) * amplitude;
            noise += Mathf.PerlinNoise(coord.y * frequency, coord.z * frequency) * amplitude;
            noise += Mathf.PerlinNoise(coord.z * frequency, coord.x * frequency) * amplitude;
            
            maxValue += 3f * amplitude;
            amplitude *= 0.5f;
            frequency *= 2f;
        }
        
        return noise / maxValue;
    }
    
    private void OnDrawGizmos()
    {
        if (!showGizmos || planetContainer == null) return;
        
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(planetContainer.transform.position, planetRadius);
    }
}