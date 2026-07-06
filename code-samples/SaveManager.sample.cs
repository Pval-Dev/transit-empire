using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Representative sample from Transit Empire's persistence system.
/// 
/// This sample shows the core idea:
/// 1. Convert runtime state into serializable DTOs.
/// 2. Store references by stable names instead of Unity object references.
/// 3. Write the snapshot as JSON.
/// 4. Rebuild the world first during load.
/// 5. Restore references through a multi-pass reconstruction process.
/// </summary>
public class SaveManagerSample : MonoBehaviour
{
    public static SaveManagerSample Instance;

    [Header("Runtime Prefabs")]
    public GameObject planePrefab;

    [Header("World Builders")]
    public AirportGenerator airportGenerator;
    public CountryBorderGenerator countryBorderGenerator;

    private int currentSlot = 1;
    private string savePath;

    private void Awake()
    {
        Instance = this;
        DontDestroyOnLoad(gameObject);
        SetSlot(1);
    }

    public void SetSlot(int slot)
    {
        currentSlot = slot;
        savePath = Path.Combine(
            Application.persistentDataPath,
            $"save_slot{slot}.json"
        );
    }

    // ---------------------------------------------------------------------
    // Save
    // ---------------------------------------------------------------------

    public void SaveGame()
    {
        GameSaveData save = new GameSaveData();

        SaveGlobalState(save);
        SaveFinanceState(save);
        SaveCountries(save);
        SaveAirports(save);
        SavePlanes(save);

        string json = JsonUtility.ToJson(save, true);
        File.WriteAllText(savePath, json);

        Debug.Log($"Save written to: {savePath}");
    }

    private void SaveGlobalState(GameSaveData save)
    {
        GameManager gm = GameManager.Instance;

        save.money = gm.Money;
        save.days = gm.Days;
        save.totalEarned = gm.TotalEarned;
        save.totalSpent = gm.TotalSpended;

        save.globalReputation = gm.GlobalReputation;
        save.airlineName = gm.AirlineName;
        save.autonomy = gm.Autonomy;
        save.routeCostMultiplier = gm.DirectorRTCostMultiplicator;
    }

    private void SaveFinanceState(GameSaveData save)
    {
        LoanManager loans = GameManager.Instance.LoanManager;

        save.loanPenalty = loans.PenaltyUmbrall;
        save.currentLoans = loans.CurrentLoans;
        save.investorAttempts = loans.attemps;

        save.perCapitaBalances = new List<float>(loans.PerCapitaActives);
        save.perCapitaPayments = new List<float>(loans.DailyPayCapita);
        save.perCapitaAutoPay = new List<bool>(loans.PerCapitaDailyActive);
        save.perCapitaNames = new List<string>(loans.PerCapitaNames);

        save.perAvgBalances = new List<float>(loans.PerAvgActives);
        save.perAvgPayments = new List<float>(loans.DailyPayAvg);
        save.perAvgAutoPay = new List<bool>(loans.PerAvgDailyActive);
        save.perAvgNames = new List<string>(loans.PerAvgNames);

        save.investorBalances = new List<float>(loans.InvestorsActives);
        save.investorShares = new List<float>(loans.DailyPayInvestors);
        save.investorNames = new List<string>(loans.InvestorNames2);
    }

    private void SaveCountries(GameSaveData save)
    {
        Country[] countries = FindObjectsByType<Country>(FindObjectsInactive.Include);

        foreach (Country country in countries)
        {
            save.countries.Add(new CountrySaveData
            {
                countryName = country.CountryNombre,
                activeState = country.ActiveState,
                totalGenerated = country.TotalGenerated,
                airportsOwned = country.ApOwned,
                averageReputation = country.REPtotal,

                regionalProgress = country.RegionalUpdating,
                capitalProgress = country.CapitalUpdating,
                internationalProgress = country.InterNationalUpdating,

                regionalThreshold = country.umbralRegional,
                capitalThreshold = country.umbralCapital,
                internationalThreshold = country.umbralInternacional
            });
        }
    }

    private void SaveAirports(GameSaveData save)
    {
        GameManager gm = GameManager.Instance;

        foreach (Airport airport in gm.GlobalApList)
        {
            AirportSaveData airportSave = BuildAirportSave(airport);
            airportSave.active = true;
            airportSave.visible = true;

            save.airports.Add(airportSave);
        }

        foreach (Airport airport in gm.VisibleApList)
        {
            save.airports.Add(new AirportSaveData
            {
                airportName = airport.AirportName,
                active = false,
                visible = true
            });
        }
    }

