using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;

/// <summary>
/// Representative sample extracted from Transit Empire's route interface controller.
/// 
/// This sample shows how the route UI coordinates:
/// - route creation mode
/// - dynamic route button generation
/// - selected route state
/// - route metric aggregation
/// - transition into aircraft assignment mode
/// </summary>
public class RouteManagerSample : MonoBehaviour
{
    [Header("Buttons")]
    public List<Button> buttons;

    [Header("UI Groups")]
    public GameObject[] guiGroups;
    public GameObject[] panels;

    [Header("Route List")]
    public Transform routeListContainer;
    public GameObject routeButtonPrefab;
    public List<Button> routeButtons = new List<Button>();

    [Header("Route Metrics")]
    public TextMeshProUGUI originAirportName;
    public List<TextMeshProUGUI> routeInfoTexts;

    private enum RouteInfoField
    {
        Name = 0,
        AverageRevenue = 1,
        TotalRevenue = 2,
        Distance = 3,
        AveragePassengers = 4,
        TotalPassengers = 5
    }

    private enum GuiGroup
    {
        RouteMain = 0,
        RouteInfo = 1,
        Garage = 2,
        InfoBackground = 3
    }

    private enum PanelGroup
    {
        RouteCreation = 0
    }

    private enum RouteButtonAction
    {
        CreateRoute = 0,
        ExitRouteMode = 1,
        AddPlane = 2,
        DeleteRoute = 3,
        ViewFleet = 4,
        CloseRouteInfo = 5
    }

    private Dictionary<RouteButtonAction, Action> buttonActions;

    void Awake()
    {
        buttonActions = new Dictionary<RouteButtonAction, Action>
        {
            {
                RouteButtonAction.CreateRoute,
                () =>
                {
                    GameManager.Instance.SetRouteMode(true);
                    SetPanel(PanelGroup.RouteCreation, true);
                    SetGui(GuiGroup.RouteMain, false);
                }
            },

            {
                RouteButtonAction.ExitRouteMode,
                () =>
                {
                    SetPanel(PanelGroup.RouteCreation, false);
                    SetGui(GuiGroup.RouteMain, false);

                    GameManager.Instance.SetRouteMode(false);
                    GameManager.Instance.RouteComplete(false);
                }
            },

            {
                RouteButtonAction.AddPlane,
                () =>
                {
                    SetPanel(PanelGroup.RouteCreation, false);
                    SetGui(GuiGroup.RouteMain, false);
                    SetGui(GuiGroup.RouteInfo, false);
                    SetGui(GuiGroup.Garage, true);

                    GameManager.Instance.setequimode(true);
                }
            },

            {
                RouteButtonAction.DeleteRoute,
                () =>
                {
                    GameManager.Instance.DeleteRoute();

                    SetGui(GuiGroup.RouteInfo, false);
                    SetCreateButtonVisible(true);

                    RebuildRouteButtons();
                }
            },

            {
                RouteButtonAction.ViewFleet,
                () =>
                {
                    GameManager.Instance.SetFloat(true);

                    SetGui(GuiGroup.Garage, true);
                    SetGui(GuiGroup.RouteMain, false);
                    SetGui(GuiGroup.RouteInfo, false);
                }
            },

            {
                RouteButtonAction.CloseRouteInfo,
                () =>
                {
                    GameManager.Instance.SetMainUi(false);
                    SetCreateButtonVisible(true);
                    CancelInvoke(nameof(UpdateSelectedRouteMetrics));
                }
            }
        };
    }

    void Start()
    {
        for (int i = 0; i < buttons.Count; i++)
        {
            int index = i;
            buttons[i].onClick.AddListener(() =>
                ExecuteButtonAction((RouteButtonAction)index));
        }
    }

    void OnEnable()
    {
        InitializeRoutePanel();
        GameManager.Instance.SetMainUi(false);
    }

    void OnDisable()
    {
        CancelInvoke();
        GameManager.Instance.SetMainUi(false);
    }

    public void InitializeRoutePanel()
    {
        GameManager.Instance.SetMainUi(false);
        GameManager.Instance.SetFloat(false);
        GameManager.Instance.setequimode(false);

        SetCreateButtonVisible(true);
        SetPanel(PanelGroup.RouteCreation, false);

        GameManager.Instance.SetRouteMode(true);

        if (GameManager.Instance.RouteCompleted())
            GameManager.Instance.CreateRoute();

        RebuildRouteButtons();
    }

    void ExecuteButtonAction(RouteButtonAction action)
    {
        if (!buttonActions.TryGetValue(action, out Action handler))
            return;

        handler.Invoke();
    }

    void RebuildRouteButtons()
    {
        ClearRouteButtons();

        List<Airport> routes = GameManager.Instance.GetRoutes();
        Transform currentAirportTransform = GameManager.Instance.GetCurrentAirport();

        if (currentAirportTransform == null || routes == null || routes.Count == 0)
            return;

        Airport currentAirport = currentAirportTransform.GetComponent<Airport>();

        for (int i = 0; i < routes.Count; i++)
        {
            Airport destination = routes[i];

            if (destination == null || destination.transform == currentAirportTransform)
                continue;

            CreateRouteButton(currentAirport, destination, i);
        }
    }

