using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Representative sample extracted from Transit Empire's aircraft information UI.
///
/// This sample shows how the UI adapts to multiple aircraft contexts:
/// - shop preview
/// - garage inspection
/// - equipped aircraft
/// - route assignment
/// - active route fleet view
/// - aircraft currently flying
///
/// It also demonstrates live stat refresh, aircraft renaming,
/// stat upgrades, maintenance toggling, and contextual action routing.
/// </summary>
public class InformationManagerSample : MonoBehaviour
{
    [Header("Stat UI")]
    public List<Image> statBars;
    public List<TextMeshProUGUI> statTexts;
    public List<TextMeshProUGUI> infoTexts;

    [Header("Progression UI")]
    public Image xpBar;
    public TextMeshProUGUI xpText;
    public TextMeshProUGUI statPointsText;
    public TextMeshProUGUI previousLevelText;
    public TextMeshProUGUI nextLevelText;

    [Header("Interaction UI")]
    public Button[] actionButtons;
    public List<Button> upgradeButtons;
    public Button maintenanceButton;
    public TMP_InputField nameInput;

    [Header("Panels")]
    public GameObject[] panels;
    public GameObject mainUI;
    public Image planeImage;

    private enum ActionButton
    {
        Buy = 0,
        Cancel = 1,

        Equip = 2,
        Delete = 3,
        RemoveFromEquipped = 4,
        ExitSecondary = 5,

        Assignipped = 4,
        ExitSecondary = 5,

        AssignToRoute = 6,
        ExitRouteAssignment = 7,

        ReturnToGarage = 8,
        ExitRouteFleetMode = 9,
        RemoveFromRoute = 10,

        ExitFlyingMode = 11
    }

    private enum Panel
    {
        Information = 0,
        Shop = 1,
        Garage = 2,
        Route = 3
    }

    void Start()
    {
        BindActionButtons();
        BindUpgradeButtons();

        maintenanceButton.onClick.AddListener(EnableMaintenanceMode);
    }

    void OnEnable()
    {
        GameManager.Instance.SetMainUi(false);

        if (mainUI != null)
            mainUI.SetActive(false);

        InvokeRepeating(nameof(RefreshAircraftInfo), 0f, 0.4f);

        ResetUI();

        AircraftContext context = ReadCurrentContext();

        if (!context.HasAnyAircraft)
            return;

        ShowBaseStats(context);
        ShowNameOrCatalogTitle(context);
        ConfigureContextualMode(context);
    }

    void OnDisable()
    {
        if (mainUI != null)
            mainUI.SetActive(true);

        GameManager.Instance.SetMainUi(true);
        GameManager.Instance.PlaneFlying(false);

        CancelInvoke(nameof(RefreshAircraftInfo));
    }

    // ---------------------------------------------------------------------
    // Context
    // ---------------------------------------------------------------------

    private struct AircraftContext
    {
        public GameManager.PlaneData CatalogPlane;
        public Plane RuntimePlane;

        public bool InGarage;
        public bool Equipped;
        public bool AssigningRoute;
        public bool RouteFleetMode;
        public bool Flying;

        public bool HasCatalogPlane => CatalogPlane != null;
        public bool HasRuntimePlane => RuntimePlane != null;
        public bool HasAnyAircraft => CatalogPlane != null || RuntimePlane != null;
    }

    private AircraftContext ReadCurrentContext()
    {
        return new AircraftContext
        {
            CatalogPlane = GameManager.Instance.GetSelectedPlane(),
            RuntimePlane = GameManager.Instance.SelectedGaragePlane,

            InGarage = GameManager.Instance.GetGarageState(),
            Equipped = GameManager.Instance.getequippedstate(),
            AssigningRoute = GameManager.Instance.getequipmode(),
            RouteFleetMode = GameManager.Instance.getfloat(),
            Flying = GameManager.Instance.PlaneIsFlying()
        };
    }

    // ---------------------------------------------------------------------
    // UI setup
    // ---------------------------------------------------------------------

    private void ResetUI()
    {
        SetAllButtons(false);
        SetAllStats(false);
        SetAllInfo(false);
        SetAllUpgradeButtons(false);

        nameInput.gameObject.SetActive(false);
        maintenanceButton.gameObject.SetActive(false);

        xpBar.gameObject.SetActive(false);
        xpText.gameObject.SetActive(false);
        statPointsText.gameObject.SetActive(false);
        previousLevelText.gameObject.SetActive(false);
        nextLevelText.gameObject.SetActive(false);
    }

