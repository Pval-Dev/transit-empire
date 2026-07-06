using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using System.Linq;

/// <summary>
/// Representative sample extracted from Transit Empire's financing system.
///
/// This sample shows how the simulation exposes capital through loans and investors:
/// - fixed repayment loans based on current cash
/// - revenue-based loans based on operational performance
/// - investor offers that trade cash for company autonomy
/// - active debt management with automatic payment toggles
/// </summary>
public class LoanManagerSample : MonoBehaviour
{
    [Header("Active Fixed Loans")]
    public List<float> fixedLoanBalances = new List<float>();
    public List<float> fixedLoanDailyPayments = new List<float>();
    public List<bool> fixedLoanAutoPayEnabled = new List<bool>();
    public List<string> fixedLoanLenders = new List<string>();

    [Header("Active Revenue Loans")]
    public List<float> revenueLoanBalances = new List<float>();
    public List<float> revenueLoanDailyPayments = new List<float>();
    public List<bool> revenueLoanAutoPayEnabled = new List<bool>();
    public List<string> revenueLoanLenders = new List<string>();

    [Header("Active Investors")]
    public List<float> investorContributions = new List<float>();
    public List<float> investorRevenueShares = new List<float>();
    public List<string> investorNames = new List<string>();

    [Header("Loan Rules")]
    public int maxLoans = 3;
    public int currentLoans = 0;
    public float penaltyThreshold = 100f;
    public float earlyPaymentDiscount = 0.10f;

    [Header("Offer Configuration")]
    public float[] fixedLoanPercentages = { 0.25f, 0.50f, 0.75f };
    public int[] fixedLoanTermDays = { 60, 130, 200 };
    public float[] fixedLoanInterestRates = { 0.10f, 0.15f, 0.25f };

    public float[] revenueLoanMultipliers = { 25f, 75f, 150f };
    public float[] revenueLoanInterestRates = { 0.05f, 0.10f, 0.18f };

    [Header("UI")]
    public Button openLoanPanelButton;
    public Button findInvestorsButton;
    public Button openActiveDebtButton;

    public Transform fixedLoanContainer;
    public Transform revenueLoanContainer;
    public Transform investorOfferContainer;
    public Transform activeFixedLoanContainer;
    public Transform activeRevenueLoanContainer;
    public Transform activeInvestorContainer;

    public GameObject loanOfferPrefab;
    public GameObject activeLoanPrefab;
    public GameObject activeInvestorPrefab;

    public int investorSearchAttempts = 3;

    private readonly string[] bankNames =
    {
        "Meridian Bank",
        "Atlas Financial",
        "Horizon Credit",
        "Ironvault Bank",
        "Redstone Capital",
        "Goldleaf Trust"
    };

    private readonly string[] revenueBankNames =
    {
        "Zephyr Lending",
        "Crimson Finance",
        "Obsidian Credit",
        "Titan Bank",
        "Nexus Capital",
        "Solaris Bank"
    };

    private readonly string[] possibleInvestors =
    {
        "Victor Crane",
        "Elena Voss",
        "Sofia Reyes",
        "Ava Sterling",
        "Clara Mendez",
        "Owen Frost"
    };

    void Start()
    {
        openLoanPanelButton.onClick.AddListener(OpenLoanCreationPanel);
        findInvestorsButton.onClick.AddListener(SearchForInvestors);
        openActiveDebtButton.onClick.AddListener(OpenActiveDebtPanel);
    }

    void OpenLoanCreationPanel()
    {
        GameManager.Instance.SetMainUi(false);

        ClearContainer(fixedLoanContainer);
        ClearContainer(revenueLoanContainer);

        CreateFixedLoanOffers();
        CreateRevenueLoanOffers();
    }

