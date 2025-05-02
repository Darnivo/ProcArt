using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class Road : MonoBehaviour
{
    public List<Vector3> points = new List<Vector3>();
    public List<float> curveStrengths = new List<float>();
    public float width = 5f;
    public bool isMajorRoad = true;
    public MeshFilter meshFilter;
    [Range(0.1f, 5f)] public float curveThreshold = 2f;
    [Range(0f, 2f)] public float defaultCurveStrength = 1f;
    private float lastThreshold = -1;

    private void Update()
    {
        if (curveThreshold != lastThreshold)
        {
            GenerateRoadMesh();
            lastThreshold = curveThreshold;
        }
        else
        {
            GenerateRoadMesh();
        }
    }

    void Start() 
    {
        while (curveStrengths.Count < points.Count)
            curveStrengths.Add(defaultCurveStrength);
    }

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

        // Pre-calculate all directions for more consistency
        List<Vector3> directions = new List<Vector3>();
        for (int i = 0; i < subdividedPoints.Count; i++)
        {
            Vector3 forward = Vector3.zero;
            
            // For first point, use direction to next point
            if (i == 0) 
            {
                forward = (subdividedPoints[1] - subdividedPoints[0]).normalized;
            }
            // For last point, use direction from previous point
            else if (i == subdividedPoints.Count - 1) 
            {
                forward = (subdividedPoints[i] - subdividedPoints[i-1]).normalized;
            }
            // For all middle points, average the directions
            else 
            {
                Vector3 inDir = (subdividedPoints[i] - subdividedPoints[i-1]).normalized;
                Vector3 outDir = (subdividedPoints[i+1] - subdividedPoints[i]).normalized;
                
                // Special case for sharp turns - avoid "pinching" by using appropriate direction
                float angle = Vector3.Angle(inDir, outDir);
                if (angle > 90f)
                {
                    // For sharp turns, don't average the directions to prevent narrowed road
                    // Instead use the bisector of the angle
                    forward = (inDir + outDir).normalized;
                    
                    // If forward is zero (180 degree turn), use perpendicular
                    if (forward.magnitude < 0.01f)
                    {
                        forward = Vector3.Cross(Vector3.Cross(inDir, Vector3.up), inDir).normalized;
                    }
                }
                else
                {
                    // For smoother turns, averaging works well
                    forward = (inDir + outDir).normalized;
                }
            }
            
            directions.Add(forward);
        }

        // Calculate consistent right vectors for width
        for (int i = 0; i < subdividedPoints.Count; i++)
        {
            Vector3 forward = directions[i];
            
            // Make sure the right vector is always perpendicular to the up vector for consistent width
            Vector3 right = Vector3.Cross(forward, Vector3.up).normalized * width;
            
            // Add vertices with precise width
            vertices.Add(subdividedPoints[i] + right);
            vertices.Add(subdividedPoints[i] - right);

            // Fixed UVs
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
        mesh.SetUVs(0, uvs);
        mesh.RecalculateNormals();
        meshFilter.mesh = mesh;
    }

    private List<Vector3> SubdividePointsForSharpTurns(List<Vector3> points) 
    {
        List<Vector3> subdivided = new List<Vector3>();

        if (points.Count < 2) return points;

        // Ensure we have curve strengths for all points
        while (curveStrengths.Count < points.Count)
            curveStrengths.Add(defaultCurveStrength);

        // Always add the first point
        subdivided.Add(points[0]);

        for (int i = 0; i < points.Count - 1; i++)
        {
            Vector3 p0 = i > 0 ? points[i - 1] : points[i] - (points[i + 1] - points[i]);
            Vector3 p1 = points[i];
            Vector3 p2 = points[i+1];
            Vector3 p3 = (i+2 < points.Count) ? points[i+2] : p2 + (p2 - p1);

            // Get the curve strength for the current and next point
            float currentStrength = curveStrengths[i];
            float nextStrength = (i + 1 < curveStrengths.Count) ? curveStrengths[i + 1] : defaultCurveStrength;
            
            // If both strengths are 0, create a straight line
            if (currentStrength <= 0.01f && nextStrength <= 0.01f)
            {
                // Only add p2 (next point) if this is the last segment or if we're doing a straight line
                subdivided.Add(p2);
                continue;
            }

            // Calculate number of segments based on the angle and curve strength
            float angle = Vector3.Angle(p2 - p1, p1 - p0);
            int segments = Mathf.CeilToInt(angle * Mathf.Max(currentStrength, nextStrength) / curveThreshold);
            segments = Mathf.Clamp(segments, 5, 20); // Always have at least 5 segment for curved sections

            // Add intermediate points using a modified Catmull-Rom calculation
            for (int s = 0; s < segments; s++)
            {
                float t = (s + 1) / (float)(segments + 1);
                
                // Calculate a weighted position based on the curve strengths
                Vector3 catmullPoint = GetCatmullRomPosition(t, p0, p1, p2, p3);
                Vector3 linearPoint = Vector3.Lerp(p1, p2, t);
                
                // Blend between the Catmull point and linear point based on the average curve strength
                float blendFactor = (currentStrength + nextStrength) / 2f;
                Vector3 finalPoint = Vector3.Lerp(linearPoint, catmullPoint, blendFactor);
                
                subdivided.Add(finalPoint);
            }
            
            // Add the endpoint of this segment
            subdivided.Add(p2);
        }
        
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