    private AirportSaveData BuildAirportSave(Airport airport)
    {
        AirportSaveData save = new AirportSaveData
        {
            airportName = airport.AirportName,
            level = airport.ApLvl,
            xp = airport.xp,
            statPoints = airport.statspoints,
            capacity = airport.capacity,
            capacityLevel = airport.capacityLevel,
            reputation = airport.Reputation,
            maxReputation = airport.maxReputation,
            maxRunways = airport.MaxRunways,
            usedRunways = airport.UsedRunWays,
            maxRouteSlots = airport.MaxRoutesSlots,
            usedRouteSlots = airport.RoutesSlots,
            canGeneratePassengers = airport.CanGen,
            maintenanceDebt = airport.MoneyForMaintenanceUi,
            maintenancePercent = airport.Porcent,
            unpaidTicks = airport.ticksStart,
            consecutivePayments = airport.consecutivePayments,
            protectedFromMaintenance = airport.Protection,
            canLevelUp = airport.CanLvlUp
        };

        foreach (Airport route in airport.routes)
        {
            if (route != null)
                save.routeNames.Add(route.AirportName);
        }

        if (airport.passengersByRoute != null)
        {
            foreach (var pair in airport.passengersByRoute)
            {
                if (pair.Key == null)
                    continue;

                save.passengerDestinationNames.Add(pair.Key.AirportName);
                save.passengerValues.Add(pair.Value);
            }
        }

        return save;
    }

    private void SavePlanes(GameSaveData save)
    {
        GameManager gm = GameManager.Instance;

        foreach (Airport airport in gm.GlobalApList)
        {
            AirportGarage garage = airport.GetComponent<AirportGarage>();
            if (garage == null)
                continue;

            foreach (Plane plane in garage.EquippedPlanes)
            {
                PlaneSaveData planeSave = BuildPlaneSave(plane, airport.AirportName);
                planeSave.inRoute = false;
                save.planes.Add(planeSave);
            }
        }

        foreach (Plane plane in gm.PlanesInRoute)
        {
            string originName = plane.cityA != null
                ? plane.cityA.GetComponent<Airport>().AirportName
                : string.Empty;

            PlaneSaveData planeSave = BuildPlaneSave(plane, originName);
            planeSave.inRoute = true;
            save.planes.Add(planeSave);
        }
    }

    private PlaneSaveData BuildPlaneSave(Plane plane, string originAirportName)
    {
        return new PlaneSaveData
        {
            planeName = plane.PlaneName,
            planeType = plane.planetype,

            speedValue = plane.speedvalue,
            consumeValue = plane.consumevalue,
            capacityValue = plane.capacityvalue,
            rangeValue = plane.rangevalue,
            reliabilityValue = plane.reliabilityvalue,

            speed = plane.speed,
            consume = plane.consume,
            capacity = plane.capac,
            range = plane.range,
            reliability = plane.reliability,
            height = plane.height,

            hp = plane.hp,
            maxHp = plane.maxHP,
            wear = plane.wear,
            age = plane.age,

            level = plane.level,
            xp = plane.xp,
            statPoints = plane.statPoints,

            travels = plane.travels,
            totalMoney = plane.totalmoney,
            routeTravels = plane.routeTravels,
            routeMoney = plane.routeMoney,
            routePassengers = plane.routePass,

            originAirportName = originAirportName,
            destinationAirportName = plane.cityB != null
                ? plane.cityB.GetComponent<Airport>().AirportName
                : string.Empty,

            savedState = plane.currentState
        };
    }

    // ---------------------------------------------------------------------
    // Load
    // ---------------------------------------------------------------------

    public void LoadGame()
    {
        StartCoroutine(LoadAfterWorldReset());
    }

