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
                road.curveStrengths.Add(road.defaultCurveStrength);
                Event.current.Use();
            }
        }

        handleTransform = road.transform;
        handleRotation = Tools.pivotRotation == PivotRotation.Local ? 
        handleTransform.rotation : Quaternion.identity;

        for (int i = 0; i < road.points.Count; i++)
        {
            Vector3 point = handleTransform.TransformPoint(road.points[i]);
            
            // Position handle
            EditorGUI.BeginChangeCheck();
            point = Handles.PositionHandle(point, handleRotation);
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(road, "Move Road Point");
                road.points[i] = handleTransform.InverseTransformPoint(point);
                road.GenerateRoadMesh();
            }

            // Curve strength handle
            float strength = i < road.curveStrengths.Count ? road.curveStrengths[i] : 1f;
            Handles.Label(point + Vector3.up * 0.5f, $"Curve: {strength:F1}");
            
            EditorGUI.BeginChangeCheck();
            strength = Handles.ScaleSlider(
                strength, 
                point, 
                Vector3.forward, 
                handleRotation, 
                1f, 
                0.1f
            );
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(road, "Adjust Curve Strength");
                if (i >= road.curveStrengths.Count)
                    road.curveStrengths.Add(strength);
                else
                    road.curveStrengths[i] = Mathf.Clamp(strength, 0f, 2f);
                road.GenerateRoadMesh();
            }
        }
    }
}