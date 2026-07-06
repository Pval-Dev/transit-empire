using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Representative sample extracted from Transit Empire's aircraft runtime model.
///
/// This sample focuses on the operational aircraft lifecycle:
/// boarding, route traversal, arrival revenue, durability degradation,
/// repair handling, and progression.
/// </summary>
public class PlaneStateMachineSample : MonoBehaviour
{
    public enum AircraftState
    {
        Idle,
        Boarding,
        Flying,
        Arrived,
        Waiting,
        Turnaround,
        OutOfService
    }

    public const float WORLD_TO_MILES = 69.17f;

    [Header("Identity")]
    public string PlaneName;
    public int PlaneType;
    public int Level = 1;

    [Header("Route")]
    public Transform Origin;
    public Transform Destination;
    public int Miles;

    [Header("Capacity")]
    public int Capacity;
    public int PassengersOnBoard;
    public int ConnectionPassengers;
    public float DepartureThreshold = 0.3f;

    [Header("Performance")]
    public float Speed;
    public float FuelConsumption;
    public int Range;
    public float Reliability;

    [Header("Durability")]
    public int HP;
    public int MaxHP;
    public float Age;
    public float Wear;
    public bool MaintenanceMode;
    public bool CanRepair;
    public float RepairCostPerHP = 500f;

    [Header("Runtime Statistics")]
    public int TotalTrips;
    public float TotalRevenue;
    public float AverageRevenue;
    public int RouteTrips;
    public float RouteRevenue;
    public int RoutePassengers;

    [Header("Progression")]
    public float XP;
    public int StatPoints;
    public int XPRequired => Mathf.RoundToInt(500 * Mathf.Pow(Level, 1.8f));

    public AircraftState CurrentState;

    private bool goingForward = true;
    private Transform currentDestination;

    private Vector3 routeFrom;
    private Vector3 routeControl;
    private float routeT;
    private float routeTotalDistance = 1f;

    private float waitTimer;
    private float turnaroundTime = 3f;
    private float repairTimer;
    private float repairDuration;
    private float reliabilityBase;
    private float distanceAccumulator;

    private readonly Dictionary<Airport, int> connectionManifest = new Dictionary<Airport, int>();
    private readonly List<Airport> routeKeyCache = new List<Airport>();

    private SpriteRenderer spriteRenderer;

    void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    void Start()
    {
        currentDestination = Destination;
        reliabilityBase = Reliability;

        if (Origin != null && Destination != null)
        {
            Miles = CalculateMiles(Origin.position, Destination.position);
            BuildBezierRoute(Origin.position, Destination.position);
            SetState(AircraftState.Boarding);
        }
        else
        {
            SetState(AircraftState.Idle);
        }
    }

    void Update()
    {
        switch (CurrentState)
        {
            case AircraftState.Boarding:
                HandleBoarding();
                break;

            case AircraftState.Flying:
                HandleFlying();
                break;

            case AircraftState.Arrived:
                HandleArrival();
                break;

            case AircraftState.Waiting:
                HandleWaiting();
                break;

            case AircraftState.Turnaround:
                HandleTurnaround();
                break;

            case AircraftState.OutOfService:
                HandleRepair();
                break;
        }
    }

    public void ConfigureFromCatalog(AircraftCatalogData data)
    {
        PlaneName = data.Name;
        PlaneType = data.Type;

        Speed = data.Speed;
        FuelConsumption = data.FuelConsumption;
        Capacity = data.Capacity;
        Range = data.Range;
        Reliability = data.Reliability;

        reliabilityBase = Reliability;

        MaxHP = Mathf.RoundToInt(Capacity * 8f + Mathf.Clamp(Reliability, 0f, 100f) * 5f);
        HP = MaxHP;
        Wear = 0f;
    }

    public void AssignDestination(Transform destination)
    {
        if (Origin == null || destination == null)
            return;

        Destination = destination;
        currentDestination = destination;
        Miles = CalculateMiles(Origin.position, Destination.position);

        if (Destination == Origin)
        {
            SetState(AircraftState.Idle);
            return;
        }

        BuildBezierRoute(transform.position, Destination.position);

        if (CurrentState == AircraftState.Idle)
            SetState(AircraftState.Boarding);
    }

