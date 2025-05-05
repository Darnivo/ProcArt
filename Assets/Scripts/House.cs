using UnityEngine;
using UnityEditor; // Required for Editor-specific attributes
using System.Collections.Generic;
using System.Linq; // Required for LINQ operations

public class House : MonoBehaviour
{
    [HideInInspector] // Hide in inspector, set by NeighborhoodCreator
    public NeighborhoodCreator neighborhoodCreator;

    [Header("House Properties")]
    [HideInInspector] // Hide in inspector, set by NeighborhoodCreator
    public HouseType houseType;
    [HideInInspector] // Hide in inspector, set by NeighborhoodCreator
    public float houseLength; // Length along the path direction
    [HideInInspector] // Hide in inspector, set by NeighborhoodCreator
    public int houseWidth; // Width perpendicular to the path direction (2 or 4)
    // [HideInInspector] // Hide in inspector, set by NeighborhoodCreator
    public int houseHeight; // Height in prefab units (3-5)
    // [HideInInspector] // Hide in inspector, set by NeighborhoodCreator
    public int seed; // Deterministic random seed for this house

    // Prefab references (assigned by NeighborhoodCreator)
    // [HideInInspector] 
    public GameObject normalWallPrefab;
    [HideInInspector] public GameObject decoratedWallPrefab;
    [HideInInspector] public GameObject[] balconyWallPrefabs;
    [HideInInspector] public GameObject[] doorPrefabs;
    [HideInInspector] public GameObject[] windowPrefabs;
    [HideInInspector] public GameObject[] roundedCornerWallPrefabs;
    [HideInInspector] public GameObject[] straightCornerWallPrefabs;
    [HideInInspector] public GameObject[] sideRoofPrefabs;
    [HideInInspector] public GameObject roundedCornerRoofPrefab;
    [HideInInspector] public GameObject straightCornerRoofPrefab;
    [HideInInspector] public GameObject topRoofPrefab;
    [HideInInspector] public GameObject specialTwoUnitRoofPrefab;
    [HideInInspector] public GameObject[] sideRoofEdgePrefabs;
    [HideInInspector] public GameObject cornerEdgeWallPrefab;
    [HideInInspector] public GameObject middleEdgeWallPrefab;
    [HideInInspector] public GameObject topRoofEdgePrefab;


    // Internal state for deterministic randomization
    private System.Random random;

    // Chosen types for this house (for consistency per floor)
    private GameObject chosenCornerWallPrefab;
    private GameObject chosenCornerRoofPrefab;
    private GameObject chosenWindowPrefabType; // Represents a type like "double window"
    private GameObject chosenBalconyPrefabType; // Represents a type like "balcony type A"

