using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// GridDisplay spawn a grid of game object (elementPrefab)
/// </summary>
public class GridDisplay : MonoBehaviour
{
    [SerializeField] GameObject elementPrefab;
    [SerializeField] float gridRadius = 3f;
    [SerializeField] float gridSpacing = 1f;

    List<GameObject> elements = new List<GameObject>();

    private void Awake()
    {
        SpawnElements();
    }

    public void SpawnElements()
    {
        foreach (var e in elements) Destroy(e.gameObject);
        elements.Clear();

        float roundedRadius = Mathf.Ceil(gridRadius / gridSpacing) * gridSpacing;

        Vector3 position = Vector3.zero;
        for (float x = -roundedRadius; x <= roundedRadius; x += gridSpacing)
        {
            for (float y = -roundedRadius; y <= roundedRadius; y += gridSpacing)
            {
                for (float z = -roundedRadius; z <= roundedRadius; z += gridSpacing)
                {
                    var g = GameObject.Instantiate(elementPrefab);
                    g.transform.parent = transform;
                    g.transform.localPosition = new Vector3(x, y, z);
                }
            }
        }
    }
}