    void CreateFixedLoanOffers()
    {
        for (int i = 0; i < fixedLoanPercentages.Length; i++)
        {
            float principal = GameManager.Instance.Money * fixedLoanPercentages[i];
            int termDays = fixedLoanTermDays[i];
            float interestRate = fixedLoanInterestRates[i];
            string lender = PickRandom(bankNames);

            GameObject offer = Instantiate(loanOfferPrefab, fixedLoanContainer);
            FillLoanOfferUI(offer, lender, principal, interestRate);

            Button button = offer.GetComponentInChildren<Button>();
            button.onClick.AddListener(() =>
                AcceptFixedLoan(principal, termDays, interestRate, lender));
        }
    }

    void CreateRevenueLoanOffers()
    {
        float averageRouteIncome = CalculateAverageAircraftIncome();

        for (int i = 0; i < revenueLoanMultipliers.Length; i++)
        {
            float principal = averageRouteIncome * revenueLoanMultipliers[i];
            int termDays = Mathf.RoundToInt(revenueLoanMultipliers[i] * 2f);
            float interestRate = revenueLoanInterestRates[i];
            string lender = PickRandom(revenueBankNames);

            GameObject offer = Instantiate(loanOfferPrefab, revenueLoanContainer);
            FillLoanOfferUI(offer, lender, principal, interestRate);

            Button button = offer.GetComponentInChildren<Button>();
            button.onClick.AddListener(() =>
                AcceptRevenueLoan(principal, termDays, interestRate, lender));
        }
    }

    void AcceptFixedLoan(float principal, int termDays, float interestRate, string lender)
    {
        if (!CanAcceptLoan())
            return;

        float balance = principal * (1f + interestRate / 100f);
        float dailyPayment = balance / termDays;

        fixedLoanBalances.Add(balance);
        fixedLoanDailyPayments.Add(dailyPayment);
        fixedLoanAutoPayEnabled.Add(false);
        fixedLoanLenders.Add(lender);

        currentLoans++;

        GameManager.Instance.CashMovement(principal);

        NotifyFinanceEvent(
            "LOAN APPROVED",
            lender + " approved " + GameManager.MoneyFormat(principal),
            iconIndex: 3
        );
    }

    void AcceptRevenueLoan(float principal, int termDays, float interestRate, string lender)
    {
        if (!CanAcceptLoan())
            return;

        float balance = principal * (1f + interestRate / 100f);
        float dailyPayment = balance / termDays;

        revenueLoanBalances.Add(balance);
        revenueLoanDailyPayments.Add(dailyPayment);
        revenueLoanAutoPayEnabled.Add(false);
        revenueLoanLenders.Add(lender);

        currentLoans++;

        GameManager.Instance.CashMovement(principal);

        NotifyFinanceEvent(
            "REVENUE LOAN APPROVED",
            lender + " approved " + GameManager.MoneyFormat(principal),
            iconIndex: 3
        );
    }

    bool CanAcceptLoan()
    {
        if (currentLoans >= maxLoans)
        {
            NotificatorManager.CreateNotifications(
                "MAX LOANS REACHED",
                "Increase reputation to unlock additional financing capacity.",
                4,
                0
            );

            return false;
        }

        if (penaltyThreshold < 50f)
        {
            NotificatorManager.CreateNotifications(
                "LOAN BLOCKED",
                "Pay existing debts before requesting additional financing.",
                4,
                1
            );

            return false;
        }

        return true;
    }

    void SearchForInvestors()
    {
        if (investorSearchAttempts <= 0)
        {
            NotificatorManager.CreateNotifications(
                "NO ATTEMPTS LEFT",
                "Wait until the next month to search again.",
                4,
                1
            );

            return;
        }

        investorSearchAttempts--;

        ClearContainer(investorOfferContainer);

        float reputation = GameManager.Instance.GlobalReputation;
        int aggressiveOffers = Mathf.RoundToInt(Mathf.Lerp(3, 0, reputation / 100f));

        for (int i = 0; i < 3; i++)
        {
            bool aggressive = i < aggressiveOffers;

            string investor = PickRandom(possibleInvestors);
            float amount = CalculateInvestorAmount(reputation);

            if (amount <= 0f)
                continue;

            float revenueShare = aggressive
                ? Random.Range(0.20f, 0.35f)
                : Random.Range(0.03f, 0.15f);

            GameObject offer = Instantiate(loanOfferPrefab, investorOfferContainer);
            FillInvestorOfferUI(offer, investor, amount, revenueShare);

            Button button = offer.GetComponentInChildren<Button>();
            button.onClick.AddListener(() =>
                AcceptInvestor(investor, amount, revenueShare));
        }
    }