    void CreateRouteButton(Airport origin, Airport destination, int routeIndex)
    {
        GameObject buttonObject = Instantiate(routeButtonPrefab, routeListContainer);
        buttonObject.name = origin.AirportName + "+" + destination.AirportName;

        TextMeshProUGUI originText =
            buttonObject.transform.Find("BG/Current").GetComponent<TextMeshProUGUI>();

        TextMeshProUGUI destinationText =
            buttonObject.transform.Find("BG/Dest").GetComponent<TextMeshProUGUI>();

        originText.text = origin.AirportName;
        destinationText.text = destination.AirportName;

        Image border = buttonObject.transform.Find("BG").GetComponent<Image>();
        border.color = GetAirportTypeColor(destination.airportType);

        Button button = buttonObject.GetComponent<Button>();

        int capturedIndex = routeIndex;
        button.onClick.AddListener(() => SelectRoute(capturedIndex));

        routeButtons.Add(button);
    }

    void SelectRoute(int routeIndex)
    {
        List<Airport> routes = GameManager.Instance.GetRoutes();

        if (routes == null || routeIndex < 0 || routeIndex >= routes.Count)
            return;

        Airport selectedRoute = routes[routeIndex];

        GameManager.Instance.SetMainUi(false);
        GameManager.Instance.CRout(routeIndex);

        SetGui(GuiGroup.RouteInfo, true);
        SetCreateButtonVisible(false);

        InvokeRepeating(nameof(UpdateSelectedRouteMetrics), 0f, 0.3f);

        Debug.Log("Selected route: " + selectedRoute.AirportName);
    }

    void UpdateSelectedRouteMetrics()
    {
        List<Plane> activePlanes = GameManager.Instance.GetPlanesInRoute();

        Airport origin = GameManager.Instance.CurrentAirport.GetComponent<Airport>();
        Airport destination = GameManager.Instance.GetcR();

        if (origin == null || destination == null)
            return;

        originAirportName.text = origin.AirportName;

        Airport.RouteStats historicStats = origin.routeStats.ContainsKey(destination)
            ? origin.routeStats[destination]
            : new Airport.RouteStats();

        int totalTrips = historicStats.totalTravels;
        float totalRevenue = historicStats.totalMoney;
        int totalPassengers = historicStats.totalPass;

        foreach (Plane plane in activePlanes)
        {
            totalTrips += plane.routeTravels;
            totalRevenue += plane.routeMoney;
            totalPassengers += plane.routePass;
        }

        float distance = Vector2.Distance(
            GameManager.Instance.CurrentAirport.position,
            destination.transform.position
        ) * Plane.WORLD_TO_MILES;

        float averageRevenue = totalTrips > 0
            ? totalRevenue / totalTrips
            : 0f;

        float averagePassengers = totalTrips > 0
            ? (float)totalPassengers / totalTrips
            : 0f;

        routeInfoTexts[(int)RouteInfoField.Name].text =
            "Route: " + origin.AirportName + " → " + destination.AirportName;

        routeInfoTexts[(int)RouteInfoField.Distance].text =
            "Distance: " + distance.ToString("F0") + " mi";

        routeInfoTexts[(int)RouteInfoField.TotalRevenue].text =
            "Total: $" + totalRevenue.ToString("F0");

        routeInfoTexts[(int)RouteInfoField.AverageRevenue].text =
            "Avg: $" + averageRevenue.ToString("F0");

        routeInfoTexts[(int)RouteInfoField.TotalPassengers].text =
            "Pax: " + totalPassengers;

        routeInfoTexts[(int)RouteInfoField.AveragePassengers].text =
            "Avg Pax: " + averagePassengers.ToString("F0");
    }

    void ClearRouteButtons()
    {
        foreach (Button button in routeButtons)
        {
            if (button != null)
                Destroy(button.gameObject);
        }

        routeButtons.Clear();
    }

    void SetGui(GuiGroup group, bool active)
    {
        int index = (int)group;

        if (index < 0 || index >= guiGroups.Length)
            return;

        if (guiGroups[index] != null)
            guiGroups[index].SetActive(active);
    }

    void SetPanel(PanelGroup panel, bool active)
    {
        int index = (int)panel;

        if (index < 0 || index >= panels.Length)
            return;

        if (panels[index] != null)
            panels[index].SetActive(active);
    }

    void SetCreateButtonVisible(bool visible)
    {
        Button createButton = buttons[(int)RouteButtonAction.CreateRoute];

        if (createButton != null)
            createButton.gameObject.SetActive(visible);
    }

    Color GetAirportTypeColor(int airportType)
    {
        switch (airportType)
        {
            case 0:
                ColorUtility.TryParseHtmlString("#52b788", out Color regional);
                return regional;

            case 1:
                ColorUtility.TryParseHtmlString("#e07b39", out Color capital);
                return capital;

            case 2:
                ColorUtility.TryParseHtmlString("#2a9d8f", out Color international);
                return international;

            default:
                return Color.white;
        }
    }
}
