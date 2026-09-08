using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/// <summary>
/// Curated excerpt from Transit Empire's Airport component.
/// Demonstrates destination-level demand, capacity pressure,
/// airport-type routing rules, reputation, and progression.
/// </summary>
public class AirportDemandSample : MonoBehaviour
{
    public string AirportName;
    public string CountryHome;
    public string Container;
    public int airportType; // 0 regional, 1 capital, 2 international

    public int passengers;
    public int capacity;
    public float multiplier = 1f;
    public float secondarymultiplierBASE = 1f;
    public float demandWeight = 1f;
    public float Reputation = 1f;
    public float maxReputation = 1f;

    public Dictionary<Airport, int> passengersByRoute = new();

    public int ApLvl = 1;
    public float xp;
    public int statspoints;
    public bool CanLvlUp;
    public float UsedRunWays;
    public float MaxRunways = 20f;
    public int RoutesSlots;
    public int MaxRoutesSlots = 7;
    public int capacityLevel = 1;
    public int Runwaylvl = 1;
    public int routeslotslvls = 1;
    public int reputationlvl = 1;
    public float BaseApPrice = 500_000f;

    public void GeneratePassengerTick()
    {
        Reputation += 0.03f;
        Reputation = Mathf.Clamp(Reputation, 0f, maxReputation);

        float reputationEffect = Mathf.Pow(Reputation, 0.3f);
        Country country = transform.parent.GetComponent<Country>();

        int generated = GameManager.Instance.GeneratePassengers(
            Container,
            country.multiplier * reputationEffect,
            multiplier * reputationEffect,
            secondarymultiplierBASE);

        int remaining = generated;
        List<Airport> destinations = ResolveDestinations();

        foreach (Airport destination in destinations)
        {
            if (remaining <= 0) break;
            if (destination == this) continue;

            if (!passengersByRoute.ContainsKey(destination))
                passengersByRoute.Add(destination, 0);

            int portion = Mathf.Clamp(
                (int)(Random.Range(0.3f, 0.7f) * remaining * destination.demandWeight),
                0,
                remaining);

            passengersByRoute[destination] += portion;
            remaining -= portion;
        }

        RecalculatePassengers();
        EnforceCapacity();
    }

    private List<Airport> ResolveDestinations()
    {
        List<Airport> active = GameManager.Instance.GlobalApList;

        return airportType switch
        {
            0 => active
                .Where(a => a.CountryHome == CountryHome)
                .OrderBy(a => Vector2.Distance(transform.position, a.transform.position))
                .Take(25).ToList(),

            1 => active
                .Where(a => a.Container == Container && a != this)
                .OrderBy(a => Vector2.Distance(transform.position, a.transform.position))
                .Take(50).ToList(),

            2 => active
                .Where(a => a.airportType >= 1)
                .OrderBy(_ => Random.value)
                .Take(10).ToList(),

            _ => active
        };
    }

    private void EnforceCapacity()
    {
        if (passengers <= capacity) return;

        int overflow = passengers - capacity;

        foreach (Airport key in new List<Airport>(passengersByRoute.Keys))
        {
            if (overflow <= 0) break;

            int reduction = Mathf.Min(passengersByRoute[key], overflow);
            passengersByRoute[key] -= reduction;
            overflow -= reduction;
        }

        passengers = capacity;
        float occupancy = capacity > 0 ? (float)passengers / capacity : 0f;

        if (occupancy >= 0.98f) ApplyReputationPenalty(3);
        else if (occupancy >= 0.90f) ApplyReputationPenalty(2);
        else if (occupancy >= 0.80f) ApplyReputationPenalty(1);
    }

    public void RecalculatePassengers()
    {
        passengers = 0;
        foreach (KeyValuePair<Airport, int> entry in passengersByRoute)
            passengers += entry.Value;
    }

    public void ApplyReputationPenalty(int severity)
    {
        Reputation -= severity switch
        {
            1 => 0.02f,
            2 => 0.04f,
            3 => 0.08f,
            _ => 0f
        };

        Reputation = Mathf.Clamp01(Reputation);
    }

    public void AddXP(float amount)
    {
        xp += amount;
        int required = Mathf.RoundToInt(800 * Mathf.Pow(ApLvl, 1.9f));
        if (xp >= required) CanLvlUp = true;
    }

    public void UpgradeAirportLevel()
    {
        if (!CanLvlUp) return;

        int moneyCost = Mathf.RoundToInt(
            BaseApPrice * 0.1f * Mathf.Pow(ApLvl, 1.5f));

        if (GameManager.Instance.Money < moneyCost) return;

        int required = Mathf.RoundToInt(800 * Mathf.Pow(ApLvl, 1.9f));
        xp -= required;
        ApLvl++;
        CanLvlUp = false;
        statspoints += 3 + (ApLvl / 3);
        multiplier = ApLvl;

        GameManager.Instance.CashMovement(-moneyCost);
    }
}
