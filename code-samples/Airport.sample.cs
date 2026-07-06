using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Representative sample extracted from Transit Empire's airport domain model.
/// 
/// This sample focuses on:
/// - airport visibility and ownership state
/// - passenger demand generation
/// - route demand distribution
/// - capacity pressure and reputation penalties
/// - route statistics
/// - XP, upgrades, infrastructure progression
/// - monthly maintenance debt
/// </summary>
public class AirportSample : MonoBehaviour
{
    public enum AirportType
    {
        Regional = 0,
        Capital = 1,
        International = 2
    }

    [Header("Identity")]
    public string AirportName;
    public string ContinentCode;
    public string CountryName;
    public AirportType Type;

    [Header("Ownership State")]
    public bool IsVisible;
    public bool IsOwned;
    public bool LoadedFromSave;

    [Header("Demand")]
    public int Passengers;
    public int Capacity;
    public bool CanGeneratePassengers;
    public float DemandWeight = 1f;
    public float LocalMultiplier = 1f;
    public float SecondaryMultiplier = 1f;

    public Dictionary<AirportSample, int> passengersByRoute =
        new Dictionary<AirportSample, int>();

    [Header("Routes")]
    public List<AirportSample> routes = new List<AirportSample>();
    public List<AirportSample> inboundRoutes = new List<AirportSample>();

    [System.Serializable]
    public class RouteStats
    {
        public int totalTravels;
        public float totalMoney;
        public int totalPassengers;
    }

    public Dictionary<AirportSample, RouteStats> routeStats =
        new Dictionary<AirportSample, RouteStats>();

    [Header("Reputation")]
    public float Reputation = 1f;
    public float MaxReputation = 1f;

    [Header("Infrastructure")]
    public int AirportLevel = 1;
    public float XP;
    public bool CanLevelUp;
    public int StatPoints;

    public float UsedRunways;
    public float MaxRunways;
    public int UsedRouteSlots;
    public int MaxRouteSlots;

    public int RunwayLevel = 1;
    public int RouteSlotLevel = 1;
    public int CapacityLevel = 1;
    public int ReputationLevel = 1;

    [Header("Economy")]
    public float BasePrice = 500_000f;
    public float Price;
    public float LevelUpCost;

    [Header("Maintenance")]
    public bool MaintenanceProtection = true;
    public float MaintenanceBaseCost;
    public float MaintenanceDebt;
    public float MaintenancePercent = 0.02f;
    public int UnpaidMaintenanceTicks;
    public int ConsecutivePayments;

    private float passengerTimer;

    void Awake()
    {
        AirportName = gameObject.name;
    }

    void Start()
    {
        if (!LoadedFromSave)
        {
            passengersByRoute = new Dictionary<AirportSample, int>();
            InitializeBaseStats();
        }

        RecalculateLevelUpCost();
    }

    void Update()
    {
        if (GameManager.Instance.Paused)
            return;

        passengerTimer += Time.deltaTime * GameManager.Instance.GameSpeed;

        if (passengerTimer >= GetGenerationInterval())
        {
            passengerTimer = 0f;
            GeneratePassengers();
        }
    }

    // ---------------------------------------------------------------------
    // Ownership / visibility
    // ---------------------------------------------------------------------

    public void Activate()
    {
        if (IsOwned)
            return;

        IsOwned = true;
        IsVisible = true;

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        Collider2D collider = GetComponent<Collider2D>();

        if (renderer != null)
            renderer.enabled = true;

        if (collider != null)
            collider.enabled = true;

        AirportActivator purchaseButton =
            GetComponentInChildren<AirportActivator>(true);

        if (purchaseButton != null)
            Destroy(purchaseButton.gameObject);

        InitializePassengerDictionary();
        InitializePassengerGeneration();
    }

    public void MakeVisible()
    {
        if (IsVisible)
            return;

        IsVisible = true;

        SpriteRenderer renderer = GetComponent<SpriteRenderer>();
        Collider2D collider = GetComponent<Collider2D>();

        if (renderer != null)
            renderer.enabled = false;

        if (collider != null)
            collider.enabled = false;
    }

    void InitializeBaseStats()
    {
        switch (Type)
        {
            case AirportType.Regional:
                Capacity = 500;
                MaxRunways = 10f;
                MaxRouteSlots = 3;
                MaxReputation = 1f;
                break;

            case AirportType.Capital:
                Capacity = 2_000;
                MaxRunways = 25f;
                MaxRouteSlots = 8;
                MaxReputation = 1.2f;
                break;

            case AirportType.International:
                Capacity = 8_000;
                MaxRunways = 60f;
                MaxRouteSlots = 20;
                MaxReputation = 1.5f;
                break;
        }
    }

    void InitializePassengerDictionary()
    {
        if (passengersByRoute == null)
            passengersByRoute = new Dictionary<AirportSample, int>();
    }

    void InitializePassengerGeneration()
    {
        passengerTimer = 0f;
    }

    // ---------------------------------------------------------------------
    // Passenger demand
    // ---------------------------------------------------------------------

