using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Curated, behavior-preserving excerpt from Transit Empire's Plane script.
/// Focuses on lifecycle state, boarding rules, connection demand,
/// movement, wear, critical failure, waiting, turnaround, and repair.
/// </summary>
public class AircraftStateMachineSample : MonoBehaviour
{
    public const float WORLD_TO_MILES = 69.17f;

    public enum PlaneState
    {
        Idle,
        Boarding,
        Flying,
        Arrived,
        Waiting,
        Turnaround,
        OutOfService
    }

    public Transform cityA;
    public Transform cityB;
    public string PlaneName;
    public int Crew;
    public int connectionCrew;
    public int capac;
    public int range;
    public int planetype;
    public int Miles;
    public float speed;
    public int hp;
    public int maxHP;
    public float age;
    public float wear;
    public float reliability;
    public bool maintenanceMode;
    public float repairCostPerHP = 500f;

    [Range(0f, 1f)]
    public float departureThreshold = 0.3f;

    public PlaneState currentState;

    private readonly Dictionary<Airport, int> connectionManifest = new();
    private bool goingForward = true;
    private Transform currentDestination;
    private float waitTimer;
    private float turnaroundTime = 3f;
    private float repairTimer;
    private float repairDuration;
    private float distanceAccumulator;
    private float reliabilityBase;

    void Start()
    {
        currentDestination = cityB;
        reliabilityBase = reliability;

        if (cityA != null && cityB != null)
        {
            Miles = Mathf.RoundToInt(
                Vector2.Distance(cityA.position, cityB.position) * WORLD_TO_MILES);
        }

        SetState(cityA == null || cityB == null
            ? PlaneState.Idle
            : PlaneState.Boarding);
    }

    void Update()
    {
        switch (currentState)
        {
            case PlaneState.Boarding: HandleBoarding(); break;
            case PlaneState.Flying: HandleFlying(); break;
            case PlaneState.Waiting: HandleWaiting(); break;
            case PlaneState.Turnaround: HandleTurnaround(); break;
            case PlaneState.OutOfService: HandleRepair(); break;
        }
    }

    private void SetState(PlaneState next)
    {
        if (currentState != next)
            currentState = next;
    }

    private void HandleBoarding()
    {
        if (cityA == null || cityB == null)
        {
            SetState(PlaneState.Idle);
            return;
        }

        if (Miles > range)
        {
            SetState(PlaneState.Idle);
            return;
        }

        Transform originTransform = goingForward ? cityA : cityB;
        Transform destinationTransform = goingForward ? cityB : cityA;

        Airport origin = originTransform.GetComponent<Airport>();
        Airport destination = destinationTransform.GetComponent<Airport>();
        Airport routeOwner = cityA.GetComponent<Airport>();

        if (origin == null || destination == null || origin.passengersByRoute == null)
            return;

        EnsureRoute(origin, destination);

        int directAvailable = origin.passengersByRoute.TryGetValue(destination, out int direct)
            ? direct
            : 0;

        int connectionAvailable = CountConnectionDemand(origin, destination, routeOwner);
        int minimumRequired = (int)(capac * departureThreshold);

        if (directAvailable + connectionAvailable < minimumRequired)
        {
            SetState(PlaneState.Waiting);
            return;
        }

        int directToTake = Mathf.Min(directAvailable, capac);
        int connectionToTake = Mathf.Min(connectionAvailable, capac - directToTake);

        Crew = directToTake + connectionToTake;
        connectionCrew = connectionToTake;

        origin.passengersByRoute[destination] -= directToTake;
        BoardConnections(origin, destination, routeOwner, connectionToTake);
        origin.RecalculatePassengers();

        distanceAccumulator = 0f;
        SetState(PlaneState.Flying);
    }

    private int CountConnectionDemand(Airport origin, Airport destination, Airport routeOwner)
    {
        int available = 0;

        foreach (KeyValuePair<Airport, int> entry in origin.passengersByRoute)
        {
            Airport finalDestination = entry.Key;

            if (finalDestination == destination)
                continue;

            if (routeOwner.routes.Contains(finalDestination)
                && destination.routes.Contains(finalDestination))
                continue;

            bool reachable =
                origin.routes.Contains(finalDestination)
                || destination.routes.Contains(finalDestination)
                || origin.inboundRoutes.Contains(finalDestination)
                || destination.inboundRoutes.Contains(finalDestination);

            if (reachable)
                available += entry.Value;
        }

        return available;
    }