    void HandleBoarding()
    {
        if (Origin == null || Destination == null)
        {
            SetState(AircraftState.Idle);
            return;
        }

        if (Miles > Range)
        {
            SetState(AircraftState.Idle);
            return;
        }

        Transform departureTransform = goingForward ? Origin : Destination;
        Transform arrivalTransform = goingForward ? Destination : Origin;

        Airport departureAirport = departureTransform.GetComponent<Airport>();
        Airport arrivalAirport = arrivalTransform.GetComponent<Airport>();

        if (departureAirport == null || arrivalAirport == null)
            return;

        EnsurePassengerRoute(departureAirport, arrivalAirport);

        int directAvailable = departureAirport.passengersByRoute.ContainsKey(arrivalAirport)
            ? departureAirport.passengersByRoute[arrivalAirport]
            : 0;

        int connectionAvailable = CalculateConnectionDemand(departureAirport, arrivalAirport);

        int minimumPassengers = Mathf.RoundToInt(Capacity * DepartureThreshold);
        int totalAvailable = directAvailable + connectionAvailable;

        if (totalAvailable < minimumPassengers)
        {
            SetState(AircraftState.Waiting);
            return;
        }

        int directToBoard = Mathf.Min(directAvailable, Capacity);
        int connectionsToBoard = Mathf.Min(connectionAvailable, Capacity - directToBoard);

        PassengersOnBoard = directToBoard + connectionsToBoard;
        ConnectionPassengers = connectionsToBoard;

        departureAirport.passengersByRoute[arrivalAirport] -= directToBoard;
        BoardConnectionPassengers(departureAirport, arrivalAirport, connectionsToBoard);

        departureAirport.RecalculatePassengers();

        distanceAccumulator = 0f;
        BuildBezierRoute(departureTransform.position, arrivalTransform.position);
        SetState(AircraftState.Flying);
    }

    int CalculateConnectionDemand(Airport departureAirport, Airport arrivalAirport)
    {
        int available = 0;

        foreach (var entry in departureAirport.passengersByRoute)
        {
            Airport finalDestination = entry.Key;

            if (finalDestination == arrivalAirport)
                continue;

            bool hasUsefulConnection =
                departureAirport.routes.Contains(finalDestination) ||
                arrivalAirport.routes.Contains(finalDestination) ||
                departureAirport.inboundRoutes.Contains(finalDestination) ||
                arrivalAirport.inboundRoutes.Contains(finalDestination);

            if (hasUsefulConnection)
                available += entry.Value;
        }

        return available;
    }

    void BoardConnectionPassengers(Airport departureAirport, Airport arrivalAirport, int requested)
    {
        int remaining = requested;
        connectionManifest.Clear();

        routeKeyCache.Clear();
        routeKeyCache.AddRange(departureAirport.passengersByRoute.Keys);

        foreach (Airport finalDestination in routeKeyCache)
        {
            if (finalDestination == arrivalAirport)
                continue;

            if (remaining <= 0)
                break;

            int available = departureAirport.passengersByRoute[finalDestination];
            int taken = Mathf.Min(available, remaining);

            if (taken <= 0)
                continue;

            connectionManifest[finalDestination] = taken;
            departureAirport.passengersByRoute[finalDestination] -= taken;
            remaining -= taken;
        }
    }

    void HandleFlying()
    {
        if (Origin == null || Destination == null)
        {
            SetState(AircraftState.Idle);
            return;
        }

        Age += Time.deltaTime * GameManager.Instance.GameSpeed;

        float movement = Speed * 0.65f * GameManager.Instance.GameSpeed * Time.deltaTime;
        routeT += movement / routeTotalDistance;
        routeT = Mathf.Clamp01(routeT);

        transform.position = EvaluateBezier(routeT);
        RotateAlongBezier(routeT);

        distanceAccumulator += movement * WORLD_TO_MILES;

        while (distanceAccumulator >= 125f)
        {
            ApplyWear();
            distanceAccumulator -= 125f;

            if (CurrentState == AircraftState.OutOfService)
                return;
        }

        if (routeT >= 1f)
            SetState(AircraftState.Arrived);
    }

    void HandleArrival()
    {
        Airport arrivalAirport = currentDestination.GetComponent<Airport>();
        if (arrivalAirport == null)
        {
            SetState(AircraftState.Idle);
            return;
        }

        RedistributePassengers(arrivalAirport);

        float revenue = CalculateTripRevenue(arrivalAirport);
        GameManager.Instance.CashMovement(revenue);

        TotalTrips++;
        TotalRevenue += revenue;
        AverageRevenue = TotalRevenue / TotalTrips;

        RouteTrips++;
        RouteRevenue += revenue;
        RoutePassengers += PassengersOnBoard + ConnectionPassengers;

        AddXP(CalculateTripXP());

        PassengersOnBoard = 0;
        ConnectionPassengers = 0;
        connectionManifest.Clear();

        goingForward = !goingForward;
        currentDestination = goingForward ? Destination : Origin;

        BuildBezierRoute(transform.position, currentDestination.position);

        waitTimer = 0f;
        SetState(AircraftState.Turnaround);
    }

