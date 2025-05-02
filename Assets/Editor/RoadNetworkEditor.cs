using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(RoadNetwork))]
public class RoadNetworkEditor : Editor
{
    private RoadNetwork network;
    private Road currentRoad;
    private bool isCreatingRoad;

    public float majorRoadWidth = 1.5f;
    public float districtRoadWidth = 0.8f;

    void OnSceneGUI()
    {
        network = (RoadNetwork)target;
        Event e = Event.current;

        HandleKeyPress(e);
        HandleRoadEditing(e);
        DrawExistingRoads();
    }

    void HandleKeyPress(Event e)
    {
        if (e.type == EventType.KeyDown)
        {
            if (e.keyCode == KeyCode.A)
            {
                StartNewRoad(true);
                e.Use();
            }
            else if (e.keyCode == KeyCode.S)
            {
                StartNewRoad(false);
                e.Use();
            }
        }
    }

    void HandleRoadEditing(Event e)
    {
        if (isCreatingRoad && e.type == EventType.MouseDown && e.button == 0)
        {
            AddPointToCurrentRoad();
            e.Use();
        }

        if (currentRoad != null && e.type == EventType.KeyDown && e.keyCode == KeyCode.Space)
        {
            FinalizeRoad();
            e.Use();
        }
    }

    void StartNewRoad(bool isMajor)
    {
        GameObject roadGO = new GameObject("Road");
        Undo.RegisterCreatedObjectUndo(roadGO, "Create Road");
        roadGO.transform.SetParent(network.transform); 
        currentRoad = roadGO.AddComponent<Road>();
        currentRoad.isMajorRoad = isMajor;
        currentRoad.width = isMajor ? majorRoadWidth : districtRoadWidth;
        currentRoad.meshFilter = roadGO.AddComponent<MeshFilter>();
        roadGO.AddComponent<MeshRenderer>().material = 
            isMajor ? network.majorRoadMaterial : network.districtRoadMaterial;
        
        Undo.RecordObject(network, "Add Road to Network");
        network.allRoads.Add(currentRoad);
        isCreatingRoad = true;
    }

    void AddPointToCurrentRoad()
    {
        Ray ray = HandleUtility.GUIPointToWorldRay(Event.current.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Undo.RecordObject(currentRoad, "Add Road Point");
            Vector3 snappedPoint = SnapToExisting(hit.point);
            if (currentRoad.isMajorRoad)
            {
                snappedPoint.y += 0.03f;
            }
            else
            {
                snappedPoint.y += 0.01f;
            }
            currentRoad.points.Add(snappedPoint);
            
            if (currentRoad.points.Count >= 2)
            {
                network.CheckIntersections(currentRoad);
            }
        }
    }

    void FinalizeRoad()
    {
        currentRoad.GenerateRoadMesh();
        isCreatingRoad = false;
        currentRoad = null;
    }

    Vector3 SnapToExisting(Vector3 position)
    {
        foreach (Road road in network.allRoads)
        {
            foreach (Vector3 point in road.points)
            {
                if (Vector3.Distance(position, point) < network.roadSnapDistance)
                    return point;
            }
        }
        return position;
    }

    void DrawExistingRoads()
    {
        foreach (Road road in network.allRoads)
        {
            Handles.color = road.isMajorRoad ? Color.red : Color.blue;
            for (int i = 0; i < road.points.Count - 1; i++)
            {
                Handles.DrawLine(road.points[i], road.points[i + 1], 3f);
            }
        }
    }
}