using UnityEngine;

public class Spawner : MonoBehaviour
{
    // Reference to the prefab you want to instantiate
    [SerializeField] public GameObject prefab;

    // Start is called before the first frame update
    void Start()
    {
        // Instantiate the prefab at the origin with no rotation
        Instantiate(prefab, Vector3.zero, Quaternion.identity);
    }
}
