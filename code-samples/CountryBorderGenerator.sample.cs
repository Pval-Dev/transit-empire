using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// Representative sample extracted from Transit Empire's country border generation system.
/// 
/// This component builds the visual country layer from a preprocessed JSON dataset.
/// It demonstrates:
/// - loading country polygon data from Resources
/// - batching generation through a coroutine
/// - creating LineRenderer borders
/// - creating triangulated MeshRenderer country surfaces
/// - adding PolygonCollider2D click zones
/// - generating country labels
/// - connecting generated visuals to country UI handlers
/// </summary>
public class CountryBorderGeneratorSample : MonoBehaviour
{
    public static CountryBorderGeneratorSample Instance;

    [Header("Prefabs")]
    public GameObject countryVisualPrefab;

    [Header("Materials")]
    public Material countryMaterial;
    public Material borderMaterial;

    [Header("Border Visuals")]
    public Color borderColor = Color.white;
    public float borderWidth = 0.2f;

    [Header("Country Labels")]
    public int labelFontSize = 120;
    public Color labelColor = new Color(1f, 1f, 1f, 0.4f);

    [Header("UI")]
    public GameObject countryInterface;
    public GameObject mainUi;

    [Header("Load Flow")]
    public bool shouldLoadSaveAfterGeneration;
    public bool loadingExistingGame;

    private CountryEntry[] countries;

    [System.Serializable]
    public class CountryEntry
    {
        public string country;
        public string continent;
        public Color color;

        public Point[] points;
        public Triangle[] triangles;
    }

    [System.Serializable]
    public class Point
    {
        public float x;
        public float y;
    }

    [System.Serializable]
    public class Triangle
    {
        public float ax;
        public float ay;

        public float bx;
        public float by;

        public float cx;
        public float cy;
    }

    void Awake()
    {
        Instance = this;
    }

    public void GenerateBorders()
    {
        StartCoroutine(GenerateBordersRoutine());
    }

    private IEnumerator GenerateBordersRoutine()
    {
        TextAsset json = Resources.Load<TextAsset>("countries");

        if (json == null)
        {
            Debug.LogError("countries.json not found.");
            yield break;
        }

        countries = JsonHelper.FromJson<CountryEntry>(json.text);

        int generatedCount = 0;

        foreach (CountryEntry countryData in countries)
        {
            Transform countryNode = ResolveCountryNode(countryData);

            if (countryNode == null)
                continue;

            GenerateCountryVisual(countryData, countryNode);

            generatedCount++;

            // Spread generation over multiple frames to avoid frame spikes.
            if (generatedCount % 5 == 0)
                yield return null;
        }

        FinishGeneration();
    }

    private Transform ResolveCountryNode(CountryEntry data)
    {
        GameObject continentObject = GameObject.Find(data.continent);

        if (continentObject == null)
        {
            Debug.LogWarning("Continent not found: " + data.continent);
            return null;
        }

        Transform countryNode = continentObject.transform.Find(data.country);

        if (countryNode == null)
        {
            Debug.LogWarning("Country node not found: " + data.country);
            return null;
        }

        return countryNode;
    }

    private void GenerateCountryVisual(CountryEntry data, Transform countryNode)
    {
        GameObject visualObject = CreateCountryObject(data, countryNode);

        Vector2[] colliderPoints;
        Vector3[] borderPoints;
        Vector3 center;

        BuildPointData(data, out colliderPoints, out borderPoints, out center);

        CreateBorder(visualObject, borderPoints);
        CreateCountryMesh(visualObject, data);
        CreateCollider(visualObject, colliderPoints);
        CreateLabel(visualObject, data.country, borderPoints, center);
        CreateClickHandler(visualObject, data, countryNode);
    }

    private GameObject CreateCountryObject(CountryEntry data, Transform countryNode)
    {
        GameObject visualObject = countryVisualPrefab != null
            ? Instantiate(countryVisualPrefab)
            : new GameObject();

        visualObject.name = data.country + "_visual";
        visualObject.transform.SetParent(countryNode);
        visualObject.transform.localPosition = Vector3.zero;

        return visualObject;
    }

    private void BuildPointData(
        CountryEntry data,
        out Vector2[] colliderPoints,
        out Vector3[] borderPoints,
        out Vector3 center)
    {
        colliderPoints = new Vector2[data.points.Length];
        borderPoints = new Vector3[data.points.Length];

        center = Vector3.zero;

        for (int i = 0; i < data.points.Length; i++)
        {
            Vector2 point2D = new Vector2(
                data.points[i].x,
                data.points[i].y
            );

            Vector3 point3D = new Vector3(
                data.points[i].x,
                data.points[i].y,
                0f
            );

            colliderPoints[i] = point2D;
            borderPoints[i] = point3D;

            center += point3D;
        }

        if (data.points.Length > 0)
            center /= data.points.Length;
    }

