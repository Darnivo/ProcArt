using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class Road : MonoBehaviour
{
    public List<Vector3> points = new List<Vector3>();
    public float width = 5f;
    public bool isMajorRoad = true;
    public MeshFilter meshFilter;
    [Range(0.1f, 5f)] public float curveThreshold = 2f;

    private void Update() => GenerateRoadMesh();

    public void GenerateRoadMesh()
    {
        if (points.Count < 2) return;

        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        // Calculate road length for UV mapping
        float totalLength = 0;
        List<float> cumulativeLengths = new List<float> { 0 };
        for (int i = 1; i < points.Count; i++)
        {
            totalLength += Vector3.Distance(points[i-1], points[i]);
            cumulativeLengths.Add(totalLength);
        }

        for (int i = 0; i < points.Count; i++)
        {
            Vector3 forward = Vector3.zero;
            
            // Calculate direction with curve smoothing
            if (i < points.Count - 1)
                forward += points[i + 1] - points[i];
            if (i > 0)
                forward += points[i] - points[i - 1];
            
            forward.Normalize();
            Vector3 right = Vector3.Cross(Vector3.up, forward).normalized;

            // Add vertices
            vertices.Add(points[i] + right * width);
            vertices.Add(points[i] - right * width);

            // Add UVs
            float uvU = totalLength > 0 ? cumulativeLengths[i] / totalLength : 0;
            uvs.Add(new Vector2(uvU, 0));
            uvs.Add(new Vector2(uvU, 1));

            // Create triangles
            if (i > 0)
            {
                int count = vertices.Count;
                triangles.Add(count - 4);
                triangles.Add(count - 2);
                triangles.Add(count - 3);
                
                triangles.Add(count - 2);
                triangles.Add(count - 1);
                triangles.Add(count - 3);
            }
        }

        mesh.SetVertices(vertices);
        mesh.SetTriangles(triangles, 0);
        mesh.SetUVs(0, uvs); // Apply UVs
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
    }

    public List<LineSegment> GetSegments()
    {
        List<LineSegment> segments = new List<LineSegment>();
        for(int i = 0; i < points.Count - 1; i++)
        {
            segments.Add(new LineSegment(points[i], points[i+1]));
        }
        return segments;
    }

    
}