    private IEnumerator LoadAfterWorldReset()
    {
        if (!File.Exists(savePath))
        {
            Debug.LogWarning($"No save file found at: {savePath}");
            yield break;
        }

        // In the full project, the world can be regenerated before restoring data.
        // This ensures countries and airports exist before routes and planes are reconnected.
        RegenerateWorld();

        yield return new WaitForSecondsRealtime(1f);

        string json = File.ReadAllText(savePath);
        GameSaveData save = JsonUtility.FromJson<GameSaveData>(json);

        Dictionary<string, Airport> airportByName =
            BuildAirportLookup(GameManager.Instance.AllAirports);

        RestoreGlobalState(save);
        RestoreFinanceState(save);
        RestoreCountries(save);
        RestoreAirports(save, airportByName);
        RestoreRoutes(save, airportByName);
        RestorePassengerMaps(save, airportByName);
        RestorePlanes(save, airportByName);

        Debug.Log("Save loaded successfully.");
    }

    private void RegenerateWorld()
    {
        GameManager gm = GameManager.Instance;

        gm.GlobalApList.Clear();
        gm.VisibleApList.Clear();
        gm.PlanesInRoute.Clear();
        gm.EquippedPlanes.Clear();
        gm.AllAirports.Clear();

        if (countryBorderGenerator != null)
            countryBorderGenerator.LoadingInGame = true;

        if (airportGenerator != null)
            airportGenerator.GenerateAirports();
    }

    private Dictionary<string, Airport> BuildAirportLookup(List<Airport> airports)
    {
        Dictionary<string, Airport> lookup = new Dictionary<string, Airport>();

        foreach (Airport airport in airports)
        {
            if (airport == null)
                continue;

            if (!lookup.ContainsKey(airport.AirportName))
                lookup.Add(airport.AirportName, airport);
        }

        return lookup;
    }

    private void RestoreGlobalState(GameSaveData save)
    {
        GameManager gm = GameManager.Instance;

        gm.Money = save.money;
        gm.Days = save.days;
        gm.TotalEarned = save.totalEarned;
        gm.TotalSpended = save.totalSpent;

        gm.GlobalReputation = save.globalReputation;
        gm.AirlineName = save.airlineName;
        gm.Autonomy = save.autonomy;
        gm.DirectorRTCostMultiplicator = save.routeCostMultiplier;
    }

    private void RestoreFinanceState(GameSaveData save)
    {
        LoanManager loans = GameManager.Instance.LoanManager;

        loans.PenaltyUmbrall = save.loanPenalty;
        loans.CurrentLoans = save.currentLoans;
        loans.attemps = save.investorAttempts;

        loans.PerCapitaActives = new List<float>(save.perCapitaBalances);
        loans.DailyPayCapita = new List<float>(save.perCapitaPayments);
        loans.PerCapitaDailyActive = new List<bool>(save.perCapitaAutoPay);
        loans.PerCapitaNames = new List<string>(save.perCapitaNames);

        loans.PerAvgActives = new List<float>(save.perAvgBalances);
        loans.DailyPayAvg = new List<float>(save.perAvgPayments);
        loans.PerAvgDailyActive = new List<bool>(save.perAvgAutoPay);
        loans.PerAvgNames = new List<string>(save.perAvgNames);

        loans.InvestorsActives = new List<float>(save.investorBalances);
        loans.DailyPayInvestors = new List<float>(save.investorShares);
        loans.InvestorNames2 = new List<string>(save.investorNames);
    }

    private void RestoreCountries(GameSaveData save)
    {
        Country[] countries = FindObjectsByType<Country>(FindObjectsInactive.Include);

        foreach (CountrySaveData countrySave in save.countries)
        {
            Country country = System.Array.Find(
                countries,
                c => c.CountryNombre == countrySave.countryName
            );

            if (country == null)
                continue;

            country.ActiveState = countrySave.activeState;
            country.TotalGenerated = countrySave.totalGenerated;
            country.ApOwned = countrySave.airportsOwned;
            country.REPtotal = countrySave.averageReputation;

            country.RegionalUpdating = countrySave.regionalProgress;
            country.CapitalUpdating = countrySave.capitalProgress;
            country.InterNationalUpdating = countrySave.internationalProgress;

            country.umbralRegional = countrySave.regionalThreshold;
            country.umbralCapital = countrySave.capitalThreshold;
            country.umbralInternacional = countrySave.internationalThreshold;
        }
    }

