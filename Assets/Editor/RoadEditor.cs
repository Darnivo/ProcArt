using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Road))]
public class RoadEditor : Editor
{
    private Road road;
    private void OnSceneGUI()
    {
        road = (Road)target;
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Space)
        {
            // Get mouse position in scene view
            Vector2 mousePos = Event.current.mousePosition;
            Ray ray = HandleUtility.GUIPointToWorldRay(mousePos);
            if (Physics.Raycast(ray, out RaycastHit hit))
            {
                Undo.RecordObject(road, "Add Road Point");
                road.points.Add(hit.point);
                Event.current.Use();
            }
        }
    }
}