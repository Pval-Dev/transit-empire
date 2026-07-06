using UnityEngine;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Representative sample from Transit Empire's central runtime orchestrator.
/// 
/// This file is intentionally reduced for portfolio documentation.
/// It shows how the prototype coordinates simulation time, money flow,
/// airport ownership, fleet lifecycle, route creation, financial pressure,
/// and global metrics.
/// </summary>
public class GameManagerSample : MonoBehaviour
{
    public static GameManagerSample Instance;

    [Header("Simulation Time")]
    [Range(1, 5)] public int GameSpeed = 1;
    public float DayDuration = 24f;
    public bool Paused;
    public int Days = 1;
    public int Months = 7;
    public int Years = 2026;

    private float dayTimer;

    [Header("Economy")]
    public float Money;
    public float DailyIncome;
    public float TotalEarned;
    public float TotalSpent;
    public float NetWorth;
    public float GlobalReputation = 50f;
    public float Autonomy = 100f;

    [Header("World State")]
    public List<Airport> AllAirports = new List<Airport>();
    public List<Airport> OwnedAirports = new List<Airport>();
    public List<Airport> VisibleAirports = new List<Airport>();

    [Header("Fleet State")]
    public GameObject PlanePrefab;
    public PlaneData SelectedPlane;
    public Plane SelectedGaragePlane;
    public List<Plane> EquippedPlanes = new List<Plane>();
    public List<Plane> PlanesInRoute = new List<Plane>();

    [Header("Route State")]
    public Transform CurrentAirport;
    public Airport Destination;
    public Airport CurrentRoute;
    public float RoutePrice;
    public float RouteCostMultiplier = 1f;
    public float RouteCountGlobal;

    [Header("Financial Systems")]
    public LoanManager LoanManager;
    public bool AutoSave = true;

    private void Awake()
    {
        Instance = this;
    }

    private void Update()
    {
        if (Paused)
            return;

        TickSimulationDay();
        TrackIncomePerMinute();
    }

    // ---------------------------------------------------------------------
    // Simulation lifecycle
    // ---------------------------------------------------------------------

    private void TickSimulationDay()
    {
        dayTimer += Time.deltaTime * GameSpeed;

        if (dayTimer < DayDuration)
            return;

        dayTimer = 0f;
        AdvanceDay();
    }

    public void AdvanceDay()
    {
        CalculateNetWorth();
        ApplyFinancialHealthRules();

        Days++;

        ProcessInvestors();
        ProcessLoans();

        if (Days % 30 == 0)
            ProcessMonthlyMaintenance();

        if (Days % 30 == 5)
            CalculateReputation();

        if (Days % 7 == 0 && AutoSave)
            SaveManager.Instance.SaveGame();

        DailyIncome = 0f;
        ChangeDate();
    }

    // ---------------------------------------------------------------------
    // Economy
    // ---------------------------------------------------------------------

    public void CashMovement(float amount)
    {
        Money += amount;

        if (amount > 0)
        {
            TotalEarned += amount;
            DailyIncome += amount;
        }
        else
        {
            TotalSpent += Mathf.Abs(amount);
        }

        AudioManager.Instance?.PlayCashMovement();
    }

    private void CalculateNetWorth()
    {
        float airportWorth = 0f;
        float aircraftWorth = 0f;

        foreach (Airport airport in OwnedAirports)
        {
            airportWorth += airport.price * 0.60f;

            AirportGarage garage = airport.GetComponent<AirportGarage>();
            if (garage == null)
                continue;

            foreach (Plane plane in garage.EquippedPlanes)
                aircraftWorth += plane.cost * 0.30f;
        }

        NetWorth = Money + airportWorth + aircraftWorth;
    }

    private void ApplyFinancialHealthRules()
    {
        float financialHealth = LoanManager.PenaltyUmbrall;

        if (financialHealth <= 0f)
        {
            // Critical failure case: clear active loans and seize assets.
            LoanManager.ClearActiveLoans();
            LoanManager.PenaltyUmbrall = 100f;
            DeleteRandomPlanes(0.75f);
            return;
        }

        if (financialHealth <= 15f)
        {
            DeleteRandomPlanes(0.50f);
        }
        else if (financialHealth <= 50f)
        {
            GlobalReputation -= 0.25f;
        }
    }

