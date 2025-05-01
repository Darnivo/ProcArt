using UnityEngine;
using UnityEditor; // Required for Editor-specific attributes and Handles (though Handles are used in the Editor script)
using System.Collections.Generic;
using System.Linq; // Required for LINQ operations like Sum

// Enum to define the type of house based on its position in the sequence
public enum HouseType
{
    Single, // Only one house in the neighborhood
    First,  // The first house in a sequence of multiple houses
    Middle, // A house in the middle of a sequence
    Last    // The last house in a sequence
}

public class NeighborhoodCreator : MonoBehaviour
{
    [Header("Path Settings")]
    [Tooltip("The starting point of the neighborhood path.")]
    public Vector3 pathStart = Vector3.zero;
    [Tooltip("The ending point of the neighborhood path.")]
    public Vector3 pathEnd = new Vector3(0, 0, 10); // Default path length

    [Header("Generation Settings")]
    [Tooltip("The number of houses to generate along the path.")]
    [Range(1, 50)] // Limit the number of houses for performance
    public int numberOfHouses = 5;
    [Tooltip("The minimum length of each house in world units (must be a multiple of 2).")]
    [Min(6)] // Minimum length 3 prefab units = 6 world units
    public float minimumHouseLength = 6f;
    [Tooltip("The width of all houses in the neighborhood in world units (must be a multiple of 2).")]
    [Range(2, 4)] // House width is 2 or 4 units (1 or 2 prefab widths)
    public int houseWidth = 2;
    [Tooltip("The minimum height of each house in prefab units (including roof level).")]
    [Range(3, 5)]
    public int minHouseHeight = 3;
    [Tooltip("The maximum height of each house in prefab units (including roof level).")]
    [Range(3, 5)]
    public int maxHouseHeight = 5;
    [Tooltip("Random seed for deterministic generation of the neighborhood and houses.")]
    public int seed = 0;

    [Header("Prefab References")]
    public GameObject normalWallPrefab;
    public GameObject decoratedWallPrefab;
    public GameObject[] balconyWallPrefabs; // Different types of balconies
    public GameObject[] doorPrefabs; // Different variants of doors
    public GameObject[] windowPrefabs; // Different types/variants of windows

    public GameObject[] roundedCornerWallPrefabs; // Variants of rounded corners
    public GameObject[] straightCornerWallPrefabs; // Variants of straight corners

    public GameObject[] sideRoofPrefabs; // Variants of side roofs
    public GameObject roundedCornerRoofPrefab;
    public GameObject straightCornerRoofPrefab;
    public GameObject topRoofPrefab; // For widths > 2
    public GameObject specialTwoUnitRoofPrefab; // The 2-unit wide roof component

    public GameObject[] sideRoofEdgePrefabs; // For when it meets neighbors (barrier in -z)
    public GameObject cornerEdgeWallPrefab; // For corners when it meets the edge
    public GameObject middleEdgeWallPrefab; // For widths > 2 when it meets the edge
    public GameObject topRoofEdgePrefab; // For widths > 2 when it meets the edge


    // List to keep track of generated houses
    private List<House> generatedHouses = new List<House>();