    private void CreateBorder(GameObject target, Vector3[] points)
    {
        LineRenderer line = target.AddComponent<LineRenderer>();

        line.material = borderMaterial;
        line.startColor = borderColor;
        line.endColor = borderColor;

        line.startWidth = borderWidth;
        line.endWidth = borderWidth;

        line.positionCount = points.Length;
        line.SetPositions(points);
        line.loop = true;

        // The dataset is already projected into world coordinates.
        line.useWorldSpace = true;

        line.sortingLayerName = "Countries";
        line.sortingOrder = -10;
    }

    private void CreateCountryMesh(GameObject target, CountryEntry data)
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        foreach (Triangle triangle in data.triangles)
        {
            int baseIndex = vertices.Count;

            vertices.Add(new Vector3(triangle.ax, triangle.ay, 0f));
            vertices.Add(new Vector3(triangle.bx, triangle.by, 0f));
            vertices.Add(new Vector3(triangle.cx, triangle.cy, 0f));

            // Reversed winding order keeps the mesh visible with the chosen material.
            triangles.Add(baseIndex);
            triangles.Add(baseIndex + 2);
            triangles.Add(baseIndex + 1);
        }

        Mesh mesh = new Mesh
        {
            vertices = vertices.ToArray(),
            triangles = triangles.ToArray()
        };

        mesh.RecalculateNormals();

        MeshFilter meshFilter = target.AddComponent<MeshFilter>();
        meshFilter.mesh = mesh;

        MeshRenderer meshRenderer = target.AddComponent<MeshRenderer>();
        meshRenderer.material = new Material(countryMaterial);
        meshRenderer.material.color = data.color;

        meshRenderer.sortingLayerName = "Countries";
        meshRenderer.sortingOrder = -10;
    }

    private void CreateCollider(GameObject target, Vector2[] points)
    {
        PolygonCollider2D collider = target.AddComponent<PolygonCollider2D>();
        collider.SetPath(0, points);
    }

    private void CreateLabel(
        GameObject parent,
        string countryName,
        Vector3[] countryPoints,
        Vector3 center)
    {
        Vector2 bounds = CalculateCountryBounds(countryPoints);
        float countrySize = Mathf.Min(bounds.x, bounds.y) * 0.15f;

        GameObject labelObject = new GameObject(countryName + "_label");
        labelObject.transform.SetParent(parent.transform);
        labelObject.transform.position = center + Vector3.back;

        TextMesh label = labelObject.AddComponent<TextMesh>();

        label.text = countryName;
        label.fontSize = labelFontSize;
        label.characterSize = countrySize / Mathf.Max(countryName.Length, 1);
        label.anchor = TextAnchor.MiddleCenter;
        label.alignment = TextAlignment.Center;
        label.color = labelColor;

        MeshRenderer labelRenderer = labelObject.GetComponent<MeshRenderer>();
        labelRenderer.sortingLayerName = "Countries";
        labelRenderer.sortingOrder = -9;

        MeshCollider textCollider = labelObject.GetComponent<MeshCollider>();

        if (textCollider != null)
            Destroy(textCollider);
    }

    private Vector2 CalculateCountryBounds(Vector3[] points)
    {
        if (points.Length == 0)
            return Vector2.zero;

        float minX = float.MaxValue;
        float maxX = float.MinValue;

        float minY = float.MaxValue;
        float maxY = float.MinValue;

        foreach (Vector3 point in points)
        {
            if (point.x < minX) minX = point.x;
            if (point.x > maxX) maxX = point.x;

            if (point.y < minY) minY = point.y;
            if (point.y > maxY) maxY = point.y;
        }

        return new Vector2(
            maxX - minX,
            maxY - minY
        );
    }

    private void CreateClickHandler(
        GameObject visualObject,
        CountryEntry data,
        Transform countryNode)
    {
        CountryClickHandler handler =
            visualObject.AddComponent<CountryClickHandler>();

        handler.countryName = data.country;
        handler.CountryUi = countryInterface;
        handler.Country = countryNode.GetComponent<Country>();
    }

    private void FinishGeneration()
    {
        if (loadingExistingGame)
        {
            Debug.Log("Country borders generated while loading an existing game.");
            return;
        }

        if (shouldLoadSaveAfterGeneration)
        {
            SaveManager.Instance.LoadGame();
            return;
        }

        if (mainUi != null)
            mainUi.SetActive(true);
    }
}