    /// <summary>
    /// Generates the mesh for this individual house.
    /// </summary>
    public void GenerateHouse()
{
    WipeHouse();
    random = new System.Random(seed);

    bool useRoundedCorners = random.NextDouble() < 0.5;
    chosenCornerWallPrefab = useRoundedCorners ? GetRandomPrefab(roundedCornerWallPrefabs) : GetRandomPrefab(straightCornerWallPrefabs);
    chosenCornerRoofPrefab = useRoundedCorners ? roundedCornerRoofPrefab : straightCornerRoofPrefab;
    chosenWindowPrefabType = GetRandomPrefab(windowPrefabs);
    chosenBalconyPrefabType = GetRandomPrefab(balconyWallPrefabs);

    int lengthSegments = Mathf.RoundToInt(houseLength / 2f);
    int widthSegments = Mathf.RoundToInt(houseWidth / 2f);
    const float prefabSize = 2f;
    const float prefabHeight = 3f;

    Vector3 centerOffset = new Vector3(
        -(widthSegments * prefabSize) / 2f + prefabSize / 2f,
        0,
        -(lengthSegments * prefabSize) / 2f + prefabSize / 2f
    );

    List<Vector3> groundFloorDoorPositions = new List<Vector3>();
    int doorsToPlace = 1;

    for (int h = 0; h < houseHeight; h++)
    {
        GameObject currentFloorWindowPrefab = null;
        GameObject currentFloorBalconyPrefab = null;
        if (h < houseHeight - 1)
        {
            float prefabTypeRoll = (float)random.NextDouble();
            if (prefabTypeRoll < 0.6f) { /* Normal wall */ }
            else if (prefabTypeRoll < 0.8f && chosenWindowPrefabType != null)
                currentFloorWindowPrefab = chosenWindowPrefabType;
            else if (chosenBalconyPrefabType != null)
                currentFloorBalconyPrefab = chosenBalconyPrefabType;
        }

        for (int l = 0; l < lengthSegments; l++)
        {
            for (int w = 0; w < widthSegments; w++)
            {
                Vector3 localPosition = new Vector3(
                    w * prefabSize,
                    h * prefabHeight,
                    l * prefabSize
                ) + centerOffset;

                bool isCorner = (l == 0 || l == lengthSegments - 1) && (w == 0 || w == widthSegments - 1);
                bool isEdgeAlongLength = (w == 0 || w == widthSegments - 1) && !isCorner;
                bool isEdgeAlongWidth = (l == 0 || l == lengthSegments - 1) && !isCorner;
                bool isMiddle = !isCorner && !isEdgeAlongLength && !isEdgeAlongWidth;

                if (h < houseHeight - 1)
                {
                    GameObject prefabToInstantiate = null;
                    Quaternion rotation = Quaternion.identity;

                    // Handle first house special case for front edge
                    if (houseType == HouseType.First && l == 0 && isEdgeAlongLength)
                    {
                        // Leave front edge open for possible door placement
                        continue;
                    }

                    bool isPositionHandled = false;
                    // For mirroring last edge walls 

                    // CORNER LOGIC - Modified to use edge prefabs between houses
                    if (isCorner)
                    {
                        // Front face (l == 0)
                        if (l == 0)
                        {
                            if ((houseType == HouseType.Single) || 
                                (houseType == HouseType.First))
                            {
                                // Use actual corner for the front of first/single house
                                prefabToInstantiate = chosenCornerWallPrefab;
                                if (w == 0) rotation = Quaternion.Euler(0, 270, 0);
                                else if (w == widthSegments - 1) rotation = Quaternion.Euler(0, 90, 0);
                            }
                            else if (houseType == HouseType.Middle || houseType == HouseType.Last)
                            {
                                // For middle and last house, use edge prefabs at front corners
                                prefabToInstantiate = cornerEdgeWallPrefab;
                                if (w == 0) rotation = Quaternion.Euler(0, 0, 0);
                                else if (w == widthSegments - 1) rotation = Quaternion.Euler(0, 0, 0);
                            }
                        }
                        // Back face (l == lengthSegments - 1)
                        else if (l == lengthSegments - 1)
                        {
                            if ((houseType == HouseType.Single) || 
                                (houseType == HouseType.Last))
                            {
                                // Use actual corner for the back of last/single house
                                prefabToInstantiate = chosenCornerWallPrefab;
                                if (w == 0) rotation = Quaternion.Euler(0, 0, 0);
                                else if (w == widthSegments - 1) rotation = Quaternion.Euler(0, 0, 0);
                            }
                            else if (houseType == HouseType.Middle || houseType == HouseType.First)
                            {
                                // For middle and first house, use edge prefabs at back corners
                                prefabToInstantiate = cornerEdgeWallPrefab;
                                if (w == 0) rotation = Quaternion.Euler(0, 0, 0);
                                else if (w == widthSegments - 1) rotation = Quaternion.Euler(0, 0, 0); 

                                Vector3 customScale = new Vector3(1, 1, -1);
                                if (w == widthSegments - 1) customScale = new Vector3(-1, 1, -1);
                                InstantiatePrefab(prefabToInstantiate, localPosition, rotation, customScale);
                                isPositionHandled = true;
                            }
                        }
                    }
                    // EDGE LOGIC
                    else if (isEdgeAlongWidth)
                    {
                        // Front face edges (l == 0)
                        if (l == 0)
                        {
                            if (houseType == HouseType.Middle || houseType == HouseType.Last)
                            {
                                // Middle/Last house front edge
                                prefabToInstantiate = (widthSegments > 1 && w > 0 && w < widthSegments - 1) ? 
                                    middleEdgeWallPrefab : cornerEdgeWallPrefab;
                                rotation = Quaternion.Euler(0, 180, 0);
                            }
                        }
                        // Back face edges (l == lengthSegments - 1)
                        else if (l == lengthSegments - 1)
                        {
                            if (houseType == HouseType.Middle || houseType == HouseType.First)
                            {
                                // Middle/First house back edge
                                prefabToInstantiate = (widthSegments > 1 && w > 0 && w < widthSegments - 1) ? 
                                    middleEdgeWallPrefab : cornerEdgeWallPrefab;
                                rotation = Quaternion.Euler(0, 0, 0);
                            }
                        }
                    }
                    
                    // Handle regular wall placement if no special case was applied
                    if (prefabToInstantiate == null)
                    {
                        if (h == 0)
                        {
                            bool isEdge = (l == 0 || l == lengthSegments - 1 || w == 0 || w == widthSegments - 1);
                            if (doorsToPlace > 0 && isEdge && !isCorner)
                            {
                                if (random.NextDouble() < 0.3f) // Increased chance for first house
                                {
                                    prefabToInstantiate = GetRandomPrefab(doorPrefabs);
                                    doorsToPlace--;
                                    groundFloorDoorPositions.Add(localPosition);
                                }
                            }

                            if (prefabToInstantiate == null)
                            {
                                float groundFloorRoll = (float)random.NextDouble();
                                if (groundFloorRoll < 0.7f) prefabToInstantiate = normalWallPrefab;
                                else if (groundFloorRoll < 0.85f) prefabToInstantiate = GetRandomPrefab(windowPrefabs);
                                else if (groundFloorRoll < 0.95f) prefabToInstantiate = GetRandomPrefab(balconyWallPrefabs);
                                else prefabToInstantiate = decoratedWallPrefab;
                            }
                        }
                        else
                        {
                            if (currentFloorWindowPrefab != null) prefabToInstantiate = GetRandomPrefab(windowPrefabs);
                            else if (currentFloorBalconyPrefab != null) prefabToInstantiate = GetRandomPrefab(balconyWallPrefabs);
                            else prefabToInstantiate = random.NextDouble() < 0.9f ? normalWallPrefab : decoratedWallPrefab;
                        }
                        rotation = Quaternion.Euler(0, 0, 0);
                    }

                    if (!isPositionHandled && prefabToInstantiate != null )
                        InstantiatePrefab(prefabToInstantiate, localPosition, rotation);
                }
                else // Roof level
                {
                    GameObject roofPrefabToInstantiate = null;
                    Quaternion rotation = Quaternion.identity;
                    Vector3 customScale = Vector3.one; // Default scale

                    // CORNER ROOF LOGIC - Modified to use edge prefabs between houses
                    if (isCorner)
                    {
                        // Front face corners (l == 0)
                        if (l == 0)
                        {
                            if ((houseType == HouseType.Single) || 
                                (houseType == HouseType.First))
                            {
                                // Use actual corner for the front of first/single house
                                roofPrefabToInstantiate = chosenCornerRoofPrefab;
                                if (w == 0) rotation = Quaternion.Euler(0, 270, 0);
                                else if (w == widthSegments - 1) rotation = Quaternion.Euler(0, 180, 0);
                            }
                            else if (houseType == HouseType.Middle || houseType == HouseType.Last)
                            {
                                // For middle and last house, use edge prefabs at front corners
                                roofPrefabToInstantiate = GetRandomPrefab(sideRoofEdgePrefabs);
                                if (w == 0) rotation = Quaternion.Euler(0, 0, 0);
                                else if (w == widthSegments - 1) rotation = Quaternion.Euler(0, 180, 0);
                                if (w == widthSegments - 1) customScale = new Vector3(1, 1, -1);
                            }
                        }
                        // Back face corners (l == lengthSegments - 1)
                        else if (l == lengthSegments - 1)
                        {
                            if ((houseType == HouseType.Single) || 
                                (houseType == HouseType.Last))
                            {
                                // Use actual corner for the back of last/single house
                                roofPrefabToInstantiate = chosenCornerRoofPrefab;
                                if (w == 0) rotation = Quaternion.Euler(0, 0, 0);
                                else if (w == widthSegments - 1) rotation = Quaternion.Euler(0, 90, 0);
                            }
                            else if (houseType == HouseType.Middle || houseType == HouseType.First)
                            {
                                // For middle and first house, use edge prefabs at back corners
                                roofPrefabToInstantiate = GetRandomPrefab(sideRoofEdgePrefabs);
                                if (w == 0) rotation = Quaternion.Euler(0, 0, 0);
                                else if (w == widthSegments - 1) rotation = Quaternion.Euler(0, 0, 0);
                                // Apply z-axis mirroring for last column edges
                                customScale = new Vector3(1, 1, -1);
                                if (w == widthSegments - 1) customScale = new Vector3(-1, 1, -1);
                            }
                        }
                    }
                    // EDGE ROOF LOGIC
                    else if (isEdgeAlongWidth)
                    {
                        // Front face edges (l == 0)
                        if (l == 0)
                        {
                            if (houseType == HouseType.Middle || houseType == HouseType.Last)
                            {
                                // Middle/Last house front edge
                                roofPrefabToInstantiate = (widthSegments > 1 && w > 0 && w < widthSegments - 1) ? 
                                    topRoofEdgePrefab : GetRandomPrefab(sideRoofEdgePrefabs);
                                rotation = Quaternion.Euler(0, 180, 0);
                            }
                            else
                            {
                                roofPrefabToInstantiate = GetRandomPrefab(sideRoofPrefabs);
                                rotation = Quaternion.Euler(0, 180, 0);
                            }
                        }
                        // Back face edges (l == lengthSegments - 1)
                        else if (l == lengthSegments - 1)
                        {
                            if (houseType == HouseType.Middle || houseType == HouseType.First)
                            {
                                // Middle/First house back edge
                                roofPrefabToInstantiate = (widthSegments > 1 && w > 0 && w < widthSegments - 1) ? 
                                    topRoofEdgePrefab : GetRandomPrefab(sideRoofEdgePrefabs);
                                rotation = Quaternion.Euler(0, 0, 0);
                                // Apply z-axis mirroring for last column edges
                                if (w == widthSegments - 1) customScale = new Vector3(-1, 1, -1);
                            }
                            else
                            {
                                roofPrefabToInstantiate = GetRandomPrefab(sideRoofPrefabs);
                                rotation = Quaternion.Euler(0, 0, 0);
                            }
                        }
                    }
                    else if (isEdgeAlongLength)
                    {
                        roofPrefabToInstantiate = GetRandomPrefab(sideRoofPrefabs);
                        if (w == 0) rotation = Quaternion.Euler(0, 0, 0);
                        else if (w == widthSegments - 1) rotation = Quaternion.Euler(0, 180, 0);
                    }
                    else if (isMiddle && specialTwoUnitRoofPrefab != null)
                    {
                        if (widthSegments > 1 && w % 2 == 0)
                        {
                            roofPrefabToInstantiate = specialTwoUnitRoofPrefab;
                            if (l == 0) rotation = Quaternion.Euler(0, 180, 0);
                            else if (l == lengthSegments - 1) rotation = Quaternion.Euler(0, 0, 0);
                        }
                    }

                    if (roofPrefabToInstantiate != null)
                        InstantiatePrefab(roofPrefabToInstantiate, localPosition, rotation, customScale);
                }
            }
        }
    }

    if (doorsToPlace > 0 && houseHeight > 0)
    {
        List<Vector3> potentialDoorPositions = new List<Vector3>();
        int lengthSegs = Mathf.RoundToInt(houseLength / 2f);
        int widthSegs = Mathf.RoundToInt(houseWidth / 2f);
        Vector3 offset = new Vector3(
            -(widthSegs * prefabSize) / 2f + prefabSize / 2f,
            0,
            -(lengthSegs * prefabSize) / 2f + prefabSize / 2f
        );

        // Special handling for first house front edge
        if (houseType == HouseType.First)
        {
            for (int w = 1; w < widthSegs - 1; w++) // Skip corners
            {
                Vector3 pos = new Vector3(w * prefabSize, 0, 0) + offset;
                potentialDoorPositions.Add(pos);
            }
        }

        // General edge collection
        for (int l = 0; l < lengthSegs; l++)
        {
            for (int w = 0; w < widthSegs; w++)
            {
                bool isEdge = (l == 0 || l == lengthSegs - 1 || w == 0 || w == widthSegs - 1);
                bool isCorner = (l == 0 || l == lengthSegs - 1) && (w == 0 || w == widthSegs - 1);
                if (isEdge && !isCorner)
                {
                    Vector3 pos = new Vector3(w * prefabSize, 0, l * prefabSize) + offset;
                    potentialDoorPositions.Add(pos);
                }
            }
        }

        // Remove occupied positions
        List<Vector3> availablePositions = new List<Vector3>();
        foreach (Vector3 pos in potentialDoorPositions)
        {
            bool occupied = false;
            foreach (Transform child in transform)
            {
                if (Vector3.Distance(child.localPosition, pos) < 0.1f)
                {
                    occupied = true;
                    break;
                }
            }
            if (!occupied) availablePositions.Add(pos);
        }

        // Fallback to replacing walls if needed
        if (availablePositions.Count == 0)
        {
            foreach (Vector3 pos in potentialDoorPositions)
            {
                // Find and destroy existing wall at this position
                foreach (Transform child in transform)
                {
                    if (Vector3.Distance(child.localPosition, pos) < 0.1f)
                    {
                        DestroyImmediate(child.gameObject);
                        availablePositions.Add(pos);
                        break;
                    }
                }
            }
        }

        if (availablePositions.Count > 0)
        {
            Vector3 doorPosition = availablePositions[random.Next(availablePositions.Count)];
            Quaternion rotation = Quaternion.identity;

            bool isFrontBackEdge = (Mathf.Abs(doorPosition.z - offset.z) < 0.1f) ||
                                (Mathf.Abs(doorPosition.z - (offset.z + (lengthSegs - 1) * prefabSize)) < 0.1f);
            bool isLeftRightEdge = (Mathf.Abs(doorPosition.x - offset.x) < 0.1f) ||
                                (Mathf.Abs(doorPosition.x - (offset.x + (widthSegs - 1) * prefabSize)) < 0.1f);

            if (isFrontBackEdge) rotation = Quaternion.Euler(0, 0, 0);
            else if (isLeftRightEdge) rotation = Quaternion.Euler(0, 90, 0);

            InstantiatePrefab(GetRandomPrefab(doorPrefabs), doorPosition, rotation);
            Debug.LogWarning($"Forced door on {gameObject.name} at {doorPosition}");
        }
        else
        {
            Debug.LogError($"Failed to place door on {gameObject.name}");
        }
    }

    Debug.Log($"Generated House: {gameObject.name}, Type: {houseType}, Dimensions: {houseLength}x{houseWidth}x{houseHeight}");
}
    /// <summary>
    /// Wipes the mesh objects for this individual house.
    /// </summary>
    public void WipeHouse()
    {
        // Destroy all child objects
        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in transform)
        {
            childrenToDestroy.Add(child.gameObject);
        }