    public void RefreshPassengerRoutes(List<AirportSample> ownedAirports)
    {
        InitializePassengerDictionary();

        foreach (AirportSample destination in ownedAirports)
        {
            if (destination == this)
                continue;

            if (!passengersByRoute.ContainsKey(destination))
                passengersByRoute.Add(destination, 0);
        }
    }

    void GeneratePassengers()
    {
        if (!CanGeneratePassengers)
            return;

        Reputation += 0.03f;
        Reputation = Mathf.Clamp(Reputation, 0f, MaxReputation);

        List<AirportSample> ownedAirports =
            GameManager.Instance.GlobalApList
                .Select(airport => airport.GetComponent<AirportSample>())
                .Where(airport => airport != null)
                .ToList();

        int generated = CalculateGeneratedPassengers();
        int remaining = generated;

        List<AirportSample> destinations =
            SelectDemandDestinations(ownedAirports);

        foreach (AirportSample destination in destinations)
        {
            if (remaining <= 0)
                break;

            if (destination == this)
                continue;

            if (!passengersByRoute.ContainsKey(destination))
                passengersByRoute.Add(destination, 0);

            int portion = Mathf.Clamp(
                Mathf.RoundToInt(Random.Range(0.3f, 0.7f) * remaining * destination.DemandWeight),
                0,
                remaining
            );

            passengersByRoute[destination] += portion;
            remaining -= portion;
        }

        RecalculatePassengers();
        ClampPassengersToCapacity();
        ApplyOccupancyPressure();
        UpdateVisualState();
    }

    int CalculateGeneratedPassengers()
    {
        float continentMultiplier = GetContinentMultiplier(ContinentCode);
        float reputationEffect = Mathf.Pow(Reputation, 0.3f);

        float countryMultiplier =
            GetCountryMultiplier() *
            reputationEffect;

        float airportMultiplier =
            LocalMultiplier *
            reputationEffect *
            SecondaryMultiplier;

        float basePassengers = 0.4f;

        return Mathf.RoundToInt(
            basePassengers *
            continentMultiplier *
            countryMultiplier *
            airportMultiplier
        );
    }

    List<AirportSample> SelectDemandDestinations(List<AirportSample> ownedAirports)
    {
        switch (Type)
        {
            case AirportType.Regional:
                return ownedAirports
                    .Where(airport => airport.CountryName == CountryName)
                    .OrderBy(airport => Vector2.Distance(transform.position, airport.transform.position))
                    .Take(25)
                    .ToList();

            case AirportType.Capital:
                return ownedAirports
                    .Where(airport => airport.ContinentCode == ContinentCode && airport != this)
                    .OrderBy(airport => Vector2.Distance(transform.position, airport.transform.position))
                    .Take(50)
                    .ToList();

            case AirportType.International:
                return ownedAirports
                    .Where(airport => airport.Type != AirportType.Regional)
                    .OrderBy(_ => Random.value)
                    .Take(10)
                    .ToList();

            default:
                return ownedAirports;
        }
    }

    public void RecalculatePassengers()
    {
        Passengers = 0;

        foreach (var entry in passengersByRoute)
            Passengers += entry.Value;
    }

    void ClampPassengersToCapacity()
    {
        if (Passengers <= Capacity)
            return;

        int overflow = Passengers - Capacity;

        foreach (AirportSample destination in passengersByRoute.Keys.ToList())
        {
            if (overflow <= 0)
                break;

            int reduction = Mathf.Min(passengersByRoute[destination], overflow);

            passengersByRoute[destination] -= reduction;
            overflow -= reduction;
        }

        Passengers = Capacity;
    }

    void ApplyOccupancyPressure()
    {
        float occupancy = Capacity > 0
            ? (float)Passengers / Capacity
            : 0f;

        if (occupancy >= 0.98f)
            ApplyReputationPenalty(3);
        else if (occupancy >= 0.90f)
            ApplyReputationPenalty(2);
        else if (occupancy >= 0.80f)
            ApplyReputationPenalty(1);
    }

    void ApplyReputationPenalty(int severity)
    {
        float[] typeMultiplier =
        {
            0.5f,
            1f,
            1.5f
        };

        float multiplier = typeMultiplier[(int)Type];

        switch (severity)
        {
            case 1:
                Reputation -= 0.02f * multiplier;
                break;

            case 2:
                Reputation -= 0.04f * multiplier;
                break;

            case 3:
                Reputation -= 0.08f * multiplier;
                break;
        }

        Reputation = Mathf.Clamp(Reputation, 0f, MaxReputation);
    }

    void UpdateVisualState()
    {
        SpriteRenderer renderer = GetComponent<SpriteRenderer>();

        if (renderer == null || Capacity <= 0)
            return;

        float occupancy = (float)Passengers / Capacity;

        if (occupancy >= 0.85f)
            renderer.color = Color.red;
        else if (occupancy >= 0.65f)
            renderer.color = new Color(1f, 0.5f, 0f);
        else
            renderer.color = Color.blue;
    }

    // ---------------------------------------------------------------------
    // Progression
    // ---------------------------------------------------------------------

