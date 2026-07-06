using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Representative sample extracted from Transit Empire's airport generation system.
///
/// This component builds the runtime airport network from a JSON dataset.
/// It groups airports under country nodes, assigns type-based metadata,
/// initializes purchase interactions, and registers generated airports
/// in the central simulation orchestrator.
/// </summary>
public class AirportGeneratorSample : MonoBehaviour
{
    public static AirportGeneratorSample Instance;

    [Header("Prefabs")]
    public GameObject airportPrefab;
    public GameObject countryPrefab;
    public GameObject buyButtonPrefab;

    [Header("Airport Sprites")]
    public Sprite regionalSprite;
    public Sprite capitalSprite;
    public Sprite internationalSprite;

    [Header("World Sizes")]
    public float regionalSize = 0.8f;
    public float capitalSize = 1.4f;
    public float internationalSize = 2.0f;

    [Header("Tutorial Mode")]
    public bool tutorialMode;
    public HashSet<string> tutorialCountries = new HashSet<string>
    {
        "Germany",
        "Poland",
        "France"
    };

    private AirportEntry[] airportEntries;

    [System.Serializable]
    public class AirportEntry
    {
        public string airportName;
        public float x;
        public float y;

        public string continent;
        public string country;

        public int airportType;

        public float CountryPriceMultiplier = 1f;
        public float PassMultiplier = 1f;

        public float[] multiplierssecond =
        {
            1f,
            1.5f,
            2f
        };
    }

    void Awake()
    {
        Instance = this;
    }

    public void GenerateAirports()
    {
        TextAsset source = Resources.Load<TextAsset>("airports");

        if (source == null)
        {
            Debug.LogError("Airport dataset not found.");
            return;
        }

        airportEntries = JsonHelper.FromJson<AirportEntry>(source.text);

        GameManager.Instance.AllAirports.Clear();

        foreach (AirportEntry entry in airportEntries)
        {
            if (tutorialMode && !tutorialCountries.Contains(entry.country))
                continue;

            Transform continentNode = FindContinentNode(entry.continent);
            if (continentNode == null)
                continue;

            Transform countryNode = FindOrCreateCountryNode(entry, continentNode);
            CreateAirport(entry, countryNode);
        }

        CountryBorderGenerator.Instance.GenerateBorders();

        Debug.Log("Generated airports: " + GameManager.Instance.AllAirports.Count);
    }

    Transform FindContinentNode(string continentName)
    {
        GameObject continentObject = GameObject.Find(continentName);
        return continentObject != null ? continentObject.transform : null;
    }

    Transform FindOrCreateCountryNode(AirportEntry entry, Transform continentNode)
    {
        Transform existingCountry = continentNode.Find(entry.country);

        if (existingCountry != null)
            return existingCountry;

        GameObject countryObject = Instantiate(countryPrefab);
        countryObject.name = entry.country;
        countryObject.transform.SetParent(continentNode);

        Country country = countryObject.GetComponent<Country>();

        if (country != null)
        {
            country.CountryNombre = entry.country;
            country.CostMultiplier = entry.CountryPriceMultiplier;
            country.multiplier = entry.PassMultiplier;
        }

        return countryObject.transform;
    }

    void CreateAirport(AirportEntry entry, Transform countryNode)
    {
        Vector3 position = new Vector3(entry.x, entry.y, 0f);

        GameObject airportObject = Instantiate(
            airportPrefab,
            position,
            Quaternion.identity
        );

        airportObject.name = BuildCompactAirportName(entry.airportName);
        airportObject.transform.SetParent(countryNode);
        airportObject.transform.localScale = Vector3.one;

        ConfigureAirportVisuals(airportObject, entry.airportType);
        ConfigureAirportCollider(airportObject);
        ConfigureAirportDomainData(airportObject, entry);
        CreatePurchaseButton(airportObject, entry.airportType);
    }