    float CalculateTripRevenue(Airport arrivalAirport)
    {
        float healthEfficiency = Mathf.Pow(HealthPercent(), 0.5f);
        if (MaintenanceMode)
            healthEfficiency *= 0.9f;

        float loadFactor = Capacity > 0 ? (float)PassengersOnBoard / Capacity : 0f;
        float demandPriceMultiplier = Mathf.Lerp(0.8f, 1.5f, loadFactor);
        float reputationMultiplier = Mathf.Lerp(0.5f, 1.5f, arrivalAirport.Reputation / 100f);

        int directPassengers = PassengersOnBoard - ConnectionPassengers;

        float[] baseTicketPrice = { 0f, 80f, 150f, 280f, 420f };
        float[] fuelEfficiency = { 0f, 0.2f, 0.35f, 0.35f, 0.5f };

        float distanceMultiplier = Mathf.Lerp(0.8f, 1.8f, Mathf.Clamp01(Miles / 5000f));
        float routeFitMultiplier = CalculateRouteFitMultiplier();

        float ticketValue =
            directPassengers * baseTicketPrice[PlaneType] * reputationMultiplier * demandPriceMultiplier * distanceMultiplier * routeFitMultiplier +
            ConnectionPassengers * baseTicketPrice[PlaneType] * 0.6f * reputationMultiplier * distanceMultiplier * routeFitMultiplier;

        float fuelCost = Miles * FuelConsumption * fuelEfficiency[PlaneType] * Mathf.Lerp(0.7f, 1.3f, loadFactor);
        float netRevenue = (ticketValue - fuelCost) * healthEfficiency;

        return netRevenue * LowHealthPenalty();
    }

    float CalculateRouteFitMultiplier()
    {
        if (PlaneType == 1 && Miles < 500) return 6.2f;
        if (PlaneType == 2 && Miles > 550 && Miles < 2500) return 4.2f;
        if (PlaneType == 3 && Miles > 2000) return 1.3f;
        if (PlaneType == 4 && Miles > 4000) return 1.5f;
        if (PlaneType == 4 && Miles < 2000) return 0.2f;
        if (PlaneType == 3 && Miles < 950) return 0.6f;
        if (PlaneType == 1 && Miles > 800) return 0.5f;

        return 1f;
    }

    void RedistributePassengers(Airport arrivalAirport)
    {
        int stayingPassengers = Mathf.RoundToInt(PassengersOnBoard * Random.Range(0.4f, 0.7f));

        if (arrivalAirport.passengersByRoute.Count == 0 || stayingPassengers <= 0)
            return;

        routeKeyCache.Clear();
        routeKeyCache.AddRange(arrivalAirport.passengersByRoute.Keys);

        Airport mainTarget = routeKeyCache[Random.Range(0, routeKeyCache.Count)];
        arrivalAirport.passengersByRoute[mainTarget] += Mathf.RoundToInt(stayingPassengers * 0.7f);

        foreach (var connection in connectionManifest)
        {
            if (!arrivalAirport.passengersByRoute.ContainsKey(connection.Key))
                arrivalAirport.passengersByRoute.Add(connection.Key, 0);

            arrivalAirport.passengersByRoute[connection.Key] += connection.Value;
        }
    }

    void HandleWaiting()
    {
        waitTimer += Time.deltaTime;

        if (waitTimer >= 1f)
        {
            waitTimer = 0f;
            SetState(AircraftState.Boarding);
        }
    }

    void HandleTurnaround()
    {
        waitTimer += Time.deltaTime;

        if (waitTimer >= turnaroundTime)
        {
            waitTimer = 0f;
            SetState(AircraftState.Boarding);
        }
    }

    void HandleRepair()
    {
        repairTimer += Time.deltaTime * GameManager.Instance.GameSpeed;

        if (repairTimer < repairDuration)
            return;

        float sizeFactor = 1f + PlaneType * 0.5f;
        float repairCost = (MaxHP - HP) * RepairCostPerHP * sizeFactor;

        GameManager.Instance.CashMovement(-repairCost);

        HP = MaxHP;
        repairTimer = 0f;
        Wear = Mathf.Max(Wear - 5f, 0f);
        Reliability = Mathf.Min(Reliability + 10f, reliabilityBase);

        SetState(Origin != null && Destination != null
            ? AircraftState.Boarding
            : AircraftState.Idle);
    }

