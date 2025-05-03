using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(MeshWarper))]
public class MeshWarperEditor : Editor
{
    private MeshWarper warper;
    private Tool lastTool = Tool.None;
    private bool editingCorners = false;
    private int selectedCorner = -1;
    
    private readonly string[] cornerNames = { "Bottom Left", "Bottom Right", "Top Right", "Top Left" };
    
    private void OnEnable()
    {
        warper = (MeshWarper)target;
        lastTool = Tools.current;
    }
    
    public override void OnInspectorGUI()
    {
        EditorGUI.BeginChangeCheck();
        
        // Draw settings section
        EditorGUILayout.LabelField("Warping Settings", EditorStyles.boldLabel);
        EditorGUILayout.Space();
        
        // Corner positions
        SerializedProperty bottomLeft = serializedObject.FindProperty("bottomLeftCorner");
        SerializedProperty bottomRight = serializedObject.FindProperty("bottomRightCorner");
        SerializedProperty topRight = serializedObject.FindProperty("topRightCorner");
        SerializedProperty topLeft = serializedObject.FindProperty("topLeftCorner");
        
        EditorGUILayout.PropertyField(bottomLeft);
        EditorGUILayout.PropertyField(bottomRight);
        EditorGUILayout.PropertyField(topRight);
        EditorGUILayout.PropertyField(topLeft);
        
        EditorGUILayout.Space();
        
        // Normalization bounds
        EditorGUILayout.LabelField("Normalization Bounds", EditorStyles.boldLabel);
        SerializedProperty minX = serializedObject.FindProperty("minX");
        SerializedProperty maxX = serializedObject.FindProperty("maxX");
        SerializedProperty minZ = serializedObject.FindProperty("minZ");
        SerializedProperty maxZ = serializedObject.FindProperty("maxZ");
        
        EditorGUILayout.PropertyField(minX);
        EditorGUILayout.PropertyField(maxX);
        EditorGUILayout.PropertyField(minZ);
        EditorGUILayout.PropertyField(maxZ);
        
        EditorGUILayout.Space();
        
        // Update settings
        EditorGUILayout.LabelField("Update Settings", EditorStyles.boldLabel);
        SerializedProperty warpOnAwake = serializedObject.FindProperty("warpOnAwake");
        SerializedProperty continuousUpdate = serializedObject.FindProperty("continuousUpdate");
        SerializedProperty forceUpdate = serializedObject.FindProperty("forceUpdateMeshes");
        SerializedProperty debugMode = serializedObject.FindProperty("debugMode");
        SerializedProperty debugColor = serializedObject.FindProperty("debugColor");
        
        EditorGUILayout.PropertyField(warpOnAwake);
        EditorGUILayout.PropertyField(continuousUpdate);
        EditorGUILayout.PropertyField(debugMode);
        EditorGUILayout.PropertyField(debugColor);
        
        if (EditorGUI.EndChangeCheck())
        {
            serializedObject.ApplyModifiedProperties();
        }
        
        EditorGUILayout.Space();
        
        // Action buttons
        EditorGUILayout.BeginHorizontal();
        
        if (GUILayout.Button("Apply Warping"))
        {
            warper.WarpMeshes();
        }
        
        if (GUILayout.Button("Refresh Mesh List"))
        {
            warper.CollectMeshFilters();
        }
        
        EditorGUILayout.EndHorizontal();
        
        if (GUILayout.Button(editingCorners ? "Finish Editing Corners" : "Edit Corners in Scene"))
        {
            editingCorners = !editingCorners;
            
            if (editingCorners)
            {
                lastTool = Tools.current;
                Tools.current = Tool.None;
            }
            else
            {
                Tools.current = lastTool;
                selectedCorner = -1;
            }
        }
        
        if (editingCorners)
        {
            EditorGUILayout.HelpBox("Click on a corner in the scene view to select it, then drag to move.", MessageType.Info);
            
            // Display which corner is selected
            if (selectedCorner >= 0 && selectedCorner < 4)
            {
                EditorGUILayout.LabelField("Selected:", cornerNames[selectedCorner], EditorStyles.boldLabel);
            }
            else
            {
                EditorGUILayout.LabelField("No corner selected", EditorStyles.boldLabel);
            }
        }
    }
    
    private void OnSceneGUI()
    {
        if (!editingCorners) return;
        
        Event e = Event.current;
        Transform transform = warper.transform;
        
        // Get corner positions in world space
        Vector3[] cornerWorldPositions = new Vector3[4];
        cornerWorldPositions[0] = transform.TransformPoint(warper.bottomLeftCorner);
        cornerWorldPositions[1] = transform.TransformPoint(warper.bottomRightCorner);
        cornerWorldPositions[2] = transform.TransformPoint(warper.topRightCorner);
        cornerWorldPositions[3] = transform.TransformPoint(warper.topLeftCorner);
        
        // Handle selection
        if (e.type == EventType.MouseDown && e.button == 0)
        {
            float minDist = float.MaxValue;
            int newSelected = -1;
            
            for (int i = 0; i < 4; i++)
            {
                Vector2 guiPoint = HandleUtility.WorldToGUIPoint(cornerWorldPositions[i]);
                float dist = Vector2.Distance(guiPoint, e.mousePosition);
                
                if (dist < 10f && dist < minDist)
                {
                    minDist = dist;
                    newSelected = i;
                }
            }
            
            if (newSelected != -1)
            {
                selectedCorner = newSelected;
                e.Use();
            }
        }
        
        // Handle the selected corner
        if (selectedCorner >= 0 && selectedCorner < 4)
        {
            EditorGUI.BeginChangeCheck();
            Vector3 newPos = Handles.PositionHandle(cornerWorldPositions[selectedCorner], Quaternion.identity);
            
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(warper, "Move Corner");
                
                Vector3 localPos = transform.InverseTransformPoint(newPos);
                
                switch (selectedCorner)
                {
                    case 0: warper.bottomLeftCorner = localPos; break;
                    case 1: warper.bottomRightCorner = localPos; break;
                    case 2: warper.topRightCorner = localPos; break;
                    case 3: warper.topLeftCorner = localPos; break;
                }
                
                if (Application.isPlaying && warper.continuousUpdate)
                {
                    warper.WarpMeshes();
                }
                
                EditorUtility.SetDirty(warper);
            }
        }
        
        // Draw handle buttons for each corner
        for (int i = 0; i < 4; i++)
        {
            Handles.color = (selectedCorner == i) ? Color.yellow : Color.red;
            if (Handles.Button(cornerWorldPositions[i], Quaternion.identity, 0.1f, 0.1f, Handles.DotHandleCap))
            {
                selectedCorner = i;
                Repaint();
            }
        }
        
        // Draw labels for each corner
        GUIStyle style = new GUIStyle();
        style.normal.textColor = Color.white;
        style.alignment = TextAnchor.MiddleCenter;
        style.fontSize = 12;
        style.fontStyle = FontStyle.Bold;
        
        for (int i = 0; i < 4; i++)
        {
            Handles.Label(cornerWorldPositions[i] + Vector3.up * 0.2f, cornerNames[i], style);
        }
    }
}