    public void AddXP(float amount)
    {
        XP += amount;

        if (XP < GetXPRequired())
            return;

        CanLevelUp = true;
    }

    public bool UpgradeAirport()
    {
        if (!CanLevelUp)
            return false;

        RecalculateLevelUpCost();

        if (GameManager.Instance.Money < LevelUpCost)
            return false;

        XP -= GetXPRequired();
        AirportLevel++;

        CanLevelUp = false;
        StatPoints += 3 + AirportLevel / 3;

        LocalMultiplier = AirportLevel;

        GameManager.Instance.CashMovement(-LevelUpCost);

        RecalculateLevelUpCost();

        return true;
    }

    public void UpgradeInfrastructure(int upgradeIndex)
    {
        float runwayIncrease = Type switch
        {
            AirportType.Regional => 3f,
            AirportType.Capital => 8f,
            AirportType.International => 15f,
            _ => 3f
        };

        int routeSlotIncrease = Type switch
        {
            AirportType.Regional => 1,
            AirportType.Capital => 2,
            AirportType.International => 4,
            _ => 1
        };

        int capacityIncrease = Type switch
        {
            AirportType.Regional => 200,
            AirportType.Capital => 500,
            AirportType.International => 1500,
            _ => 200
        };

        float reputationIncrease = Type switch
        {
            AirportType.Regional => 0.05f,
            AirportType.Capital => 0.07f,
            AirportType.International => 0.10f,
            _ => 0.05f
        };

        int cost = GetInfrastructureUpgradeCost(upgradeIndex);

        if (StatPoints < cost)
            return;

        switch (upgradeIndex)
        {
            case 0:
                MaxRunways += runwayIncrease * (1f + RunwayLevel * 0.15f);
                RunwayLevel++;
                break;

            case 1:
                MaxRouteSlots += Mathf.RoundToInt(routeSlotIncrease * (1f + RouteSlotLevel * 0.15f));
                RouteSlotLevel++;
                break;

            case 2:
                CapacityLevel++;
                Capacity += Mathf.RoundToInt(capacityIncrease * (1f + CapacityLevel * 0.15f));
                break;

            case 3:
                MaxReputation += reputationIncrease;
                ReputationLevel++;
                break;
        }

        StatPoints -= cost;
    }

    int GetInfrastructureUpgradeCost(int upgradeIndex)
    {
        int currentValue = upgradeIndex switch
        {
            0 => Mathf.RoundToInt(MaxRunways / 9f),
            1 => MaxRouteSlots / 4,
            2 => CapacityLevel,
            3 => Mathf.RoundToInt(Reputation),
            _ => 1
        };

        return Mathf.Max(1, currentValue / 3);
    }

    int GetXPRequired()
    {
        return Mathf.RoundToInt(800 * Mathf.Pow(AirportLevel, 1.9f));
    }

    void RecalculateLevelUpCost()
    {
        LevelUpCost = Mathf.RoundToInt(
            BasePrice *
            0.1f *
            Mathf.Pow(AirportLevel, 1.5f)
        );
    }

    public float XPPercent()
    {
        return GetXPRequired() > 0
            ? XP / GetXPRequired()
            : 0f;
    }

    // ---------------------------------------------------------------------
    // Maintenance
    // ---------------------------------------------------------------------

    public void CalculateMonthlyMaintenance()
    {
        if (MaintenanceProtection)
        {
            UnpaidMaintenanceTicks = 0;
            MaintenanceDebt = 0f;
            MaintenanceProtection = false;
            return;
        }

        float levelMultiplier = 1f + AirportLevel * 0.04f;

        MaintenanceDebt +=
            MaintenanceBaseCost *
            levelMultiplier *
            MaintenancePercent;

        if (UnpaidMaintenanceTicks >= 3 &&
            (UnpaidMaintenanceTicks - 3) % 2 == 0)
        {
            MaintenancePercent += 0.01f;
        }
    }

    public bool PayMaintenance()
    {
        if (GameManager.Instance.Money < MaintenanceDebt)
            return false;

        GameManager.Instance.CashMovement(-MaintenanceDebt);

        MaintenanceDebt = 0f;
        UnpaidMaintenanceTicks = 0;
        ConsecutivePayments++;

        return true;
    }

    // ---------------------------------------------------------------------
    // Utilities
    // ---------------------------------------------------------------------

    float GetGenerationInterval()
    {
        return Type switch
        {
            AirportType.Regional => 5f,
            AirportType.Capital => 3f,
            AirportType.International => 1f,
            _ => 5f
        };
    }

    float GetContinentMultiplier(string continentCode)
    {
        return continentCode switch
        {
            "NA" => 2.2f,
            "SA" => 1.4f,
            "EP" => 2.5f,
            "A" => 2.3f,
            "O" => 1.2f,
            "AFR" => 0.9f,
            "SB/ARTIC" => 0.4f,
            _ => 1f
        };
    }

    float GetCountryMultiplier()
    {
        Country country = GetComponentInParent<Country>();

        if (country == null)
            return 1f;

        return country.multiplier;
    }
}
