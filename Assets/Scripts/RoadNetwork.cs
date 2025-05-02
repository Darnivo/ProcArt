using UnityEngine;
using System.Collections.Generic;
using UnityEditor;

[ExecuteInEditMode]
public class RoadNetwork : MonoBehaviour
{
    public List<Road> allRoads = new List<Road>();
    public GameObject intersectionPrefab;
    public Material majorRoadMaterial;
    public Material districtRoadMaterial;
    public float roadSnapDistance = 1.0f;

    public void CheckIntersections(Road newRoad)
    {
        foreach(Road existingRoad in allRoads)
        {
            if(existingRoad == newRoad) continue;

            foreach(LineSegment newSegment in newRoad.GetSegments())
            {
                foreach(LineSegment existingSegment in existingRoad.GetSegments())
                {
                    if(LineSegment.Intersect(newSegment, existingSegment, out Vector3 intersection))
                    {
                        CreateIntersection(intersection);
                    }
                }
            }
        }
    }

    void CreateIntersection(Vector3 position)
    {
        float mergeThreshold = 0.5f;
        foreach (Transform child in transform)
        {
            if (Vector3.Distance(child.position, position) < mergeThreshold)
                return;
        }
        GameObject intersectionGO = Instantiate(intersectionPrefab, position, Quaternion.identity, transform);
        Intersection intersection = intersectionGO.GetComponent<Intersection>();
        Undo.RegisterCreatedObjectUndo(intersectionGO, "Create Intersection");
    }

    [MenuItem("Roads/Delete All Roads")]
    public static void DeleteAllRoads()
    {
        RoadNetwork network = Selection.activeGameObject?.GetComponent<RoadNetwork>();
        if (network == null)
        {
            Debug.LogError("No RoadNetwork selected.");
            return;
        }

        Undo.RecordObject(network, "Delete all roads");
        // Create temp list to avoid modification during iteration
        var roadsToDelete = new List<Road>(network.allRoads);
        foreach (var road in roadsToDelete)
        {
            if (road != null)
                Undo.DestroyObjectImmediate(road.gameObject);
        }
        network.allRoads.Clear();
    }
    
}

public struct LineSegment
{
    public Vector3 start;
    public Vector3 end;

    public LineSegment(Vector3 s, Vector3 e)
    {
        start = s;
        end = e;
    }

    public static bool Intersect(LineSegment a, LineSegment b, out Vector3 intersection)
    {
        intersection = Vector3.zero;
        
        Vector3 dirA = a.end - a.start;
        Vector3 dirB = b.end - b.start;

        Vector3 cross = Vector3.Cross(dirA, dirB);
        if(Mathf.Approximately(cross.sqrMagnitude, 0)) return false;

        Vector3 between = b.start - a.start;
        float t = Vector3.Dot(Vector3.Cross(between, dirB), cross) / cross.sqrMagnitude;
        float s = Vector3.Dot(Vector3.Cross(between, dirA), cross) / cross.sqrMagnitude;

        if(t >= 0 && t <= 1 && s >= 0 && s <= 1)
        {
            intersection = a.start + t * dirA;
            return true;
        }
        return false;
    }
}