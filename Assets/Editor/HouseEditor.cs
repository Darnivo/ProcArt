using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(House))]
public class HouseEditor : Editor
{
    private House house;

    private void OnEnable()
    {
        house = (House)target;
         // Ensure the target is not null when the editor is enabled
        if (house == null)
        {
            Debug.LogError("House target is null in OnEnable.");
            return;
        }
    }

    public override void OnInspectorGUI()
    {
        // Draw the default inspector fields (those not hidden with [HideInInspector])
        DrawDefaultInspector();

        // Add the Generate and Wipe buttons
        GUILayout.Space(10);
        if (GUILayout.Button("Generate House"))
        {
            house.GenerateHouse();
        }

        if (GUILayout.Button("Wipe House"))
        {
            house.WipeHouse();
        }

        // Apply changes if any were made in the inspector
        if (GUI.changed)
        {
            EditorUtility.SetDirty(house);
        }
    }
}
