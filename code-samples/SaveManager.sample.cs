using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// Curated excerpt from Transit Empire's JSON persistence layer.
/// The important design is staged reconstruction of Unity references:
/// airports first, routes second, aircraft last.
/// </summary>
public class SaveManagerSample : MonoBehaviour
{
    public GameObject planePrefab;
    private string savePath;

    void Awake()
    {
        savePath = Application.persistentDataPath + "/save.json";
    }

    public void SaveGame()
    {
        GameSave save = new GameSave
        {
            money = GameManager.Instance.Money,
            days = GameManager.Instance.Days,
            totalEarned = GameManager.Instance.TotalEarned,
            totalSpent = GameManager.Instance.TotalSpended
        };

        List<Airport> purchased = GameManager.Instance.GlobalApList;
        List<Airport> all = GameManager.Instance.AllAirports;

        for (int i = 0; i < purchased.Count; i++)
        {
            Airport airport = purchased[i];
            if (airport == null) continue;

            AirportSave snapshot = new AirportSave
            {
                listIndex = i,
                airportName = airport.AirportName,
                active = airport.IsAlreadyActive,
                level = airport.ApLvl,
                xp = airport.xp,
                statPoints = airport.statspoints,
                maxRunways = airport.MaxRunways,
                maxRouteSlots = airport.MaxRoutesSlots,
                reputation = airport.Reputation,
                maxReputation = airport.maxReputation,
                canGenerate = airport.CanGen,
                capacity = airport.capacity,
                capacityLevel = airport.capacityLevel
            };

            foreach (Airport route in airport.routes)
            {
                int destinationIndex = all.IndexOf(route);
                if (destinationIndex >= 0)
                    snapshot.routeIndexes.Add(destinationIndex);
            }

            save.airports.Add(snapshot);
        }

        foreach (Airport airport in purchased)
        {
            AirportGarage garage = airport.GetComponent<AirportGarage>();
            if (garage == null) continue;

            foreach (Plane plane in garage.EquippedPlanes)
            {
                if (plane == null) continue;

                save.planes.Add(new PlaneSave
                {
                    planeName = plane.PlaneName,
                    planeType = plane.planetype,
                    speedValue = plane.speedvalue,
                    consumptionValue = plane.consumevalue,
                    capacityValue = plane.capacityvalue,
                    rangeValue = plane.rangevalue,
                    reliabilityValue = plane.reliabilityvalue,
                    speed = plane.speed,
                    consumption = plane.consume,
                    capacity = plane.capac,
                    range = plane.range,
                    reliability = plane.reliability,
                    maxHP = plane.maxHP,
                    height = plane.height,
                    age = plane.age,
                    hp = plane.hp,
                    wear = plane.wear,
                    level = plane.level,
                    xp = plane.xp,
                    statPoints = plane.statPoints,
                    travels = plane.travels,
                    totalMoney = plane.totalmoney,
                    originIndex = all.IndexOf(airport),
                    destinationIndex = plane.cityB != null
                        ? all.IndexOf(plane.cityB.GetComponent<Airport>())
                        : -1,
                    state = plane.currentState
                });
            }
        }

        File.WriteAllText(savePath, JsonUtility.ToJson(save, true));
    }

