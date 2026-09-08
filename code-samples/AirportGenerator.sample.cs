using UnityEngine;

/// <summary>
/// Curated excerpt from Transit Empire's archived AirportGenerator.
/// Demonstrates JSON-driven airport creation and runtime hierarchy setup.
/// </summary>
public class AirportGeneratorSample : MonoBehaviour
{
    [System.Serializable]
    public class AirportEntry
    {
        public string airportName;
        public float x;
        public float y;
        public string continent;
        public string country;
        public float CountryPriceMultiplier = 1f;
        public float PassMultiplier = 1f;
        public int airportType;
        public float[] multiplierssecond = { 1f, 1.5f, 2f };
    }

    public GameObject airportPrefab;
    public GameObject countryPrefab;
    public GameObject buyButtonPrefab;
    public GameObject buyAirportUi;
    public float airportScale = 0.5f;
    public float buttonScale = 0.4f;

    public void GenerateAirports()
    {
        TextAsset json = Resources.Load<TextAsset>("airports");
        if (json == null)
        {
            Debug.LogError("airports.json not found");
            return;
        }

        AirportEntry[] airports = JsonHelper.FromJson<AirportEntry>(json.text);
        GameManager.Instance.AllAirports.Clear();

        foreach (AirportEntry data in airports)
        {
            Transform continent = GameObject.Find(data.continent)?.transform;
            if (continent == null)
                continue;

            Transform country = continent.Find(data.country);
            if (country == null)
            {
                GameObject countryObject = Instantiate(countryPrefab);
                countryObject.name = data.country;
                countryObject.transform.SetParent(continent);

                Country countryState = countryObject.GetComponent<Country>();
                if (countryState != null)
                {
                    countryState.CountryNombre = data.country;
                    countryState.CostMultiplier = data.CountryPriceMultiplier;
                    countryState.multiplier = data.PassMultiplier;
                }

                country = countryObject.transform;
            }

            Vector3 position = new Vector3(data.x, data.y, 0f);
            GameObject airportObject = Instantiate(airportPrefab, position, Quaternion.identity);
            airportObject.name = data.airportName;
            airportObject.transform.SetParent(country);
            airportObject.transform.localScale = Vector3.one * airportScale;

            Airport airport = airportObject.GetComponent<Airport>();
            airport.BaseApPrice = data.CountryPriceMultiplier * 5_000_000f;
            airport.price = airport.BaseApPrice;
            airport.AirportName = data.airportName;
            airport.airportType = data.airportType;
            airport.Container = data.continent;
            airport.CountryHome = data.country;
            airport.secondarymultiplierBASE = data.multiplierssecond[data.airportType];
            airport.InitType();

            GameManager.Instance.AllAirports.Add(airport);

            airportObject.GetComponent<SpriteRenderer>().enabled = false;
            airportObject.GetComponent<Collider2D>().enabled = false;

            GameObject button = Instantiate(buyButtonPrefab, position, Quaternion.identity);
            button.transform.SetParent(airportObject.transform);
            button.transform.localScale = Vector3.one * buttonScale;
            button.transform.localPosition = Vector3.zero;

            AirportActivator activator = button.GetComponent<AirportActivator>();
            if (activator != null)
            {
                activator.BuyAirportUi = buyAirportUi;
                activator.pepe = airportObject.transform;
                activator.boolean = true;
            }
        }
    }
}
