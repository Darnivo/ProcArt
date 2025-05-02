using UnityEngine;
using UnityEditor; // Required for Editor scripts
using System.Collections.Generic;

/// <summary>
/// Custom Editor for the RingHouse component.
/// Provides handles to move corner points in the Scene view and buttons for generation/wiping.
/// </summary>
[CustomEditor(typeof(RingHouse))]
public class RingHouseEditor : Editor
{
    private RingHouse ringHouseTarget; // The instance of RingHouse being inspected
    private Tool lastTool = Tool.None; // To store the previous tool selected

    private void OnEnable()
    {
        // Store the target component
        ringHouseTarget = (RingHouse)target;

        // Hide the default transform tools when this object is selected
        lastTool = Tools.current;
        Tools.current = Tool.None;
    }

    private void OnDisable()
    {
        // Restore the previously selected tool
        Tools.current = lastTool;
    }

    /// <summary>
    /// Draws the custom inspector GUI elements.
    /// </summary>
    public override void OnInspectorGUI()
    {
        // Draw the default inspector fields (prefabs, settings)
        DrawDefaultInspector();

        // Add spacing
        EditorGUILayout.Space(10);

        // Add Generate Button
        if (GUILayout.Button("Generate Ring House", GUILayout.Height(30)))
        {
            // Register an undo state before generating
            Undo.RecordObject(ringHouseTarget, "Generate Ring House");
            // Record undo for any child objects that will be destroyed
            foreach (Transform child in ringHouseTarget.transform)
            {
                Undo.DestroyObjectImmediate(child.gameObject);
            }

            ringHouseTarget.GenerateRingHouse();

            // Mark the scene as dirty to ensure changes are saved
            if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(ringHouseTarget.gameObject.scene);
            }
        }

        // Add Wipe Button
        if (GUILayout.Button("Wipe Ring House", GUILayout.Height(30)))
        {
             // Register undo for the child objects that will be destroyed
            foreach (Transform child in ringHouseTarget.transform)
            {
                Undo.DestroyObjectImmediate(child.gameObject);
            }
            // No need to record the RingHouse component itself for wiping, just its children
            ringHouseTarget.WipeRingHouse();

            // Mark the scene as dirty
             if (!Application.isPlaying)
            {
                UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(ringHouseTarget.gameObject.scene);
            }
        }
    }

    /// <summary>
    /// Draws handles in the Scene view for manipulating corner points.
    /// </summary>
    private void OnSceneGUI()
    {
        if (ringHouseTarget == null || ringHouseTarget.cornerPoints == null) return;

        // Ensure list has 4 points, initialize if needed (though should be handled by RingHouse)
        if (ringHouseTarget.cornerPoints.Count != 4)
        {
             // Optionally reset or log error
             // return;
             // For safety, let's just ensure it's initialized if empty
             if(ringHouseTarget.cornerPoints.Count == 0) {
                 ringHouseTarget.cornerPoints = new List<Vector3> {
                    new Vector3(-10, 0, 10), new Vector3(10, 0, 10),
                    new Vector3(10, 0, -10), new Vector3(-10, 0, -10)
                 };
             } else {
                 // If count is wrong but not 0, it's an invalid state
                 Handles.Label(ringHouseTarget.transform.position + Vector3.up * 2, "RingHouse requires exactly 4 points!", EditorStyles.boldLabel);
                 return;
             }
        }

        // --- Handle Manipulation ---
        Transform handleTransform = ringHouseTarget.transform; // Use object's transform for handle space
        Quaternion handleRotation = Tools.pivotRotation == PivotRotation.Local ? handleTransform.rotation : Quaternion.identity; // Use world or local rotation

        // Store current points to check for changes
        List<Vector3> currentPoints = new List<Vector3>(ringHouseTarget.cornerPoints);
        bool changed = false;

        Handles.color = Color.cyan; // Set color for handles
        for (int i = 0; i < ringHouseTarget.cornerPoints.Count; i++)
        {
            // Convert local point to world space for handle display
            Vector3 worldPoint = handleTransform.TransformPoint(ringHouseTarget.cornerPoints[i]);

            EditorGUI.BeginChangeCheck(); // Start checking for changes on this handle

            // Draw a position handle for the current point
            Vector3 newWorldPoint = Handles.PositionHandle(worldPoint, handleRotation);

            // Add a label to the handle
            Handles.Label(worldPoint + Vector3.up * 0.5f, $"P{i}");

            if (EditorGUI.EndChangeCheck()) // If the handle was moved
            {
                Undo.RecordObject(ringHouseTarget, "Move RingHouse Point"); // Record state for Undo

                // Convert the new world position back to local space and update the list
                ringHouseTarget.cornerPoints[i] = handleTransform.InverseTransformPoint(newWorldPoint);
                changed = true;
            }
        }

        // --- Draw Lines Between Points ---
        Handles.color = Color.yellow;
        for (int i = 0; i < ringHouseTarget.cornerPoints.Count; i++)
        {
            Vector3 p1 = handleTransform.TransformPoint(ringHouseTarget.cornerPoints[i]);
            Vector3 p2 = handleTransform.TransformPoint(ringHouseTarget.cornerPoints[(i + 1) % ringHouseTarget.cornerPoints.Count]);
            Handles.DrawLine(p1, p2);

             // Draw side length label
             Handles.Label((p1 + p2) / 2f + Vector3.up * 0.2f, Vector3.Distance(p1, p2).ToString("F1"));
        }

         // Highlight the gap side in red
        if (ringHouseTarget.gapSideIndex >= 0 && ringHouseTarget.gapSideIndex < ringHouseTarget.cornerPoints.Count)
        {
             Handles.color = Color.red;
             Vector3 p1 = handleTransform.TransformPoint(ringHouseTarget.cornerPoints[ringHouseTarget.gapSideIndex]);
             Vector3 p2 = handleTransform.TransformPoint(ringHouseTarget.cornerPoints[(ringHouseTarget.gapSideIndex + 1) % ringHouseTarget.cornerPoints.Count]);
             Handles.DrawLine(p1, p2);
             Handles.Label((p1 + p2) / 2f + Vector3.down * 0.2f, "GAP");
        }


        // If points were changed, mark the object as dirty so changes are saved
        if (changed)
        {
            EditorUtility.SetDirty(ringHouseTarget);
             // Optional: Automatically regenerate if desired, but can be slow.
             // ringHouseTarget.GenerateRingHouse();
        }
    }
}
