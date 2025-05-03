using UnityEngine;
using System.Collections.Generic;

public class MeshWarper : MonoBehaviour
{
    // The four corner control points (in local space)
    public Vector3 topLeftCorner = new Vector3(-2f, 0f, 2f);
    public Vector3 topRightCorner = new Vector3(2f, 0f, 2f);
    public Vector3 bottomLeftCorner = new Vector3(-2f, 0f, -2f);
    public Vector3 bottomRightCorner = new Vector3(2f, 0f, -2f);
    
    // Debug settings
    public bool debugMode = true;
    public Color debugColor = Color.magenta;
    
    // Normalization settings
    public float minX = -2f;
    public float maxX = 2f;
    public float minZ = -2f;
    public float maxZ = 2f;
    
    // Update settings
    public bool warpOnAwake = true;
    public bool continuousUpdate = false;
    public bool forceUpdateMeshes = false;
    
    // Mesh data
    private MeshFilter[] meshFilters;
    private Mesh[] originalMeshes;
    private Vector3[][] originalVertices;
    
    // Initialize on enable
    void OnEnable()
    {
        CollectMeshFilters();
    }
    
    // Force mesh warping through inspector
    void OnValidate()
    {
        if (forceUpdateMeshes && Application.isPlaying)
        {
            forceUpdateMeshes = false;
            WarpMeshes();
            Debug.Log("Forced mesh warping");
        }
    }
    
    void Awake()
    {
        if (warpOnAwake)
        {
            WarpMeshes();
        }
    }
    
    void Update()
    {
        if (continuousUpdate)
        {
            WarpMeshes();
        }
    }
    
    public void CollectMeshFilters()
    {
        // Auto-collect all child mesh filters
        meshFilters = GetComponentsInChildren<MeshFilter>();
        
        if (meshFilters.Length == 0)
        {
            Debug.LogWarning("No meshes found in children!");
            return;
        }
        
        Debug.Log($"Found {meshFilters.Length} meshes to warp");
        
        // Store original mesh data
        originalMeshes = new Mesh[meshFilters.Length];
        originalVertices = new Vector3[meshFilters.Length][];
        
        for (int i = 0; i < meshFilters.Length; i++)
        {
            if (meshFilters[i].sharedMesh == null)
            {
                Debug.LogError($"Mesh filter {meshFilters[i].name} has no shared mesh!");
                continue;
            }
            
            // Store original mesh
            originalMeshes[i] = meshFilters[i].sharedMesh;
            
            // Create mesh instance
            Mesh meshInstance = Instantiate(originalMeshes[i]);
            meshFilters[i].mesh = meshInstance;
            
            // Store original vertices
            originalVertices[i] = originalMeshes[i].vertices;
            
            if (debugMode)
            {
                Debug.Log($"Stored mesh for {meshFilters[i].name} with {originalVertices[i].Length} vertices");
            }
        }
    }
    
    [ContextMenu("Apply Warping")]
    public void WarpMeshes()
    {
        if (meshFilters == null || meshFilters.Length == 0)
        {
            CollectMeshFilters();
        }
        
        // Safety check
        if (meshFilters == null || originalVertices == null)
        {
            Debug.LogError("Mesh data not initialized properly!");
            return;
        }
        
        Debug.Log("Applying mesh warping...");
        
        // Warp each mesh
        for (int meshIndex = 0; meshIndex < meshFilters.Length; meshIndex++)
        {
            if (meshFilters[meshIndex] == null || meshFilters[meshIndex].mesh == null)
            {
                Debug.LogError($"Mesh filter {meshIndex} is null or has no mesh!");
                continue;
            }
            
            Vector3[] origVerts = originalVertices[meshIndex];
            Vector3[] newVertices = new Vector3[origVerts.Length];
            
            // Get mesh's world-to-local matrix to handle transforms
            Transform meshTransform = meshFilters[meshIndex].transform;
            Matrix4x4 worldToLocal = meshTransform.worldToLocalMatrix;
            Matrix4x4 localToWorld = meshTransform.localToWorldMatrix;
            Matrix4x4 parentLocalToWorld = transform.localToWorldMatrix;
            Matrix4x4 parentWorldToLocal = transform.worldToLocalMatrix;
            
            for (int i = 0; i < origVerts.Length; i++)
            {
                // Convert vertex to world space
                Vector3 vertexWorldPos = localToWorld.MultiplyPoint3x4(origVerts[i]);
                
                // Convert to parent space
                Vector3 vertexParentPos = parentWorldToLocal.MultiplyPoint3x4(vertexWorldPos);
                
                // Normalize position relative to bounding area
                float normalizedX = Mathf.InverseLerp(minX, maxX, vertexParentPos.x);
                float normalizedZ = Mathf.InverseLerp(minZ, maxZ, vertexParentPos.z);
                
                // Bilinear interpolation between corners
                Vector3 warpedPosParentSpace = BilinearInterpolation(
                    normalizedX,
                    normalizedZ,
                    bottomLeftCorner,
                    bottomRightCorner,
                    topRightCorner,
                    topLeftCorner
                );
                
                // Preserve Y height
                warpedPosParentSpace.y = vertexParentPos.y;
                
                // Convert back to world space
                Vector3 warpedWorldPos = parentLocalToWorld.MultiplyPoint3x4(warpedPosParentSpace);
                
                // Convert back to local space
                newVertices[i] = worldToLocal.MultiplyPoint3x4(warpedWorldPos);
            }
            
            // Apply new vertices
            meshFilters[meshIndex].mesh.vertices = newVertices;
            meshFilters[meshIndex].mesh.RecalculateNormals();
            meshFilters[meshIndex].mesh.RecalculateBounds();
            
            if (debugMode)
            {
                Debug.Log($"Warped mesh {meshFilters[meshIndex].name}");
            }
        }
    }
    
    Vector3 BilinearInterpolation(float x, float z, Vector3 bl, Vector3 br, Vector3 tr, Vector3 tl)
    {
        // Bilinear interpolation formula
        Vector3 bottom = Vector3.Lerp(bl, br, x);
        Vector3 top = Vector3.Lerp(tl, tr, x);
        return Vector3.Lerp(bottom, top, z);
    }
    
    // Visualize in editor
    void OnDrawGizmos()
    {
        // Draw the warped quad
        Gizmos.color = debugColor;
        
        Vector3 bl = transform.TransformPoint(bottomLeftCorner);
        Vector3 br = transform.TransformPoint(bottomRightCorner);
        Vector3 tr = transform.TransformPoint(topRightCorner);
        Vector3 tl = transform.TransformPoint(topLeftCorner);
        
        Gizmos.DrawLine(bl, br);
        Gizmos.DrawLine(br, tr);
        Gizmos.DrawLine(tr, tl);
        Gizmos.DrawLine(tl, bl);
        
        // Draw corner points
        float cubeSize = 0.15f;
        Gizmos.DrawCube(bl, Vector3.one * cubeSize);
        Gizmos.DrawCube(br, Vector3.one * cubeSize);
        Gizmos.DrawCube(tr, Vector3.one * cubeSize);
        Gizmos.DrawCube(tl, Vector3.one * cubeSize);
        
        // Draw original bounds
        Gizmos.color = Color.yellow;
        Vector3 center = transform.position + new Vector3((minX + maxX) / 2f, 0, (minZ + maxZ) / 2f);
        Vector3 size = new Vector3(maxX - minX, 0.1f, maxZ - minZ);
        Gizmos.DrawWireCube(center, size);
    }
}