    private void ConfigureContextualMode(AircraftContext context)
    {
        if (context.AssigningRoute)
        {
            ShowRuntimePlaneDetails(true);

            ShowButton(ActionButton.ExitRouteAssignment, true);

            if (GameManager.Instance.calctheRTButton())
                ShowButton(ActionButton.RemoveFromRoute, true);
            else
                ShowButton(ActionButton.AssignToRoute, true);

            return;
        }

        if (context.RouteFleetMode)
        {
            ShowRuntimePlaneDetails(true);

            ShowButton(ActionButton.ReturnToGarage, true);
            ShowButton(ActionButton.ExitRouteFleetMode, true);
            ShowButton(ActionButton.RemoveFromRoute, true);

            return;
        }

        if (context.Equipped)
        {
            ShowRuntimePlaneDetails(true);

            ShowButton(ActionButton.RemoveFromEquipped, true);
            ShowButton(ActionButton.ExitSecondary, true);

            return;
        }

        if (context.InGarage)
        {
            ShowRuntimePlaneDetails(true);

            ShowButton(ActionButton.Equip, true);
            ShowButton(ActionButton.Delete, true);
            ShowButton(ActionButton.ExitSecondary, true);

            return;
        }

        if (context.Flying)
        {
            ShowRuntimePlaneDetails(true);

            ShowButton(ActionButton.ExitFlyingMode, true);

            return;
        }

        ShowShopPreview(context);
    }

    private void ShowShopPreview(AircraftContext context)
    {
        if (!context.HasCatalogPlane)
            return;

        infoTexts[1].text = "$" + GameManager.MoneyFormat(context.CatalogPlane.Price);
        infoTexts[1].gameObject.SetActive(true);

        if (planeImage != null)
            planeImage.sprite = PlaneSpritesManager.Instance.GetSprite(
                Mathf.Max(0, context.CatalogPlane.Type - 1)
            );

        maintenanceButton.gameObject.SetActive(false);
        ShowProgressionBlock(false);

        ShowButton(ActionButton.Buy, true);
        ShowButton(ActionButton.Cancel, true);
    }

    private void ShowRuntimePlaneDetails(bool active)
    {
        statBars[5].gameObject.SetActive(active);
        statBars[6].gameObject.SetActive(active);

        statTexts[5].gameObject.SetActive(active);
        statTexts[6].gameObject.SetActive(active);

        infoTexts[3].gameObject.SetActive(active);
        infoTexts[4].gameObject.SetActive(active);
        infoTexts[5].gameObject.SetActive(active);
        infoTexts[6].gameObject.SetActive(active);

        nameInput.gameObject.SetActive(active);
        maintenanceButton.gameObject.SetActive(active);

        ShowProgressionBlock(active);
    }

    private void ShowProgressionBlock(bool active)
    {
        xpBar.gameObject.SetActive(active);
        xpText.gameObject.SetActive(active);
        statPointsText.gameObject.SetActive(active);
        previousLevelText.gameObject.SetActive(active);
        nextLevelText.gameObject.SetActive(active);

        SetAllUpgradeButtons(active);
    }

    private void ShowNameOrCatalogTitle(AircraftContext context)
    {
        if (context.HasRuntimePlane)
        {
            nameInput.text = context.RuntimePlane.PlaneName;
            nameInput.gameObject.SetActive(true);

            nameInput.onEndEdit.RemoveAllListeners();
            nameInput.onEndEdit.AddListener(newName =>
            {
                context.RuntimePlane.PlaneName = newName;
                context.RuntimePlane.gameObject.name =
                    newName + " [" + context.RuntimePlane.planetype + "]";
            });

            return;
        }

        if (context.HasCatalogPlane)
        {
            infoTexts[0].text = context.CatalogPlane.Name;
            infoTexts[0].gameObject.SetActive(true);
        }
    }

    private void ShowBaseStats(AircraftContext context)
    {
        for (int i = 0; i < 5; i++)
        {
            statBars[i].gameObject.SetActive(true);
            statTexts[i].gameObject.SetActive(true);
        }

        float height = context.HasRuntimePlane
            ? context.RuntimePlane.height
            : context.CatalogPlane.Height;

        infoTexts[2].text = "Height: " + height;
        infoTexts[2].gameObject.SetActive(true);
    }