        foreach (GameObject child in childrenToDestroy)
        {
            // Use DestroyImmediate in Editor scripts for immediate cleanup
            if (Application.isEditor)
            {
                DestroyImmediate(child);
            }
            else
            {
                Destroy(child);
            }
        }
        Debug.Log($"Wiped House: {gameObject.name}");
    }

    /// <summary>
/// Instantiates a prefab and sets its parent to this house's transform.
/// Handles mirroring for the opposite side.
/// </summary>
private void InstantiatePrefab(GameObject prefab, Vector3 localPosition, Quaternion localRotation, Vector3? customScale = null)
{
    if (prefab == null)
    {
        Debug.LogWarning("Attempted to instantiate a null prefab.");
        return;
    }

    // Instantiate the prefab
    GameObject instance = Instantiate(prefab, transform);
    instance.transform.localPosition = localPosition;
    instance.transform.localRotation = localRotation;

    // Check if this prefab is on the "other side" of the house (relative to the path direction)
    int widthSegments = Mathf.RoundToInt(houseWidth / 2f);
    const float prefabSize = 2f;

    // Calculate the local X position relative to the house's left edge (before centering)
    float localXRelativeToLeftEdge = localPosition.x - ((-(widthSegments * prefabSize) / 2f + prefabSize / 2f));
    // Calculate the width segment index
    int wIndex = Mathf.RoundToInt(localXRelativeToLeftEdge / prefabSize);

    // If custom scale is provided, apply it
    if (customScale.HasValue)
    {
        instance.transform.localScale = customScale.Value;
    }
    // Otherwise apply standard mirroring if needed
    else if (houseWidth == 4 && wIndex == 1)
    {
        // Mirror the prefab by scaling the local X axis by -1
        instance.transform.localScale = new Vector3(-1, 1, 1);
    }
    else // For width 2 or other cases, use the default scale
    {
        instance.transform.localScale = new Vector3(1, 1, 1);
    }
}

    /// <summary>
    /// Gets a random prefab from an array, handling null or empty arrays.
    /// </summary>
    private GameObject GetRandomPrefab(GameObject[] prefabs)
    {
        if (prefabs == null || prefabs.Length == 0)
        {
            return null;
        }
        return prefabs[random.Next(prefabs.Length)];
    }

    /// <summary>
    /// Gets a random variant of a specific prefab type from an array.
    /// Assumes prefabs array contains all variants of all types.
    /// This method is less useful with the new structure where we choose a type first.
    /// Keeping it for potential future use or if the prefab structure changes.
    /// </summary>
    private GameObject GetRandomPrefabVariant(GameObject[] prefabsOfType)
    {
        if (prefabsOfType == null || prefabsOfType.Length == 0)
        {
            return null;
        }
        return prefabsOfType[random.Next(prefabsOfType.Length)];
    }
}