    private void ProcessInvestors()
    {
        for (int i = LoanManager.InvestorsActives.Count - 1; i >= 0; i--)
        {
            float payment = DailyIncome * LoanManager.DailyPayInvestors[i];

            LoanManager.InvestorsActives[i] -= payment;
            CashMovement(-payment);

            if (LoanManager.InvestorsActives[i] > 0f)
                continue;

            Autonomy += LoanManager.DailyPayInvestors[i] * 100f;
            LoanManager.RemoveInvestorAt(i);
        }
    }

    private void ProcessLoans()
    {
        ProcessLoanGroup(
            LoanManager.PerCapitaActives,
            LoanManager.DailyPayCapita,
            LoanManager.PerCapitaDailyActive,
            LoanManager.PerCapitaNames
        );

        ProcessLoanGroup(
            LoanManager.PerAvgActives,
            LoanManager.DailyPayAvg,
            LoanManager.PerAvgDailyActive,
            LoanManager.PerAvgNames
        );

        LoanManager.PenaltyUmbrall = Mathf.Clamp(
            LoanManager.PenaltyUmbrall,
            0f,
            100f
        );
    }

    private void ProcessLoanGroup(
        List<float> balances,
        List<float> dailyPayments,
        List<bool> autoPayments,
        List<string> names)
    {
        for (int i = balances.Count - 1; i >= 0; i--)
        {
            if (!autoPayments[i])
            {
                balances[i] += balances[i] * 0.003f;
                continue;
            }

            if (Money < dailyPayments[i])
            {
                autoPayments[i] = false;
                continue;
            }

            balances[i] -= dailyPayments[i];
            CashMovement(-dailyPayments[i]);

            // Debt pressure scales against current company value.
            if (NetWorth > 0f)
                LoanManager.PenaltyUmbrall += balances[i] / NetWorth;

            if (balances[i] > 0f)
                continue;

            balances.RemoveAt(i);
            dailyPayments.RemoveAt(i);
            autoPayments.RemoveAt(i);
            names.RemoveAt(i);

            LoanManager.CurrentLoans--;
            LoanManager.PenaltyUmbrall += 15f;
        }
    }

    // ---------------------------------------------------------------------
    // Airport ownership
    // ---------------------------------------------------------------------

    public bool BuyAirport(float basePrice, string airportName, Airport airport)
    {
        if (Money < basePrice)
            return false;

        CashMovement(-basePrice);

        if (!OwnedAirports.Contains(airport))
        {
            OwnedAirports.Add(airport);
            airport.Activate();

            foreach (Airport ownedAirport in OwnedAirports)
                ownedAirport.RefreshPassengerRoutes();
        }

        return true;
    }

    public bool BuyCountry(Country country)
    {
        if (Money < country.CountryPrice)
            return false;

        CashMovement(-country.CountryPrice);
        country.gameObject.SetActive(true);
        country.ActivateCountry();

        return true;
    }

    // ---------------------------------------------------------------------
    // Fleet lifecycle
    // ---------------------------------------------------------------------

    public bool BuyPlane()
    {
        if (CurrentAirport == null || SelectedPlane == null)
            return false;

        if (Money < SelectedPlane.Price)
            return false;

        CashMovement(-SelectedPlane.Price);

        Transform garageTransform = CurrentAirport.Find("Garage");
        GameObject planeObject = Instantiate(PlanePrefab, garageTransform);

        Plane plane = planeObject.GetComponent<Plane>();
        plane.Setup(SelectedPlane);

        AirportGarage garage = CurrentAirport.GetComponent<AirportGarage>();
        garage.planes.Add(plane);

        return true;
    }

    public void EquipPlane()
    {
        AirportGarage garage = CurrentAirport.GetComponent<AirportGarage>();

        garage.planes.Remove(SelectedGaragePlane);
        garage.EquippedPlanes.Add(SelectedGaragePlane);

        EquippedPlanes.Add(SelectedGaragePlane);
    }