    float CalculateInvestorAmount(float reputation)
    {
        float averageRouteIncome = CalculateAverageAircraftIncome();

        float confidence =
            GameManager.Instance.PlanesInRoute.Count * 0.3f +
            GameManager.Instance.GlobalApList.Count * 0.5f +
            reputation / 100f;

        return averageRouteIncome * Random.Range(80f, 150f) * confidence;
    }

    void AcceptInvestor(string investor, float amount, float revenueShare)
    {
        investorContributions.Add(amount);
        investorRevenueShares.Add(revenueShare);
        investorNames.Add(investor);

        GameManager.Instance.Autonomy -= revenueShare * 100f;
        GameManager.Instance.CashMovement(amount);

        NotifyFinanceEvent(
            "INVESTOR ACCEPTED",
            investor + " invested " + GameManager.MoneyFormat(amount),
            iconIndex: 1
        );

        ClearContainer(investorOfferContainer);
    }

    void OpenActiveDebtPanel()
    {
        GameManager.Instance.SetMainUi(false);

        CreateActiveLoanRows(
            fixedLoanBalances,
            fixedLoanDailyPayments,
            fixedLoanAutoPayEnabled,
            fixedLoanLenders,
            activeFixedLoanContainer
        );

        CreateActiveLoanRows(
            revenueLoanBalances,
            revenueLoanDailyPayments,
            revenueLoanAutoPayEnabled,
            revenueLoanLenders,
            activeRevenueLoanContainer
        );

        CreateActiveInvestorRows();
    }

    void CreateActiveLoanRows(
        List<float> balances,
        List<float> dailyPayments,
        List<bool> autoPayFlags,
        List<string> lenders,
        Transform container)
    {
        ClearContainer(container);

        for (int i = balances.Count - 1; i >= 0; i--)
        {
            int index = i;

            GameObject row = Instantiate(activeLoanPrefab, container);
            TextMeshProUGUI[] texts = row.GetComponentsInChildren<TextMeshProUGUI>();
            Button[] buttons = row.GetComponentsInChildren<Button>();

            float earlyPaymentTotal = balances[i] * (1f - earlyPaymentDiscount);

            texts[0].text = GameManager.MoneyFormat(earlyPaymentTotal) + " total with early payment discount";
            texts[1].text = GameManager.MoneyFormat(dailyPayments[i]) + " per day";
            texts[2].text = lenders[i];

            buttons[0].onClick.AddListener(() =>
                ToggleAutoPayment(index, autoPayFlags, buttons[0]));

            buttons[1].onClick.AddListener(() =>
                PayLoanInFull(index, balances, dailyPayments, autoPayFlags, lenders));
        }
    }

    void CreateActiveInvestorRows()
    {
        ClearContainer(activeInvestorContainer);

        for (int i = investorContributions.Count - 1; i >= 0; i--)
        {
            GameObject row = Instantiate(activeInvestorPrefab, activeInvestorContainer);
            TextMeshProUGUI[] texts = row.GetComponentsInChildren<TextMeshProUGUI>();

            texts[0].text = (investorRevenueShares[i] * 100f).ToString("F0") + "% of company revenue";
            texts[1].text = GameManager.MoneyFormat(investorContributions[i]) + " invested";
            texts[2].text = investorNames[i];
        }
    }

