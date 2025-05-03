using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(NeighborhoodCreator))]
public class NeighborhoodCreatorEditor : Editor
{
    private NeighborhoodCreator creator;

    private void OnEnable()
    {
        creator = (NeighborhoodCreator)target;
        if (creator == null)
        {
            Debug.LogError("NeighborhoodCreator target is null in OnEnable.");
            return;
        }
    }

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        GUILayout.Space(10);
        if (GUILayout.Button("Generate Neighborhood"))
        {
            creator.GenerateNeighborhood();
        }

        if (GUILayout.Button("Wipe Neighborhood"))
        {
            creator.WipeNeighborhood();
        }

        if (GUI.changed)
        {
            EditorUtility.SetDirty(creator);
        }
    }

    private void OnSceneGUI()
    {
        if (creator == null)
        {
            return;
        }

        Undo.RecordObject(creator, "Change Neighborhood Path");

        // Draw and handle the start point
        Handles.color = Color.green;
        Vector3 newPathStart = Handles.PositionHandle(creator.pathStart, Quaternion.identity);
        if (newPathStart != creator.pathStart)
        {
            creator.pathStart = newPathStart;
            EditorUtility.SetDirty(creator);
        }

        // Draw and handle the end point
        Handles.color = Color.red;
        Vector3 newPathEnd = Handles.PositionHandle(creator.pathEnd, Quaternion.identity);
        if (newPathEnd != creator.pathEnd)
        {
            creator.pathEnd = newPathEnd;
            EditorUtility.SetDirty(creator);
        }

        // Draw the path line
        Handles.color = Color.white;
        Handles.DrawLine(creator.pathStart, creator.pathEnd);

        // Handle A/D key presses to set points via raycast
        Event guiEvent = Event.current;
        if (guiEvent.type == EventType.KeyDown)
        {
            if (guiEvent.keyCode == KeyCode.A || guiEvent.keyCode == KeyCode.D)
            {
                // Cast a ray from the mouse position
                Ray ray = HandleUtility.GUIPointToWorldRay(guiEvent.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                {
                    if (hit.collider != null)
                    {
                        if (guiEvent.keyCode == KeyCode.A)
                        {
                            creator.pathStart = hit.point;
                        }
                        else
                        {
                            creator.pathEnd = hit.point;
                        }
                        EditorUtility.SetDirty(creator);
                        guiEvent.Use(); // Consume the event
                    }
                }
            }
        }
    }
}