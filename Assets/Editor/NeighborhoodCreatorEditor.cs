using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NeighborhoodCreator))]
public class NeighborhoodCreatorEditor : Editor
{
    private NeighborhoodCreator creator;
    private int selectedPoint = 0; // 0 for pathStart, 1 for pathEnd

    private void OnEnable()
    {
        creator = (NeighborhoodCreator)target;
        // Ensure the target is not null when the editor is enabled
        if (creator == null)
        {
            Debug.LogError("NeighborhoodCreator target is null in OnEnable.");
            return;
        }
    }

    public override void OnInspectorGUI()
    {
        // Draw the default inspector fields
        DrawDefaultInspector();

        // Add the Generate and Wipe buttons
        GUILayout.Space(10);
        if (GUILayout.Button("Generate Neighborhood"))
        {
            creator.GenerateNeighborhood();
        }

        if (GUILayout.Button("Wipe Neighborhood"))
        {
            creator.WipeNeighborhood();
        }

        // Apply changes if any were made in the inspector
        if (GUI.changed)
        {
            EditorUtility.SetDirty(creator);
        }
    }

    private void OnSceneGUI()
    {
        // Ensure the target is not null in OnSceneGUI
        if (creator == null)
        {
            return;
        }

        // Allow undo for changes made in the scene view
        Undo.RecordObject(creator, "Change Neighborhood Path");

        // Draw and handle the start point
        Handles.color = Color.green;
        Vector3 newPathStart = Handles.PositionHandle(creator.pathStart, Quaternion.identity);
        if (newPathStart != creator.pathStart)
        {
            creator.pathStart = newPathStart;
            // Mark the object as dirty to save the changes
            EditorUtility.SetDirty(creator);
        }

        // Draw and handle the end point
        Handles.color = Color.red;
        Vector3 newPathEnd = Handles.PositionHandle(creator.pathEnd, Quaternion.identity);
        if (newPathEnd != creator.pathEnd)
        {
            creator.pathEnd = newPathEnd;
            // Mark the object as dirty to save the changes
            EditorUtility.SetDirty(creator);
        }

        // Draw the path line
        Handles.color = Color.white;
        Handles.DrawLine(creator.pathStart, creator.pathEnd);

        // Handle key presses for selecting points
        HandleUtility.AddDefaultControl(GUIUtility.GetControlID(FocusType.Passive)); // Allow clicking on handles

        Event guiEvent = Event.current;
        if (guiEvent.type == EventType.KeyDown)
        {
            if (guiEvent.keyCode == KeyCode.A)
            {
                selectedPoint = 0; // Select start point
                guiEvent.Use(); // Consume the event
            }
            else if (guiEvent.keyCode == KeyCode.D)
            {
                selectedPoint = 1; // Select end point
                guiEvent.Use(); // Consume the event
            }
        }

        // Draw a visual indicator for the currently selected point
        Handles.color = Color.yellow;
        if (selectedPoint == 0)
        {
            Handles.SphereHandleCap(0, creator.pathStart, Quaternion.identity, 0.5f, EventType.Repaint);
        }
        else
        {
            Handles.SphereHandleCap(0, creator.pathEnd, Quaternion.identity, 0.5f, EventType.Repaint);
        }
    }
}