    public void RemovePlaneFromRoute()
    {
        if (SelectedGaragePlane == null || CurrentAirport == null)
            return;

        Airport origin = CurrentAirport.GetComponent<Airport>();
        Airport destination = SelectedGaragePlane.cityB?.GetComponent<Airport>();

        if (destination != null && destination != origin)
            StoreRouteHistory(origin, destination, SelectedGaragePlane);

        SelectedGaragePlane.routeTravels = 0;
        SelectedGaragePlane.routeMoney = 0;
        SelectedGaragePlane.routePass = 0;

        origin.UsedRunWays -= SelectedGaragePlane.height;

        SelectedGaragePlane.cityA = CurrentAirport;
        SelectedGaragePlane.SetDestination(CurrentAirport);
        SelectedGaragePlane.transform.position = CurrentAirport.position;
        SelectedGaragePlane.Crew = 0;

        PlanesInRoute.Remove(SelectedGaragePlane);
        EquippedPlanes.Add(SelectedGaragePlane);
    }

    private void StoreRouteHistory(Airport origin, Airport destination, Plane plane)
    {
        if (!origin.routeStats.ContainsKey(destination))
            origin.routeStats.Add(destination, new Airport.RouteStats());

        origin.routeStats[destination].totalTravels += plane.routeTravels;
        origin.routeStats[destination].totalMoney += plane.routeMoney;
        origin.routeStats[destination].totalPass += plane.routePass;
    }

    // ---------------------------------------------------------------------
    // Route system
    // ---------------------------------------------------------------------

    public bool SelectRouteDestination(Airport destination)
    {
        Airport origin = CurrentAirport.GetComponent<Airport>();

        if (destination == origin)
            return false;

        Destination = destination;
        return true;
    }

    public float CalculateRoutePrice()
    {
        Airport origin = CurrentAirport.GetComponent<Airport>();

        float dx = Mathf.Abs(CurrentAirport.position.x - Destination.transform.position.x);
        float dy = Mathf.Abs(CurrentAirport.position.y - Destination.transform.position.y);

        // Handle map wrapping near the antimeridian.
        if (dx > 180f)
            dx = 360f - dx;

        int miles = Mathf.RoundToInt(Mathf.Sqrt(dx * dx + dy * dy) * Plane.WORLD_TO_MILES);

        RoutePrice =
            miles *
            1500f *
            Destination.ApLvl *
            RouteCostMultiplier /
            origin.ApLvl;

        return RoutePrice;
    }

    public bool CreateRoute()
    {
        if (CurrentAirport == null || Destination == null)
            return false;

        Airport origin = CurrentAirport.GetComponent<Airport>();

        if (Destination == origin)
            return false;

        if (origin.routes.Contains(Destination))
            return false;

        if (origin.RoutesSlots + 1 > origin.MaxRoutesSlots)
            return false;

        if (Money < RoutePrice)
            return false;

        origin.routes.Add(Destination);
        Destination.inboundRoutes.Add(origin);

        if (!origin.passengersByRoute.ContainsKey(Destination))
            origin.passengersByRoute.Add(Destination, 0);

        origin.RoutesSlots++;
        RouteCountGlobal++;

        CashMovement(-RoutePrice);

        return true;
    }

    public bool AssignPlaneToRoute()
    {
        if (SelectedGaragePlane == null || CurrentRoute == null)
            return false;

        Airport origin = CurrentAirport.GetComponent<Airport>();

        if (SelectedGaragePlane.cityB == CurrentRoute.transform)
            return false;

        if (origin.UsedRunWays + SelectedGaragePlane.height > origin.MaxRunways)
            return false;

        origin.UsedRunWays += SelectedGaragePlane.height;

        SelectedGaragePlane.cityA = CurrentAirport;
        SelectedGaragePlane.SetDestination(CurrentRoute.transform);
        SelectedGaragePlane.transform.position = CurrentAirport.position;

        PlanesInRoute.Add(SelectedGaragePlane);
        EquippedPlanes.Remove(SelectedGaragePlane);

        return true;
    }

    // ---------------------------------------------------------------------
    // Reputation and maintenance
    // ---------------------------------------------------------------------

