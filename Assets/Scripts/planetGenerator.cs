using UnityEngine;
using System.Collections.Generic;

public class planetGenerator : MonoBehaviour
{
    [Header("Planet Settings")]
    public float planetRadius = 100f;
    public int faceResolution = 30;
    public float noiseFrequency = 0.1f;
    public float noiseAmplitude = 10f;
    public Material planetMaterial;
    
    [Header("Debug Settings")]
    public bool showGizmos = true;
    
    private GameObject planetContainer;
    
    private void Start()
    {
        GeneratePlanet();
    }
    
    private void GeneratePlanet()
    {
        // Create the 6 faces of the cube-sphere
        Vector3[] faceNormals = {
            Vector3.up, Vector3.down,
            Vector3.left, Vector3.right,
            Vector3.forward, Vector3.back
        };
        
        // Create a container for all faces
        planetContainer = new GameObject("Planet");
        planetContainer.transform.position = transform.position;
        
        // Create a single mesh for the entire planet
        GameObject planetObject = new GameObject("PlanetMesh");
        planetObject.transform.parent = planetContainer.transform;
        planetObject.transform.position = planetContainer.transform.position;
        
        MeshFilter meshFilter = planetObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = planetObject.AddComponent<MeshRenderer>();
        meshRenderer.material = planetMaterial;
        
        Mesh mesh = new Mesh();
        meshFilter.mesh = mesh;
        
        // Generate combined mesh for all faces
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        
        foreach (Vector3 normal in faceNormals)
        {
            GenerateFace(normal, vertices, triangles, uvs);
        }
        
        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();
        mesh.uv = uvs.ToArray();
        
        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
        mesh.Optimize();
        
        // Add collider to the planet
        MeshCollider collider = planetObject.AddComponent<MeshCollider>();
        collider.sharedMesh = mesh;
    }
    
    private void GenerateFace(Vector3 localUp, List<Vector3> vertices, List<int> triangles, List<Vector2> uvs)
    {
        // Calculate perpendicular axes
        Vector3 axisA = new Vector3(localUp.y, localUp.z, localUp.x);
        Vector3 axisB = Vector3.Cross(localUp, axisA);
        
        // Store start index for this face
        int faceStartIndex = vertices.Count;
        
        // Generate grid of vertices
        for (int y = 0; y < faceResolution; y++)
        {
            for (int x = 0; x < faceResolution; x++)
            {
                Vector2 percent = new Vector2(x, y) / Mathf.Max(1, faceResolution - 1);
                Vector3 pointOnCube = localUp + (percent.x - 0.5f) * 2 * axisA + (percent.y - 0.5f) * 2 * axisB;
                Vector3 pointOnSphere = ProjectToSphere(pointOnCube);
                
                vertices.Add(pointOnSphere);
                uvs.Add(new Vector2(percent.x, percent.y));
            }
        }
        
        // Create triangles
        for (int y = 0; y < faceResolution - 1; y++)
        {
            for (int x = 0; x < faceResolution - 1; x++)
            {
                int i0 = faceStartIndex + y * faceResolution + x;
                int i1 = faceStartIndex + y * faceResolution + x + 1;
                int i2 = faceStartIndex + (y + 1) * faceResolution + x;
                int i3 = faceStartIndex + (y + 1) * faceResolution + x + 1;
                
                // First triangle (clockwise)
                triangles.Add(i0);
                triangles.Add(i2);
                triangles.Add(i1);
                
                // Second triangle (clockwise)
                triangles.Add(i1);
                triangles.Add(i2);
                triangles.Add(i3);
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