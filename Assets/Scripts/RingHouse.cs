using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class RingHouse : MonoBehaviour
{
    [Header("Shape Definition")]
    [Tooltip("The four corner points defining the ring shape in local space. Must have exactly 4 points.")]
    public List<Vector3> cornerPoints = new List<Vector3> {
        new Vector3(-10, 0, 10),
        new Vector3(10, 0, 10),
        new Vector3(10, 0, -10),
        new Vector3(-10, 0, -10)
    };

    [Header("Generation Settings")]
    [Tooltip("The minimum height of the house in prefab units (2-5).")]
    [Range(2, 5)]
    public int minHouseHeight = 2;
    [Tooltip("The maximum height of the house in prefab units (2-5).")]
    [Range(2, 5)]
    public int maxHouseHeight = 5;
    [Tooltip("Random seed for deterministic generation.")]
    public int seed = 0;
    [Tooltip("Index of the side (0-3) that will have the gap. 0: P0-P1, 1: P1-P2, 2: P2-P3, 3: P3-P0.")]
    [Range(0, 3)]
    public int gapSideIndex = 3;
    [Tooltip("Width of the gap in number of prefab units (must be even).")]
    [Min(2)]
    public int gapWidthUnits = 4; // e.g., 4 units = 2 prefabs

    [Header("Prefab References")]
    [Tooltip("Prefab for standard wall segments.")]
    public GameObject normalWallPrefab;
    [Tooltip("Prefab for door segments.")]
    public GameObject doorPrefab; // Single prefab
    [Tooltip("Array of prefabs for window segments.")]
    public GameObject[] windowPrefabs; // Array of variants
    [Tooltip("Array of prefabs for the main 'outer' corner wall piece (e.g., rounded).")]
    public GameObject[] roundedCornerWallPrefabs; // Array of variants - Assumed to be the main outer corner piece
    [Tooltip("Prefab for the main 'outer' corner roof piece.")]
    public GameObject roundedCornerRoofPrefab; // Roof version of the outer corner
    [Tooltip("Array of prefabs for straight roof segments.")]
    public GameObject[] sideRoofPrefabs; // Array of variants - For straight roof sections
    [Tooltip("Prefab for the 'inner' corner wall piece.")]
    public GameObject innerCornerWallPrefab; // Single prefab - For the inner corner piece
    [Tooltip("Prefab for the 'inner' corner roof piece.")]
    public GameObject innerCornerRoofPrefab; // Roof version of the inner corner

    // Constants
    private const float PREFAB_WIDTH = 2f;
    private const float PREFAB_DEPTH = 2f; // Assuming square base for prefabs
    private const float PREFAB_HEIGHT = 3f;
    // Minimum side length calculation: needs space for two corner blocks (PREFAB_WIDTH each along the wall) + 1 wall segment
    private const float MIN_SIDE_LENGTH_CALC = PREFAB_WIDTH * 3f;

    // Internal state
    private System.Random random;
    private int currentHouseHeight;

    // Child object containers
    private Transform cornersContainer;
    private Transform wallsContainer;

    /// <summary>
    /// Generates the entire ring house structure.
    /// </summary>
    public void GenerateRingHouse()
    {
        WipeRingHouse(); // Clear previous generation

        // --- Validation ---
        if (!ValidateSettings()) return;

        // Initialize random state
        random = new System.Random(seed);
        currentHouseHeight = random.Next(minHouseHeight, maxHouseHeight + 1);

        // --- Setup Hierarchy ---
        CreateHierarchy();

        // --- Generation ---
        // Loop through each side defined by the corner points
        for (int i = 0; i < cornerPoints.Count; i++)
        {
            // Get points in world space for calculations
            Vector3 pStartWorld = transform.TransformPoint(cornerPoints[i]);
            Vector3 pEndWorld = transform.TransformPoint(cornerPoints[(i + 1) % cornerPoints.Count]); // Loop back to start
            Vector3 pNextWorld = transform.TransformPoint(cornerPoints[(i + 2) % cornerPoints.Count]); // Next corner for corner generation

            // Generate the corner structure AT THE END point of the current segment (pEndWorld)
            // The corner is associated with the vertex index (i + 1) % cornerPoints.Count
            GenerateCorner(pEndWorld, pStartWorld, pNextWorld, (i + 1) % cornerPoints.Count);

            // Generate the wall segments for the current side (pStartWorld to pEndWorld)
            GenerateSideWalls(pStartWorld, pEndWorld, i);
        }

        Debug.Log($"Generated Ring House with height {currentHouseHeight} prefab units.");
    }

    /// <summary>
    /// Validates the necessary settings before generation.
    /// </summary>
    /// <returns>True if settings are valid, false otherwise.</returns>
    private bool ValidateSettings()
    {
        if (cornerPoints == null || cornerPoints.Count != 4)
        {
            Debug.LogError("RingHouse requires exactly 4 corner points defined.");
            return false;
        }

        if (minHouseHeight > maxHouseHeight)
        {
            Debug.LogError("Minimum house height cannot be greater than maximum house height.");
            return false;
        }
        if (minHouseHeight < 1) {
             Debug.LogWarning("Minimum height must be at least 1.");
             minHouseHeight = 1;
        }


        if (gapWidthUnits % 2 != 0)
        {
             Debug.LogError("Gap Width Units must be an even number.");
             gapWidthUnits = Mathf.Max(2, Mathf.FloorToInt(gapWidthUnits / 2f) * 2); // Correct it
             Debug.LogWarning("Corrected Gap Width Units to " + gapWidthUnits);
        }
         if (gapWidthUnits < 2) {
             gapWidthUnits = 2;
         }

        // Check prefabs
        if (normalWallPrefab == null || doorPrefab == null || windowPrefabs == null || windowPrefabs.Length == 0 ||
            roundedCornerWallPrefabs == null || roundedCornerWallPrefabs.Length == 0 || roundedCornerRoofPrefab == null ||
            sideRoofPrefabs == null || sideRoofPrefabs.Length == 0 || innerCornerWallPrefab == null || innerCornerRoofPrefab == null)
        {
            Debug.LogError("One or more required prefabs are not assigned in the RingHouse component.");
            return false;
        }

        // Check side lengths
        for (int i = 0; i < cornerPoints.Count; i++)
        {
            Vector3 p1World = transform.TransformPoint(cornerPoints[i]);
            Vector3 p2World = transform.TransformPoint(cornerPoints[(i + 1) % cornerPoints.Count]);
            float length = Vector3.Distance(p1World, p2World);
            if (length < MIN_SIDE_LENGTH_CALC)
            {
                Debug.LogError($"Side {i} (Points {i} to {(i + 1) % 4}) is too short ({length:F1} units). Minimum required length is {MIN_SIDE_LENGTH_CALC:F1} units (for corners + 1 wall segment).");
                return false;
            }
        }

        return true;
    }

    /// <summary>
    /// Creates the necessary child GameObjects for organization.
    /// </summary>
    private void CreateHierarchy()
    {
        cornersContainer = new GameObject("Corners").transform;
        cornersContainer.SetParent(transform, false); // Set parent without changing world position

        wallsContainer = new GameObject("Walls").transform;
        wallsContainer.SetParent(transform, false);
    }

    /// <summary>
    /// Generates the 4 prefabs that make up a corner structure using the "build local block, then rotate" method.
    /// </summary>
    /// <param name="cornerPosWorld">The world position of the corner vertex.</param>
    /// <param name="prevPosWorld">The world position of the previous corner vertex.</param>
    /// <param name="nextPosWorld">The world position of the next corner vertex.</param>
    /// <param name="cornerIndex">Index of the corner (0-3) for naming.</param>
    private void GenerateCorner(Vector3 cornerPosWorld, Vector3 prevPosWorld, Vector3 nextPosWorld, int cornerIndex)
    {
        // 1. Create Corner Parent GameObject at cornerPosWorld
        GameObject cornerGO = new GameObject($"Corner_{cornerIndex}");
        cornerGO.transform.SetParent(cornersContainer, false);
        cornerGO.transform.position = cornerPosWorld;

        // 2. Calculate the final rotation for the entire corner block.
        // Align the block's local +Z axis with the direction towards the next corner.
        Vector3 dirToNext = (nextPosWorld - cornerPosWorld).normalized;
        // Handle case where points might be identical (though validation should prevent this)
        if (dirToNext == Vector3.zero) dirToNext = Vector3.forward; // Default fallback
        Quaternion cornerBlockRotation = Quaternion.LookRotation(dirToNext, Vector3.up);
        cornerGO.transform.rotation = cornerBlockRotation; // Apply the rotation to the parent

        // 3. Define LOCAL positions for the center of the 4 prefabs within the Corner_X object's local space.
        // Local +Z points towards Next, Local +X points "Right" relative to that direction.
        float halfPrefab = PREFAB_WIDTH / 2f;
        Vector3 localPosOuterCorner = new Vector3(-halfPrefab, 0, -halfPrefab); // Back-Left relative to dirToNext
        Vector3 localPosInnerCorner = new Vector3(halfPrefab, 0, halfPrefab);   // Front-Right relative to dirToNext
        Vector3 localPosSidePrev = new Vector3(halfPrefab, 0, -halfPrefab);    // Back-Right relative to dirToNext (will face -X)
        Vector3 localPosSideNext = new Vector3(-halfPrefab, 0, halfPrefab);    // Front-Left relative to dirToNext (will face +Z)

        // 4. Define LOCAL rotations for the 4 prefabs relative to the Corner_X parent.
        // These orient the prefabs correctly within the standard 2x2 block structure.
        // Assumptions:
        // - Side prefabs' forward is +Z.
        // - Rounded Corner prefab's curved face spans the +X/+Z quadrant.
        // - Inner Corner prefab's concave face spans the -X/-Z quadrant.
        Quaternion localRotSideNext = Quaternion.Euler(0, 0, 0);                 // Faces +Z (towards Next)
        Quaternion localRotSidePrev = Quaternion.Euler(0, 270, 0);         // Faces -X (towards Prev)
        Quaternion localRotOuterCorner = Quaternion.Euler(0, 270, 0);      // Rotated 180 deg to face -X/-Z
        Quaternion localRotInnerCorner = Quaternion.Euler(0, 90, 0);              // Faces +X/+Z

        // 5. Instantiate prefabs for each height level using LOCAL positions/rotations.
        for (int h = 0; h < currentHouseHeight; h++)
        {
            bool isRoof = (h == currentHouseHeight - 1);
            GameObject sidePrefabVariant; // Wall or Window
            GameObject outerCornerPrefabVariant;
            GameObject innerCornerPrefabVariant;

            if (isRoof)
            {
                sidePrefabVariant = GetRandomPrefab(sideRoofPrefabs); // Use Side Roof for side pieces
                outerCornerPrefabVariant = roundedCornerRoofPrefab;
                innerCornerPrefabVariant = innerCornerRoofPrefab;
            }
            else
            {
                // Use Normal Wall or Window for side pieces on non-roof levels
                sidePrefabVariant = (h > 0 && random.NextDouble() < 0.5 && windowPrefabs.Length > 0) ? GetRandomPrefab(windowPrefabs) : normalWallPrefab;
                outerCornerPrefabVariant = GetRandomPrefab(roundedCornerWallPrefabs);
                innerCornerPrefabVariant = innerCornerWallPrefab;
            }

            // Instantiate using helper function that takes local coords
            InstantiatePrefabLocal(outerCornerPrefabVariant, localPosOuterCorner, localRotOuterCorner, cornerGO.transform, h);
            InstantiatePrefabLocal(innerCornerPrefabVariant, localPosInnerCorner, localRotInnerCorner, cornerGO.transform, h);
            InstantiatePrefabLocal(sidePrefabVariant, localPosSidePrev, localRotSidePrev, cornerGO.transform, h);
            InstantiatePrefabLocal(sidePrefabVariant, localPosSideNext, localRotSideNext, cornerGO.transform, h);
        }
    }

     /// <summary>
    /// Helper function to instantiate a prefab using local position and rotation relative to a parent.
    /// Also handles vertical placement based on height level.
    /// </summary>
    private void InstantiatePrefabLocal(GameObject prefab, Vector3 localPosition, Quaternion localRotation, Transform parent, int heightLevel)
    {
        if (prefab == null) {
             Debug.LogWarning($"InstantiatePrefabLocal: Prefab is null. Parent: {parent?.name ?? "null"}, Height: {heightLevel}", parent);
             return;
        }
         if (parent == null){
             Debug.LogError($"InstantiatePrefabLocal: Parent is null. Prefab: {prefab.name}, Height: {heightLevel}");
             return;
         }


        // Calculate the final local position including the height offset
        Vector3 finalLocalPos = localPosition + (Vector3.up * heightLevel * PREFAB_HEIGHT);

        // Instantiate the prefab as a child of the parent
        GameObject instance = Instantiate(prefab, parent);

        // Set local position and rotation
        instance.transform.localPosition = finalLocalPos;
        instance.transform.localRotation = localRotation;

        // Set name for easier debugging
        instance.name = $"{prefab.name}_h{heightLevel}";
    }


    /// <summary>
    /// Generates BOTH the outer and inner wall segments between two corner points for all height levels.
    /// </summary>
    /// <param name="startPosWorld">World position of the start corner vertex.</param>
    /// <param name="endPosWorld">World position of the end corner vertex.</param>
    /// <param name="sideIndex">Index of the side (0-3) for naming and gap logic.</param>
    private void GenerateSideWalls(Vector3 startPosWorld, Vector3 endPosWorld, int sideIndex)
    {
        // Create parent object for this wall section (containing inner and outer)
        GameObject wallGO = new GameObject($"Wall_{sideIndex}");
        wallGO.transform.SetParent(wallsContainer, false);
        wallGO.transform.position = (startPosWorld + endPosWorld) / 2f; // Center the parent roughly

        Vector3 sideDirection = (endPosWorld - startPosWorld).normalized;
        Quaternion wallRotation = Quaternion.LookRotation(sideDirection, Vector3.up);
        float sideLength = Vector3.Distance(startPosWorld, endPosWorld);

        // Calculate the direction perpendicular (rightward) to the wall for offsetting inner wall
        // Assuming counter-clockwise points, Cross(forward, up) gives right.
        Vector3 rightDir = Vector3.Cross(sideDirection, Vector3.up).normalized;

        // Calculate number of wall segments needed between the corners
        // Effective length for walls = sideLength - CornerWidthAtStart - CornerWidthAtEnd
        float wallSectionLength = sideLength - PREFAB_WIDTH - PREFAB_WIDTH;
        int numWallSegments = Mathf.FloorToInt(wallSectionLength / PREFAB_WIDTH);

        if (numWallSegments < 0) numWallSegments = 0; // Safety check

        // Determine start and end indices for the gap if this is the gap side
        int gapStartIndex = -1;
        int gapEndIndex = -1;
        bool isGapSide = (sideIndex == gapSideIndex);
        int gapWidthSegments = gapWidthUnits / (int)PREFAB_WIDTH;

        if (isGapSide && numWallSegments >= gapWidthSegments)
        {
            // Center the gap
            gapStartIndex = Mathf.FloorToInt((numWallSegments - gapWidthSegments) / 2f);
            gapEndIndex = gapStartIndex + gapWidthSegments;
        }
        else if (isGapSide)
        {
             Debug.LogWarning($"Side {sideIndex} is too short ({numWallSegments} segments) to contain the requested gap ({gapWidthSegments} segments). Skipping gap.");
             isGapSide = false; // Treat as normal side if gap doesn't fit
        }

        bool placedDoorOnSide = false; // Track door placement for the outer wall

        // Generate wall segments for each level
        for (int h = 0; h < currentHouseHeight; h++)
        {
            bool isGroundFloor = (h == 0);
            bool isRoofLevel = (h == currentHouseHeight - 1);
            // Determine prefab type for non-ground/non-roof floors (consistent per floor)
            bool useWindowsThisFloor = !isGroundFloor && !isRoofLevel && random.NextDouble() < 0.5;
            GameObject upperFloorWallPrefab = useWindowsThisFloor ? GetRandomPrefab(windowPrefabs) : normalWallPrefab;
            if (upperFloorWallPrefab == null) upperFloorWallPrefab = normalWallPrefab; // Fallback

            for (int i = 0; i < numWallSegments; i++)
            {
                // Check if this segment is within the gap
                if (isGapSide && i >= gapStartIndex && i < gapEndIndex)
                {
                    continue; // Skip generating wall segments in the gap
                }

                // Calculate the base position for the center of this segment slot along the side's centerline
                Vector3 segmentBasePos = startPosWorld
                                     + sideDirection * PREFAB_WIDTH // Offset past start corner structure center
                                     + sideDirection * (i * PREFAB_WIDTH) // Offset to the start of this segment slot
                                     + sideDirection * (PREFAB_WIDTH / 2f); // Offset to the center of the segment slot

                // --- Generate Outer Wall Segment ---
                Vector3 outerWallPos = segmentBasePos - rightDir * (PREFAB_WIDTH / 2f); // Offset outwards (left if looking along sideDir)
                outerWallPos += Vector3.up * h * PREFAB_HEIGHT; // Add height offset

                GameObject outerPrefabToInstantiate = null;
                if (isRoofLevel)
                {
                    outerPrefabToInstantiate = GetRandomPrefab(sideRoofPrefabs);
                }
                else if (isGroundFloor)
                {
                    // Try place door on outer wall only, if not gap side
                    if (!placedDoorOnSide && !isGapSide && random.NextDouble() < 0.2) // Lower chance per segment
                    {
                        outerPrefabToInstantiate = doorPrefab;
                        placedDoorOnSide = true;
                    }
                    else
                    {
                        outerPrefabToInstantiate = normalWallPrefab; // Ground floor mostly solid walls
                    }
                }
                else // Upper floors (not roof)
                {
                    outerPrefabToInstantiate = upperFloorWallPrefab;
                }

                if (outerPrefabToInstantiate != null)
                {
                    InstantiatePrefabWorld(outerPrefabToInstantiate, outerWallPos, wallRotation, wallGO.transform);
                }

                // --- Generate Inner Wall Segment ---
                Vector3 innerWallPos = segmentBasePos + rightDir * (PREFAB_WIDTH / 2f); // Offset inwards (right if looking along sideDir)
                innerWallPos += Vector3.up * h * PREFAB_HEIGHT; // Add height offset

                GameObject innerPrefabToInstantiate = null;
                if (isRoofLevel)
                {
                    innerPrefabToInstantiate = GetRandomPrefab(sideRoofPrefabs);
                }
                else if (isGroundFloor)
                {
                    // Inner ground floor: Normal walls or maybe windows
                    innerPrefabToInstantiate = (random.NextDouble() < 0.2 && windowPrefabs.Length > 0) ? GetRandomPrefab(windowPrefabs) : normalWallPrefab;
                }
                else // Upper floors (not roof)
                {
                     innerPrefabToInstantiate = upperFloorWallPrefab;
                }

                 if (innerPrefabToInstantiate != null)
                {
                    // Inner walls face inwards, so rotate 180 degrees around Y relative to the main wall rotation
                    Quaternion innerWallRotation = wallRotation * Quaternion.Euler(0, 180, 0);
                    InstantiatePrefabWorld(innerPrefabToInstantiate, innerWallPos, innerWallRotation, wallGO.transform);
                }
            } // End loop through segments (i)
        } // End loop through height (h)

         // Ensure at least one door is placed on non-gap outer ground floors if not already placed randomly
         if (!isGapSide && !placedDoorOnSide && numWallSegments > 0 && currentHouseHeight > 0)
         {
             int doorSegmentIndex = random.Next(0, numWallSegments);
             // Calculate position again for the chosen segment on outer ground floor
              Vector3 segmentBasePos = startPosWorld
                                     + sideDirection * PREFAB_WIDTH
                                     + sideDirection * (doorSegmentIndex * PREFAB_WIDTH)
                                     + sideDirection * (PREFAB_WIDTH / 2f);
             Vector3 doorPos = segmentBasePos - rightDir * (PREFAB_WIDTH / 2f); // Outer wall position

             // Find and replace the existing wall prefab at that location on the ground floor
             Collider[] colliders = Physics.OverlapSphere(doorPos, PREFAB_WIDTH * 0.1f); // Small overlap sphere
             foreach (var col in colliders)
             {
                 if (col.transform.parent == wallGO.transform && Mathf.Abs(col.transform.position.y - 0) < PREFAB_HEIGHT * 0.1f)
                 {
                     if (Application.isEditor && !Application.isPlaying) DestroyImmediate(col.gameObject);
                     else Destroy(col.gameObject);
                     break;
                 }
             }
             // Instantiate the door
             InstantiatePrefabWorld(doorPrefab, doorPos, wallRotation, wallGO.transform);
             // Debug.Log($"Forced door placement on Wall {sideIndex}");
         }
    }

    /// <summary>
    /// Clears all generated child objects (corners and walls).
    /// </summary>
    public void WipeRingHouse()
    {
        bool isEditor = Application.isEditor && !Application.isPlaying;
        List<GameObject> childrenToDestroy = new List<GameObject>();
        for (int i = transform.childCount - 1; i >= 0; i--) {
            childrenToDestroy.Add(transform.GetChild(i).gameObject);
        }
        foreach (GameObject child in childrenToDestroy) {
            if (child != null) {
                 if (isEditor) DestroyImmediate(child);
                 else Destroy(child);
            }
        }
        cornersContainer = null;
        wallsContainer = null;
    }

    /// <summary>
    /// Instantiates a prefab at the specified WORLD position and rotation, parenting it correctly.
    /// Renamed from previous InstantiatePrefab to avoid confusion with InstantiatePrefabLocal.
    /// </summary>
    private GameObject InstantiatePrefabWorld(GameObject prefab, Vector3 worldPosition, Quaternion worldRotation, Transform parent)
    {
        if (prefab == null) {
             Debug.LogWarning($"InstantiatePrefabWorld: Prefab is null. Parent: {parent?.name ?? "null"}, Pos: {worldPosition}", parent);
             return null;
         }
        if (parent == null) {
             Debug.LogError($"InstantiatePrefabWorld: Parent is null. Prefab: {prefab.name}, Pos: {worldPosition}");
             return null;
        }

        GameObject instance = Instantiate(prefab, worldPosition, worldRotation, parent);
        instance.name = prefab.name;
        return instance;
    }

    /// <summary>
    /// Gets a random prefab from an array using the internal random generator. Handles null/empty arrays.
    /// </summary>
    private GameObject GetRandomPrefab(GameObject[] prefabs)
    {
        if (prefabs == null || prefabs.Length == 0) return null;
        var validPrefabs = prefabs.Where(p => p != null).ToArray();
        if (validPrefabs.Length == 0) return null;
        return validPrefabs[random.Next(validPrefabs.Length)];
    }

    // Draw gizmos in the editor to visualize the points and connections (using world space)
    private void OnDrawGizmosSelected()
    {
        if (cornerPoints == null || cornerPoints.Count < 4) return;

        Gizmos.color = Color.yellow;
        Vector3[] worldPoints = new Vector3[cornerPoints.Count];
        for(int i = 0; i < cornerPoints.Count; i++) {
            worldPoints[i] = transform.TransformPoint(cornerPoints[i]);
        }

        for (int i = 0; i < worldPoints.Length; i++)
        {
            Vector3 currentPoint = worldPoints[i];
            Vector3 nextPoint = worldPoints[(i + 1) % worldPoints.Length];

            Gizmos.color = Color.yellow; // Centerline
            Gizmos.DrawSphere(currentPoint, 0.3f);
            Gizmos.DrawLine(currentPoint, nextPoint);

            Vector3 sideDir = (nextPoint - currentPoint).normalized;
            Vector3 rightDir = Vector3.Cross(sideDir, Vector3.up).normalized;
            float offset = PREFAB_WIDTH; // Offset for inner/outer lines visualization

            Gizmos.color = Color.green; // Outer line approx
            Gizmos.DrawLine(currentPoint - rightDir * offset, nextPoint - rightDir * offset);
            Gizmos.color = Color.blue; // Inner line approx
            Gizmos.DrawLine(currentPoint + rightDir * offset, nextPoint + rightDir * offset);

            #if UNITY_EDITOR
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label( (currentPoint + nextPoint) / 2f, Vector3.Distance(currentPoint, nextPoint).ToString("F1"));
            #endif
        }

        if (gapSideIndex >= 0 && gapSideIndex < worldPoints.Length)
        {
             Gizmos.color = Color.red; // Highlight gap centerline
             Vector3 p1 = worldPoints[gapSideIndex];
             Vector3 p2 = worldPoints[(gapSideIndex + 1) % worldPoints.Length];
             Gizmos.DrawLine(p1, p2);
             #if UNITY_EDITOR
             UnityEditor.Handles.color = Color.red;
             UnityEditor.Handles.Label((p1 + p2) / 2f + Vector3.down * 0.2f, "GAP SIDE");
             #endif
        }
    }
}