    private void ProcessMonthlyMaintenance()
    {
        LoanManager.attemps = 3;

        foreach (Airport airport in OwnedAirports)
        {
            if (airport.Protection)
            {
                airport.Protection = false;
                airport.ticksStart = 0;
                airport.MoneyForMaintenanceUi = 0f;
                continue;
            }

            airport.ticksStart++;
            airport.CalcMensualCost();

            if (airport.ticksStart > 0)
                airport.consecutivePayments = 0;
        }
    }

    public bool PayAirportMaintenance(Airport airport)
    {
        if (Money < airport.MoneyForMaintenanceUi)
            return false;

        CashMovement(-airport.MoneyForMaintenanceUi);

        airport.ticksStart = 0;
        airport.Porcent = 0.02f;
        airport.MoneyForMaintenanceUi = 0f;
        airport.consecutivePayments++;

        return true;
    }

    private void CalculateReputation()
    {
        float positive = 0f;
        float negative = 0f;

        positive += OwnedAirports.Count * 0.5f;

        foreach (Airport airport in OwnedAirports)
        {
            if (airport.MoneyForMaintenanceUi > 1f)
                negative += 2f;

            if (airport.ticksStart > 3)
                negative += 1f;

            if (airport.consecutivePayments >= 3)
                positive += 1f;

            positive += airport.RoutesSlots * 0.3f;
        }

        foreach (Plane plane in PlanesInRoute)
        {
            if (plane.average < 0f)
                negative += 1.5f;
            else
                positive += Mathf.Clamp(plane.average / 1_000_000f, 0f, 2f);
        }

        float delta = positive - negative;
        float scaleFactor = 1f - (GlobalReputation / 250f);

        GlobalReputation = Mathf.Clamp(
            GlobalReputation + delta * scaleFactor,
            0f,
            100f
        );

        CalculateMaxLoansFromReputation();
    }

    private void CalculateMaxLoansFromReputation()
    {
        int[] maxLoansByReputation = { 1, 2, 3, 4, 5, 7, 10 };
        int index = Mathf.Clamp(
            (int)(GlobalReputation / 15f),
            0,
            maxLoansByReputation.Length - 1
        );

        LoanManager.MaxLoans = maxLoansByReputation[index];
    }

    // ---------------------------------------------------------------------
    // Utilities
    // ---------------------------------------------------------------------

    public void SetSpeed(int speed)
    {
        GameSpeed = Mathf.Clamp(speed, 1, 5);
    }

    private void ChangeDate()
    {
        if (Days > 30)
        {
            Days = 1;
            Months++;
        }

        if (Months > 12)
        {
            Months = 1;
            Years++;
        }
    }

    public static string MoneyFormat(float amount)
    {
        if (amount >= 1_000_000_000_000_000f)
            return (amount / 1_000_000_000_000_000f).ToString("F1") + "Q";

        if (amount >= 1_000_000_000_000f)
            return (amount / 1_000_000_000_000f).ToString("F1") + "T";

        if (amount >= 1_000_000_000f)
            return (amount / 1_000_000_000f).ToString("F1") + "B";

        if (amount >= 1_000_000f)
            return (amount / 1_000_000f).ToString("F1") + "M";

        if (amount >= 1_000f)
            return (amount / 1_000f).ToString("F1") + "K";

        return amount.ToString("F0");
    }

    private void TrackIncomePerMinute()
    {
        // Omitted from sample:
        // The production script tracks money delta every 0.5 seconds
        // and scales it into a per-minute UI metric.
    }

    private void DeleteRandomPlanes(float percentage)
    {
        // Omitted from sample:
        // The production script collects aircraft from garages and active routes,
        // then removes a percentage of them as a financial penalty.
    }
}

/// <summary>
/// Reduced version of the aircraft catalog entry used by the central orchestrator.
/// </summary>
public class PlaneData
{
    public string Name { get; }
    public int Price { get; }
    public int Type { get; }
    public int Speed { get; }
    public int Consume { get; }
    public int Capacity { get; }
    public int Range { get; }
    public int Reliability { get; }
    public float Height { get; }

    public PlaneData(
        string name,
        int price,
        int type,
        int speed,
        int consume,
        int capacity,
        int range,
        int reliability,
        float height)
    {
        Name = name;
        Price = price;
        Type = type;
        Speed = speed;
        Consume = consume;
        Capacity = capacity;
        Range = range;
        Reliability = reliability;
        Height = height;
    }
}