    void ApplyWear()
    {
        Wear = Mathf.Min(Wear + 0.00012f, 100f);
        reliabilityBase = Mathf.Max(20f, reliabilityBase - 0.0002f);

        Reliability += MaintenanceMode ? 0.00005f : -0.001f;
        Reliability = Mathf.Clamp(Reliability, 20f, reliabilityBase);

        float reliabilityRatio = reliabilityBase > 0f ? Reliability / reliabilityBase : 1f;
        float distanceStress = (Mathf.Clamp(Miles / 500f, 0.5f, 2f) - 1f) * 0.5f;
        float ageStress = Mathf.Clamp(Age / 10000f, 0f, 1.5f) * 0.5f;

        float damageChance = Mathf.Clamp01((1f - reliabilityRatio) + distanceStress + ageStress);

        if (MaintenanceMode)
            damageChance *= 0.5f;

        if (Random.value < damageChance)
            HP = Mathf.Max(HP - Random.Range(1, 3), 0);

        float criticalFailureChance = (1f - reliabilityRatio) * 0.0002f;

        if (Random.value < criticalFailureChance || HP <= 0)
        {
            CalculateRepairDuration();
            ReturnToNearestAirport();
            SetState(AircraftState.OutOfService);
        }
    }

    void CalculateRepairDuration()
    {
        float damagePercent = 1f - HealthPercent();
        repairDuration = damagePercent * 5f * 24f;
        repairTimer = 0f;
    }

    void ReturnToNearestAirport()
    {
        if (Origin == null && Destination == null)
            return;

        float distanceToOrigin = Origin != null
            ? Vector2.Distance(transform.position, Origin.position)
            : float.MaxValue;

        float distanceToDestination = Destination != null
            ? Vector2.Distance(transform.position, Destination.position)
            : float.MaxValue;

        Transform nearest = distanceToOrigin <= distanceToDestination
            ? Origin
            : Destination;

        transform.position = nearest.position;
        currentDestination = nearest;
    }

    void AddXP(float amount)
    {
        XP += amount;

        if (XP < XPRequired)
            return;

        XP -= XPRequired;
        Level++;
        StatPoints += 5;
    }

    float CalculateTripXP()
    {
        return (PassengersOnBoard * 0.5f) + (Miles * 0.01f) + (HealthPercent() * 10f);
    }

    void BuildBezierRoute(Vector3 from, Vector3 to)
    {
        routeFrom = from;
        routeT = 0f;

        float deltaX = to.x - from.x;

        if (Mathf.Abs(deltaX) > 180f)
            to.x += deltaX > 0 ? -360f : 360f;

        routeTotalDistance = Mathf.Max(Vector3.Distance(from, to), 0.01f);

        routeControl = (from + to) / 2f;
        routeControl.y += routeTotalDistance * 0.15f;
    }

    Vector3 EvaluateBezier(float t)
    {
        float u = 1f - t;

        return
            u * u * routeFrom +
            2f * u * t * routeControl +
            t * t * currentDestination.position;
    }

    void RotateAlongBezier(float t)
    {
        float u = 1f - t;

        Vector3 tangent =
            2f * u * (routeControl - routeFrom) +
            2f * t * (currentDestination.position - routeControl);

        if (tangent == Vector3.zero)
            return;

        float angle = Mathf.Atan2(tangent.y, tangent.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle - 90f);
    }

    void EnsurePassengerRoute(Airport origin, Airport destination)
    {
        if (!origin.passengersByRoute.ContainsKey(destination))
            origin.passengersByRoute.Add(destination, 0);
    }

    int CalculateMiles(Vector3 from, Vector3 to)
    {
        return Mathf.RoundToInt(Vector2.Distance(from, to) * WORLD_TO_MILES);
    }

    float HealthPercent()
    {
        return MaxHP > 0 ? (float)HP / MaxHP : 1f;
    }

    float LowHealthPenalty()
    {
        float missingHP = MaxHP - HP;

        if (missingHP >= MaxHP * 0.75f) return 0.70f;
        if (missingHP >= MaxHP * 0.50f) return 0.85f;
        if (missingHP >= MaxHP * 0.25f) return 0.95f;

        return 1f;
    }

    void SetState(AircraftState nextState)
    {
        if (CurrentState == nextState)
            return;

        CurrentState = nextState;
        UpdateVisibility();
    }

    void UpdateVisibility()
    {
        if (spriteRenderer == null)
            return;

        spriteRenderer.color = CurrentState == AircraftState.Flying
            ? Color.white
            : Color.clear;
    }

    public class AircraftCatalogData
    {
        public string Name;
        public int Type;
        public float Speed;
        public float FuelConsumption;
        public int Capacity;
        public int Range;
        public float Reliability;
    }
}
