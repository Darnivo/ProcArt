using UnityEngine;

[ExecuteAlways]
[RequireComponent(typeof(Renderer))]
public class UVScaler : MonoBehaviour
{
    [Range(0.1f, 20f)]
    public float scale = 1f;

    private Renderer rend;

    void OnValidate()
    {
        ApplyScale();
    }

    void OnEnable()
    {
        ApplyScale();
    }

    void ApplyScale()
    {
        if (rend == null)
            rend = GetComponent<Renderer>();

        if (rend != null && rend.sharedMaterial != null)
        {
            Vector2 newScale = new Vector2(scale, scale);
            rend.sharedMaterial.mainTextureScale = newScale;
        }
    }
}