    private void RestoreAirports(
        GameSaveData save,
        Dictionary<string, Airport> airportByName)
    {
        GameManager gm = GameManager.Instance;
        gm.GlobalApList.Clear();

        foreach (AirportSaveData airportSave in save.airports)
        {
            if (!airportByName.TryGetValue(airportSave.airportName, out Airport airport))
                continue;

            if (airportSave.active)
            {
                airport.loadedFromSave = true;
                airport.Activate();

                airport.IsAlreadyActive = true;
                airport.IsVisible = true;

                airport.ApLvl = airportSave.level;
                airport.xp = airportSave.xp;
                airport.statspoints = airportSave.statPoints;
                airport.capacity = airportSave.capacity;
                airport.capacityLevel = airportSave.capacityLevel;

                airport.Reputation = airportSave.reputation;
                airport.maxReputation = airportSave.maxReputation;

                airport.MaxRunways = airportSave.maxRunways;
                airport.UsedRunWays = airportSave.usedRunways;
                airport.MaxRoutesSlots = airportSave.maxRouteSlots;
                airport.RoutesSlots = airportSave.usedRouteSlots;

                airport.CanGen = airportSave.canGeneratePassengers;
                airport.MoneyForMaintenanceUi = airportSave.maintenanceDebt;
                airport.Porcent = airportSave.maintenancePercent;
                airport.ticksStart = airportSave.unpaidTicks;
                airport.consecutivePayments = airportSave.consecutivePayments;
                airport.Protection = airportSave.protectedFromMaintenance;
                airport.CanLvlUp = airportSave.canLevelUp;

                airport.routes.Clear();
                gm.GlobalApList.Add(airport);
            }
            else if (airportSave.visible)
            {
                airport.MakeVisible();

                if (!gm.VisibleApList.Contains(airport))
                    gm.VisibleApList.Add(airport);
            }
        }
    }

    private void RestoreRoutes(
        GameSaveData save,
        Dictionary<string, Airport> airportByName)
    {
        foreach (AirportSaveData airportSave in save.airports)
        {
            if (!airportByName.TryGetValue(airportSave.airportName, out Airport origin))
                continue;

            foreach (string routeName in airportSave.routeNames)
            {
                if (airportByName.TryGetValue(routeName, out Airport destination))
                    origin.routes.Add(destination);
            }
        }
    }

    private void RestorePassengerMaps(
        GameSaveData save,
        Dictionary<string, Airport> airportByName)
    {
        foreach (AirportSaveData airportSave in save.airports)
        {
            if (!airportSave.active)
                continue;

            if (!airportByName.TryGetValue(airportSave.airportName, out Airport airport))
                continue;

            airport.passengersByRoute = new Dictionary<Airport, int>();

            for (int i = 0; i < airportSave.passengerDestinationNames.Count; i++)
            {
                string destinationName = airportSave.passengerDestinationNames[i];

                if (!airportByName.TryGetValue(destinationName, out Airport destination))
                    continue;

                airport.passengersByRoute[destination] = airportSave.passengerValues[i];
            }

            airport.RecalculatePassengers();
        }
    }

    private void RestorePlanes(
        GameSaveData save,
        Dictionary<string, Airport> airportByName)
    {
        foreach (PlaneSaveData planeSave in save.planes)
        {
            if (!airportByName.TryGetValue(planeSave.originAirportName, out Airport origin))
                continue;

            AirportGarage garage = origin.GetComponent<AirportGarage>();
            Transform garageTransform = origin.transform.Find("Garage");

            if (garage == null || garageTransform == null)
                continue;

            GameObject planeObject = Instantiate(planePrefab, garageTransform);
            Plane plane = planeObject.GetComponent<Plane>();

            ApplyPlaneSaveData(plane, planeSave, origin, airportByName);

            if (planeSave.inRoute)
            {
                planeObject.transform.SetParent(null);
                planeObject.transform.position = origin.transform.position;
                GameManager.Instance.PlanesInRoute.Add(plane);
            }
            else
            {
                garage.EquippedPlanes.Add(plane);
            }
        }
    }

