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

        List<Vector3> subdividedPoints = SubdividePointsForSharpTurns(points);

        Mesh mesh = new Mesh();
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        // Calculate road length for UV mapping
        float totalLength = 0;
        List<float> cumulativeLengths = new List<float> { 0 };
        for (int i = 1; i < subdividedPoints.Count; i++)
        {
            totalLength += Vector3.Distance(subdividedPoints[i - 1], subdividedPoints[i]);
            cumulativeLengths.Add(totalLength);
        }

        Vector3 prevForward = Vector3.forward;
        for (int i = 0; i < subdividedPoints.Count; i++)
        {
            // Calculate smooth forward direction
            Vector3 forward = Vector3.zero;
            if (i < subdividedPoints.Count - 1) forward += (subdividedPoints[i + 1] - subdividedPoints[i]).normalized;
            if (i > 0) forward += (subdividedPoints[i] - subdividedPoints[i - 1]).normalized;

            if (forward == Vector3.zero) forward = prevForward;
            forward.Normalize();
            prevForward = forward;

            // Get stable right vector using cross product with world up
            Vector3 right = Vector3.Cross(forward, Vector3.up).normalized * width;

            // Add vertices with stable offset
            vertices.Add(subdividedPoints[i] + right);
            vertices.Add(subdividedPoints[i] - right);

            // Fixed UVs (flipped V component)
            float uvU = totalLength > 0 ? cumulativeLengths[i] / totalLength : 0;
            uvs.Add(new Vector2(uvU, 1));  // Top edge
            uvs.Add(new Vector2(uvU, 0));  // Bottom edge

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

    private List<Vector3> SubdividePointsForSharpTurns(List<Vector3> points) 
    {
        List<Vector3> subdivided = new List<Vector3>();
        for (int i = 0; i < points.Count - 1; i++) {
            Vector3 p0 = i > 0 ? points[i-1] : points[i];
            Vector3 p1 = points[i];
            Vector3 p2 = points[i+1];
            Vector3 p3 = (i+2 < points.Count) ? points[i+2] : p2;

            // Add intermediate points using Catmull-Rom
            int segments = Mathf.CeilToInt(Vector3.Angle(p1 - p0, p2 - p1) / curveThreshold);
            for (int s = 0; s < segments; s++) {
                float t = s / (float)segments;
                subdivided.Add(GetCatmullRomPosition(t, p0, p1, p2, p3));
            }
        }
        subdivided.Add(points[points.Count-1]);
        return subdivided;
    }

    private Vector3 GetCatmullRomPosition(float t, Vector3 p0, Vector3 p1, Vector3 p2, Vector3 p3) 
    {
        float t2 = t * t;
        float t3 = t2 * t;
        return 0.5f * ((2 * p1) + (-p0 + p2) * t + 
            (2*p0 - 5*p1 + 4*p2 - p3) * t2 + 
            (-p0 + 3*p1 - 3*p2 + p3) * t3);
    }
    public List<LineSegment> GetSegments()
    {
        List<LineSegment> segments = new List<LineSegment>();
        for (int i = 0; i < points.Count - 1; i++)
        {
            segments.Add(new LineSegment(points[i], points[i + 1]));
        }
        return segments;
    }

    [System.Obsolete]
    private void OnDestroy()
    {
        if (FindObjectOfType<RoadNetwork>())
            FindObjectOfType<RoadNetwork>().allRoads.Remove(this);
    }
}
