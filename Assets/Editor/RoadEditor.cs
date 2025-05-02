using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(Road))]
public class RoadEditor : Editor
{
    private Road road;
    private Transform handleTransform;
    private Quaternion handleRotation;
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
                Vector3 newPoint = hit.point;
                if (road.isMajorRoad)
                {
                    newPoint.y += 0.03f;
                }
                else
                {
                    newPoint.y += 0.01f;
                }
                road.points.Add(newPoint);
                Event.current.Use();
            }
        }

        handleTransform = road.transform;
        handleRotation = Tools.pivotRotation == PivotRotation.Local ? 
        handleTransform.rotation : Quaternion.identity;

        for (int i = 0; i < road.points.Count; i++)
        {
            // Draw position handle
            Vector3 point = handleTransform.TransformPoint(road.points[i]);
            EditorGUI.BeginChangeCheck();
            point = Handles.PositionHandle(point, handleRotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(road, "Move Road Point");
                road.points[i] = handleTransform.InverseTransformPoint(point);
                road.GenerateRoadMesh();
            }

            // Draw sphere handle
            Handles.color = Color.yellow;
            float handleSize = HandleUtility.GetHandleSize(point) * 0.15f;
            
            //deletion logic:
            if (Handles.Button(point, handleRotation, handleSize, handleSize, Handles.SphereHandleCap)) {
                Undo.RecordObject(road, "Delete Road Point");
                road.points.RemoveAt(i);
                road.GenerateRoadMesh();
                Event.current.Use();
            }
        }
    }
}