    // ---------------------------------------------------------------------
    // Live refresh
    // ---------------------------------------------------------------------

    private void RefreshAircraftInfo()
    {
        GameManager.Instance.SetMainUi(false);

        AircraftContext context = ReadCurrentContext();

        if (!context.HasAnyAircraft)
            return;

        UpdatePrimaryStats(context);

        if (!context.HasRuntimePlane)
            return;

        UpdateRuntimeStats(context.RuntimePlane);
        UpdateProgression(context.RuntimePlane);
        UpdateRouteText(context.RuntimePlane);
    }

    private void UpdatePrimaryStats(AircraftContext context)
    {
        float speed = context.HasRuntimePlane
            ? context.RuntimePlane.speedvalue
            : context.CatalogPlane.Speed;

        float consume = context.HasRuntimePlane
            ? context.RuntimePlane.consumevalue
            : context.CatalogPlane.Consume;

        float capacity = context.HasRuntimePlane
            ? context.RuntimePlane.capacityvalue
            : context.CatalogPlane.Capacity;

        float range = context.HasRuntimePlane
            ? context.RuntimePlane.rangevalue
            : context.CatalogPlane.Range;

        float reliability = context.HasRuntimePlane
            ? context.RuntimePlane.reliabilityvalue
            : context.CatalogPlane.Reliability;

        float[] values =
        {
            speed,
            consume,
            capacity,
            range,
            reliability
        };

        for (int i = 0; i < values.Length && i < statBars.Count; i++)
        {
            statBars[i].fillAmount = values[i] / 10f;
            statTexts[i].text = values[i] + " / 10";
        }

        if (planeImage != null && context.HasRuntimePlane)
        {
            planeImage.sprite = PlaneSpritesManager.Instance.GetSprite(
                Mathf.Max(0, context.RuntimePlane.planetype - 1)
            );
        }
    }

    private void UpdateRuntimeStats(Plane plane)
    {
        statBars[5].fillAmount = plane.hp / (float)plane.maxHP;
        statBars[6].fillAmount = plane.wear / 100f;

        statTexts[5].text = plane.hp + " / " + plane.maxHP;
        statTexts[6].text = plane.wear.ToString("F0") + "%";

        infoTexts[3].text = "State: " + plane.currentState;
        infoTexts[4].text = "Travels: " + plane.travels;
        infoTexts[5].text = "Avg: $" + plane.average.ToString("F0");
    }

    private void UpdateProgression(Plane plane)
    {
        xpBar.fillAmount = plane.XPPercent();

        previousLevelText.text = plane.level.ToString();
        nextLevelText.text = (plane.level + 1).ToString();

        xpText.text =
            plane.xp.ToString("F0") +
            " / " +
            plane.XPRequired;

        statPointsText.text = "Stat Points: " + plane.statPoints;
    }

    private void UpdateRouteText(Plane plane)
    {
        if (plane.cityA != null && plane.cityB != null && plane.cityB != plane.cityA)
        {
            infoTexts[6].text =
                "Route: " +
                plane.cityA.name +
                " → " +
                plane.cityB.name;
        }
        else
        {
            infoTexts[6].text = "No Route";
        }
    }

    // ---------------------------------------------------------------------
    // Actions
    // ---------------------------------------------------------------------

    private void HandleButton(ActionButton button)
    {
        AircraftContext context = ReadCurrentContext();

        SetPanel(Panel.Information, false);

        if (context.AssigningRoute)
        {
            HandleRouteAssignmentButton(button);
            return;
        }

        if (context.RouteFleetMode)
        {
            HandleRouteFleetButton(button);
            return;
        }

        HandleDefaultButton(button, context);
    }

    private void HandleRouteAssignmentButton(ActionButton button)
    {
        switch (button)
        {
            case ActionButton.AssignToRoute:
                if (GameManager.Instance.AddPlaneToRoute())
                {
                    SetPanel(Panel.Garage, true);
                    SetPanel(Panel.Information, false);
                }
                else
                {
                    SetPanel(Panel.Garage, false);
                    SetPanel(Panel.Information, true);
                }
                break;

            case ActionButton.ExitRouteAssignment:
                GameManager.Instance.setequimode(true);
                SetPanel(Panel.Garage, true);
                break;

            case ActionButton.RemoveFromRoute:
                RemovePlaneFromRouteIfAllowed();
                break;
        }
    }