    /// <summary>
    /// Generates the neighborhood and its houses.
    /// </summary>
    public void GenerateNeighborhood()
    {
        // Wipe existing houses before generating
        WipeNeighborhood();

        // Validate settings
        if (houseWidth != 2 && houseWidth != 4)
        {
            Debug.LogError("House width must be 2 or 4 units.");
            return;
        }
         if (minimumHouseLength % 2 != 0 || minimumHouseLength < 6)
        {
            Debug.LogError("Minimum house length must be a multiple of 2 and at least 6.");
            return;
        }
        if (minHouseHeight > maxHouseHeight)
        {
            Debug.LogError("Minimum house height cannot be greater than maximum house height.");
            return;
        }
        if (minHouseHeight < 3 || maxHouseHeight > 5)
        {
             Debug.LogError("House height must be between 3 and 5 prefab units (inclusive).");
            return;
        }

        // Calculate total available length
        float totalPathLength = Vector3.Distance(pathStart, pathEnd);

        // Calculate required minimum length for all houses
        float requiredMinLength = numberOfHouses * minimumHouseLength;

        if (requiredMinLength > totalPathLength)
        {
            Debug.LogError($"Not enough path length ({totalPathLength}) for {numberOfHouses} houses with minimum length {minimumHouseLength}. Required: {requiredMinLength}");
            return;
        }

        // Calculate remaining length to distribute
        float remainingLength = totalPathLength - requiredMinLength;

        // Use the main seed for neighborhood-level randomization (like distributing lengths)
        Random.InitState(seed);

        // Calculate house lengths
        List<float> houseLengths = new List<float>();
        float distributedLength = 0;
        for (int i = 0; i < numberOfHouses; i++)
        {
            // Calculate a random extra length for this house
            // Ensure the sum of extra lengths equals the remaining length
            float extraLength = 0;
            if (remainingLength > 0)
            {
                // Distribute remaining length, ensuring total sum is correct
                float maxExtra = remainingLength / (numberOfHouses - i);
                extraLength = Random.Range(0, maxExtra);
                // Ensure extraLength is a multiple of 2
                extraLength = Mathf.Floor(extraLength / 2) * 2;
                remainingLength -= extraLength;
            }

            float houseLength = minimumHouseLength + extraLength;
             // Ensure the final house length is a multiple of 2
            houseLength = Mathf.Floor(houseLength / 2) * 2;
            if (houseLength < minimumHouseLength) houseLength = minimumHouseLength; // Ensure minimum length is met

            houseLengths.Add(houseLength);
            distributedLength += houseLength;
        }

         // Adjust the last house length slightly if there's a floating point discrepancy
        float lengthDifference = totalPathLength - distributedLength;
        if (Mathf.Abs(lengthDifference) > 0.01f && houseLengths.Count > 0)
        {
             houseLengths[houseLengths.Count - 1] += lengthDifference;
             // Ensure the final house length is a multiple of 2 again
             houseLengths[houseLengths.Count - 1] = Mathf.Floor(houseLengths[houseLengths.Count - 1] / 2) * 2;
             if (houseLengths[houseLengths.Count - 1] < minimumHouseLength) houseLengths[houseLengths.Count - 1] = minimumHouseLength;
        }


        // Calculate house positions and generate houses
        Vector3 currentPosition = pathStart;
        Vector3 pathDirection = (pathEnd - pathStart).normalized;
        Quaternion pathRotation = Quaternion.LookRotation(pathDirection);

        for (int i = 0; i < numberOfHouses; i++)
        {
            // Determine house type
            HouseType houseType = HouseType.Middle;
            if (numberOfHouses == 1)
            {
                houseType = HouseType.Single;
            }
            else if (i == 0)
            {
                houseType = HouseType.First;
            }
            else if (i == numberOfHouses - 1)
            {
                houseType = HouseType.Last;
            }

            // Calculate house position (center of the house along the path)
            float houseLength = houseLengths[i];
            Vector3 houseCenterPosition = currentPosition + pathDirection * (houseLength / 2f);

            // Create House GameObject
            GameObject houseGO = new GameObject($"House_{i}");
            houseGO.transform.parent = transform; // Make it a child of the NeighborhoodCreator
            houseGO.transform.position = houseCenterPosition;
            houseGO.transform.rotation = pathRotation; // Align house rotation with path direction

            // Add House script
            House house = houseGO.AddComponent<House>();

            // Assign properties to the House script
            house.neighborhoodCreator = this; // Reference back to the creator
            house.houseType = houseType;
            house.houseLength = houseLength;
            house.houseWidth = houseWidth;
            house.houseHeight = Random.Range(minHouseHeight, maxHouseHeight + 1); // Random height within range
            house.seed = seed + i + 1; // Generate a unique seed for each house

            // Assign prefab references to the House script
            house.normalWallPrefab = normalWallPrefab;
            house.decoratedWallPrefab = decoratedWallPrefab;
            house.balconyWallPrefabs = balconyWallPrefabs;
            house.doorPrefabs = doorPrefabs;
            house.windowPrefabs = windowPrefabs;
            house.roundedCornerWallPrefabs = roundedCornerWallPrefabs;
            house.straightCornerWallPrefabs = straightCornerWallPrefabs;
            house.sideRoofPrefabs = sideRoofPrefabs;
            house.roundedCornerRoofPrefab = roundedCornerRoofPrefab;
            house.straightCornerRoofPrefab = straightCornerRoofPrefab;
            house.topRoofPrefab = topRoofPrefab;
            house.specialTwoUnitRoofPrefab = specialTwoUnitRoofPrefab;
            house.sideRoofEdgePrefabs = sideRoofEdgePrefabs;
            house.cornerEdgeWallPrefab = cornerEdgeWallPrefab;
            house.middleEdgeWallPrefab = middleEdgeWallPrefab;
            house.topRoofEdgePrefab = topRoofEdgePrefab;


            // Generate the individual house mesh
            house.GenerateHouse();

            // Add to generated houses list
            generatedHouses.Add(house);

            // Move current position along the path for the next house
            currentPosition += pathDirection * houseLength;
        }

        Debug.Log($"Generated {numberOfHouses} houses along the path.");
    }

    /// <summary>
    /// Wipes all generated houses.
    /// </summary>
    public void WipeNeighborhood()
    {
        // Destroy all child objects that are houses
        foreach (House house in generatedHouses)
        {
            if (house != null && house.gameObject != null)
            {
                 // Use DestroyImmediate in Editor scripts for immediate cleanup
                if (Application.isEditor)
                {
                    DestroyImmediate(house.gameObject);
                }
                else
                {
                    Destroy(house.gameObject);
                }
            }
        }
        generatedHouses.Clear();

        // Also clean up any remaining child objects just in case
        List<GameObject> childrenToDestroy = new List<GameObject>();
        foreach (Transform child in transform)
        {
            childrenToDestroy.Add(child.gameObject);
        }
         foreach (GameObject child in childrenToDestroy)
        {
             if (Application.isEditor)
            {
                DestroyImmediate(child);
            }
            else
            {
                Destroy(child);
            }
        }

        Debug.Log("Wiped generated neighborhood.");
    }

    // Helper method to get the path direction
    public Vector3 GetPathDirection()
    {
        return (pathEnd - pathStart).normalized;
    }

     // Helper method to get the path rotation
    public Quaternion GetPathRotation()
    {
        return Quaternion.LookRotation(GetPathDirection());
    }
}
