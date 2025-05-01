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
    [HideInInspector] // Hide in inspector, set by NeighborhoodCreator
    public int houseHeight; // Height in prefab units (3-5)
    [HideInInspector] // Hide in inspector, set by NeighborhoodCreator
    public int seed; // Deterministic random seed for this house

    // Prefab references (assigned by NeighborhoodCreator)
    [HideInInspector] public GameObject normalWallPrefab;
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
        // Wipe existing mesh objects before generating
        WipeHouse();

        // Initialize the deterministic random number generator
        random = new System.Random(seed);

        // Determine corner type (rounded or straight)
        bool useRoundedCorners = random.NextDouble() < 0.5;
        chosenCornerWallPrefab = useRoundedCorners ? GetRandomPrefab(roundedCornerWallPrefabs) : GetRandomPrefab(straightCornerWallPrefabs);
        chosenCornerRoofPrefab = useRoundedCorners ? roundedCornerRoofPrefab : straightCornerRoofPrefab;


        // Determine the types of windows and balconies to use for this house
        chosenWindowPrefabType = GetRandomPrefab(windowPrefabs); // Choose one window type for the whole house
        chosenBalconyPrefabType = GetRandomPrefab(balconyWallPrefabs); // Choose one balcony type for the whole house


        // Calculate grid dimensions (in 2x2 prefab units)
        int lengthSegments = Mathf.RoundToInt(houseLength / 2f);
        int widthSegments = Mathf.RoundToInt(houseWidth / 2f);

        // Prefab size is 2x2x3
        const float prefabSize = 2f; // Horizontal size (x and z)
        const float prefabHeight = 3f; // Vertical size (y)

        // Offset to center the house around its transform position
        Vector3 centerOffset = new Vector3(
            -(widthSegments * prefabSize) / 2f + prefabSize / 2f,
            0,
            -(lengthSegments * prefabSize) / 2f + prefabSize / 2f
        );

        // Store positions where a door has been placed on the ground floor
        List<Vector3> groundFloorDoorPositions = new List<Vector3>();
        int doorsToPlace = 1; // Ensure at least one door

        // Iterate through the grid and place prefabs
        for (int h = 0; h < houseHeight; h++) // Height levels (0 to houseHeight - 1)
        {
            // Determine the type of window/balcony to use for this floor (if applicable)
            GameObject currentFloorWindowPrefab = null;
            GameObject currentFloorBalconyPrefab = null;
            if (h < houseHeight - 1) // Not the roof floor
            {
                 // Randomly choose between normal, window, or balcony for this floor's main prefab type
                float prefabTypeRoll = (float)random.NextDouble();
                if (prefabTypeRoll < 0.6f) // 60% chance of normal wall
                {
                    // Use normal wall
                }
                else if (prefabTypeRoll < 0.8f && chosenWindowPrefabType != null) // 20% chance of window (if available)
                {
                    currentFloorWindowPrefab = chosenWindowPrefabType;
                }
                else if (chosenBalconyPrefabType != null) // Remaining chance of balcony (if available)
                {
                     currentFloorBalconyPrefab = chosenBalconyPrefabType;
                }
            }


            for (int l = 0; l < lengthSegments; l++) // Length segments along the path (Z)
            {
                for (int w = 0; w < widthSegments; w++) // Width segments perpendicular to the path (X)
                {
                    // Calculate the local position for the prefab
                    Vector3 localPosition = new Vector3(
                        w * prefabSize,
                        h * prefabHeight,
                        l * prefabSize
                    ) + centerOffset;

                    // Determine if this is a corner, edge, or middle piece
                    bool isCorner = (l == 0 || l == lengthSegments - 1) && (w == 0 || w == widthSegments - 1);
                    bool isEdgeAlongLength = (w == 0 || w == widthSegments - 1) && !isCorner;
                    bool isEdgeAlongWidth = (l == 0 || l == lengthSegments - 1) && !isCorner;
                    bool isMiddle = !isCorner && !isEdgeAlongLength && !isEdgeAlongWidth;


                    // --- Place Walls (Ground and Upper Floors) ---
                    if (h < houseHeight - 1) // Not the roof level
                    {
                        GameObject prefabToInstantiate = null;
                        Quaternion rotation = Quaternion.identity;

                        // Handle corners based on house type and position
                        if (isCorner)
                        {
                             // Corners only appear on the first and last house (or single)
                            if (houseType == HouseType.Single || houseType == HouseType.First || houseType == HouseType.Last)
                            {
                                prefabToInstantiate = chosenCornerWallPrefab;

                                // Determine corner rotation based on position and path direction
                                // Corner prefabs point to +z, -x
                                if (l == 0 && w == 0) rotation = Quaternion.Euler(0, 90, 0); // Front-Left
                                else if (l == 0 && w == widthSegments - 1) rotation = Quaternion.Euler(0, 180, 0); // Front-Right
                                else if (l == lengthSegments - 1 && w == 0) rotation = Quaternion.Euler(0, 0, 0); // Back-Left
                                else if (l == lengthSegments - 1 && w == widthSegments - 1) rotation = Quaternion.Euler(0, -90, 0); // Back-Right
                            }
                        }
                        // Handle edges that meet neighbors (barrier in -z direction)
                        else if ((houseType == HouseType.Middle || houseType == HouseType.Last) && l == 0 && isEdgeAlongLength) // Front edge of middle/last house
                        {
                             // This side meets the previous house, use edge walls
                             prefabToInstantiate = (widthSegments > 1 && w > 0 && w < widthSegments - 1) ? middleEdgeWallPrefab : cornerEdgeWallPrefab;
                             rotation = Quaternion.Euler(0, 180, 0); // Rotate to face the correct direction (barrier in -z)
                        }
                         else if ((houseType == HouseType.Middle || houseType == HouseType.First) && l == lengthSegments - 1 && isEdgeAlongLength) // Back edge of middle/first house
                        {
                             // This side meets the next house, use edge walls
                            prefabToInstantiate = (widthSegments > 1 && w > 0 && w < widthSegments - 1) ? middleEdgeWallPrefab : cornerEdgeWallPrefab;
                            rotation = Quaternion.Euler(0, 0, 0); // Rotate to face the correct direction (barrier in -z)
                         }
                        // Handle normal walls and other types
                        else
                        {
                            // Normal walls, windows, balconies, decorations
                            if (h == 0) // Ground floor
                            {
                                // Try to place a door
                                if (doorsToPlace > 0 && (l == 0 || l == lengthSegments - 1) && (w == 0 || w == widthSegments - 1) == false) // Only on non-corner sides
                                {
                                    // Random chance to place a door
                                    if (random.NextDouble() < 0.2f) // 20% chance for a door on an available spot
                                    {
                                        prefabToInstantiate = GetRandomPrefab(doorPrefabs);
                                        doorsToPlace--; // Decrement the count of doors to place
                                        groundFloorDoorPositions.Add(localPosition); // Record door position
                                    }
                                }

                                // If no door was placed, choose another ground floor prefab
                                if (prefabToInstantiate == null)
                                {
                                    float groundFloorRoll = (float)random.NextDouble();
                                    if (groundFloorRoll < 0.7f) // 70% chance of normal wall
                                    {
                                        prefabToInstantiate = normalWallPrefab;
                                    }
                                    else if (groundFloorRoll < 0.85f && chosenWindowPrefabType != null) // 15% chance of window
                                    {
                                        prefabToInstantiate = GetRandomPrefab(windowPrefabs); // Use chosen type, random variant
                                    }
                                    else if (groundFloorRoll < 0.95f && chosenBalconyPrefabType != null) // 10% chance of balcony
                                    {
                                        prefabToInstantiate = GetRandomPrefab(balconyWallPrefabs); // Use chosen type, random variant
                                    }
                                    else // 5% chance of decorated wall
                                    {
                                        prefabToInstantiate = decoratedWallPrefab;
                                    }
                                }
                            }
                            else // Upper floors
                            {
                                // Use the chosen type for this floor (window, balcony, or normal)
                                if (currentFloorWindowPrefab != null)
                                {
                                     prefabToInstantiate = GetRandomPrefab(windowPrefabs); // Use chosen type, random variant
                                }
                                else if (currentFloorBalconyPrefab != null)
                                {
                                     prefabToInstantiate = GetRandomPrefab(balconyWallPrefabs); // Use chosen type, random variant
                                }
                                else
                                {
                                     // Randomly choose between normal and decorated for upper floors if no specific type is chosen
                                     prefabToInstantiate = random.NextDouble() < 0.9f ? normalWallPrefab : decoratedWallPrefab; // 90% normal, 10% decorated
                                }
                            }

                            // Determine rotation for non-corner/edge walls (they point to -x)
                             if (w == 0) rotation = Quaternion.Euler(0, 90, 0); // Left side
                             else if (w == widthSegments - 1) rotation = Quaternion.Euler(0, -90, 0); // Right side
                             else if (l == 0) rotation = Quaternion.Euler(0, 180, 0); // Front side
                             else if (l == lengthSegments - 1) rotation = Quaternion.Euler(0, 0, 0); // Back side

                        }

                        // Instantiate the wall prefab
                        if (prefabToInstantiate != null)
                        {
                            InstantiatePrefab(prefabToInstantiate, localPosition, rotation);
                        }
                    }
                    // --- Place Roofs ---
                    else // This is the roof level (h == houseHeight - 1)
                    {
                         GameObject roofPrefabToInstantiate = null;
                         Quaternion rotation = Quaternion.identity;

                         // Handle corner roofs
                         if (isCorner)
                         {
                             if (houseType == HouseType.Single || houseType == HouseType.First || houseType == HouseType.Last)
                             {
                                 roofPrefabToInstantiate = chosenCornerRoofPrefab;

                                 // Determine corner rotation based on position and path direction
                                 // Corner roof prefabs point to +z, -x (same as corner walls)
                                 if (l == 0 && w == 0) rotation = Quaternion.Euler(0, 90, 0); // Front-Left
                                 else if (l == 0 && w == widthSegments - 1) rotation = Quaternion.Euler(0, 180, 0); // Front-Right
                                 else if (l == lengthSegments - 1 && w == 0) rotation = Quaternion.Euler(0, 0, 0); // Back-Left
                                 else if (l == lengthSegments - 1 && w == widthSegments - 1) rotation = Quaternion.Euler(0, -90, 0); // Back-Right
                             }
                         }
                         // Handle edge roofs that meet neighbors (barrier in -z direction)
                         else if ((houseType == HouseType.Middle || houseType == HouseType.Last) && l == 0 && isEdgeAlongLength) // Front edge of middle/last house
                         {
                              // This side meets the previous house, use edge roofs
                              roofPrefabToInstantiate = (widthSegments > 1 && w > 0 && w < widthSegments - 1) ? topRoofEdgePrefab : GetRandomPrefab(sideRoofEdgePrefabs);
                              rotation = Quaternion.Euler(0, 180, 0); // Rotate to face the correct direction (barrier in -z)
                         }
                          else if ((houseType == HouseType.Middle || houseType == HouseType.First) && l == lengthSegments - 1 && isEdgeAlongLength) // Back edge of middle/first house
                         {
                              // This side meets the next house, use edge roofs
                             roofPrefabToInstantiate = (widthSegments > 1 && w > 0 && w < widthSegments - 1) ? topRoofEdgePrefab : GetRandomPrefab(sideRoofEdgePrefabs);
                             rotation = Quaternion.Euler(0, 0, 0); // Rotate to face the correct direction (barrier in -z)
                          }
                         // Handle normal side roofs and top roofs
                         else
                         {
                             // Side roofs along the length (Z)
                             if (isEdgeAlongLength)
                             {
                                 roofPrefabToInstantiate = GetRandomPrefab(sideRoofPrefabs);
                                 // Determine rotation for side roofs (they point to -x)
                                 if (w == 0) rotation = Quaternion.Euler(0, 90, 0); // Left side
                                 else if (w == widthSegments - 1) rotation = Quaternion.Euler(0, -90, 0); // Right side
                             }
                             // Top roofs for width > 2 along the width (X)
                             else if (widthSegments > 1 && isEdgeAlongWidth)
                             {
                                 roofPrefabToInstantiate = topRoofPrefab;
                                 // Determine rotation for top roofs (they point to -x, but need to be rotated 90 around Y to cover width)
                                 if (l == 0) rotation = Quaternion.Euler(0, 180, 0); // Front side
                                 else if (l == lengthSegments - 1) rotation = Quaternion.Euler(0, 0, 0); // Back side
                             }
                              // Special 2-unit wide roof component
                             else if (isMiddle && specialTwoUnitRoofPrefab != null)
                             {
                                 // This prefab is 2 units wide, so it occupies two width segments.
                                 // Only place it for the first of the two segments it covers.
                                 if (widthSegments > 1 && w % 2 == 0)
                                 {
                                     roofPrefabToInstantiate = specialTwoUnitRoofPrefab;
                                      // Rotation depends on which side of the house it's on (front/back)
                                     if (l == 0) rotation = Quaternion.Euler(0, 180, 0); // Front side
                                     else if (l == lengthSegments - 1) rotation = Quaternion.Euler(0, 0, 0); // Back side
                                 }
                             }
                         }

                         // Instantiate the roof prefab
                         if (roofPrefabToInstantiate != null)
                         {
                             InstantiatePrefab(roofPrefabToInstantiate, localPosition, rotation);
                         }
                    }
                }
            }
        }

        // Ensure at least one door was placed on the ground floor. If not, force one.
        if (doorsToPlace > 0 && houseHeight > 0)
        {
            // Find a valid position on the ground floor that isn't a corner
            List<Vector3> potentialDoorPositions = new List<Vector3>();
            int lengthSegs = Mathf.RoundToInt(houseLength / 2f);
            int widthSegs = Mathf.RoundToInt(houseWidth / 2f);
             Vector3 offset = new Vector3(
                -(widthSegs * prefabSize) / 2f + prefabSize / 2f,
                0, // Ground floor
                -(lengthSegs * prefabSize) / 2f + prefabSize / 2f
            );

            for (int l = 0; l < lengthSegs; l++)
            {
                for (int w = 0; w < widthSegs; w++)
                {
                     bool isCorner = (l == 0 || l == lengthSegs - 1) && (w == 0 || w == widthSegs - 1);
                     if (!isCorner)
                     {
                         Vector3 pos = new Vector3(w * prefabSize, 0, l * prefabSize) + offset;
                         // Check if a prefab already exists at this position (might be an edge wall)
                         // This is a simplified check; a more robust method would involve checking for existing colliders or tags.
                         bool positionOccupied = false;
                         foreach(Transform child in transform)
                         {
                             if (Vector3.Distance(child.localPosition, pos) < 0.1f)
                             {
                                 positionOccupied = true;
                                 break;
                             }
                         }
                         if (!positionOccupied)
                         {
                             potentialDoorPositions.Add(pos);
                         }
                     }
                }
            }

            if (potentialDoorPositions.Count > 0)
            {
                Vector3 doorPosition = potentialDoorPositions[random.Next(potentialDoorPositions.Count)];
                 // Determine rotation for the forced door
                 Quaternion rotation = Quaternion.identity;
                 // Assuming door is placed on a non-corner side
                 // Need to figure out which side based on position relative to centerOffset
                 float epsilon = 0.1f;
                 if (Mathf.Abs(doorPosition.x - offset.x) < epsilon) rotation = Quaternion.Euler(0, 90, 0); // Left side
                 else if (Mathf.Abs(doorPosition.x - (offset.x + (widthSegs - 1) * prefabSize)) < epsilon) rotation = Quaternion.Euler(0, -90, 0); // Right side
                 else if (Mathf.Abs(doorPosition.z - offset.z) < epsilon) rotation = Quaternion.Euler(0, 180, 0); // Front side
                 else if (Mathf.Abs(doorPosition.z - (offset.z + (lengthSegs - 1) * prefabSize)) < epsilon) rotation = Quaternion.Euler(0, 0, 0); // Back side

                InstantiatePrefab(GetRandomPrefab(doorPrefabs), doorPosition, rotation);
                 Debug.LogWarning($"Forced a door on House {gameObject.name} at position {doorPosition}.");
            }
            else
            {
                 Debug.LogError($"Could not find a valid position to place a door on House {gameObject.name}.");
            }
        }


        Debug.Log($"Generated House: {gameObject.name}, Type: {houseType}, Length: {houseLength}, Width: {houseWidth}, Height: {houseHeight}");
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
    private void InstantiatePrefab(GameObject prefab, Vector3 localPosition, Quaternion localRotation)
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
        // Assuming the path is generally along Z and width is along X
        // The "other side" is the side with positive X local position relative to the house center.
         // Prefabs are centered at their base, so localPosition.x determines the side.
         // The center line is at local x = (widthSegments * prefabSize) / 2f - prefabSize / 2f
         // If the prefab's localPosition.x is on the right half of the house's width, mirror it.
         // The width of the house spans from 0 to (widthSegments - 1) * prefabSize in local X before centering.
         // After centering, it spans from -(widthSegments * prefabSize)/2 + prefabSize/2 to (widthSegments * prefabSize)/2 - prefabSize/2
         // The mirroring point is the local X center of the house: (widthSegments * prefabSize) / 2f - prefabSize / 2f
         // Let's simplify: the first half of width segments (0 to widthSegments/2 - 1) are on one side, the rest are on the other.
         int widthSegments = Mathf.RoundToInt(houseWidth / 2f);
         const float prefabSize = 2f;

         // Calculate the local X position relative to the house's left edge (before centering)
         float localXRelativeToLeftEdge = localPosition.x - ((-(widthSegments * prefabSize) / 2f + prefabSize / 2f));
         // Calculate the width segment index
         int wIndex = Mathf.RoundToInt(localXRelativeToLeftEdge / prefabSize);

         // If the width is 4 (widthSegments = 2), segments are 0 and 1.
         // Segment 0 is on the left side, Segment 1 is on the right. Mirror Segment 1.
         // If the width is 2 (widthSegments = 1), segment is 0. No mirroring needed for width.
         if (houseWidth == 4 && wIndex == 1)
         {
              // Mirror the prefab by scaling the local X axis by -1
            //   instance.transform.localScale = new Vector3(-1, 1, 1);
         }
          else if (houseWidth == 2 && wIndex == 0)
          {
              // No mirroring needed for a width 2 house
            //    instance.transform.localScale = new Vector3(1, 1, 1);
          }
           else
           {
               // Default scale for other cases (like corners that might not fit the simple mirroring logic)
            //    instance.transform.localScale = new Vector3(1, 1, 1);
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