    private void BoardConnections(
        Airport origin,
        Airport destination,
        Airport routeOwner,
        int seats)
    {
        int remaining = seats;

        foreach (Airport finalDestination in new List<Airport>(origin.passengersByRoute.Keys))
        {
            if (finalDestination == destination || remaining <= 0)
                continue;

            if (routeOwner.routes.Contains(finalDestination)
                && destination.routes.Contains(finalDestination))
                continue;

            bool reachable =
                origin.routes.Contains(finalDestination)
                || destination.routes.Contains(finalDestination)
                || origin.inboundRoutes.Contains(finalDestination)
                || destination.inboundRoutes.Contains(finalDestination);

            if (!reachable)
                continue;

            int take = Mathf.Min(origin.passengersByRoute[finalDestination], remaining);

            if (!connectionManifest.ContainsKey(finalDestination))
                connectionManifest[finalDestination] = 0;

            connectionManifest[finalDestination] += take;
            origin.passengersByRoute[finalDestination] -= take;
            remaining -= take;
        }
    }

    private void HandleFlying()
    {
        if (cityA == null || cityB == null)
        {
            SetState(PlaneState.Idle);
            return;
        }

        age += Time.deltaTime * GameManager.Instance.GameSpeed;
        float movement = speed * GameManager.Instance.GameSpeed * Time.deltaTime;

        transform.position = Vector2.MoveTowards(
            transform.position,
            currentDestination.position,
            movement);

        distanceAccumulator += movement * WORLD_TO_MILES;

        while (distanceAccumulator >= 125f)
        {
            ApplyWearPerDistanceStep();
            distanceAccumulator -= 125f;

            if (currentState == PlaneState.OutOfService)
                return;
        }

        if (Vector2.Distance(transform.position, currentDestination.position) < 0.05f)
            SetState(PlaneState.Arrived);
    }

    private void ApplyWearPerDistanceStep()
    {
        wear = Mathf.Min(wear + 0.00012f, 100f);
        reliabilityBase = Mathf.Max(20f, reliabilityBase - 0.0002f);

        reliability += maintenanceMode ? 0.00005f : -0.001f;
        reliability = Mathf.Clamp(reliability, 20f, reliabilityBase);

        float reliabilityNormalized = reliabilityBase > 0f
            ? reliability / reliabilityBase
            : 1f;

        float damageChance =
            (1f - reliabilityNormalized)
            + (Mathf.Clamp(Miles / 500f, 0.5f, 2f) - 1f) * 0.5f
            + Mathf.Clamp(age / 10_000f, 0f, 1.5f) * 0.5f;

        if (maintenanceMode)
            damageChance *= 0.5f;

        damageChance = 1f - Mathf.Exp(-Mathf.Clamp01(damageChance));

        if (Random.value < damageChance)
            hp = Mathf.Max(hp - Random.Range(1, 3), 0);

        float criticalChance = (1f - reliabilityNormalized) * 0.0002f;

        if (Random.value < criticalChance)
        {
            CalculateRepairTime();
            ReturnToNearestAirport();
            SetState(PlaneState.OutOfService);
        }
    }

    private void HandleWaiting()
    {
        waitTimer += Time.deltaTime;
        if (waitTimer >= 1f)
        {
            waitTimer = 0f;
            SetState(PlaneState.Boarding);
        }
    }

    private void HandleTurnaround()
    {
        waitTimer += Time.deltaTime;
        if (waitTimer >= turnaroundTime)
        {
            waitTimer = 0f;
            SetState(PlaneState.Boarding);
        }
    }

    private void HandleRepair()
    {
        repairTimer += Time.deltaTime;
        if (repairTimer < repairDuration)
            return;

        float sizeFactor = 1f + planetype * 0.5f;
        float repairCost = (maxHP - hp) * repairCostPerHP * sizeFactor;

        GameManager.Instance.CashMovement(-repairCost);

        hp = maxHP;
        repairTimer = 0f;
        reliability = Mathf.Min(reliability + 10f, reliabilityBase);
        wear = Mathf.Max(wear - 5f, 0f);

        SetState(cityA != null && cityB != null
            ? PlaneState.Boarding
            : PlaneState.Idle);
    }

    private void CalculateRepairTime()
    {
        float damagePercent = maxHP > 0 ? 1f - (float)hp / maxHP : 0f;
        float baseDays = damagePercent * 5f;
        repairDuration = (baseDays * 24f) / GameManager.Instance.GameSpeed;
        repairTimer = 0f;
    }

    private void ReturnToNearestAirport()
    {
        float distanceA = cityA != null
            ? Vector2.Distance(transform.position, cityA.position)
            : float.MaxValue;

        float distanceB = cityB != null
            ? Vector2.Distance(transform.position, cityB.position)
            : float.MaxValue;

        currentDestination = distanceA <= distanceB ? cityA : cityB;

        if (currentDestination != null)
            transform.position = currentDestination.position;
    }

    private static void EnsureRoute(Airport origin, Airport destination)
    {
        if (!origin.passengersByRoute.ContainsKey(destination))
            origin.passengersByRoute.Add(destination, 0);
    }
}