    void ConfigureAirportVisuals(GameObject airportObject, int airportType)
    {
        SpriteRenderer renderer = airportObject.GetComponent<SpriteRenderer>();

        if (renderer == null)
            return;

        renderer.sprite = GetSpriteForType(airportType);
        renderer.sortingLayerName = "Airports";
        renderer.sortingOrder = 10;

        // Airports start hidden until purchased or unlocked by progression.
        renderer.enabled = false;

        ApplySpriteSize(renderer, GetSizeForType(airportType));

        foreach (Transform child in airportObject.transform)
        {
            SpriteRenderer childRenderer = child.GetComponent<SpriteRenderer>();

            if (childRenderer == null)
                continue;

            childRenderer.sortingLayerName = "Airports";
            childRenderer.sortingOrder = 10;
        }
    }

    void ConfigureAirportCollider(GameObject airportObject)
    {
        CircleCollider2D collider = airportObject.GetComponent<CircleCollider2D>();

        if (collider == null)
            return;

        collider.radius = 1f;

        // The airport is not directly clickable until it becomes visible.
        collider.enabled = false;
    }

    void ConfigureAirportDomainData(GameObject airportObject, AirportEntry entry)
    {
        Airport airport = airportObject.GetComponent<Airport>();

        if (airport == null)
            return;

        float basePrice = CalculateBaseAirportPrice(entry);

        airport.BaseApPrice = basePrice;
        airport.price = basePrice;

        airport.AirportName = entry.airportName;
        airport.airportType = entry.airportType;

        airport.Container = entry.continent;
        airport.CountryHome = entry.country;

        airport.secondarymultiplierBASE =
            entry.multiplierssecond[entry.airportType];

        airport.MoneyForMaintenance = basePrice;

        airport.InitType();

        GameManager.Instance.AllAirports.Add(airport);
    }

    void CreatePurchaseButton(GameObject airportObject, int airportType)
    {
        GameObject buttonObject = Instantiate(
            buyButtonPrefab,
            airportObject.transform.position,
            Quaternion.identity
        );

        buttonObject.transform.SetParent(airportObject.transform);
        buttonObject.transform.localPosition = Vector3.zero;
        buttonObject.transform.localScale = Vector3.one;

        SpriteRenderer renderer = buttonObject.GetComponent<SpriteRenderer>();

        if (renderer != null)
        {
            renderer.sprite = GetSpriteForType(airportType);
            renderer.sortingLayerName = "Airports";
            renderer.sortingOrder = 11;
            renderer.color = GetPurchaseColorForType(airportType);

            ApplySpriteSize(renderer, GetSizeForType(airportType));

            float parentScale = airportObject.transform.localScale.x;

            if (parentScale != 0f)
                buttonObject.transform.localScale /= parentScale;
        }

        CircleCollider2D collider = buttonObject.GetComponent<CircleCollider2D>();

        if (collider != null)
            collider.radius = 1f;

        AirportActivator activator = buttonObject.GetComponent<AirportActivator>();

        if (activator != null)
            activator.pepe = airportObject.transform;
    }

    float CalculateBaseAirportPrice(AirportEntry entry)
    {
        float[] priceMultipliers =
        {
            1f,
            2.5f,
            6f
        };

        return entry.CountryPriceMultiplier *
               5_000_000f *
               priceMultipliers[entry.airportType];
    }

    Sprite GetSpriteForType(int airportType)
    {
        return airportType switch
        {
            0 => regionalSprite,
            1 => capitalSprite,
            2 => internationalSprite,
            _ => regionalSprite
        };
    }

    float GetSizeForType(int airportType)
    {
        return airportType switch
        {
            0 => regionalSize,
            1 => capitalSize,
            2 => internationalSize,
            _ => regionalSize
        };
    }

    Color GetPurchaseColorForType(int airportType)
    {
        return airportType switch
        {
            0 => Color.cyan,
            1 => Color.yellow,
            2 => Color.red,
            _ => Color.white
        };
    }

    string BuildCompactAirportName(string fullName)
    {
        string[] words = fullName.Split(' ');
        int wordCount = Mathf.Clamp(words.Length, 1, 5);

        return string.Join(" ", words, 0, wordCount);
    }

    void ApplySpriteSize(SpriteRenderer renderer, float desiredWorldSize)
    {
        if (renderer == null || renderer.sprite == null)
            return;

        float nativeSize = renderer.sprite.bounds.size.x;

        if (nativeSize == 0f)
            return;

        float scale = desiredWorldSize / nativeSize;
        renderer.transform.localScale = Vector3.one * scale;
    }
}