    public void LoadGame()
    {
        if (!File.Exists(savePath))
            return;

        GameSave save = JsonUtility.FromJson<GameSave>(File.ReadAllText(savePath));

        List<Airport> purchased = GameManager.Instance.GlobalApList;
        List<Airport> all = GameManager.Instance.AllAirports;

        GameManager.Instance.Money = save.money;
        GameManager.Instance.Days = save.days;
        GameManager.Instance.TotalEarned = save.totalEarned;
        GameManager.Instance.TotalSpended = save.totalSpent;

        // Pass 1: rebuild purchased airports and restore their state.
        purchased.Clear();
        for (int i = 0; i < save.airports.Count; i++)
            purchased.Add(null);

        foreach (AirportSave snapshot in save.airports)
        {
            Airport airport = all.Find(a => a.AirportName == snapshot.airportName);
            if (airport == null) continue;

            purchased[snapshot.listIndex] = airport;
            airport.ApLvl = snapshot.level;
            airport.xp = snapshot.xp;
            airport.statspoints = snapshot.statPoints;
            airport.MaxRunways = snapshot.maxRunways;
            airport.MaxRoutesSlots = snapshot.maxRouteSlots;
            airport.Reputation = snapshot.reputation;
            airport.maxReputation = snapshot.maxReputation;
            airport.IsAlreadyActive = snapshot.active;
            airport.CanGen = snapshot.canGenerate;
            airport.capacity = snapshot.capacity;
            airport.capacityLevel = snapshot.capacityLevel;
            airport.routes.Clear();
        }

        // Pass 2: resolve persisted destination indexes back to Airport refs.
        foreach (AirportSave snapshot in save.airports)
        {
            Airport origin = purchased[snapshot.listIndex];
            if (origin == null) continue;

            foreach (int destinationIndex in snapshot.routeIndexes)
            {
                if (destinationIndex >= 0 && destinationIndex < all.Count)
                    origin.routes.Add(all[destinationIndex]);
            }
        }

        // Pass 3: instantiate aircraft and bind runtime references.
        foreach (PlaneSave snapshot in save.planes)
        {
            if (snapshot.originIndex < 0 || snapshot.originIndex >= all.Count)
                continue;

            Airport origin = all[snapshot.originIndex];
            Transform garageTransform = origin.transform.Find("Garage");
            if (garageTransform == null) continue;

            GameObject planeObject = Instantiate(planePrefab, garageTransform);
            Plane plane = planeObject.GetComponent<Plane>();

            plane.PlaneName = snapshot.planeName;
            plane.planetype = snapshot.planeType;
            plane.speedvalue = snapshot.speedValue;
            plane.consumevalue = snapshot.consumptionValue;
            plane.capacityvalue = snapshot.capacityValue;
            plane.rangevalue = snapshot.rangeValue;
            plane.reliabilityvalue = snapshot.reliabilityValue;
            plane.speed = snapshot.speed;
            plane.consume = snapshot.consumption;
            plane.capac = snapshot.capacity;
            plane.range = snapshot.range;
            plane.reliability = snapshot.reliability;
            plane.maxHP = snapshot.maxHP;
            plane.height = snapshot.height;
            plane.age = snapshot.age;
            plane.hp = snapshot.hp;
            plane.wear = snapshot.wear;
            plane.level = snapshot.level;
            plane.xp = snapshot.xp;
            plane.statPoints = snapshot.statPoints;
            plane.travels = snapshot.travels;
            plane.totalmoney = snapshot.totalMoney;
            plane.cityA = origin.transform;

            if (snapshot.destinationIndex >= 0 && snapshot.destinationIndex < all.Count)
            {
                Airport destination = all[snapshot.destinationIndex];
                if (destination != null && destination != origin)
                    plane.SetDestination(destination.transform);
            }

            plane.currentState = snapshot.state;
            origin.GetComponent<AirportGarage>().EquippedPlanes.Add(plane);
        }
    }
}

[System.Serializable]
public class GameSave
{
    public float money;
    public int days;
    public float totalEarned;
    public float totalSpent;
    public List<AirportSave> airports = new();
    public List<PlaneSave> planes = new();
}

[System.Serializable]
public class AirportSave
{
    public int listIndex;
    public string airportName;
    public bool active;
    public int level;
    public float xp;
    public int statPoints;
    public float maxRunways;
    public int maxRouteSlots;
    public float reputation;
    public float maxReputation;
    public bool canGenerate;
    public int capacity;
    public int capacityLevel;
    public List<int> routeIndexes = new();
}

[System.Serializable]
public class PlaneSave
{
    public string planeName;
    public int planeType;
    public int speedValue;
    public float consumptionValue;
    public int capacityValue;
    public int rangeValue;
    public int reliabilityValue;
    public float speed;
    public float consumption;
    public int capacity;
    public int range;
    public float reliability;
    public int maxHP;
    public float height;
    public int level;
    public float xp;
    public int statPoints;
    public int hp;
    public float wear;
    public float age;
    public int originIndex;
    public int destinationIndex;
    public int travels;
    public float totalMoney;
    public Plane.PlaneState state;
}
