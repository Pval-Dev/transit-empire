using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Curated excerpt from Transit Empire's route/fleet operations in GameManager.
/// Demonstrates route pricing, slot/runway validation, route creation,
/// route metrics, and aircraft assignment.
/// </summary>
public class RouteOperationsSample : MonoBehaviour
{
    public float Money;
    public float RouteCostMultiplier = 1f;

    public Transform CurrentAirport;
    public Airport Destination;
    public Plane SelectedPlane;
    public Airport CurrentRoute;

    public float CalculateRoutePrice()
    {
        int miles = Mathf.RoundToInt(
            Vector2.Distance(
                CurrentAirport.position,
                Destination.transform.position) * Plane.WORLD_TO_MILES);

        Airport origin = CurrentAirport.GetComponent<Airport>();

        return miles
            * 1500f
            * Destination.ApLvl
            * RouteCostMultiplier
            / origin.ApLvl;
    }

    public bool CreateRoute()
    {
        if (CurrentAirport == null || Destination == null)
            return false;

        Airport origin = CurrentAirport.GetComponent<Airport>();
        List<Airport> routes = origin.routes;

        if (Destination == origin)
            return false;

        if (origin.RoutesSlots + 1 > origin.MaxRoutesSlots)
            return false;

        if (routes.Contains(Destination))
            return false;

        float price = CalculateRoutePrice();
        if (Money < price)
            return false;

        routes.Add(Destination);
        Destination.inboundRoutes.Add(origin);

        if (!origin.passengersByRoute.ContainsKey(Destination))
            origin.passengersByRoute.Add(Destination, 0);

        origin.RoutesSlots++;
        Money -= price;

        return true;
    }

    public bool AssignPlaneToRoute()
    {
        if (SelectedPlane == null || CurrentRoute == null || CurrentAirport == null)
            return false;

        if (SelectedPlane.cityB == CurrentRoute.transform)
            return false;

        Airport origin = CurrentAirport.GetComponent<Airport>();
        float projectedRunwayUse = origin.UsedRunWays + SelectedPlane.height;

        if (projectedRunwayUse > origin.MaxRunways)
            return false;

        origin.UsedRunWays = projectedRunwayUse;

        if (SelectedPlane.cityB != null && SelectedPlane.cityB != SelectedPlane.cityA)
        {
            SelectedPlane.SetDestination(CurrentRoute.transform);
            return true;
        }

        SelectedPlane.cityA = CurrentAirport;
        SelectedPlane.SetDestination(CurrentRoute.transform);
        SelectedPlane.transform.position = CurrentAirport.position;

        return true;
    }

    public void RemovePlaneFromCurrentRoute()
    {
        if (SelectedPlane == null || CurrentAirport == null)
            return;

        Airport origin = CurrentAirport.GetComponent<Airport>();
        Airport destination = SelectedPlane.cityB?.GetComponent<Airport>();

        if (destination != null && destination != origin)
        {
            if (!origin.routeStats.ContainsKey(destination))
                origin.routeStats.Add(destination, new Airport.RouteStats());

            Airport.RouteStats history = origin.routeStats[destination];
            history.totalTravels += SelectedPlane.routeTravels;
            history.totalMoney += SelectedPlane.routeMoney;
            history.totalPass += SelectedPlane.routePass;
        }

        SelectedPlane.routeTravels = 0;
        SelectedPlane.routeMoney = 0;
        SelectedPlane.routePass = 0;

        origin.UsedRunWays -= SelectedPlane.height;

        SelectedPlane.cityA = CurrentAirport;
        SelectedPlane.SetDestination(CurrentAirport);
        SelectedPlane.transform.position = CurrentAirport.position;
        SelectedPlane.Crew = 0;
    }
}
