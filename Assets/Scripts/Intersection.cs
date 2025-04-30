using UnityEngine;
using System.Collections.Generic;

public class Intersection : MonoBehaviour
{
    public List<Road> connectedRoads = new List<Road>();

    public void Initialize(RoadNetwork network)
    {
        // You can add intersection visualization logic here
        GetComponent<MeshRenderer>().sharedMaterial = network.majorRoadMaterial;
    }
}