    void ToggleAutoPayment(int index, List<bool> autoPayFlags, Button button)
    {
        if (index < 0 || index >= autoPayFlags.Count)
            return;

        autoPayFlags[index] = !autoPayFlags[index];

        button.image.color = autoPayFlags[index]
            ? Color.green
            : Color.red;

        NotifyFinanceEvent(
            autoPayFlags[index] ? "AUTO PAYMENT ENABLED" : "AUTO PAYMENT DISABLED",
            autoPayFlags[index] ? "Daily payments enabled." : "Daily payments disabled.",
            iconIndex: autoPayFlags[index] ?  3 : 1
        );
    }

    void PayLoanInFull(
        int index,
        List<float> balances,
        List<float> dailyPayments,
        List<bool> autoPayFlags,
        List<string> lenders)
    {
        if (index < 0 || index >= balances.Count)
            return;

        float total = balances[index] * (1f - earlyPaymentDiscount);

        if (GameManager.Instance.Money < total)
            return;

        GameManager.Instance.CashMovement(-total);

        balances.RemoveAt(index);
        dailyPayments.RemoveAt(index);
        autoPayFlags.RemoveAt(index);
        lenders.RemoveAt(index);

        currentLoans--;

        NotifyFinanceEvent(
            "LOAN FINISHED",
            "Debt fully repaid.",
            iconIndex: 0
        );

        OpenActiveDebtPanel();
    }

    /// <summary>
    /// Called externally by the simulation lifecycle once per day.
    /// In the full project, this daily settlement is coordinated by GameManager.
    /// </summary>
    public void ProcessDailyPayments()
    {
        ProcessLoanGroup(fixedLoanBalances, fixedLoanDailyPayments, fixedLoanAutoPayEnabled);
        ProcessLoanGroup(revenueLoanBalances, revenueLoanDailyPayments, revenueLoanAutoPayEnabled);
    }

    void ProcessLoanGroup(
        List<float> balances,
        List<float> dailyPayments,
        List<bool> autoPayFlags)
    {
        for (int i = balances.Count - 1; i >= 0; i--)
        {
            if (!autoPayFlags[i])
                continue;

            float payment = Mathf.Min(dailyPayments[i], balances[i]);

            if (GameManager.Instance.Money < payment)
            {
                penaltyThreshold -= 5f;
                continue;
            }

            GameManager.Instance.CashMovement(-payment);
            balances[i] -= payment;

            if (balances[i] <= 0f)
            {
                balances.RemoveAt(i);
                dailyPayments.RemoveAt(i);
                autoPayFlags.RemoveAt(i);
                currentLoans--;
            }
        }
    }

    float CalculateAverageAircraftIncome()
    {
        if (GameManager.Instance.PlanesInRoute.Count == 0)
            return 0f;

        return GameManager.Instance.PlanesInRoute.Sum(plane => plane.average) /
               GameManager.Instance.PlanesInRoute.Count;
    }

    void FillLoanOfferUI(GameObject offer, string lender, float amount, float interestRate)
    {
        TextMeshProUGUI[] texts = offer.GetComponentsInChildren<TextMeshProUGUI>();

        texts[0].text = lender;
        texts[1].text = GameManager.MoneyFormat(amount);
        texts[2].text = interestRate.ToString("F2") + "% interest";
    }

    void FillInvestorOfferUI(GameObject offer, string investor, float amount, float revenueShare)
    {
        TextMeshProUGUI[] texts = offer.GetComponentsInChildren<TextMeshProUGUI>();

        texts[0].text = investor;
        texts[1].text = GameManager.MoneyFormat(amount);
        texts[2].text = (revenueShare * 100f).ToString("F0") + "% of revenue";
    }

    void ClearContainer(Transform container)
    {
        foreach (Transform child in container)
            Destroy(child.gameObject);
    }

    string PickRandom(string[] values)
    {
        return values[Random.Range(0, values.Length)];
    }

    void NotifyFinanceEvent(string title, string message, int iconIndex)
    {
        NotificatorManager.CreateNotifications(
            title,
            message,
            3,
            iconIndex
        );
    }
}