    private void ApplyPlaneSaveData(
        Plane plane,
        PlaneSaveData save,
        Airport origin,
        Dictionary<string, Airport> airportByName)
    {
        plane.loadedFromSave = true;

        plane.PlaneName = save.planeName;
        plane.planetype = save.planeType;

        plane.speedvalue = save.speedValue;
        plane.consumevalue = save.consumeValue;
        plane.capacityvalue = save.capacityValue;
        plane.rangevalue = save.rangeValue;
        plane.reliabilityvalue = save.reliabilityValue;

        plane.speed = save.speed;
        plane.consume = save.consume;
        plane.capac = save.capacity;
        plane.range = save.range;
        plane.reliability = save.reliability;
        plane.height = save.height;

        plane.hp = save.hp;
        plane.maxHP = save.maxHp;
        plane.wear = save.wear;
        plane.age = save.age;

        plane.level = save.level;
        plane.xp = save.xp;
        plane.statPoints = save.statPoints;

        plane.travels = save.travels;
        plane.totalmoney = save.totalMoney;
        plane.routeTravels = save.routeTravels;
        plane.routeMoney = save.routeMoney;
        plane.routePass = save.routePassengers;

        plane.cityA = origin.transform;

        if (airportByName.TryGetValue(save.destinationAirportName, out Airport destination))
        {
            plane.cityB = destination.transform;
            plane.SetDestination(destination.transform);
        }

        plane.currentState = save.savedState;
    }

    // ---------------------------------------------------------------------
    // Slot helpers
    // ---------------------------------------------------------------------

    public bool SaveExists()
    {
        return File.Exists(savePath);
    }

    public void DeleteSave()
    {
        if (File.Exists(savePath))
            File.Delete(savePath);
    }

    public GameSaveData PeekSlot(int slot)
    {
        string path = Path.Combine(
            Application.persistentDataPath,
            $"save_slot{slot}.json"
        );

        if (!File.Exists(path))
            return null;

        return JsonUtility.FromJson<GameSaveData>(File.ReadAllText(path));
    }
}

// -------------------------------------------------------------------------
// Serializable DTOs
// -------------------------------------------------------------------------

[System.Serializable]
public class GameSaveData
{
    public float money;
    public int days;
    public float totalEarned;
    public float totalSpent;

    public float globalReputation;
    public string airlineName;
    public float autonomy;
    public float routeCostMultiplier;

    public float loanPenalty;
    public int currentLoans;
    public int investorAttempts;

    public List<float> perCapitaBalances = new List<float>();
    public List<float> perCapitaPayments = new List<float>();
    public List<bool> perCapitaAutoPay = new List<bool>();
    public List<string> perCapitaNames = new List<string>();

    public List<float> perAvgBalances = new List<float>();
    public List<float> perAvgPayments = new List<float>();
    public List<bool> perAvgAutoPay = new List<bool>();
    public List<string> perAvgNames = new List<string>();

    public List<float> investorBalances = new List<float>();
    public List<float> investorShares = new List<float>();
    public List<string> investorNames = new List<string>();

    public List<CountrySaveData> countries = new List<CountrySaveData>();
    public List<AirportSaveData> airports = new List<AirportSaveData>();
    public List<PlaneSaveData> planes = new List<PlaneSaveData>();
}

[System.Serializable]
public class CountrySaveData
{
    public string countryName;
    public int activeState;
    public float totalGenerated;
    public float airportsOwned;
    public float averageReputation;

    public float regionalProgress;
    public float capitalProgress;
    public float internationalProgress;

    public float regionalThreshold;
    public float capitalThreshold;
    public float internationalThreshold;
}

[System.Serializable]
public class AirportSaveData
{
    public string airportName;
    public bool active;
    public bool visible;

    public int level;
    public float xp;
    public int statPoints;

    public int capacity;
    public int capacityLevel;

    public float reputation;
    public float maxReputation;

    public float maxRunways;
    public float usedRunways;

    public int maxRouteSlots;
    public int usedRouteSlots;

    public bool canGeneratePassengers;

    public float maintenanceDebt;
    public float maintenancePercent;
    public int unpaidTicks;
    public int consecutivePayments;
    public bool protectedFromMaintenance;
    public bool canLevelUp;

    public List<string> routeNames = new List<string>();

    public List<string> passengerDestinationNames = new List<string>();
    public List<int> passengerValues = new List<int>();
}

[System.Serializable]
public class PlaneSaveData
{
    public string planeName;
    public int planeType;

    public int speedValue;
    public float consumeValue;
    public int capacityValue;
    public int rangeValue;
    public int reliabilityValue;

    public float speed;
    public float consume;
    public int capacity;
    public int range;
    public float reliability;
    public float height;

    public int hp;
    public int maxHp;
    public float wear;
    public float age;

    public int level;
    public float xp;
    public int statPoints;

    public int travels;
    public float totalMoney;

    public int routeTravels;
    public float routeMoney;
    public int routePassengers;

    public string originAirportName;
    public string destinationAirportName;

    public Plane.PlaneState savedState;
    public bool inRoute;
}