    private void HandleRouteFleetButton(ActionButton button)
    {
        switch (button)
        {
            case ActionButton.RemoveFromRoute:
                RemovePlaneFromRouteIfAllowed();
                break;

            case ActionButton.ExitRouteFleetMode:
                GameManager.Instance.SetFloat(true);
                break;
        }
    }

    private void HandleDefaultButton(ActionButton button, AircraftContext context)
    {
        switch (button)
        {
            case ActionButton.Buy:
                if (GameManager.Instance.BuyPlane())
                    SetPanel(Panel.Shop, true);
                break;

            case ActionButton.Cancel:
                if (context.InGarage)
                {
                    GameManager.Instance.IsGarage(true);
                    gameObject.SetActive(false);
                }
                else
                {
                    SetPanel(Panel.Shop, true);
                }
                break;

            case ActionButton.Equip:
                GameManager.Instance.EquipPlane();
                SetPanel(Panel.Garage, true);
                break;

            case ActionButton.Delete:
                GameManager.Instance.DeletePlane();
                SetPanel(Panel.Garage, true);
                break;

            case ActionButton.RemoveFromEquipped:
                if (CanModifySelectedPlane())
                {
                    GameManager.Instance.PlaneJobless();
                    SetPanel(Panel.Garage, true);
                }
                else
                {
                    SetPanel(Panel.Garage, false);
                    SetPanel(Panel.Information, true);
                }
                break;
        }
    }

    private void RemovePlaneFromRouteIfAllowed()
    {
        if (!CanModifySelectedPlane())
        {
            SetPanel(Panel.Garage, false);
            SetPanel(Panel.Information, true);
            return;
        }

        GameManager.Instance.DeletePlaneRTmode();
        SetPanel(Panel.Garage, true);
    }

    private bool CanModifySelectedPlane()
    {
        Plane plane = GameManager.Instance.SelectedGaragePlane;

        if (plane == null)
            return false;

        return plane.currentState != Plane.PlaneState.Flying &&
               plane.currentState != Plane.PlaneState.Arrived;
    }

    private void EnableMaintenanceMode()
    {
        Plane plane = GameManager.Instance.SelectedGaragePlane;

        if (plane == null)
            return;

        plane.CanRepair = true;
        maintenanceButton.GetComponent<Image>().color = Color.green;
    }

    private void UpgradeSelectedAircraftStat(int statIndex)
    {
        Plane plane = GameManager.Instance.SelectedGaragePlane;

        if (plane == null)
            return;

        plane.UpgradeStat(statIndex);
    }

    // ---------------------------------------------------------------------
    // Binding / helpers
    // ---------------------------------------------------------------------

    private void BindActionButtons()
    {
        for (int i = 0; i < actionButtons.Length; i++)
        {
            int index = i;
            actionButtons[i].onClick.AddListener(() =>
                HandleButton((ActionButton)index)
            );
        }
    }

    private void BindUpgradeButtons()
    {
        for (int i = 0; i < upgradeButtons.Count; i++)
        {
            int index = i;
            upgradeButtons[i].onClick.AddListener(() =>
                UpgradeSelectedAircraftStat(index)
            );
        }
    }

    private void SetAllButtons(bool active)
    {
        foreach (Button button in actionButtons)
            button.gameObject.SetActive(active);
    }

    private void SetAllStats(bool active)
    {
        foreach (Image stat in statBars)
            stat.gameObject.SetActive(active);

        foreach (TextMeshProUGUI text in statTexts)
            text.gameObject.SetActive(active);
    }

    private void SetAllInfo(bool active)
    {
        foreach (TextMeshProUGUI text in infoTexts)
            text.gameObject.SetActive(active);
    }

    private void SetAllUpgradeButtons(bool active)
    {
        foreach (Button button in upgradeButtons)
            button.gameObject.SetActive(active);
    }

    private void ShowButton(ActionButton button, bool active)
    {
        int index = (int)button;

        if (index < 0 || index >= actionButtons.Length)
            return;

        actionButtons[index].gameObject.SetActive(active);
    }

    private void SetPanel(Panel panel, bool active)
    {
        int index = (int)panel;

        if (index < 0 || index >= panels.Length)
            return;

        if (panels[index] != null)
            panels[index].SetActive(active);
    }
}
